using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Input;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Social;
using BlockPuzzle.UnityAdapter;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Boot
{
    [ExecuteAlways]
    /// <summary>
    /// Production gameplay composition root for `Assets/Scenes/OyunEkranı.unity`.
    /// This is the active gameplay bootstrap used by gameplay adapters and scene-bound UI systems.
    /// Do not confuse it with `Assets/Scripts/Systems/GameBootstrap.cs`, which is a separate app-level startup bootstrap.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const int AnalyticsSchemaVersion = 1;

        [Header("ğŸ“± MOBILE BOARD SETTINGS")]
        [SerializeField] [Range(8, 10)] private int boardWidth = 10;
        [SerializeField] [Range(8, 10)] private int boardHeight = 10;
        [SerializeField] [Range(8, 10)] private int challengeBoardWidth = 10;
        [SerializeField] [Range(8, 10)] private int challengeBoardHeight = 10;
        [SerializeField] [Range(8, 12)] private int zenBoardWidth = 10;
        [SerializeField] [Range(8, 12)] private int zenBoardHeight = 10;
        [SerializeField] private int gameSeed = -1;

        [Header("ğŸ”§ DEBUG")]
        [SerializeField] private bool enableDebugLogging = true;

        [Header("ğŸ“± MOBILE CAMERA SETTINGS")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0, 0, -10);
        [SerializeField] [Range(4f, 8f)] private float cameraSize = 6f;
        
        [Header("ADAPTIVE SCREEN LAYOUT")]
        [SerializeField] private bool autoAdaptToScreen = true;
        [SerializeField] private bool lockPortraitOrientation = true;
        [SerializeField] [Range(0f, 2f)] private float cameraHorizontalPadding = 0.4f;
        [SerializeField] [Range(4f, 20f)] private float minAdaptiveCameraSize = 6f;
        [SerializeField] [Range(4f, 20f)] private float maxAdaptiveCameraSize = 14f;
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] [Range(0f, 1f)] private float canvasMatchWidthOrHeight = 0.5f;

        [Header("GAMEPLAY VISUAL READABILITY")]
        [SerializeField] private bool useWorldBackgroundLayer = true;
        [SerializeField] private bool preserveAuthoredWorldBackground = true;
        [SerializeField] private Sprite gameplayBackgroundSpriteOverride;
        [SerializeField] private Color gameplayBackgroundTint = Color.white;
        [SerializeField] private Color gameplayBackgroundDimmerColor = new Color(0.07f, 0.11f, 0.2f, 0.34f);
        [SerializeField] private Color gameplayCameraClearColor = new Color(0.055f, 0.082f, 0.141f, 1f);
        [SerializeField] [Range(0f, 1f)] private float legacyOverlayBackgroundAlpha = 0f;
        [SerializeField] [Range(-500, 500)] private int worldBackgroundSortingOrder = -200;
        [SerializeField] [Range(-500, 500)] private int worldDimmerSortingOrder = -150;
        [SerializeField] private string legacyOverlayBackgroundName = "Background";

        [Header("SCORING")]
        [SerializeField] private ScoreConfigAsset scoreConfigAsset;
        [SerializeField] private GameplayFeatureFlagsAsset gameplayFeatureFlags;

        [Header("PERSISTENCE")]
        [SerializeField] [Range(0.1f, 5f)] private float saveDebounceSeconds = 1.25f;

        private GameEngine _gameEngine;
        private IBestScoreStore _bestScoreStore;
        private GameState _currentGameState;
        private IGameStatePersistence _gameStatePersistence;
        private IStatisticsPersistence _statisticsPersistence;
        private ISettingsPersistence _settingsPersistence;
        private GameSaveManager _gameSaveManager;
        private StatisticsManager _statisticsManager;
        private CameraController _cameraController;
        private ScreenLayoutManager _screenLayoutManager;
        private VisualBackgroundManager _visualManager;
        private const string SaveKey = "default";
        private int _currentSeed;
        private int _rescueTokensRemaining = 1;
        private int _sessionHighestCombo;
        private int _sessionDailyMissionCompletions;
        private int _sessionWeeklyMissionCompletions;
        private int _sessionBestAtStart;
        private bool _sessionReachedNewBest;
        private ScoreConfig _scoreConfig;
        private GameMode _currentMode = GameMode.Classic;
        private GameSettings _settingsCache;
        private TutorialService _tutorialService;
        private AnalyticsTelemetryService _analyticsService;
        private AnalyticsSessionContext _analyticsContext;
        private string _lastGameOverGuidanceCode = string.Empty;
        private bool _onboardingSpawnProfileLogged;
        private string _lastGameOverRiskSnapshotCode = string.Empty;
        private readonly Queue<MoveRiskSnapshot> _recentMoveSnapshots = new Queue<MoveRiskSnapshot>(2);
        private bool _pendingGameSave;
        private float _scheduledSaveTime = -1f;
        private bool _gameSaveInFlight;
        private bool _saveRequestedDuringFlight;

        public static event Action<BoardState, Int2[], int> OnBoardChanged;
        public static event Action<int, int, bool> OnScoreChanged;
        public static event Action<ScoreBreakdownInfo> OnScoreBreakdown;
        public static event Action<int> OnBestScoreChanged;
        public static event Action<AnalyticsEventData> OnAnalyticsEvent;
        public static event Action<ShapeDefinition[]> OnBlocksChanged;
        public static event Action<int> OnGameOver;
        public static event Action OnGameStarted;
        public static event Action OnGameContinued;
        public static event Action<TutorialStepPayload> OnTutorialStepChanged;

        public GameEngine Engine => _gameEngine;
        public GameState CurrentState => _currentGameState;
        public IBestScoreStore BestScoreStore => _bestScoreStore;

        public bool IsGameActive => _gameEngine != null && _gameEngine.IsGameStarted && !_gameEngine.IsGameOverState;
        public int CurrentScore => _gameEngine?.Score ?? 0;
        public int BestScore => _bestScoreStore?.GetBestScore() ?? 0;
        public int ScoreFormulaVersion => _gameEngine?.ScoreFormulaVersion ?? _scoreConfig?.FormulaVersion ?? ScoreConfig.DefaultFormulaVersion;
        public string LastGameOverGuidanceCode => _lastGameOverGuidanceCode;
        public string LastGameOverRiskSnapshotCode => _lastGameOverRiskSnapshotCode;

        private bool CanLog => enableDebugLogging && Debug.isDebugBuild;
        private bool IsModesEnabled => gameplayFeatureFlags == null || gameplayFeatureFlags.EnableModes;
        private bool IsMissionsEnabled => gameplayFeatureFlags == null || gameplayFeatureFlags.EnableMissions;
        private bool IsRescueEnabled => gameplayFeatureFlags == null || gameplayFeatureFlags.EnableRescueToken;
        private bool IsExtendedTelemetryEnabled => gameplayFeatureFlags == null || gameplayFeatureFlags.EnableExtendedTelemetry;
        private bool IsTutorialEnabled => false; // gameplayFeatureFlags == null || gameplayFeatureFlags.EnableTutorial;
        
        /// <summary>
        /// Get current available shapes for UI components that may have missed the initial event.
        /// Used by NewBlockTray to handle Start() order race condition.
        /// </summary>
        public ShapeDefinition[] GetCurrentShapes()
        {
            return _currentGameState?.AvailableShapes;
        }

        private void Awake()
        {
            var dataProvider = new UnityPlayerPrefsDataProvider();
            _gameStatePersistence = dataProvider;
            _statisticsPersistence = dataProvider;
            _settingsPersistence = dataProvider;
            _bestScoreStore = new BestScoreStore(new PlayerPrefsStorage());
            _scoreConfig = ResolveScoreConfig();
            ScoringRules.SetDefaultConfig(_scoreConfig);
            _cameraController = new CameraController(() => CanLog);
            _screenLayoutManager = new ScreenLayoutManager();
            _gameSaveManager = new GameSaveManager(_gameStatePersistence, SaveKey);
            _statisticsManager = new StatisticsManager(_statisticsPersistence);
            _tutorialService = new TutorialService(_settingsPersistence, IsTutorialEnabled, () => CanLog);
            _analyticsService = new AnalyticsTelemetryService();
            _visualManager = new VisualBackgroundManager(
                transform,
                useWorldBackgroundLayer,
                preserveAuthoredWorldBackground,
                gameplayBackgroundSpriteOverride,
                gameplayBackgroundTint,
                gameplayBackgroundDimmerColor,
                gameplayCameraClearColor,
                legacyOverlayBackgroundAlpha,
                worldBackgroundSortingOrder,
                worldDimmerSortingOrder,
                legacyOverlayBackgroundName);
            _visualManager.CleanupDuplicates();
            if (CanLog)
                Debug.Log($"[GameBootstrap] Initialized with BestScoreStore (PlayerPrefs), ScoreFormulaVersion={_scoreConfig.FormulaVersion}");
        }

        private void Start()
        {
            SetupCamera();

            // Yeni input sisteminin sahnede olduÄŸundan emin ol (log amaÃ§lÄ±)
            var dragSystem = FindFirstObjectByType<NewDragSystem>();
            if (dragSystem != null)
            {
                if (CanLog) Debug.Log("[GameBootstrap] NewDragSystem found - input system ready");
            }
            else
            {
                if (CanLog) Debug.LogWarning("[GameBootstrap] NewDragSystem not found! Make sure it's in the scene.");
            }

            _tutorialService.StepChanged += OnTutorialStepChangedFromService;
            _analyticsService.AnalyticsEvent += OnAnalyticsEventFromService;
            StartFromLaunchMode();
            ApplyResponsiveLayout(true);
            SubscribeToRemoteScoreChanges();
        }

        private void Update()
        {
            if (_pendingGameSave && !_gameSaveInFlight && Time.unscaledTime >= _scheduledSaveTime)
                FlushPendingGameSave();

            if (!autoAdaptToScreen)
                return;

            if (HasScreenMetricsChanged() || !Application.isPlaying)
            {
                ApplyResponsiveLayout(true);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
                return;

            FlushPendingGameSave();
            _statisticsManager?.FlushPendingStatistics();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                return;

            FlushPendingGameSave();
            _statisticsManager?.FlushPendingStatistics();
        }

        private void OnApplicationQuit()
        {
            FlushPendingGameSave();
            _statisticsManager?.FlushPendingStatistics();
        }

        private void SetupCamera()
        {
            var camera = _cameraController != null
                ? _cameraController.SetupCamera(cameraPosition, cameraSize)
                : Camera.main;
            if (camera == null)
            {
                if (CanLog) Debug.LogWarning("[GameBootstrap] Main Camera not found!");
                return;
            }

            _visualManager?.NormalizeGameplayCamera(camera);
            ApplyResponsiveLayout(true);

            float aspectRatio = (float)Screen.width / Mathf.Max(1, Screen.height);

            if (CanLog)
                Debug.Log($"[GameBootstrap] Responsive camera: Aspect={aspectRatio:F2}, Size={camera.orthographicSize:F1}, Pos={camera.transform.position}");
        }

        private async void StartFromLaunchMode()
        {
            bool loaded = false;
            _currentMode = IsModesEnabled ? GameLaunchState.SelectedMode : GameMode.Classic;
            _settingsCache = await LoadSettingsAsync();
            _tutorialService.SetSettingsCache(_settingsCache);

            if (GameLaunchState.LaunchMode != GameLaunchMode.NewGame)
            {
                loaded = _gameSaveManager != null && await _gameSaveManager.TryLoadSavedGameAsync(ApplyLoadedGameData);
            }

            if (!loaded)
            {
                _tutorialService.MarkForActivation(_currentMode);
                StartNewGame(GameLaunchState.LaunchMode == GameLaunchMode.NewGame);
            }
            else
            {
                _tutorialService.ResetRuntimeState(notify: true);
            }

            GameLaunchState.Reset();
        }

        public void StartNewGame()
        {
            StartNewGame(true);
        }

        public void StartNewGame(bool clearSaved)
        {
            try
            {
                if (clearSaved)
                    ClearSavedGame();

                int actualSeed = gameSeed == -1 ? UnityEngine.Random.Range(1, int.MaxValue) : gameSeed;
                _currentSeed = actualSeed;
                var rng = new SeededRng(actualSeed);
                _scoreConfig = ResolveScoreConfig();
                ScoringRules.SetDefaultConfig(_scoreConfig);
                BeginSessionBestTracking();
                _lastGameOverGuidanceCode = string.Empty;
                _lastGameOverRiskSnapshotCode = string.Empty;
                _onboardingSpawnProfileLogged = false;
                _recentMoveSnapshots.Clear();

                var modeBoardSize = ResolveBoardSizeForMode(_currentMode);
                _gameEngine = new GameEngine(rng, modeBoardSize.width, modeBoardSize.height, _scoreConfig);
                _rescueTokensRemaining = 1;
                _sessionDailyMissionCompletions = 0;
                _sessionWeeklyMissionCompletions = 0;
                RebuildAnalyticsContext();

                if (CanLog)
                    Debug.Log($"[GameBootstrap] Created game engine {modeBoardSize.width}x{modeBoardSize.height}, mode={_currentMode}, seed: {actualSeed}");

                _gameEngine.StartNewGame(actualSeed);
                ApplyModeTuning();
                _currentGameState = _gameEngine.CurrentState;
                _tutorialService.SetEngine(_gameEngine);
                _tutorialService.ActivateIfPending();
                if (!_tutorialService.IsActive)
                    _tutorialService.ResetRuntimeState(notify: false);
                _sessionHighestCombo = Math.Max(0, _currentGameState.Combo);
                var scoreTransaction = ApplyBestScoreTransaction(_currentGameState.Score);

                NotifyGameStarted();
                NotifyBoardChanged();
                NotifyScoreChanged(scoreTransaction);
                NotifyBlocksChanged();
                ApplyResponsiveLayout(true);
                SaveGameIfNeeded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to start new game: {ex.Message}");
            }
        }

        public bool TryPlaceBlock(int slotIndex, Int2 gridAnchor)
        {
            if (_gameEngine == null || _gameEngine.IsGameOver())
            {
                if (CanLog)
                    Debug.LogWarning("[GameBootstrap] Cannot place block: Game not active");
                return false;
            }

            try
            {
                int activeBlockIndex = ConvertSlotToActiveBlockIndex(slotIndex);
                int comboBeforeMove = _currentGameState?.Combo ?? 0;
                ShapeId placedShapeId = _currentGameState != null && _currentGameState.ActiveBlocks.HasBlockAt(slotIndex)
                    ? _currentGameState.ActiveBlocks.GetShapeId(slotIndex)
                    : default;
                if (activeBlockIndex < 0)
                {
                    if (CanLog)
                        Debug.Log($"[GameBootstrap] Move failed: Invalid slot index {slotIndex}");
                    return false;
                }

                var moveResult = _gameEngine.AttemptMove(activeBlockIndex, gridAnchor);

                if (CanLog)
                    Debug.Log($"[GameBootstrap] Move result: {moveResult}");

                if (!moveResult.Success)
                    return false;

                _currentGameState = _gameEngine.CurrentState;
                _tutorialService.UpdateProgress(moveResult, placedShapeId);
                ApplyModeTuning();
                RecordMoveRiskSnapshot(moveResult);
                _sessionHighestCombo = Math.Max(_sessionHighestCombo, _currentGameState.Combo);
                var scoreTransaction = ApplyBestScoreTransaction(_currentGameState.Score);
                _statisticsManager?.RecordMove(moveResult.ScoreDelta, moveResult.LinesCleared);
                _analyticsService.EmitGameplayTelemetry(
                    moveResult,
                    comboBeforeMove,
                    _currentGameState.Combo,
                    _currentGameState.Score,
                    scoreTransaction.BestScoreAfter,
                    scoreTransaction.IsNewBest,
                    _currentGameState.MoveCount,
                    _analyticsContext);

                NotifyBoardChanged(moveResult);
                NotifyScoreBreakdown(moveResult, scoreTransaction.IsNewBest);
                NotifyScoreChanged(scoreTransaction);
                NotifyBlocksChanged();

                if (_gameEngine.IsGameOver())
                {
                    NotifyGameOver();
                }
                else
                {
                    SaveGameIfNeeded();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Error during move: {ex.Message}");
                return false;
            }
        }

        private int ConvertSlotToActiveBlockIndex(int slotIndex)
        {
            if (_currentGameState?.ActiveBlocks == null) return -1;

            if (slotIndex >= 0 && slotIndex < 3 && _currentGameState.ActiveBlocks.HasBlockAt(slotIndex))
                return slotIndex;

            return -1;
        }

        public bool CanPlaceBlock(int slotIndex, Int2 gridAnchor)
        {
            if (_gameEngine == null || _gameEngine.IsGameOver())
                return false;

            int activeBlockIndex = ConvertSlotToActiveBlockIndex(slotIndex);
            if (activeBlockIndex < 0) return false;

            return _gameEngine.IsValidMove(activeBlockIndex, gridAnchor);
        }

        public bool TryContinueAfterRewardedAd()
        {
            if (_gameEngine == null)
                return false;

            bool continued = _gameEngine.TryContinueAfterGameOver();
            if (!continued)
            {
                if (CanLog)
                    Debug.LogWarning("[GameBootstrap] Continue-after-reward failed: Engine did not continue.");
                return false;
            }

            _currentGameState = _gameEngine.CurrentState;

            var scoreTransaction = new ScoreTransactionResult(
                bestScoreAfter: _bestScoreStore?.GetBestScore() ?? 0,
                isNewBest: false);

            NotifyBoardChanged();
            NotifyBlocksChanged();
            NotifyScoreChanged(scoreTransaction);
            FindFirstObjectByType<NewBlockTray>()?.SyncFromCurrentState();
            FindFirstObjectByType<NewDragSystem>()?.SyncAfterContinue();
            OnGameContinued?.Invoke();
            ApplyResponsiveLayout(true);
            SaveGameIfNeeded();

            if (CanLog)
            {
                var shapes = _currentGameState?.AvailableShapes;
                int nonNull = 0;
                int placeable = 0;

                if (shapes != null)
                {
                    for (int i = 0; i < shapes.Length; i++)
                    {
                        var shape = shapes[i];
                        if (shape == null)
                        {
                            Debug.Log($"[GameBootstrap] Continue slot {i}: NULL");
                            continue;
                        }

                        nonNull++;
                        bool hasPlacement = PlacementSearch.HasAnyValidPlacement(_currentGameState.Board, shape);
                        if (hasPlacement)
                            placeable++;

                        Debug.Log($"[GameBootstrap] Continue slot {i}: {shape.Name}, placeable={hasPlacement}");
                    }
                }

                Debug.Log($"[GameBootstrap] Continue-after-reward succeeded. nonNull={nonNull}, placeable={placeable}, gameOver={_currentGameState?.IsGameOver}");
            }

            if (CanLog)
                Debug.Log("[GameBootstrap] Continue flow: tray/input sync dispatched.");

            return true;
        }

        public bool TryUseRescueToken()
        {
            if (!IsRescueEnabled || _gameEngine == null || _rescueTokensRemaining <= 0)
                return false;

            bool continued = _gameEngine.TryContinueAfterGameOver();
            if (!continued)
                return false;

            _rescueTokensRemaining--;
            _currentGameState = _gameEngine.CurrentState;

            var scoreTransaction = new ScoreTransactionResult(
                bestScoreAfter: _bestScoreStore?.GetBestScore() ?? 0,
                isNewBest: false);

            NotifyBoardChanged();
            NotifyBlocksChanged();
            NotifyScoreChanged(scoreTransaction);
            FindFirstObjectByType<NewBlockTray>()?.SyncFromCurrentState();
            FindFirstObjectByType<NewDragSystem>()?.SyncAfterContinue();
            OnGameContinued?.Invoke();
            ApplyResponsiveLayout(true);
            SaveGameIfNeeded();
            return true;
        }

        public int RescueTokensRemaining => _gameEngine != null && _gameEngine.CurrentState != null
            ? Math.Max(0, 3 - _gameEngine.CurrentState.RescueCount)
            : 0;

        public void SkipActiveTutorial()
        {
            _tutorialService?.SkipActiveTutorial();
        }

        public void RecordMissionProgress(int progressDelta, bool dailyCompleted, bool weeklyCompleted)
        {
            if (!IsMissionsEnabled)
                return;
            _statisticsManager?.RecordMissionProgress(progressDelta, dailyCompleted, weeklyCompleted);

            if (dailyCompleted)
                _sessionDailyMissionCompletions++;
            if (weeklyCompleted)
                _sessionWeeklyMissionCompletions++;
            if (dailyCompleted || weeklyCompleted)
                RebuildAnalyticsContext();

            if (dailyCompleted || weeklyCompleted)
            {
                var state = _gameEngine?.CurrentState;
                _analyticsService.EmitMissionCompletedEvent(
                    state?.MoveCount ?? 0,
                    _gameEngine?.Score ?? 0,
                    state?.TotalLinesCleared ?? 0,
                    state?.Combo ?? 0,
                    BestScore,
                    _analyticsContext);
            }
        }

        private void OnDestroy()
        {
            if (_tutorialService != null)
                _tutorialService.StepChanged -= OnTutorialStepChangedFromService;
            if (_analyticsService != null)
                _analyticsService.AnalyticsEvent -= OnAnalyticsEventFromService;
            UnsubscribeFromRemoteScoreChanges();
            FlushPendingGameSave();
            _statisticsManager?.FlushPendingStatistics();
        }

        private bool ApplyLoadedGameData(GameData data)
        {
            if (data == null)
                return false;

            int expectedCells = data.BoardWidth * data.BoardHeight;
            if (data.BoardWidth <= 0 || data.BoardHeight <= 0 || data.BoardCells == null || data.BoardCells.Length != expectedCells)
            {
                if (CanLog)
                    Debug.LogWarning("[GameBootstrap] Saved game data invalid, starting new game.");
                return false;
            }

            try
            {
                int seed = data.RandomSeed > 0 ? data.RandomSeed : UnityEngine.Random.Range(1, int.MaxValue);
                _currentSeed = seed;
                var rng = new SeededRng(seed);
                _scoreConfig = ResolveScoreConfig();
                ScoringRules.SetDefaultConfig(_scoreConfig);
                BeginSessionBestTracking();

                var migration = ScoreFormulaMigration.MigrateInPlace(data, _scoreConfig.FormulaVersion);
                if (CanLog && migration.Migrated)
                    Debug.Log($"[GameBootstrap] {migration.Note}");

                _gameEngine = new GameEngine(rng, data.BoardWidth, data.BoardHeight, _scoreConfig);
                _gameEngine.LoadGame(data.ToGameState());
                _rescueTokensRemaining = 1;
                _sessionDailyMissionCompletions = 0;
                _sessionWeeklyMissionCompletions = 0;
                ApplyModeTuning();

                if (data.SpawnerData != null)
                {
                    _gameEngine.BlockSpawner.RestoreDifficultyState(
                        data.DifficultyLevel,
                        data.SpawnerData.TotalPlacements,
                        data.SpawnerData.RecentSuccessRate,
                        data.SpawnerData.OverallSuccessRate,
                        data.SpawnerData.RecentPlacementHistory);
                }
                else
                {
                    _gameEngine.BlockSpawner.RestoreDifficultyState(
                        data.DifficultyLevel,
                        0,
                        0f,
                        0f,
                        null);
                }

                _currentGameState = _gameEngine.CurrentState;
                _sessionHighestCombo = Math.Max(0, _currentGameState.Combo);
                var scoreTransaction = ApplyBestScoreTransaction(_currentGameState.Score);

                NotifyGameStarted();
                NotifyBoardChanged();
                NotifyScoreChanged(scoreTransaction);
                NotifyBlocksChanged();
                ApplyResponsiveLayout(true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to apply saved game: {ex.Message}");
                return false;
            }
        }

        private void SaveGameIfNeeded()
        {
            if (_gameSaveManager == null || _gameEngine == null || _currentGameState == null)
                return;

            _pendingGameSave = true;
            _scheduledSaveTime = Time.unscaledTime + Mathf.Max(0.1f, saveDebounceSeconds);
        }

        private void ClearSavedGame()
        {
            _pendingGameSave = false;
            _scheduledSaveTime = -1f;
            _ = _gameSaveManager?.ClearSavedGameAsync();
        }

        private async void FlushPendingGameSave()
        {
            if (!_pendingGameSave || _gameSaveManager == null || _gameEngine == null || _currentGameState == null)
                return;

            if (_gameSaveInFlight)
            {
                _saveRequestedDuringFlight = true;
                return;
            }

            _pendingGameSave = false;
            _gameSaveInFlight = true;

            try
            {
                await _gameSaveManager.SaveGameIfNeededAsync(
                    _gameEngine,
                    _currentGameState,
                    _currentSeed,
                    _gameEngine?.ScoreFormulaVersion ?? _scoreConfig?.FormulaVersion ?? ScoreConfig.DefaultFormulaVersion);
            }
            finally
            {
                _gameSaveInFlight = false;

                if (_saveRequestedDuringFlight)
                {
                    _saveRequestedDuringFlight = false;
                    _pendingGameSave = true;
                    _scheduledSaveTime = Time.unscaledTime + Mathf.Max(0.1f, saveDebounceSeconds);
                }
            }
        }

        private void NotifyGameStarted()
        {
            OnGameStarted?.Invoke();
            _tutorialService.RefreshStepState();
        }

        private void NotifyBoardChanged(MoveResult moveResult = null)
        {
            if (_currentGameState?.Board == null)
                return;

            Int2[] clearedPositions = Array.Empty<Int2>();
            int linesCleared = 0;

            if (moveResult != null && moveResult.Success)
            {
                linesCleared = moveResult.LinesCleared;
                clearedPositions = moveResult.ClearedPositions ?? Array.Empty<Int2>();
            }

            OnBoardChanged?.Invoke(_currentGameState.Board, clearedPositions, linesCleared);
        }

        private void NotifyScoreBreakdown(MoveResult moveResult, bool isNewBest)
        {
            if (moveResult == null || !moveResult.Success)
                return;

            int totalScore = _gameEngine?.Score ?? 0;
            var breakdown = new ScoreBreakdownInfo(moveResult.ScoreResult, totalScore, isNewBest);
            OnScoreBreakdown?.Invoke(breakdown);
        }

        private void NotifyScoreChanged(ScoreTransactionResult transaction)
        {
            int currentScore = _gameEngine?.Score ?? 0;
            int bestScore = transaction.BestScoreAfter;

            OnScoreChanged?.Invoke(currentScore, bestScore, transaction.IsNewBest);

            if (transaction.IsNewBest)
                OnBestScoreChanged?.Invoke(bestScore);
        }

        // ────────────────────────────────────────────────────────────────
        // Remote Firestore score sync
        // ────────────────────────────────────────────────────────────────

        private void SubscribeToRemoteScoreChanges()
        {
            if (FirebaseManager.Instance != null)
                FirebaseManager.Instance.OnRemoteScoreChanged += HandleRemoteScoreChanged;
        }

        private void UnsubscribeFromRemoteScoreChanges()
        {
            if (FirebaseManager.Instance != null)
                FirebaseManager.Instance.OnRemoteScoreChanged -= HandleRemoteScoreChanged;
        }

        private void HandleRemoteScoreChanged(int remoteScore)
        {
            if (_bestScoreStore == null)
                return;

            int localBest = _bestScoreStore.GetBestScore();

            // Always accept remote score (allows admin override in either direction)
            if (remoteScore == localBest)
                return;

            _bestScoreStore.ForceSetBestScore(remoteScore);

            int currentScore = _gameEngine?.Score ?? 0;
            bool isNewBest = remoteScore > localBest;

            if (CanLog)
                Debug.Log($"[GameBootstrap] Remote score applied: {localBest} → {remoteScore}");

            OnScoreChanged?.Invoke(currentScore, remoteScore, isNewBest);
            OnBestScoreChanged?.Invoke(remoteScore);
        }

        private async Task<GameSettings> LoadSettingsAsync()
        {
            if (_settingsPersistence == null)
                return GameSettings.CreateDefault();

            try
            {
                return await _settingsPersistence.LoadSettingsAsync() ?? GameSettings.CreateDefault();
            }
            catch (Exception ex)
            {
                if (CanLog)
                    Debug.LogWarning($"[GameBootstrap] Settings load failed for tutorial gating: {ex.Message}");
                return GameSettings.CreateDefault();
            }
        }

        private void OnTutorialStepChangedFromService(TutorialStepPayload payload)
        {
            OnTutorialStepChanged?.Invoke(payload);
        }

        private void OnAnalyticsEventFromService(AnalyticsEventData payload)
        {
            OnAnalyticsEvent?.Invoke(payload);
        }

        private void RebuildAnalyticsContext()
        {
            _analyticsContext = new AnalyticsSessionContext(
                gameMode: _currentMode.ToString(),
                dailyMissionCompletions: _sessionDailyMissionCompletions,
                weeklyMissionCompletions: _sessionWeeklyMissionCompletions,
                scoreFormulaVersion: ScoreFormulaVersion,
                isExtendedTelemetryEnabled: IsExtendedTelemetryEnabled,
                schemaVersion: AnalyticsSchemaVersion);
        }

        private string ResolveGameOverGuidanceCode()
        {
            var tutorialCode = _tutorialService.GetGuidanceCode();
            if (!string.IsNullOrEmpty(tutorialCode))
                return tutorialCode;

            var state = _currentGameState;
            if (state == null)
                return string.Empty;

            if (state.MoveCount <= 5 && state.TotalLinesCleared <= 0)
                return "generic_space";

            if (_recentMoveSnapshots.Count > 0)
            {
                var snapshots = _recentMoveSnapshots.ToArray();
                var latest = snapshots[snapshots.Length - 1];
                if (latest.AvailableThreeByThreeCount <= 0)
                    return "tutorial_3x3";
                if (latest.FutureOpenAreaScore < 0.22f || latest.LargestEmptyRectangleArea < 5)
                    return "generic_space";
            }

            return string.Empty;
        }

        private void NotifyBlocksChanged()
        {
            if (_currentGameState?.AvailableShapes == null) return;
            
            var shapes = _currentGameState.AvailableShapes;
            
            // Debug logging: Show exactly what shapes are being sent to UI
            if (CanLog)
            {
                int nonNullCount = 0;
                for (int i = 0; i < shapes.Length; i++)
                {
                    var shape = shapes[i];
                    if (shape != null)
                    {
                        nonNullCount++;
                        Debug.Log($"[GameBootstrap.NotifyBlocksChanged] Slot {i}: {shape.Name} (ShapeId: {shape.Id})");
                    }
                    else
                    {
                        Debug.Log($"[GameBootstrap.NotifyBlocksChanged] Slot {i}: NULL (empty)");
                    }
                }
                
                // CRITICAL: If ActiveBlocks is full but we have less than 3 non-null shapes, there's a bug!
                if (_currentGameState.ActiveBlocks.IsFull && nonNullCount < 3)
                {
                    Debug.LogError($"[GameBootstrap.NotifyBlocksChanged] BUG DETECTED: ActiveBlocks.IsFull=true but only {nonNullCount} shapes are non-null! Expected 3.");
                    Debug.LogError($"[GameBootstrap.NotifyBlocksChanged] ActiveBlocks state: {_currentGameState.ActiveBlocks}");
                }
            }
            
            OnBlocksChanged?.Invoke(shapes);
        }

        private void NotifyGameOver()
        {
            int finalScore = _gameEngine?.Score ?? 0;
            _lastGameOverGuidanceCode = ResolveGameOverGuidanceCode();
            _lastGameOverRiskSnapshotCode = BuildGameOverRiskSnapshotCode();
            ClearSavedGame();
            RecordStatisticsOnGameOver(finalScore);
            _tutorialService.ResetRuntimeState(notify: true);
            OnGameOver?.Invoke(finalScore);

            if (CanLog)
                Debug.Log($"[GameBootstrap] Game Over! Final Score: {finalScore}, Best: {BestScore}");
        }

        private void RecordStatisticsOnGameOver(int finalScore)
        {
            _statisticsManager?.RecordGameSession(_currentGameState, finalScore, _sessionHighestCombo);
        }

        public bool IsCurrentSessionNewBest(int score)
        {
            return _sessionReachedNewBest || EvaluateIsNewBestScore(score, _sessionBestAtStart);
        }

        public static bool EvaluateIsNewBestScore(int score, int bestScoreBefore)
        {
            return score > 0 && score > bestScoreBefore;
        }

        private void BeginSessionBestTracking()
        {
            _sessionBestAtStart = _bestScoreStore?.GetBestScore() ?? 0;
            _sessionReachedNewBest = false;
        }

        private ScoreTransactionResult ApplyBestScoreTransaction(int score)
        {
            int bestBefore = _bestScoreStore?.GetBestScore() ?? 0;
            bool isNewBest = EvaluateIsNewBestScore(score, bestBefore);
            int bestAfter = bestBefore;

            if (isNewBest && _bestScoreStore != null)
            {
                _bestScoreStore.SetBestScore(score);
                bestAfter = _bestScoreStore.GetBestScore();
                _sessionReachedNewBest = true;
            }

            return new ScoreTransactionResult(bestAfter, isNewBest);
        }

        private ScoreConfig ResolveScoreConfig()
        {
            if (scoreConfigAsset == null)
                return ScoreConfig.Default;

            try
            {
                var resolved = scoreConfigAsset.ToCoreConfig();
                return resolved ?? ScoreConfig.Default;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to resolve ScoreConfigAsset: {ex.Message}. Falling back to default config.");
                return ScoreConfig.Default;
            }
        }

        private readonly struct ScoreTransactionResult
        {
            public readonly int BestScoreAfter;
            public readonly bool IsNewBest;

            public ScoreTransactionResult(int bestScoreAfter, bool isNewBest)
            {
                BestScoreAfter = bestScoreAfter;
                IsNewBest = isNewBest;
            }
        }

        private readonly struct MoveRiskSnapshot
        {
            public readonly int MoveNumber;
            public readonly int LinesCleared;
            public readonly float FutureOpenAreaScore;
            public readonly int LargestEmptyRectangleArea;
            public readonly int AvailableThreeByThreeCount;

            public MoveRiskSnapshot(
                int moveNumber,
                int linesCleared,
                float futureOpenAreaScore,
                int largestEmptyRectangleArea,
                int availableThreeByThreeCount)
            {
                MoveNumber = moveNumber;
                LinesCleared = linesCleared;
                FutureOpenAreaScore = futureOpenAreaScore;
                LargestEmptyRectangleArea = largestEmptyRectangleArea;
                AvailableThreeByThreeCount = availableThreeByThreeCount;
            }
        }

        private bool HasScreenMetricsChanged()
        {
            return _screenLayoutManager != null && _screenLayoutManager.HasScreenMetricsChanged();
        }

        private void ApplyResponsiveLayout(bool forceTrayRefresh)
        {
            _screenLayoutManager?.ApplyResponsiveLayout(
                cameraPosition,
                cameraSize,
                lockPortraitOrientation,
                cameraHorizontalPadding,
                minAdaptiveCameraSize,
                maxAdaptiveCameraSize,
                canvasReferenceResolution,
                canvasMatchWidthOrHeight,
                boardWidth,
                _currentGameState,
                forceTrayRefresh);

            _visualManager?.ApplyVisualReadability(Camera.main);
        }

        public void RefreshThemeBackground(Sprite background, Color tint, Color dimmer, Color clearColor)
        {
            _visualManager?.SetThemeBackground(background, tint, dimmer, clearColor);
            _visualManager?.NormalizeGameplayCamera(Camera.main);
            _visualManager?.ApplyVisualReadability(Camera.main);
        }

        private void OnValidate()
        {
            boardWidth = Mathf.Clamp(boardWidth, 4, 20);
            boardHeight = Mathf.Clamp(boardHeight, 4, 20);
            challengeBoardWidth = Mathf.Clamp(challengeBoardWidth, 4, 20);
            challengeBoardHeight = Mathf.Clamp(challengeBoardHeight, 4, 20);
            zenBoardWidth = Mathf.Clamp(zenBoardWidth, 4, 20);
            zenBoardHeight = Mathf.Clamp(zenBoardHeight, 4, 20);
            cameraHorizontalPadding = Mathf.Clamp(cameraHorizontalPadding, 0f, 2f);
            minAdaptiveCameraSize = Mathf.Max(1f, minAdaptiveCameraSize);
            maxAdaptiveCameraSize = Mathf.Max(minAdaptiveCameraSize, maxAdaptiveCameraSize);
            canvasReferenceResolution.x = Mathf.Max(320f, canvasReferenceResolution.x);
            canvasReferenceResolution.y = Mathf.Max(320f, canvasReferenceResolution.y);
            canvasMatchWidthOrHeight = Mathf.Clamp01(canvasMatchWidthOrHeight);
            legacyOverlayBackgroundAlpha = Mathf.Clamp01(legacyOverlayBackgroundAlpha);
            if (_visualManager != null)
                _visualManager.CleanupDuplicates();
        }

        private (int width, int height) ResolveBoardSizeForMode(GameMode mode)
        {
            return mode switch
            {
                GameMode.Challenge => (challengeBoardWidth, challengeBoardHeight),
                GameMode.Zen => (zenBoardWidth, zenBoardHeight),
                _ => (boardWidth, boardHeight)
            };
        }

        private void ApplyModeTuning()
        {
            if (_gameEngine?.BlockSpawner == null)
                return;

            switch (_currentMode)
            {
                case GameMode.Challenge:
                    _gameEngine.BlockSpawner.MaxGenerationAttempts = 4;
                    _gameEngine.BlockSpawner.UseSafetyChecks = true;
                    _gameEngine.BlockSpawner.UseFutureSolvabilityChecks = true;
                    _gameEngine.BlockSpawner.UseMiniBag = true;
                    _gameEngine.BlockSpawner.DifficultyModel.TargetSuccessRate = 0.52f;
                    _gameEngine.BlockSpawner.DifficultyModel.AdaptationRate = 0.28f;
                    _gameEngine.BlockSpawner.MinFutureOpenAreaScore = 0.2f;
                    _gameEngine.BlockSpawner.MinLargestEmptyRectangleArea = 5;
                    _rescueTokensRemaining = 0;
                    break;
                case GameMode.Zen:
                    _gameEngine.BlockSpawner.MaxGenerationAttempts = 7;
                    _gameEngine.BlockSpawner.UseSafetyChecks = true;
                    _gameEngine.BlockSpawner.UseFutureSolvabilityChecks = true;
                    _gameEngine.BlockSpawner.UseMiniBag = true;
                    _gameEngine.BlockSpawner.DifficultyModel.TargetSuccessRate = 0.72f;
                    _gameEngine.BlockSpawner.DifficultyModel.AdaptationRate = 0.12f;
                    _gameEngine.BlockSpawner.MinFutureOpenAreaScore = 0.24f;
                    _gameEngine.BlockSpawner.MinLargestEmptyRectangleArea = 7;
                    _rescueTokensRemaining = 2;
                    break;
                default:
                    _gameEngine.BlockSpawner.MaxGenerationAttempts = 5;
                    _gameEngine.BlockSpawner.UseSafetyChecks = true;
                    _gameEngine.BlockSpawner.UseFutureSolvabilityChecks = true;
                    _gameEngine.BlockSpawner.UseMiniBag = true;
                    _gameEngine.BlockSpawner.DifficultyModel.TargetSuccessRate = 0.6f;
                    _gameEngine.BlockSpawner.DifficultyModel.AdaptationRate = 0.2f;
                    _gameEngine.BlockSpawner.MinFutureOpenAreaScore = 0.22f;
                    _gameEngine.BlockSpawner.MinLargestEmptyRectangleArea = 6;
                    _rescueTokensRemaining = 1;
                    break;
            }

            ApplyOnboardingSpawnProfileIfNeeded();
        }

        private void ApplyOnboardingSpawnProfileIfNeeded()
        {
            if (_gameEngine?.BlockSpawner == null || _currentMode != GameMode.Classic)
                return;

            bool tutorialCompleted = _settingsCache != null && _settingsCache.TutorialCompleted;
            int moveCount = _currentGameState?.MoveCount ?? 0;
            bool shouldEaseFirstSession = _tutorialService.IsActive || (!tutorialCompleted && moveCount < 12);

            if (!shouldEaseFirstSession)
                return;

            if (!_onboardingSpawnProfileLogged)
            {
                AppAnalytics.TrackOnboardingSpawnProfileApplied(moveCount, _tutorialService.IsActive);
                _onboardingSpawnProfileLogged = true;
            }

            _gameEngine.BlockSpawner.MaxGenerationAttempts = Mathf.Max(_gameEngine.BlockSpawner.MaxGenerationAttempts, 8);
            _gameEngine.BlockSpawner.UseSafetyChecks = true;
            _gameEngine.BlockSpawner.UseFutureSolvabilityChecks = true;
            _gameEngine.BlockSpawner.UseMiniBag = true;
            _gameEngine.BlockSpawner.DifficultyModel.TargetSuccessRate = Mathf.Max(_gameEngine.BlockSpawner.DifficultyModel.TargetSuccessRate, 0.74f);
            _gameEngine.BlockSpawner.DifficultyModel.AdaptationRate = Mathf.Min(_gameEngine.BlockSpawner.DifficultyModel.AdaptationRate, 0.1f);
            _gameEngine.BlockSpawner.DifficultyModel.MinDifficulty = 0.05f;
            _gameEngine.BlockSpawner.MinFutureOpenAreaScore = Mathf.Max(_gameEngine.BlockSpawner.MinFutureOpenAreaScore, 0.24f);
            _gameEngine.BlockSpawner.MinLargestEmptyRectangleArea = Mathf.Max(_gameEngine.BlockSpawner.MinLargestEmptyRectangleArea, 7);
        }

        private void RecordMoveRiskSnapshot(MoveResult moveResult)
        {
            if (_currentGameState?.Board == null)
                return;

            var snapshot = BoardHeuristics.Evaluate(_currentGameState.Board, _currentGameState.AvailableShapes);
            _recentMoveSnapshots.Enqueue(new MoveRiskSnapshot(
                _currentGameState.MoveCount,
                moveResult?.LinesCleared ?? 0,
                snapshot.FutureOpenAreaScore,
                snapshot.LargestEmptyRectangleArea,
                snapshot.AvailableThreeByThreeCount));

            while (_recentMoveSnapshots.Count > 2)
                _recentMoveSnapshots.Dequeue();
        }

        private string BuildGameOverRiskSnapshotCode()
        {
            if (_recentMoveSnapshots.Count == 0)
                return string.Empty;

            var snapshots = _recentMoveSnapshots.ToArray();
            if (snapshots.Length >= 2)
            {
                var previous = snapshots[snapshots.Length - 2];
                var latest = snapshots[snapshots.Length - 1];
                return $"m{previous.MoveNumber}:{previous.AvailableThreeByThreeCount}x3/{previous.LargestEmptyRectangleArea}rect/{previous.FutureOpenAreaScore:F2}|m{latest.MoveNumber}:{latest.AvailableThreeByThreeCount}x3/{latest.LargestEmptyRectangleArea}rect/{latest.FutureOpenAreaScore:F2}";
            }

            var only = snapshots[0];
            return $"m{only.MoveNumber}:{only.AvailableThreeByThreeCount}x3/{only.LargestEmptyRectangleArea}rect/{only.FutureOpenAreaScore:F2}";
        }
    }
}
