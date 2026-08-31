using GoogleMobileAds.Api;
using BlockPuzzle.Core.Common;
using UnityEngine;
using System;
using BlockPuzzle.Core.Monetization;

/// <summary>
/// Odullu reklam yukleme ve gosterim yasam dongusunu yonetir.
/// </summary>
public class RewardedAdManager : MonoBehaviour
{
    private const float RetryBaseDelaySeconds = 5f;
    private const float RetryMaxDelaySeconds = 60f;
    private const float LoadTimeoutSeconds = 20f;
    private RewardedAd _rewardedAd;
    private bool _isLoading;
    private string _cachedAdUnitId;
    private int _retryAttempts;
    private bool _retryScheduled;
    private float? _timeScaleBeforeFullscreen;
    private float _loadStartedAt;
    private int _loadGeneration;

    public bool IsLoading => _isLoading;

    public event Action OnRewardedAdLoaded;
    public event Action<string> OnRewardedAdFailedToLoad;
    public event Action<Reward> OnUserRewarded;
    public event Action OnRewardedAdShown;
    public event Action OnRewardedAdClosed;
    public event Action<AdValue> OnRewardedAdPaid;

    public void LoadRewardedAd(string adUnitId)
    {
        if (!AdPolicyManager.AreRewardedAdsAllowed()) return;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow) return;
        GameLogger.Log("[Rewarded] LoadRewardedAd called.");
        
        if (_isLoading)
        {
            GameLogger.Log("[Rewarded] Odullu reklam zaten yukleniyor.");
            return;
        }

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            GameLogger.Log("[Rewarded] Odullu reklam zaten hazir.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            GameLogger.LogError("[Rewarded] CRITICAL: Ad unit id bos! Yukleme atlandi.");
            return;
        }

        DestroyRewardedAd();

        _cachedAdUnitId = adUnitId;
        _isLoading = true;
        _loadStartedAt = Time.realtimeSinceStartup;
        _retryScheduled = false;

        int loadGeneration = ++_loadGeneration;
        RewardedAd.Load(adUnitId, AdRequestFactory.Create(), (ad, error) =>
        {
            if (loadGeneration != _loadGeneration)
            {
                ad?.Destroy();
                return;
            }

            HandleOnRewardedAdLoaded(ad, error);
        });
        GameLogger.Log("[Rewarded] Odullu reklam yukleniyor...");
    }

    public void ShowRewardedAd()
    {
        GameLogger.Log($"[Rewarded] ShowRewardedAd called - Ad ready: {(_rewardedAd != null && _rewardedAd.CanShowAd())}");
        
        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
        {
            GameLogger.LogError($"[Rewarded] HATA - Reklam gosterilemiyor! Ad null: {_rewardedAd == null}, CanShow: {(_rewardedAd != null && _rewardedAd.CanShowAd())}");
            return;
        }

        _rewardedAd.Show((Reward reward) =>
        {
            GameLogger.Log($"[Rewarded] Kullanici odul kazandi: {reward.Type} = {reward.Amount}");
            OnUserRewarded?.Invoke(reward);
        });

        GameLogger.Log("[Rewarded] Odullu reklam gosteriliyor.");
    }

    public bool IsReady()
    {
        return _rewardedAd != null && _rewardedAd.CanShowAd();
    }

    public void DestroyRewardedAd()
    {
        _loadGeneration++;
        _isLoading = false;
        if (_rewardedAd == null)
            return;

        _rewardedAd.Destroy();
        _rewardedAd = null;
        GameLogger.Log("[Rewarded] Odullu reklam yok edildi.");
    }

    private void Update()
    {
        if (!_isLoading || !AdRecoveryPolicy.HasTimedOut(_loadStartedAt, Time.realtimeSinceStartup, LoadTimeoutSeconds))
            return;

        DestroyRewardedAd();
        OnRewardedAdFailedToLoad?.Invoke("load_timeout");
        if (!_retryScheduled && !string.IsNullOrWhiteSpace(_cachedAdUnitId))
        {
            _retryAttempts++;
            _retryScheduled = true;
            Invoke(nameof(RetryLoadRewardedAd), GetRetryDelaySeconds());
        }
        GameLogger.LogWarning("[Rewarded] Yukleme zaman asimina ugradi; kilit temizlendi.");
    }

    private void RetryLoadRewardedAd()
    {
        _retryScheduled = false;
        if (AdMobManager.ExistingInstance != null && !AdMobManager.ExistingInstance.CanLoadAdsNow)
            return;

        if (!string.IsNullOrWhiteSpace(_cachedAdUnitId))
            LoadRewardedAd(_cachedAdUnitId);
    }

    private void HandleOnRewardedAdLoaded(RewardedAd ad, LoadAdError error)
    {
        _isLoading = false;
        GameLogger.Log($"[Rewarded] HandleOnRewardedAdLoaded called - Ad: {(ad != null ? "NOT NULL" : "NULL")}, Error: {(error != null ? error.GetMessage() : "NO ERROR")}");

        if (error != null || ad == null)
        {
            string errorMessage = error?.GetMessage() ?? "unknown_load_error";
            GameLogger.LogError($"[Rewarded] HATA - Yuklenmesi basarisiz: {errorMessage}");
            OnRewardedAdFailedToLoad?.Invoke(errorMessage);
            if (AdMobManager.ExistingInstance != null
                && AdMobManager.ExistingInstance.CanLoadAdsNow
                && !_retryScheduled)
            {
                _retryAttempts++;
                _retryScheduled = true;
                Invoke(nameof(RetryLoadRewardedAd), GetRetryDelaySeconds());
            }
            return;
        }

        _retryAttempts = 0;
        _rewardedAd = ad;
        RegisterLifecycleCallbacks(ad);

        GameLogger.Log("[Rewarded] Odullu reklam basariyla yuklendi!");
        OnRewardedAdLoaded?.Invoke();
    }

    private void RegisterLifecycleCallbacks(RewardedAd ad)
    {
        ad.OnAdPaid += adValue => OnRewardedAdPaid?.Invoke(adValue);
        ad.OnAdFullScreenContentOpened += HandleOnAdFullScreenContentOpened;
        ad.OnAdFullScreenContentClosed += HandleOnAdFullScreenContentClosed;
        ad.OnAdFullScreenContentFailed += HandleOnAdFullScreenContentFailed;
        ad.OnAdClicked += () => AdPolicyManager.RecordAdClick();
    }

    private void HandleOnAdFullScreenContentOpened()
    {
        GameLogger.Log("[Rewarded] Reklam acildi.");
        _timeScaleBeforeFullscreen = Time.timeScale;
        Time.timeScale = 0f;
        OnRewardedAdShown?.Invoke();
    }

    private void HandleOnAdFullScreenContentClosed()
    {
        GameLogger.Log("[Rewarded] Reklam kapatildi.");
        RestoreTimeScale();
        DestroyRewardedAd();
        OnRewardedAdClosed?.Invoke();
    }

    private void HandleOnAdFullScreenContentFailed(AdError error)
    {
        GameLogger.LogWarning($"[Rewarded] Reklam acilamadi: {error.GetMessage()}");
        RestoreTimeScale();
        DestroyRewardedAd();
        OnRewardedAdClosed?.Invoke();
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
        DestroyRewardedAd();
    }
}
