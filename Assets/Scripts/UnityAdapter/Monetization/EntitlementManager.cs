using System;
using UnityEngine;

namespace BlockPuzzle.Core.Monetization
{
    public static class AdEntitlementPolicy
    {
        public static bool AllowAutomaticAds(bool removeAdsActive) => !removeAdsActive;
        public static bool AllowRewardedAds(bool removeAdsActive) => true;
    }

    public static class AdRecoveryPolicy
    {
        public static bool HasTimedOut(float startedAt, float now, float timeoutSeconds)
        {
            return timeoutSeconds > 0f && now >= startedAt && now - startedAt >= timeoutSeconds;
        }

        public static float GetRetryDelaySeconds(int attempt, float baseDelaySeconds, float maxDelaySeconds)
        {
            int exponent = Math.Max(0, attempt - 1);
            double delay = Math.Max(0f, baseDelaySeconds) * Math.Pow(2d, exponent);
            return (float)Math.Min(Math.Max(0f, maxDelaySeconds), delay);
        }
    }

    public class EntitlementManager : MonoBehaviour
    {
        public static EntitlementManager Instance { get; private set; }

        private const string REMOVE_ADS_KEY = "Entitlement_RemoveAds";

        public static bool IsRemoveAdsActive => PlayerPrefs.GetInt(REMOVE_ADS_KEY, 0) == 1;
        public static event Action RemoveAdsGranted;

        public event Action OnAdsRemoved;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool HasRemovedAds()
        {
            return IsRemoveAdsActive;
        }

        public void GrantRemoveAds()
        {
            if (!HasRemovedAds())
            {
                PlayerPrefs.SetInt(REMOVE_ADS_KEY, 1);
                PlayerPrefs.Save();
                OnAdsRemoved?.Invoke();
                RemoveAdsGranted?.Invoke();
                Debug.Log("Ads removed entitlement granted.");
            }
        }
    }
}
