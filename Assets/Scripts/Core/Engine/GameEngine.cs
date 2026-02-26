// File: Core/Engine/GameEngine.cs
using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.RNG;

namespace BlockPuzzle.Core.Engine
{
    /// <summary>
    /// Main game engine orchestrating all game systems.
    /// Handles move validation, execution, scoring, and game state transitions.
    /// Uses static utility classes for placement, detection, and clearing.
    /// </summary>
    public class GameEngine
    {
        private readonly BlockSpawner _blockSpawner;
        private readonly ScoreConfig _scoreConfig;
        
        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState { get; private set; }
        
        /// <summary>
        /// Block spawner for generating new blocks.
        /// </summary>
        public BlockSpawner BlockSpawner => _blockSpawner;
        
        /// <summary>
        /// Whether the game has started.
        /// </summary>
        public bool IsGameStarted => CurrentState.MoveCount > 0 || !CurrentState.ActiveBlocks.IsEmpty;
        
        /// <summary>
        /// Whether the game is over (as a property).
        /// </summary>
        public bool IsGameOverState => CurrentState.IsGameOver;
        
        /// <summary>
        /// Current score.
        /// </summary>
        public int Score => CurrentState.Score;

        /// <summary>
        /// Active score formula version.
        /// </summary>
        public int ScoreFormulaVersion => _scoreConfig.FormulaVersion;
        
        /// <summary>
        /// Event fired when game state changes.
        /// </summary>
        public event Action<GameState> StateChanged;
        
        /// <summary>
        /// Event fired when lines are cleared.
        /// </summary>
        public event Action<ClearResult> LinesCleared;
        
        /// <summary>
        /// Event fired when score changes.
        /// </summary>
        public event Action<ScoreResult> ScoreChanged;
        
        /// <summary>
        /// Event fired when game ends.
        /// </summary>
        public event Action<GameState> GameOver;
        
        public GameEngine(SeededRng rng, int boardWidth = 10, int boardHeight = 10, ScoreConfig scoreConfig = null)
        {
            _blockSpawner = new BlockSpawner(rng);
            _scoreConfig = scoreConfig ?? ScoreConfig.Default;
            CurrentState = new GameState(boardWidth, boardHeight);
        }
        
        /// <summary>
        /// Starts a new game with fresh state.
        /// </summary>
        /// <param name="seed">Optional seed for deterministic gameplay</param>
        public void StartNewGame(int? seed = null)
        {
            if (seed.HasValue)
                _blockSpawner.Reset(seed.Value);
            
            CurrentState = new GameState(CurrentState.Board.Width, CurrentState.Board.Height);
            SpawnNewBlocks();
            OnStateChanged();
        }

        /// <summary>
        /// Loads an existing game state.
        /// </summary>
        /// <param name="loadedState">Previously saved game state</param>
        public void LoadGame(GameState loadedState)
        {
            if (loadedState == null)
                throw new ArgumentNullException(nameof(loadedState));

            CurrentState = loadedState;
            OnStateChanged();

            if (CurrentState.IsGameOver)
                OnGameOver();
        }
        
        /// <summary>
        /// Attempts to place an active block at the specified position.
        /// </summary>
        /// <param name="activeBlockIndex">Index of active block to place (0-2)</param>
        /// <param name="boardPosition">Target position on board</param>
        /// <returns>Result of the placement attempt</returns>
        public MoveResult TryPlaceBlock(int activeBlockIndex, Int2 boardPosition)
        {
            // Validate game state
            if (CurrentState.IsGameOver)
                return MoveResult.GameOverResult;
                
            // Validate block index
            if (!CurrentState.ActiveBlocks.HasBlockAt(activeBlockIndex))
                return MoveResult.InvalidBlockIndex;
                
            // Get shape definition
            var shapeId = CurrentState.ActiveBlocks.GetShapeId(activeBlockIndex);
            if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                return MoveResult.InvalidShape;
                
            // Validate placement using static PlacementEngine
            var placementResult = PlacementEngine.CanPlace(CurrentState.Board, boardPosition.X, boardPosition.Y, shape.Offsets);
            if (placementResult != PlacementResult.Success)
            {
                _blockSpawner.RecordPlacement(false);
                return ConvertPlacementResult(placementResult);
            }
            
            // Execute the move and get detailed scoring outcome.
            MoveExecutionResult executionResult = ExecuteMove(activeBlockIndex, shape, boardPosition);
            
            // Check if new blocks were spawned (all blocks were placed)
            bool triggersSpawn = CurrentState.ActiveBlocks.IsFull; // If full, new blocks were just spawned
            
            return MoveResult.CreateSuccess(CurrentState.Score, executionResult.ScoreResult, triggersSpawn);
        }
        
