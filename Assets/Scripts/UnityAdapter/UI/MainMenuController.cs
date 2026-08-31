using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.UI.Localization;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using Debug = BlockPuzzle.Core.Common.GameLogger;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    public partial class MainMenuController : MonoBehaviour, ILanguageListener
    {
        private const string SaveKey = "default";
        private const long ShortToggleVibrationMs = 70L;
        private const long LongToggleVibrationMs = 160L;
        private static MainMenuController _activeInstance;

        [Header("Continue")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button challengeModeButton;
        [SerializeField] private Button zenModeButton;

        [Header("Vibration Toggle")]
        [SerializeField] private Button vibrationToggleButton;
        [SerializeField] private Graphic vibrationToggleBackground;
        [SerializeField] private Graphic vibrationOnIcon;
        [SerializeField] private Graphic vibrationOffIcon;
        [SerializeField] private bool playToggleVibrationFeedback = true;
        [SerializeField] private Color vibrationEnabledBackgroundColor = new Color(0.13333334f, 0.6039216f, 0.90588236f, 0.78f);
        [SerializeField] private Color vibrationDisabledBackgroundColor = new Color(0.06666667f, 0.13333334f, 0.31764707f, 0.72f);

        [Header("Scene Names (Build Settings'te ekli olmali)")]
        [SerializeField] private string gameSceneName = SceneCatalog.Game;
        [SerializeField] private string mainMenuSceneName = SceneCatalog.MainMenu;
        [SerializeField] private string scoresSceneName = SceneCatalog.Scores;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Header("Language Visual Sprites (Drag & Drop)")]
        [SerializeField] private bool enableLanguageSpriteSwap = true;
        [SerializeField] private Image playButtonImage;
        [SerializeField] private Sprite playButtonTurkishSprite;
        [SerializeField] private Sprite playButtonEnglishSprite;
        [SerializeField] private Sprite playButtonKoreanSprite;
        [SerializeField] private Image continueButtonImage;
        [SerializeField] private Sprite continueButtonTurkishSprite;
        [SerializeField] private Sprite continueButtonEnglishSprite;
        [SerializeField] private Sprite continueButtonKoreanSprite;
        [SerializeField] private Image scoresButtonImage;
        [SerializeField] private Sprite scoresButtonTurkishSprite;
        [SerializeField] private Sprite scoresButtonEnglishSprite;
        [SerializeField] private Sprite scoresButtonKoreanSprite;
        [SerializeField] private Button loginButton;
        [SerializeField] private Image loginButtonImage;
        [SerializeField] private Sprite loginButtonTurkishSprite;
        [SerializeField] private Sprite loginButtonEnglishSprite;
        [SerializeField] private Sprite loginButtonKoreanSprite;
        [SerializeField] private Button registerButton;
        [SerializeField] private Image registerButtonImage;
        [SerializeField] private Sprite registerButtonTurkishSprite;
        [SerializeField] private Sprite registerButtonEnglishSprite;
        [SerializeField] private Sprite registerButtonKoreanSprite;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Image logoutButtonImage;
        [SerializeField] private Sprite logoutButtonTurkishSprite;
        [SerializeField] private Sprite logoutButtonEnglishSprite;
        [SerializeField] private Sprite logoutButtonKoreanSprite;

        [Header("Logo Language Objects (Drag & Drop)")]
        [SerializeField] private GameObject logoTurkishObject;
        [SerializeField] private GameObject logoEnglishObject;
        [SerializeField] private GameObject logoKoreanObject;

        [Header("MainMenu Button Hit Areas")]
        [SerializeField] private bool enforceTightButtonHitAreas = true;
        [SerializeField] private bool autoFindAllSceneButtons = false;
        [SerializeField] private Button playButton;
        [SerializeField] private Button scoresButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private RectTransform playButtonHitZone;
        [SerializeField] private RectTransform continueButtonHitZone;
        [SerializeField] private RectTransform scoresButtonHitZone;

        [Header("Settings Popup")]
        [SerializeField] private MainMenuSettingsPopupView settingsPopupView;

        [Header("Responsive Layout")]
        [SerializeField] private bool applyResponsiveLayout = true;
        [SerializeField] private RectTransform mainMenuSafeAreaRoot;
        [SerializeField] private RectTransform authPanelRoot;
        [SerializeField] private RectTransform bestScoreBadgeRoot;
        [SerializeField] private RectTransform playButtonGlowRig;

        [Header("Device Layout Profiles")]
        [SerializeField] private DeviceLayoutProfile[] deviceProfiles;
        [SerializeField] private DeviceLayoutProfile defaultProfile;
        [SerializeField] private bool autoDetectDevice = true;
        [SerializeField] private bool showLayoutDebug = false;

        private UnityPlayerPrefsDataProvider _dataProvider;
        private Button _vibrationOnIconButton;
        private Button _vibrationOffIconButton;
        private bool _hasContinueGame;
        private bool _editorRefreshQueued;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private DeviceLayoutProfile _currentProfile;
        private float _currentDiagonalInches;
        private RectTransform _responsiveFrameRoot;
        private RectTransform _responsiveContentRoot;
        private RectTransform _logoSlot;
        private RectTransform _badgeSlot;
        private RectTransform _playSlot;
        private RectTransform _secondaryButtonsRow;
        private RectTransform _authSlot;

        public static readonly Color MainMenuSolidBgColor = new Color(0.0196f, 0.1176f, 0.2980f, 1f); // #051E4C Dark Royal Navy

        private void SetupMainMenuBackground()
        {
            var cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = MainMenuSolidBgColor;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var bgTransform = canvas.transform.Find("MainMenuBackground");
                Image bgImage = null;
                if (bgTransform == null)
                {
                    var bgGo = new GameObject("MainMenuBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    bgGo.transform.SetParent(canvas.transform, false);
                    bgGo.transform.SetAsFirstSibling();
                    bgTransform = bgGo.transform;
                    bgImage = bgGo.GetComponent<Image>();
                }
                else
                {
                    bgImage = bgTransform.GetComponent<Image>();
                    bgTransform.SetAsFirstSibling();
                }

                if (bgImage != null)
                {
                    var rt = bgImage.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);

                    Sprite bgSprite = null;
#if UNITY_EDITOR
                    bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/mainmenu_background.png");
#endif
                    if (bgSprite == null)
                        bgSprite = Resources.Load<Sprite>("mainmenu_background");

                    if (bgSprite != null)
                    {
                        bgImage.sprite = bgSprite;
                        bgImage.type = Image.Type.Simple;
                        bgImage.color = Color.white;
                    }
                    else
                    {
                        bgImage.sprite = null;
                        bgImage.color = MainMenuSolidBgColor;
                    }
                    bgImage.raycastTarget = false;
                }
            }
        }

        void Start()
        {
            SetupMainMenuBackground();
        }

        void Awake()
        {
            SetupMainMenuBackground();
            if (_activeInstance != null && _activeInstance != this)
            {
                if (verboseLogs)
                    Debug.LogWarning($"[MainMenuController] Duplicate controller disabled on '{gameObject.name}'.");
                enabled = false;
                return;
            }

            _activeInstance = this;
            _dataProvider = new UnityPlayerPrefsDataProvider();
            ResolveRuntimeReferences(allowCreate: true);
            BindButtonListeners();
            RefreshContinueAvailability();
            RefreshContinueButton();
            RefreshVibrationButton();
            SetupMenuLocalization();
            ApplyLanguageSpriteBindings();
            ConfigureAllMenuButtonHitAreas();
            ApplyResponsiveMenuLayout(force: true);
        }

        private void OnDestroy()
        {
            if (_activeInstance == this)
                _activeInstance = null;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (_editorRefreshQueued)
                return;

            _editorRefreshQueued = true;
            EditorApplication.delayCall += RefreshEditorReferences;
#endif
        }

