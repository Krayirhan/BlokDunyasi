using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    // PlayerPrefs keys
    const string KEY_MUSIC_ON = "settings_music_on";
    const string KEY_MUSIC_VOL = "settings_music_vol";
    const string KEY_SFX_ON = "settings_sfx_on";
    const string KEY_SFX_VOL = "settings_sfx_vol";
    const string KEY_VIBRATION = "settings_vibration";

    // Gameplay keys
    const string KEY_PLACEMENT_PREVIEW = "settings_placement_preview";
    const string KEY_GRID_HIGHLIGHT = "settings_grid_highlight";
    const string KEY_COMBO_VISUAL = "settings_combo_visual";
    const string KEY_ANIMATIONS = "settings_animations";
    const string KEY_AUTOSAVE = "settings_autosave";

    // Visual
    const string KEY_THEME = "settings_theme"; // 0=Light,1=Dark,2=Colorful
    const string KEY_REDUCE_MOTION = "settings_reduce_motion";
    const string KEY_HIGH_CONTRAST = "settings_high_contrast";

    // Language
    const string KEY_LANGUAGE = "settings_language"; // "tr" or "en"

    // Notifications
    const string KEY_DAILY_REMINDER = "settings_daily_reminder";
    const string KEY_NEW_FEATURES = "settings_new_features";
    const string KEY_TIPS = "settings_tips";

    // UI references (found at runtime)
    Slider musicSlider;
    Toggle musicToggle;
    Slider sfxSlider;
    Toggle sfxToggle;
    Toggle vibrationToggle;

    Toggle placementPreviewToggle;
    Toggle gridHighlightToggle;
    Toggle comboVisualToggle;
    Toggle animationsToggle;
    Toggle autoSaveToggle;

    Button themeLightButton;
    Button themeDarkButton;
    Button themeColorfulButton;

    Dropdown languageDropdown;

    Toggle dailyReminderToggle;
    Toggle newFeaturesToggle;
    Toggle tipsToggle;

    Button resetProgressButton;
    Button clearCacheButton;
    Button restoreDefaultsButton;

    Text versionText;
    bool CanLog => Debug.isDebugBuild;

    void Awake()
    {
        // Find UI elements by path - created by the editor setup
        musicToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/AudioCard/MusicToggle");
        musicSlider = FindSlider("SettingsCanvas/ScrollView/Viewport/Content/AudioCard/MusicSlider");
        sfxToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/AudioCard/SFXToggle");
        sfxSlider = FindSlider("SettingsCanvas/ScrollView/Viewport/Content/AudioCard/SFXSlider");
        vibrationToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/AudioCard/VibrationToggle");

        placementPreviewToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/GameplayCard/PlacementPreviewToggle");
        gridHighlightToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/GameplayCard/GridHighlightToggle");
        comboVisualToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/GameplayCard/ComboVisualToggle");
        animationsToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/GameplayCard/AnimationsToggle");
        autoSaveToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/GameplayCard/AutoSaveToggle");

        themeLightButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/VisualCard/ThemeLightButton");
        themeDarkButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/VisualCard/ThemeDarkButton");
        themeColorfulButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/VisualCard/ThemeColorfulButton");

        languageDropdown = FindDropdown("SettingsCanvas/ScrollView/Viewport/Content/LanguageCard/LanguageDropdown");

        dailyReminderToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/NotificationsCard/DailyReminderToggle");
        newFeaturesToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/NotificationsCard/NewFeaturesToggle");
        tipsToggle = FindToggle("SettingsCanvas/ScrollView/Viewport/Content/NotificationsCard/TipsToggle");

        resetProgressButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/DataCard/ResetProgressButton");
        clearCacheButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/DataCard/ClearCacheButton");
        restoreDefaultsButton = FindButton("SettingsCanvas/ScrollView/Viewport/Content/DataCard/RestoreDefaultsButton");

        versionText = FindText("SettingsCanvas/ScrollView/Viewport/Content/AboutCard/VersionText");

        WarnIfMissing(musicToggle, "MusicToggle");
        WarnIfMissing(musicSlider, "MusicSlider");
        WarnIfMissing(sfxToggle, "SFXToggle");
        WarnIfMissing(sfxSlider, "SFXSlider");
        WarnIfMissing(vibrationToggle, "VibrationToggle");
        WarnIfMissing(placementPreviewToggle, "PlacementPreviewToggle");
        WarnIfMissing(gridHighlightToggle, "GridHighlightToggle");
        WarnIfMissing(comboVisualToggle, "ComboVisualToggle");
        WarnIfMissing(animationsToggle, "AnimationsToggle");
        WarnIfMissing(autoSaveToggle, "AutoSaveToggle");
        WarnIfMissing(themeLightButton, "ThemeLightButton");
        WarnIfMissing(themeDarkButton, "ThemeDarkButton");
        WarnIfMissing(themeColorfulButton, "ThemeColorfulButton");
        WarnIfMissing(languageDropdown, "LanguageDropdown");
        WarnIfMissing(dailyReminderToggle, "DailyReminderToggle");
        WarnIfMissing(newFeaturesToggle, "NewFeaturesToggle");
        WarnIfMissing(tipsToggle, "TipsToggle");
        WarnIfMissing(resetProgressButton, "ResetProgressButton");
        WarnIfMissing(clearCacheButton, "ClearCacheButton");
        WarnIfMissing(restoreDefaultsButton, "RestoreDefaultsButton");
        WarnIfMissing(versionText, "VersionText");

        BindListeners();
        LoadValuesToUI();
    }

    void BindListeners()
    {
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);

        if (placementPreviewToggle != null) placementPreviewToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_PLACEMENT_PREVIEW, v ? 1 : 0));
        if (gridHighlightToggle != null) gridHighlightToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_GRID_HIGHLIGHT, v ? 1 : 0));
        if (comboVisualToggle != null) comboVisualToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_COMBO_VISUAL, v ? 1 : 0));
        if (animationsToggle != null) animationsToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_ANIMATIONS, v ? 1 : 0));
        if (autoSaveToggle != null) autoSaveToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_AUTOSAVE, v ? 1 : 0));

        if (themeLightButton != null) themeLightButton.onClick.AddListener(() => SetTheme(0));
        if (themeDarkButton != null) themeDarkButton.onClick.AddListener(() => SetTheme(1));
        if (themeColorfulButton != null) themeColorfulButton.onClick.AddListener(() => SetTheme(2));

        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        if (dailyReminderToggle != null) dailyReminderToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_DAILY_REMINDER, v ? 1 : 0));
        if (newFeaturesToggle != null) newFeaturesToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_NEW_FEATURES, v ? 1 : 0));
        if (tipsToggle != null) tipsToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_TIPS, v ? 1 : 0));

        if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgressClicked);
        if (clearCacheButton != null) clearCacheButton.onClick.AddListener(OnClearCacheClicked);
        if (restoreDefaultsButton != null) restoreDefaultsButton.onClick.AddListener(OnRestoreDefaultsClicked);
    }

    void LoadValuesToUI()
    {
        // Audio
        bool musicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
        float musicVol = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        bool sfxOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        bool vibrationOn = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;

        if (musicToggle != null) musicToggle.isOn = musicOn;
        if (musicSlider != null) { musicSlider.value = musicVol; musicSlider.interactable = musicOn; }
        if (sfxToggle != null) sfxToggle.isOn = sfxOn;
        if (sfxSlider != null) { sfxSlider.value = sfxVol; sfxSlider.interactable = sfxOn; }
        if (vibrationToggle != null) vibrationToggle.isOn = vibrationOn;

        // Gameplay
        if (placementPreviewToggle != null) placementPreviewToggle.isOn = PlayerPrefs.GetInt(KEY_PLACEMENT_PREVIEW, 1) == 1;
        if (gridHighlightToggle != null) gridHighlightToggle.isOn = PlayerPrefs.GetInt(KEY_GRID_HIGHLIGHT, 1) == 1;
        if (comboVisualToggle != null) comboVisualToggle.isOn = PlayerPrefs.GetInt(KEY_COMBO_VISUAL, 1) == 1;
        if (animationsToggle != null) animationsToggle.isOn = PlayerPrefs.GetInt(KEY_ANIMATIONS, 1) == 1;
        if (autoSaveToggle != null) autoSaveToggle.isOn = PlayerPrefs.GetInt(KEY_AUTOSAVE, 1) == 1;

        // Visual
        int theme = PlayerPrefs.GetInt(KEY_THEME, 1);
        // highlight handled in SetTheme when clicked
        if (languageDropdown != null)
        {
            string lang = PlayerPrefs.GetString(KEY_LANGUAGE, "tr");
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "Türkçe", "English" });
            languageDropdown.value = (lang == "tr") ? 0 : 1;
        }

        if (dailyReminderToggle != null) dailyReminderToggle.isOn = PlayerPrefs.GetInt(KEY_DAILY_REMINDER, 0) == 1;
        if (newFeaturesToggle != null) newFeaturesToggle.isOn = PlayerPrefs.GetInt(KEY_NEW_FEATURES, 1) == 1;
        if (tipsToggle != null) tipsToggle.isOn = PlayerPrefs.GetInt(KEY_TIPS, 1) == 1;

        if (versionText != null) versionText.text = "v1.0.0";
    }

    // Callbacks
    void OnMusicToggleChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_MUSIC_ON, on ? 1 : 0);
        if (musicSlider != null) musicSlider.interactable = on;
        if (CanLog) Debug.Log("Music toggled: " + on);
        // Optionally mute music source
    }

    void OnMusicVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, v);
        if (CanLog) Debug.Log("Music volume: " + v);
        // Update audio mixer or music player if available
        AudioListener.volume = Mathf.Clamp01(v); // simple master effect
    }

    void OnSFXToggleChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_SFX_ON, on ? 1 : 0);
        if (sfxSlider != null) sfxSlider.interactable = on;
        if (CanLog) Debug.Log("SFX toggled: " + on);
    }

    void OnSFXVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOL, v);
        if (CanLog) Debug.Log("SFX volume: " + v);
    }

    void OnVibrationToggleChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_VIBRATION, on ? 1 : 0);
        if (CanLog) Debug.Log("Vibration toggled: " + on);
        if (on)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    void SetTheme(int id)
    {
        PlayerPrefs.SetInt(KEY_THEME, id);
        if (CanLog) Debug.Log("Theme set: " + id);
        // Apply theme preview logic here (colors, background, etc.)
    }

    void OnLanguageChanged(int idx)
    {
        string lang = idx == 0 ? "tr" : "en";
        PlayerPrefs.SetString(KEY_LANGUAGE, lang);
        if (CanLog) Debug.Log("Language set: " + lang);
    }

    void OnResetProgressClicked()
    {
        // Show a confirmation modal
        ShowConfirm("Sıfırlama", "Tüm ilerlemeyi sıfırlamak istediğinize emin misiniz?", () =>
        {
            // Perform reset
            PlayerPrefs.DeleteKey("player_highscore");
            PlayerPrefs.Save();
            if (CanLog) Debug.Log("Progress reset.");
        });
    }

    void OnClearCacheClicked()
    {
        // Simulate clear cache
        if (CanLog) Debug.Log("Cache cleared.");
        // Show small UI feedback if you have a toast system. For now just log.
    }

    void OnRestoreDefaultsClicked()
    {
        ShowConfirm("Varsayılanlara Dön", "Ayarları varsayılan değerlere geri döndürmek istiyor musunuz?", () =>
        {
            RestoreDefaults();
            if (CanLog) Debug.Log("Defaults restored.");
        });
    }

    void RestoreDefaults()
    {
        PlayerPrefs.SetInt(KEY_MUSIC_ON, 1);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, 1f);
        PlayerPrefs.SetInt(KEY_SFX_ON, 1);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, 1f);
        PlayerPrefs.SetInt(KEY_VIBRATION, 1);

        PlayerPrefs.SetInt(KEY_PLACEMENT_PREVIEW, 1);
        PlayerPrefs.SetInt(KEY_GRID_HIGHLIGHT, 1);
        PlayerPrefs.SetInt(KEY_COMBO_VISUAL, 1);
        PlayerPrefs.SetInt(KEY_ANIMATIONS, 1);
        PlayerPrefs.SetInt(KEY_AUTOSAVE, 1);

        PlayerPrefs.SetInt(KEY_THEME, 1);
        PlayerPrefs.SetInt(KEY_REDUCE_MOTION, 0);
        PlayerPrefs.SetInt(KEY_HIGH_CONTRAST, 0);

        PlayerPrefs.SetString(KEY_LANGUAGE, "tr");

        PlayerPrefs.SetInt(KEY_DAILY_REMINDER, 0);
        PlayerPrefs.SetInt(KEY_NEW_FEATURES, 1);
        PlayerPrefs.SetInt(KEY_TIPS, 1);

        PlayerPrefs.Save();
        LoadValuesToUI();
    }

    // Confirmation modal implementation (simple)
    GameObject confirmPanel;
    void ShowConfirm(string title, string message, Action onYes)
    {
        if (confirmPanel == null)
        {
            CreateConfirmPanel();
        }
        if (confirmPanel == null)
        {
            if (CanLog) Debug.LogWarning("Confirm panel could not be created. Check SettingsCanvas.");
            return;
        }

        var titleTransform = confirmPanel.transform.Find("Title");
        var msgTransform = confirmPanel.transform.Find("Message");
        var yesTransform = confirmPanel.transform.Find("YesButton");
        var noTransform = confirmPanel.transform.Find("NoButton");

        var titleText = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        var msgText = msgTransform != null ? msgTransform.GetComponent<Text>() : null;
        var yesBtn = yesTransform != null ? yesTransform.GetComponent<Button>() : null;
        var noBtn = noTransform != null ? noTransform.GetComponent<Button>() : null;

        if (titleText == null || msgText == null || yesBtn == null || noBtn == null)
        {
            if (CanLog) Debug.LogWarning("Confirm panel is missing expected UI elements.");
            return;
        }

        titleText.text = title;
        msgText.text = message;

        yesBtn.onClick.RemoveAllListeners();
        noBtn.onClick.RemoveAllListeners();

        yesBtn.onClick.AddListener(() => { onYes?.Invoke(); confirmPanel.SetActive(false); });
        noBtn.onClick.AddListener(() => { confirmPanel.SetActive(false); });

        confirmPanel.SetActive(true);
    }

    void CreateConfirmPanel()
    {
        var canvas = GameObject.Find("SettingsCanvas");
        if (canvas == null)
        {
            if (CanLog) Debug.LogWarning("SettingsCanvas not found. Cannot create confirm panel.");
            return;
        }

        confirmPanel = new GameObject("ConfirmPanel");
        confirmPanel.transform.SetParent(canvas.transform, false);
        var img = confirmPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.8f);
        var rt = confirmPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.3f);
        rt.anchorMax = new Vector2(0.9f, 0.7f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var title = new GameObject("Title"); title.transform.SetParent(confirmPanel.transform, false); var ttext = title.AddComponent<Text>(); ttext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); ttext.alignment = TextAnchor.UpperCenter; ttext.fontSize = 24; ttext.color = Color.white; ttext.rectTransform.anchoredPosition = new Vector2(0, 80);
        var msg = new GameObject("Message"); msg.transform.SetParent(confirmPanel.transform, false); var mtext = msg.AddComponent<Text>(); mtext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); mtext.alignment = TextAnchor.MiddleCenter; mtext.fontSize = 18; mtext.color = Color.white; mtext.rectTransform.anchoredPosition = new Vector2(0, 10);

        var yes = new GameObject("YesButton"); yes.transform.SetParent(confirmPanel.transform, false); var ybtn = yes.AddComponent<Button>(); var yimg = yes.AddComponent<Image>(); yimg.color = new Color(0.2f, 0.6f, 1f); var ytxt = new GameObject("Text"); ytxt.transform.SetParent(yes.transform, false); var ytext = ytxt.AddComponent<Text>(); ytext.text = "Evet"; ytext.alignment = TextAnchor.MiddleCenter; ytext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); ytext.color = Color.white; ybtn.targetGraphic = yimg; ybtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, -60);

        var no = new GameObject("NoButton"); no.transform.SetParent(confirmPanel.transform, false); var nbtn = no.AddComponent<Button>(); var nimg = no.AddComponent<Image>(); nimg.color = new Color(0.6f, 0.6f, 0.6f); var ntxt = new GameObject("Text"); ntxt.transform.SetParent(no.transform, false); var ntext = ntxt.AddComponent<Text>(); ntext.text = "Hayır"; ntext.alignment = TextAnchor.MiddleCenter; ntext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); ntext.color = Color.white; nbtn.targetGraphic = nimg; nbtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -60);

        confirmPanel.SetActive(false);
    }

    void WarnIfMissing(UnityEngine.Object obj, string name)
    {
        if (obj == null && CanLog)
            Debug.LogWarning("SettingsManager missing UI element: " + name);
    }

    // Helper finders
    Toggle FindToggle(string path)
    {
        var go = GameObject.Find(path);
        return go != null ? go.GetComponent<Toggle>() : null;
    }
    Slider FindSlider(string path)
    {
        var go = GameObject.Find(path);
        return go != null ? go.GetComponent<Slider>() : null;
    }
    Button FindButton(string path)
    {
        var go = GameObject.Find(path);
        return go != null ? go.GetComponent<Button>() : null;
    }
    Dropdown FindDropdown(string path)
    {
        var go = GameObject.Find(path);
        return go != null ? go.GetComponent<Dropdown>() : null;
    }
    Text FindText(string path)
    {
        var go = GameObject.Find(path);
        return go != null ? go.GetComponent<Text>() : null;
    }
}