        /// <summary>
        /// Checks if the game is over (no valid placements for any active blocks).
        /// </summary>
        /// <returns>True if game is over</returns>
        public bool IsGameOver()
        {
            if (CurrentState.ActiveBlocks.IsEmpty)
                return false; // Will spawn new blocks
                
            return !CurrentState.ActiveBlocks.HasPlaceableBlocks(CurrentState.Board);
        }
        
        /// <summary>
        /// Gets valid placement positions for an active block.
        /// </summary>
        /// <param name="activeBlockIndex">Index of active block</param>
        /// <returns>Array of valid positions, empty if none</returns>
        public Int2[] GetValidPlacements(int activeBlockIndex)
        {
            if (!CurrentState.ActiveBlocks.HasBlockAt(activeBlockIndex))
                return new Int2[0];
                
            var shapeId = CurrentState.ActiveBlocks.GetShapeId(activeBlockIndex);
            if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                return new Int2[0];
                
            return PlacementSearch.FindValidPlacements(CurrentState.Board, shape);
        }
        
        /// <summary>
        /// Alias for TryPlaceBlock - attempts a move.
        /// </summary>
        public MoveResult AttemptMove(int activeBlockIndex, Int2 boardPosition)
        {
            return TryPlaceBlock(activeBlockIndex, boardPosition);
        }
        
        /// <summary>
        /// Checks if a move would be valid.
        /// </summary>
        public bool IsValidMove(int activeBlockIndex, Int2 boardPosition)
        {
            if (CurrentState.IsGameOver || !CurrentState.ActiveBlocks.HasBlockAt(activeBlockIndex))
                return false;
                
            var shapeId = CurrentState.ActiveBlocks.GetShapeId(activeBlockIndex);
            if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                return false;
                
            var placementResult = PlacementEngine.CanPlace(CurrentState.Board, boardPosition.X, boardPosition.Y, shape.Offsets);
            return placementResult == PlacementResult.Success;
        }
        
        /// <summary>
        /// Previews the score for a potential move.
        /// </summary>
        public ScoreResult PreviewMoveScore(int activeBlockIndex, Int2 boardPosition)
        {
            if (!IsValidMove(activeBlockIndex, boardPosition))
                return ScoreResult.Empty;

            var shapeId = CurrentState.ActiveBlocks.GetShapeId(activeBlockIndex);
            if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                return ScoreResult.Empty;

            var previewBoard = CurrentState.Board.Clone();

            int blockId = CurrentState.MoveCount < int.MaxValue
                ? CurrentState.MoveCount + 1
                : int.MaxValue;
            int colorId = CurrentState.ActiveBlocks.GetColorId(activeBlockIndex);

            var placement = PlacementEngine.PlaceAtomic(
                previewBoard,
                boardPosition.X,
                boardPosition.Y,
                shape.Offsets,
                blockId,
                colorId,
                out _);

            if (placement != PlacementResult.Success)
                return ScoreResult.Empty;

            var lineResult = LineDetector.DetectFullLines(previewBoard);
            int totalLinesCleared = lineResult.FullRowCount + lineResult.FullColumnCount;
            if (totalLinesCleared <= 0)
                return ScoreResult.Empty;

            var previewCombo = CurrentState.ComboState.Clone().IncrementCombo();
            return ScoringRules.CalculateScore(totalLinesCleared, previewCombo, _scoreConfig);
        }
        
        /// <summary>
        /// Forces game over state (for debug).
        /// </summary>
        public void ForceGameOver()
        {
            CurrentState = CurrentState.WithGameOver();
            OnGameOver();
        }
        
        /// <summary>
        /// Gets debug info about the game state.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Score: {CurrentState.Score}, Moves: {CurrentState.MoveCount}, " +
                   $"Lines: {CurrentState.TotalLinesCleared}, GameOver: {CurrentState.IsGameOver}";
        }
        
