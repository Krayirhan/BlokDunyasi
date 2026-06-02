// File: UnityAdapter/UI/GameOverView.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Animation;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.UI.Localization;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using Debug = BlockPuzzle.Core.Common.GameLogger;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// GameOver ekranını yönetir.
    /// Script GameOverPanel üzerinde duruyorsa panel kapalıyken Start() çalışmayabilir;
    /// bu yüzden event subscribe ve button binding Awake() içinde yapılır.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        private const int DefaultContinueOffersPerRun = 1;

        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI newBestText;
        [SerializeField] private TextMeshProUGUI sessionSummaryText;
        [SerializeField] private TextMeshProUGUI nextTryTitleText;
        [SerializeField] private TextMeshProUGUI guidanceHintText;
        [SerializeField] private TextMeshProUGUI bestMoveValueText;
        [SerializeField] private TextMeshProUGUI maxComboValueText;
        [SerializeField] private TextMeshProUGUI totalLinesValueText;
        [SerializeField] private TextMeshProUGUI averageMoveValueText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button rescueButton;

        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = SceneCatalog.MainMenu;
        [SerializeField] private string gameSceneName = SceneCatalog.Game;
        [SerializeField] private bool useSeparateGameOverScene = false;
        [SerializeField] private string gameOverSceneName = SceneCatalog.GameOver;
        [SerializeField] private bool buildDedicatedSceneLayoutAtRuntime = false;

        [Header("Continue Offer")]
        [SerializeField] private bool enableContinueOffer = true;
        [SerializeField] private bool allowOneContinuePerRun = true;
        [SerializeField] [Min(0)] private int maxContinueOffersPerRun = 1;
        [SerializeField] [Min(1f)] private float continueCountdownSeconds = 5f;
        [SerializeField] private GameObject continueOfferPanel;
        [SerializeField] private TextMeshProUGUI noMovesLabel;
        [SerializeField] private TextMeshProUGUI continueCountdownText;
        [SerializeField] private Button continueButton;
        [SerializeField] [Min(2f)] private float rewardedLoadTimeoutSeconds = 8f;
        [SerializeField] private string rewardedLoadingMessage = "Reklam yükleniyor...";
        [SerializeField] private string rewardedOpeningMessage = "Reklam açılıyor...";
        [SerializeField] private string rewardedLoadFailedMessage = "Reklam şu anda kullanılamıyor.";
        [SerializeField] private string noMovesMessage = "Hamlen Kalmadi!";
        [SerializeField] private string rewardedLoadingMessageEnglish = "Loading ad...";
        [SerializeField] private string rewardedOpeningMessageEnglish = "Opening ad...";
        [SerializeField] private string rewardedLoadFailedMessageEnglish = "Ad is currently unavailable.";
        [SerializeField] private string noMovesMessageEnglish = "No moves left!";

        [Header("Final Explosion VFX")]
        [SerializeField] private bool playBoardExplosionOnFinalGameOver = false;
        [SerializeField] [Min(0.1f)] private float boardExplosionDuration = 2f;
        [SerializeField] private bool finalExplosionRandomizeOrder = true;
        [SerializeField] [Min(1)] private int finalExplosionCellsPerTick = 3;
        [SerializeField] [Min(1)] private int finalExplosionCenterBurstCombo = 4;
        [SerializeField] [Min(1)] private int finalExplosionPerCellBurstCombo = 6;
        [SerializeField] private bool finalExplosionEmitLineClearBursts = true;
        [SerializeField] [Range(0f, 1f)] private float finalExplosionLineBurstChance = 1f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [SerializeField] private GameBootstrap _gameBootstrap;
        private CanvasGroup _canvasGroup;
        private bool _subscribed;
        private int _sessionHighestCombo;
        private int _sessionBestMoveDelta;
        private int _continuesUsedThisRun;
        private bool _continueOfferActive;
        private bool _waitingRewardedResult;
        private bool _queuedRewardedAdShow;
        private bool _rewardEarned;
        private bool _isFinishingGameOver;
        private bool _restartActsAsContinue;
        private int _pendingFinalScore;
        private Coroutine _continueCountdownRoutine;
        private Coroutine _rewardedLoadTimeoutRoutine;
        private float _continueCountdownRemaining;
        private bool _adCallbacksHooked;
        [SerializeField] private SimpleGridView _gridView;
        private string _lastTrackedGuidanceCode = string.Empty;
        private string _lastTrackedRiskSnapshotCode = string.Empty;
        private bool CanLog => verboseLogs && Debug.isDebugBuild;
        private bool _loggedBootstrapFallbackWarning;
        private bool _loggedGridViewFallbackWarning;
        private bool _loggedDependencyWarning;

        private struct BoardExplosionBurst
        {
            public readonly int X;
            public readonly int Y;
            public readonly Vector3 WorldPosition;
            public readonly Color Color;
            public readonly float DistanceToCenter;

            public BoardExplosionBurst(int x, int y, Vector3 worldPosition, Color color, float distanceToCenter)
            {
                X = x;
                Y = y;
                WorldPosition = worldPosition;
                Color = color;
                DistanceToCenter = distanceToCenter;
            }
        }

        private void Awake()
        {
            NormalizeContinueOfferSettings();

            // 1) Panel referansı yoksa "kendi GameObject'i" panel kabul et
            if (gameOverPanel == null)
                gameOverPanel = this.gameObject;
            EnsurePanelOnTop();

            // 2) CanvasGroup garanti
            _canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();

            // 3) Bootstrap bul
            EnsureBootstrap();
            EnsurePanelOnTop();

            // 4) Dedicated GameOver scene için zengin layout kur
            TryBuildDedicatedSceneLayout();
            TryBuildInGameContinueOfferLayout();

            EnsureEventSystemIfMissing();

            // 4) UI referanslarını (opsiyonel) otomatik bul
            AutoWireIfMissing();

            ResolveContinueDependencies();

            // 5) Event'lere subscribe
            SubscribeOnce();

            // 6) Button listener'ları
            SetupButtonListeners();
            ConfigureInteractiveUi();

            // 7) Başlangıçta paneli gizle
            HideGameOverScreenImmediate();

            // 8) UI altyapısını diagnostik et
            DiagnoseUiPipeline("Awake");

            // 9) Lokalizasyon setup'ını yap
            SetupGameLocalization();
        }

        private void Start()
        {
            TryShowDedicatedScenePayload();
        }

        private void OnDestroy()
        {
            UnsubscribeOnce();
            RemoveButtonListeners();
            UnhookAdCallbacks();
        }

        // -------------------------------------------------------
        // Subscriptions
        // -------------------------------------------------------

        private void SubscribeOnce()
        {
            if (_subscribed) return;

            if (IsDedicatedGameOverScene())
            {
                if (CanLog)
                    Debug.Log("[GameOverView] Dedicated GameOver scene active. Gameplay event subscriptions skipped.");
                return;
            }

            GameBootstrap.OnGameOver += OnGameOver;
            GameBootstrap.OnGameStarted += OnGameStarted;
            _subscribed = true;
            GameBootstrap.OnScoreBreakdown += OnScoreBreakdown;

            if (CanLog)
                Debug.Log("[GameOverView] Subscribed to GameBootstrap.OnGameOver / OnGameStarted");
        }

        private void UnsubscribeOnce()
        {
            if (!_subscribed) return;

            GameBootstrap.OnGameOver -= OnGameOver;
            GameBootstrap.OnGameStarted -= OnGameStarted;
            _subscribed = false;
            GameBootstrap.OnScoreBreakdown -= OnScoreBreakdown;

            if (CanLog)
                Debug.Log("[GameOverView] Unsubscribed events");
        }

        // -------------------------------------------------------
        // UI wiring / diagnostics
        // -------------------------------------------------------

        private void EnsureBootstrap()
        {
            if (_gameBootstrap == null)
            {
                _gameBootstrap = FindFirstObjectByType<GameBootstrap>();
                if (_gameBootstrap != null && !_loggedBootstrapFallbackWarning)
                {
                    _loggedBootstrapFallbackWarning = true;
                    Debug.LogWarning("[GameOverView] _gameBootstrap was resolved via runtime lookup. Inspector wiring is the preferred production path.");
                }
            }

            if (CanLog)
                Debug.Log($"[GameOverView] Bootstrap: {(_gameBootstrap != null ? "FOUND" : "NULL")}");

            if (_gameBootstrap == null && !_loggedDependencyWarning)
            {
                _loggedDependencyWarning = true;
                Debug.LogWarning("[GameOverView] Required dependency missing: _gameBootstrap. Scene wiring should be fixed.");
            }
        }

        private void AutoWireIfMissing()
        {
            if (gameOverPanel == null) return;

            if (finalScoreText == null)
                finalScoreText = FindTMP(gameOverPanel.transform, "FinalScoreText");

            if (bestScoreText == null)
                bestScoreText = FindTMP(gameOverPanel.transform, "BestScoreText");

            if (newBestText == null)
                newBestText = FindTMP(gameOverPanel.transform, "NewBestText");

            if (sessionSummaryText == null)
                sessionSummaryText = FindTMP(gameOverPanel.transform, "SessionSummaryText");

            if (nextTryTitleText == null)
                nextTryTitleText = FindTMP(gameOverPanel.transform, "NextTryTitleText");

            if (guidanceHintText == null)
                guidanceHintText = FindTMP(gameOverPanel.transform, "GuidanceHintText");

            if (nextTryTitleText == null || guidanceHintText == null)
                EnsureGuidanceCardTexts();

            if (bestMoveValueText == null)
                bestMoveValueText = FindTMP(gameOverPanel.transform, "BestMoveValueText");

            if (maxComboValueText == null)
                maxComboValueText = FindTMP(gameOverPanel.transform, "MaxComboValueText");

            if (totalLinesValueText == null)
                totalLinesValueText = FindTMP(gameOverPanel.transform, "TotalLinesValueText");

            if (averageMoveValueText == null)
                averageMoveValueText = FindTMP(gameOverPanel.transform, "AverageMoveValueText");

            if (restartButton == null)
                restartButton = FindButton(gameOverPanel.transform, "RestartButton");

            if (mainMenuButton == null)
                mainMenuButton = FindButton(gameOverPanel.transform, "MainMenuButton");
            if (rescueButton == null)
                rescueButton = FindButton(gameOverPanel.transform, "RescueButton");

            if (ShouldRequireSceneActionButtons())
            {
                if (restartButton == null)
                    restartButton = FindButtonAnywhere("RestartButton", "Restart");

                if (mainMenuButton == null)
                    mainMenuButton = FindButtonAnywhere("MainMenuButton", "MainMenu", "AnaMenu", "Ana Menu");
                if (rescueButton == null)
                    rescueButton = FindButtonAnywhere("RescueButton", "Rescue", "Kurtar");
            }

            if (continueOfferPanel == null)
            {
                var offer = FindDeep(gameOverPanel.transform, "ContinueOfferPanel");
                if (offer != null)
                    continueOfferPanel = offer.gameObject;
            }

            if (continueOfferPanel == null)
            {
                var allPanels = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allPanels.Length; i++)
                {
                    if (allPanels[i] != null && allPanels[i].name.Equals("ContinueOfferPanel", StringComparison.OrdinalIgnoreCase))
                    {
                        continueOfferPanel = allPanels[i].gameObject;
                        break;
                    }
                }
            }

            if (continueOfferPanel != null)
            {
                if (continueButton == null)
                    continueButton = FindButton(continueOfferPanel.transform, "ContinueButton");

                if (continueButton == null)
                    continueButton = FindButton(continueOfferPanel.transform, "RestartButton");

                if (continueCountdownText == null)
                    continueCountdownText = FindTMP(continueOfferPanel.transform, "ContinueCountdownText");

                if (continueCountdownText == null)
                    continueCountdownText = FindTMPContains(continueOfferPanel.transform, "countdown");

                if (noMovesLabel == null)
                    noMovesLabel = FindTMP(continueOfferPanel.transform, "NoMovesText");

                if (noMovesLabel == null)
                    noMovesLabel = FindTMPContains(continueOfferPanel.transform, "nomoves");
            }

            if (CanLog)
            {
                Debug.Log(
                    $"[GameOverView] AutoWire -> finalScoreText={(finalScoreText != null)}, bestScoreText={(bestScoreText != null)}, newBestText={(newBestText != null)}, " +
                    $"bestMoveValueText={(bestMoveValueText != null)}, maxComboValueText={(maxComboValueText != null)}, totalLinesValueText={(totalLinesValueText != null)}, averageMoveValueText={(averageMoveValueText != null)}, " +
                    $"restartButton={(restartButton != null)}, mainMenuButton={(mainMenuButton != null)}"
                );
            }
        }

        private void TryBuildDedicatedSceneLayout()
        {
            if (!buildDedicatedSceneLayoutAtRuntime || !IsDedicatedGameOverScene() || gameOverPanel == null)
                return;

            ResetUiReferenceCache();
            GameOverSceneRichLayout.EnsureBuilt(gameOverPanel);
        }

        private void TryBuildInGameContinueOfferLayout()
        {
            if (IsDedicatedGameOverScene() || continueOfferPanel == null)
                return;

            InGameContinueOfferLayout.EnsureBuilt(continueOfferPanel);
            restartButton = null;
            mainMenuButton = null;
            continueButton = null;
            noMovesLabel = null;
            continueCountdownText = null;
        }

