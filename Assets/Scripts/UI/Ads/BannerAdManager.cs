using GoogleMobileAds.Api;
using BlockPuzzle.Core.Common;
using UnityEngine;
using System;
using BlockPuzzle.Core.Monetization;

/// <summary>
/// Banner reklam yukleme ve gorunurluk yonetimi.
/// </summary>
public class BannerAdManager : MonoBehaviour
{
    private const float RetryBaseDelaySeconds = 5f;
    private const float RetryMaxDelaySeconds = 60f;
    private const float LoadTimeoutSeconds = 20f;
    private BannerView _bannerView;
    private bool _isLoading;
    private bool _isVisible;
    private bool _shouldBeVisible;
    private string _cachedAdUnitId;
    private int _lastMeasuredHeightInPixels;
    private int _retryAttempts;
    private bool _retryScheduled;
    private float _loadStartedAt;
    private int _loadGeneration;

    public event Action OnBannerLoaded;
    public event Action<string> OnBannerFailedToLoad;
    public event Action OnBannerShown;
    public event Action OnBannerHidden;
    public event Action<AdValue> OnBannerPaid;

    public bool IsVisible => _bannerView != null && _isVisible;
    public bool IsLoading => _isLoading;
    public bool HasAd => _bannerView != null && !_isLoading;
    public int CurrentOccupiedHeightInPixels => IsVisible ? GetMeasuredHeightInPixels() : 0;

    public void LoadBannerAd(string adUnitId)
    {
        if (!AdPolicyManager.AreAdsAllowed()) return;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow) return;
        GameLogger.Log("[Banner] LoadBannerAd called.");
        
        if (_isLoading)
        {
            GameLogger.LogWarning("[Banner] Banner zaten yukleniyor.");
            return;
        }

        if (_bannerView != null)
        {
            GameLogger.LogWarning("[Banner] Banner zaten olusturulmus.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            GameLogger.LogError("[Banner] CRITICAL: Ad unit id bos! Yukleme atlandi.");
            return;
        }

        _cachedAdUnitId = adUnitId;
        _isLoading = true;
        _loadStartedAt = Time.realtimeSinceStartup;

        int deviceWidth = ResolveAdaptiveBannerWidth();
        AdSize bannerSize = deviceWidth > 0
            ? AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(deviceWidth)
            : AdSize.Banner;

        if (deviceWidth <= 0)
            GameLogger.LogWarning("[Banner] Banner genisligi belirlenemedi. Son fallback olarak standart Banner boyutu kullaniliyor.");

        _bannerView = new BannerView(adUnitId, bannerSize, AdPosition.Bottom);
        int loadGeneration = ++_loadGeneration;
        RegisterLifecycleCallbacks(_bannerView, loadGeneration);
        _bannerView.LoadAd(AdRequestFactory.Create());

        _retryScheduled = false;
        GameLogger.Log($"[Banner] Banner yukleniyor... visibleWanted={_shouldBeVisible}, width={deviceWidth}");
    }

    private int ResolveAdaptiveBannerWidth()
    {
        int deviceSafeWidth = MobileAds.Utils.GetDeviceSafeWidth();
        if (deviceSafeWidth > 0)
            return deviceSafeWidth;

        int safeAreaWidth = Mathf.RoundToInt(Screen.safeArea.width);
        if (safeAreaWidth > 0)
        {
            GameLogger.LogWarning($"[Banner] Device safe width 0 geldi. Screen.safeArea.width fallback kullaniliyor: {safeAreaWidth}");
            return safeAreaWidth;
        }

        if (Screen.width > 0)
        {
            GameLogger.LogWarning($"[Banner] Device safe width 0 geldi. Screen.width fallback kullaniliyor: {Screen.width}");
            return Screen.width;
        }

        return 0;
    }

    public void ShowBannerAd()
    {
        _shouldBeVisible = true;

        if (_bannerView == null)
        {
            GameLogger.Log("[Banner] Banner henuz hazir degil. Yuklenince gosterilecek.");
            return;
        }

        ReapplyBottomPosition();
        _bannerView.Show();
        _isVisible = true;
        OnBannerShown?.Invoke();
        GameLogger.Log("[Banner] Banner gosteriliyor.");
    }

    public void HideBannerAd()
    {
        _shouldBeVisible = false;

        if (_bannerView == null)
            return;

        _bannerView.Hide();
        _isVisible = false;
        OnBannerHidden?.Invoke();
        GameLogger.Log("[Banner] Banner gizlendi.");
    }

    public void DestroyBannerAd()
    {
        _loadGeneration++;
        _isLoading = false;
        if (_bannerView == null)
            return;

        _bannerView.Destroy();
        _bannerView = null;
        _isVisible = false;
        _lastMeasuredHeightInPixels = 0;
        GameLogger.Log("[Banner] Banner yok edildi.");
    }

