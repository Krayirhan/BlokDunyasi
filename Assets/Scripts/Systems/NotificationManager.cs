using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

/// <summary>
/// Local Push Notifications yönetir.
/// Oyuncu uygulamadan çıktıktan sonra belirli aralıklarla "Bloklar kırılmayı bekliyor!" gibi komik bildirimler gönderir.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    private static NotificationManager instance;
    private const string DAILY_REMINDER_CHANNEL = "blok_dunyasi_daily";
    private const int DailyReminderHour = 19;
    private const int NewFeaturesDelayDays = 3;
    private const int ComboReminderDelayHours = 12;

    // Bildirim mesajları
    private static readonly string[] DailyReminderMessages = new string[]
    {
        "Bloklar kendi kendine kırılmıyor! Hadi aç ve çöz! 🧱",
        "Rekorunu kır! Bloklar seni bekliyor! 🏆",
        "Yeni günün yeni rekorları, başla! 💪",
        "Blok Dünyası seni özledii! Gel oynamayı devam et! 👋",
        "30 saniyede kaç blok kıracaksın? Merak ediyoruz! 🤔",
    };

    private static readonly string[] NewFeaturesMessages = new string[]
    {
        "Yeni özellikleri keşfet! Blok Dünyası güncellendi! ✨",
        "Oyun gelişti, sen yetiştin mi? Kontrol et! 🆕",
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

    private void Start()
    {
#if UNITY_ANDROID
        // Android bildirim channel'ını oluştur
        CreateNotificationChannel();
        
        // Oyuncu tercihe göre bildirimler göndermeyi başlat
        ScheduleNotifications();
#endif
    }

#if UNITY_ANDROID
    /// <summary>
    /// Bildirim channel'ı oluştur (Android 8.0+)
    /// </summary>
    private void CreateNotificationChannel()
    {
        try
        {
            var channel = new AndroidNotificationChannel()
            {
                Id = DAILY_REMINDER_CHANNEL,
                Name = "Blok Dünyası Hatırlatmaları",
                Description = "Günlük oyun hatırlatmaları ve yeni özellik duyuruları",
                Importance = Importance.Default,
                CanBypassDnd = false,
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            Debug.Log("[NotificationManager] Bildirim channel'ı oluşturuldu.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NotificationManager] Channel oluşturma hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Kullanıcı tercihlerine göre bildirimler zamanla
    /// </summary>
    private void ScheduleNotifications()
    {
        // SettingsManager'dan bildirim tercihlerini oku
        bool dailyReminderEnabled = PlayerPrefs.GetInt(SettingsKeys.DailyReminder, 1) == 1;
        bool newFeaturesEnabled = PlayerPrefs.GetInt(SettingsKeys.NewFeatures, 1) == 1;

        // Önceki bildirimleri temizle
        AndroidNotificationCenter.CancelAllNotifications();

        if (dailyReminderEnabled)
        {
            SendDailyReminderNotification();
        }

        if (newFeaturesEnabled)
        {
            SendNewFeaturesNotification();
        }
    }

    /// <summary>
    /// Günlük hatırlatma bildirimi gönder
    /// </summary>
    private void SendDailyReminderNotification()
    {
        try
        {
            // Rastgele bir mesaj seç
            string message = DailyReminderMessages[UnityEngine.Random.Range(0, DailyReminderMessages.Length)];

            var notification = new AndroidNotification();
            notification.Title = "Blok Dünyası";
            notification.Text = message;
            notification.SmallIcon = "icon_0";
            notification.FireTime = GetNextLocalNotificationTime(DailyReminderHour);

            AndroidNotificationCenter.SendNotification(notification, DAILY_REMINDER_CHANNEL);

            Debug.Log($"[NotificationManager] Günlük bildirim zamanlandı: '{message}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NotificationManager] Günlük bildirim gönderme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Yeni özellikler bildirimi gönder
    /// </summary>
    private void SendNewFeaturesNotification()
    {
        try
        {
            // Rastgele bir mesaj seç
            string message = NewFeaturesMessages[UnityEngine.Random.Range(0, NewFeaturesMessages.Length)];

            var notification = new AndroidNotification();
            notification.Title = "Güncellemeler Mevcut!";
            notification.Text = message;
            notification.SmallIcon = "icon_0";
            notification.FireTime = DateTime.Now.AddDays(NewFeaturesDelayDays);

            AndroidNotificationCenter.SendNotification(notification, DAILY_REMINDER_CHANNEL);

            Debug.Log($"[NotificationManager] Yeni özellik bildirimi zamanlandı: '{message}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NotificationManager] Yeni özellik bildirimi gönderme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Combo kombo ayarı bildirimi gönder (isteğe bağlı, belirli bir combo skor sonrasında)
    /// </summary>
    public void SendComboNotification(int comboCount)
    {
        if (!PlayerPrefs.HasKey("ComboNotificationTime")) return;

        try
        {
            string message = $"Wow! {comboCount} kombo! Yeni rekor mu yaptın? Kontrol et! 🔥";

            var notification = new AndroidNotification();
            notification.Title = "Harika Combo!";
            notification.Text = message;
            notification.SmallIcon = "icon_0";
            notification.FireTime = DateTime.Now.AddHours(ComboReminderDelayHours);

            AndroidNotificationCenter.SendNotification(notification, DAILY_REMINDER_CHANNEL);

            Debug.Log($"[NotificationManager] Combo bildirimi gönderildi: '{message}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NotificationManager] Combo bildirimi hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Oyuncu ayarlarında tercihler değiştiğinde cagırılacak method
    /// </summary>
    public void RefreshNotifications()
    {
        ScheduleNotifications();
        Debug.Log("[NotificationManager] Bildirimler yenilendi.");
    }

    private static DateTime GetNextLocalNotificationTime(int preferredHour)
    {
        var now = DateTime.Now;
        var scheduled = new DateTime(now.Year, now.Month, now.Day, preferredHour, 0, 0);

        if (scheduled <= now)
            scheduled = scheduled.AddDays(1);

        return scheduled;
    }
#else
    private void ScheduleNotifications() { }
    public void SendComboNotification(int comboCount) { }
    public void RefreshNotifications() { }
#endif
}
