using BlockPuzzle.UnityAdapter.UI;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Privacy;
using GoogleMobileAds.Api;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using BlockPuzzle.Core.Monetization;
using Firebase.Crashlytics;

/// <summary>
/// Tum reklam formatlarini tek noktadan yonetir.
/// </summary>
public class AdMobManager : MonoBehaviour
{
    public enum AdsRuntimeState
    {
        NotReady = 0,
        WaitingForConsent = 1,
        ConsentDeclined = 2,
        Initializing = 3,
        Ready = 4,
        Failed = 5,
        Disabled = 6
    }

    private const string BannerPlacementName = "menu_banner";
    private const string InterstitialPlacementName = "gameover_interstitial";
    private const string RewardedPlacementName = "continue_rewarded";
    // Observed behavior: the native UMP callback either fires within ~1-2s or
    // never fires at all (binary, not gradual) -- there is no benefit to
    // waiting longer or retrying before falling back.
    private const float ConsentTimeoutSeconds = 3f;

    private static AdMobManager _instance;

    [SerializeField] private string _bannerAdUnitId = "";
    [SerializeField] private string _interstitialAdUnitId = "";
    [SerializeField] private string _rewardedAdUnitId = "";

    private BannerAdManager _bannerAdManager;
    private InterstitialAdManager _interstitialAdManager;
    private RewardedAdManager _rewardedAdManager;
    private bool _adsConfigured;
    private bool _bannerLoadRequested;
    private bool _interstitialLoadRequested;
    private bool _rewardedLoadRequested;
    private bool _consentRetryScheduled;
    private Coroutine _consentWatchdogRoutine;
    private int _consentRequestGeneration;
    private int _consentWatchdogFailureCount;
    private int _lastForegroundRecoveryFrame = -1;
    private Coroutine _staggeredLoadRoutine;
    private bool _sdkInitializationRequested;
    private AdMobRuntimeConfig _runtimeConfig;
    private AdsRuntimeState _runtimeState = AdsRuntimeState.NotReady;
    [SerializeField] [Min(0f)] private float bannerLoadDelaySeconds = 0.35f;
    [SerializeField] [Min(0f)] private float rewardedLoadDelaySeconds = 1.5f;
    [SerializeField] [Min(0f)] private float interstitialLoadDelaySeconds = 2.25f;

    public event Action OnBannerLoaded;
    public event Action<string> OnBannerFailedToLoad;
    public event Action OnBannerShown;
    public event Action OnBannerHidden;
    public event Action<AdValue> OnBannerPaid;

    public event Action OnInterstitialLoaded;
    public event Action<string> OnInterstitialFailedToLoad;
    public event Action OnInterstitialShown;
    public event Action OnInterstitialClosed;
    public event Action<AdValue> OnInterstitialPaid;

    public event Action OnRewardedAdLoaded;
    public event Action<string> OnRewardedAdFailedToLoad;
    public event Action OnRewardedAdShown;
    public event Action OnRewardedUserEarned;
    public event Action OnRewardedAdClosed;
    public event Action<AdValue> OnRewardedAdPaid;

    public static AdMobManager ExistingInstance => _instance;
    public bool IsBannerVisible => _bannerAdManager != null && _bannerAdManager.IsVisible;
    public int CurrentBannerOccupiedHeightInPixels => _bannerAdManager != null ? _bannerAdManager.CurrentOccupiedHeightInPixels : 0;
    public AdsRuntimeState RuntimeState => _runtimeState;
    public bool CanLoadAdsNow => _adsConfigured
        && _runtimeState == AdsRuntimeState.Ready
        && ConsentGate.CanInitializeAds
        && AdMobBootstrap.Instance.IsInitialized;
    public bool CanLoadAutomaticAdsNow => CanLoadAdsNow && AdPolicyManager.AreAutomaticAdsAllowed();

