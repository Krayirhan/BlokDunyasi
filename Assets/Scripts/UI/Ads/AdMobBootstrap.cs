using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;

/// <summary>
/// Google Mobile Ads SDK baslatma yasam dongusunu yonetir.
/// Consent tamamlanmadan reklam yuklemesi tetiklenmez.
/// </summary>
public class AdMobBootstrap : MonoBehaviour
{
    private const float InitializationTimeoutSeconds = 15f;
    private static AdMobBootstrap _instance;
    private static bool _initialized;
    private static bool _initializing;
    private static readonly List<Action<bool>> PendingInitializationCallbacks = new();
    private Coroutine _initializationWatchdogRoutine;
    private int _initializationGeneration;

    public static AdMobBootstrap Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject bootstrapGO = new GameObject("AdMobBootstrap");
                _instance = bootstrapGO.AddComponent<AdMobBootstrap>();
                DontDestroyOnLoad(bootstrapGO);
            }

            return _instance;
        }
    }

    public bool IsInitialized => _initialized;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeGoogleMobileAds(AdMobRuntimeConfig config, Action<bool> onComplete = null)
    {
        if (onComplete != null)
            PendingInitializationCallbacks.Add(onComplete);

        if (_initialized)
        {
            FlushPendingCallbacks(true);
            return;
        }

        if (_initializing)
            return;

        _initializing = true;
        int generation = ++_initializationGeneration;
        UnityEngine.Debug.Log($"[AdMob] SDK initialization started. generation={generation}; editor={Application.isEditor}; platform={Application.platform}");
        if (_initializationWatchdogRoutine != null)
            StopCoroutine(_initializationWatchdogRoutine);
        _initializationWatchdogRoutine = StartCoroutine(InitializationWatchdogRoutine(generation));
        ConfigureRequestSettings(config);

        MobileAds.Initialize((initStatus) =>
        {
            if (generation != _initializationGeneration)
                return;

            if (_initializationWatchdogRoutine != null)
            {
                StopCoroutine(_initializationWatchdogRoutine);
                _initializationWatchdogRoutine = null;
            }
            _initializing = false;
            _initialized = initStatus != null;
            UnityEngine.Debug.Log($"[AdMob] SDK initialization callback received. success={_initialized}; statusNull={initStatus == null}");

            if (_initialized)
            {
                Debug.Log("[AdMob] SDK basariyla initialize edildi.");
            }
            else
            {
                Debug.LogError("[AdMob] SDK initialize edilemedi.");
            }

            FlushPendingCallbacks(_initialized);
        });
    }

    private System.Collections.IEnumerator InitializationWatchdogRoutine(int generation)
    {
        yield return new WaitForSecondsRealtime(InitializationTimeoutSeconds);
        if (generation != _initializationGeneration || !_initializing)
            yield break;

        _initializationWatchdogRoutine = null;
        _initializing = false;
        UnityEngine.Debug.LogError("[AdMob] SDK initialization callback timeout.");
        Debug.LogError("[AdMob] SDK initialization callback zaman asimi.");
        FlushPendingCallbacks(false);
    }

    private static void ConfigureRequestSettings(AdMobRuntimeConfig config)
    {
#pragma warning disable 0618
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore 0618
        MobileAds.SetiOSAppPauseOnBackground(true);

        var requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = config != null ? new List<string>(config.GetTestDeviceHashedIds()) : new List<string>()
        };

        MobileAds.SetRequestConfiguration(requestConfiguration);
    }

    private static void FlushPendingCallbacks(bool success)
    {
        if (PendingInitializationCallbacks.Count == 0)
            return;

        Action<bool>[] callbacks = PendingInitializationCallbacks.ToArray();
        PendingInitializationCallbacks.Clear();

        for (int i = 0; i < callbacks.Length; i++)
        {
            try
            {
                callbacks[i]?.Invoke(success);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdMob] Initialization callback failed: {ex.Message}");
            }
        }
    }
}
