using GoogleMobileAds.Api;
using BlockPuzzle.Core.Common;
using UnityEngine;
using System;

/// <summary>
/// Odullu reklam yukleme ve gosterim yasam dongusunu yonetir.
/// </summary>
public class RewardedAdManager : MonoBehaviour
{
    private const int MaxRetryAttempts = 3;
    private RewardedAd _rewardedAd;
    private bool _isLoading;
    private string _cachedAdUnitId;
    private int _retryAttempts;
    private bool _retryScheduled;

    public event Action OnRewardedAdLoaded;
    public event Action<string> OnRewardedAdFailedToLoad;
    public event Action<Reward> OnUserRewarded;
    public event Action OnRewardedAdShown;
    public event Action OnRewardedAdClosed;
    public event Action<AdValue> OnRewardedAdPaid;

    public void LoadRewardedAd(string adUnitId)
    {
        if (!AdPolicyManager.AreAdsAllowed()) return;
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
        _retryScheduled = false;

        RewardedAd.Load(adUnitId, new AdRequest(), HandleOnRewardedAdLoaded);
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
        if (_rewardedAd == null)
            return;

        _rewardedAd.Destroy();
        _rewardedAd = null;
        GameLogger.Log("[Rewarded] Odullu reklam yok edildi.");
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
                && !_retryScheduled
                && _retryAttempts < MaxRetryAttempts)
            {
                _retryAttempts++;
                _retryScheduled = true;
                Invoke(nameof(RetryLoadRewardedAd), 5f * _retryAttempts);
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
        Time.timeScale = 0f;
        OnRewardedAdShown?.Invoke();
    }

    private void HandleOnAdFullScreenContentClosed()
    {
        GameLogger.Log("[Rewarded] Reklam kapatildi.");
        Time.timeScale = 1f;
        DestroyRewardedAd();
        OnRewardedAdClosed?.Invoke();
    }

    private void HandleOnAdFullScreenContentFailed(AdError error)
    {
        GameLogger.LogWarning($"[Rewarded] Reklam acilamadi: {error.GetMessage()}");
        Time.timeScale = 1f;
        DestroyRewardedAd();
        OnRewardedAdClosed?.Invoke();
    }

    private void OnDestroy()
    {
        DestroyRewardedAd();
    }
}
