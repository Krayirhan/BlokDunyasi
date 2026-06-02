using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.Audio;
using BlockPuzzle.UnityAdapter.Privacy;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

public class SettingsManager : MonoBehaviour
{
    private const string KEY_MUSIC_ON = SettingsKeys.MusicOn;
    private const string KEY_MUSIC_VOL = SettingsKeys.MusicVolume;
    private const string KEY_SFX_ON = SettingsKeys.SfxOn;
    private const string KEY_SFX_VOL = SettingsKeys.SfxVolume;
    private const string KEY_VIBRATION = SettingsKeys.Vibration;

    private const string KEY_GRID_HIGHLIGHT = SettingsKeys.GridHighlight;
    private const string KEY_COMBO_VISUAL = SettingsKeys.ComboVisual;
    private const string KEY_ANIMATIONS = SettingsKeys.Animations;
    private const string KEY_AUTOSAVE = SettingsKeys.AutoSave;

    private const string KEY_LANGUAGE = SettingsKeys.Language;
    private const string KEY_DAILY_REMINDER = SettingsKeys.DailyReminder;
    private const string KEY_NEW_FEATURES = SettingsKeys.NewFeatures;
    private const string KEY_TIPS = SettingsKeys.Tips;

    private const string SAVE_DATA_KEY = SettingsKeys.SaveDataDefault;
    private const string BEST_SCORE_KEY = SettingsKeys.BestScore;
    private const string STATISTICS_KEY = SettingsKeys.Statistics;
    private const string DEFAULT_LANGUAGE = "tr";

    [SerializeField] private string mainMenuSceneName = SceneCatalog.MainMenu;
    [SerializeField] private bool verboseLogs = true;

    private Toggle musicToggle;
    private Slider musicSlider;
    private Toggle sfxToggle;
    private Slider sfxSlider;
    private Toggle vibrationToggle;

    private Toggle gridHighlightToggle;
    private Toggle comboVisualToggle;
    private Toggle animationsToggle;
    private Toggle autoSaveToggle;

    private Button themeClassicButton;
    private Button themeNightButton;
    private Button themeVividButton;
    private GameObject themeButtonsRow;
    private Toggle reduceMotionToggle;
    private Toggle highContrastToggle;
    private Toggle accessiblePaletteToggle;

    private Button languageTrButton;
    private Button languageEnButton;
    private Button languageKoButton;

    private Toggle dailyReminderToggle;
    private Toggle newFeaturesToggle;
    private Toggle tipsToggle;

    private Button resetProgressButton;
    private Button clearCacheButton;
    private Button restoreDefaultsButton;

    private Button privacyButton;
    private Button termsButton;
    private Button creditsButton;
    private Button backButton;

    private Image backgroundArtImage;
    private Image backgroundDimImage;
    private Image headerBackingImage;
    private Image titlePlateImage;
    private Image windowShellImage;
    private Image shellAccentImage;
    private TMP_Text screenTitleText;
    private TMP_Text screenSubtitleText;
    private TMP_Text versionText;
    private TMP_Text aboutSummaryText;

    private GameObject confirmDialog;
    private Image dialogDimImage;
    private Image dialogPanelImage;
    private TMP_Text dialogTitleText;
    private TMP_Text dialogMessageText;
    private Button confirmYesButton;
    private Button confirmNoButton;
    private TMP_Text confirmYesLabel;
    private TMP_Text confirmNoLabel;

    private readonly List<Image> cardImages = new List<Image>();
    private readonly List<Image> accentImages = new List<Image>();
    private readonly List<Image> decorImages = new List<Image>();
    private readonly Dictionary<Toggle, Image> toggleTracks = new Dictionary<Toggle, Image>();
    private readonly Dictionary<Toggle, RectTransform> toggleKnobs = new Dictionary<Toggle, RectTransform>();
    private readonly Dictionary<Toggle, TMP_Text> toggleValueTexts = new Dictionary<Toggle, TMP_Text>();
    private readonly Dictionary<Slider, Image> sliderFills = new Dictionary<Slider, Image>();
    private readonly Dictionary<Slider, TMP_Text> sliderValueTexts = new Dictionary<Slider, TMP_Text>();
    private readonly Dictionary<Button, Image> selectionGlows = new Dictionary<Button, Image>();

    private Action pendingPositiveAction;
    private Action pendingNegativeAction;
    private ThemePalette currentPalette;
    private bool listenersBound;

    private bool CanLog => verboseLogs && Debug.isDebugBuild;

    private struct ThemePalette
    {
        public Color BackgroundTint;
        public Color BackgroundDim;
        public Color HeaderPanel;
        public Color Shell;
        public Color ShellAccent;
        public Color Card;
        public Color Accent;
        public Color AccentSecondary;
        public Color Row;
        public Color RowSelected;
        public Color ToggleOn;
        public Color ToggleOff;
        public Color ToggleKnob;
        public Color ValueText;
        public Color Danger;
        public Color Dialog;
        public Color DialogDim;
        public Color SelectionGlow;
    }

    private void Awake()
    {
        ResolveRuntimeReferences();
        BindListeners();
        LoadValuesToUi();
        SyncLanguageManagerOnStartup();
        RefreshAllVisuals();
    }

    /// <summary>
    /// SettingsManager kendi "settings_language" (string) key'ini kullanırken
    /// LanguageManager ayrı bir "SelectedLanguage" (int) key'i kullanır.
    /// Uygulama açıldığında ikisini senkronize eder.
    /// </summary>
    private void SyncLanguageManagerOnStartup()
    {
        // First determine system language fallback if there is no KEY_LANGUAGE in PlayerPrefs
        if (!PlayerPrefs.HasKey(KEY_LANGUAGE))
        {
            string systemLangCode = DEFAULT_LANGUAGE;
            if (Application.systemLanguage == SystemLanguage.Korean)
                systemLangCode = "ko";
            else if (Application.systemLanguage == SystemLanguage.Turkish)
                systemLangCode = "tr";
            else
                systemLangCode = "en";

            PlayerPrefs.SetString(KEY_LANGUAGE, systemLangCode);
            PlayerPrefs.Save();
        }

        string savedLang = PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        var targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.Turkish;
        if (savedLang == "en")
            targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.English;
        else if (savedLang == "ko")
            targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.Korean;

        // Sadece gerçekten farklıysa değiştir (gereksiz listener tetiklemesini önlemek için)
        if (BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Instance.CurrentLanguage != targetLang)
            BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Instance.SetLanguage(targetLang, true);
    }

