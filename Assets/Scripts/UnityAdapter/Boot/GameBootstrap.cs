using UnityEngine;
using System;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Input;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter;

namespace BlockPuzzle.UnityAdapter.Boot
{
    public class GameBootstrap : MonoBehaviour
    {
        public const int AnalyticsSchemaVersion = 1;

        [Header("ğŸ“± MOBILE BOARD SETTINGS")]
        [SerializeField] [Range(8, 10)] private int boardWidth = 10;
        [SerializeField] [Range(8, 10)] private int boardHeight = 10;
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

        [Header("SCORING")]
        [SerializeField] private ScoreConfigAsset scoreConfigAsset;

        private GameEngine _gameEngine;
        private IBestScoreStore _bestScoreStore;
        private GameState _currentGameState;
        private IGameStatePersistence _gameStatePersistence;
        private IStatisticsPersistence _statisticsPersistence;
        private GameSaveManager _gameSaveManager;
        private StatisticsManager _statisticsManager;
        private CameraController _cameraController;
        private ScreenLayoutManager _screenLayoutManager;
        private const string SaveKey = "default";
        private int _currentSeed;
        private int _sessionHighestCombo;
        private int _sessionBestAtStart;
        private bool _sessionReachedNewBest;
        private ScoreConfig _scoreConfig;

        public static event Action<BoardState, Int2[], int> OnBoardChanged;
        public static event Action<int, int, bool> OnScoreChanged;
        public static event Action<ScoreBreakdownInfo> OnScoreBreakdown;
        public static event Action<int> OnBestScoreChanged;
        public static event Action<AnalyticsEventData> OnAnalyticsEvent;
        public static event Action<ShapeDefinition[]> OnBlocksChanged;
        public static event Action<int> OnGameOver;
        public static event Action OnGameStarted;

        public GameEngine Engine => _gameEngine;
        public GameState CurrentState => _currentGameState;
        public IBestScoreStore BestScoreStore => _bestScoreStore;

        public bool IsGameActive => _gameEngine != null && _gameEngine.IsGameStarted && !_gameEngine.IsGameOverState;
        public int CurrentScore => _gameEngine?.Score ?? 0;
        public int BestScore => _bestScoreStore?.GetBestScore() ?? 0;
        public int ScoreFormulaVersion => _gameEngine?.ScoreFormulaVersion ?? _scoreConfig?.FormulaVersion ?? ScoreConfig.DefaultFormulaVersion;

        private bool CanLog => enableDebugLogging && Debug.isDebugBuild;
        
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
            if (!autoAdaptToScreen)
                return;

            if (HasScreenMetricsChanged())
            {
                ApplyResponsiveLayout(true);
            }
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
            
            ApplyResponsiveLayout(true);

            float aspectRatio = (float)Screen.width / Mathf.Max(1, Screen.height);

            if (CanLog)
                Debug.Log($"[GameBootstrap] Responsive camera: Aspect={aspectRatio:F2}, Size={camera.orthographicSize:F1}, Pos={camera.transform.position}");
        }

        private void StartFromLaunchMode()
        {
            bool loaded = false;

            if (GameLaunchState.LaunchMode != GameLaunchMode.NewGame)
            {
                loaded = _gameSaveManager != null && _gameSaveManager.TryLoadSavedGame(ApplyLoadedGameData);
            }

            if (!loaded)
            {
                StartNewGame(GameLaunchState.LaunchMode == GameLaunchMode.NewGame);
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

                _gameEngine = new GameEngine(rng, boardWidth, boardHeight, _scoreConfig);

                if (CanLog)
                    Debug.Log($"[GameBootstrap] Created game engine {boardWidth}x{boardHeight}, seed: {actualSeed}");

                _gameEngine.StartNewGame(actualSeed);
                _currentGameState = _gameEngine.CurrentState;
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
                Debug.LogError(ex.StackTrace);
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
                Debug.LogError(ex.StackTrace);
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

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveGameIfNeeded();
        }

        private void OnApplicationQuit()
        {
            SaveGameIfNeeded();
        }

        private void OnDestroy()
        {
            SaveGameIfNeeded();
        }

        private bool TryLoadSavedGame()
        {
            return _gameSaveManager != null && _gameSaveManager.TryLoadSavedGame(ApplyLoadedGameData);
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
                Debug.LogError(ex.StackTrace);
                return false;
            }
        }

        private void SaveGameIfNeeded()
        {
            _gameSaveManager?.SaveGameIfNeeded(
                _gameEngine,
                _currentGameState,
                _currentSeed,
                _gameEngine?.ScoreFormulaVersion ?? _scoreConfig?.FormulaVersion ?? ScoreConfig.DefaultFormulaVersion);
        }

        private void ClearSavedGame()
        {
            _gameSaveManager?.ClearSavedGame();
        }

        private void NotifyGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        private void NotifyBoardChanged(MoveResult moveResult = null)
        {
            if (_currentGameState?.Board == null)
                return;

            Int2[] clearedPositions = Array.Empty<Int2>();
            int linesCleared = 0;

            if (moveResult != null && moveResult.Success)
                linesCleared = moveResult.LinesCleared;

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
                timestampUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

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

            if (linesCleared <= 0 && scoreDelta > 0)
            {
                anomalyCode = "SCORE_WITHOUT_LINES";
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
            ClearSavedGame();
            RecordStatisticsOnGameOver(finalScore);
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
        }

        private void OnValidate()
        {
            boardWidth = Mathf.Clamp(boardWidth, 4, 20);
            boardHeight = Mathf.Clamp(boardHeight, 4, 20);
            cameraHorizontalPadding = Mathf.Clamp(cameraHorizontalPadding, 0f, 2f);
            minAdaptiveCameraSize = Mathf.Max(1f, minAdaptiveCameraSize);
            maxAdaptiveCameraSize = Mathf.Max(minAdaptiveCameraSize, maxAdaptiveCameraSize);
            canvasReferenceResolution.x = Mathf.Max(320f, canvasReferenceResolution.x);
            canvasReferenceResolution.y = Mathf.Max(320f, canvasReferenceResolution.y);
            canvasMatchWidthOrHeight = Mathf.Clamp01(canvasMatchWidthOrHeight);
        }
    }
}

