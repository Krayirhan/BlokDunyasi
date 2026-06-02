using UnityEngine;
using System.Collections;
using BlockPuzzle.Core.Common;
using Debug = BlockPuzzle.Core.Common.GameLogger;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

/// <summary>
/// Play Store tabanli In-App Update kontrolu yapar.
/// Uzak JSON veya harici endpoint bagimliligi yoktur.
/// </summary>
public class InAppUpdateManager : MonoBehaviour
{
    private static InAppUpdateManager instance;
    private bool _isChecking;
    private bool _hasCheckedOnce;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AppUpdateManager _appUpdateManager;
#endif

    [Header("Update Ayarlari")]
    [SerializeField] private bool preferImmediateUpdate = true;
    [SerializeField] [Min(1)] private int optionalPromptCooldownHours = 12;

    public static InAppUpdateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("InAppUpdateManager");
                instance = obj.AddComponent<InAppUpdateManager>();
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
#if UNITY_ANDROID && !UNITY_EDITOR
            _appUpdateManager = new AppUpdateManager();
#endif
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Tetikleme AppInitializer tarafindan yapiliyor.
    }

    public void CheckForUpdates(bool ignoreCooldown = false)
    {
        if (_isChecking)
            return;

        if (_hasCheckedOnce && !ignoreCooldown)
            return;

        StartCoroutine(CheckForUpdatesRoutine(ignoreCooldown));
    }

    private IEnumerator CheckForUpdatesRoutine(bool ignoreCooldown)
    {
        _isChecking = true;
        _hasCheckedOnce = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (_appUpdateManager == null)
            _appUpdateManager = new AppUpdateManager();

        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOp = _appUpdateManager.GetAppUpdateInfo();
        yield return appUpdateInfoOp;

        if (appUpdateInfoOp.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppUpdateManager] Play Store update info alinamadi: {appUpdateInfoOp.Error}");
            _isChecking = false;
            yield break;
        }

        AppUpdateInfo appUpdateInfo = appUpdateInfoOp.GetResult();
        if (appUpdateInfo.UpdateAvailability != UpdateAvailability.UpdateAvailable)
        {
            Debug.Log("[InAppUpdateManager] Play Store update yok.");
            _isChecking = false;
            yield break;
        }

        bool immediateAllowed = appUpdateInfo.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions());
        bool flexibleAllowed = appUpdateInfo.IsUpdateTypeAllowed(AppUpdateOptions.FlexibleAppUpdateOptions());
        bool shouldUseImmediate = immediateAllowed && (preferImmediateUpdate || !flexibleAllowed);

        if (!shouldUseImmediate && !ignoreCooldown && !ShouldPromptOptionalUpdate(appUpdateInfo.AvailableVersionCode.ToString()))
        {
            Debug.Log("[InAppUpdateManager] Esnek update promptu cooldown nedeniyle ertelendi.");
            _isChecking = false;
            yield break;
        }

        var updateOptions = shouldUseImmediate
            ? AppUpdateOptions.ImmediateAppUpdateOptions()
            : AppUpdateOptions.FlexibleAppUpdateOptions();

        var startUpdateRequest = _appUpdateManager.StartUpdate(appUpdateInfo, updateOptions);
        yield return startUpdateRequest;

        if (startUpdateRequest.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning($"[InAppUpdateManager] Update baslatilamadi: {startUpdateRequest.Error}");
            _isChecking = false;
            yield break;
        }

        MarkPrompted(appUpdateInfo.AvailableVersionCode.ToString());
        Debug.Log($"[InAppUpdateManager] Update akisi baslatildi. Tip={(shouldUseImmediate ? "Immediate" : "Flexible")}");
#else
        Debug.Log("[InAppUpdateManager] Play Store in-app update sadece Android build'de aktif.");
        yield return null;
#endif

        _isChecking = false;
    }

    private bool ShouldPromptOptionalUpdate(string latestVersion)
    {
        string lastVersion = PlayerPrefs.GetString(SettingsKeys.UpdateLastPromptedVersion, string.Empty);
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string rawLastPrompt = PlayerPrefs.GetString(SettingsKeys.UpdateLastPromptUnixSeconds, "0");
        if (!long.TryParse(rawLastPrompt, out long lastPrompt))
            lastPrompt = 0L;
        long cooldownSeconds = System.Math.Max(1, optionalPromptCooldownHours) * 3600L;

        if (!string.Equals(lastVersion, latestVersion, System.StringComparison.Ordinal))
            return true;

        return now - lastPrompt >= cooldownSeconds;
    }

    private void MarkPrompted(string latestVersion)
    {
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(SettingsKeys.UpdateLastPromptedVersion, latestVersion ?? string.Empty);
        PlayerPrefs.SetString(SettingsKeys.UpdateLastPromptUnixSeconds, now.ToString());
        PlayerPrefs.Save();
    }
}