        /// <summary>
        /// Spawns a new set of blocks when current set is empty.
        /// </summary>
        public void SpawnNewBlocks()
        {
            if (!CurrentState.ActiveBlocks.IsEmpty)
            {
                System.Diagnostics.Debug.WriteLine($"[GameEngine.SpawnNewBlocks] Skipped: ActiveBlocks not empty (Count={CurrentState.ActiveBlocks.Count})");
                return; // Don't spawn if blocks still exist
            }
            
            System.Diagnostics.Debug.WriteLine("[GameEngine.SpawnNewBlocks] Spawning new block set...");
                
            var newBlocks = _blockSpawner.SpawnBlockSet(CurrentState.Board);
            
            System.Diagnostics.Debug.WriteLine($"[GameEngine.SpawnNewBlocks] BlockSpawner returned {newBlocks.Length} blocks:");
            for (int i = 0; i < newBlocks.Length; i++)
            {
                bool exists = ShapeLibrary.TryGetShape(newBlocks[i], out var shape);
                System.Diagnostics.Debug.WriteLine($"  Slot {i}: ShapeId={newBlocks[i]}, ExistsInLibrary={exists}, Name={shape?.Name ?? "NULL"}");
            }
            
            var newActiveBlocks = new ActiveBlocks();
            newActiveBlocks.SetBlocks(newBlocks);
            
            System.Diagnostics.Debug.WriteLine($"[GameEngine.SpawnNewBlocks] ActiveBlocks after SetBlocks: Count={newActiveBlocks.Count}, IsFull={newActiveBlocks.IsFull}");
            
            CurrentState = CurrentState.WithActiveBlocks(newActiveBlocks);
            
            // Check for game over after spawning
            if (IsGameOver())
            {
                CurrentState = CurrentState.WithGameOver();
                OnGameOver();
            }
        }
        
        /// <summary>
        /// Forces spawning of new blocks (for testing/debug).
        /// </summary>
        public void ForceSpawnBlocks()
        {
            var newBlocks = _blockSpawner.SpawnBlockSet(CurrentState.Board);
            var newActiveBlocks = new ActiveBlocks();
            newActiveBlocks.SetBlocks(newBlocks);
            
            CurrentState = CurrentState.WithActiveBlocks(newActiveBlocks);
            OnStateChanged();
        }
        
        /// <summary>
        /// Gets the current game statistics.
        /// </summary>
        /// <returns>Game statistics</returns>
        public GameStats GetStats()
        {
            return new GameStats(
                CurrentState.Score,
                CurrentState.MoveCount,
                CurrentState.TotalLinesCleared,
                CurrentState.GetElapsedTime(),
                CurrentState.IsGameOver,
                _blockSpawner.GetStats()
            );
        }
        
        private MoveExecutionResult ExecuteMove(int activeBlockIndex, ShapeDefinition shape, Int2 boardPosition)
        {
            // Generate unique block ID for this placement
            int blockId = CurrentState.MoveCount < int.MaxValue
                ? CurrentState.MoveCount + 1
                : int.MaxValue;
            // Traydeki bloktan colorId al
            int colorId = 1;
            // Aktif bloklardan colorId'yi bul
            if (CurrentState.ActiveBlocks.HasBlockAt(activeBlockIndex))
            {
                // NewSimpleBlock objesinden colorId alınmalı, burada ActiveBlocks'tan alınamıyorsa
                // colorId = ... (gerekirse eklenir)
                // Şimdilik slot index ile uyumlu olarak 1-based
                colorId = CurrentState.ActiveBlocks.GetColorId(activeBlockIndex);
            }
            // Place the block using static PlacementEngine
            PlacementEngine.PlaceAtomic(
                CurrentState.Board, 
                boardPosition.X, 
                boardPosition.Y, 
                shape.Offsets, 
                blockId, 
                colorId, 
                out int placedCount);
            
            // Remove the used block (slots stay empty until all three are used)
            var newActiveBlocks = CurrentState.ActiveBlocks.Clone();
            newActiveBlocks.RemoveBlock(activeBlockIndex);
            CurrentState = CurrentState.WithActiveBlocks(newActiveBlocks);
            
            // Record placement for difficulty adaptation
            _blockSpawner.RecordPlacement(true);
            
            // Track lines cleared for return value
            int totalLinesCleared = 0;
            var scoreResult = ScoreResult.Empty;
            
            // Check for line clears using static LineDetector
            var lineResult = LineDetector.DetectFullLines(CurrentState.Board);
            if (lineResult.HasFullLines)
            {
                // Get arrays from the result
                int[] fullRows = new int[lineResult.FullRowCount];
                int[] fullCols = new int[lineResult.FullColumnCount];
                Array.Copy(lineResult.FullRows, fullRows, lineResult.FullRowCount);
                Array.Copy(lineResult.FullColumns, fullCols, lineResult.FullColumnCount);
                
                // Clear lines using static LineClearer
                var clearResult = LineClearer.ClearLines(CurrentState.Board, fullRows, fullCols);
                
                totalLinesCleared = lineResult.FullRowCount + lineResult.FullColumnCount;
                CurrentState = CurrentState.WithLinesCleared(totalLinesCleared);
                
                OnLinesCleared(clearResult);
                
                // Update combo and calculate score
                var newCombo = CurrentState.ComboState.IncrementCombo();
                CurrentState = CurrentState.WithComboState(newCombo);
                
                scoreResult = ScoringRules.CalculateScore(totalLinesCleared, newCombo, _scoreConfig);
                int nextScore = AddScoreSafely(CurrentState.Score, scoreResult.ScoreDelta);
                CurrentState = CurrentState.WithScore(nextScore);
                
                OnScoreChanged(scoreResult);
            }
            else
            {
                // Break combo if no lines cleared
                var newCombo = CurrentState.ComboState.ResetCombo();
                CurrentState = CurrentState.WithComboState(newCombo);
                scoreResult = new ScoreResult(
                    scoreDelta: 0,
                    linesCleared: 0,
                    comboStreak: newCombo.Streak,
                    comboMultiplier: _scoreConfig.EvaluateComboMultiplier(newCombo.Streak),
                    baseScore: 0,
                    lineClearMultiplier: 1.0f,
                    formulaVersion: _scoreConfig.FormulaVersion);
            }
            
            // Increment move count
            CurrentState = CurrentState.WithIncrementedMoveCount();
            
            // Spawn new blocks if all placed
            if (CurrentState.ActiveBlocks.IsEmpty)
            {
                SpawnNewBlocks();
            }
            
            OnStateChanged();
            
            return new MoveExecutionResult(scoreResult);
        }