#if UNITY_EDITOR
        private void RefreshEditorReferences()
        {
            _editorRefreshQueued = false;

            if (this == null || gameObject == null)
                return;

            ResolveRuntimeReferences(allowCreate: false);
        }
#endif

        private void OnEnable()
        {
            ResolveRuntimeReferences(allowCreate: Application.isPlaying);
            BindButtonListeners();
            LanguageManager.Instance.Subscribe(this);
            RefreshContinueAvailability();
            RefreshContinueButton();
            RefreshVibrationButton();
            ApplyLanguageSpriteBindings();
            ConfigureAllMenuButtonHitAreas();
            ApplyResponsiveMenuLayout(force: true);
        }

        private void OnDisable()
        {
            LanguageManager.Instance.Unsubscribe(this);
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            SetupMenuLocalization();
            ApplyLanguageSpriteBindings();
            RefreshContinueButton();
            RefreshVibrationButton();

            Canvas.ForceUpdateCanvases();
            ApplyResponsiveMenuLayout(force: true);
        }

        private void Update()
        {
            var activeCam = Camera.main;
            if (activeCam != null && activeCam.backgroundColor != MainMenuSolidBgColor)
            {
                activeCam.clearFlags = CameraClearFlags.SolidColor;
                activeCam.backgroundColor = MainMenuSolidBgColor;
            }
            if (!applyResponsiveLayout)
                return;

            if (HasScreenMetricsChanged())
                ApplyResponsiveMenuLayout(force: true);
        }

        public void StartGame()
        {
            GameLaunchState.RequestNewGame(GameMode.Classic);
            AppAnalytics.TrackModeSelected("new_game", GameMode.Classic.ToString());

            if (verboseLogs)
                Debug.Log($"[MainMenuController] StartGame clicked. Loading scene: {gameSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void StartTutorialReplay()
        {
            Debug.Log("[MainMenuController] Tutorial replay is disabled.");
        }

        public void ContinueGame()
        {
            if (!HasContinueGame())
            {
                if (verboseLogs)
                    Debug.Log("[MainMenuController] Continue requested but no saved game. Starting new game.");
                StartGame();
                return;
            }

            GameLaunchState.RequestContinue();
            AppAnalytics.TrackModeSelected("continue", GameMode.Classic.ToString());

            if (verboseLogs)
                Debug.Log($"[MainMenuController] ContinueGame clicked. Loading scene: {gameSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void StartChallengeMode()
        {
            if (challengeModeButton != null && !challengeModeButton.gameObject.activeInHierarchy)
                return;
            GameLaunchState.RequestNewGame(GameMode.Challenge);
            AppAnalytics.TrackModeSelected("new_game", GameMode.Challenge.ToString());
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void StartZenMode()
        {
            if (zenModeButton != null && !zenModeButton.gameObject.activeInHierarchy)
                return;
            GameLaunchState.RequestNewGame(GameMode.Zen);
            AppAnalytics.TrackModeSelected("new_game", GameMode.Zen.ToString());
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void ReturnToMainMenu()
        {
            if (verboseLogs)
                Debug.Log($"[MainMenuController] ReturnToMainMenu clicked. Loading scene: {mainMenuSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{mainMenuSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void OpenSettings()
        {
            if (settingsPopupView == null)
            {
                settingsPopupView = FindFirstObjectByType<MainMenuSettingsPopupView>(FindObjectsInactive.Include);
                if (settingsPopupView == null)
                {
                    Debug.LogError("[MainMenuController] settingsPopupView bulunamadi.");
                    return;
                }
            }

            settingsPopupView.Open();

            if (verboseLogs)
                Debug.Log($"[MainMenuController] Settings popup acildi: {settingsPopupView.name}");
        }

        public void OnSettingsButtonClicked()
        {
            OpenSettings();
        }

        public void OpenScores()
        {
            if (verboseLogs)
                Debug.Log($"[MainMenuController] OpenScores clicked. Loading scene: {scoresSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(scoresSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{scoresSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(scoresSceneName);
        }

        public void ToggleVibration()
        {
            bool vibrationEnabled = !IsVibrationEnabled();
            PlayerPrefs.SetInt(SettingsKeys.Vibration, vibrationEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (verboseLogs)
                Debug.Log($"[MainMenuController] Vibration toggled from main menu: {vibrationEnabled}");

            PlayToggleVibrationFeedback(vibrationEnabled);
            RefreshVibrationButton();
        }

        public void QuitGame()
        {
            if (verboseLogs)
                Debug.Log("[MainMenuController] Quit clicked");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool HasContinueGame()
        {
            return _hasContinueGame;
        }

        private void RefreshContinueAvailability()
        {
            _ = RefreshContinueAvailabilityAsync();
        }

        private async Task RefreshContinueAvailabilityAsync()
        {
            if (_dataProvider == null)
            {
                _hasContinueGame = false;
                return;
            }

            var data = await _dataProvider.LoadGameDataAsync(SaveKey);
            _hasContinueGame = data != null && !data.IsGameOver;
            RefreshContinueButton();
        }

        private void ResolveRuntimeReferences(bool allowCreate)
        {
            if (continueButton == null)
                continueButton = FindButtonAnywhere("ContinueButton");

            if (playButton == null)
                playButton = FindButtonAnywhere("PlayButton");

            if (scoresButton == null)
                scoresButton = FindButtonAnywhere("ScoresButton");

            var loginUiManager = FindFirstObjectByType<LoginUIManager>(FindObjectsInactive.Include);

            if (loginButton == null)
                loginButton = loginUiManager != null ? loginUiManager.playLoginButton : FindButtonAnywhere("PlayLoginButton");

            if (registerButton == null)
                registerButton = loginUiManager != null ? loginUiManager.guestLoginButton : FindButtonAnywhere("GuestLoginButton");

            if (logoutButton == null)
                logoutButton = loginUiManager != null ? loginUiManager.logoutButton : FindButtonAnywhere("LogoutButton");

            if (loginButtonImage == null)
                loginButtonImage = GetButtonImage(loginButton);

            if (registerButtonImage == null)
                registerButtonImage = GetButtonImage(registerButton);

            if (logoutButtonImage == null)
                logoutButtonImage = GetButtonImage(logoutButton);

            if (settingsButton == null)
            {
                settingsButton = FindButtonAnywhere("SettingsButton");
            }

            if (settingsButton == null)
            {
                var obj = FindGameObjectAnywhere("SettingsButton");
                if (obj != null && (allowCreate || obj.GetComponent<Button>() != null))
                    settingsButton = EnsureButton(obj);
            }

            if (settingsButton == null)
            {
                var fallbacks = new[] { "Settingsbutton", "SettingsBtn", "Settings", "AyarlarButton", "Ayarlar" };
                for (int i = 0; i < fallbacks.Length && settingsButton == null; i++)
                {
                    var obj = FindGameObjectAnywhere(fallbacks[i]);
                    if (obj != null)
                    {
                        if (allowCreate || obj.GetComponent<Button>() != null)
                            settingsButton = EnsureButton(obj);
                    }
                }
            }

            if (settingsButton == null)
            {
                var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allButtons.Length; i++)
                {
                    string lower = allButtons[i].name.ToLower();
                    if (lower.Contains("settings") || lower.Contains("ayar") || lower.Contains("gear"))
                    {
                        settingsButton = allButtons[i];
                        break;
                    }
                }
            }

            if (vibrationToggleButton == null)
            {
                vibrationToggleButton = FindButtonAnywhere("VibrationToggleButton");

                if (vibrationToggleButton == null)
                {
                    var candidate = FindGameObjectAnywhere("Titresim_acik") ??
                                    FindGameObjectAnywhere("Titresim_kapali");

                    if (candidate != null && (allowCreate || candidate.GetComponent<Button>() != null))
                        vibrationToggleButton = EnsureButton(candidate);
                }
            }

            if (vibrationOnIcon == null)
                vibrationOnIcon = FindGraphicAnywhere("Titresim_acik");

            if (vibrationOffIcon == null)
                vibrationOffIcon = FindGraphicAnywhere("Titresim_kapali");

            if (vibrationOnIcon != null)
                _vibrationOnIconButton = allowCreate ? EnsureButton(vibrationOnIcon.gameObject) : vibrationOnIcon.GetComponent<Button>();

            if (vibrationOffIcon != null)
                _vibrationOffIconButton = allowCreate ? EnsureButton(vibrationOffIcon.gameObject) : vibrationOffIcon.GetComponent<Button>();

            if (vibrationToggleButton == null)
                vibrationToggleButton = _vibrationOnIconButton ?? _vibrationOffIconButton;

            if (vibrationToggleBackground == null && vibrationToggleButton != null)
                vibrationToggleBackground = vibrationToggleButton.targetGraphic;

            if (settingsButton != null && settingsPopupView == null)
                settingsPopupView = FindFirstObjectByType<MainMenuSettingsPopupView>(FindObjectsInactive.Include);

            Canvas targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (allowCreate && settingsButton != null && settingsPopupView != null && targetCanvas != null && settingsPopupView.transform.parent != targetCanvas.transform)
                settingsPopupView.transform.SetParent(targetCanvas.transform, false);

            if (allowCreate && settingsButton != null && settingsPopupView == null)
            {
                var popupHost = new GameObject("SettingsPopupHost");

                if (targetCanvas != null)
                    popupHost.transform.SetParent(targetCanvas.transform, false);
                else
                    popupHost.transform.SetParent(transform, false);

                settingsPopupView = popupHost.AddComponent<MainMenuSettingsPopupView>();
            }

            if (mainMenuSafeAreaRoot == null)
                mainMenuSafeAreaRoot = FindRectTransformAnywhere("__MainMenuSafeAreaRoot");

            if (authPanelRoot == null)
            {
                if (loginButton != null)
                    authPanelRoot = loginButton.transform.parent as RectTransform;
                else if (logoutButton != null)
                    authPanelRoot = logoutButton.transform.parent as RectTransform;
            }

            if (bestScoreBadgeRoot == null)
                bestScoreBadgeRoot = FindRectTransformAnywhere("BestScoreBadge");

            if (playButtonGlowRig == null)
                playButtonGlowRig = FindRectTransformAnywhere("__PlayButtonGlowRig");

            if (logoTurkishObject == null)
            {
                logoTurkishObject = FindGameObjectAnywhere("LogoTr") ?? 
                                    FindGameObjectAnywhere("LogoTurkish") ?? 
                                    FindGameObjectAnywhere("Logo_Tr") ?? 
                                    FindGameObjectAnywhere("Logo_Turkish");
            }

            if (logoEnglishObject == null)
            {
                logoEnglishObject = FindGameObjectAnywhere("LogoEn") ?? 
                                    FindGameObjectAnywhere("LogoEnglish") ?? 
                                    FindGameObjectAnywhere("Logo_En") ?? 
                                    FindGameObjectAnywhere("Logo_English");
            }

            if (logoKoreanObject == null)
            {
                logoKoreanObject = FindGameObjectAnywhere("LogoKo") ?? 
                                   FindGameObjectAnywhere("LogoKorean") ?? 
                                   FindGameObjectAnywhere("Logo_Ko") ?? 
                                   FindGameObjectAnywhere("Logo_Korean") ??
                                   FindGameObjectAnywhere("LogoKor") ??
                                   FindGameObjectAnywhere("Logokore") ??
                                   FindGameObjectAnywhere("LogoKore");
            }
        }

        private void BindButtonListeners()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(StartGame);
                playButton.onClick.AddListener(StartGame);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueGame);
                continueButton.onClick.AddListener(ContinueGame);
            }

            if (challengeModeButton != null)
            {
                challengeModeButton.onClick.RemoveListener(StartChallengeMode);
                challengeModeButton.onClick.AddListener(StartChallengeMode);
            }

            if (zenModeButton != null)
            {
                zenModeButton.onClick.RemoveListener(StartZenMode);
                zenModeButton.onClick.AddListener(StartZenMode);
            }

            if (scoresButton != null)
            {
                scoresButton.onClick.RemoveListener(OpenScores);
                scoresButton.onClick.AddListener(OpenScores);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
                settingsButton.onClick.AddListener(OpenSettings);

                var graphic = settingsButton.targetGraphic;
                if (graphic != null)
                    graphic.raycastTarget = true;

                settingsButton.interactable = true;
            }
            else
            {
                Debug.LogError("[MainMenuController] Settings button bulunamadi, listener baglanamadi.");
            }

            if (vibrationToggleButton != null)
            {
                vibrationToggleButton.onClick.RemoveListener(ToggleVibration);
                vibrationToggleButton.onClick.AddListener(ToggleVibration);
            }

            if (_vibrationOnIconButton != null && _vibrationOnIconButton != vibrationToggleButton)
            {
                _vibrationOnIconButton.onClick.RemoveListener(ToggleVibration);
                _vibrationOnIconButton.onClick.AddListener(ToggleVibration);
            }

            if (_vibrationOffIconButton != null && _vibrationOffIconButton != vibrationToggleButton)
            {
                _vibrationOffIconButton.onClick.RemoveListener(ToggleVibration);
                _vibrationOffIconButton.onClick.AddListener(ToggleVibration);
            }
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
                return;

            continueButton.interactable = HasContinueGame();
        }

        private void RefreshVibrationButton()
        {
            bool vibrationEnabled = IsVibrationEnabled();

            SetIconVisible(vibrationOnIcon, vibrationEnabled);
            SetIconVisible(vibrationOffIcon, !vibrationEnabled);
            RefreshVibrationBackground(vibrationEnabled);

            if (vibrationToggleButton != null)
                vibrationToggleButton.interactable = true;

            if (_vibrationOnIconButton != null)
                _vibrationOnIconButton.interactable = true;

            if (_vibrationOffIconButton != null)
                _vibrationOffIconButton.interactable = true;
        }

        private void RefreshVibrationBackground(bool vibrationEnabled)
        {
            if (vibrationToggleBackground == null)
                return;

            vibrationToggleBackground.color = vibrationEnabled
                ? vibrationEnabledBackgroundColor
                : vibrationDisabledBackgroundColor;
        }

        private static bool IsVibrationEnabled()
        {
            return PlayerPrefs.GetInt(SettingsKeys.Vibration, 1) == 1;
        }

        private void SetIconVisible(Graphic graphic, bool isVisible)
        {
            if (graphic == null)
                return;

            bool sharesToggleRoot = vibrationToggleButton != null &&
                                    graphic.gameObject == vibrationToggleButton.gameObject;

            if (!sharesToggleRoot && graphic.gameObject.activeSelf != isVisible)
                graphic.gameObject.SetActive(isVisible);

            graphic.enabled = isVisible;
            graphic.raycastTarget = isVisible;
        }

        private void PlayToggleVibrationFeedback(bool vibrationEnabled)
        {
            if (!playToggleVibrationFeedback)
                return;

            long durationMs = vibrationEnabled ? LongToggleVibrationMs : ShortToggleVibrationMs;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null)
                    return;

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdkInt = version.GetStatic<int>("SDK_INT");
                if (sdkInt >= 26)
                {
                    using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", durationMs, -1);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
            catch (System.Exception ex)
            {
                if (verboseLogs)
                    Debug.LogWarning($"[MainMenuController] Toggle vibration feedback failed: {ex.Message}");
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static Button EnsureButton(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            var button = gameObject.GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();

            if (button.targetGraphic == null)
                button.targetGraphic = gameObject.GetComponent<Graphic>();

            if (button.targetGraphic == null)
            {
                var fallbackImage = gameObject.GetComponent<Image>();
                if (fallbackImage == null)
                    fallbackImage = gameObject.AddComponent<Image>();

                fallbackImage.color = new Color(1f, 1f, 1f, 0f);
                fallbackImage.raycastTarget = true;
                button.targetGraphic = fallbackImage;
            }
            else
            {
                button.targetGraphic.raycastTarget = true;
            }

            button.interactable = true;
            return button;
        }

        private static Button FindButtonAnywhere(string name)
        {
            var go = FindGameObjectAnywhere(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static RectTransform FindRectTransformAnywhere(string name)
        {
            var go = FindGameObjectAnywhere(name);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private static Graphic FindGraphicAnywhere(string name)
        {
            var go = FindGameObjectAnywhere(name);
            return go != null ? go.GetComponent<Graphic>() : null;
        }

        private static GameObject FindGameObjectAnywhere(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var activeObject = GameObject.Find(name);
            if (activeObject != null)
                return activeObject;

            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var candidate = allTransforms[i];
                if (candidate == null || candidate.hideFlags != HideFlags.None)
                    continue;

                if (!candidate.gameObject.scene.IsValid())
                    continue;

                if (candidate.name == name)
                    return candidate.gameObject;
            }

            return null;
        }

        private bool HasScreenMetricsChanged()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                return true;

            Rect safeArea = Screen.safeArea;
            const float threshold = 0.5f;
            return Mathf.Abs(safeArea.x - _lastSafeArea.x) > threshold ||
                   Mathf.Abs(safeArea.y - _lastSafeArea.y) > threshold ||
                   Mathf.Abs(safeArea.width - _lastSafeArea.width) > threshold ||
                   Mathf.Abs(safeArea.height - _lastSafeArea.height) > threshold;
        }

        private void CacheScreenMetrics()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        private void ApplyResponsiveMenuLayout(bool force)
        {
            MainMenuLayoutPresenter.ApplyResponsiveMenuLayout(this, force);
        }

        private static void ApplySafeAreaToRoot(RectTransform rect)
        {
            if (rect == null)
                return;

            Rect safeArea = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            rect.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            rect.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private RectTransform GetActiveLogoRect()
        {
            if (logoKoreanObject != null && logoKoreanObject.activeInHierarchy)
                return logoKoreanObject.GetComponent<RectTransform>();

            if (logoEnglishObject != null && logoEnglishObject.activeInHierarchy)
                return logoEnglishObject.GetComponent<RectTransform>();

            if (logoTurkishObject != null && logoTurkishObject.activeInHierarchy)
                return logoTurkishObject.GetComponent<RectTransform>();

            if (logoKoreanObject != null)
                return logoKoreanObject.GetComponent<RectTransform>();

            return logoEnglishObject != null
                ? logoEnglishObject.GetComponent<RectTransform>()
                : logoTurkishObject != null ? logoTurkishObject.GetComponent<RectTransform>() : null;
        }

        private RectTransform GetInactiveLogoRect()
        {
            RectTransform active = GetActiveLogoRect();
            RectTransform english = logoEnglishObject != null ? logoEnglishObject.GetComponent<RectTransform>() : null;
            RectTransform turkish = logoTurkishObject != null ? logoTurkishObject.GetComponent<RectTransform>() : null;
            if (active == english)
                return turkish;
            if (active == turkish)
                return english;
            return english != null ? english : turkish;
        }

        private void EnsureResponsiveLayoutHierarchy(RectTransform safeRoot)
        {
            _responsiveFrameRoot = EnsureRectTransform("__MainMenuResponsiveFrame", safeRoot);
            _responsiveContentRoot = EnsureRectTransform("__MainMenuResponsiveContent", _responsiveFrameRoot);
            _logoSlot = EnsureRectTransform("__MainMenuLogoSlot", _responsiveContentRoot);
            _badgeSlot = EnsureRectTransform("__MainMenuBadgeSlot", _responsiveContentRoot);
            _playSlot = EnsureRectTransform("__MainMenuPlaySlot", _responsiveContentRoot);
            _secondaryButtonsRow = EnsureRectTransform("__MainMenuSecondaryRow", _responsiveContentRoot);
            _authSlot = EnsureRectTransform("__MainMenuAuthSlot", _responsiveContentRoot);

            ConfigureVerticalContentRoot(_responsiveContentRoot, _currentProfile?.verticalSpacing ?? 12f);
            ConfigureHorizontalRow(_secondaryButtonsRow, _currentProfile?.horizontalSpacing ?? 16f);

            SetSlotDefaults(_logoSlot);
            SetSlotDefaults(_badgeSlot);
            SetSlotDefaults(_playSlot);
            SetSlotDefaults(_secondaryButtonsRow);
            SetSlotDefaults(_authSlot);
        }

        private static RectTransform EnsureRectTransform(string name, Transform parent)
        {
            if (parent == null)
                return null;

            Transform child = parent.Find(name);
            RectTransform rect;
            if (child != null)
            {
                rect = child as RectTransform;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.layer = parent.gameObject.layer;
                rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static void ConfigureVerticalContentRoot(RectTransform root, float spacing = 12f)
        {
            if (root == null)
                return;

            var layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureHorizontalRow(RectTransform row, float spacing)
        {
            if (row == null)
                return;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        private static void SetSlotDefaults(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void UpdateLayoutElement(RectTransform rect, float width, float height)
        {
            if (rect == null)
                return;

            var element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        private static void LayoutManagedButton(Button button, RectTransform parent, RectTransform hitZone)
        {
            if (button == null || parent == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                return;

            EnsureEffectiveSize(rect);
            SetRectAsCenteredChild(rect, parent, false);
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;

            var element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;

            if (hitZone != null)
                StretchHitZone(hitZone);

            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                fitter.aspectMode = AspectRatioFitter.AspectMode.None;
        }

        private static void EnsureEffectiveSize(RectTransform rect)
        {
            if (rect == null)
                return;

            float width = Mathf.Max(1f, Mathf.Abs(rect.sizeDelta.x * rect.localScale.x));
            float height = Mathf.Max(1f, Mathf.Abs(rect.sizeDelta.y * rect.localScale.y));
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void UpdateSlotPreferredSize(RectTransform slot, RectTransform child, float horizontalPadding, float verticalPadding)
        {
            if (slot == null || child == null)
                return;

            Vector2 size = child.sizeDelta;
            UpdateLayoutElement(slot, size.x + horizontalPadding, size.y + verticalPadding);
        }

        private static void SetRectAsCenteredChild(RectTransform rect, RectTransform parent, bool worldPositionStays)
        {
            if (rect == null || parent == null)
                return;

            if (rect.parent != parent)
                rect.SetParent(parent, worldPositionStays);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
        }

        private static void StretchHitZone(RectTransform hitZone)
        {
            if (hitZone == null)
                return;

            hitZone.anchorMin = Vector2.zero;
            hitZone.anchorMax = Vector2.one;
            hitZone.pivot = new Vector2(0.5f, 0.5f);
            hitZone.anchoredPosition = Vector2.zero;
            hitZone.sizeDelta = Vector2.zero;
            hitZone.localScale = Vector3.one;
        }

        private static void StretchToParent(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void UpdateRowPreferredSize(RectTransform row, float spacing, params Button[] buttons)
        {
            if (row == null)
                return;

            float width = 0f;
            float height = 0f;
            int visibleCount = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeInHierarchy)
                    continue;

                var rect = button.transform as RectTransform;
                if (rect == null)
                    continue;

                width += rect.sizeDelta.x;
                height = Mathf.Max(height, rect.sizeDelta.y);
                visibleCount++;
            }

            if (visibleCount > 1)
                width += spacing * (visibleCount - 1);

            UpdateLayoutElement(row, width, height);
        }

        private static void SetRectTopRight(RectTransform rect, RectTransform parent, float rightPadding, float topPadding)
        {
            if (rect == null || parent == null)
                return;

            if (rect.parent != parent)
                rect.SetParent(parent, false);

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-rightPadding, -topPadding);
            rect.localScale = Vector3.one;
        }

        private void ApplyLanguageSpriteBindings()
        {
            MainMenuLocalizationPresenter.ApplyLanguageSpriteBindings(this);
        }

        private static void ApplySprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;

            var rect = image.rectTransform;
            if (rect != null && sprite.rect.height > 0)
            {
                float currentHeight = rect.sizeDelta.y;
                if (currentHeight <= 1f)
                {
                    currentHeight = sprite.rect.height;
                }
                float aspect = sprite.rect.width / sprite.rect.height;
                Vector2 newSize = new Vector2(currentHeight * aspect, currentHeight);
                rect.sizeDelta = newSize;

                var button = image.GetComponentInParent<Button>();
                if (button != null)
                {
                    var buttonRect = button.transform as RectTransform;
                    if (buttonRect != null && buttonRect != rect)
                    {
                        buttonRect.sizeDelta = newSize;
                    }
                }
            }
        }

        private static Image GetButtonImage(Button button)
        {
            if (button == null)
                return null;

            if (button.targetGraphic is Image image)
                return image;

            return button.GetComponent<Image>();
        }

        private static void ApplyLanguageObjectVisibility(GameObject turkishObject, GameObject englishObject, bool isEnglish)
        {
            if (turkishObject != null)
                turkishObject.SetActive(!isEnglish);

            if (englishObject != null)
                englishObject.SetActive(isEnglish);
        }

        private void ConfigureAllMenuButtonHitAreas()
        {
            if (!enforceTightButtonHitAreas)
                return;

            ConfigureTightHitArea(playButton, playButtonHitZone);
            ConfigureTightHitArea(continueButton, continueButtonHitZone);
            ConfigureTightHitArea(scoresButton, scoresButtonHitZone);
            ConfigureTightHitArea(settingsButton, null);

            if (autoFindAllSceneButtons)
            {
                var sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < sceneButtons.Length; i++)
                {
                    if (sceneButtons[i] != null && sceneButtons[i] != playButton && sceneButtons[i] != continueButton && sceneButtons[i] != scoresButton && sceneButtons[i] != settingsButton)
                        ConfigureTightHitArea(sceneButtons[i], null);
                }
            }
        }

        private static void ConfigureTightHitArea(Button button, RectTransform hitZone)
        {
            if (button == null)
                return;

            var targetGraphic = button.targetGraphic;
            if (targetGraphic == null)
                targetGraphic = button.GetComponent<Image>();

            if (targetGraphic == null)
                targetGraphic = button.GetComponent<Graphic>();

            if (targetGraphic == null)
                return;

            if (ShouldIgnoreCustomHitZone(targetGraphic.transform as RectTransform, hitZone))
                hitZone = null;

            button.targetGraphic = targetGraphic;
            targetGraphic.raycastTarget = true;

            var childGraphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                if (childGraphics[i] == null)
                    continue;

                if (childGraphics[i].gameObject == targetGraphic.gameObject)
                    continue;

                childGraphics[i].raycastTarget = false;
            }

            var limiter = button.GetComponent<ButtonHitAreaLimiter>();
            if (limiter == null)
                limiter = button.gameObject.AddComponent<ButtonHitAreaLimiter>();

            limiter.SetTarget(targetGraphic, hitZone);
        }

        private static bool ShouldIgnoreCustomHitZone(RectTransform targetRect, RectTransform hitZone)
        {
            if (targetRect == null || hitZone == null)
                return false;

            Rect target = targetRect.rect;
            Rect zone = hitZone.rect;
            if (target.width <= 0f || target.height <= 0f || zone.width <= 0f || zone.height <= 0f)
                return false;

            float targetArea = target.width * target.height;
            float zoneArea = zone.width * zone.height;
            if (targetArea <= 0f || zoneArea <= 0f)
                return false;

            return zoneArea < targetArea * 0.6f;
        }

        /// <summary>
        /// Menü butonlarına LocalizedText kurulumu yapısır
        /// </summary>
        private void SetupMenuLocalization()
        {
            MainMenuLocalizationPresenter.SetupMenuLocalization(this);
        }

        private void SetupButtonLocalization(Button button, string turkishText, string englishText, HashSet<TextMeshProUGUI> localizedLabels, string buttonName, string koreanText = "")
        {
            var text = FindMenuButtonLabelText(button);
            if (text == null)
                return;

            if (localizedLabels.Contains(text))
            {
                if (verboseLogs)
                    Debug.LogWarning($"[MainMenuController] '{buttonName}' localization skipped: shared label '{text.name}' already used by another button.");
                return;
            }

            localizedLabels.Add(text);
            LocalizedTextSetup.SetupLocalization(text, turkishText, englishText, koreanText);
        }

        private static TextMeshProUGUI FindMenuButtonLabelText(Button button)
        {
            if (button == null)
                return null;

            Transform directLabel = button.transform.Find("LabelText");
            if (directLabel != null)
            {
                var directText = directLabel.GetComponent<TextMeshProUGUI>();
                if (directText != null)
                    return directText;
            }

            var allTexts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI bestMatch = null;
            int bestDepth = int.MaxValue;

            for (int i = 0; i < allTexts.Length; i++)
            {
                var candidate = allTexts[i];
                if (candidate == null)
                    continue;

                string nameLower = candidate.name.ToLowerInvariant();
                bool looksLikeLabel = nameLower.Contains("label") || nameLower.Contains("text") || nameLower.Contains("title");
                if (!looksLikeLabel || nameLower.Contains("icon"))
                    continue;

                int depth = 0;
                Transform current = candidate.transform;
                while (current != null && current != button.transform)
                {
                    depth++;
                    current = current.parent;
                }

                if (current != button.transform)
                    continue;

                if (depth < bestDepth)
                {
                    bestDepth = depth;
                    bestMatch = candidate;
                }
            }

            if (bestMatch != null)
                return bestMatch;

            for (int i = 0; i < allTexts.Length; i++)
            {
                var candidate = allTexts[i];
                if (candidate == null)
                    continue;

                string nameLower = candidate.name.ToLowerInvariant();
                if (!nameLower.Contains("icon"))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Detects the appropriate device profile based on current screen dimensions
        /// </summary>
        private DeviceLayoutProfile DetectDeviceProfile()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            _currentDiagonalInches = GetDiagonalInches();

            if (deviceProfiles == null || deviceProfiles.Length == 0)
                return defaultProfile;

            // Try to find matching profile
            for (int i = 0; i < deviceProfiles.Length; i++)
            {
                if (deviceProfiles[i] != null && 
                    deviceProfiles[i].MatchesScreen(screenWidth, screenHeight, _currentDiagonalInches))
                {
                    if (verboseLogs)
                        Debug.Log($"[MainMenuController] Device profile matched: {deviceProfiles[i].deviceName}");
                    return deviceProfiles[i];
                }
            }

            // If no profile matches, use default
            if (verboseLogs)
                Debug.LogWarning($"[MainMenuController] No matching device profile for {screenWidth}x{screenHeight} ({_currentDiagonalInches:F1}\"). Using default.");
            
            return defaultProfile;
        }

        /// <summary>
        /// Calculate diagonal screen size in inches
        /// </summary>
        private static float GetDiagonalInches()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // Screen.dpi might be 0 on some devices, default to 441 (average Android DPI)
            float dpi = Screen.dpi > 0 ? Screen.dpi : 441f;
            
            float widthInches = screenWidth / dpi;
            float heightInches = screenHeight / dpi;
            
            float diagonalPixels = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float diagonalInches = diagonalPixels / dpi;
            
            return diagonalInches;
        }

        /// <summary>
        /// Display layout debug information on screen
        /// </summary>
        private void DebugLayoutInfo(float safeWidth, float safeHeight, float fitScale)
        {
            if (!verboseLogs || _currentProfile == null)
                return;

            string debugText = $"[Layout Debug]\n" +
                              $"Device: {_currentProfile.deviceName} ({_currentProfile.deviceType})\n" +
                              $"Screen: {Screen.width}x{Screen.height} ({_currentDiagonalInches:F1}\")\n" +
                              $"Safe Area: {safeWidth:F0}x{safeHeight:F0}\n" +
                              $"Spacing: V={_currentProfile.verticalSpacing} H={_currentProfile.horizontalSpacing}\n" +
                              $"Min Scale: {_currentProfile.minScale:F2} | Fit Scale: {fitScale:F2}\n" +
                              $"Aspect: {(Screen.width / (float)Screen.height):F2}:1";

            Debug.Log(debugText);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw gizmos for layout visualization in editor
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showLayoutDebug || mainMenuSafeAreaRoot == null)
                return;

            // Draw safe area frame
            if (_currentProfile != null)
            {
                Gizmos.color = _currentProfile.debugColorFrame;
                var safeAreaRect = mainMenuSafeAreaRoot.rect;
                var pos = mainMenuSafeAreaRoot.position;
                DrawRectGizmo(pos, safeAreaRect.width, safeAreaRect.height, 2f);
            }
        }

        private static void DrawRectGizmo(Vector3 center, float width, float height, float lineWidth)
        {
            Vector3 topLeft = center + new Vector3(-width / 2, height / 2, 0);
            Vector3 topRight = center + new Vector3(width / 2, height / 2, 0);
            Vector3 bottomRight = center + new Vector3(width / 2, -height / 2, 0);
            Vector3 bottomLeft = center + new Vector3(-width / 2, -height / 2, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
#endif
    }
}
