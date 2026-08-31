using UnityEngine;
using BlockPuzzle.Core.Common;
using Debug = BlockPuzzle.Core.Common.GameLogger;
using BlockPuzzle.Core.Monetization;

public static class AdPolicyManager
{
    public static void RecordAdClick()
    {
        // AdMob invalid-click protection is handled by Google. A local click
        // counter must not permanently disable ads for legitimate players.
        Debug.Log("[AdPolicyManager] Ad click recorded; no local ad ban applied.");
    }

    public static void OnMatchEnded()
    {
        // Kept as a no-op bridge for existing game-over event subscribers.
    }

    public static bool AreAutomaticAdsAllowed()
    {
        return AdEntitlementPolicy.AllowAutomaticAds(EntitlementManager.IsRemoveAdsActive);
    }

    // Rewarded ads are user-initiated and remain available after Remove Ads.
    public static bool AreRewardedAdsAllowed() => AdEntitlementPolicy.AllowRewardedAds(EntitlementManager.IsRemoveAdsActive);

    public static bool AreAdsAllowed() => AreAutomaticAdsAllowed();
}
