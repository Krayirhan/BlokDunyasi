using System;
using UnityEngine;

/// <summary>
/// Game-over interstitial reklamlarinin frekansini ve oturum kurallarini uygular.
/// </summary>
public static class InterstitialAdPolicy
{
    private const string CompletedSessionsKey = "BlokDunyasi_InterstitialCompletedSessions";
    private const string LastInterstitialUnixSecondsKey = "BlokDunyasi_LastInterstitialUnixSeconds";
    private const string LastRewardedUnixSecondsKey = "BlokDunyasi_LastRewardedUnixSeconds";

    private static int _interstitialsShownThisAppSession;

    public static bool TryRegisterCompletedSessionAndCanShow(AdMobRuntimeConfig config)
    {
        if (config == null || !config.EnableInterstitialOnGameOverScene)
            return false;

        int completedSessions = PlayerPrefs.GetInt(CompletedSessionsKey, 0) + 1;
        PlayerPrefs.SetInt(CompletedSessionsKey, completedSessions);
        PlayerPrefs.Save();

        if (completedSessions < config.MinimumCompletedSessionsBeforeInterstitial)
            return false;

        if (_interstitialsShownThisAppSession >= config.MaxInterstitialsPerSession)
            return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!HasElapsed(now, LastInterstitialUnixSecondsKey, config.MinimumSecondsBetweenInterstitials))
            return false;

        return HasElapsed(now, LastRewardedUnixSecondsKey, config.CooldownAfterRewardedSeconds);
    }

    public static void ResetForTesting()
    {
        PlayerPrefs.DeleteKey(CompletedSessionsKey);
        PlayerPrefs.DeleteKey(LastInterstitialUnixSecondsKey);
        PlayerPrefs.DeleteKey(LastRewardedUnixSecondsKey);
        _interstitialsShownThisAppSession = 0;
        PlayerPrefs.Save();
    }

    public static void RecordInterstitialShown()
    {
        _interstitialsShownThisAppSession++;
        PlayerPrefs.SetString(LastInterstitialUnixSecondsKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    public static void RecordRewardedShown()
    {
        PlayerPrefs.SetString(LastRewardedUnixSecondsKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    private static bool HasElapsed(long nowUnixSeconds, string key, float requiredSeconds)
    {
        if (requiredSeconds <= 0f)
            return true;

        string rawTimestamp = PlayerPrefs.GetString(key, string.Empty);
        if (!long.TryParse(rawTimestamp, out long previousUnixSeconds) || previousUnixSeconds <= 0)
            return true;

        // Device clock rollback must not suppress ads indefinitely.
        if (nowUnixSeconds < previousUnixSeconds)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return true;
        }

        return nowUnixSeconds - previousUnixSeconds >= Mathf.CeilToInt(requiredSeconds);
    }
}
