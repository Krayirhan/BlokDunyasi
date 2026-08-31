using System;
using BlockPuzzle.Core.Common;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

/// <summary>
/// Manages local notifications for reminders and update availability.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    private static NotificationManager instance;

    private const string DailyReminderChannel = "blok_dunyasi_daily";
    private const string UpdateReminderChannel = "blok_dunyasi_updates";
    private const int DailyReminderHour = 19;
    private const int NewFeaturesDelayDays = 3;
    private const int ComboReminderDelayHours = 12;
    private const int UpdateReminderDelayHours = 2;

    private static readonly string[] DailyReminderMessages =
    {
        "Bloklar kendi kendine kirilmiyor. Ac ve coz.",
        "Rekorunu kir. Bloklar seni bekliyor.",
        "Yeni gun, yeni rekorlar. Basla.",
        "Blok Dunyasi seni bekliyor. Devam et.",
        "30 saniyede kac blok kiracaksin?"
    };

    private static readonly string[] NewFeaturesMessages =
    {
        "Yeni ozellikleri kesfet. Blok Dunyasi guncellendi.",
        "Oyunda yenilik var. Kontrol et."
    };

    public static NotificationManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("NotificationManager");
                instance = obj.AddComponent<NotificationManager>();
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
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
#if UNITY_ANDROID
        SubscribeUpdateSignals();
#endif
    }

    private void OnDisable()
    {
#if UNITY_ANDROID
        UnsubscribeUpdateSignals();
#endif
    }

    private void Start()
    {
#if UNITY_ANDROID
        CreateNotificationChannels();
        SubscribeUpdateSignals();
        ScheduleNotifications();
#endif
    }