    public static AdMobManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AdMobManager");
                _instance = go.AddComponent<AdMobManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAdManagers();
        ConsentGate.ConsentStateChanged += HandleConsentStateChanged;
        RewardedAdBridge.RegisterProvider(IsRewardedAdReady, ShowRewardedAd);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // Korumayı devreye sokmak için oyun bittiğinde sayaç düşürülmeli
        BlockPuzzle.UnityAdapter.Boot.GameBootstrap.OnGameOver += HandleGameOverForPolicy;
        EntitlementManager.RemoveAdsGranted += HandleRemoveAdsGranted;
    }

    private void HandleGameOverForPolicy(int score)
    {
        AdPolicyManager.OnMatchEnded();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_runtimeConfig == null)
            _runtimeConfig = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);

        bool wantBanner = _runtimeConfig == null || _runtimeConfig.ShouldShowBannerForScene(scene.name);
        SetBannerWanted(wantBanner);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        RecoverAdsAfterForeground();
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused)
            RecoverAdsAfterForeground();
    }

    private void RecoverAdsAfterForeground()
    {
        if (!_adsConfigured || _lastForegroundRecoveryFrame == Time.frameCount)
            return;

        _lastForegroundRecoveryFrame = Time.frameCount;

        if (_runtimeState == AdsRuntimeState.Ready && CanLoadAdsNow)
        {
            ReconcileLoadRequests();
            EnsureBannerAdLoaded();
            EnsureInterstitialAdLoaded();
            EnsureRewardedAdLoaded();

            var config = _runtimeConfig ?? Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
            bool wantBanner = config == null || config.ShouldShowBannerForScene(SceneManager.GetActiveScene().name);
            SetBannerWanted(wantBanner);
            GameLogger.Log($"[AdMobManager] App foreground oldu; {GetDiagnosticSummary()}");
        }
        else if (_runtimeState == AdsRuntimeState.Failed && !_consentRetryScheduled)
        {
            StartConsentGatedAdsFlow();
        }
    }

    public void ConfigureAndLoadAds(string bannerAdUnitId, string interstitialAdUnitId, string rewardedAdUnitId)
    {
        _runtimeState = AdsRuntimeState.NotReady;
        var config = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        _runtimeConfig = config;
        if (config != null && config.TryResolveAdUnitIds(out var resolvedBanner, out var resolvedInterstitial, out var resolvedRewarded))
        {
            _bannerAdUnitId = resolvedBanner ?? string.Empty;
            _interstitialAdUnitId = resolvedInterstitial ?? string.Empty;
            _rewardedAdUnitId = resolvedRewarded ?? string.Empty;

            if (config.ShouldUseGoogleTestAdUnits())
            {
                GameLogger.Log(
                    $"[AdMobManager] Yayinci guvenligi: {config.DescribeResolvedAdUnitMode()} — gercek ad unit ID'leri bu ortamda kullanilmiyor.");
            }
        }
        else
        {
            _bannerAdUnitId = bannerAdUnitId ?? string.Empty;
            _interstitialAdUnitId = interstitialAdUnitId ?? string.Empty;
            _rewardedAdUnitId = rewardedAdUnitId ?? string.Empty;
            GameLogger.LogWarning("[AdMobManager] AdMobRuntimeConfig cozumlenemedi; disaridan gelen ad unit ID'leri kullaniliyor.");
        }

        _adsConfigured = true;
        if (!ValidateResolvedAdUnitConfiguration(config))
            return;

        StartConsentGatedAdsFlow();
    }

    public bool IsConfigured => _adsConfigured;

    public void ResetAdTestingState()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ConsentGate.ResetForTesting();
        InterstitialAdPolicy.ResetForTesting();
        _sdkInitializationRequested = false;
        _consentRetryScheduled = false;
        _runtimeState = AdsRuntimeState.NotReady;
        GameLogger.Log("[AdMobManager] Development test state reset. Restarting consent flow.");
        if (_adsConfigured)
            StartConsentGatedAdsFlow();
#else
        GameLogger.LogWarning("[AdMobManager] ResetAdTestingState is disabled in production builds.");
