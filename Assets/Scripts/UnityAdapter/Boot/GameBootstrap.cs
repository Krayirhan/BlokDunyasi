using UnityEngine;
using System;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Input;
using BlockPuzzle.UnityAdapter;

namespace BlockPuzzle.UnityAdapter.Boot
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("📱 MOBILE BOARD SETTINGS")]
        [SerializeField] [Range(8, 10)] private int boardWidth = 10;
        [SerializeField] [Range(8, 10)] private int boardHeight = 10;
        [SerializeField] private int gameSeed = -1;

        [Header("🔧 DEBUG")]
        [SerializeField] private bool enableDebugLogging = true;

        [Header("📱 MOBILE CAMERA SETTINGS")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0, 0, -10);
        [SerializeField] [Range(4f, 8f)] private float cameraSize = 6f;

        private GameEngine _gameEngine;
        private IBestScoreStore _bestScoreStore;
        private GameState _currentGameState;
        private IGameDataProvider _dataProvider;
        private const string SaveKey = "default";
        private int _currentSeed;
        private int _sessionHighestCombo;

        public static event Action<BoardState, Int2[], int> OnBoardChanged;
        public static event Action<int, int, bool> OnScoreChanged;
        public static event Action<int> OnBestScoreChanged;
        public static event Action<ShapeDefinition[]> OnBlocksChanged;
        public static event Action<int> OnGameOver;
        public static event Action OnGameStarted;

        public GameEngine Engine => _gameEngine;
        public GameState CurrentState => _currentGameState;
        public IBestScoreStore BestScoreStore => _bestScoreStore;

        public bool IsGameActive => _gameEngine != null && _gameEngine.IsGameStarted && !_gameEngine.IsGameOverState;
        public int CurrentScore => _gameEngine?.Score ?? 0;
        public int BestScore => _bestScoreStore?.GetBestScore() ?? 0;

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
            _dataProvider = new UnityPlayerPrefsDataProvider();
            _bestScoreStore = new BestScoreStore(new PlayerPrefsStorage());
            if (CanLog)
                Debug.Log("[GameBootstrap] Initialized with BestScoreStore (PlayerPrefs)");
        }

        private void Start()
        {
            SetupCamera();

            // Yeni input sisteminin sahnede olduğundan emin ol (log amaçlı)
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
        }

        private void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                if (CanLog) Debug.LogWarning("[GameBootstrap] Main Camera not found!");
                return;
            }

            float aspectRatio = (float)Screen.width / Screen.height;

            if (aspectRatio < 1.0f)
                camera.orthographicSize = cameraSize;
            else
                camera.orthographicSize = cameraSize * aspectRatio;

            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;

            if (CanLog)
                Debug.Log($"[GameBootstrap] Responsive camera: Aspect={aspectRatio:F2}, Size={camera.orthographicSize:F1}, Pos={camera.transform.position}");
        }

        private void StartFromLaunchMode()
        {
            bool loaded = false;

            if (GameLaunchState.LaunchMode != GameLaunchMode.NewGame)
            {
                loaded = TryLoadSavedGame();
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

                _gameEngine = new GameEngine(rng, boardWidth, boardHeight);

                if (CanLog)
                    Debug.Log($"[GameBootstrap] Created game engine {boardWidth}x{boardHeight}, seed: {actualSeed}");

                _gameEngine.StartNewGame(actualSeed);
                _currentGameState = _gameEngine.CurrentState;
                _sessionHighestCombo = _currentGameState.Combo;
                _sessionHighestCombo = Math.Max(_sessionHighestCombo, _currentGameState.Combo);
                _sessionHighestCombo = _currentGameState.Combo;

                NotifyGameStarted();
                NotifyBoardChanged();
                NotifyScoreChanged(false);
                NotifyBlocksChanged();
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

                int currentScore = _gameEngine.CurrentState.Score;
                bool isNewBest = currentScore > _bestScoreStore.GetBestScore();
                if (isNewBest)
                    _bestScoreStore.SetBestScore(currentScore);

                NotifyBoardChanged(moveResult);
                NotifyScoreChanged(isNewBest);
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
            if (_dataProvider == null)
                return false;

            GameData data = null;
            try
            {
                data = _dataProvider.LoadGameDataAsync(SaveKey).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to load saved game: {ex.Message}");
                return false;
            }

            if (data == null)
                return false;

            if (data.IsGameOver)
            {
                ClearSavedGame();
                return false;
            }

            if (!ApplyLoadedGameData(data))
            {
                ClearSavedGame();
                return false;
            }

            return true;
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

                _gameEngine = new GameEngine(rng, data.BoardWidth, data.BoardHeight);
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

                int currentScore = _currentGameState.Score;
                bool isNewBest = currentScore > _bestScoreStore.GetBestScore();
                if (isNewBest)
                    _bestScoreStore.SetBestScore(currentScore);

                NotifyGameStarted();
                NotifyBoardChanged();
                NotifyScoreChanged(isNewBest);
                NotifyBlocksChanged();

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
            if (_dataProvider == null || _gameEngine == null || _currentGameState == null)
                return;

            if (_currentGameState.IsGameOver || _gameEngine.IsGameOver())
                return;

            try
            {
                var stats = _gameEngine.BlockSpawner.GetStats();
                var data = GameData.FromGameState(_currentGameState, stats, _currentSeed);
                _dataProvider.SaveGameDataAsync(SaveKey, data).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to save game: {ex.Message}");
            }
        }

        private void ClearSavedGame()
        {
            if (_dataProvider == null)
                return;

            try
            {
                _dataProvider.DeleteGameDataAsync(SaveKey).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to clear saved game: {ex.Message}");
            }
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

        private void NotifyScoreChanged(bool isNewBest)
        {
            int currentScore = _gameEngine?.Score ?? 0;
            int bestScore = _bestScoreStore?.GetBestScore() ?? 0;

            OnScoreChanged?.Invoke(currentScore, bestScore, isNewBest);

            if (isNewBest)
                OnBestScoreChanged?.Invoke(bestScore);
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
            if (_dataProvider == null || _currentGameState == null)
                return;

            try
            {
                var stats = _dataProvider.LoadStatisticsAsync().GetAwaiter().GetResult() ?? GameStatistics.CreateDefault();
                var elapsedTime = _currentGameState.GetElapsedTime();
                var blocksPlaced = _currentGameState.MoveCount;
                var linesCleared = _currentGameState.TotalLinesCleared;

                stats.RecordGameSession(finalScore, elapsedTime, blocksPlaced, linesCleared, _sessionHighestCombo);
                _dataProvider.SaveStatisticsAsync(stats).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to record statistics: {ex.Message}");
            }
        }

        private void OnValidate()
        {
            boardWidth = Mathf.Clamp(boardWidth, 4, 20);
            boardHeight = Mathf.Clamp(boardHeight, 4, 20);
        }
    }
}
