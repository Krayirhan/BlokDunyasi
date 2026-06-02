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
    private static AdMobBootstrap _instance;
    private static bool _initialized;
    private static bool _initializing;
    private static readonly List<Action<bool>> PendingInitializationCallbacks = new();

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
        ConfigureRequestSettings(config);

        MobileAds.Initialize((initStatus) =>
        {
            _initializing = false;
            _initialized = initStatus != null;

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

    private static void ConfigureRequestSettings(AdMobRuntimeConfig config)
    {
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