#if UNITY_ANDROID
    private void CreateNotificationChannels()
    {
        try
        {
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = DailyReminderChannel,
                Name = "Blok Dunyasi Hatirlatmalari",
                Description = "Gunluk oyun hatirlatmalari ve duyurular",
                Importance = Importance.Default,
                CanBypassDnd = false
            });

            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = UpdateReminderChannel,
                Name = "Blok Dunyasi Guncellemeleri",
                Description = "Yeni surum hatirlatmalari",
                Importance = Importance.Default,
                CanBypassDnd = false
            });

            Debug.Log("[NotificationManager] Notification channels registered.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NotificationManager] Channel registration failed: {ex.Message}");
        }
    }

    private void ScheduleNotifications()
    {
        bool dailyReminderEnabled = PlayerPrefs.GetInt(SettingsKeys.DailyReminder, 1) == 1;
        bool newFeaturesEnabled = PlayerPrefs.GetInt(SettingsKeys.NewFeatures, 1) == 1;

        AndroidNotificationCenter.CancelAllScheduledNotifications();

        if (dailyReminderEnabled)
            SendDailyReminderNotification();

        if (newFeaturesEnabled)
            SendNewFeaturesNotification();

        if (newFeaturesEnabled)
            RefreshUpdateReminderFromState();
        else
            CancelStoredUpdateReminderOnly();
    }

    private void SendDailyReminderNotification()
    {
        try
        {
            string message = DailyReminderMessages[UnityEngine.Random.Range(0, DailyReminderMessages.Length)];
            var notification = new AndroidNotification
            {
                Title = "Blok Dunyasi",
                Text = message,
                SmallIcon = "icon_0",
                FireTime = GetNextLocalNotificationTime(DailyReminderHour)
            };

            AndroidNotificationCenter.SendNotification(notification, DailyReminderChannel);
            Debug.Log($"[NotificationManager] Daily reminder scheduled: '{message}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NotificationManager] Daily reminder failed: {ex.Message}");
        }
    }

    private void SendNewFeaturesNotification()
    {
        try
        {
            string message = NewFeaturesMessages[UnityEngine.Random.Range(0, NewFeaturesMessages.Length)];
            var notification = new AndroidNotification
            {
                Title = "Guncelleme Duyurusu",
                Text = message,
                SmallIcon = "icon_0",
                FireTime = DateTime.Now.AddDays(NewFeaturesDelayDays)
            };

            AndroidNotificationCenter.SendNotification(notification, DailyReminderChannel);
            Debug.Log($"[NotificationManager] Feature update notification scheduled: '{message}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NotificationManager] Feature update notification failed: {ex.Message}");
        }
    }

    public void SendComboNotification(int comboCount)
    {
        if (!PlayerPrefs.HasKey("ComboNotificationTime"))
            return;

        try
        {
            var notification = new AndroidNotification
            {
                Title = "Harika Combo",
                Text = $"Wow. {comboCount} kombo yaptin. Tekrar bak.",
                SmallIcon = "icon_0",
                FireTime = DateTime.Now.AddHours(ComboReminderDelayHours)
            };

            AndroidNotificationCenter.SendNotification(notification, DailyReminderChannel);
            Debug.Log($"[NotificationManager] Combo notification scheduled for combo={comboCount}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NotificationManager] Combo notification failed: {ex.Message}");
        }
    }

    public void RefreshNotifications()
    {
        ScheduleNotifications();
        Debug.Log("[NotificationManager] Notifications refreshed.");
    }

    public void ScheduleUpdateReminder(string storeVersion)
    {
        bool newFeaturesEnabled = PlayerPrefs.GetInt(SettingsKeys.NewFeatures, 1) == 1;
        if (!newFeaturesEnabled)
        {
            CancelStoredUpdateReminderOnly();
            return;
        }

        string normalizedStoreVersion = string.IsNullOrWhiteSpace(storeVersion) ? "latest" : storeVersion.Trim();
        PlayerPrefs.SetString(SettingsKeys.UpdateReminderStoreVersion, normalizedStoreVersion);
        PlayerPrefs.Save();

        try
        {
            var immediateNotification = new AndroidNotification
            {
                Title = "Guncelleme Hazir",
                Text = $"Yeni surum mevcut. Oyun: {Application.version}, Magaza: {normalizedStoreVersion}.",
                SmallIcon = "icon_0",
                FireTime = DateTime.Now.AddSeconds(5)
            };

            AndroidNotificationCenter.SendNotification(immediateNotification, UpdateReminderChannel);

            var notification = new AndroidNotification
            {
                Title = "Guncelleme Hazir",
                Text = $"Yeni surum mevcut. Oyun: {Application.version}, Magaza: {normalizedStoreVersion}.",
                SmallIcon = "icon_0",
                FireTime = DateTime.Now.AddHours(UpdateReminderDelayHours),
                RepeatInterval = TimeSpan.FromHours(UpdateReminderDelayHours)
            };

            AndroidNotificationCenter.SendNotification(notification, UpdateReminderChannel);
            Debug.Log($"[NotificationManager] Update reminder scheduled. StoreVersion={normalizedStoreVersion}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NotificationManager] Update reminder scheduling failed: {ex.Message}");
        }
    }

    private void SubscribeUpdateSignals()
    {
        if (InAppUpdateManager.Instance == null)
            return;

        InAppUpdateManager.Instance.OnUpdateAvailabilityEvaluated -= HandleUpdateAvailabilityEvaluated;
        InAppUpdateManager.Instance.OnUpdateAvailabilityEvaluated += HandleUpdateAvailabilityEvaluated;
    }

    private void UnsubscribeUpdateSignals()
    {
        if (InAppUpdateManager.Instance == null)
            return;

        InAppUpdateManager.Instance.OnUpdateAvailabilityEvaluated -= HandleUpdateAvailabilityEvaluated;
    }

    private void HandleUpdateAvailabilityEvaluated(bool isAvailable, string storeVersion)
    {
        if (isAvailable)
        {
            ScheduleUpdateReminder(storeVersion);
            return;
        }

        CancelStoredUpdateReminderOnly();
    }

    private void RefreshUpdateReminderFromState()
    {
        if (InAppUpdateManager.Instance != null && InAppUpdateManager.Instance.IsUpdateAvailable)
        {
            ScheduleUpdateReminder(InAppUpdateManager.Instance.AvailableStoreVersion);
            return;
        }

        string storedVersion = PlayerPrefs.GetString(SettingsKeys.UpdateReminderStoreVersion, string.Empty);
        if (!string.IsNullOrWhiteSpace(storedVersion))
            ScheduleUpdateReminder(storedVersion);
    }

    private void CancelStoredUpdateReminderOnly()
    {
        PlayerPrefs.DeleteKey(SettingsKeys.UpdateReminderStoreVersion);
        PlayerPrefs.Save();
    }

    private static DateTime GetNextLocalNotificationTime(int preferredHour)
    {
        DateTime now = DateTime.Now;
        DateTime scheduled = new DateTime(now.Year, now.Month, now.Day, preferredHour, 0, 0);

        if (scheduled <= now)
            scheduled = scheduled.AddDays(1);

        return scheduled;
    }
#else
    public void SendComboNotification(int comboCount) { }
    public void RefreshNotifications() { }
    public void ScheduleUpdateReminder(string storeVersion) { }
#endif
}