    private void OnEnable()
    {
        RefreshAllVisuals();
    }

    private void ResolveRuntimeReferences()
    {
        musicToggle = FindComponent<Toggle>("MusicToggle");
        musicSlider = FindComponent<Slider>("MusicSlider");
        sfxToggle = FindComponent<Toggle>("SfxToggle");
        sfxSlider = FindComponent<Slider>("SfxSlider");
        vibrationToggle = FindComponent<Toggle>("VibrationToggle");

        gridHighlightToggle = FindComponent<Toggle>("GridHighlightToggle");
        comboVisualToggle = FindComponent<Toggle>("ComboVisualToggle");
        animationsToggle = FindComponent<Toggle>("AnimationsToggle");
        autoSaveToggle = FindComponent<Toggle>("AutoSaveToggle");

        themeClassicButton = FindComponent<Button>("ThemeClassicButton");
        themeNightButton = FindComponent<Button>("ThemeNightButton");
        themeVividButton = FindComponent<Button>("ThemeVividButton");
        themeButtonsRow = FindGameObject("ThemeButtonsRow");
        reduceMotionToggle = FindComponent<Toggle>("ReduceMotionToggle");
        highContrastToggle = FindComponent<Toggle>("HighContrastToggle");
        accessiblePaletteToggle = FindComponent<Toggle>("AccessiblePaletteToggle");
        if (accessiblePaletteToggle == null)
            accessiblePaletteToggle = CreateAccessibilityPaletteToggle();

        languageTrButton = FindComponent<Button>("LanguageTrButton");
        languageEnButton = FindComponent<Button>("LanguageEnButton");
        languageKoButton = FindComponent<Button>("LanguageKoButton");

        dailyReminderToggle = FindComponent<Toggle>("DailyReminderToggle");
        newFeaturesToggle = FindComponent<Toggle>("NewFeaturesToggle");
        tipsToggle = FindComponent<Toggle>("TipsToggle");

        resetProgressButton = FindComponent<Button>("ResetProgressButton");
        clearCacheButton = FindComponent<Button>("ClearCacheButton");
        restoreDefaultsButton = FindComponent<Button>("RestoreDefaultsButton");

        privacyButton = FindComponent<Button>("PrivacyButton");
        termsButton = FindComponent<Button>("TermsButton");
        creditsButton = FindComponent<Button>("CreditsButton");
        backButton = FindComponent<Button>("BackButton");

        backgroundArtImage = FindComponent<Image>("BackgroundArt");
        backgroundDimImage = FindComponent<Image>("BackgroundDim");
        headerBackingImage = FindComponent<Image>("HeaderBacking");
        titlePlateImage = FindComponent<Image>("TitlePlate");
        windowShellImage = FindComponent<Image>("WindowShell");
        shellAccentImage = FindComponent<Image>("ShellAccentBar");
        screenTitleText = FindText("ScreenTitleText");
        screenSubtitleText = FindText("ScreenSubtitleText");
        versionText = FindText("VersionText");
        aboutSummaryText = FindText("AboutSummaryText");

        confirmDialog = FindGameObject("ConfirmDialog");
        dialogDimImage = FindComponent<Image>("DialogDim");
        dialogPanelImage = FindComponent<Image>("DialogPanel");
        dialogTitleText = FindText("DialogTitleText");
        dialogMessageText = FindText("DialogMessageText");
        confirmYesButton = FindComponent<Button>("ConfirmYesButton");
        confirmNoButton = FindComponent<Button>("ConfirmNoButton");
        confirmYesLabel = confirmYesButton != null ? FindChildText(confirmYesButton.transform, "LabelText") : null;
        confirmNoLabel = confirmNoButton != null ? FindChildText(confirmNoButton.transform, "LabelText") : null;

        RegisterCard("AudioCard");
        RegisterCard("GameplayCard");
        RegisterCard("VisualCard");
        RegisterCard("LanguageCard");
        RegisterCard("NotificationsCard");
        RegisterCard("DataCard");
        RegisterCard("AboutCard");

        RegisterAccent("ShellAccentBar");
        RegisterAccent("AudioCard/AccentBar");
        RegisterAccent("GameplayCard/AccentBar");
        RegisterAccent("VisualCard/AccentBar");
        RegisterAccent("LanguageCard/AccentBar");
        RegisterAccent("NotificationsCard/AccentBar");
        RegisterAccent("DataCard/AccentBar");
        RegisterAccent("AboutCard/AccentBar");

        RegisterDecorImage("DecorBlockLeft");
        RegisterDecorImage("DecorBlockRight");
        RegisterDecorImage("DecorBlockLowerLeft");
        RegisterDecorImage("DecorBlockLowerRight");

        RegisterToggle(musicToggle);
        RegisterToggle(sfxToggle);
        RegisterToggle(vibrationToggle);
        RegisterToggle(gridHighlightToggle);
        RegisterToggle(comboVisualToggle);
        RegisterToggle(animationsToggle);
        RegisterToggle(autoSaveToggle);
        RegisterToggle(reduceMotionToggle);
        RegisterToggle(highContrastToggle);
        RegisterToggle(accessiblePaletteToggle);
        RegisterToggle(dailyReminderToggle);
        RegisterToggle(newFeaturesToggle);
        RegisterToggle(tipsToggle);

        RegisterSlider(musicSlider);
        RegisterSlider(sfxSlider);

        RegisterChoiceButton(themeClassicButton);
        RegisterChoiceButton(themeNightButton);
        RegisterChoiceButton(themeVividButton);
        RegisterChoiceButton(languageTrButton);
        RegisterChoiceButton(languageEnButton);
        RegisterChoiceButton(languageKoButton);
    }

    private void BindListeners()
    {
        if (listenersBound)
            return;

        listenersBound = true;

        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(value => PersistToggle(KEY_VIBRATION, vibrationToggle, value));

        if (gridHighlightToggle != null) gridHighlightToggle.onValueChanged.AddListener(value => PersistToggle(KEY_GRID_HIGHLIGHT, gridHighlightToggle, value));
        if (comboVisualToggle != null) comboVisualToggle.onValueChanged.AddListener(value => PersistToggle(KEY_COMBO_VISUAL, comboVisualToggle, value));
        if (animationsToggle != null) animationsToggle.onValueChanged.AddListener(value => PersistToggle(KEY_ANIMATIONS, animationsToggle, value));
        if (autoSaveToggle != null) autoSaveToggle.onValueChanged.AddListener(value => PersistToggle(KEY_AUTOSAVE, autoSaveToggle, value));

        if (reduceMotionToggle != null) reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
        if (highContrastToggle != null) highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);
        if (accessiblePaletteToggle != null) accessiblePaletteToggle.onValueChanged.AddListener(OnAccessibilityPaletteChanged);