    private void Update()
    {
        if (!_isLoading || !AdRecoveryPolicy.HasTimedOut(_loadStartedAt, Time.realtimeSinceStartup, LoadTimeoutSeconds))
            return;

        DestroyBannerAd();
        OnBannerFailedToLoad?.Invoke("load_timeout");
        if (!_retryScheduled && !string.IsNullOrWhiteSpace(_cachedAdUnitId))
        {
            _retryAttempts++;
            _retryScheduled = true;
            Invoke(nameof(RetryLoadBannerAd), AdRecoveryPolicy.GetRetryDelaySeconds(_retryAttempts, RetryBaseDelaySeconds, RetryMaxDelaySeconds));
        }
        GameLogger.LogWarning("[Banner] Yukleme zaman asimina ugradi; kilit temizlendi.");
    }

    public void RefreshBannerLayout()
    {
        if (_bannerView == null)
            return;

        ReapplyBottomPosition();

        if (_shouldBeVisible)
        {
            _bannerView.Show();
            _isVisible = true;
            GameLogger.Log("[Banner] Layout refresh sonrasi banner tekrar gosterildi.");
        }
        else
        {
            _bannerView.Hide();
            _isVisible = false;
            GameLogger.Log("[Banner] Layout refresh sonrasi banner gizli tutuldu.");
        }
    }

    private void RetryLoadBannerAd()
    {
        _retryScheduled = false;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow)
            return;

        if (!string.IsNullOrWhiteSpace(_cachedAdUnitId))
            LoadBannerAd(_cachedAdUnitId);
    }

    private void RegisterLifecycleCallbacks(BannerView bannerView, int loadGeneration)
    {
        bannerView.OnBannerAdLoaded += () =>
        {
            if (loadGeneration == _loadGeneration)
                HandleOnBannerAdLoaded();
        };
        bannerView.OnBannerAdLoadFailed += error =>
        {
            if (loadGeneration == _loadGeneration)
                HandleOnBannerAdLoadFailed(error);
        };
        bannerView.OnAdPaid += adValue =>
        {
            if (loadGeneration == _loadGeneration)
                OnBannerPaid?.Invoke(adValue);
        };
        
        bannerView.OnAdClicked += () => 
        {
            if (loadGeneration != _loadGeneration)
                return;

            AdPolicyManager.RecordAdClick();
            if (!AdPolicyManager.AreAdsAllowed())
            {
                HideBannerAd();
                DestroyBannerAd();
            }
        };
    }

    private void HandleOnBannerAdLoaded()
    {
        _isLoading = false;
        _retryAttempts = 0;
        ReapplyBottomPosition();
        CacheMeasuredHeight();
        GameLogger.Log($"[Banner] Banner yuklendi. px={_bannerView?.GetWidthInPixels()}x{_bannerView?.GetHeightInPixels()} visibleWanted={_shouldBeVisible}");

        if (_shouldBeVisible)
        {
            _bannerView?.Show();
            _isVisible = true;
            OnBannerShown?.Invoke();
            GameLogger.Log("[Banner] Banner yuklenir yuklenmez gosterildi.");
        }
        else
        {
            _bannerView?.Hide();
            _isVisible = false;
            OnBannerHidden?.Invoke();
            GameLogger.Log("[Banner] Banner yuklendi ancak sahne politikasi nedeniyle gizli tutuldu.");
        }

        OnBannerLoaded?.Invoke();
    }

    private void HandleOnBannerAdLoadFailed(LoadAdError error)
    {
        _isLoading = false;
        _isVisible = false;
        DestroyBannerAd();

        string errorMessage = error?.GetMessage() ?? "unknown_load_error";
        GameLogger.LogWarning($"[Banner] Yuklenmesi basarisiz: {errorMessage}");
        OnBannerFailedToLoad?.Invoke(errorMessage);

        if (AdMobManager.ExistingInstance != null
            && AdMobManager.ExistingInstance.CanLoadAdsNow
            && !_retryScheduled
            )
        {
            _retryAttempts++;
            _retryScheduled = true;
            float retryDelay = Mathf.Min(
                RetryMaxDelaySeconds,
                RetryBaseDelaySeconds * Mathf.Pow(2f, Mathf.Max(0, _retryAttempts - 1)));
            Invoke(nameof(RetryLoadBannerAd), retryDelay);
        }
    }

    private void ReapplyBottomPosition()
    {
        if (_bannerView == null)
            return;

        _bannerView.SetPosition(AdPosition.Bottom);
        CacheMeasuredHeight();
    }

    private int GetMeasuredHeightInPixels()
    {
        CacheMeasuredHeight();
        return Mathf.Max(0, _lastMeasuredHeightInPixels);
    }

    private void CacheMeasuredHeight()
    {
        if (_bannerView == null)
        {
            _lastMeasuredHeightInPixels = 0;
            return;
        }

        try
        {
            float measuredHeight = _bannerView.GetHeightInPixels();
            if (measuredHeight > 0f)
                _lastMeasuredHeightInPixels = Mathf.CeilToInt(measuredHeight);
        }
        catch (System.Exception)
        {
            // Banner internal GameObject may be destroyed during scene transition
        }
    }

    private void OnDestroy()
    {
        DestroyBannerAd();
    }
}
