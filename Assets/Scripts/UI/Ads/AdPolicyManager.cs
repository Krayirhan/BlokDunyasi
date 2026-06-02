using UnityEngine;
using BlockPuzzle.Core.Common;
using Debug = BlockPuzzle.Core.Common.GameLogger;

public static class AdPolicyManager
{
    private const string ClickCountKey = SettingsKeys.AdPolicyClickCount;
    private const string BanMatchesLeftKey = SettingsKeys.AdPolicyBanMatchesLeft;
    
    private const int MaxClicksAllowed = 3;
    private const int BanDurationMatches = 2; // "2 oyun boyunca"

    public static void RecordAdClick()
    {
        var config = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        if (config != null && config.ShouldUseGoogleTestAdUnits())
            return;

        int currentClicks = PlayerPrefs.GetInt(ClickCountKey, 0);
        currentClicks++;
        
        Debug.Log($"[AdPolicyManager] Ad clicked! Total clicks: {currentClicks}");
        
        if (currentClicks >= MaxClicksAllowed)
        {
            PlayerPrefs.SetInt(BanMatchesLeftKey, BanDurationMatches);
            PlayerPrefs.SetInt(ClickCountKey, 0);
            Debug.LogWarning($"[AdPolicyManager] Penalty applied! Ads banned for {BanDurationMatches} matches.");
        }
        else
        {
            PlayerPrefs.SetInt(ClickCountKey, currentClicks);
        }
        PlayerPrefs.Save();
    }

    public static void OnMatchEnded()
    {
        int banLeft = PlayerPrefs.GetInt(BanMatchesLeftKey, 0);
        if (banLeft > 0)
        {
            banLeft--;
            PlayerPrefs.SetInt(BanMatchesLeftKey, banLeft);
            PlayerPrefs.Save();
            Debug.Log($"[AdPolicyManager] Match ended. Ban matches left: {banLeft}");
        }
    }

    public static bool AreAdsAllowed()
    {
        return PlayerPrefs.GetInt(BanMatchesLeftKey, 0) <= 0;
    }
}
