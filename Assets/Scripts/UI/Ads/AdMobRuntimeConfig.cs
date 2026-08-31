using UnityEngine;
using System;
using BlockPuzzle.Core.Common;

/// <summary>
/// AdMob reklam kimliklerini Inspector'dan yonetmek icin kullanilan runtime config asset'i.
/// </summary>
[CreateAssetMenu(fileName = "AdMobRuntimeConfig", menuName = "Blok Dunyasi/Ads/AdMob Runtime Config")]
public class AdMobRuntimeConfig : ScriptableObject
{
    public enum AdUnitMode
    {
        Production = 0,
        GoogleTestManualOverride = 1,
        GoogleTestDebugBuild = 2,
        /// <summary>
        /// Unity Editor: asla gercek yayıncı birimleri yuklenmez (ozel-dokunma ihlali riski).
        /// </summary>
        GoogleTestEditorSafety = 3
    }

    public const string ResourcesPath = "AdMobRuntimeConfig";

    private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    private const string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    private const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    [Header("General")]
    [SerializeField] private bool loadAdsOnStartup = true;
    [Tooltip("Aciksa her zaman Google demo birimleri. Release disinda genelde kapali tutun; Editor/Debug zaten otomatik teste zorlanir.")]
    [SerializeField] private bool useGoogleTestAdUnits = false;
    [Tooltip("Development Build'te gercek birimleri yukleme (kendi reklamina tiklama riskine karsi).")]
    [SerializeField] private bool forceGoogleTestAdsInDebugBuilds = true;
    [SerializeField] private string[] testDeviceHashedIds = Array.Empty<string>();
    [SerializeField] private bool tagForUnderAgeOfConsent = false;

    [Header("Banner Strategy")]
    [SerializeField] private bool showBannerOnAllScenes = true;
    [SerializeField] private bool showBannerOnNonGameplayScenes = true;
    [SerializeField] private string[] bannerSceneNames = new[] { SceneCatalog.Scores, SceneCatalog.Settings };

    [Header("Interstitial Strategy")]
    [SerializeField] private bool enableInterstitialOnGameOverScene = true;
    [SerializeField] [Min(0)] private int minimumCompletedSessionsBeforeInterstitial = 2;
    [SerializeField] [Min(0f)] private float minimumSecondsBetweenInterstitials = 90f;
    [SerializeField] [Min(0)] private int maxInterstitialsPerSession = 3;
    [SerializeField] [Min(0f)] private float cooldownAfterRewardedSeconds = 90f;

    [Header("Analytics")]
    [SerializeField] private bool enableAnalyticsFileLogging = true;

    [Header("Android Ad Unit IDs")]
    [SerializeField] private string androidBannerAdUnitId = "";
    [SerializeField] private string androidInterstitialAdUnitId = "";
    [SerializeField] private string androidRewardedAdUnitId = "";

    [Header("iOS Ad Unit IDs")]
    [SerializeField] private string iosBannerAdUnitId = "";
    [SerializeField] private string iosInterstitialAdUnitId = "";
    [SerializeField] private string iosRewardedAdUnitId = "";

    public bool LoadAdsOnStartup => loadAdsOnStartup;
    public bool UseGoogleTestAdUnits => useGoogleTestAdUnits;
    public bool ForceGoogleTestAdsInDebugBuilds => forceGoogleTestAdsInDebugBuilds;
    public bool TagForUnderAgeOfConsent => tagForUnderAgeOfConsent;
    public bool ShowBannerOnNonGameplayScenes => showBannerOnNonGameplayScenes;
    public bool EnableInterstitialOnGameOverScene => enableInterstitialOnGameOverScene;
    public int MinimumCompletedSessionsBeforeInterstitial => Mathf.Max(0, minimumCompletedSessionsBeforeInterstitial);
    public float MinimumSecondsBetweenInterstitials => Mathf.Max(0f, minimumSecondsBetweenInterstitials);
    public int MaxInterstitialsPerSession => Mathf.Max(0, maxInterstitialsPerSession);
    public float CooldownAfterRewardedSeconds => Mathf.Max(0f, cooldownAfterRewardedSeconds);
    public bool EnableAnalyticsFileLogging => enableAnalyticsFileLogging;
    public bool HasProductionAndroidAdUnitIds => !string.IsNullOrWhiteSpace(androidBannerAdUnitId)
        && !string.IsNullOrWhiteSpace(androidInterstitialAdUnitId)
        && !string.IsNullOrWhiteSpace(androidRewardedAdUnitId);