        if (languageTrButton != null) languageTrButton.onClick.AddListener(() => SetLanguage("tr"));
        if (languageEnButton != null) languageEnButton.onClick.AddListener(() => SetLanguage("en"));
        if (languageKoButton != null) languageKoButton.onClick.AddListener(() => SetLanguage("ko"));

        if (dailyReminderToggle != null) dailyReminderToggle.onValueChanged.AddListener(OnNotificationsToggleChanged);
        if (newFeaturesToggle != null) newFeaturesToggle.onValueChanged.AddListener(OnNewFeaturesToggleChanged);
        if (tipsToggle != null) tipsToggle.onValueChanged.AddListener(value => PersistToggle(KEY_TIPS, tipsToggle, value));

        if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgressClicked);
        if (clearCacheButton != null) clearCacheButton.onClick.AddListener(OnClearCacheClicked);
        if (restoreDefaultsButton != null) restoreDefaultsButton.onClick.AddListener(OnRestoreDefaultsClicked);

        if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacyClicked);
        if (termsButton != null) termsButton.onClick.AddListener(OnTermsClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (backButton != null) backButton.onClick.AddListener(ReturnToMainMenu);

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(HandleDialogAccepted);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(HandleDialogDismissed);
    }

    private void LoadValuesToUi()
    {
        SetToggleWithoutNotify(musicToggle, PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1);
        SetSliderWithoutNotify(musicSlider, PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f));
        SetToggleWithoutNotify(sfxToggle, PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1);
        SetSliderWithoutNotify(sfxSlider, PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f));
        SetToggleWithoutNotify(vibrationToggle, PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1);

        SetToggleWithoutNotify(gridHighlightToggle, PlayerPrefs.GetInt(KEY_GRID_HIGHLIGHT, 0) == 1);
        SetToggleWithoutNotify(comboVisualToggle, PlayerPrefs.GetInt(KEY_COMBO_VISUAL, 1) == 1);
        SetToggleWithoutNotify(animationsToggle, PlayerPrefs.GetInt(KEY_ANIMATIONS, 1) == 1);
        SetToggleWithoutNotify(autoSaveToggle, PlayerPrefs.GetInt(KEY_AUTOSAVE, 1) == 1);

        SetToggleWithoutNotify(reduceMotionToggle, UISettingsProfile.IsReduceMotionEnabled());
        SetToggleWithoutNotify(highContrastToggle, UISettingsProfile.IsHighContrastEnabled());
        SetToggleWithoutNotify(accessiblePaletteToggle, UISettingsProfile.IsAccessibilityPaletteEnabled());

        SetToggleWithoutNotify(dailyReminderToggle, PlayerPrefs.GetInt(KEY_DAILY_REMINDER, 1) == 1);
        SetToggleWithoutNotify(newFeaturesToggle, PlayerPrefs.GetInt(KEY_NEW_FEATURES, 1) == 1);
        SetToggleWithoutNotify(tipsToggle, PlayerPrefs.GetInt(KEY_TIPS, 1) == 1);

        if (musicSlider != null)
            musicSlider.interactable = musicToggle == null || musicToggle.isOn;

        if (sfxSlider != null)
            sfxSlider.interactable = sfxToggle == null || sfxToggle.isOn;
    }

    private void RefreshAllVisuals()
    {
        currentPalette = ResolvePalette(UISettingsProfile.GetThemeId(), UISettingsProfile.IsHighContrastEnabled());
        ApplyThemePreview();
        RefreshLocalizedTexts();
        RefreshThemeControlsVisibility();
        RefreshLanguageSelection();
        RefreshToggleVisuals();
        RefreshSliderVisuals();
        RefreshDialogVisuals();
    }

    private void RefreshLocalizedTexts()
    {
        SetText(screenTitleText, T("AYARLAR", "SETTINGS", "설정"));
        SetText(screenSubtitleText, T("Oyunu kendine gore ayarla", "Tune the game your way", "원하는 대로 설정을 조정하세요"));
        SetText(versionText, string.Format(T("Surum {0}", "Version {0}", "버전 {0}"), string.IsNullOrWhiteSpace(Application.version) ? "1.0.0" : Application.version));
        SetText(
            aboutSummaryText,
            T(
                "Blok Dunyasi hizli karar, temiz geri bildirim ve canli renkler uzerine kurulu casual bir bulmaca.",
                "Blok Dunyasi is a casual puzzle built around fast decisions, clear feedback and vivid color.",
                "Blok Dunyasi는 빠른 결정, 명확한 피드백, 생생한 색상으로 구성된 캐주얼 퍼즐 게임입니다."));

        SetCardText("AudioCard", T("Ses", "Audio", "오디오"), T("Muzik ve efekt dengesini ayarla", "Tune music and effect balance", "음악 및 효과음 균형 조정"));
        SetCardText("GameplayCard", T("Oynanis", "Gameplay", "게임플레이"), T("Tahta yardimcilari ve konfor secenekleri", "Board helpers and comfort options", "보드 도우미 및 편의 옵션"));
        SetCardText("VisualCard", T("Gorunum", "Visual", "비주얼"), T("Hareket, kontrast ve erisilebilirlik ayarlari", "Motion, contrast and accessibility settings", "모션, 대비 및 접근성 설정"));
        SetCardText("LanguageCard", T("Dil", "Language", "언어"), T("Arayuz dilini sec", "Choose the interface language", "인터페이스 언어 선택"));
        SetCardText("NotificationsCard", T("Bildirimler", "Notifications", "알림"), T("Geri donus bildirimlerini kontrol et", "Control reminder notifications", "알림 메시지 제어"));
        SetCardText("DataCard", T("Veri ve sifirlama", "Data and reset", "데이터 및 초기화"), T("Kayit ve onbellek islemleri", "Saved data and cache actions", "저장 데이터 및 캐시 작업"));
        SetCardText("AboutCard", T("Hakkinda", "About", "정보"), T("Proje ve politika bilgileri", "Project and policy details", "프로젝트 및 정책 정보"));

        SetToggleTexts(musicToggle, T("Muzik", "Music", "배경음악"), T("Menu ve oyun muziklerini acar", "Enables menu and in-game music", "메뉴 및 게임 배경음악 활성화"));
        SetSliderTexts(musicSlider, T("Muzik seviyesi", "Music level", "배경음악 음량"), T("Arka plan ses gucu", "Background volume", "배경음악 볼륨"));
        SetToggleTexts(sfxToggle, T("Efekt sesleri", "Sound effects", "효과음"), T("Buton ve oyun tepkilerini calar", "Plays button and gameplay cues", "버튼 및 게임 효과음 재생"));
        SetSliderTexts(sfxSlider, T("Efekt seviyesi", "Effects level", "효과음 음량"), T("Anlik geri bildirim gucu", "Reactive sound intensity", "효과음 강도"));
        SetToggleTexts(vibrationToggle, T("Titresim", "Vibration", "진동"), T("Kisa fiziksel geri bildirim verir", "Adds haptic feedback when supported", "지원되는 경우 햅틱 피드백 추가"));

        SetToggleTexts(gridHighlightToggle, T("Izgara vurgusu", "Grid highlight", "그리드 강조"), T("Bos ve dolu alanlari daha net ayirir", "Separates empty and filled cells more clearly", "빈 칸과 채워진 칸을 더 명확하게 구분"));
        SetToggleTexts(comboVisualToggle, T("Kombo efektleri", "Combo effects", "콤보 효과"), T("Gorsel kombo kutlamalarini acar", "Enables visual combo celebrations", "시각적 콤보 축하 효과 활성화"));
        SetToggleTexts(animationsToggle, T("Oyun animasyonlari", "Game animations", "게임 애니메이션"), T("Hareketli gecis ve vurgu animasyonlari", "Animated transitions and emphasis", "애니메이션 효과 및 강조 표시"));
        SetToggleTexts(autoSaveToggle, T("Otomatik kayit", "Auto save", "자동 저장"), T("Oyunu sahneden cikarken saklar", "Keeps progress when leaving the scene", "화면을 나갈 때 진행 상황 저장"));

        SetToggleTexts(reduceMotionToggle, T("Hareketi azalt", "Reduce motion", "동작 줄이기"), T("Puls ve glow hareketlerini azaltir", "Reduces pulse and glow motion", "펄스 및 글로우 동작 감소"));
        SetToggleTexts(highContrastToggle, T("Yuksek kontrast", "High contrast", "고대비"), T("Okunurlugu guclendirir", "Boosts readability", "가독성 향상"));
        SetToggleTexts(accessiblePaletteToggle, T("Erisilebilir palet", "Accessible palette", "접근성 색상표"), T("Renk korlugune daha dayanikli blok renkleri", "Uses a colorblind-safer block palette", "색약자를 위한 안전한 블록 색상표 사용"));

        SetChoiceButtonTexts(languageTrButton, "Türkçe", T("Varsayilan deneyim", "Default experience", "기본 설정"));
        SetChoiceButtonTexts(languageEnButton, "English", T("Ingilizce arayuz", "English interface", "영어 인터페이스"));
        SetChoiceButtonTexts(languageKoButton, "한국어", T("Korece arayuz", "Korean interface", "한국어 인터페이스"));

        SetToggleTexts(dailyReminderToggle, T("Gunluk hatirlatma", "Daily reminder", "일일 알림"), T("Her gun geri gelmen icin bir bildirim", "Sends one reminder each day", "매일 접속 유도를 위한 알림 전송"));
        SetToggleTexts(newFeaturesToggle, T("Yeni ozellik duyurulari", "Feature updates", "새로운 기능 알림"), T("Buyuk degisikliklerde haber verir", "Alerts you when major updates arrive", "주요 업데이트 발생 시 알림"));
        SetToggleTexts(tipsToggle, T("Ipucu onerileri", "Tips", "팁"), T("Kisa oynanis fikirleri gosterir", "Shows short gameplay tips", "간단한 게임플레이 팁 표시"));

        SetChoiceButtonTexts(backButton, T("GERI", "BACK", "뒤로 가기"), T("Ana menuye don", "Return to main menu", "메인 메뉴로 돌아가기"));
        SetChoiceButtonTexts(resetProgressButton, T("ILERLEMEYI SIFIRLA", "RESET PROGRESS", "진행 상황 초기화"), T("Kayit ve rekorlari temizler", "Clears save and best score", "저장 데이터 및 최고 점수 삭제"));
        SetChoiceButtonTexts(clearCacheButton, T("ONBELLEGI TEMIZLE", "CLEAR CACHE", "캐시 지우기"), T("Gecici verileri bosaltir", "Flushes temporary cache", "임시 캐시 파일 정리"));
        SetChoiceButtonTexts(restoreDefaultsButton, T("VARSAYILANLARI YUKLE", "RESTORE DEFAULTS", "기본값 복원"), T("Tum ayarlari bastan kurar", "Resets all settings to defaults", "모든 설정을 초기 상태로 재설정"));

        SetChoiceButtonTexts(privacyButton, T("GIZLILIK", "PRIVACY", "개인정보 보호정책"), T("Veri kullanim notu", "Data usage note", "데이터 사용 지침"));
        SetChoiceButtonTexts(termsButton, T("KOSULLAR", "TERMS", "이용 약관"), T("Kullanim ozeti", "Usage summary", "이용 약관 요약"));
        SetChoiceButtonTexts(creditsButton, T("EKIP", "CREDITS", "크레딧"), T("Katkilar ve assetler", "Contributors and assets", "기여자 및 에셋 정보"));
    }

    private void ApplyThemePreview()
    {
        ApplyImageColor(backgroundArtImage, currentPalette.BackgroundTint);
        ApplyImageColor(backgroundDimImage, currentPalette.BackgroundDim);
        ApplyImageColor(headerBackingImage, currentPalette.HeaderPanel);
        ApplyImageColor(titlePlateImage, currentPalette.Accent);
        ApplyImageColor(windowShellImage, currentPalette.Shell);
        ApplyImageColor(shellAccentImage, currentPalette.ShellAccent);

        for (int i = 0; i < cardImages.Count; i++)
            ApplyImageColor(cardImages[i], currentPalette.Card);

        for (int i = 0; i < accentImages.Count; i++)
        {
            Color accent = (i % 2 == 0) ? currentPalette.Accent : currentPalette.AccentSecondary;
            ApplyImageColor(accentImages[i], accent);
        }

        for (int i = 0; i < decorImages.Count; i++)
        {
            Color color = (i % 2 == 0) ? currentPalette.Accent : currentPalette.AccentSecondary;
            color.a = 0.88f;
            ApplyImageColor(decorImages[i], color);
        }
    }

    private void RefreshThemeControlsVisibility()
    {
        if (themeButtonsRow != null)
            themeButtonsRow.SetActive(false);

        if (themeClassicButton != null)
            themeClassicButton.gameObject.SetActive(false);

        if (themeNightButton != null)
            themeNightButton.gameObject.SetActive(false);

        if (themeVividButton != null)
            themeVividButton.gameObject.SetActive(false);
    }

    private void RefreshLanguageSelection()
    {
        string savedLang = PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        ApplyChoiceButtonSelection(languageTrButton, savedLang == "tr");
        ApplyChoiceButtonSelection(languageEnButton, savedLang == "en");
        ApplyChoiceButtonSelection(languageKoButton, savedLang == "ko");
    }

    private void RefreshToggleVisuals()
    {
        foreach (KeyValuePair<Toggle, Image> entry in toggleTracks)
            RefreshToggleVisual(entry.Key);
    }

    private void RefreshToggleVisual(Toggle toggle)
    {
        if (toggle == null)
            return;

        Image rowImage = toggle.GetComponent<Image>();
        if (rowImage != null)
            rowImage.color = toggle.isOn ? currentPalette.RowSelected : currentPalette.Row;

        Image track;
        if (toggleTracks.TryGetValue(toggle, out track) && track != null)
            track.color = toggle.isOn ? currentPalette.ToggleOn : currentPalette.ToggleOff;

        RectTransform knob;
        if (toggleKnobs.TryGetValue(toggle, out knob) && knob != null)
        {
            knob.anchoredPosition = new Vector2(toggle.isOn ? 54f : -54f, 0f);
            knob.localScale = toggle.isOn ? Vector3.one : new Vector3(0.94f, 0.94f, 1f);
        }

        TMP_Text valueText;
        if (toggleValueTexts.TryGetValue(toggle, out valueText) && valueText != null)
        {
            valueText.text = toggle.isOn ? T("ACIK", "ON") : T("KAPALI", "OFF");
            valueText.color = currentPalette.ValueText;
        }
    }

    private void RefreshSliderVisuals()
    {
        foreach (KeyValuePair<Slider, Image> entry in sliderFills)
            RefreshSliderVisual(entry.Key);
    }

    private void RefreshSliderVisual(Slider slider)
    {
        if (slider == null)
            return;

        Image fillImage;
        if (sliderFills.TryGetValue(slider, out fillImage) && fillImage != null)
            fillImage.color = slider.interactable ? currentPalette.AccentSecondary : new Color(currentPalette.AccentSecondary.r, currentPalette.AccentSecondary.g, currentPalette.AccentSecondary.b, 0.32f);

        TMP_Text valueText;
        if (sliderValueTexts.TryGetValue(slider, out valueText) && valueText != null)
        {
            int percent = Mathf.RoundToInt(slider.value * 100f);
            valueText.text = percent.ToString() + "%";
            valueText.color = currentPalette.ValueText;
        }

        Transform row = slider.transform.parent;
        if (row != null)
        {
            Image rowImage = row.GetComponent<Image>();
            if (rowImage != null)
                rowImage.color = slider.interactable ? currentPalette.Row : new Color(currentPalette.Row.r, currentPalette.Row.g, currentPalette.Row.b, 0.55f);
        }
    }

    private void RefreshDialogVisuals()
    {
        ApplyImageColor(dialogDimImage, currentPalette.DialogDim);
        ApplyImageColor(dialogPanelImage, currentPalette.Dialog);
    }

    private void OnMusicToggleChanged(bool value)
    {
        PersistToggle(KEY_MUSIC_ON, musicToggle, value);
        if (musicSlider != null)
            musicSlider.interactable = value;

        RefreshSliderVisual(musicSlider);
        NotifyAudioSettingsChanged();
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, value);
        PlayerPrefs.Save();
        AppAnalytics.TrackSettingsChanged(KEY_MUSIC_VOL, value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        RefreshSliderVisual(musicSlider);
        NotifyAudioSettingsChanged();
    }

    private void OnSfxToggleChanged(bool value)
    {
        PersistToggle(KEY_SFX_ON, sfxToggle, value);
        if (sfxSlider != null)
            sfxSlider.interactable = value;

        RefreshSliderVisual(sfxSlider);
        NotifyAudioSettingsChanged();
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOL, value);
        PlayerPrefs.Save();
        AppAnalytics.TrackSettingsChanged(KEY_SFX_VOL, value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        RefreshSliderVisual(sfxSlider);
        NotifyAudioSettingsChanged();
    }

    private void OnReduceMotionChanged(bool value)
    {
        UISettingsProfile.SetReduceMotion(value);
        RefreshToggleVisual(reduceMotionToggle);
    }

    private void OnHighContrastChanged(bool value)
    {
        UISettingsProfile.SetHighContrast(value);
        RefreshAllVisuals();
    }

    private void OnAccessibilityPaletteChanged(bool value)
    {
        UISettingsProfile.SetAccessibilityPalette(value);
        AppAnalytics.TrackSettingsChanged(UISettingsProfile.AccessibilityPaletteKey, value ? "1" : "0");
        RefreshToggleVisual(accessiblePaletteToggle);
        RefreshAllVisuals();
    }

    private void OnNotificationsToggleChanged(bool value)
    {
        PersistToggle(KEY_DAILY_REMINDER, dailyReminderToggle, value);
        RefreshNotifications();
    }

    private void OnNewFeaturesToggleChanged(bool value)
    {
        PersistToggle(KEY_NEW_FEATURES, newFeaturesToggle, value);
        RefreshNotifications();
    }

    private void PersistToggle(string key, Toggle toggle, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);

        PlayerPrefs.Save();
        AppAnalytics.TrackSettingsChanged(key, value ? "1" : "0");
        RefreshToggleVisual(toggle);
    }

    private void SetLanguage(string languageCode)
    {
        string normalized = DEFAULT_LANGUAGE;
        if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            normalized = "en";
        else if (string.Equals(languageCode, "ko", StringComparison.OrdinalIgnoreCase))
            normalized = "ko";

        PlayerPrefs.SetString(KEY_LANGUAGE, normalized);
        PlayerPrefs.Save();
        AppAnalytics.TrackSettingsChanged(KEY_LANGUAGE, normalized);
        
        // LanguageManager'a (tüm oyun içi çevirilere) bildir
        var targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.Turkish;
        if (normalized == "en")
            targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.English;
        else if (normalized == "ko")
            targetLang = BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.Korean;

        BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Instance.SetLanguage(targetLang, true);

        RefreshAllVisuals();
    }

    private void OnResetProgressClicked()
    {
        ShowDialog(
            T("Ilerlemeyi sifirla", "Reset progress"),
            T("Kayitli oyunu, rekoru ve istatistikleri silmek istiyor musun?", "Do you want to delete the saved game, best score and statistics?"),
            T("SIFIRLA", "RESET"),
            T("VAZGEC", "CANCEL"),
            ResetProgress,
            null);
    }

    private void OnClearCacheClicked()
    {
        ShowDialog(
            T("Onbellek temizle", "Clear cache"),
            T("Gecici onbellek temizlenecek. Devam etmek istiyor musun?", "Temporary cache will be cleared. Continue?"),
            T("TEMIZLE", "CLEAR"),
            T("VAZGEC", "CANCEL"),
            ClearCache,
            null);
    }

    private void OnRestoreDefaultsClicked()
    {
        ShowDialog(
            T("Varsayilanlari yukle", "Restore defaults"),
            T("Tum tercihleri varsayilan durumuna dondurmek istiyor musun?", "Do you want to restore all preferences to their defaults?"),
            T("YUKLE", "RESTORE"),
            T("VAZGEC", "CANCEL"),
            RestoreDefaults,
            null);
    }

    private void OnPrivacyClicked()
    {
        OpenLegalLink(
            isPrivacy: true,
            missingMessageTr: "Privacy policy URL tanimli degil. Bu build release icin blocker durumundadir.",
            missingMessageEn: "Privacy policy URL is not configured. This build remains blocked for release.");
    }

    private void OnTermsClicked()
    {
        OpenLegalLink(
            isPrivacy: false,
            missingMessageTr: "Terms URL tanimli degil. Bu build release icin blocker durumundadir.",
            missingMessageEn: "Terms URL is not configured. This build remains blocked for release.");
    }

    private void OpenLegalLink(bool isPrivacy, string missingMessageTr, string missingMessageEn)
    {
        var config = Resources.Load<LegalLinksConfig>(LegalLinksConfig.ResourcesPath);
        string url = isPrivacy
            ? config?.PrivacyPolicyUrl
            : config?.TermsOfServiceUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning(isPrivacy
                ? "[SettingsManager] Privacy policy URL is missing. Release blocker remains open."
                : "[SettingsManager] Terms URL is missing. Release blocker remains open.");

            ShowDialog(
                T(isPrivacy ? "Gizlilik" : "Kullanim kosullari", isPrivacy ? "Privacy" : "Terms"),
                T(missingMessageTr, missingMessageEn),
                T("TAMAM", "OK"),
                string.Empty,
                null,
                null);
            return;
        }

        Application.OpenURL(url);
    }

    private void OnCreditsClicked()
    {
        ShowDialog(
            T("Katkilar", "Credits"),
            T("Bu ekran mevcut blok, arka plan, buton sprite'lari ve TMP font ailesi kullanilarak yeniden tasarlandi.", "This screen was redesigned using the existing block art, background assets, button sprites and TMP font family."),
            T("TAMAM", "OK"),
            string.Empty,
            null,
            null);
    }

    private void ResetProgress()
    {
        PlayerPrefs.DeleteKey(SAVE_DATA_KEY);
        PlayerPrefs.DeleteKey(BEST_SCORE_KEY);
        PlayerPrefs.DeleteKey(STATISTICS_KEY);
        PlayerPrefs.Save();
        UISettingsProfile.SetThemeId(UISettingsProfile.ThemeClassic);
        UISettingsProfile.SetLastAutomaticThemeId(-1);

        if (CanLog)
            Debug.Log("[SettingsManager] Progress reset.");

        ShowDialog(
            T("Tamamlandi", "Done"),
            T("Kayitli ilerleme temizlendi.", "Saved progress was cleared."),
            T("TAMAM", "OK"),
            string.Empty,
            null,
            null);
    }

    private void ClearCache()
    {
        bool cleared = false;
        try
        {
            cleared = Caching.ClearCache();
        }
        catch (Exception ex)
        {
            if (CanLog)
                Debug.LogWarning("[SettingsManager] Cache clear failed: " + ex.Message);
        }

        ShowDialog(
            T("Onbellek", "Cache"),
            cleared ? T("Gecici onbellek temizlendi.", "Temporary cache cleared.") : T("Temizlenecek ayri bir onbellek bulunamadi.", "There was no separate cache to clear."),
            T("TAMAM", "OK"),
            string.Empty,
            null,
            null);
    }

    private void RestoreDefaults()
    {
        PlayerPrefs.SetInt(KEY_MUSIC_ON, 1);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, 1f);
        PlayerPrefs.SetInt(KEY_SFX_ON, 1);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, 1f);
        PlayerPrefs.SetInt(KEY_VIBRATION, 1);

        PlayerPrefs.SetInt(KEY_GRID_HIGHLIGHT, 0);
        PlayerPrefs.SetInt(KEY_COMBO_VISUAL, 1);
        PlayerPrefs.SetInt(KEY_ANIMATIONS, 1);
        PlayerPrefs.SetInt(KEY_AUTOSAVE, 1);

        UISettingsProfile.SetThemeId(UISettingsProfile.ThemeClassic);
        UISettingsProfile.SetLastAutomaticThemeId(-1);
        UISettingsProfile.SetReduceMotion(false);
        UISettingsProfile.SetHighContrast(false);
        UISettingsProfile.SetAccessibilityPalette(false);

        PlayerPrefs.SetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Instance.SetLanguage(BlockPuzzle.UnityAdapter.UI.Localization.LanguageManager.Language.Turkish, true);
        
        PlayerPrefs.SetInt(KEY_DAILY_REMINDER, 1);
        PlayerPrefs.SetInt(KEY_NEW_FEATURES, 1);
        PlayerPrefs.SetInt(KEY_TIPS, 1);
        PlayerPrefs.Save();

        LoadValuesToUi();
        NotifyAudioSettingsChanged();
        RefreshNotifications();
        RefreshAllVisuals();

        ShowDialog(
            T("Varsayilanlar yuklendi", "Defaults restored"),
            T("Tum ayarlar baslangic durumuna getirildi.", "All settings were restored to their starting values."),
            T("TAMAM", "OK"),
            string.Empty,
            null,
            null);
    }

    private void ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError("[SettingsManager] Scene cannot be loaded: " + mainMenuSceneName);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowDialog(string title, string message, string positiveText, string negativeText, Action onPositive, Action onNegative)
    {
        if (confirmDialog == null)
        {
            if (CanLog)
                Debug.LogWarning("[SettingsManager] Confirm dialog is missing.");
            return;
        }

        pendingPositiveAction = onPositive;
        pendingNegativeAction = onNegative;

        SetText(dialogTitleText, title);
        SetText(dialogMessageText, message);
        SetText(confirmYesLabel, string.IsNullOrEmpty(positiveText) ? T("TAMAM", "OK") : positiveText);
        SetText(confirmNoLabel, string.IsNullOrEmpty(negativeText) ? T("VAZGEC", "CANCEL") : negativeText);

        bool showNegative = !string.IsNullOrEmpty(negativeText);
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(showNegative);

        confirmDialog.SetActive(true);
        RefreshDialogVisuals();
    }

    private void HandleDialogAccepted()
    {
        Action action = pendingPositiveAction;
        CloseDialog();
        action?.Invoke();
    }

    private void HandleDialogDismissed()
    {
        Action action = pendingNegativeAction;
        CloseDialog();
        action?.Invoke();
    }

    private void CloseDialog()
    {
        pendingPositiveAction = null;
        pendingNegativeAction = null;

        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }

    private void RefreshNotifications()
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.RefreshNotifications();
    }

    private static void NotifyAudioSettingsChanged()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.RefreshFromPrefs();
    }

    private bool IsTurkish()
    {
        return string.Equals(PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE), DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase);
    }

    private string T(string tr, string en)
    {
        return T(tr, en, "");
    }

    private string T(string tr, string en, string ko)
    {
        string savedLang = PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        if (savedLang == "ko")
            return string.IsNullOrEmpty(ko) ? en : ko;
        else if (savedLang == "en")
            return en;
        else
            return tr;
    }

    private static void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(value);
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private void RegisterCard(string name)
    {
        Image image = FindComponent<Image>(name);
        if (image != null && !cardImages.Contains(image))
            cardImages.Add(image);
    }

    private void RegisterAccent(string pathOrName)
    {
        Transform target = pathOrName.Contains("/") ? FindPath(pathOrName) : FindTransform(pathOrName);
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null && !accentImages.Contains(image))
            accentImages.Add(image);
    }

    private void RegisterDecorImage(string name)
    {
        Image image = FindComponent<Image>(name);
        if (image != null && !decorImages.Contains(image))
            decorImages.Add(image);
    }

    private void RegisterToggle(Toggle toggle)
    {
        if (toggle == null)
            return;

        Image track = FindChildImage(toggle.transform, "Track");
        if (track != null)
            toggleTracks[toggle] = track;

        RectTransform knob = FindChildRect(toggle.transform, "Knob");
        if (knob != null)
            toggleKnobs[toggle] = knob;

        TMP_Text valueText = FindChildText(toggle.transform, "ValueText");
        if (valueText != null)
            toggleValueTexts[toggle] = valueText;
    }

    private Toggle CreateAccessibilityPaletteToggle()
    {
        if (highContrastToggle == null)
            return null;

        GameObject clone = Instantiate(highContrastToggle.gameObject, highContrastToggle.transform.parent);
        clone.name = "AccessiblePaletteToggle";
        clone.transform.SetSiblingIndex(highContrastToggle.transform.GetSiblingIndex() + 1);

        var toggle = clone.GetComponent<Toggle>();
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(UISettingsProfile.IsAccessibilityPaletteEnabled());

        return toggle;
    }

    private void RegisterSlider(Slider slider)
    {
        if (slider == null)
            return;

        Image fill = FindChildImage(slider.transform, "Fill");
        if (fill != null)
            sliderFills[slider] = fill;

        TMP_Text valueText = slider.transform.parent != null ? FindChildText(slider.transform.parent, "ValueText") : null;
        if (valueText != null)
            sliderValueTexts[slider] = valueText;
    }

    private void RegisterChoiceButton(Button button)
    {
        if (button == null)
            return;

        Image glow = FindChildImage(button.transform, "SelectionGlow");
        if (glow != null)
            selectionGlows[button] = glow;
    }

    private void ApplyChoiceButtonSelection(Button button, bool selected)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.92f);

        button.transform.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;

        Image glow;
        if (selectionGlows.TryGetValue(button, out glow) && glow != null)
        {
            glow.gameObject.SetActive(true);
            glow.color = selected ? currentPalette.SelectionGlow : new Color(0f, 0f, 0f, 0f);
        }

        Shadow shadow = button.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = selected ? new Color(currentPalette.SelectionGlow.r, currentPalette.SelectionGlow.g, currentPalette.SelectionGlow.b, 0.38f) : new Color(0f, 0f, 0f, 0.2f);
            shadow.effectDistance = selected ? new Vector2(0f, -10f) : new Vector2(0f, -6f);
        }
    }

    private void SetCardText(string cardName, string title, string subtitle)
    {
        Transform card = FindTransform(cardName);
        if (card == null)
            return;

        SetText(FindChildText(card, "CardTitleText"), title);
        SetText(FindChildText(card, "CardSubtitleText"), subtitle);
    }

    private void SetToggleTexts(Toggle toggle, string title, string subtitle)
    {
        if (toggle == null)
            return;

        SetText(FindChildText(toggle.transform, "TitleText"), title);
        SetText(FindChildText(toggle.transform, "SubtitleText"), subtitle);
    }

    private void SetSliderTexts(Slider slider, string title, string subtitle)
    {
        if (slider == null || slider.transform.parent == null)
            return;

        Transform row = slider.transform.parent;
        SetText(FindChildText(row, "TitleText"), title);
        SetText(FindChildText(row, "SubtitleText"), subtitle);
    }

    private void SetChoiceButtonTexts(Button button, string label, string subtitle)
    {
        if (button == null)
            return;

        SetText(FindChildText(button.transform, "LabelText"), label);
        SetText(FindChildText(button.transform, "SubtitleText"), subtitle);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void ApplyImageColor(Image image, Color color)
    {
        if (image != null)
            image.color = color;
    }

    private ThemePalette ResolvePalette(int themeId, bool highContrast)
    {
        ThemePalette palette = new ThemePalette();

        switch (themeId)
        {
            case UISettingsProfile.ThemeClassic:
                palette.BackgroundTint = new Color(1f, 0.98f, 0.95f, 1f);
                palette.BackgroundDim = new Color(0.02f, 0.05f, 0.12f, 0.58f);
                palette.HeaderPanel = new Color(0.04f, 0.08f, 0.2f, 0.62f);
                palette.Shell = new Color(0.05f, 0.11f, 0.26f, 0.86f);
                palette.ShellAccent = new Color(0.15f, 0.82f, 1f, 0.92f);
                palette.Card = new Color(0.07f, 0.15f, 0.35f, 0.86f);
                palette.Accent = new Color(1f, 0.74f, 0.16f, 1f);
                palette.AccentSecondary = new Color(0.15f, 0.78f, 1f, 1f);
                palette.Row = new Color(1f, 1f, 1f, 0.1f);
                palette.RowSelected = new Color(1f, 0.8f, 0.22f, 0.18f);
                palette.ToggleOn = new Color(1f, 0.74f, 0.16f, 1f);
                palette.ToggleOff = new Color(0.18f, 0.28f, 0.46f, 1f);
                palette.ToggleKnob = Color.white;
                palette.ValueText = new Color(1f, 0.98f, 0.9f, 1f);
                palette.Danger = new Color(1f, 0.36f, 0.24f, 1f);
                palette.Dialog = new Color(0.07f, 0.12f, 0.28f, 0.98f);
                palette.DialogDim = new Color(0f, 0f, 0f, 0.76f);
                palette.SelectionGlow = new Color(1f, 0.78f, 0.2f, 0.34f);
                break;

            case UISettingsProfile.ThemeVivid:
                palette.BackgroundTint = new Color(1f, 0.96f, 0.98f, 1f);
                palette.BackgroundDim = new Color(0.06f, 0.04f, 0.14f, 0.62f);
                palette.HeaderPanel = new Color(0.11f, 0.06f, 0.24f, 0.62f);
                palette.Shell = new Color(0.12f, 0.08f, 0.28f, 0.86f);
                palette.ShellAccent = new Color(0.26f, 0.95f, 0.62f, 0.94f);
                palette.Card = new Color(0.15f, 0.1f, 0.34f, 0.86f);
                palette.Accent = new Color(1f, 0.46f, 0.16f, 1f);
                palette.AccentSecondary = new Color(0.27f, 0.95f, 0.58f, 1f);
                palette.Row = new Color(1f, 1f, 1f, 0.11f);
                palette.RowSelected = new Color(1f, 0.54f, 0.16f, 0.2f);
                palette.ToggleOn = new Color(0.27f, 0.95f, 0.58f, 1f);
                palette.ToggleOff = new Color(0.29f, 0.18f, 0.48f, 1f);
                palette.ToggleKnob = Color.white;
                palette.ValueText = new Color(1f, 0.98f, 0.92f, 1f);
                palette.Danger = new Color(1f, 0.34f, 0.27f, 1f);
                palette.Dialog = new Color(0.13f, 0.08f, 0.3f, 0.98f);
                palette.DialogDim = new Color(0f, 0f, 0f, 0.78f);
                palette.SelectionGlow = new Color(1f, 0.46f, 0.16f, 0.34f);
                break;

            default:
                palette.BackgroundTint = new Color(0.96f, 0.99f, 1f, 1f);
                palette.BackgroundDim = new Color(0.01f, 0.05f, 0.14f, 0.66f);
                palette.HeaderPanel = new Color(0.03f, 0.08f, 0.22f, 0.68f);
                palette.Shell = new Color(0.04f, 0.1f, 0.24f, 0.88f);
                palette.ShellAccent = new Color(0.14f, 0.68f, 1f, 0.96f);
                palette.Card = new Color(0.06f, 0.13f, 0.3f, 0.88f);
                palette.Accent = new Color(0.18f, 0.72f, 1f, 1f);
                palette.AccentSecondary = new Color(0.99f, 0.76f, 0.16f, 1f);
                palette.Row = new Color(1f, 1f, 1f, 0.1f);
                palette.RowSelected = new Color(0.2f, 0.72f, 1f, 0.22f);
                palette.ToggleOn = new Color(0.2f, 0.72f, 1f, 1f);
                palette.ToggleOff = new Color(0.14f, 0.24f, 0.42f, 1f);
                palette.ToggleKnob = Color.white;
                palette.ValueText = new Color(0.95f, 0.98f, 1f, 1f);
                palette.Danger = new Color(0.97f, 0.32f, 0.28f, 1f);
                palette.Dialog = new Color(0.05f, 0.11f, 0.26f, 0.98f);
                palette.DialogDim = new Color(0f, 0f, 0f, 0.78f);
                palette.SelectionGlow = new Color(0.18f, 0.72f, 1f, 0.32f);
                break;
        }

        if (highContrast)
        {
            palette.BackgroundDim.a = Mathf.Clamp01(palette.BackgroundDim.a + 0.08f);
            palette.HeaderPanel.a = Mathf.Clamp01(palette.HeaderPanel.a + 0.1f);
            palette.Shell.a = Mathf.Clamp01(palette.Shell.a + 0.08f);
            palette.Card.a = Mathf.Clamp01(palette.Card.a + 0.08f);
            palette.Row = new Color(1f, 1f, 1f, 0.16f);
            palette.RowSelected = new Color(palette.RowSelected.r, palette.RowSelected.g, palette.RowSelected.b, Mathf.Clamp01(palette.RowSelected.a + 0.08f));
            palette.SelectionGlow.a = Mathf.Clamp01(palette.SelectionGlow.a + 0.08f);
        }

        return palette;
    }

    private GameObject FindGameObject(string objectName)
    {
        Transform target = FindTransform(objectName);
        return target != null ? target.gameObject : null;
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        Transform target = FindTransform(objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private TMP_Text FindText(string objectName)
    {
        return FindComponent<TMP_Text>(objectName);
    }

    private Transform FindTransform(string objectName)
    {
        return FindTransformRecursive(transform, objectName);
    }

    private Transform FindPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] parts = path.Split('/');
        Transform current = transform;
        for (int i = 0; i < parts.Length; i++)
        {
            current = FindTransformRecursive(current, parts[i]);
            if (current == null)
                return null;
        }

        return current;
    }

    private static Transform FindTransformRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (string.Equals(root.name, objectName, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindTransformRecursive(root.GetChild(i), objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    private static TMP_Text FindChildText(Transform root, string childName)
    {
        Transform child = FindTransformRecursive(root, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Image FindChildImage(Transform root, string childName)
    {
        Transform child = FindTransformRecursive(root, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static RectTransform FindChildRect(Transform root, string childName)
    {
        Transform child = FindTransformRecursive(root, childName);
        return child as RectTransform;
    }
}