        private static int AddScoreSafely(int currentScore, int scoreDelta)
        {
            if (scoreDelta < 0)
                throw new InvalidOperationException($"Score delta cannot be negative. Received: {scoreDelta}");

            long next = (long)currentScore + scoreDelta;
            if (next > int.MaxValue)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameEngine] Score overflow prevented. Current={currentScore}, Delta={scoreDelta}. Clamped to int.MaxValue.");
                return int.MaxValue;
            }

            if (next < 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameEngine] Score underflow prevented. Current={currentScore}, Delta={scoreDelta}. Clamped to 0.");
                return 0;
            }

            return (int)next;
        }
        
        private MoveResult ConvertPlacementResult(PlacementResult placementResult)
        {
            return placementResult switch
            {
                PlacementResult.OutOfBounds => MoveResult.OutOfBounds,
                PlacementResult.Collision => MoveResult.CellsOccupied,
                _ => MoveResult.InvalidPlacement
            };
        }
        
        private void OnStateChanged()
        {
            StateChanged?.Invoke(CurrentState);
        }
        
        private void OnLinesCleared(ClearResult clearResult)
        {
            LinesCleared?.Invoke(clearResult);
        }
        
        private void OnScoreChanged(ScoreResult scoreResult)
        {
            ScoreChanged?.Invoke(scoreResult);
        }
        
        private void OnGameOver()
        {
            GameOver?.Invoke(CurrentState);
        }

        private readonly struct MoveExecutionResult
        {
            public readonly ScoreResult ScoreResult;

            public MoveExecutionResult(ScoreResult scoreResult)
            {
                ScoreResult = scoreResult;
            }
        }

    }
    
    /// <summary>
    /// Statistics about current game session.
    /// </summary>
    public readonly struct GameStats
    {
        public readonly int Score;
        public readonly int MoveCount;
        public readonly int LinesCleared;
        public readonly TimeSpan ElapsedTime;
        public readonly bool IsGameOver;
        public readonly SpawnerStats SpawnerStats;
        
        public GameStats(int score, int moveCount, int linesCleared, TimeSpan elapsedTime, bool isGameOver, SpawnerStats spawnerStats)
        {
            Score = score;
            MoveCount = moveCount;
            LinesCleared = linesCleared;
            ElapsedTime = elapsedTime;
            IsGameOver = isGameOver;
            SpawnerStats = spawnerStats;
        }
        
        public float ScorePerMinute => ElapsedTime.TotalMinutes > 0 ? (float)(Score / ElapsedTime.TotalMinutes) : 0f;
        public float MovesPerMinute => ElapsedTime.TotalMinutes > 0 ? (float)(MoveCount / ElapsedTime.TotalMinutes) : 0f;
        public float AverageScorePerMove => MoveCount > 0 ? (float)Score / MoveCount : 0f;
        
        public override string ToString()
        {
            return $"Score: {Score}, Moves: {MoveCount}, Lines: {LinesCleared}, " +
                   $"Time: {ElapsedTime:mm\\:ss}, GameOver: {IsGameOver}";
        }
    }
}
