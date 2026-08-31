using GoogleMobileAds.Api;
using BlockPuzzle.Core.Common;
using UnityEngine;
using System;
using BlockPuzzle.Core.Monetization;

/// <summary>
/// Gecis reklami yasam dongusunu yonetir.
/// </summary>
public class InterstitialAdManager : MonoBehaviour
{
    private const float RetryBaseDelaySeconds = 5f;
    private const float RetryMaxDelaySeconds = 60f;
    private const float LoadTimeoutSeconds = 20f;
    private InterstitialAd _interstitialAd;
    private bool _isLoading;
    private string _cachedAdUnitId;
    private int _retryAttempts;
    private bool _retryScheduled;
    private float? _timeScaleBeforeFullscreen;
    private float _loadStartedAt;
    private int _loadGeneration;

    public bool IsLoading => _isLoading;

    public event Action OnInterstitialLoaded;
    public event Action<string> OnInterstitialFailedToLoad;
    public event Action OnInterstitialShown;
    public event Action OnInterstitialClosed;
    public event Action<AdValue> OnInterstitialPaid;

    public void LoadInterstitialAd(string adUnitId)
    {
        if (!AdPolicyManager.AreAdsAllowed()) return;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow) return;
        if (_isLoading)
        {
            GameLogger.Log("[Interstitial] Gecis reklami zaten yukleniyor.");
            return;
        }

        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            GameLogger.Log("[Interstitial] Gecis reklami zaten hazir.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            GameLogger.LogWarning("[Interstitial] Ad unit id bos oldugu icin yukleme atlandi.");
            return;
        }

        DestroyInterstitialAd();

        _cachedAdUnitId = adUnitId;
        _isLoading = true;
        _loadStartedAt = Time.realtimeSinceStartup;
        _retryScheduled = false;

        int loadGeneration = ++_loadGeneration;
        InterstitialAd.Load(adUnitId, AdRequestFactory.Create(), (ad, error) =>
        {
            if (loadGeneration != _loadGeneration)
            {
                ad?.Destroy();
                return;
            }

            HandleOnInterstitialAdLoaded(ad, error);
        });
        GameLogger.Log("[Interstitial] Gecis reklami yukleniyor...");
    }

    public void ShowInterstitialAd()
    {
        if (_interstitialAd == null || !_interstitialAd.CanShowAd())
        {
            GameLogger.LogWarning("[Interstitial] Gecis reklami gosterilemiyor - reklam hazir degil.");
            return;
        }

        _interstitialAd.Show();
        GameLogger.Log("[Interstitial] Gecis reklami gosteriliyor.");
    }

    public bool IsReady()
    {
        return _interstitialAd != null && _interstitialAd.CanShowAd();
    }

    public void DestroyInterstitialAd()
    {
        _loadGeneration++;
        _isLoading = false;
        if (_interstitialAd == null)
            return;

        _interstitialAd.Destroy();
        _interstitialAd = null;
        GameLogger.Log("[Interstitial] Gecis reklami yok edildi.");
    }

    private void Update()
    {
        if (!_isLoading || !AdRecoveryPolicy.HasTimedOut(_loadStartedAt, Time.realtimeSinceStartup, LoadTimeoutSeconds))
            return;

        DestroyInterstitialAd();
        OnInterstitialFailedToLoad?.Invoke("load_timeout");
        if (!_retryScheduled && !string.IsNullOrWhiteSpace(_cachedAdUnitId))
        {
            _retryAttempts++;
            _retryScheduled = true;
            Invoke(nameof(RetryLoadInterstitialAd), GetRetryDelaySeconds());
        }
        GameLogger.LogWarning("[Interstitial] Yukleme zaman asimina ugradi; kilit temizlendi.");
    }

    private void RetryLoadInterstitialAd()
    {
        _retryScheduled = false;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow)
            return;

        if (!string.IsNullOrWhiteSpace(_cachedAdUnitId))
            LoadInterstitialAd(_cachedAdUnitId);
    }

    private void HandleOnInterstitialAdLoaded(InterstitialAd ad, LoadAdError error)
    {
        _isLoading = false;

        if (error != null || ad == null)
        {
            string errorMessage = error?.GetMessage() ?? "unknown_load_error";
            GameLogger.LogWarning($"[Interstitial] Yuklenmesi basarisiz: {errorMessage}");
            OnInterstitialFailedToLoad?.Invoke(errorMessage);
            if (AdMobManager.ExistingInstance != null
                && AdMobManager.ExistingInstance.CanLoadAdsNow
                && !_retryScheduled)
            {
                _retryAttempts++;
                _retryScheduled = true;
                Invoke(nameof(RetryLoadInterstitialAd), GetRetryDelaySeconds());
            }
            return;
        }

        _retryAttempts = 0;
        _interstitialAd = ad;
        RegisterLifecycleCallbacks(ad);

        GameLogger.Log("[Interstitial] Gecis reklami yuklendi.");
        OnInterstitialLoaded?.Invoke();
    }

    private void RegisterLifecycleCallbacks(InterstitialAd ad)
    {
        ad.OnAdPaid += adValue => OnInterstitialPaid?.Invoke(adValue);
        ad.OnAdFullScreenContentOpened += HandleOnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentClosed += HandleOnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentFailed += HandleOnAdFullScreenContentFailed;
        ad.OnAdClicked += () => AdPolicyManager.RecordAdClick();
    }

    private void HandleOnAdFullScreenContentOpened()
    {
        GameLogger.Log("[Interstitial] Reklam acildi.");
        _timeScaleBeforeFullscreen = Time.timeScale;
        Time.timeScale = 0f;
        OnInterstitialShown?.Invoke();
    }

    private void HandleOnAdFullScreenContentClosed()
    {
        GameLogger.Log("[Interstitial] Reklam kapatildi.");
        RestoreTimeScale();
        DestroyInterstitialAd();
        OnInterstitialClosed?.Invoke();
    }

    private void HandleOnAdFullScreenContentFailed(AdError error)
    {
        GameLogger.LogWarning($"[Interstitial] Reklam acilamadi: {error.GetMessage()}");
        RestoreTimeScale();
        DestroyInterstitialAd();
        OnInterstitialClosed?.Invoke();
    }

    private float GetRetryDelaySeconds()
    {
        return AdRecoveryPolicy.GetRetryDelaySeconds(_retryAttempts, RetryBaseDelaySeconds, RetryMaxDelaySeconds);
    }

    private void RestoreTimeScale()
    {
        if (!_timeScaleBeforeFullscreen.HasValue)
            return;

        Time.timeScale = _timeScaleBeforeFullscreen.Value;
        _timeScaleBeforeFullscreen = null;
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
        DestroyInterstitialAd();
    }
}
