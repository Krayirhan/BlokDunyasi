// File: Core/Persistence/GameData.cs
using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Serializable representation of complete game state for persistence.
    /// Contains all necessary data to restore a game session.
    /// </summary>
    [Serializable]
    public class GameData
    {
        /// <summary>
        /// Version of this save data format (for backwards compatibility).
        /// </summary>
        public int SaveVersion { get; set; } = 1;
        
        /// <summary>
        /// Timestamp when this save was created.
        /// </summary>
        public DateTime SaveTime { get; set; }
        
        /// <summary>
        /// Board state as flattened array.
        /// </summary>
        public CellState[] BoardCells { get; set; }
        
        /// <summary>
        /// Board width.
        /// </summary>
        public int BoardWidth { get; set; }
        
        /// <summary>
        /// Board height.
        /// </summary>
        public int BoardHeight { get; set; }
        
        /// <summary>
        /// Current player score.
        /// </summary>
        public int Score { get; set; }
        
        /// <summary>
        /// Current combo streak.
        /// </summary>
        public int ComboStreak { get; set; }
        
        /// <summary>
        /// Active block shape IDs.
        /// </summary>
        public ShapeId[] ActiveBlocks { get; set; }

        /// <summary>
        /// Active block slots (fixed 3 slots, -1 for empty).
        /// </summary>
        public int[] ActiveBlockSlots { get; set; }
        
        /// <summary>
        /// Total number of moves made.
        /// </summary>
        public int MoveCount { get; set; }
        
        /// <summary>
        /// Total lines cleared.
        /// </summary>
        public int TotalLinesCleared { get; set; }
        
        /// <summary>
        /// Game start time.
        /// </summary>
        public DateTime GameStartTime { get; set; }
        
        /// <summary>
        /// Time of last move.
        /// </summary>
        public DateTime LastMoveTime { get; set; }
        
        /// <summary>
        /// Whether game is over.
        /// </summary>
        public bool IsGameOver { get; set; }
        
        /// <summary>
        /// Random seed for spawner state.
        /// </summary>
        public int RandomSeed { get; set; }
        
        /// <summary>
        /// Current difficulty level.
        /// </summary>
        public float DifficultyLevel { get; set; }
        
        /// <summary>
        /// Spawner statistics for difficulty adaptation.
        /// </summary>
        public SpawnerSaveData SpawnerData { get; set; }
        
        public GameData()
        {
            SaveTime = DateTime.Now;
            ActiveBlocks = new ShapeId[0];
            ActiveBlockSlots = null;
            SpawnerData = new SpawnerSaveData();
        }
        
        /// <summary>
        /// Creates GameData from a GameState.
        /// </summary>
        /// <param name="gameState">Current game state</param>
        /// <param name="spawnerStats">Current spawner statistics</param>
        /// <param name="randomSeed">Current random seed</param>
        /// <returns>Serializable GameData</returns>
        public static GameData FromGameState(GameState gameState, SpawnerStats spawnerStats, int randomSeed)
        {
            return new GameData
            {
                SaveTime = DateTime.Now,
                BoardCells = gameState.Board.GetCells(),
                BoardWidth = gameState.Board.Width,
                BoardHeight = gameState.Board.Height,
                Score = gameState.Score,
                ComboStreak = gameState.ComboState.CurrentStreak,
                ActiveBlocks = gameState.ActiveBlocks.GetShapeIds(),
                ActiveBlockSlots = gameState.ActiveBlocks.GetSlotIds(),
                MoveCount = gameState.MoveCount,
                TotalLinesCleared = gameState.TotalLinesCleared,
                GameStartTime = gameState.StartTime,
                LastMoveTime = gameState.LastMoveTime,
                IsGameOver = gameState.IsGameOver,
                RandomSeed = randomSeed,
                DifficultyLevel = spawnerStats.DifficultyLevel,
                SpawnerData = new SpawnerSaveData
                {
                    TotalPlacements = spawnerStats.TotalPlacements,
                    RecentSuccessRate = spawnerStats.RecentSuccessRate,
                    OverallSuccessRate = spawnerStats.OverallSuccessRate
                }
            };
        }
        
        /// <summary>
        /// Converts this GameData back to a GameState.
        /// Note: This creates the state but doesn't restore RNG/spawner state.
        /// </summary>
        /// <returns>GameState from this save data</returns>
        public GameState ToGameState()
        {
            var gameState = new GameState(BoardWidth, BoardHeight);
            
            // Restore board cells
            gameState.Board.SetCells(BoardCells);
            
            // Restore basic state
            gameState = gameState.WithScore(Score);
            
            // Restore combo state
            var comboState = new Rules.ComboState();
            comboState.SetStreak(ComboStreak);
            gameState = gameState.WithComboState(comboState);
            
            // Restore active blocks
            var activeBlocks = new ActiveBlocks();
            if (ActiveBlockSlots != null && ActiveBlockSlots.Length == 3)
            {
                for (int i = 0; i < ActiveBlockSlots.Length; i++)
                {
                    int slotValue = ActiveBlockSlots[i];
                    if (slotValue >= 0)
                        activeBlocks.SetBlockAt(i, new ShapeId(slotValue));
                }
            }
            else if (ActiveBlocks != null && ActiveBlocks.Length > 0)
            {
                activeBlocks.SetBlocks(ActiveBlocks);
            }
            gameState = gameState.WithActiveBlocks(activeBlocks);
            
            // Restore counters
            gameState = gameState.WithMoveCount(MoveCount);
            gameState = gameState.WithTotalLinesCleared(TotalLinesCleared);
            gameState = gameState.WithStartTime(GameStartTime);
            gameState = gameState.WithLastMoveTime(LastMoveTime);
            
            // Mark as game over if needed
            if (IsGameOver)
            {
                gameState = gameState.WithGameOver();
            }
            
            return gameState;
        }
    }
    
    /// <summary>
    /// Spawner-specific save data for difficulty adaptation.
    /// </summary>
    [Serializable]
    public class SpawnerSaveData
    {
        public int TotalPlacements { get; set; }
        public float RecentSuccessRate { get; set; }
        public float OverallSuccessRate { get; set; }
        
        // Could add recent placement history if needed
        public bool[] RecentPlacementHistory { get; set; }
    }
}
