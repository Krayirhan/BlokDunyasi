using GoogleMobileAds.Api;
using BlockPuzzle.Core.Common;
using UnityEngine;
using System;

/// <summary>
/// Gecis reklami yasam dongusunu yonetir.
/// </summary>
public class InterstitialAdManager : MonoBehaviour
{
    private const int MaxRetryAttempts = 3;
    private InterstitialAd _interstitialAd;
    private bool _isLoading;
    private string _cachedAdUnitId;
    private int _retryAttempts;
    private bool _retryScheduled;

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
        _retryScheduled = false;

        InterstitialAd.Load(adUnitId, new AdRequest(), HandleOnInterstitialAdLoaded);
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
        if (_interstitialAd == null)
            return;

        _interstitialAd.Destroy();
        _interstitialAd = null;
        GameLogger.Log("[Interstitial] Gecis reklami yok edildi.");
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
                && !_retryScheduled
                && _retryAttempts < MaxRetryAttempts)
            {
                _retryAttempts++;
                _retryScheduled = true;
                Invoke(nameof(RetryLoadInterstitialAd), 5f * _retryAttempts);
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
        Time.timeScale = 0f;
        OnInterstitialShown?.Invoke();
    }

    private void HandleOnAdFullScreenContentClosed()
    {
        GameLogger.Log("[Interstitial] Reklam kapatildi.");
        Time.timeScale = 1f;
        DestroyInterstitialAd();
        OnInterstitialClosed?.Invoke();
    }

    private void HandleOnAdFullScreenContentFailed(AdError error)
    {
        GameLogger.LogWarning($"[Interstitial] Reklam acilamadi: {error.GetMessage()}");
        Time.timeScale = 1f;
        DestroyInterstitialAd();
        OnInterstitialClosed?.Invoke();
    }

    private void OnDestroy()
    {
        DestroyInterstitialAd();
    }
}