#endif
    }

    public string GetDiagnosticSummary()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool bannerWanted = _runtimeConfig == null || _runtimeConfig.ShouldShowBannerForScene(sceneName);
        return $"configured={_adsConfigured}; runtime={_runtimeState}; consent={ConsentGate.CurrentState}; " +
               $"canRequest={ConsentGate.CanInitializeAds}; sdk={AdMobBootstrap.Instance.IsInitialized}; " +
               $"scene={sceneName}; bannerWanted={bannerWanted}; removeAds={EntitlementManager.IsRemoveAdsActive}; " +
               $"bannerLoading={_bannerAdManager?.IsLoading ?? false}; bannerReady={_bannerAdManager?.HasAd ?? false}; " +
               $"interstitialLoading={_interstitialAdManager?.IsLoading ?? false}; interstitialReady={_interstitialAdManager?.IsReady() ?? false}; " +
               $"rewardedLoading={_rewardedAdManager?.IsLoading ?? false}; rewardedReady={_rewardedAdManager?.IsReady() ?? false}";
    }

    public void ShowBannerAd()
    {
        if (!CanLoadAutomaticAdsNow) return;
        EnsureBannerAdLoaded();
        _bannerAdManager?.ShowBannerAd();
    }

    public void HideBannerAd()
    {
        _bannerAdManager?.HideBannerAd();
    }

    public void SetBannerWanted(bool wanted)
    {
        if (wanted && !AdPolicyManager.AreAutomaticAdsAllowed())
        {
            _bannerAdManager?.HideBannerAd();
            return;
        }

        if (wanted)
        {
            if (CanLoadAutomaticAdsNow)
            {
                EnsureBannerAdLoaded();
                _bannerAdManager?.ShowBannerAd();
            }
            else
                _bannerAdManager?.HideBannerAd();
        }
        else
            _bannerAdManager?.HideBannerAd();
    }

    public void RefreshBannerLayout()
    {
        if (!CanLoadAutomaticAdsNow) return;
        EnsureBannerAdLoaded();
        _bannerAdManager?.RefreshBannerLayout();
    }

    public bool IsInterstitialReady()
    {
        if (!CanLoadAutomaticAdsNow) return false;
        EnsureInterstitialAdLoaded();
        return _interstitialAdManager != null && _interstitialAdManager.IsReady();
    }

    public void ShowInterstitialAd()
    {
        if (!CanLoadAutomaticAdsNow) return;
        EnsureInterstitialAdLoaded();
        if (_interstitialAdManager != null && _interstitialAdManager.IsReady()) 
            _interstitialAdManager.ShowInterstitialAd();
    }

    /// <summary>
    /// Final game-over sonrasinda, konfigurasyon ve frekans kurallari uygunsa gecis reklami gosterir.
    /// Reklam hazir degilse oyun akisi bekletilmez.
    /// </summary>
    public bool TryShowInterstitialOnGameOver()
    {
        if (!CanLoadAdsNow || _runtimeConfig == null)
            return false;

        if (_interstitialAdManager == null || !_interstitialAdManager.IsReady())
        {
            EnsureInterstitialAdLoaded();
            return false;
        }

        // Do not consume a completed-session slot when no ad can actually be
        // shown. This keeps policy counters honest after a no-fill response.
        if (!InterstitialAdPolicy.TryRegisterCompletedSessionAndCanShow(_runtimeConfig))
            return false;

        _interstitialAdManager.ShowInterstitialAd();
        return true;
    }

    public bool IsRewardedAdReady()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return false;
        if (!AdPolicyManager.AreRewardedAdsAllowed()) return false;
        EnsureRewardedAdLoaded();
        return _rewardedAdManager != null && _rewardedAdManager.IsReady();      
    }

    public void ShowRewardedAd()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return;
        if (!AdPolicyManager.AreRewardedAdsAllowed()) return;
        EnsureRewardedAdLoaded();
        if (_rewardedAdManager != null && _rewardedAdManager.IsReady())
            _rewardedAdManager.ShowRewardedAd();
    }

    private void StartConsentGatedAdsFlow()
    {
        if (!_adsConfigured)
            return;

        int generation = ++_consentRequestGeneration;
        if (_consentWatchdogRoutine != null)
            StopCoroutine(_consentWatchdogRoutine);
        _consentWatchdogRoutine = StartCoroutine(ConsentWatchdogRoutine(generation));
        _runtimeState = AdsRuntimeState.WaitingForConsent;
        AdMobConsentManager.Instance.EnsureConsentResolved(_runtimeConfig, (state, errorMessage) =>
        {
            if (generation != _consentRequestGeneration)
                return;

            if (_consentWatchdogRoutine != null)
            {
                StopCoroutine(_consentWatchdogRoutine);
                _consentWatchdogRoutine = null;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                _runtimeState = AdsRuntimeState.Failed;
                GameLogger.LogWarning($"[AdMobManager] Consent resolution failed. Ads startup is deferred. reason={errorMessage}");
                ScheduleConsentRetry();
                return;
            }

            if (state == ConsentState.Accepted)
            {
                _consentWatchdogFailureCount = 0;
                ConsentGate.RequireNonPersonalizedAds = false;
                InitializeSdkAndStartLoading();
            }
            else
            {
                _runtimeState = AdsRuntimeState.ConsentDeclined;
                GameLogger.LogWarning("[AdMobManager] Consent was not granted. Ads will remain disabled.");
            }
        });
    }

    private IEnumerator ConsentWatchdogRoutine(int generation)
    {
        yield return new WaitForSecondsRealtime(ConsentTimeoutSeconds);
        if (generation != _consentRequestGeneration || _runtimeState != AdsRuntimeState.WaitingForConsent)
            yield break;

        AdMobConsentManager.Instance.InvalidatePendingRequest();
        _consentWatchdogRoutine = null;
        GameLogger.LogWarning("[AdMobManager] Consent callback zaman asimi.");
        ReportConsentTimeoutToCrashlytics(_consentWatchdogFailureCount);
        _consentWatchdogFailureCount++;

        // The native ConsentInformation.Update() callback has been observed to
        // never fire at all on repeat app launches (confirmed: gdprApplies is
        // resolved to non-EEA locally, only the SDK's own completion signal is
        // stuck), and retrying does not help -- confirmed across 3+ relaunches.
        // Waiting/retrying forever means ads NEVER load after the very first
        // install run, so proceed immediately without personalized-consent
        // gating rather than wasting a retry cycle.
        GameLogger.LogWarning("[AdMobManager] Consent hala cozulemedi; SDK dogrudan baslatiliyor (GDPR uygulanmiyor varsayimi).");
        UnityEngine.Debug.Log("[AdMobManager] Consent watchdog exhausted; initializing ads without waiting for UMP callback.");

        // We never got a confirmed consent decision, so we cannot prove the
        // user actually agreed to personalization. Request non-personalized
        // ads only until a real decision resolves (e.g. from foreground
        // recovery or a later successful Update() call).
        ConsentGate.RequireNonPersonalizedAds = true;
        InitializeSdkAndStartLoading();
    }

    // Makes repeated UMP consent-callback hangs visible in the Crashlytics
    // console as a non-fatal event instead of silently vanishing behind
    // GameLogger, which is a no-op in release builds. This is a monetization
    // -critical failure path and must stay observable in production.
    private static void ReportConsentTimeoutToCrashlytics(int priorFailureCount)
    {
        try
        {
            Crashlytics.Log($"[AdMobManager] UMP ConsentInformation.Update() callback timeout (attempt {priorFailureCount + 1}).");
            Crashlytics.LogException(new TimeoutException(
                $"AdMob UMP consent callback never fired within {ConsentTimeoutSeconds}s (attempt {priorFailureCount + 1})."));
        }
        catch (Exception)
        {
            // Firebase may not be initialized yet this early in the boot
            // sequence; never let telemetry reporting break ad startup.
        }
    }

    private void ScheduleConsentRetry()
    {
        if (_consentRetryScheduled)
            return;

        _consentRetryScheduled = true;
        Invoke(nameof(RetryConsentGatedAdsFlow), 5f);
    }

    private void RetryConsentGatedAdsFlow()
    {
        _consentRetryScheduled = false;
        if (_adsConfigured && _runtimeState != AdsRuntimeState.Ready)
            StartConsentGatedAdsFlow();
    }

    private void InitializeSdkAndStartLoading()
    {
        if (_sdkInitializationRequested)
            return;

        _sdkInitializationRequested = true;
        _runtimeState = AdsRuntimeState.Initializing;
        UnityEngine.Debug.Log("[AdMobManager] Consent accepted; starting SDK initialization.");
        AdMobBootstrap.Instance.InitializeGoogleMobileAds(_runtimeConfig, success =>
        {
            UnityEngine.Debug.Log($"[AdMobManager] SDK initialization completion received. success={success}");
            if (!success)
            {
                _sdkInitializationRequested = false;
                _runtimeState = AdsRuntimeState.Failed;
                GameLogger.LogWarning("[AdMobManager] AdMob SDK init failed. Ad loading stays disabled.");
                ScheduleConsentRetry();
                return;
            }

            _runtimeState = AdsRuntimeState.Ready;
            StartStaggeredAdLoading();
            GameLogger.Log($"[AdMobManager] Reklam sistemi hazir; {GetDiagnosticSummary()}");
        });
    }

    private void HandleConsentStateChanged(ConsentState state)
    {
        if (_consentWatchdogRoutine != null)
        {
            StopCoroutine(_consentWatchdogRoutine);
            _consentWatchdogRoutine = null;
        }

        if (state == ConsentState.Accepted)
        {
            // Consent can change from declined back to accepted from the
            // privacy-options form. A previous failed/invalidated init must
            // not permanently block the new startup attempt.
            if (_runtimeState != AdsRuntimeState.Ready)
                _sdkInitializationRequested = false;

            // A real, confirmed consent decision just arrived -- no longer
            // need to restrict to non-personalized ads.
            ConsentGate.RequireNonPersonalizedAds = false;

            if (_adsConfigured)
                InitializeSdkAndStartLoading();
            return;
        }

        _runtimeState = state == ConsentState.Declined
            ? AdsRuntimeState.ConsentDeclined
            : AdsRuntimeState.WaitingForConsent;

        _sdkInitializationRequested = false;
        _bannerLoadRequested = false;
        _interstitialLoadRequested = false;
        _rewardedLoadRequested = false;
        _bannerAdManager?.DestroyBannerAd();
        _interstitialAdManager?.DestroyInterstitialAd();
        _rewardedAdManager?.DestroyRewardedAd();

        if (_staggeredLoadRoutine != null)
        {
            StopCoroutine(_staggeredLoadRoutine);
            _staggeredLoadRoutine = null;
        }

        if (_consentRetryScheduled)
        {
            CancelInvoke(nameof(RetryConsentGatedAdsFlow));
            _consentRetryScheduled = false;
        }

        HideBannerAd();
    }

    private bool ValidateResolvedAdUnitConfiguration(AdMobRuntimeConfig config)
    {
        bool usingTestIds = config != null && config.ShouldUseGoogleTestAdUnits();
        bool allIdsMissing = string.IsNullOrWhiteSpace(_bannerAdUnitId)
            && string.IsNullOrWhiteSpace(_interstitialAdUnitId)
            && string.IsNullOrWhiteSpace(_rewardedAdUnitId);

        if (allIdsMissing)
        {
            _runtimeState = AdsRuntimeState.Disabled;
            GameLogger.LogError("[AdMobManager] Ads disabled because no resolved ad unit IDs are available.");
            return false;
        }

        if (!usingTestIds && (string.IsNullOrWhiteSpace(_bannerAdUnitId)
            || string.IsNullOrWhiteSpace(_interstitialAdUnitId)
            || string.IsNullOrWhiteSpace(_rewardedAdUnitId)))
        {
            _runtimeState = AdsRuntimeState.Failed;
            GameLogger.LogError("[AdMobManager] Production ad unit configuration is incomplete. Release blocker remains open.");
            return false;
        }

        return true;
    }

    private void InitializeAdManagers()
    {
        GameObject bannerGO = new GameObject("BannerAdManager");
        bannerGO.transform.SetParent(transform);
        _bannerAdManager = bannerGO.AddComponent<BannerAdManager>();
        _bannerAdManager.OnBannerLoaded += () => OnBannerLoaded?.Invoke();
        _bannerAdManager.OnBannerFailedToLoad += error =>
        {
            _bannerLoadRequested = false;
            AdTelemetry.DispatchLoadFailed("banner", BannerPlacementName, error);
            OnBannerFailedToLoad?.Invoke(error);
        };
        _bannerAdManager.OnBannerShown += () => OnBannerShown?.Invoke();
        _bannerAdManager.OnBannerHidden += () => OnBannerHidden?.Invoke();
        _bannerAdManager.OnBannerPaid += adValue =>
        {
            AdTelemetry.DispatchPaidEvent("banner", BannerPlacementName, adValue, string.Empty);
            OnBannerPaid?.Invoke(adValue);
        };

        GameObject interstitialGO = new GameObject("InterstitialAdManager");
        interstitialGO.transform.SetParent(transform);
        _interstitialAdManager = interstitialGO.AddComponent<InterstitialAdManager>();
        _interstitialAdManager.OnInterstitialLoaded += () => OnInterstitialLoaded?.Invoke();
        _interstitialAdManager.OnInterstitialFailedToLoad += error =>
        {
            _interstitialLoadRequested = false;
            AdTelemetry.DispatchLoadFailed("interstitial", InterstitialPlacementName, error);
            OnInterstitialFailedToLoad?.Invoke(error);
        };
        _interstitialAdManager.OnInterstitialShown += () =>
        {
            InterstitialAdPolicy.RecordInterstitialShown();
            AdTelemetry.DispatchLifecycleEvent("interstitial", InterstitialPlacementName, "shown");
            OnInterstitialShown?.Invoke();
        };
        _interstitialAdManager.OnInterstitialClosed += () =>
        {
            AdTelemetry.DispatchLifecycleEvent("interstitial", InterstitialPlacementName, "closed");
            OnInterstitialClosed?.Invoke();
            if (!string.IsNullOrWhiteSpace(_interstitialAdUnitId) && AdPolicyManager.AreAdsAllowed())
            {
                _interstitialLoadRequested = false;
                EnsureInterstitialAdLoaded();
            }
        };
        _interstitialAdManager.OnInterstitialPaid += adValue =>
        {
            AdTelemetry.DispatchPaidEvent("interstitial", InterstitialPlacementName, adValue, string.Empty);
            OnInterstitialPaid?.Invoke(adValue);
        };

        GameObject rewardedGO = new GameObject("RewardedAdManager");
        rewardedGO.transform.SetParent(transform);
        _rewardedAdManager = rewardedGO.AddComponent<RewardedAdManager>();
        _rewardedAdManager.OnRewardedAdLoaded += () =>
        {
            RewardedAdBridge.NotifyLoaded();
            OnRewardedAdLoaded?.Invoke();
        };
        _rewardedAdManager.OnRewardedAdFailedToLoad += error =>
        {
            _rewardedLoadRequested = false;
            AdTelemetry.DispatchLoadFailed("rewarded", RewardedPlacementName, error);
            RewardedAdBridge.NotifyFailedToLoad(error);
            OnRewardedAdFailedToLoad?.Invoke(error);
        };
        _rewardedAdManager.OnRewardedAdShown += () =>
        {
            AdTelemetry.DispatchLifecycleEvent("rewarded", RewardedPlacementName, "shown");
            OnRewardedAdShown?.Invoke();
        };
        _rewardedAdManager.OnUserRewarded += reward =>
        {
            InterstitialAdPolicy.RecordRewardedShown();
            AdTelemetry.DispatchLifecycleEvent("rewarded", RewardedPlacementName, "reward_earned", reward.Type, reward.Amount);
            RewardedAdBridge.NotifyUserEarned();
            OnRewardedUserEarned?.Invoke();
        };
        _rewardedAdManager.OnRewardedAdClosed += () =>
        {
            AdTelemetry.DispatchLifecycleEvent("rewarded", RewardedPlacementName, "closed");
            RewardedAdBridge.NotifyClosed();
            OnRewardedAdClosed?.Invoke();
            if (!string.IsNullOrWhiteSpace(_rewardedAdUnitId) && AdPolicyManager.AreRewardedAdsAllowed())
            {
                _rewardedLoadRequested = false;
                EnsureRewardedAdLoaded();
            }
        };
        _rewardedAdManager.OnRewardedAdPaid += adValue =>
        {
            AdTelemetry.DispatchPaidEvent("rewarded", RewardedPlacementName, adValue, string.Empty);
            OnRewardedAdPaid?.Invoke(adValue);
        };

        GameLogger.Log("[AdMobManager] Tum Ad manager'lar baslatildi (AdMob Mediation destekli).");
    }

    private void StartStaggeredAdLoading()
    {
        if (_staggeredLoadRoutine != null)
            StopCoroutine(_staggeredLoadRoutine);

        var config = _runtimeConfig ?? Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        bool wantBanner = config == null || config.ShouldShowBannerForScene(SceneManager.GetActiveScene().name);
        SetBannerWanted(wantBanner);
        _staggeredLoadRoutine = StartCoroutine(LoadAdsStaggeredRoutine());
    }

    private IEnumerator LoadAdsStaggeredRoutine()
    {
        if (bannerLoadDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(bannerLoadDelaySeconds);
        EnsureBannerAdLoaded();

        float rewardedDelta = Mathf.Max(0f, rewardedLoadDelaySeconds - bannerLoadDelaySeconds);
        if (rewardedDelta > 0f)
            yield return new WaitForSecondsRealtime(rewardedDelta);
        EnsureRewardedAdLoaded();

        float interstitialDelta = Mathf.Max(0f, interstitialLoadDelaySeconds - rewardedLoadDelaySeconds);
        if (interstitialDelta > 0f)
            yield return new WaitForSecondsRealtime(interstitialDelta);
        EnsureInterstitialAdLoaded();

        GameLogger.Log("[AdMobManager] Reklam yukleri kademeli olarak baslatildi.");
        _staggeredLoadRoutine = null;
    }

    private void EnsureBannerAdLoaded()
    {
        if (!CanLoadAutomaticAdsNow)
            return;
        if (_bannerLoadRequested || string.IsNullOrWhiteSpace(_bannerAdUnitId) || _bannerAdManager == null)
            return;

        _bannerLoadRequested = true;
        _bannerAdManager.LoadBannerAd(_bannerAdUnitId);
    }

    private void EnsureInterstitialAdLoaded()
    {
        if (!CanLoadAutomaticAdsNow)
            return;
        if (_interstitialLoadRequested || string.IsNullOrWhiteSpace(_interstitialAdUnitId) || _interstitialAdManager == null)
            return;

        _interstitialLoadRequested = true;
        _interstitialAdManager.LoadInterstitialAd(_interstitialAdUnitId);
    }

    private void EnsureRewardedAdLoaded()
    {
        if (!CanLoadAdsNow || !AdPolicyManager.AreRewardedAdsAllowed())
            return;
        if (_rewardedLoadRequested || string.IsNullOrWhiteSpace(_rewardedAdUnitId) || _rewardedAdManager == null)
            return;

        _rewardedLoadRequested = true;
        _rewardedAdManager.LoadRewardedAd(_rewardedAdUnitId);
    }

    private void ReconcileLoadRequests()
    {
        _bannerLoadRequested = _bannerAdManager != null && (_bannerAdManager.IsLoading || _bannerAdManager.HasAd);
        _interstitialLoadRequested = _interstitialAdManager != null && (_interstitialAdManager.IsLoading || _interstitialAdManager.IsReady());
        _rewardedLoadRequested = _rewardedAdManager != null && (_rewardedAdManager.IsLoading || _rewardedAdManager.IsReady());
    }

    private void HandleRemoveAdsGranted()
    {
        _bannerLoadRequested = false;
        _interstitialLoadRequested = false;
        _bannerAdManager?.HideBannerAd();
        _bannerAdManager?.DestroyBannerAd();
        _interstitialAdManager?.DestroyInterstitialAd();
        GameLogger.Log("[AdMobManager] Remove Ads etkin; banner ve interstitial temizlendi.");
    }

    private void OnDestroy()
    {
        if (_instance == this)
            RewardedAdBridge.RegisterProvider(null, null);

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_staggeredLoadRoutine != null)
        {
            StopCoroutine(_staggeredLoadRoutine);
            _staggeredLoadRoutine = null;
        }

        if (_consentRetryScheduled)
        {
            CancelInvoke(nameof(RetryConsentGatedAdsFlow));
            _consentRetryScheduled = false;
        }

        if (_consentWatchdogRoutine != null)
        {
            StopCoroutine(_consentWatchdogRoutine);
            _consentWatchdogRoutine = null;
        }

        BlockPuzzle.UnityAdapter.Boot.GameBootstrap.OnGameOver -= HandleGameOverForPolicy;
        ConsentGate.ConsentStateChanged -= HandleConsentStateChanged;
        EntitlementManager.RemoveAdsGranted -= HandleRemoveAdsGranted;

        _bannerAdManager?.DestroyBannerAd();
        _interstitialAdManager?.DestroyInterstitialAd();
        _rewardedAdManager?.DestroyRewardedAd();
    }
}
