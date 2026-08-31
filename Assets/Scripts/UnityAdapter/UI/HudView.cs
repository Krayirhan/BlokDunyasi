// File: UnityAdapter/UI/HudView.cs

using System.Collections;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Animation;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.UI.Localization;
using UnityEngine.SceneManagement;
using Debug = BlockPuzzle.Core.Common.GameLogger;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Facade for gameplay HUD event handling and score/combo presentation.
    /// Target/progress state belongs to TargetGoalSystem; HudView only triggers its refresh path.
    /// </summary>
    public partial class HudView : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private bool keepBestScoreAtInspectorPosition = true;
        
        [Header("Game Info")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI gameStatusText;
        [SerializeField] private TargetGoalSystem targetGoalSystem;
        [SerializeField] [Tooltip("(Deprecated - use TargetGoalSystem instead)")] private TextMeshProUGUI targetProgressText;
        
        [Header("Animation")]
        [SerializeField] private float scoreCountDuration = 0.5f;
        [SerializeField] private AnimationCurve scoreCountCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Score Breakdown Debug")]
        [SerializeField] private bool showScoreBreakdownDebug = false;
        [SerializeField] private GameObject scoreBreakdownPanel;
        [SerializeField] private TextMeshProUGUI scoreBreakdownText;

        [Header("Score Reactive Glow")]
        [SerializeField] private bool enableScoreReactiveGlow = true;
        [SerializeField] [Min(1f)] private float glowNormalizationMaxScoreDelta = 120f;
        [SerializeField] private AnimationCurve glowIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] [Range(0f, 1f)] private float glowMinAlpha = 0f;
        [SerializeField] [Range(0f, 1f)] private float glowMaxAlpha = 0.75f;
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] [Min(0.05f)] private float glowDuration = 0.2f;

        [Header("Responsive Layout")]
        [SerializeField] private bool applySafeAreaLayout = true;
        [SerializeField] private bool useSceneAnchoredHudLayout = true;
        [SerializeField] private Vector2 topPadding = new Vector2(40f, 40f);
        [SerializeField] private Vector2 bottomPadding = new Vector2(40f, 40f);
        [SerializeField] private Vector2 comboTopPadding = new Vector2(40f, 120f);
        [SerializeField] private bool reserveSpaceForBottomBanner = true;
        [SerializeField] [Min(0f)] private float bottomBannerSpacing = 24f;

        [Header("Combo Visual Style")]
        [SerializeField] private bool enableHudComboDisplay = false;
        [SerializeField] private TMP_FontAsset comboStyleFont;
        [SerializeField] private bool comboStyleBold = true;
        [SerializeField] [Min(1f)] private float comboStyleFontSize = 120f;
        [SerializeField] [Min(0.1f)] private float comboDisplayDuration = 1.2f;
        [SerializeField] private bool comboStyleUseGradient = true;
        [SerializeField] private Color comboStyleTopColor = new Color(0.73f, 0.95f, 1f, 1f);
        [SerializeField] private Color comboStyleBottomColor = new Color(0.16f, 0.48f, 0.98f, 1f);
        [SerializeField] private bool comboStyleUseOutline = true;
        [SerializeField] [Range(0f, 1f)] private float comboStyleOutlineWidth = 0.34f;
        [SerializeField] private Color comboStyleOutlineColor = new Color(0.02f, 0.27f, 0.72f, 1f);
        [SerializeField] private bool comboStyleUseUnderlay = true;
        [SerializeField] private Color comboStyleUnderlayColor = new Color(0.01f, 0.12f, 0.36f, 0.85f);
        [SerializeField] private Vector2 comboStyleUnderlayOffset = new Vector2(-0.08f, -0.12f);
        [SerializeField] [Range(-1f, 1f)] private float comboStyleUnderlayDilate = 0.25f;

        [Header("In-Game Navigation")]
        [SerializeField] private Button inGameMainMenuButton;
        [SerializeField] private string mainMenuSceneName = SceneCatalog.MainMenu;
        [SerializeField] private bool hideInGameMenuOnGameOver = true;
        [SerializeField] private bool autoPositionInGameMainMenuButton = true;
        [SerializeField] private Vector2 inGameMainMenuTopPadding = new Vector2(40f, 40f);

        [Header("Legacy Theme Test Button")]
        [SerializeField] private bool enableThemeTestButton = false;
        [SerializeField] private bool autoPositionThemeTestButton = true;
        [SerializeField] private Button themeTestButton;
        [SerializeField] private Vector2 themeTestButtonTopPadding = new Vector2(40f, 116f);
        [SerializeField] private Vector2 themeTestButtonSize = new Vector2(168f, 52f);
        [SerializeField] private string themeTestButtonLabel = "TEMA";
        
        // State tracking
        private int _displayedScore = 0;
        private int _targetScore = 0;
        private Coroutine _scoreCountAnimation;
        private Coroutine _scoreGlowAnimation;
        private Coroutine _statusMessageRoutine;
        private Coroutine _comboVisibilityRoutine;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private int _lastBannerBottomInsetPixels;
        private Rect _lastSafeArea;
        private Vector2 _currentScoreBaseAnchoredPosition;
        private Vector2 _currentScoreBaseAnchorMin;
        private Vector2 _currentScoreBaseAnchorMax;
        private Vector2 _currentScoreBasePivot;
        private bool _hasCachedCurrentScoreAnchor;
        private Vector2 _bestScoreBaseAnchoredPosition;
        private Vector2 _bestScoreBaseAnchorMin;
        private Vector2 _bestScoreBaseAnchorMax;
        private Vector2 _bestScoreBasePivot;
        private bool _hasCachedBestScoreAnchor;
        private Vector2 _turnCountBaseAnchoredPosition;
        private Vector2 _gameStatusBaseAnchoredPosition;
        private bool _hasCachedSceneAnchors;
        private string _baseStatusText = "Playing";
        private TextMeshProUGUI _themeTestButtonText;
        private GameSceneThemeController _gameSceneThemeController;
        private bool _loggedBootstrapFallbackWarning;
        private bool _loggedMissingDependencyWarning;
        
        private void Start()
        {
            ApplyUserAccessibilityPreferences();
            EnsureHudElements();
            ResolveRequiredDependencies();
            AutoResolveUiReferences();
            CacheSceneAnchoredPositions();
            SetupInGameMainMenuButton();
            SetupThemeTestButton();

            // Subscribe to game events
            GameBootstrap.OnScoreChanged += OnScoreChanged;
            GameBootstrap.OnScoreBreakdown += OnScoreBreakdown;
            GameBootstrap.OnGameStarted += OnGameStarted;
            GameBootstrap.OnGameContinued += OnGameContinued;
            GameBootstrap.OnGameOver += OnGameOver;
            
            // Initialize display
            InitializeDisplay();
            ApplyScoreBreakdownDebugVisibility();
            ApplyResponsiveHudLayout(force: true);

            // Setup localization
            SetupHudLocalization();
        }

        private void AutoResolveUiReferences()
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                var candidate = allTexts[i];
                if (candidate == null)
                    continue;

                string nameLower = candidate.name.ToLowerInvariant();
                if (currentScoreText == null && (nameLower.Contains("currentscore") || nameLower == "scoretext"))
                    currentScoreText = candidate;

                if (bestScoreText == null && nameLower.Contains("bestscore"))
                    bestScoreText = candidate;

                if (turnCountText == null && nameLower.Contains("turncount"))
                    turnCountText = candidate;

                if (gameStatusText == null && nameLower.Contains("gamestatus"))
                    gameStatusText = candidate;

                if (comboText == null && nameLower.Contains("combo"))
                {
                    comboText = candidate;
                    Debug.Log($"[HudView] Auto-resolved comboText reference: {candidate.name}");
                }

                if (targetProgressText == null && (nameLower.Contains("target") || nameLower.Contains("hedef")))
                {
                    targetProgressText = candidate;
                    Debug.Log($"[HudView] Auto-resolved targetProgressText reference: {candidate.name}");
                }
            }
        }

        private void CacheSceneAnchoredPositions()
        {
            if (currentScoreText != null)
            {
                RectTransform scoreRect = currentScoreText.rectTransform;
                _currentScoreBaseAnchoredPosition = scoreRect.anchoredPosition;
                _currentScoreBaseAnchorMin = scoreRect.anchorMin;
                _currentScoreBaseAnchorMax = scoreRect.anchorMax;
                _currentScoreBasePivot = scoreRect.pivot;
                _hasCachedCurrentScoreAnchor = true;
            }

            if (bestScoreText != null)
            {
                RectTransform bestRect = bestScoreText.rectTransform;
                _bestScoreBaseAnchoredPosition = bestRect.anchoredPosition;
                _bestScoreBaseAnchorMin = bestRect.anchorMin;
                _bestScoreBaseAnchorMax = bestRect.anchorMax;
                _bestScoreBasePivot = bestRect.pivot;
                _hasCachedBestScoreAnchor = true;
            }

            if (turnCountText != null)
                _turnCountBaseAnchoredPosition = turnCountText.rectTransform.anchoredPosition;

            if (gameStatusText != null)
                _gameStatusBaseAnchoredPosition = gameStatusText.rectTransform.anchoredPosition;

            _hasCachedSceneAnchors = true;
        }

        private void RestoreCurrentScoreTextPosition()
        {
            if (!_hasCachedCurrentScoreAnchor || currentScoreText == null)
                return;

            if (!useSceneAnchoredHudLayout)
                return;

            RectTransform scoreRect = currentScoreText.rectTransform;
            scoreRect.anchorMin = _currentScoreBaseAnchorMin;
            scoreRect.anchorMax = _currentScoreBaseAnchorMax;
            scoreRect.pivot = _currentScoreBasePivot;
            scoreRect.anchoredPosition = _currentScoreBaseAnchoredPosition;
        }

        private void RestoreBestScoreTextPosition()
        {
            if (!keepBestScoreAtInspectorPosition)
                return;

            if (!_hasCachedBestScoreAnchor || bestScoreText == null)
                return;

            RectTransform bestRect = bestScoreText.rectTransform;
            bestRect.anchorMin = _bestScoreBaseAnchorMin;
            bestRect.anchorMax = _bestScoreBaseAnchorMax;
            bestRect.pivot = _bestScoreBasePivot;
            bestRect.anchoredPosition = _bestScoreBaseAnchoredPosition;
        }

        private void Update()
        {
            if (!applySafeAreaLayout)
                return;

            if (HasScreenChanged() || HasBannerInsetChanged())
            {
                ApplyResponsiveHudLayout(force: true);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameBootstrap.OnScoreChanged -= OnScoreChanged;
            GameBootstrap.OnScoreBreakdown -= OnScoreBreakdown;
            GameBootstrap.OnGameStarted -= OnGameStarted;
            GameBootstrap.OnGameContinued -= OnGameContinued;
            GameBootstrap.OnGameOver -= OnGameOver;

            if (inGameMainMenuButton != null)
            {
                inGameMainMenuButton.onClick.RemoveListener(HandleInGameMainMenuClicked);
            }

            if (themeTestButton != null)
            {
                themeTestButton.onClick.RemoveListener(HandleThemeTestButtonClicked);
            }
            
            // Stop animations
            if (_scoreCountAnimation != null)
            {
                StopCoroutine(_scoreCountAnimation);
            }

            if (_scoreGlowAnimation != null)
            {
                StopCoroutine(_scoreGlowAnimation);
            }

            if (_statusMessageRoutine != null)
            {
                StopCoroutine(_statusMessageRoutine);
            }

            if (_comboVisibilityRoutine != null)
            {
                StopCoroutine(_comboVisibilityRoutine);
            }
        }
        
        private void InitializeDisplay()
        {
            SyncScoreDisplayFromBootstrap();
            UpdateScoreBreakdownDebug(default);
            UpdateGameInfo();
        }
        
        private void OnGameStarted()
        {
            SyncScoreDisplayFromBootstrap();
            UpdateScoreBreakdownDebug(default);
            ResetTargetGoalSystem();
            UpdateGameInfo();
            if (gameStatusText != null)
            {
                _baseStatusText = TrEn("Oynanıyor", "Playing");
                gameStatusText.text = _baseStatusText;
            }

            SetInGameMainMenuVisibility(true);
        }

        private void SyncScoreDisplayFromBootstrap()
        {
            ResolveRequiredDependencies();
            if (gameBootstrap == null)
            {
                UpdateScoreDisplay(0, 0, false);
                return;
            }

            UpdateScoreDisplay(gameBootstrap.CurrentScore, gameBootstrap.BestScore, false);
        }
        
        private void OnScoreChanged(int currentScore, int bestScore, bool isNewBest)
        {
            UpdateScoreDisplay(currentScore, bestScore, isNewBest);
            UpdateGameInfo();
        }

        private void OnGameContinued()
        {
            SyncScoreDisplayFromBootstrap();
            UpdateGameInfo();
        }

        private void OnScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            UpdateScoreBreakdownDebug(breakdown);
            PlayScoreReactiveGlow(breakdown.ScoreDelta);
            ShowMoveQualityFeedback(breakdown);

            if (enableHudComboDisplay)
                UpdateComboDisplayFromBreakdown(breakdown);
            else
                HideComboText();
        }
        
        private void OnGameOver(int finalScore)
        {
            if (gameStatusText != null)
            {
                _baseStatusText = TrEn("Oyun Bitti", "Game Over");
                gameStatusText.text = _baseStatusText;
            }
            UpdateGameInfo();

            HideComboText();

            if (hideInGameMenuOnGameOver)
            {
                SetInGameMainMenuVisibility(false);
            }
        }

        private void SetupInGameMainMenuButton()
        {
            if (inGameMainMenuButton == null)
            {
                var allButtons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < allButtons.Length; i++)
                {
                    var candidate = allButtons[i];
                    if (candidate == null)
                        continue;

                    string nameLower = candidate.name.ToLowerInvariant();
                    if (!nameLower.Contains("mainmenu"))
                        continue;

                    string pathLower = candidate.transform.GetHierarchyPath().ToLowerInvariant();
                    if (pathLower.Contains("gameover"))
                        continue;

                    inGameMainMenuButton = candidate;
                    break;
                }
            }

            if (inGameMainMenuButton == null)
                return;

            inGameMainMenuButton.onClick.RemoveListener(HandleInGameMainMenuClicked);
            inGameMainMenuButton.onClick.AddListener(HandleInGameMainMenuClicked);
            SetInGameMainMenuVisibility(true);
        }

        private void SetupThemeTestButton()
        {
            if (!enableThemeTestButton)
            {
                SetThemeTestButtonVisibility(false);
                return;
            }

            if (themeTestButton == null)
                themeTestButton = FindThemeTestButtonInChildren();

            if (themeTestButton == null)
                themeTestButton = CreateThemeTestButton();

            if (themeTestButton == null)
            {
                SetThemeTestButtonVisibility(false);
                Debug.LogWarning("[HudView] Theme button could not be created or found.");
                return;
            }

            themeTestButton.onClick.RemoveListener(HandleThemeTestButtonClicked);
            themeTestButton.onClick.AddListener(HandleThemeTestButtonClicked);
            SetThemeTestButtonVisibility(true);
        }

        private Button FindThemeTestButtonInChildren()
        {
            var allButtons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                var candidate = allButtons[i];
                if (candidate == null)
                    continue;

                string nameLower = candidate.name.ToLowerInvariant();
                if (nameLower.Contains("themetest") || nameLower.Contains("themebutton"))
                    return candidate;
            }

            return null;
        }

        private Button CreateThemeTestButton()
        {
            RectTransform root = transform as RectTransform;
            if (root == null)
                return null;

            var buttonGo = new GameObject("ThemeTestButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(root, false);

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.sizeDelta = themeTestButtonSize;

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.09f, 0.21f, 0.27f, 0.92f);
            image.raycastTarget = true;

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(buttonGo.transform, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TMP_FontAsset font = ResolveHudFont();
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.97f, 0.93f, 0.86f, 1f);
            label.raycastTarget = false;
            label.text = string.IsNullOrWhiteSpace(themeTestButtonLabel) ? "TEMA" : themeTestButtonLabel;

            _themeTestButtonText = label;
            return button;
        }

        private void HandleInGameMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void CycleThemeForTesting()
        {
            if (!enableThemeTestButton)
                return;

            // The gameplay theme system contains nine themes. Keep the legacy
            // HUD button on the same cycle so it cannot get stuck on themes 1-3.
            int currentThemeId = UISettingsProfile.GetThemeId();
            int nextThemeId = currentThemeId >= UISettingsProfile.ThemeClassic &&
                              currentThemeId <= UISettingsProfile.ThemeWood
                ? (currentThemeId + 1) % 4
                : UISettingsProfile.ThemeClassic;

            if (_gameSceneThemeController == null)
                _gameSceneThemeController = GameSceneThemeController.GetOrCreateRuntimeController();

            if (_gameSceneThemeController != null)
                _gameSceneThemeController.ApplyManualThemeById(nextThemeId);
            else
                UISettingsProfile.SetThemeId(nextThemeId);

            string themeName = nextThemeId switch
            {
                UISettingsProfile.ThemeClassic => "Klasik",
                UISettingsProfile.ThemeNight => "Gece",
                UISettingsProfile.ThemeVivid => "Doğa",
                UISettingsProfile.ThemeWood => "Ahşap",
                _ => "Theme"
            };

            ShowTransientStatusMessage($"Theme: {themeName}", 1.25f);
        }

        private void HandleThemeTestButtonClicked()
        {
            CycleThemeForTesting();
        }

        private void SetInGameMainMenuVisibility(bool isVisible)
        {
            if (inGameMainMenuButton == null)
                return;

            inGameMainMenuButton.gameObject.SetActive(isVisible);
        }

        private void SetThemeTestButtonVisibility(bool isVisible)
        {
            if (themeTestButton == null)
                return;

            themeTestButton.gameObject.SetActive(isVisible);
        }
        
        private void UpdateScoreDisplay(int currentScore, int bestScore, bool isNewBest)
        {
            HudPresentationPresenter.UpdateScoreDisplay(this, currentScore, bestScore, isNewBest);
        }

        private void ApplyScoreBreakdownDebugVisibility()
        {
            if (scoreBreakdownPanel != null)
            {
                scoreBreakdownPanel.SetActive(showScoreBreakdownDebug);
            }
            else if (scoreBreakdownText != null)
            {
                scoreBreakdownText.gameObject.SetActive(showScoreBreakdownDebug);
            }
        }

        private void UpdateScoreBreakdownDebug(ScoreBreakdownInfo breakdown)
        {
            HudPresentationPresenter.UpdateScoreBreakdownDebug(this, breakdown);
        }

        private void PlayScoreReactiveGlow(int scoreDelta)
        {
            if (!enableScoreReactiveGlow || currentScoreText == null)
                return;

            if (UISettingsProfile.IsReduceMotionEnabled())
                return;

            float intensity = EvaluateGlowIntensity(scoreDelta);
            if (intensity <= 0f)
                return;

            if (_scoreGlowAnimation != null)
                StopCoroutine(_scoreGlowAnimation);

            _scoreGlowAnimation = StartCoroutine(AnimateScoreGlow(intensity));
        }

        private float EvaluateGlowIntensity(int scoreDelta)
        {
            float normalized = scoreDelta <= 0
                ? 0f
                : Mathf.Clamp01(scoreDelta / Mathf.Max(1f, glowNormalizationMaxScoreDelta));

            float curved = glowIntensityCurve != null
                ? Mathf.Clamp01(glowIntensityCurve.Evaluate(normalized))
                : normalized;

            return Mathf.Clamp01(Mathf.Lerp(glowMinAlpha, glowMaxAlpha, curved));
        }
        
        private void UpdateGameInfo()
        {
            HudPresentationPresenter.UpdateGameInfo(this);
        }

        private void ResetTargetGoalSystem()
        {
            HudPresentationPresenter.ResetTargetGoalSystem(this);
        }

        private void ApplyComboVisualStyle(TextMeshProUGUI target)
        {
            if (target == null)
                return;

            if (useSceneAnchoredHudLayout)
                return;

            target.fontStyle = comboStyleBold ? FontStyles.Bold : FontStyles.Normal;
            target.fontSize = comboStyleFontSize;
            if (comboStyleFont != null)
            {
                target.font = comboStyleFont;
                target.fontSharedMaterial = comboStyleFont.material;
            }

            target.enableVertexGradient = comboStyleUseGradient;
            if (comboStyleUseGradient)
            {
                target.colorGradient = new VertexGradient(
                    comboStyleTopColor,
                    comboStyleTopColor,
                    comboStyleBottomColor,
                    comboStyleBottomColor
                );
            }

            target.outlineWidth = comboStyleUseOutline ? comboStyleOutlineWidth : 0f;
            target.outlineColor = comboStyleOutlineColor;
            target.color = Color.white;

            var currentMat = target.fontSharedMaterial;
            if (currentMat != null)
            {
                var matInstance = new Material(currentMat);

                if (matInstance.HasProperty(ShaderUtilities.ID_FaceColor))
                    matInstance.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

                if (matInstance.HasProperty(ShaderUtilities.ID_OutlineColor))
                    matInstance.SetColor(ShaderUtilities.ID_OutlineColor, comboStyleOutlineColor);

                if (comboStyleUseUnderlay && matInstance.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    matInstance.SetColor(ShaderUtilities.ID_UnderlayColor, comboStyleUnderlayColor);

                if (comboStyleUseUnderlay && matInstance.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    matInstance.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, comboStyleUnderlayOffset.x);

                if (comboStyleUseUnderlay && matInstance.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    matInstance.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, comboStyleUnderlayOffset.y);

                if (comboStyleUseUnderlay && matInstance.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    matInstance.SetFloat(ShaderUtilities.ID_UnderlayDilate, comboStyleUnderlayDilate);

                if (!comboStyleUseUnderlay && matInstance.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    matInstance.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);

                target.fontSharedMaterial = matInstance;
            }
        }

        private void UpdateComboDisplayFromBreakdown(ScoreBreakdownInfo breakdown)
        {
            HudPresentationPresenter.UpdateComboDisplayFromBreakdown(this, breakdown);
        }

        private IEnumerator HideComboAfterDelay(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, delay));
            HideComboText();
            _comboVisibilityRoutine = null;
        }

        private void HideComboText()
        {
            HudPresentationPresenter.HideComboText(this);
        }
        
        private System.Collections.IEnumerator AnimateScoreCount()
        {
            int startScore = _displayedScore;
            int endScore = _targetScore;
            float duration = UISettingsProfile.IsReduceMotionEnabled()
                ? Mathf.Min(0.12f, scoreCountDuration)
                : scoreCountDuration;
            
            if (startScore == endScore)
            {
                yield break;
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float t = duration <= 0.0001f ? 1f : (elapsedTime / duration);
                float curveValue = scoreCountCurve.Evaluate(t);
                
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, curveValue));
                
                if (currentScoreText != null)
                {
                    currentScoreText.text = $"{_displayedScore:N0}";
                    RestoreCurrentScoreTextPosition();
                }
                
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // Ensure final value
            _displayedScore = endScore;
            if (currentScoreText != null)
            {
                currentScoreText.text = $"{_displayedScore:N0}";
                RestoreCurrentScoreTextPosition();
            }
        }

        private System.Collections.IEnumerator AnimateScoreGlow(float intensity)
        {
            if (currentScoreText == null)
                yield break;

            Color original = currentScoreText.color;
            Color target = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Clamp01(intensity));
            target = ProjectColorGrading.Apply(target);

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, glowDuration);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float alphaT = 1f - t;
                currentScoreText.color = Color.Lerp(original, target, alphaT);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentScoreText.color = original;
            _scoreGlowAnimation = null;
        }
        
        private System.Collections.IEnumerator FlashNewBest()
        {
            if (bestScoreText == null) yield break;
            
            Color originalColor = bestScoreText.color;
            Color flashColor = ProjectColorGrading.Apply(Color.yellow);
            
            // Flash sequence
            for (int i = 0; i < 3; i++)
            {
                bestScoreText.color = flashColor;
                yield return new WaitForSeconds(0.1f);
                bestScoreText.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// Shows a temporary message on the HUD.
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="duration">How long to show the message</param>
        public void ShowMessage(string message, float duration = 2f)
        {
            if (gameStatusText != null)
            {
                ShowTransientStatusMessage(message, duration);
            }
        }

        private void ShowMoveQualityFeedback(ScoreBreakdownInfo breakdown)
        {
            HudPresentationPresenter.ShowMoveQualityFeedback(this, breakdown);
        }

        private void ShowTransientStatusMessage(string message, float duration)
        {
            if (gameStatusText == null)
                return;

            if (_statusMessageRoutine != null)
                StopCoroutine(_statusMessageRoutine);

            _statusMessageRoutine = StartCoroutine(ShowTemporaryMessage(message, duration));
        }

        public void SetScoreBreakdownDebugVisible(bool visible)
        {
            showScoreBreakdownDebug = visible;
            ApplyScoreBreakdownDebugVisibility();
        }
        
        private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            string originalText = string.IsNullOrEmpty(_baseStatusText) ? gameStatusText.text : _baseStatusText;
            gameStatusText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            gameStatusText.text = originalText;
            _statusMessageRoutine = null;
        }

        private static string ResolveComboLabel(int comboStreak)
        {
            if (comboStreak >= 8)
                return TrEn("İnanılmaz", "Incredible");
            if (comboStreak >= 5)
                return TrEn("Efsane Seri", "Legendary Streak");
            if (comboStreak >= 3)
                return TrEn("Harika Seri", "Great Streak");
            if (comboStreak >= 2)
                return TrEn("Alev Aldı", "On Fire");

            return TrEn("Combo", "Combo");
        }

        private static string ResolveSafeComboLabel(int comboStreak)
        {
            if (comboStreak >= 8)
                return TrEn("Inanilmaz", "Incredible");
            if (comboStreak >= 5)
                return TrEn("Efsane Seri", "Legendary Streak");
            if (comboStreak >= 3)
                return TrEn("Harika Seri", "Great Streak");
            if (comboStreak >= 2)
                return TrEn("Alev Aldi", "On Fire");

            return TrEn("Combo", "Combo");
        }

        private static string ResolveMoveQualityLabel(ScoreBreakdownInfo breakdown)
        {
            if (breakdown.ScoreDelta >= 100 || breakdown.LinesCleared >= 3)
                return TrEn("Mükemmel hamle!", "Perfect move!");
            if (breakdown.ScoreDelta >= 50 || breakdown.LinesCleared >= 2 || breakdown.ComboStreak >= 4)
                return TrEn("Harika hamle!", "Great move!");
            if (breakdown.ScoreDelta >= 20 || breakdown.LinesCleared >= 1)
                return TrEn("Temiz hamle", "Clean move");

            return TrEn("İyi hamle", "Nice move");
        }

        private static bool IsEnglishSelected()
        {
            return LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.English;
        }

        private static readonly Dictionary<string, string> KoreanTranslations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Oynanıyor", "게임 중" },
            { "Playing", "게임 중" },
            { "Oyun Bitti", "게임 오버" },
            { "Game Over", "게임 오버" },
            { "İnanılmaz", "놀라워요!" },
            { "Inanilmaz", "놀라워요!" },
            { "Incredible", "놀라워요!" },
            { "Efsane Seri", "전설적인 스트릭!" },
            { "Legendary Streak", "전설적인 스트릭!" },
            { "Harika Seri", "대단한 스트릭!" },
            { "Great Streak", "대단한 스트릭!" },
            { "Alev Aldı", "불타오르네!" },
            { "Alev Aldi", "불타오르네!" },
            { "On Fire", "불타오르네!" },
            { "Combo", "콤보" },
            { "Mükemmel hamle!", "완벽한 수!" },
            { "Perfect move!", "완벽한 수!" },
            { "Harika hamle!", "멋진 수!" },
            { "Great move!", "멋진 수!" },
            { "Temiz hamle", "깔끔한 수!" },
            { "Clean move", "깔끔한 수!" },
            { "İyi hamle", "좋은 수!" },
            { "Nice move", "좋은 수!" },
            { "Hamle", "이동" },
            { "Move", "이동" }
        };

        private static string TrEn(string turkish, string english)
        {
            if (LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Korean)
            {
                if (!string.IsNullOrEmpty(english) && KoreanTranslations.TryGetValue(english, out string koTranslation))
                    return koTranslation;
                if (!string.IsNullOrEmpty(turkish) && KoreanTranslations.TryGetValue(turkish, out string koTranslation2))
                    return koTranslation2;
                return !string.IsNullOrEmpty(english) ? english : turkish;
            }

            return IsEnglishSelected() ? english : turkish;
        }

        private bool HasScreenChanged()
        {
            Rect safeArea = Screen.safeArea;
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                return true;

            const float threshold = 0.5f;
            return Mathf.Abs(safeArea.x - _lastSafeArea.x) > threshold ||
                   Mathf.Abs(safeArea.y - _lastSafeArea.y) > threshold ||
                   Mathf.Abs(safeArea.width - _lastSafeArea.width) > threshold ||
                   Mathf.Abs(safeArea.height - _lastSafeArea.height) > threshold;
        }

        private void CacheScreenState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
            _lastBannerBottomInsetPixels = GetBottomBannerInsetPixels();
        }

        private bool HasBannerInsetChanged()
        {
            return GetBottomBannerInsetPixels() != _lastBannerBottomInsetPixels;
        }

        private Vector2 GetBottomInsetPadding()
        {
            float bannerInsetUnits = 0f;
            if (reserveSpaceForBottomBanner)
            {
                int insetPixels = GetBottomBannerInsetPixels();
                if (insetPixels > 0)
                    bannerInsetUnits = ConvertScreenPixelsToCanvasUnits(insetPixels) + bottomBannerSpacing;
            }

            return new Vector2(bottomPadding.x, bottomPadding.y + bannerInsetUnits);
        }

        private int GetBottomBannerInsetPixels()
        {
            if (!reserveSpaceForBottomBanner)
                return 0;

            var managerType = System.Type.GetType("AdMobManager");
            if (managerType == null)
                return 0;

            var instanceProperty = managerType.GetProperty("ExistingInstance", BindingFlags.Public | BindingFlags.Static);
            var manager = instanceProperty?.GetValue(null);
            if (manager == null)
                return 0;

            var visibleProperty = managerType.GetProperty("IsBannerVisible", BindingFlags.Public | BindingFlags.Instance);
            bool isVisible = visibleProperty != null && (bool)visibleProperty.GetValue(manager);
            if (!isVisible)
                return 0;

            var heightProperty = managerType.GetProperty("CurrentBannerOccupiedHeightInPixels", BindingFlags.Public | BindingFlags.Instance);
            if (heightProperty == null)
                return 0;

            int occupiedHeight = (int)heightProperty.GetValue(manager);
            return Mathf.Max(0, occupiedHeight);
        }

        private float ConvertScreenPixelsToCanvasUnits(float screenPixels)
        {
            if (screenPixels <= 0f)
                return 0f;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            float scaleFactor = rootCanvas != null ? Mathf.Max(0.0001f, rootCanvas.scaleFactor) : 1f;
            return screenPixels / scaleFactor;
        }

        private void ApplyResponsiveHudLayout(bool force)
        {
            HudLayoutPresenter.ApplyResponsiveHudLayout(this, force);
        }

        private void ApplySceneAnchoredHudLayout(float bannerInsetOffset)
        {
            if (!_hasCachedSceneAnchors)
                CacheSceneAnchoredPositions();

            ApplyBottomInsetToRect(turnCountText, _turnCountBaseAnchoredPosition, bannerInsetOffset);
            ApplyBottomInsetToRect(gameStatusText, _gameStatusBaseAnchoredPosition, bannerInsetOffset);
        }

        private static void ApplyBottomInsetToRect(TextMeshProUGUI text, Vector2 baseAnchoredPosition, float bannerInsetOffset)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = new Vector2(baseAnchoredPosition.x, baseAnchoredPosition.y + bannerInsetOffset);
        }

        private void SetToTopLeft(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(padding.x, -padding.y);
        }

        private void SetToTopRight(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-padding.x, -padding.y);
        }

        private void SetToTopCenter(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(padding.x, -padding.y);
        }

        private void SetToBottomRight(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-padding.x, padding.y);
        }

        private void SetToBottomLeft(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(padding.x, padding.y);
        }

        private void StretchHudPanelToSafeArea()
        {
            var panel = transform as RectTransform;
            if (panel == null)
                return;

            Rect safe = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);

            Vector2 min = new Vector2(safe.xMin / width, safe.yMin / height);
            Vector2 max = new Vector2(safe.xMax / width, safe.yMax / height);

            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.localScale = Vector3.one;
            panel.localRotation = Quaternion.identity;
        }

        private void OnValidate()
        {
            glowNormalizationMaxScoreDelta = Mathf.Max(1f, glowNormalizationMaxScoreDelta);
            glowDuration = Mathf.Max(0.05f, glowDuration);
            glowMinAlpha = Mathf.Clamp01(glowMinAlpha);
            glowMaxAlpha = Mathf.Clamp01(glowMaxAlpha);
            if (glowMaxAlpha < glowMinAlpha)
                glowMaxAlpha = glowMinAlpha;

#if UNITY_EDITOR
            if (gameBootstrap == null)
                gameBootstrap = TryAutoAssignSingleton<GameBootstrap>();

            bool shouldRepairSceneHud =
                useSceneAnchoredHudLayout ||
                currentScoreText == null ||
                bestScoreText == null ||
                turnCountText == null ||
                gameStatusText == null;

            if (!Application.isPlaying && shouldRepairSceneHud)
                EditorEnsureSceneHudReferences();
#endif
        }

        private void ResolveRequiredDependencies()
        {
            if (gameBootstrap == null)
            {
                gameBootstrap = FindFirstObjectByType<GameBootstrap>();
                if (gameBootstrap != null && !_loggedBootstrapFallbackWarning)
                {
                    _loggedBootstrapFallbackWarning = true;
                    Debug.LogWarning("[HudView] gameBootstrap was resolved via runtime lookup. Inspector wiring is the preferred production path.");
                }
            }

            if (gameBootstrap == null && !_loggedMissingDependencyWarning)
            {
                _loggedMissingDependencyWarning = true;
                Debug.LogWarning("[HudView] Required dependency missing: gameBootstrap. HUD will degrade until scene wiring is fixed.");
            }
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        private void ApplyUserAccessibilityPreferences()
        {
            applySafeAreaLayout = true;

            if (UISettingsProfile.IsReduceMotionEnabled())
            {
                enableScoreReactiveGlow = false;
                scoreCountDuration = Mathf.Min(scoreCountDuration, 0.12f);
                comboDisplayDuration = Mathf.Min(comboDisplayDuration, 0.7f);
            }
        }

        private void EnsureHudElements()
        {
            RectTransform root = transform as RectTransform;
            if (root == null)
                return;

            TMP_FontAsset defaultFont = ResolveHudFont();
            TMP_FontAsset comboFont = comboStyleFont != null ? comboStyleFont : defaultFont;

            if (useSceneAnchoredHudLayout)
            {
                currentScoreText = ResolveSceneHudText(root, currentScoreText, "ScoreText", "CurrentScoreText");
                bestScoreText = ResolveSceneHudText(root, bestScoreText, "BestScoreText");
                targetProgressText = ResolveSceneHudText(root, targetProgressText, "TargetProgressText");
                turnCountText = ResolveSceneHudText(root, turnCountText, "TurnCountText");
                gameStatusText = ResolveSceneHudText(root, gameStatusText, "GameStatusText");
                comboText = ResolveSceneHudText(root, comboText, "ComboText");
            }
            else
            {
                currentScoreText = EnsureTextElement(root, currentScoreText, "ScoreText", defaultFont, 50f, new Color(1f, 0.95f, 0.72f, 1f), FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(380f, 60f), "Skor: 0");
                bestScoreText = EnsureTextElement(root, bestScoreText, "BestScoreText", defaultFont, 22f, new Color(0.92f, 0.94f, 1f, 0.94f), FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(320f, 36f), "0");
                targetProgressText = EnsureTextElement(root, targetProgressText, "TargetProgressText", defaultFont, 20f, new Color(0.88f, 0.95f, 1f, 0.94f), FontStyles.Bold, TextAlignmentOptions.Top, new Vector2(320f, 60f), "Hedef 500\nKalan 500");
                turnCountText = EnsureTextElement(root, turnCountText, "TurnCountText", defaultFont, 22f, new Color(0.87f, 0.91f, 1f, 0.9f), FontStyles.Bold, TextAlignmentOptions.BottomRight, new Vector2(240f, 42f), "Hamle: 0");
                gameStatusText = EnsureTextElement(root, gameStatusText, "GameStatusText", defaultFont, 22f, new Color(0.98f, 0.98f, 1f, 0.95f), FontStyles.Bold, TextAlignmentOptions.BottomLeft, new Vector2(280f, 42f), _baseStatusText);
                comboText = EnsureTextElement(root, comboText, "ComboText", comboFont, 74f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(360f, 110f), "Combo 2");

                ApplyHudTextPreset(currentScoreText, defaultFont, 50f, new Color(1f, 0.95f, 0.72f, 1f), FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(380f, 60f), "Skor: 0");
                ApplyHudTextPreset(bestScoreText, defaultFont, 22f, new Color(0.92f, 0.94f, 1f, 0.94f), FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(320f, 36f), "0");
                ApplyHudTextPreset(targetProgressText, defaultFont, 20f, new Color(0.88f, 0.95f, 1f, 0.94f), FontStyles.Bold, TextAlignmentOptions.Top, new Vector2(320f, 60f), "Hedef 500\nKalan 500");
                ApplyHudTextPreset(turnCountText, defaultFont, 22f, new Color(0.87f, 0.91f, 1f, 0.9f), FontStyles.Bold, TextAlignmentOptions.BottomRight, new Vector2(240f, 42f), "Hamle: 0");
                ApplyHudTextPreset(gameStatusText, defaultFont, 22f, new Color(0.98f, 0.98f, 1f, 0.95f), FontStyles.Bold, TextAlignmentOptions.BottomLeft, new Vector2(280f, 42f), _baseStatusText);
                ApplyHudTextPreset(comboText, comboFont, 74f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(360f, 110f), "Combo 2");
            }

            if (comboText != null)
                comboText.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI ResolveSceneHudText(RectTransform parent, TextMeshProUGUI existing, params string[] names)
        {
            if (existing != null)
                return existing;

            for (int i = 0; i < names.Length; i++)
            {
                Transform found = parent.Find(names[i]);
                if (found == null)
                    continue;

                TextMeshProUGUI text = found.GetComponent<TextMeshProUGUI>();
                if (text != null)
                    return text;
            }

            return null;
        }

#if UNITY_EDITOR
        private void EditorEnsureSceneHudReferences()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            RectTransform root = transform as RectTransform;
            if (root == null || !gameObject.scene.IsValid())
                return;

            TMP_FontAsset defaultFont = ResolveHudFont();
            TMP_FontAsset comboFont = comboStyleFont != null ? comboStyleFont : defaultFont;
            bool changed = false;

            if (!useSceneAnchoredHudLayout)
            {
                useSceneAnchoredHudLayout = true;
                changed = true;
            }

            if (!applySafeAreaLayout)
            {
                applySafeAreaLayout = true;
                changed = true;
            }

            currentScoreText = EditorEnsureHudText(
                root,
                currentScoreText,
                "ScoreText",
                "CurrentScoreText",
                defaultFont,
                50f,
                new Color(1f, 0.95f, 0.72f, 1f),
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                new Vector2(380f, 60f),
                "Skor: 0",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -28f),
                0,
                false,
                ref changed);

            bestScoreText = EditorEnsureHudText(
                root,
                bestScoreText,
                "BestScoreText",
                null,
                defaultFont,
                22f,
                new Color(0.92f, 0.94f, 1f, 0.94f),
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                new Vector2(320f, 36f),
                "En \u0130yi: 0",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -86f),
                1,
                true,
                ref changed);

            targetProgressText = EditorEnsureHudText(
                root,
                targetProgressText,
                "TargetProgressText",
                null,
                defaultFont,
                20f,
                new Color(0.88f, 0.95f, 1f, 0.94f),
                FontStyles.Bold,
                TextAlignmentOptions.Top,
                new Vector2(320f, 60f),
                "Hedef 500\nKalan 500",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -28f),
                2,
                false,
                ref changed);

            turnCountText = EditorEnsureHudText(
                root,
                turnCountText,
                "TurnCountText",
                null,
                defaultFont,
                22f,
                new Color(0.87f, 0.91f, 1f, 0.9f),
                FontStyles.Bold,
                TextAlignmentOptions.BottomRight,
                new Vector2(240f, 42f),
                "Hamle: 0",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-32f, 64f),
                3,
                false,
                ref changed);

            gameStatusText = EditorEnsureHudText(
                root,
                gameStatusText,
                "GameStatusText",
                null,
                defaultFont,
                22f,
                new Color(0.98f, 0.98f, 1f, 0.95f),
                FontStyles.Bold,
                TextAlignmentOptions.BottomLeft,
                new Vector2(280f, 42f),
                _baseStatusText,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(32f, 64f),
                4,
                false,
                ref changed);

            comboText = ResolveSceneHudText(root, comboText, "ComboText");

            if (changed)
            {
                CacheSceneAnchoredPositions();
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        private TextMeshProUGUI EditorEnsureHudText(
            RectTransform parent,
            TextMeshProUGUI existing,
            string primaryName,
            string legacyName,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 size,
            string defaultText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            int siblingIndex,
            bool keepExistingParent,
            ref bool changed)
        {
            TextMeshProUGUI text = existing;
            if (text == null)
                text = ResolveSceneHudText(parent, null, legacyName == null ? new[] { primaryName } : new[] { primaryName, legacyName });

            bool created = false;
            if (text == null)
            {
                var go = new GameObject(primaryName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, $"Create {primaryName}");
                go.transform.SetParent(parent, false);
                text = go.GetComponent<TextMeshProUGUI>();
                created = true;
                changed = true;
            }

            bool shouldRename =
                created ||
                (!string.IsNullOrEmpty(legacyName) && text.gameObject.name == legacyName);

            if (shouldRename && text.gameObject.name != primaryName)
            {
                text.gameObject.name = primaryName;
                changed = true;
            }

            if (!keepExistingParent && text.transform.parent != parent)
            {
                text.transform.SetParent(parent, false);
                changed = true;
            }

            if (created && text.transform.GetSiblingIndex() != siblingIndex)
            {
                text.transform.SetSiblingIndex(siblingIndex);
                changed = true;
            }

            if (created)
            {
                ApplyHudTextPreset(text, font, fontSize, color, fontStyle, alignment, size, defaultText);

                RectTransform rect = text.rectTransform;
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.anchoredPosition = anchoredPosition;
                rect.localScale = Vector3.one;
            }

            return text;
        }
#endif

        private static void ApplyHudTextPreset(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 size,
            string defaultText)
        {
            if (text == null)
                return;

            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.text = defaultText;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.05f, 0.11f, 0.28f, 0.9f);
        }

        private TMP_FontAsset ResolveHudFont()
        {
            if (currentScoreText != null && currentScoreText.font != null)
                return currentScoreText.font;

            var anyText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (anyText != null && anyText.font != null)
                return anyText.font;

            return TMP_Settings.defaultFontAsset;
        }

        private static TextMeshProUGUI EnsureTextElement(
            RectTransform parent,
            TextMeshProUGUI existing,
            string name,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 size,
            string defaultText)
        {
            TextMeshProUGUI text = existing;
            if (text == null)
            {
                Transform found = parent.Find(name);
                text = found != null ? found.GetComponent<TextMeshProUGUI>() : null;
            }

            if (text == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                text = go.GetComponent<TextMeshProUGUI>();
            }

            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = fontStyle;
            text.text = defaultText;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.05f, 0.11f, 0.28f, 0.9f);
            return text;
        }

        /// <summary>
        /// HUD metin bileşenlerine LocalizedText kurulumu yapısır
        /// </summary>
        private void SetupHudLocalization()
        {
            // En Yüksek Skor başlığı
            if (bestScoreText != null)
                LocalizedTextSetup.SetupLocalization(bestScoreText, "En Yüksek Skor: ", "Best Score: ");

            // Kombo başlığı
            if (comboText != null)
                LocalizedTextSetup.SetupLocalization(comboText, "Kombo: ", "Combo: ");

            // Tur sayısı başlığı
            if (turnCountText != null)
                LocalizedTextSetup.SetupLocalization(turnCountText, "Tur: ", "Turn: ");

            // Oyun durumu metni
            if (gameStatusText != null)
                LocalizedTextSetup.SetupLocalization(gameStatusText, "Oynanıyor", "Playing");

            Debug.Log("[HudView] HUD lokalizasyon kurulumu tamamlandı!");
        }
    }

    internal static class TransformPathExtensions
    {
        public static string GetHierarchyPath(this Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
