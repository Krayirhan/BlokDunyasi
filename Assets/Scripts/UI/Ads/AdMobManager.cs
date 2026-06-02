using BlockPuzzle.UnityAdapter.UI;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Privacy;
using GoogleMobileAds.Api;
using System;
using UnityEngine;
using System.Collections;

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
        && AdMobBootstrap.Instance.IsInitialized
        && AdPolicyManager.AreAdsAllowed();

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

        // Korumayı devreye sokmak için oyun bittiğinde sayaç düşürülmeli
        BlockPuzzle.UnityAdapter.Boot.GameBootstrap.OnGameOver += HandleGameOverForPolicy;
    }

    private void HandleGameOverForPolicy(int score)
    {
        AdPolicyManager.OnMatchEnded();
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

    public void ShowBannerAd()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return;
        if (!AdPolicyManager.AreAdsAllowed()) return;
        EnsureBannerAdLoaded();
        _bannerAdManager?.ShowBannerAd();
    }

    public void HideBannerAd()
    {
        _bannerAdManager?.HideBannerAd();
    }

    public void RefreshBannerLayout()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return;
        if (!AdPolicyManager.AreAdsAllowed()) return;
        EnsureBannerAdLoaded();
        _bannerAdManager?.RefreshBannerLayout();
    }

    public bool IsInterstitialReady()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return false;
        if (!AdPolicyManager.AreAdsAllowed()) return false;
        EnsureInterstitialAdLoaded();
        return _interstitialAdManager != null && _interstitialAdManager.IsReady();
    }

    public void ShowInterstitialAd()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return;
        if (!AdPolicyManager.AreAdsAllowed()) return;
        EnsureInterstitialAdLoaded();
        if (_interstitialAdManager != null && _interstitialAdManager.IsReady()) 
            _interstitialAdManager.ShowInterstitialAd();
    }

    public bool IsRewardedAdReady()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return false;
        if (!AdPolicyManager.AreAdsAllowed()) return false;
        EnsureRewardedAdLoaded();
        return _rewardedAdManager != null && _rewardedAdManager.IsReady();      
    }

    public void ShowRewardedAd()
    {
        if (!ConsentGate.CanInitializeAds || !AdMobBootstrap.Instance.IsInitialized) return;
        if (!AdPolicyManager.AreAdsAllowed()) return;
        EnsureRewardedAdLoaded();
        if (_rewardedAdManager != null && _rewardedAdManager.IsReady())
            _rewardedAdManager.ShowRewardedAd();
    }

    private void StartConsentGatedAdsFlow()
    {
        if (!_adsConfigured)
            return;

        if (ConsentGate.CurrentState == ConsentState.Accepted)
        {
            InitializeSdkAndStartLoading();
            return;
        }

        if (ConsentGate.CurrentState == ConsentState.Declined)
        {
            _runtimeState = AdsRuntimeState.ConsentDeclined;
            GameLogger.LogWarning("[AdMobManager] Ads remain disabled because consent was declined.");
            return;
        }

        _runtimeState = AdsRuntimeState.WaitingForConsent;
        AdMobConsentManager.Instance.EnsureConsentResolved(_runtimeConfig, (state, errorMessage) =>
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                _runtimeState = AdsRuntimeState.Failed;
                GameLogger.LogWarning($"[AdMobManager] Consent resolution failed. Ads startup is deferred. reason={errorMessage}");
                return;
            }

            if (state == ConsentState.Accepted)
                InitializeSdkAndStartLoading();
            else
            {
                _runtimeState = AdsRuntimeState.ConsentDeclined;
                GameLogger.LogWarning("[AdMobManager] Consent was not granted. Ads will remain disabled.");
            }
        });
    }

    private void InitializeSdkAndStartLoading()
    {
        if (_sdkInitializationRequested)
            return;

        _sdkInitializationRequested = true;
        _runtimeState = AdsRuntimeState.Initializing;
        AdMobBootstrap.Instance.InitializeGoogleMobileAds(_runtimeConfig, success =>
        {
            if (!success)
            {
                _sdkInitializationRequested = false;
                _runtimeState = AdsRuntimeState.Failed;
                GameLogger.LogWarning("[AdMobManager] AdMob SDK init failed. Ad loading stays disabled.");
                return;
            }

            _runtimeState = AdsRuntimeState.Ready;
            StartStaggeredAdLoading();
        });
    }

    private void HandleConsentStateChanged(ConsentState state)
    {
        if (state == ConsentState.Accepted)
        {
            StartConsentGatedAdsFlow();
            return;
        }

        _runtimeState = state == ConsentState.Declined
            ? AdsRuntimeState.ConsentDeclined
            : AdsRuntimeState.WaitingForConsent;

        if (_staggeredLoadRoutine != null)
        {
            StopCoroutine(_staggeredLoadRoutine);
            _staggeredLoadRoutine = null;
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
        _bannerAdManager.OnBannerFailedToLoad += error => OnBannerFailedToLoad?.Invoke(error);
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
        _interstitialAdManager.OnInterstitialFailedToLoad += error => OnInterstitialFailedToLoad?.Invoke(error);
        _interstitialAdManager.OnInterstitialShown += () =>
        {
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
            AdTelemetry.DispatchLifecycleEvent("rewarded", RewardedPlacementName, "reward_earned", reward.Type, reward.Amount);
            RewardedAdBridge.NotifyUserEarned();
            OnRewardedUserEarned?.Invoke();
        };
        _rewardedAdManager.OnRewardedAdClosed += () =>
        {
            AdTelemetry.DispatchLifecycleEvent("rewarded", RewardedPlacementName, "closed");
            RewardedAdBridge.NotifyClosed();
            OnRewardedAdClosed?.Invoke();
            if (!string.IsNullOrWhiteSpace(_rewardedAdUnitId) && AdPolicyManager.AreAdsAllowed())
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
        if (!AdPolicyManager.AreAdsAllowed())
        {
            GameLogger.LogWarning("[AdMobManager] AdPolicyManager says ads are CURRENTLY BANNED due to click limits. Not loading any ads.");
            return;
        }

        if (_staggeredLoadRoutine != null)
            StopCoroutine(_staggeredLoadRoutine);

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
        if (_bannerLoadRequested || string.IsNullOrWhiteSpace(_bannerAdUnitId) || _bannerAdManager == null)
            return;

        _bannerLoadRequested = true;
        _bannerAdManager.LoadBannerAd(_bannerAdUnitId);
    }

    private void EnsureInterstitialAdLoaded()
    {
        if (_interstitialLoadRequested || string.IsNullOrWhiteSpace(_interstitialAdUnitId) || _interstitialAdManager == null)
            return;

        _interstitialLoadRequested = true;
        _interstitialAdManager.LoadInterstitialAd(_interstitialAdUnitId);
    }

    private void EnsureRewardedAdLoaded()
    {
        if (_rewardedLoadRequested || string.IsNullOrWhiteSpace(_rewardedAdUnitId) || _rewardedAdManager == null)
            return;

        _rewardedLoadRequested = true;
        _rewardedAdManager.LoadRewardedAd(_rewardedAdUnitId);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            RewardedAdBridge.RegisterProvider(null, null);

        if (_staggeredLoadRoutine != null)
        {
            StopCoroutine(_staggeredLoadRoutine);
            _staggeredLoadRoutine = null;
        }

        BlockPuzzle.UnityAdapter.Boot.GameBootstrap.OnGameOver -= HandleGameOverForPolicy;
        ConsentGate.ConsentStateChanged -= HandleConsentStateChanged;

        _bannerAdManager?.DestroyBannerAd();
        _interstitialAdManager?.DestroyInterstitialAd();
        _rewardedAdManager?.DestroyRewardedAd();
    }
}