    public bool HasProductionIosAdUnitIds => !string.IsNullOrWhiteSpace(iosBannerAdUnitId)
        && !string.IsNullOrWhiteSpace(iosInterstitialAdUnitId)
        && !string.IsNullOrWhiteSpace(iosRewardedAdUnitId);

    public AdUnitMode ResolveAdUnitMode()
    {
#if ADMOB_FORCE_TEST_ADS
        return AdUnitMode.GoogleTestManualOverride;
#elif UNITY_EDITOR
        return AdUnitMode.GoogleTestEditorSafety;
#else
        if (useGoogleTestAdUnits)
            return AdUnitMode.GoogleTestManualOverride;

        if (forceGoogleTestAdsInDebugBuilds && Debug.isDebugBuild)
            return AdUnitMode.GoogleTestDebugBuild;

        return AdUnitMode.Production;
#endif
    }

    public bool ShouldUseGoogleTestAdUnits()
    {
        return ResolveAdUnitMode() != AdUnitMode.Production;
    }

    public string DescribeResolvedAdUnitMode()
    {
        switch (ResolveAdUnitMode())
        {
            case AdUnitMode.GoogleTestManualOverride:
                return "Google Test IDs (manual override)";
            case AdUnitMode.GoogleTestDebugBuild:
                return "Google Test IDs (debug/development build)";
            case AdUnitMode.GoogleTestEditorSafety:
                return "Google Test IDs (Unity Editor — publisher safety)";
            default:
                return "Production IDs (release build)";
        }
    }

    public string[] GetTestDeviceHashedIds()
    {
        return testDeviceHashedIds != null ? (string[])testDeviceHashedIds.Clone() : Array.Empty<string>();
    }

    public bool ShouldShowBannerForScene(string sceneName)
    {
        if (showBannerOnAllScenes)
            return true;

        if (!showBannerOnNonGameplayScenes || string.IsNullOrWhiteSpace(sceneName) || bannerSceneNames == null)
            return false;

        for (int i = 0; i < bannerSceneNames.Length; i++)
        {
            string configuredScene = bannerSceneNames[i];
            if (!string.IsNullOrWhiteSpace(configuredScene) &&
                sceneName.Equals(configuredScene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryResolveAdUnitIds(out string bannerAdUnitId, out string interstitialAdUnitId, out string rewardedAdUnitId)
    {
        if (ShouldUseGoogleTestAdUnits())
        {
            bannerAdUnitId = TestBannerAdUnitId;
            interstitialAdUnitId = TestInterstitialAdUnitId;
            rewardedAdUnitId = TestRewardedAdUnitId;
            return true;
        }

        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                bannerAdUnitId = iosBannerAdUnitId;
                interstitialAdUnitId = iosInterstitialAdUnitId;
                rewardedAdUnitId = iosRewardedAdUnitId;
                break;

            case RuntimePlatform.Android:
            default:
                bannerAdUnitId = androidBannerAdUnitId;
                interstitialAdUnitId = androidInterstitialAdUnitId;
                rewardedAdUnitId = androidRewardedAdUnitId;
                break;
        }

        return !string.IsNullOrWhiteSpace(bannerAdUnitId)
            || !string.IsNullOrWhiteSpace(interstitialAdUnitId)
            || !string.IsNullOrWhiteSpace(rewardedAdUnitId);
    }

    private void OnValidate()
    {
        if (!HasProductionAndroidAdUnitIds)
            Debug.LogWarning("[AdMobRuntimeConfig] Android production ad unit configuration is incomplete.", this);

        if (Application.platform == RuntimePlatform.IPhonePlayer && !HasProductionIosAdUnitIds)
            Debug.LogWarning("[AdMobRuntimeConfig] iOS production ad unit configuration is incomplete.", this);

        if (minimumCompletedSessionsBeforeInterstitial < 0 || maxInterstitialsPerSession < 0)
            Debug.LogError("[AdMobRuntimeConfig] Interstitial limits cannot be negative.", this);
    }
}