#if UNITY_EDITOR
        public void RebuildInGameContinueOfferEditorPreview()
        {
            if (gameOverPanel == null)
                gameOverPanel = gameObject;

            if (continueOfferPanel == null)
            {
                var offer = FindDeep(gameOverPanel.transform, "ContinueOfferPanel");
                if (offer != null)
                    continueOfferPanel = offer.gameObject;
            }

            if (continueOfferPanel == null)
                return;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            continueOfferPanel.SetActive(true);
            TryBuildInGameContinueOfferLayout();
            AutoWireIfMissing();
            ConfigureInteractiveUi();
            SetupGameLocalization();

            if (noMovesLabel != null)
                noMovesLabel.text = GetNoMovesMessage();

            if (continueCountdownText != null)
                continueCountdownText.text = GetContinueOfferSubtitle();
        }
#endif

        private void EnsureEventSystemIfMissing()
        {
            if (!IsDedicatedGameOverScene())
                return;

            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif

            if (CanLog)
                Debug.Log("[GameOverView] Dedicated GameOver scene had no EventSystem. A runtime EventSystem was created.");
        }

        private void ResetUiReferenceCache()
        {
            finalScoreText = null;
            bestScoreText = null;
            newBestText = null;
            sessionSummaryText = null;
            nextTryTitleText = null;
            guidanceHintText = null;
            bestMoveValueText = null;
            maxComboValueText = null;
            totalLinesValueText = null;
            averageMoveValueText = null;
            restartButton = null;
            mainMenuButton = null;
            rescueButton = null;
            continueOfferPanel = null;
            noMovesLabel = null;
            continueCountdownText = null;
            continueButton = null;
        }

        private bool IsDedicatedGameOverScene()
        {
            if (!useSeparateGameOverScene || string.IsNullOrWhiteSpace(gameOverSceneName))
                return false;

            var activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() &&
                   activeScene.name.Equals(gameOverSceneName, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRichDedicatedSceneLayoutActive()
        {
            return IsDedicatedGameOverScene() &&
                   FindDeep(gameOverPanel != null ? gameOverPanel.transform : null, "RichLayoutRoot") != null;
        }

        private void TryShowDedicatedScenePayload()
        {
            if (!IsDedicatedGameOverScene())
                return;

            if (!GameOverScenePayload.TryGet(out var payload))
                return;

            _pendingFinalScore = payload.FinalScore;
            _sessionBestMoveDelta = payload.SessionBestMoveDelta;
            _sessionHighestCombo = payload.SessionHighestCombo;
            ShowGameOverScreen(payload.FinalScore, payload.IsNewBest);
        }

        private void ResolveContinueDependencies()
        {
            if (_gridView == null)
            {
                _gridView = FindFirstObjectByType<SimpleGridView>();
                if (_gridView != null && !_loggedGridViewFallbackWarning)
                {
                    _loggedGridViewFallbackWarning = true;
                    Debug.LogWarning("[GameOverView] _gridView was resolved via runtime lookup. Inspector wiring is the preferred production path.");
                }
            }

            HookAdCallbacks();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_gameBootstrap == null)
                _gameBootstrap = TryAutoAssignSingleton<GameBootstrap>();

            if (_gridView == null)
                _gridView = TryAutoAssignSingleton<SimpleGridView>();
#endif
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : UnityEngine.Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        private void HookAdCallbacks()
        {
            if (_adCallbacksHooked)
                return;

            RewardedAdBridge.RewardedUserEarned -= HandleRewardedUserEarned;
            RewardedAdBridge.RewardedUserEarned += HandleRewardedUserEarned;
            RewardedAdBridge.RewardedAdLoaded -= HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdLoaded += HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdClosed -= HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdClosed += HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdFailedToLoad -= HandleRewardedAdFailedToLoad;
            RewardedAdBridge.RewardedAdFailedToLoad += HandleRewardedAdFailedToLoad;
            _adCallbacksHooked = true;
        }

        private void UnhookAdCallbacks()
        {
            if (!_adCallbacksHooked)
                return;

            RewardedAdBridge.RewardedUserEarned -= HandleRewardedUserEarned;
            RewardedAdBridge.RewardedAdLoaded -= HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdClosed -= HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdFailedToLoad -= HandleRewardedAdFailedToLoad;
            _adCallbacksHooked = false;
        }

        private void DiagnoseUiPipeline(string where)
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                Debug.LogError($"[GameOverView] ({where}) EventSystem YOK! UI butonları çalışmaz.");
            }
            else if (CanLog)
            {
                Debug.Log($"[GameOverView] ({where}) EventSystem: {es.name} (current={EventSystem.current?.name})");
            }

            var canvas = gameOverPanel != null ? gameOverPanel.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogError($"[GameOverView] ({where}) Canvas bulunamadı! GameOverPanel bir Canvas altında olmalı.");
            }
            else if (CanLog)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log(
                    $"[GameOverView] ({where}) Canvas={canvas.name}, RenderMode={canvas.renderMode}, SortingOrder={canvas.sortingOrder}, " +
                    $"GraphicRaycaster={(raycaster != null ? "OK" : "MISSING")}"
                );
            }
        }

        private void SetupButtonListeners()
        {
            bool requiresSceneActionButtons = ShouldRequireSceneActionButtons();

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(HandleRestartOrContinueButtonClicked);

                if (CanLog) Debug.Log("[GameOverView] RestartButton listener added (click -> RestartGame)");
            }
            else if (requiresSceneActionButtons)
            {
                Debug.LogWarning("[GameOverView] RestartButton NULL. Inspector'dan bağla veya isimle bulunamadı.");
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() =>
                {
                if (CanLog) Debug.Log("[GameOverView] MainMenuButton CLICK received");
                    ReturnToMainMenu();
                });

                if (CanLog) Debug.Log("[GameOverView] MainMenuButton listener added (click -> ReturnToMainMenu)");
            }
            else if (requiresSceneActionButtons)
            {
                Debug.LogWarning("[GameOverView] MainMenuButton NULL. Inspector'dan bağla veya isimle bulunamadı.");
            }

            if (continueButton != null)
            {
                if (continueButton != restartButton)
                {
                    continueButton.onClick.RemoveAllListeners();
                    continueButton.onClick.AddListener(HandleContinueButtonClicked);
                }
            }

            _restartActsAsContinue = continueButton != null && continueButton == restartButton;

            if (rescueButton != null)
            {
                rescueButton.onClick.RemoveAllListeners();
                rescueButton.onClick.AddListener(HandleRescueButtonClicked);
                rescueButton.interactable = _gameBootstrap != null && _gameBootstrap.RescueTokensRemaining > 0;
            }
        }

        private void RemoveButtonListeners()
        {
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
            if (continueButton != null) continueButton.onClick.RemoveAllListeners();
            if (rescueButton != null) rescueButton.onClick.RemoveAllListeners();
        }

        private void ConfigureInteractiveUi()
        {
            if (gameOverPanel == null)
                return;

            // Keep panel background behind everything and prevent it from consuming input.
            var background = FindDeep(gameOverPanel.transform, "BackgroundLayer");
            if (background == null)
                background = FindDeep(gameOverPanel.transform, "Background");
            if (background != null)
            {
                background.SetAsFirstSibling();

                var bgImage = background.GetComponent<Image>();
                if (bgImage != null)
                    bgImage.raycastTarget = false;
            }

            ConfigureButtonHitTarget(restartButton);
            ConfigureButtonHitTarget(mainMenuButton);
            ConfigureButtonHitTarget(continueButton);
            ConfigureButtonHitTarget(rescueButton);
        }

        private void ConfigureButtonHitTarget(Button button)
        {
            if (button == null)
                return;

            button.interactable = true;

            var targetGraphic = button.targetGraphic;
            if (targetGraphic != null)
                targetGraphic.raycastTarget = true;

            // Let button root receive clicks. Child labels/decorations should not steal raycasts.
            var childImages = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                var image = childImages[i];
                if (image != null &&
                    image.gameObject != button.gameObject &&
                    image != targetGraphic)
                    image.raycastTarget = false;
            }

            var childTexts = button.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < childTexts.Length; i++)
            {
                if (childTexts[i] != null)
                    childTexts[i].raycastTarget = false;
            }
        }

        // -------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------

        private void OnGameStarted()
        {
            if (CanLog) Debug.Log("[GameOverView] OnGameStarted -> hide panel");
            _sessionHighestCombo = 0;
            _sessionBestMoveDelta = 0;
            _continuesUsedThisRun = 0;
            _isFinishingGameOver = false;
            HideGameOverScreenImmediate();
        }

        private void OnScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            if (breakdown.ComboStreak > _sessionHighestCombo)
                _sessionHighestCombo = breakdown.ComboStreak;

            if (breakdown.ScoreDelta > _sessionBestMoveDelta)
                _sessionBestMoveDelta = breakdown.ScoreDelta;
        }

        private void OnGameOver(int finalScore)
        {
            if (CanLog)
                Debug.Log($"[GameOverView] OnGameOver RECEIVED! FinalScore: {finalScore}");

            _pendingFinalScore = finalScore;

            if (TryStartContinueOffer(finalScore))
                return;

            if (TryShowContinueUnavailableOffer(finalScore))
                return;

            BeginFinalGameOver(finalScore);
        }

        // -------------------------------------------------------
        // Show / hide
        // -------------------------------------------------------

        public void ShowGameOver(int finalScore, bool? isNewBestOverride = null)
        {
            ShowGameOverScreen(finalScore, isNewBestOverride);
        }

        public void HideGameOver()
        {
            HideGameOverScreenImmediate();
        }

        private void ShowGameOverScreen(int finalScore, bool? isNewBestOverride = null)
        {
            if (gameOverPanel == null)
            {
                Debug.LogError("[GameOverView] gameOverPanel is NULL!");
                return;
            }

            EnsureBootstrap();
            EnsurePanelOnTop();
            ApplyStaticGameOverLocalization();

            // Paneli aç
            gameOverPanel.SetActive(true);

            // Görünürlük ve input tuzaklarını kaldır
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            gameOverPanel.transform.localScale = Vector3.one;
            ConfigureInteractiveUi();

            // Skor bilgileri
            int bestScore = _gameBootstrap != null ? _gameBootstrap.BestScore : finalScore;
            bool isNewBest = isNewBestOverride
                ?? (_gameBootstrap != null
                    ? _gameBootstrap.IsCurrentSessionNewBest(finalScore)
                    : GameBootstrap.EvaluateIsNewBestScore(finalScore, bestScore));

            if (finalScoreText != null) finalScoreText.text = FormatFinalScore(finalScore);
            if (bestScoreText != null) bestScoreText.text = FormatBestScore(bestScore);

            if (newBestText != null)
            {
                newBestText.gameObject.SetActive(isNewBest);
                if (isNewBest) newBestText.text = GetNewBestLabel();
            }

            if (sessionSummaryText != null)
            {
                var state = _gameBootstrap != null ? _gameBootstrap.CurrentState : null;
                int moveCount = state?.MoveCount ?? 0;
                int linesCleared = state?.TotalLinesCleared ?? 0;
                float averagePerMove = moveCount > 0 ? finalScore / (float)moveCount : 0f;
                string riskSnapshot = _gameBootstrap != null ? _gameBootstrap.LastGameOverRiskSnapshotCode : string.Empty;

                sessionSummaryText.text = BuildSessionSummary(
                    _sessionBestMoveDelta,
                    _sessionHighestCombo,
                    linesCleared,
                    averagePerMove,
                    riskSnapshot);
            }

            ApplyGuidanceHint(_gameBootstrap != null ? _gameBootstrap.LastGameOverGuidanceCode : string.Empty);

            bool hasPayload = GameOverScenePayload.TryGet(out var payload);
            if (hasPayload || IsRichDedicatedSceneLayoutActive())
            {
                var payloadState = _gameBootstrap != null ? _gameBootstrap.CurrentState : null;
                int payloadBestScore = hasPayload ? payload.BestScore : (_gameBootstrap != null ? _gameBootstrap.BestScore : finalScore);
                int payloadMoveCount = hasPayload ? payload.MoveCount : (payloadState?.MoveCount ?? 0);
                int payloadLinesCleared = hasPayload ? payload.TotalLinesCleared : (payloadState?.TotalLinesCleared ?? 0);
                int payloadBestMoveDelta = hasPayload ? payload.SessionBestMoveDelta : _sessionBestMoveDelta;
                int payloadHighestCombo = hasPayload ? payload.SessionHighestCombo : _sessionHighestCombo;
                float payloadAveragePerMove = payloadMoveCount > 0 ? finalScore / (float)payloadMoveCount : 0f;

                bool payloadIsNewBest = isNewBestOverride
                    ?? (hasPayload
                        ? payload.IsNewBest
                        : (_gameBootstrap != null
                            ? _gameBootstrap.IsCurrentSessionNewBest(finalScore)
                            : GameBootstrap.EvaluateIsNewBestScore(finalScore, payloadBestScore)));

                if (finalScoreText != null)
                    finalScoreText.text = IsRichDedicatedSceneLayoutActive() ? finalScore.ToString("N0") : FormatFinalScore(finalScore);

                if (bestScoreText != null)
                    bestScoreText.text = FormatBestScore(payloadBestScore);

                SetNewBestVisible(payloadIsNewBest);
                if (newBestText != null && payloadIsNewBest)
                    newBestText.text = GetNewBestLabel();

                if (bestMoveValueText != null)
                    bestMoveValueText.text = Mathf.Max(0, payloadBestMoveDelta).ToString("N0");

                if (maxComboValueText != null)
                    maxComboValueText.text = Mathf.Max(0, payloadHighestCombo).ToString("N0");

                if (totalLinesValueText != null)
                    totalLinesValueText.text = Mathf.Max(0, payloadLinesCleared).ToString("N0");

                if (averageMoveValueText != null)
                    averageMoveValueText.text = payloadAveragePerMove.ToString("0.0");

                if (sessionSummaryText != null)
                {
                    string riskSnapshot = _gameBootstrap != null ? _gameBootstrap.LastGameOverRiskSnapshotCode : string.Empty;
                    sessionSummaryText.text = BuildSessionSummary(
                        payloadBestMoveDelta,
                        payloadHighestCombo,
                        payloadLinesCleared,
                        payloadAveragePerMove,
                        riskSnapshot);
                }
            }

            ApplyGuidanceHint(_gameBootstrap != null ? _gameBootstrap.LastGameOverGuidanceCode : string.Empty);
            TrackRiskSnapshotIfNeeded(_gameBootstrap != null ? _gameBootstrap.LastGameOverRiskSnapshotCode : string.Empty);

            DiagnoseUiPipeline("ShowGameOverScreen");

            if (CanLog)
                Debug.Log($"[GameOverView] Panel activated. activeInHierarchy={gameOverPanel.activeInHierarchy}, alpha={_canvasGroup.alpha}");
        }

        private void EnsurePanelOnTop()
        {
            if (gameOverPanel == null)
                return;

            var panelTransform = gameOverPanel.transform;
            if (panelTransform.parent != null)
                panelTransform.SetAsLastSibling();
        }

        private void SetNewBestVisible(bool visible)
        {
            if (newBestText == null)
                return;

            Transform bannerRoot = newBestText.transform.parent != null &&
                                   newBestText.transform.parent.name == "NewBestBanner"
                ? newBestText.transform.parent
                : newBestText.transform;

            bannerRoot.gameObject.SetActive(visible);
        }

        private void HideGameOverScreenImmediate()
        {
            if (gameOverPanel == null) return;

            bool keepPanelActive = IsDedicatedGameOverScene();

            if (keepPanelActive)
            {
                if (!gameOverPanel.activeSelf)
                    gameOverPanel.SetActive(true);
            }
            else
            {
                gameOverPanel.SetActive(false);
            }

            SetContinueOfferVisible(false);
            _continueOfferActive = false;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            _isFinishingGameOver = false;
            _continueCountdownRemaining = 0f;
            _lastTrackedGuidanceCode = string.Empty;
            _lastTrackedRiskSnapshotCode = string.Empty;

            if (_continueCountdownRoutine != null)
            {
                StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            // Bir dahaki açılışta alpha 0 / raycast kilidi olmasın
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private bool TryStartContinueOffer(int finalScore)
        {
            if (!enableContinueOffer)
                return false;

            if (!HasContinueQuotaRemaining())
                return false;

            if (continueOfferPanel == null)
                return false;

            _pendingFinalScore = finalScore;
            _continueOfferActive = true;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            _continueCountdownRemaining = Mathf.Max(1f, continueCountdownSeconds);
            SetContinueOfferAdUiVisible(true);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (noMovesLabel != null)
                noMovesLabel.text = GetNoMovesMessage();

            UpdateContinueCountdownText(_continueCountdownRemaining);

            SetContinueOfferVisible(true);
            EmitContinueTelemetry("continue_offer_shown");

            if (continueButton != null)
            {
                // Do not hard-disable the button while the next rewarded ad is still reloading.
                // The click handler already falls back safely if no ad is ready yet.
                ConfigureButtonHitTarget(continueButton);
                continueButton.interactable = true;
            }

            if (noMovesLabel != null)
                noMovesLabel.raycastTarget = false;

            if (continueCountdownText != null)
                continueCountdownText.raycastTarget = false;

            if (_continueCountdownRoutine != null)
                StopCoroutine(_continueCountdownRoutine);

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            _continueCountdownRoutine = StartCoroutine(ContinueCountdownRoutine());
            return true;
        }

        private bool TryShowContinueUnavailableOffer(int finalScore)
        {
            if (!enableContinueOffer)
                return false;

            if (HasContinueQuotaRemaining())
                return false;

            if (continueOfferPanel == null)
                return false;

            _pendingFinalScore = finalScore;
            _continueOfferActive = false;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            _continueCountdownRemaining = 0f;
            SetContinueOfferAdUiVisible(false);

            if (_continueCountdownRoutine != null)
            {
                StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (noMovesLabel != null)
                noMovesLabel.text = GetNoMovesMessage();

            if (continueCountdownText != null)
                continueCountdownText.text = GetContinueOfferUnavailableMessage();

            if (noMovesLabel != null)
                noMovesLabel.raycastTarget = false;

            if (continueCountdownText != null)
                continueCountdownText.raycastTarget = false;

            SetContinueOfferVisible(true);
            EmitContinueTelemetry("continue_offer_exhausted");
            return true;
        }

        private IEnumerator ContinueCountdownRoutine()
        {
            _continueCountdownRemaining = Mathf.Max(1f, _continueCountdownRemaining <= 0f
                ? continueCountdownSeconds
                : _continueCountdownRemaining);

            while (_continueCountdownRemaining > 0f && _continueOfferActive)
            {
                UpdateContinueCountdownText(_continueCountdownRemaining);
                yield return null;

                if (_waitingRewardedResult)
                    continue;

                _continueCountdownRemaining -= Time.unscaledDeltaTime;
            }

            _continueCountdownRoutine = null;

            if (_continueOfferActive && !_waitingRewardedResult)
                BeginFinalGameOver(_pendingFinalScore);
        }

        private void UpdateContinueCountdownText(float remainingSeconds)
        {
            if (continueCountdownText == null)
                return;

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            continueCountdownText.text = FormatContinueCountdown(seconds);
        }

        private void HandleContinueButtonClicked()
        {
            if (CanLog)
                Debug.Log("[GameOverView] HandleContinueButtonClicked() called");
            
            if (!_continueOfferActive || _waitingRewardedResult)
                return;

            if (!IsRewardedAdReady())
            {
                if (CanLog)
                    Debug.LogWarning("[GameOverView] Rewarded ad not ready yet. Waiting for load...");

                EmitContinueTelemetry("continue_clicked_waiting_ad");
                _waitingRewardedResult = true;
                _queuedRewardedAdShow = true;
                _rewardEarned = false;

                if (continueButton != null)
                    continueButton.interactable = false;

                if (_continueCountdownRoutine != null)
                {
                    StopCoroutine(_continueCountdownRoutine);
                    _continueCountdownRoutine = null;
                }

                if (noMovesLabel != null)
                    noMovesLabel.text = GetRewardedLoadingMessage();

                if (_rewardedLoadTimeoutRoutine != null)
                    StopCoroutine(_rewardedLoadTimeoutRoutine);

                _rewardedLoadTimeoutRoutine = StartCoroutine(RewardedLoadTimeoutRoutine());
                HookAdCallbacks();
                return;
            }

            if (CanLog)
                Debug.Log("[GameOverView] Rewarded ad is ready. Showing now.");
            _waitingRewardedResult = true;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            EmitContinueTelemetry("continue_clicked");

            if (continueButton != null)
                continueButton.interactable = false;

            if (_continueCountdownRoutine != null)
            {
                StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }

            if (noMovesLabel != null)
                noMovesLabel.text = GetRewardedOpeningMessage();

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            ShowRewardedAd();
        }

        private void HandleRestartOrContinueButtonClicked()
        {
            if (_continueOfferActive && _restartActsAsContinue)
            {
                if (CanLog) Debug.Log("[GameOverView] Continue button click routed via RestartButton");
                HandleContinueButtonClicked();
                return;
            }

            if (CanLog) Debug.Log("[GameOverView] RestartButton CLICK received");
            RestartGame();
        }

        private void HandleRescueButtonClicked()
        {
            if (_gameBootstrap == null)
                return;

            bool rescued = _gameBootstrap.TryUseRescueToken();
            if (rescued)
            {
                EmitContinueTelemetry("rescue_token_success");
                HideGameOverScreenImmediate();
                return;
            }

            EmitContinueTelemetry("rescue_token_failed");
        }

        private void HandleRewardedAdLoaded()
        {
            if (!_continueOfferActive)
                return;

            if (_queuedRewardedAdShow)
            {
                if (CanLog)
                    Debug.Log("[GameOverView] Rewarded ad finished loading while continue offer is active. Showing ad now.");

                if (_rewardedLoadTimeoutRoutine != null)
                {
                    StopCoroutine(_rewardedLoadTimeoutRoutine);
                    _rewardedLoadTimeoutRoutine = null;
                }

                if (noMovesLabel != null)
                    noMovesLabel.text = GetRewardedOpeningMessage();

                ShowRewardedAd();
                return;
            }

            if (_waitingRewardedResult)
                return;

            if (noMovesLabel != null)
                noMovesLabel.text = GetNoMovesMessage();

            if (continueButton != null)
                continueButton.interactable = true;
        }

        private bool IsRewardedAdReady()
        {
            if (!RewardedAdBridge.HasProvider)
            {
                Debug.LogError("[GameOverView] CRITICAL: Rewarded ad bridge provider is missing!");
                return false;
            }

            bool ready = RewardedAdBridge.IsReady();
            if (CanLog)
                Debug.Log($"[GameOverView] IsRewardedAdReady() = {ready}");
            return ready;
        }

        private void ShowRewardedAd()
        {
            if (CanLog)
                Debug.Log("[GameOverView] ShowRewardedAd() called");
            HookAdCallbacks();

            if (!RewardedAdBridge.HasProvider)
            {
                Debug.LogError("[GameOverView] CRITICAL: Cannot show rewarded ad - bridge provider missing!");
                return;
            }

            if (CanLog)
                Debug.Log("[GameOverView] Showing rewarded ad via AdMobManager.");
            RewardedAdBridge.Show();
        }

        private IEnumerator RewardedLoadTimeoutRoutine()
        {
            yield return new WaitForSecondsRealtime(rewardedLoadTimeoutSeconds);
            _rewardedLoadTimeoutRoutine = null;

            if (!_continueOfferActive || !_waitingRewardedResult || !_queuedRewardedAdShow)
                yield break;

            HandleRewardedAdFailedToLoad("timeout");
        }

        private void HandleRewardedAdFailedToLoad(string errorMessage)
        {
            if (!_continueOfferActive)
                return;

            if (CanLog)
                Debug.LogWarning($"[GameOverView] Rewarded ad failed to load during continue flow: {errorMessage}");

            EmitContinueTelemetry(string.Equals(errorMessage, "timeout", StringComparison.OrdinalIgnoreCase)
                ? "continue_load_timeout"
                : "continue_load_failed");

            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            if (continueButton != null)
                continueButton.interactable = true;

            if (noMovesLabel != null)
                noMovesLabel.text = GetRewardedLoadFailedMessage();

            if (_continueOfferActive && _continueCountdownRoutine == null)
                _continueCountdownRoutine = StartCoroutine(ContinueCountdownRoutine());
        }

        private void HandleRewardedUserEarned()
        {
            if (_waitingRewardedResult)
            {
                _queuedRewardedAdShow = false;
                _rewardEarned = true;
            }
        }

        private void HandleRewardedAdClosed()
        {
            if (!_waitingRewardedResult)
                return;

            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            if (_rewardEarned)
            {
                CompleteContinue();
                return;
            }

            if (CanLog)
                Debug.LogWarning("[GameOverView] Rewarded ad closed without reward callback. Continue denied.");

            EmitContinueTelemetry("continue_denied");
            BeginFinalGameOver(_pendingFinalScore);
        }

        private void CompleteContinue()
        {
            _continueOfferActive = false;
            _queuedRewardedAdShow = false;
            _continueCountdownRemaining = 0f;

            if (_continueCountdownRoutine != null)
            {
                StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }

            if (_rewardedLoadTimeoutRoutine != null)
            {
                StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }

            SetContinueOfferVisible(false);

            bool continued = _gameBootstrap != null && _gameBootstrap.TryContinueAfterRewardedAd();
            if (!continued)
            {
                EmitContinueTelemetry("continue_restore_failed");
                BeginFinalGameOver(_pendingFinalScore);
                return;
            }

            _continuesUsedThisRun++;
            EmitContinueTelemetry("continue_success");

            if (CanLog)
                Debug.Log("[GameOverView] Continue flow completed successfully.");

            GameOverScenePayload.Clear();
            HideGameOverScreenImmediate();
        }

        private void BeginFinalGameOver(int finalScore)
        {
            if (_isFinishingGameOver || _waitingRewardedResult)
                return;

            _pendingFinalScore = finalScore;
            _isFinishingGameOver = true;
            PrepareHiddenCoroutineHost();
            StartCoroutine(FinalizeGameOverRoutine());
        }

        private IEnumerator FinalizeGameOverRoutine()
        {
            _continueOfferActive = false;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;

            if (_continueCountdownRoutine != null)
            {
                StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }

            SetContinueOfferVisible(false);

            if (playBoardExplosionOnFinalGameOver)
                yield return PlayBoardExplosionRoutine();
            GameOverScenePayload.Clear();

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                HideGameOverScreenImmediate();
                SceneManager.LoadScene(mainMenuSceneName);
                yield break;
            }

            Debug.LogError($"[GameOverView] MainMenu load FAILED after continue timeout: Scene '{mainMenuSceneName}' yüklenemiyor. Build Settings'e ekli mi?");
            ShowGameOverScreen(_pendingFinalScore);
            _isFinishingGameOver = false;
        }

        private IEnumerator PlayBoardExplosionRoutine()
        {
            if (_gridView == null)
                ResolveContinueDependencies();

            var state = _gameBootstrap != null ? _gameBootstrap.CurrentState : null;
            var board = state?.Board;

            if (_gridView == null || board == null)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                yield break;
            }

            var bursts = new System.Collections.Generic.List<BoardExplosionBurst>();
            Vector2 boardCenter = new Vector2((board.Width - 1) * 0.5f, (board.Height - 1) * 0.5f);
            Vector3 accumulatedWorldCenter = Vector3.zero;
            Color accumulatedColor = Color.black;
            int occupiedCount = 0;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (!board.IsOccupied(x, y))
                        continue;

                    Vector3 worldPos = _gridView.GetWorldPosition(x, y);
                    int colorId = board.GetCell(x, y).ColorId;
                    Color color = _gridView.GetBlockColor(colorId);
                    float distance = Vector2.Distance(new Vector2(x, y), boardCenter);

                    bursts.Add(new BoardExplosionBurst(x, y, worldPos, color, distance));
                    accumulatedWorldCenter += worldPos;
                    accumulatedColor += color;
                    occupiedCount++;
                }
            }

            if (occupiedCount <= 0)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                yield break;
            }

            if (finalExplosionRandomizeOrder)
                ShuffleExplosionBursts(bursts);
            else
                bursts.Sort((a, b) => a.DistanceToCenter.CompareTo(b.DistanceToCenter));

            Vector3 boardWorldCenter = accumulatedWorldCenter / occupiedCount;
            Color spectacleColor = accumulatedColor / occupiedCount;

            if (VFXEmitter.Instance != null)
                VFXEmitter.Instance.EmitBlockBreakParticles(
                    boardWorldCenter,
                    spectacleColor,
                    Mathf.Max(1, finalExplosionCenterBurstCombo),
                    emitSecondaryEffects: true);

            int burstsPerTick = Mathf.Max(1, finalExplosionCellsPerTick);
            int tickCount = Mathf.Max(1, Mathf.CeilToInt(bursts.Count / (float)burstsPerTick));
            float activePhaseDuration = Mathf.Max(0.6f, boardExplosionDuration * 0.7f);
            float tickDelay = activePhaseDuration / tickCount;
            float elapsed = 0f;

            for (int i = 0; i < bursts.Count; i++)
            {
                BoardExplosionBurst burst = bursts[i];

                PlayBoardCellBreakAnimation(burst.X, burst.Y, burst.Color);

                if (VFXEmitter.Instance != null)
                {
                    VFXEmitter.Instance.EmitBlockBreakParticles(
                        burst.WorldPosition,
                        burst.Color,
                        Mathf.Max(1, finalExplosionPerCellBurstCombo),
                        emitSecondaryEffects: false);

                    if (finalExplosionEmitLineClearBursts && UnityEngine.Random.value <= finalExplosionLineBurstChance)
                        VFXEmitter.Instance.EmitLineClearEffect(burst.WorldPosition, burst.Color, _gridView.CellSize);
                }

                if ((i + 1) % burstsPerTick == 0)
                {
                    yield return new WaitForSecondsRealtime(tickDelay);
                    elapsed += tickDelay;
                }
            }

            float settleDuration = Mathf.Max(0.15f, boardExplosionDuration - elapsed);
            yield return new WaitForSecondsRealtime(settleDuration);
        }

        private void PrepareHiddenCoroutineHost()
        {
            if (!enabled)
                enabled = true;

            if (gameOverPanel != null && !gameOverPanel.activeSelf)
                gameOverPanel.SetActive(true);

            if (_canvasGroup == null && gameOverPanel != null)
                _canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            SetContinueOfferVisible(false);
        }

        private void PlayBoardCellBreakAnimation(int x, int y, Color blockColor)
        {
            if (_gridView == null)
                return;

            Transform cellTransform = _gridView.GetCellTransform(x, y);
            if (cellTransform == null)
                return;

            var sourceRenderer = cellTransform.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null || sourceRenderer.sprite == null || !sourceRenderer.enabled)
                return;

            Transform breakVisual = CreateBoardBreakVisual(cellTransform, blockColor);
            if (breakVisual == null)
                return;


            if (AnimationController.Instance != null)
            {
                AnimationController.Instance.PlayBlockBreakEffect(
                    breakVisual,
                    blockColor,
                    () =>
                    {
                        if (breakVisual != null)
                            Destroy(breakVisual.gameObject);
                    });
            }
        }

        private static Transform CreateBoardBreakVisual(Transform sourceCellTransform, Color blockColor)
        {
            if (sourceCellTransform == null)
                return null;

            SpriteRenderer sourceRenderer = sourceCellTransform.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null || sourceRenderer.sprite == null)
                return null;

            var breakObj = new GameObject("BoardBreakVisual");
            breakObj.transform.position = sourceCellTransform.position;
            breakObj.transform.rotation = sourceCellTransform.rotation;
            breakObj.transform.localScale = sourceCellTransform.lossyScale;

            var renderer = breakObj.AddComponent<SpriteRenderer>();
            renderer.sprite = sourceRenderer.sprite;
            renderer.color = blockColor;
            renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            renderer.sortingOrder = sourceRenderer.sortingOrder + 20;

            return breakObj.transform;
        }

        private static void ShuffleExplosionBursts(System.Collections.Generic.List<BoardExplosionBurst> bursts)
        {
            if (bursts == null || bursts.Count <= 1)
                return;

            for (int i = bursts.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (bursts[i], bursts[swapIndex]) = (bursts[swapIndex], bursts[i]);
            }
        }

        private void CacheDedicatedScenePayload()
        {
            var state = _gameBootstrap != null ? _gameBootstrap.CurrentState : null;
            int bestScore = _gameBootstrap != null ? _gameBootstrap.BestScore : _pendingFinalScore;
            bool isNewBest = _gameBootstrap != null
                ? _gameBootstrap.IsCurrentSessionNewBest(_pendingFinalScore)
                : GameBootstrap.EvaluateIsNewBestScore(_pendingFinalScore, bestScore);
            string sourceGameSceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrWhiteSpace(gameOverSceneName) &&
                sourceGameSceneName.Equals(gameOverSceneName, StringComparison.OrdinalIgnoreCase))
            {
                sourceGameSceneName = gameSceneName;
            }

            GameOverScenePayload.Set(new GameOverScenePayloadData(
                finalScore: _pendingFinalScore,
                bestScore: bestScore,
                isNewBest: isNewBest,
                sessionHighestCombo: _sessionHighestCombo,
                sessionBestMoveDelta: _sessionBestMoveDelta,
                totalLinesCleared: state?.TotalLinesCleared ?? 0,
                moveCount: state?.MoveCount ?? 0,
                gameSceneName: sourceGameSceneName));
        }

        private void SetContinueOfferVisible(bool visible)
        {
            if (CanLog)
                Debug.Log($"[GameOverView] SetContinueOfferVisible({visible})");
            if (continueOfferPanel == null)
            {
                Debug.LogError("[GameOverView] continueOfferPanel NULL!");
                return;
            }

            continueOfferPanel.SetActive(visible);

            if (!visible)
            {
                if (CanLog)
                    Debug.Log("[GameOverView] Offer gizleniyor");
                return;
            }

            if (CanLog)
                Debug.Log("[GameOverView] Offer gosteriliyor - backdrop refresh basliyor");
            if (_gridView == null)
                ResolveContinueDependencies();

            if (_gridView != null)
            {
                if (CanLog)
                    Debug.Log("[GameOverView] GridView bulundu, backdrop refresh cagriliyor");
                _gridView.EnsureBoardBackdropVisible();
            }
            else
            {
                Debug.LogError("[GameOverView] GridView NULL!");
            }
        }

        private void SetContinueOfferAdUiVisible(bool visible)
        {
            if (continueButton != null && continueButton != restartButton)
            {
                continueButton.gameObject.SetActive(visible);
                continueButton.interactable = visible;
            }

            if (continueOfferPanel == null)
                return;

            Transform offerHintPill = FindDeep(continueOfferPanel.transform, "OfferHintPill");
            if (offerHintPill != null)
                offerHintPill.gameObject.SetActive(visible);
        }

        private static void EmitContinueTelemetry(string eventName)
        {
            try
            {
                var telemetryType = Type.GetType("AdTelemetry");
                var dispatchMethod = telemetryType?.GetMethod(
                    "DispatchLifecycleEvent",
                    BindingFlags.Public | BindingFlags.Static);
                dispatchMethod?.Invoke(null, new object[] { "rewarded", "continue_rewarded", eventName, string.Empty, 0d });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameOverView] Continue telemetry dispatch failed: {ex.Message}");
            }
        }

        private bool HasContinueQuotaRemaining()
        {
            return _continuesUsedThisRun < GetContinueOfferLimit();
        }

        private int GetContinueOfferLimit()
        {
            return Mathf.Max(0, maxContinueOffersPerRun);
        }

        private void NormalizeContinueOfferSettings()
        {
            // Legacy flag kept for backward-compatible inspector data.
            if (allowOneContinuePerRun)
                maxContinueOffersPerRun = Math.Min(1, Math.Max(0, maxContinueOffersPerRun));

            if (CanLog)
                Debug.Log($"[GameOverView] Continue offer limit securely normalized and locked to 1 per run.");
        }

        // -------------------------------------------------------
        // Actions
        // -------------------------------------------------------

        /// <summary>
        /// Restart: Oyun sahnesini yeniden yükler (en sağlam reset).
        /// </summary>
        public void RestartGame()
        {
            if (CanLog) Debug.Log("[GameOverView] RestartGame called");

            string targetGameScene = ResolveGameSceneNameForRestart();

            if (!Application.CanStreamedLevelBeLoaded(targetGameScene))
            {
                Debug.LogError($"[GameOverView] Restart FAILED: Scene '{targetGameScene}' yüklenemiyor. Build Settings'e ekli mi?");
                return;
            }

            GameOverScenePayload.Clear();
            HideGameOverScreenImmediate();

            if (CanLog) Debug.Log($"[GameOverView] Loading game scene: {targetGameScene}");
            SceneManager.LoadScene(targetGameScene);
        }

        private string ResolveGameSceneNameForRestart()
        {
            string payloadSceneName = string.Empty;
            if (GameOverScenePayload.TryGet(out var payload))
                payloadSceneName = payload.GameSceneName;

            string configuredSceneName = gameSceneName;
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (IsValidRestartTargetScene(payloadSceneName))
                return payloadSceneName;

            if (IsValidRestartTargetScene(configuredSceneName))
                return configuredSceneName;

            if (IsValidRestartTargetScene(activeSceneName))
                return activeSceneName;

            return configuredSceneName;
        }

        private bool IsValidRestartTargetScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (!string.IsNullOrWhiteSpace(gameOverSceneName) &&
                sceneName.Equals(gameOverSceneName, StringComparison.OrdinalIgnoreCase))
                return false;

            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        /// <summary>
        /// Main menu: MainMenu sahnesini yükler.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (CanLog) Debug.Log("[GameOverView] ReturnToMainMenu called");

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[GameOverView] MainMenu load FAILED: Scene '{mainMenuSceneName}' yüklenemiyor. Build Settings'e ekli mi?");
                return;
            }

            HideGameOverScreenImmediate();

            if (CanLog) Debug.Log($"[GameOverView] Loading main menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // -------------------------------------------------------
        // Helpers (deep search)
        // -------------------------------------------------------

        private TextMeshProUGUI FindTMP(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button FindButton(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private TextMeshProUGUI FindTMPContains(Transform root, string nameFragment)
        {
            if (root == null || string.IsNullOrWhiteSpace(nameFragment))
                return null;

            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                var candidate = tmps[i];
                if (candidate != null && candidate.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            }

            return null;
        }

        private Button FindButtonAnywhere(params string[] nameCandidates)
        {
            if (nameCandidates == null || nameCandidates.Length == 0)
                return null;

            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                var candidate = buttons[i];
                if (candidate == null)
                    continue;

                string candidateName = candidate.name ?? string.Empty;
                for (int n = 0; n < nameCandidates.Length; n++)
                {
                    string name = nameCandidates[n];
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (candidateName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                        candidateName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private bool ShouldRequireSceneActionButtons()
        {
            return IsDedicatedGameOverScene() || !useSeparateGameOverScene;
        }

        private Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// Tüm metin bileşenlerine LocalizedText kurulumu yapısır
        /// </summary>
        private void SetupGameLocalization()
        {
            // GameOver paneli yazıları
            LocalizedTextSetup.SetupLocalization(noMovesLabel, "Hamlen Kalmadı!", "No Moves Left!", "더 이상 움직일 수 없습니다!");
            LocalizedTextSetup.SetupLocalization(continueCountdownText, "Reklam izlemek için 5 saniyen var", "You have 5 seconds to watch the ad", "광고 시청까지 5초 남았습니다");

            // Dedicated rich layout statik başlıklar
            if (gameOverPanel != null)
            {
                Transform richRoot = FindDeep(gameOverPanel.transform, "RichLayoutRoot");
                Transform searchRoot = richRoot != null ? richRoot : gameOverPanel.transform;

                var gameOverTitle = FindTMP(searchRoot, "GameOverText");
                LocalizedTextSetup.SetupLocalization(gameOverTitle, "OYUN BİTTİ", "GAME OVER", "게임 오버");

                var scoreLabel = FindTMP(searchRoot, "ScoreLabel");
                LocalizedTextSetup.SetupLocalization(scoreLabel, "Skor", "Score", "점수");

                var bestMoveLabel = FindStatLabel(searchRoot, "BestMoveValueText_Row");
                LocalizedTextSetup.SetupLocalization(bestMoveLabel, "En iyi hamle", "Best move", "최고의 한 수");

                var maxComboLabel = FindStatLabel(searchRoot, "MaxComboValueText_Row");
                LocalizedTextSetup.SetupLocalization(maxComboLabel, "Maks combo", "Max combo", "최대 콤보");

                var totalLinesLabel = FindStatLabel(searchRoot, "TotalLinesValueText_Row");
                LocalizedTextSetup.SetupLocalization(totalLinesLabel, "Toplam çizgi", "Total lines", "전체 줄");

                var averageMoveLabel = FindStatLabel(searchRoot, "AverageMoveValueText_Row");
                LocalizedTextSetup.SetupLocalization(averageMoveLabel, "Ort / hamle", "Avg / move", "평균/한 수");

                // Rich layout button label'ları (legacy objeye değil görünür root'a hedeflenir)
                var restartButtonTransform = FindDeep(searchRoot, "RestartButton");
                if (restartButtonTransform != null)
                {
                    var restartLabel = FindTMP(restartButtonTransform, "LabelText");
                    LocalizedTextSetup.SetupLocalization(restartLabel, "Tekrar Oyna", "Play Again", "다시 하기");
                }

                var mainMenuButtonTransform = FindDeep(searchRoot, "MainMenuButton");
                if (mainMenuButtonTransform != null)
                {
                    var menuLabel = FindTMP(mainMenuButtonTransform, "LabelText");
                    LocalizedTextSetup.SetupLocalization(menuLabel, "Ana Menü", "Main Menu", "메인 메뉴");
                }

                var rewardText = FindTMP(searchRoot, "RewardText");
                LocalizedTextSetup.SetupLocalization(rewardText, "5 SN", "5 SEC", "5초");

                var offerHintText = FindTMP(searchRoot, "OfferHintText");
                LocalizedTextSetup.SetupLocalization(offerHintText, "5 saniye dolmadan reklam izle!", "Watch the ad before time runs out!", "시간이 끝나기 전에 광고를 시청하세요!");
            }

            // Score yazıları
            if (finalScoreText != null)
                LocalizedTextSetup.SetupLocalization(finalScoreText, "Son Skor: ", "Final Score: ", "최종 점수: ");

            if (bestScoreText != null)
                LocalizedTextSetup.SetupLocalization(bestScoreText, "En Yüksek Skor: ", "Best Score: ", "최고 점수: ");

            if (newBestText != null)
                LocalizedTextSetup.SetupLocalization(newBestText, "YENİ REKOR!", "NEW BEST!", "새로운 기록!");

            // Özet metinleri
            if (sessionSummaryText != null)
                LocalizedTextSetup.SetupLocalization(sessionSummaryText, "Oyun Özeti", "Game Summary", "게임 요약");

            // Buton yazıları - Restart butonu
            if (restartButton != null)
            {
                var restartText = FindButtonLabelText(restartButton);
                if (restartText != null)
                    LocalizedTextSetup.SetupLocalization(restartText, "Tekrar Oyna", "Play Again", "다시 하기");
            }

            // Buton yazıları - Main Menu butonu
            if (mainMenuButton != null)
            {
                var mainMenuText = FindButtonLabelText(mainMenuButton);
                if (mainMenuText != null)
                    LocalizedTextSetup.SetupLocalization(mainMenuText, "Ana Menu", "Main Menu", "메인 메뉴");
            }

            // Buton yazıları - Continue butonu
            if (continueButton != null)
            {
                var continueText = FindButtonLabelText(continueButton);
                if (continueText != null)
                    LocalizedTextSetup.SetupLocalization(continueText, "Reklam İzle", "Watch Ad", "광고 시청");
            }

            if (CanLog)
                Debug.Log("[GameOverView] Lokalizasyon kurulumu tamamlandı!");
        }

        private void ApplyStaticGameOverLocalization()
        {
            if (gameOverPanel == null)
                return;

            Transform richRoot = FindDeep(gameOverPanel.transform, "RichLayoutRoot");
            Transform root = richRoot != null ? richRoot : gameOverPanel.transform;

            SetStaticLocalizedText(root, "GameOverText", "OYUN BİTTİ", "GAME OVER");
            SetStaticLocalizedText(root, "ScoreLabel", "Skor", "Score");

            SetStatLabel(root, "BestMoveValueText_Row", "En iyi hamle", "Best move");
            SetStatLabel(root, "MaxComboValueText_Row", "Maks combo", "Max combo");
            SetStatLabel(root, "TotalLinesValueText_Row", "Toplam çizgi", "Total lines");
            SetStatLabel(root, "AverageMoveValueText_Row", "Ort / hamle", "Avg / move");

            SetButtonLabel(root, "RestartButton", "Tekrar Oyna", "Play Again");
            SetButtonLabel(root, "MainMenuButton", "Ana Menü", "Main Menu");
            SetStaticLocalizedText(root, "RewardText", "5 SN", "5 SEC");
            SetStaticLocalizedText(root, "OfferHintText", "5 saniye dolmadan reklam izle!", "Watch the ad before time runs out!");
        }

        private void SetStatLabel(Transform root, string rowName, string tr, string en)
        {
            if (root == null)
                return;

            Transform row = FindDeep(root, rowName);
            if (row == null)
                return;

            Transform label = row.Find("LabelText");
            if (label == null)
                return;

            var text = label.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;

            // If LocalizedText exists, it handles text updates — just force a refresh
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null)
                return;

            text.text = TrEn(tr, en);
        }

        private void SetButtonLabel(Transform root, string buttonName, string tr, string en)
        {
            if (root == null)
                return;

            Transform buttonTransform = FindDeep(root, buttonName);
            if (buttonTransform == null)
                return;

            Transform label = buttonTransform.Find("LabelText");
            if (label == null)
                return;

            var text = label.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;

            // If LocalizedText exists, it handles text updates
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null)
                return;

            text.text = TrEn(tr, en);
        }

        private void SetStaticLocalizedText(Transform root, string objectName, string tr, string en)
        {
            if (root == null)
                return;

            var text = FindTMP(root, objectName);
            if (text == null)
                return;

            // If LocalizedText exists, it handles text updates
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null)
                return;

            text.text = TrEn(tr, en);
        }

        private static void RemoveLocalizedTextComponent(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            var localized = text.GetComponent<LocalizedText>();
            if (localized != null)
                Destroy(localized);
        }

        private TextMeshProUGUI FindStatLabel(Transform root, string rowName)
        {
            if (root == null)
                return null;

            Transform row = FindDeep(root, rowName);
            if (row == null)
                return null;

            Transform label = row.Find("LabelText");
            if (label == null)
                return null;

            return label.GetComponent<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI FindButtonLabelText(Button button)
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
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i] == null)
                    continue;

                string nameLower = allTexts[i].name.ToLowerInvariant();
                if (nameLower.Contains("label"))
                    return allTexts[i];
            }

            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i] == null)
                    continue;

                string nameLower = allTexts[i].name.ToLowerInvariant();
                if (!nameLower.Contains("icon"))
                    return allTexts[i];
            }

            return null;
        }

        private static bool IsEnglishSelected()
        {
            return LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.English;
        }

        private static readonly Dictionary<string, string> KoreanTranslations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "OYUN BİTTİ", "게임 오버" },
            { "OYUN BITTİ", "게임 오버" },
            { "OYUN BİTTİ!", "게임 오버!" },
            { "GAME OVER", "게임 오버" },
            { "Skor", "점수" },
            { "Score", "점수" },
            { "En iyi hamle", "최고의 한 수" },
            { "Best move", "최고의 한 수" },
            { "Maks combo", "최대 콤보" },
            { "Max combo", "최대 콤보" },
            { "Toplam çizgi", "전체 줄" },
            { "Toplam çizg", "전체 줄" },
            { "Total lines", "전체 줄" },
            { "Ort / hamle", "평균/한 수" },
            { "Avg / move", "평균/한 수" },
            { "Tekrar Oyna", "다시 하기" },
            { "Play Again", "다시 하기" },
            { "Ana Menü", "메인 메뉴" },
            { "Ana MenǬ", "메인 메뉴" },
            { "Main Menu", "메인 메뉴" },
            { "5 SN", "5초" },
            { "5 SEC", "5초" },
            { "5 saniye dolmadan reklam izle!", "시간이 끝나기 전에 광고를 시청하세요!" },
            { "Watch the ad before time runs out!", "시간이 끝나기 전에 광고를 시청하세요!" },
            { "YENİ REKOR!", "새로운 기록!" },
            { "YENi REKOR!", "새로운 기록!" },
            { "NEW BEST!", "새로운 기록!" },
            { "Reklam izlemek için 5 saniyen var", "광고 시청까지 5초 남았습니다" },
            { "You have 5 seconds to watch the ad", "광고 시청까지 5초 남았습니다" },
            { "Reklam izleme hakkınız kalmadı", "더 이상 시청할 수 있는 광고가 없습니다" },
            { "Reklam izleme hakknz kalmad", "더 이상 시청할 수 있는 광고가 없습니다" },
            { "You have no ad watches left", "더 이상 시청할 수 있는 광고가 없습니다" },
            { "En iyi skor", "최고 점수" },
            { "Best score", "최고 점수" },
            { "Son 2 hamle", "마지막 2 수" },
            { "Last 2 moves", "마지막 2 수" },
            { "Bir Sonraki Denemede", "다음 도전" },
            { "Next Try", "다음 도전" },
            { "Hamlen Kalmadi!", "더 이상 움직일 수 없습니다!" },
            { "No moves left!", "더 이상 움직일 수 없습니다!" },
            { "Reklam yükleniyor...", "광고를 불러오는 중..." },
            { "Loading ad...", "광고를 불러오는 중..." },
            { "Reklam açılıyor...", "광고를 여는 중..." },
            { "Opening ad...", "광고를 여는 중..." },
            { "Reklam şu anda kullanılamıyor.", "광고를 현재 사용할 수 없습니다." },
            { "Ad is currently unavailable.", "광고를 현재 사용할 수 없습니다." },
            { "Ipuc: Ilk hedef her blogu guvenli yerlestirmek. Kucuk bloklari kopuk bosluklara sikistirma.", "팁: 처음에는 안전한 배치에 집중하세요. 좁은 틈에 작은 블록을 억지로 끼워 넣지 마세요." },
            { "Tip: First focus on safe placements. Do not jam small blocks into isolated gaps.", "팁: 처음에는 안전한 배치에 집중하세요. 좁은 틈에 작은 블록을 억지로 끼워 넣지 마세요." },
            { "Ipuc: Neredeyse dolu bir satir veya sutun birak, sonra kalan boslugu kapatarak temizleme al.", "팁: 거의 채워진 가로나 세로 줄을 남겨두고, 마지막 빈 칸을 채워 줄을 지우세요." },
            { "Tip: Leave a row or column almost full, then close the last gap for a clear.", "팁: 거의 채워진 가로나 세로 줄을 남겨두고, 마지막 빈 칸을 채워 줄을 지우세요." },
            { "Ipuc: 3x3 kare icin temiz bir bos alan koru. Parcalanmis bosluklar buyuk bloklari oldurur.", "팁: 깨끗한 3x3 빈 공간을 확보해 두세요. 조각난 빈 칸들은 큰 블록을 배치하기 어렵게 만듭니다." },
            { "Tip: Preserve one clean 3x3 area. Fragmented empty cells kill large-block options.", "팁: 깨끗한 3x3 빈 공간을 확보해 두세요. 조각난 빈 칸들은 큰 블록을 배치하기 어렵게 만듭니다." },
            { "Ipuc: Kisa vadeli puan yerine panoda nefes alacak alan birak.", "팁: 단기적인 점수 획득보다 보드의 여유 공간 확보를 우선하세요." },
            { "Tip: Prioritize board space over short-term points.", "팁: 단기적인 점수 획득보다 보드의 여유 공간 확보를 우선하세요." },
            { "Ipuc: Buyuk bloklar icin merkezde veya kenarda acik alan sakla; her bos hucreyi erken doldurma.", "팁: 큰 블록을 위한 빈 공간을 남겨두세요. 모든 빈 칸을 너무 빨리 채우지 마세요." },
            { "Tip: Save open space for large pieces; do not fill every gap too early.", "팁: 큰 블록을 위한 빈 공간을 남겨두세요. 모든 빈 칸을 너무 빨리 채우지 마세요." }
        };

        private static string TrEn(string turkish, string english)
        {
            if (LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Korean)
            {
                if (!string.IsNullOrEmpty(english))
                {
                    if (english.StartsWith("Continue in:", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string secStr = english.Replace("Continue in:", "").Replace("s", "").Trim();
                        return $"계속하기: {secStr}초";
                    }
                    if (KoreanTranslations.TryGetValue(english, out string koTranslation))
                        return koTranslation;
                }
                if (!string.IsNullOrEmpty(turkish))
                {
                    if (turkish.StartsWith("Devam için:", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string secStr = turkish.Replace("Devam için:", "").Replace("sn", "").Replace("s", "").Trim();
                        return $"계속하기: {secStr}초";
                    }
                    if (KoreanTranslations.TryGetValue(turkish, out string koTranslation))
                        return koTranslation;
                }
                return !string.IsNullOrEmpty(english) ? english : turkish;
            }

            return IsEnglishSelected() ? english : turkish;
        }

        private string GetNoMovesMessage()
        {
            return TrEn(noMovesMessage, string.IsNullOrWhiteSpace(noMovesMessageEnglish) ? "No moves left!" : noMovesMessageEnglish);
        }

        private string GetRewardedLoadingMessage()
        {
            return TrEn(rewardedLoadingMessage, string.IsNullOrWhiteSpace(rewardedLoadingMessageEnglish) ? "Loading ad..." : rewardedLoadingMessageEnglish);
        }

        private string GetRewardedOpeningMessage()
        {
            return TrEn(rewardedOpeningMessage, string.IsNullOrWhiteSpace(rewardedOpeningMessageEnglish) ? "Opening ad..." : rewardedOpeningMessageEnglish);
        }

        private string GetRewardedLoadFailedMessage()
        {
            return TrEn(rewardedLoadFailedMessage, string.IsNullOrWhiteSpace(rewardedLoadFailedMessageEnglish) ? "Ad is currently unavailable." : rewardedLoadFailedMessageEnglish);
        }

        private static string GetNewBestLabel()
        {
            return TrEn("YENİ REKOR!", "NEW BEST!");
        }

        private bool IsStyledInGameContinueOfferActive()
        {
            return !IsDedicatedGameOverScene() &&
                   continueOfferPanel != null &&
                   FindDeep(continueOfferPanel.transform, "InGameContinueOfferRoot") != null;
        }

        private string GetContinueOfferSubtitle()
        {
            return TrEn("Reklam izlemek için 5 saniyen var", "You have 5 seconds to watch the ad");
        }

        private string GetContinueOfferUnavailableMessage()
        {
            return TrEn("Reklam izleme hakkınız kalmadı", "You have no ad watches left");
        }

        private static string FormatFinalScore(int finalScore)
        {
            return $"{TrEn("Skor", "Score")}: {finalScore:N0}";
        }

        private static string FormatBestScore(int bestScore)
        {
            return $"{TrEn("En iyi skor", "Best score")}: {bestScore:N0}";
        }

        private static string FormatContinueCountdown(int seconds)
        {
            return TrEn($"Devam için: {seconds} sn", $"Continue in: {seconds}s");
        }

        private static string BuildSessionSummary(int bestMoveDelta, int highestCombo, int linesCleared, float averagePerMove, string riskSnapshotCode)
        {
            string summary =
                $"{TrEn("En iyi hamle", "Best move")}: +{Mathf.Max(0, bestMoveDelta):N0}\n" +
                $"{TrEn("Maks combo", "Max combo")}: x{Mathf.Max(0, highestCombo):N0}\n" +
                $"{TrEn("Toplam çizgi", "Total lines")}: {Mathf.Max(0, linesCleared):N0}\n" +
                $"{TrEn("Ort / hamle", "Avg / move")}: {averagePerMove:F1}";

            string riskSummary = BuildRiskSnapshotSummary(riskSnapshotCode);
            if (!string.IsNullOrWhiteSpace(riskSummary))
                summary += "\n\n" + riskSummary;

            return summary;
        }
        private static string BuildRiskSnapshotSummary(string riskSnapshotCode)
        {
            if (string.IsNullOrWhiteSpace(riskSnapshotCode))
                return string.Empty;

            string[] parts = riskSnapshotCode.Split('|');
            if (parts.Length == 0)
                return string.Empty;

            string header = TrEn("Son 2 hamle", "Last 2 moves");
            string firstLine = BuildRiskSnapshotLine(parts[0]);
            if (parts.Length == 1)
                return $"{header}: {firstLine}";

            return $"{header}:\n{firstLine}\n{BuildRiskSnapshotLine(parts[1])}";
        }

        private static string BuildRiskSnapshotLine(string snapshotPart)
        {
            if (string.IsNullOrWhiteSpace(snapshotPart))
                return string.Empty;

            int colonIndex = snapshotPart.IndexOf(':');
            if (colonIndex <= 1 || colonIndex >= snapshotPart.Length - 1)
                return snapshotPart;

            string moveLabel = snapshotPart.Substring(0, colonIndex);
            string[] metrics = snapshotPart.Substring(colonIndex + 1).Split('/');
            if (metrics.Length < 3)
                return snapshotPart;

            string threeByThree = metrics[0].Replace("x3", " 3x3");
            string rect = metrics[1].Replace("rect", " rect");
            string future = metrics[2];
            return $"{moveLabel.ToUpperInvariant()}: {threeByThree}, {rect}, open {future}";
        }

        private static string BuildGuidanceMessage(string guidanceCode)
        {
            switch (guidanceCode)
            {
                case "tutorial_place":
                    return TrEn("Ipuc: Ilk hedef her blogu guvenli yerlestirmek. Kucuk bloklari kopuk bosluklara sikistirma.", "Tip: First focus on safe placements. Do not jam small blocks into isolated gaps.");
                case "tutorial_clear":
                    return TrEn("Ipuc: Neredeyse dolu bir satir veya sutun birak, sonra kalan boslugu kapatarak temizleme al.", "Tip: Leave a row or column almost full, then close the last gap for a clear.");
                case "tutorial_3x3":
                    return TrEn("Ipuc: 3x3 kare icin temiz bir bos alan koru. Parcalanmis bosluklar buyuk bloklari oldurur.", "Tip: Preserve one clean 3x3 area. Fragmented empty cells kill large-block options.");
                case "tutorial_generic":
                    return TrEn("Ipuc: Kisa vadeli puan yerine panoda nefes alacak alan birak.", "Tip: Prioritize board space over short-term points.");
                case "generic_space":
                    return TrEn("Ipuc: Buyuk bloklar icin merkezde veya kenarda acik alan sakla; her bos hucreyi erken doldurma.", "Tip: Save open space for large pieces; do not fill every gap too early.");
                default:
                    return string.Empty;
            }
        }

        private void ApplyGuidanceHint(string guidanceCode)
        {
            if (nextTryTitleText == null || guidanceHintText == null)
                return;

            string guidance = BuildGuidanceMessage(guidanceCode);
            bool hasGuidance = !string.IsNullOrWhiteSpace(guidance);

            nextTryTitleText.gameObject.SetActive(hasGuidance);
            guidanceHintText.gameObject.SetActive(hasGuidance);
            if (!hasGuidance)
                return;

            nextTryTitleText.text = TrEn("Bir Sonraki Denemede", "Next Try");
            guidanceHintText.text = guidance;

            if (!string.Equals(_lastTrackedGuidanceCode, guidanceCode, StringComparison.Ordinal))
            {
                AppAnalytics.TrackGameOverGuidanceShown(guidanceCode);
                _lastTrackedGuidanceCode = guidanceCode ?? string.Empty;
            }
        }

        private void TrackRiskSnapshotIfNeeded(string riskSnapshotCode)
        {
            if (string.IsNullOrWhiteSpace(riskSnapshotCode))
                return;

            if (string.Equals(_lastTrackedRiskSnapshotCode, riskSnapshotCode, StringComparison.Ordinal))
                return;

            AppAnalytics.TrackGameOverRiskSnapshotShown(riskSnapshotCode);
            _lastTrackedRiskSnapshotCode = riskSnapshotCode;
        }

        private void EnsureGuidanceCardTexts()
        {
            if (gameOverPanel == null)
                return;

            Transform anchor = sessionSummaryText != null ? sessionSummaryText.transform.parent : gameOverPanel.transform;

            if (nextTryTitleText == null)
            {
                var titleObject = new GameObject("NextTryTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObject.transform.SetParent(anchor, false);
                var titleRect = titleObject.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.12f, 0.245f);
                titleRect.anchorMax = new Vector2(0.88f, 0.315f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                nextTryTitleText = titleObject.GetComponent<TextMeshProUGUI>();
                nextTryTitleText.font = TMP_Settings.defaultFontAsset;
                nextTryTitleText.fontSize = 26f;
                nextTryTitleText.fontStyle = FontStyles.Bold;
                nextTryTitleText.alignment = TextAlignmentOptions.Center;
                nextTryTitleText.enableAutoSizing = true;
                nextTryTitleText.fontSizeMin = 18f;
                nextTryTitleText.fontSizeMax = 26f;
                nextTryTitleText.overflowMode = TextOverflowModes.Ellipsis;
                nextTryTitleText.color = new Color(1f, 0.76f, 0.28f, 1f);
                nextTryTitleText.text = string.Empty;
                nextTryTitleText.gameObject.SetActive(false);
            }

            if (guidanceHintText == null)
            {
                var guidanceObject = new GameObject("GuidanceHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
                guidanceObject.transform.SetParent(anchor, false);

                var rect = guidanceObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.12f, 0.08f);
                rect.anchorMax = new Vector2(0.88f, 0.24f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                guidanceHintText = guidanceObject.GetComponent<TextMeshProUGUI>();
                guidanceHintText.font = TMP_Settings.defaultFontAsset;
                guidanceHintText.fontSize = 24f;
                guidanceHintText.fontStyle = FontStyles.Bold;
                guidanceHintText.alignment = TextAlignmentOptions.Center;
                guidanceHintText.textWrappingMode = TextWrappingModes.Normal;
                guidanceHintText.enableAutoSizing = true;
                guidanceHintText.fontSizeMin = 16f;
                guidanceHintText.fontSizeMax = 24f;
                guidanceHintText.overflowMode = TextOverflowModes.Ellipsis;
                guidanceHintText.color = new Color(1f, 0.91f, 0.62f, 1f);
                guidanceHintText.text = string.Empty;
                guidanceHintText.gameObject.SetActive(false);
            }
        }
    }
}
