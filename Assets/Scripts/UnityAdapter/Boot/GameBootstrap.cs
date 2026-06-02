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
using BlockPuzzle.UnityAdapter;
using UnityEngine.UI;
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
        [SerializeField] private Color gameplayCameraClearColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);
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
        private SpriteRenderer _worldBackgroundRenderer;
        private SpriteRenderer _worldDimmerRenderer;
        private Image _legacyOverlayBackground;
        private Sprite _generatedBackdropSprite;
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
        private bool _isTutorialRunActive;
        private int _tutorialStepIndex;
        private bool _tutorialPendingThreeByThreeSet;
        private bool _tutorialThreeByThreeSetApplied;
        private string _lastGameOverGuidanceCode = string.Empty;
        private bool _onboardingSpawnProfileLogged;
        private string _lastGameOverRiskSnapshotCode = string.Empty;
        private readonly Queue<MoveRiskSnapshot> _recentMoveSnapshots = new Queue<MoveRiskSnapshot>(2);
        private bool _pendingGameSave;
        private float _scheduledSaveTime = -1f;
        private bool _gameSaveInFlight;
        private bool _saveRequestedDuringFlight;
        private static readonly ShapeId[] TutorialOpeningSet =
        {
            ShapeLibrary.Single,
            new ShapeId(3),
            new ShapeId(8)
        };
        private static readonly ShapeId[] TutorialThreeByThreeSet =
        {
            new ShapeId(9),
            ShapeLibrary.Single,
            new ShapeId(5)
        };

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
            CleanupBackdropDuplicates();
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

            StartFromLaunchMode();
            ApplyResponsiveLayout(true);
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

            NormalizeGameplayCamera(camera);
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

            if (GameLaunchState.LaunchMode != GameLaunchMode.NewGame)
            {
                loaded = _gameSaveManager != null && await _gameSaveManager.TryLoadSavedGameAsync(ApplyLoadedGameData);
            }

            if (!loaded)
            {
                _isTutorialRunActive = ShouldActivateTutorialForNewRun();
                StartNewGame(GameLaunchState.LaunchMode == GameLaunchMode.NewGame);
            }
            else
            {
                ResetTutorialRuntimeState(notify: true);
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

                if (CanLog)
                    Debug.Log($"[GameBootstrap] Created game engine {modeBoardSize.width}x{modeBoardSize.height}, mode={_currentMode}, seed: {actualSeed}");

                _gameEngine.StartNewGame(actualSeed);
                ApplyModeTuning();
                _currentGameState = _gameEngine.CurrentState;
                if (_isTutorialRunActive)
                    ActivateTutorialRun();
                else
                    ResetTutorialRuntimeState(notify: false);
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
                UpdateTutorialProgress(moveResult, placedShapeId);
                ApplyModeTuning();
                RecordMoveRiskSnapshot(moveResult);
                _sessionHighestCombo = Math.Max(_sessionHighestCombo, _currentGameState.Combo);
                var scoreTransaction = ApplyBestScoreTransaction(_currentGameState.Score);
                _statisticsManager?.RecordMove(moveResult.ScoreDelta, moveResult.LinesCleared);
                EmitGameplayTelemetry(moveResult, scoreTransaction, comboBeforeMove, _currentGameState.Combo);

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

        public int RescueTokensRemaining => _rescueTokensRemaining;

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
            {
                var state = _gameEngine?.CurrentState;
                EmitAnalyticsEvent(
                    AnalyticsEventName.MissionCompleted,
                    state?.MoveCount ?? 0,
                    _gameEngine?.Score ?? 0,
                    scoreDelta: 0,
                    linesCleared: state?.TotalLinesCleared ?? 0,
                    comboBefore: state?.Combo ?? 0,
                    comboAfter: state?.Combo ?? 0,
                    bestScore: BestScore,
                    isNewBest: false,
                    isScoreAnomaly: false,
                    scoreAnomalyCode: string.Empty);
            }
        }

        private void OnDestroy()
        {
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
            EmitTutorialStepState();
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

        private void EmitGameplayTelemetry(MoveResult moveResult, ScoreTransactionResult scoreTransaction, int comboBeforeMove, int comboAfterMove)
        {
            if (moveResult == null || !moveResult.Success)
                return;

            int currentScore = _gameEngine?.Score ?? 0;
            int bestScore = scoreTransaction.BestScoreAfter;
            int sessionMoveCount = _currentGameState?.MoveCount ?? 0;
            bool isScoreAnomaly = TryGetScoreAnomalyCode(
                scoreDelta: moveResult.ScoreDelta,
                linesCleared: moveResult.LinesCleared,
                totalScore: currentScore,
                comboBefore: comboBeforeMove,
                comboAfter: comboAfterMove,
                anomalyCode: out string anomalyCode);

            EmitAnalyticsEvent(
                AnalyticsEventName.MoveScored,
                sessionMoveCount,
                currentScore,
                moveResult.ScoreDelta,
                moveResult.LinesCleared,
                comboBeforeMove,
                comboAfterMove,
                bestScore,
                scoreTransaction.IsNewBest,
                isScoreAnomaly,
                anomalyCode);

            if (moveResult.LinesCleared > 0)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.LineCleared,
                    sessionMoveCount,
                    currentScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    scoreTransaction.IsNewBest,
                    isScoreAnomaly,
                    anomalyCode);
            }

            if (comboBeforeMove != comboAfterMove)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.ComboChanged,
                    sessionMoveCount,
                    currentScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    scoreTransaction.IsNewBest,
                    isScoreAnomaly,
                    anomalyCode);
            }

            if (scoreTransaction.IsNewBest)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.BestScoreUpdated,
                    sessionMoveCount,
                    currentScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    isNewBest: true,
                    isScoreAnomaly: isScoreAnomaly,
                    scoreAnomalyCode: anomalyCode);
            }
        }

        private void EmitAnalyticsEvent(
            string eventName,
            int sessionMoveCount,
            int totalScore,
            int scoreDelta,
            int linesCleared,
            int comboBefore,
            int comboAfter,
            int bestScore,
            bool isNewBest,
            bool isScoreAnomaly,
            string scoreAnomalyCode)
        {
            var payload = new AnalyticsEventData(
                eventName: eventName,
                schemaVersion: AnalyticsSchemaVersion,
                scoreFormulaVersion: ScoreFormulaVersion,
                sessionMoveCount: sessionMoveCount,
                totalScore: totalScore,
                scoreDelta: scoreDelta,
                linesCleared: linesCleared,
                comboBefore: comboBefore,
                comboAfter: comboAfter,
                bestScore: bestScore,
                isNewBest: isNewBest,
                isScoreAnomaly: isScoreAnomaly,
                scoreAnomalyCode: scoreAnomalyCode,
                timestampUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                gameMode: _currentMode.ToString(),
                dailyMissionCompletions: IsExtendedTelemetryEnabled ? _sessionDailyMissionCompletions : -1,
                weeklyMissionCompletions: IsExtendedTelemetryEnabled ? _sessionWeeklyMissionCompletions : -1);

            OnAnalyticsEvent?.Invoke(payload);
        }

        private static bool TryGetScoreAnomalyCode(
            int scoreDelta,
            int linesCleared,
            int totalScore,
            int comboBefore,
            int comboAfter,
            out string anomalyCode)
        {
            anomalyCode = string.Empty;

            if (scoreDelta < 0)
            {
                anomalyCode = "NEGATIVE_SCORE_DELTA";
                return true;
            }

            if (totalScore < 0)
            {
                anomalyCode = "NEGATIVE_TOTAL_SCORE";
                return true;
            }

            if (linesCleared < 0)
            {
                anomalyCode = "NEGATIVE_LINES_CLEARED";
                return true;
            }

            if (linesCleared > 0 && scoreDelta <= 0)
            {
                anomalyCode = "LINES_WITHOUT_SCORE";
                return true;
            }

            if (linesCleared > 0 && comboAfter <= comboBefore)
            {
                anomalyCode = "COMBO_NOT_INCREASED_AFTER_CLEAR";
                return true;
            }

            if (linesCleared == 0 && comboAfter > comboBefore)
            {
                anomalyCode = "COMBO_INCREASED_WITHOUT_CLEAR";
                return true;
            }

            return false;
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

        private bool ShouldActivateTutorialForNewRun()
        {
            if (!IsTutorialEnabled)
                return false;

            if (_currentMode != GameMode.Classic)
                return false;

            if (GameLaunchState.ForceTutorialReplay)
                return true;

            return _settingsCache == null || !_settingsCache.TutorialCompleted;
        }

        private void ActivateTutorialRun()
        {
            _isTutorialRunActive = true;
            _tutorialStepIndex = 1;
            _tutorialPendingThreeByThreeSet = false;
            _tutorialThreeByThreeSetApplied = false;

            AppAnalytics.TrackTutorialStarted("first_session");
            ApplyTutorialBlockSet(TutorialOpeningSet);
        }

        private void ApplyTutorialBlockSet(ShapeId[] shapeIds)
        {
            if (_gameEngine == null || shapeIds == null || shapeIds.Length == 0)
                return;

            _gameEngine.OverrideActiveBlocks(shapeIds);
            _currentGameState = _gameEngine.CurrentState;
        }

        private void UpdateTutorialProgress(MoveResult moveResult, ShapeId placedShapeId)
        {
            if (!_isTutorialRunActive || moveResult == null || !moveResult.Success)
                return;

            if (_tutorialStepIndex <= 1)
            {
                _tutorialStepIndex = 2;
                EmitTutorialStepState();
                return;
            }

            if (_tutorialStepIndex == 2 && moveResult.LinesCleared > 0)
            {
                _tutorialStepIndex = 3;
                _tutorialPendingThreeByThreeSet = true;

                if (moveResult.TriggersSpawn)
                    ApplyPendingTutorialSpawnOverrideIfNeeded();

                EmitTutorialStepState();
                return;
            }

            if (_tutorialStepIndex == 3)
            {
                if (_tutorialPendingThreeByThreeSet && moveResult.TriggersSpawn)
                    ApplyPendingTutorialSpawnOverrideIfNeeded();

                if (placedShapeId.Equals(new ShapeId(9)))
                    CompleteTutorialRun("first_session");
            }
        }

        private void ApplyPendingTutorialSpawnOverrideIfNeeded()
        {
            if (!_isTutorialRunActive || !_tutorialPendingThreeByThreeSet || _tutorialThreeByThreeSetApplied)
                return;

            ApplyTutorialBlockSet(TutorialThreeByThreeSet);
            _tutorialPendingThreeByThreeSet = false;
            _tutorialThreeByThreeSetApplied = true;
        }

        public void SkipActiveTutorial()
        {
            if (!_isTutorialRunActive)
                return;

            CompleteTutorialRun("first_session_skipped");
        }

        private async void CompleteTutorialRun(string analyticsContext)
        {
            if (!_isTutorialRunActive)
                return;

            _isTutorialRunActive = false;
            _tutorialPendingThreeByThreeSet = false;
            _tutorialThreeByThreeSetApplied = false;

            AppAnalytics.TrackTutorialCompleted(string.IsNullOrWhiteSpace(analyticsContext) ? "first_session" : analyticsContext);

            _settingsCache ??= GameSettings.CreateDefault();
            _settingsCache.TutorialCompleted = true;

            if (_settingsPersistence != null)
            {
                try
                {
                    await _settingsPersistence.SaveSettingsAsync(_settingsCache);
                }
                catch (Exception ex)
                {
                    if (CanLog)
                        Debug.LogWarning($"[GameBootstrap] Tutorial completion could not be persisted: {ex.Message}");
                }
            }

            EmitTutorialStepState();
        }

        private void ResetTutorialRuntimeState(bool notify)
        {
            _isTutorialRunActive = false;
            _tutorialStepIndex = 0;
            _tutorialPendingThreeByThreeSet = false;
            _tutorialThreeByThreeSetApplied = false;
            _onboardingSpawnProfileLogged = false;

            if (notify)
                EmitTutorialStepState();
        }

        private string ResolveGameOverGuidanceCode()
        {
            if (_isTutorialRunActive)
            {
                return _tutorialStepIndex switch
                {
                    <= 1 => "tutorial_place",
                    2 => "tutorial_clear",
                    3 => "tutorial_3x3",
                    _ => "tutorial_generic"
                };
            }

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

        private void EmitTutorialStepState()
        {
            if (!_isTutorialRunActive)
            {
                OnTutorialStepChanged?.Invoke(new TutorialStepPayload(false, 0, 3, string.Empty, string.Empty));
                return;
            }

            string title;
            string description;

            var lang = UI.Localization.LanguageManager.Instance != null 
                ? UI.Localization.LanguageManager.Instance.CurrentLanguage 
                : UI.Localization.LanguageManager.Language.Turkish;

            if (lang == UI.Localization.LanguageManager.Language.Korean)
            {
                switch (_tutorialStepIndex)
                {
                    case 1:
                        title = "첫 번째 블록 배치하기";
                        description = "블록을 드래그하여 그리드에 올려놓으세요. 첫 번째 목표는 블록 배치를 익히는 것입니다.";
                        break;
                    case 2:
                        title = "가로 줄 또는 세로 줄 지우기";
                        description = "가로 또는 세로 한 줄을 가득 채우면 자동으로 지워집니다. 블록을 채워 줄을 지워보세요.";
                        break;
                    case 3:
                        title = "3x3 공간 확보하기";
                        description = "큰 3x3 블록이 들어갈 수 있는 빈 공간을 남겨두는 것이 중요합니다. 항상 충분한 공간을 유지하세요.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }
            else if (lang == UI.Localization.LanguageManager.Language.English)
            {
                switch (_tutorialStepIndex)
                {
                    case 1:
                        title = "Place the First Block";
                        description = "Drag any block and drop it on the grid. The first goal is simply to get a feel for placing blocks.";
                        break;
                    case 2:
                        title = "Clear a Row or Column";
                        description = "A fully filled row or column is automatically cleared. Place your blocks to trigger a clear.";
                        break;
                    case 3:
                        title = "Keep 3x3 Space Free";
                        description = "Large block shapes require a 3x3 empty area. Keep enough open space to accommodate future 3x3 blocks.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }
            else
            {
                switch (_tutorialStepIndex)
                {
                    case 1:
                        title = "İlk Bloğu Yerleştir";
                        description = "Herhangi bir bloğu sürükleyip grid üzerine bırak. İlk hedef sadece yerleştirme mantığını hissetmek.";
                        break;
                    case 2:
                        title = "Bir Satır Veya Sütun Temizle";
                        description = "Tam dolu bir satır ya da sütun otomatik temizlenir. Bloklarını bu temizlemeyi kuracak şekilde kullan.";
                        break;
                    case 3:
                        title = "3x3 Alan Açık Tut";
                        description = "Büyük kare bloklar için 3x3 boşluk lazım. Sıradaki 3x3 bloğu yerleştirebileceğin kadar alan koru.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }

            OnTutorialStepChanged?.Invoke(new TutorialStepPayload(true, _tutorialStepIndex, 3, title, description));
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
            ResetTutorialRuntimeState(notify: true);
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

            EnsureGameplayVisualReadability(Camera.main);
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
            CleanupBackdropDuplicates();
        }

        private void CleanupBackdropDuplicates()
        {
            _worldBackgroundRenderer = CleanupBackdropDuplicatesForName("SceneBackground", _worldBackgroundRenderer);
            _worldDimmerRenderer = CleanupBackdropDuplicatesForName("SceneBackgroundDimmer", _worldDimmerRenderer);
        }

        private SpriteRenderer CleanupBackdropDuplicatesForName(string objectName, SpriteRenderer preferred)
        {
            SpriteRenderer keptRenderer = preferred;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.name != objectName)
                    continue;

                var childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer == null)
                    continue;

                if (keptRenderer == null)
                {
                    keptRenderer = childRenderer;
                    continue;
                }

                if (childRenderer == keptRenderer)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            return keptRenderer;
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
            bool shouldEaseFirstSession = _isTutorialRunActive || (!tutorialCompleted && moveCount < 12);

            if (!shouldEaseFirstSession)
                return;

            if (!_onboardingSpawnProfileLogged)
            {
                AppAnalytics.TrackOnboardingSpawnProfileApplied(moveCount, _isTutorialRunActive);
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

        private void NormalizeGameplayCamera(Camera camera)
        {
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = gameplayCameraClearColor;
            camera.nearClipPlane = 0.3f;
        }

        private void EnsureGameplayVisualReadability(Camera camera)
        {
            if (!useWorldBackgroundLayer || camera == null)
                return;

            Image overlayBackground = ResolveLegacyOverlayBackground();
            Sprite existingBackgroundSprite = _worldBackgroundRenderer != null ? _worldBackgroundRenderer.sprite : null;
            bool hasThemeBackgroundOverride = gameplayBackgroundSpriteOverride != null;
            bool keepAuthoredBackground = preserveAuthoredWorldBackground && !hasThemeBackgroundOverride && existingBackgroundSprite != null;
            Sprite backgroundSprite = hasThemeBackgroundOverride
                ? gameplayBackgroundSpriteOverride
                : keepAuthoredBackground
                    ? existingBackgroundSprite
                    : overlayBackground != null
                        ? overlayBackground.sprite
                        : null;

            if (overlayBackground != null)
            {
                Color overlayColor = overlayBackground.color;
                if (!Mathf.Approximately(overlayColor.a, legacyOverlayBackgroundAlpha))
                    overlayColor.a = legacyOverlayBackgroundAlpha;

                overlayBackground.color = overlayColor;
                overlayBackground.raycastTarget = false;
            }

            if (backgroundSprite == null)
                return;

            SpriteRenderer backgroundRenderer = GetOrCreateBackdropRenderer(
                ref _worldBackgroundRenderer,
                "SceneBackground",
                backgroundSprite,
                worldBackgroundSortingOrder);
            if (!keepAuthoredBackground)
                backgroundRenderer.color = gameplayBackgroundTint;
            backgroundRenderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 10f);
            ScaleRendererToCamera(backgroundRenderer, camera, preserveAspectCover: true);

            bool hasAuthoredDimmer = preserveAuthoredWorldBackground && _worldDimmerRenderer != null;
            SpriteRenderer dimmerRenderer = GetOrCreateBackdropRenderer(
                ref _worldDimmerRenderer,
                "SceneBackgroundDimmer",
                GetGeneratedBackdropSprite(),
                worldDimmerSortingOrder);
            if (!hasAuthoredDimmer)
                dimmerRenderer.color = gameplayBackgroundDimmerColor;
            dimmerRenderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 9.5f);
            ScaleRendererToCamera(dimmerRenderer, camera, preserveAspectCover: false);
        }

        private Image ResolveLegacyOverlayBackground()
        {
            if (_legacyOverlayBackground != null)
                return _legacyOverlayBackground;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return null;

            Transform candidate = canvas.transform.Find(legacyOverlayBackgroundName);
            if (candidate == null)
                candidate = FindDeep(canvas.transform, legacyOverlayBackgroundName);

            _legacyOverlayBackground = candidate != null ? candidate.GetComponent<Image>() : null;
            return _legacyOverlayBackground;
        }

        private SpriteRenderer GetOrCreateBackdropRenderer(ref SpriteRenderer renderer, string objectName, Sprite sprite, int sortingOrder)
        {
            if (renderer == null)
            {
                renderer = FindOrCreateBackdropRenderer(objectName);
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = true;
            return renderer;
        }

        private SpriteRenderer FindOrCreateBackdropRenderer(string objectName)
        {
            SpriteRenderer foundRenderer = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.name != objectName)
                    continue;

                var childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer == null)
                    continue;

                if (foundRenderer == null)
                {
                    foundRenderer = childRenderer;
                    continue;
                }

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            if (foundRenderer != null)
                return foundRenderer;

            var go = new GameObject(objectName, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            return go.GetComponent<SpriteRenderer>();
        }

        private void ScaleRendererToCamera(SpriteRenderer renderer, Camera camera, bool preserveAspectCover)
        {
            if (renderer == null || renderer.sprite == null || camera == null)
                return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;

            if (preserveAspectCover)
            {
                float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                return;
            }

            renderer.transform.localScale = new Vector3(
                worldWidth / spriteSize.x,
                worldHeight / spriteSize.y,
                1f);
        }

        private Sprite GetGeneratedBackdropSprite()
        {
            if (_generatedBackdropSprite != null)
                return _generatedBackdropSprite;

            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GameplayBackdropRuntime";
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            _generatedBackdropSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _generatedBackdropSprite.name = "GameplayBackdropRuntimeSprite";
            _generatedBackdropSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedBackdropSprite;
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindDeep(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
