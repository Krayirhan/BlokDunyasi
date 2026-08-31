using UnityEngine;
using UnityEngine.SceneManagement;
using BlockPuzzle.Core.Common;

/// <summary>
/// Oyun baslangicinda tum sistem yoneticilerini baslatir.
/// Singleton olarak calisir ve otomatik olarak kendisini olusturur.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    private static AppInitializer instance;

    [SerializeField] [Min(0f)] private float updateCheckDelaySeconds = 2.5f;
    [SerializeField] [Min(0f)] private float notificationRefreshDelaySeconds = 0.75f;

    private Coroutine _initializationRoutine;

    public static AppInitializer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("AppInitializer");
                instance = obj.AddComponent<AppInitializer>();
                DontDestroyOnLoad(obj);
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CleanDuplicateEventSystems();
            InitializeAds();
            InitializeMetaSystems();
            _initializationRoutine = StartCoroutine(InitializeSystemsRoutine());
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAds()
    {
        var config = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        UnityEngine.Debug.Log($"[AppInitializer] Automatic ads startup. configFound={config != null}; loadOnStartup={config == null || config.LoadAdsOnStartup}");
        if (config != null && !config.LoadAdsOnStartup)
        {
            GameLogger.Log("[AppInitializer] Reklam baslatma runtime config tarafindan kapatildi.");
            return;
        }

        AdMobManager.Instance.ConfigureAndLoadAds(string.Empty, string.Empty, string.Empty);
        SceneManager.sceneLoaded += HandleSceneLoadedForAds;
        ApplyBannerPolicyForScene(SceneManager.GetActiveScene().name);
    }

    private void InitializeMetaSystems()
    {
        _ = BlockPuzzle.Core.Meta.Missions.MissionManager.Instance;
        _ = BlockPuzzle.Core.Meta.AchievementManager.Instance;
    }

    private void HandleSceneLoadedForAds(Scene scene, LoadSceneMode mode)
    {
        ApplyBannerPolicyForScene(scene.name);
    }

    private void ApplyBannerPolicyForScene(string sceneName)
    {
        var config = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        bool wantBanner = config == null || config.ShouldShowBannerForScene(sceneName);
        AdMobManager.Instance.SetBannerWanted(wantBanner);
    }

    private void CleanDuplicateEventSystems()
    {
        var eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsSortMode.None);
        if (eventSystems == null || eventSystems.Length <= 1)
            return;

        for (int i = 1; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] == null || eventSystems[i].gameObject == null)
                continue;

            Destroy(eventSystems[i].gameObject);
            GameLogger.Log("[AppInitializer] Fazla EventSystem silindi.");
        }
    }

    private System.Collections.IEnumerator InitializeSystemsRoutine()
    {
        yield return null;

        if (notificationRefreshDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(notificationRefreshDelaySeconds);

        NotificationManager.Instance.RefreshNotifications();

        float remainingUpdateDelay = updateCheckDelaySeconds - notificationRefreshDelaySeconds;
        if (remainingUpdateDelay > 0f)
            yield return new WaitForSecondsRealtime(remainingUpdateDelay);

        InAppUpdateManager.Instance.CheckForUpdates();

        GameLogger.Log("[AppInitializer] Sistem yoneticileri gecikmeli olarak baslatildi.");
        _initializationRoutine = null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoadedForAds;
        if (_initializationRoutine != null)
        {
            StopCoroutine(_initializationRoutine);
            _initializationRoutine = null;
        }
    }
}
