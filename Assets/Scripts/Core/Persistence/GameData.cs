// File: Core/Persistence/GameData.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;
using CoreGameMode = BlockPuzzle.Core.Common.GameMode;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Serializable representation of complete game state for persistence.
    /// Contains all necessary data to restore a game session.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public const int LegacySaveVersion1 = 1;
        public const int LegacySaveVersion2 = 2;
        public const int LegacySaveVersion3 = 3;
        public const int CurrentSaveVersion = 4;
        private const int DefaultBoardWidth = 10;
        private const int DefaultBoardHeight = 10;

        /// <summary>
        /// Version of this save data format (for backwards compatibility).
        /// </summary>
        public int SaveVersion { get; set; } = CurrentSaveVersion;
        
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
        /// Remaining setup-move grace allowance before the combo breaks.
        /// </summary>
        public int ComboGraceMovesRemaining { get; set; }

        /// <summary>
        /// Score formula version used when this save was written.
        /// </summary>
        public int ScoreFormulaVersion { get; set; } = ScoreConfig.DefaultFormulaVersion;
        
        /// <summary>
        /// Active block shape IDs.
        /// </summary>
        public ShapeId[] ActiveBlocks { get; set; }

        /// <summary>
        /// Active block slots (fixed 3 slots, -1 for empty).
        /// </summary>
        public int[] ActiveBlockSlots { get; set; }

        /// <summary>
        /// Active block color IDs for fixed 3 slots (0 for empty/unknown).
        /// </summary>
        public int[] ActiveBlockColorIds { get; set; }
        
        /// <summary>
        /// Total number of moves made.
        /// </summary>
        public int MoveCount { get; set; }

        /// <summary>
        /// Number of rescues/continues used.
        /// </summary>
        public int RescueCount { get; set; }

        /// <summary>
        /// Selected Game Mode.
        /// </summary>
        public string GameMode { get; set; }
        
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
            ActiveBlockColorIds = null;
            SpawnerData = new SpawnerSaveData();
        }
        
        /// <summary>
        /// Creates GameData from a GameState.
        /// </summary>
        /// <param name="gameState">Current game state</param>
        /// <param name="spawnerStats">Current spawner statistics</param>
        /// <param name="randomSeed">Current random seed</param>
        /// <returns>Serializable GameData</returns>
        public static GameData FromGameState(
            GameState gameState,
            SpawnerStats spawnerStats,
            int randomSeed,
            int scoreFormulaVersion = ScoreConfig.DefaultFormulaVersion)
        {
            var activeBlockColorIds = new int[3];
            for (int i = 0; i < activeBlockColorIds.Length; i++)
            {
                if (gameState.ActiveBlocks.TryGetColorId(i, out int colorId))
                    activeBlockColorIds[i] = colorId;
            }

            return new GameData
            {
                SaveTime = DateTime.Now,
                SaveVersion = CurrentSaveVersion,
                BoardCells = gameState.Board.GetCells(),
                BoardWidth = gameState.Board.Width,
                BoardHeight = gameState.Board.Height,
                Score = gameState.Score,
                ComboStreak = gameState.ComboState.CurrentStreak,
                ComboGraceMovesRemaining = gameState.ComboState.GraceMovesRemaining,
                ScoreFormulaVersion = scoreFormulaVersion <= 0 ? ScoreConfig.DefaultFormulaVersion : scoreFormulaVersion,
                ActiveBlocks = gameState.ActiveBlocks.GetShapeIds(),
                ActiveBlockSlots = gameState.ActiveBlocks.GetSlotIds(),
                ActiveBlockColorIds = activeBlockColorIds,
                MoveCount = gameState.MoveCount,
                RescueCount = gameState.RescueCount,
                GameMode = gameState.Mode.ToString(),
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
                    OverallSuccessRate = spawnerStats.OverallSuccessRate,
                    RecentPlacementHistory = spawnerStats.RecentPlacementHistory
                }
            };
        }

        /// <summary>
        /// Migrates loaded save data to the current version and sanitizes required fields.
        /// </summary>
        public GameDataMigrationResult MigrateToCurrentInPlace()
        {
            int sourceVersion = NormalizeIncomingVersion(SaveVersion);
            if (sourceVersion > CurrentSaveVersion)
                throw new NotSupportedException(
                    $"Save version {sourceVersion} is newer than supported current version {CurrentSaveVersion}.");

            SaveVersion = sourceVersion;
            bool migrated = false;
            bool sanitized = false;
            var notes = new List<string>();

            while (SaveVersion < LegacySaveVersion2)
            {
                MigrateV1ToV2();
                migrated = true;
                notes.Add("v1->v2 applied.");
            }

            while (SaveVersion < LegacySaveVersion3)
            {
                MigrateV2ToV3();
                migrated = true;
                notes.Add("v2->v3 applied.");
            }

            while (SaveVersion < CurrentSaveVersion)
            {
                MigrateV3ToV4();
                migrated = true;
                notes.Add("v3->v4 applied.");
            }

            if (ValidateAndSanitizeInPlace(notes))
                sanitized = true;

            SaveVersion = CurrentSaveVersion;

            return new GameDataMigrationResult(
                sourceVersion,
                CurrentSaveVersion,
                migrated,
                sanitized,
                notes.Count == 0 ? "Save data already current and valid." : string.Join(" ", notes));
        }
        
        /// <summary>
        /// Converts this GameData back to a GameState.
        /// Note: This creates the state but doesn't restore RNG/spawner state.
        /// </summary>
        /// <returns>GameState from this save data</returns>
        public GameState ToGameState()
        {
            MigrateToCurrentInPlace();

            var mode = CoreGameMode.Classic;
            if (!string.IsNullOrEmpty(GameMode) && Enum.TryParse<CoreGameMode>(GameMode, out var parsedMode))
            {
                mode = parsedMode;
            }

            var gameState = new GameState(BoardWidth, BoardHeight, mode);
            gameState = gameState.WithRescueCount(RescueCount);
            
            // Restore board cells
            gameState.Board.SetCells(BoardCells);
            
            // Restore basic state
            gameState = gameState.WithScore(Score);
            
            // Restore combo state
            var comboState = new Rules.ComboState();
            int restoredGrace = ComboGraceMovesRemaining > 0
                ? ComboGraceMovesRemaining
                : (ComboStreak > 0 ? 1 : 0);
            comboState.SetState(ComboStreak, restoredGrace);
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

            if (ActiveBlockColorIds != null && ActiveBlockColorIds.Length == 3)
            {
                for (int i = 0; i < ActiveBlockColorIds.Length; i++)
                {
                    int colorId = ActiveBlockColorIds[i];
                    if (colorId > 0 && activeBlocks.HasBlockAt(i))
                        activeBlocks.SetColorId(i, colorId);
                }
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

        private static int NormalizeIncomingVersion(int saveVersion)
        {
            return saveVersion <= 0 ? LegacySaveVersion1 : saveVersion;
        }

        private void MigrateV1ToV2()
        {
            EnsureActiveBlockSlotsFromLegacyArray();
            SaveVersion = LegacySaveVersion2;
        }

        private void MigrateV2ToV3()
        {
            EnsureActiveBlockSlotsFromLegacyArray();
            EnsureActiveBlockColorIds();

            if (ScoreFormulaVersion <= 0)
                ScoreFormulaVersion = ScoreConfig.DefaultFormulaVersion;

            if (ComboStreak > 0 && ComboGraceMovesRemaining <= 0)
                ComboGraceMovesRemaining = 1;

            SaveVersion = LegacySaveVersion3;
        }

        private void MigrateV3ToV4()
        {
            EnsureActiveBlockSlotsFromLegacyArray();
            EnsureActiveBlockColorIds();
            EnsureLegacyActiveBlocksArrayFromSlots();
            SaveVersion = CurrentSaveVersion;
        }

        private bool ValidateAndSanitizeInPlace(List<string> notes)
        {
            bool sanitized = false;

            if (BoardWidth <= 0)
            {
                BoardWidth = DefaultBoardWidth;
                sanitized = true;
                notes.Add("BoardWidth defaulted.");
            }

            if (BoardHeight <= 0)
            {
                BoardHeight = DefaultBoardHeight;
                sanitized = true;
                notes.Add("BoardHeight defaulted.");
            }

            int expectedCellCount = checked(BoardWidth * BoardHeight);
            if (BoardCells == null)
            {
                BoardCells = new CellState[expectedCellCount];
                sanitized = true;
                notes.Add("BoardCells created empty.");
            }
            else if (BoardCells.Length != expectedCellCount)
            {
                BoardCells = new CellState[expectedCellCount];
                sanitized = true;
                notes.Add("BoardCells length mismatch sanitized to empty board.");
            }

            if (Score < 0)
            {
                Score = 0;
                sanitized = true;
                notes.Add("Score clamped.");
            }

            if (MoveCount < 0)
            {
                MoveCount = 0;
                sanitized = true;
                notes.Add("MoveCount clamped.");
            }

            if (TotalLinesCleared < 0)
            {
                TotalLinesCleared = 0;
                sanitized = true;
                notes.Add("TotalLinesCleared clamped.");
            }

            if (ComboStreak < 0)
            {
                ComboStreak = 0;
                sanitized = true;
                notes.Add("ComboStreak clamped.");
            }

            if (ComboGraceMovesRemaining < 0)
            {
                ComboGraceMovesRemaining = 0;
                sanitized = true;
                notes.Add("ComboGraceMovesRemaining clamped.");
            }

            if (ScoreFormulaVersion <= 0)
            {
                ScoreFormulaVersion = ScoreConfig.DefaultFormulaVersion;
                sanitized = true;
                notes.Add("ScoreFormulaVersion defaulted.");
            }

            if (ActiveBlocks == null)
            {
                ActiveBlocks = Array.Empty<ShapeId>();
                sanitized = true;
                notes.Add("ActiveBlocks defaulted.");
            }

            if (SpawnerData == null)
            {
                SpawnerData = new SpawnerSaveData();
                sanitized = true;
                notes.Add("SpawnerData defaulted.");
            }

            if (SpawnerData.RecentPlacementHistory == null)
            {
                SpawnerData.RecentPlacementHistory = Array.Empty<bool>();
                sanitized = true;
                notes.Add("RecentPlacementHistory defaulted.");
            }

            if (ActiveBlockSlots == null || ActiveBlockSlots.Length != 3)
            {
                EnsureActiveBlockSlotsFromLegacyArray();
                sanitized = true;
                notes.Add("ActiveBlockSlots rebuilt.");
            }

            if (ActiveBlockColorIds == null || ActiveBlockColorIds.Length != 3)
            {
                EnsureActiveBlockColorIds();
                sanitized = true;
                notes.Add("ActiveBlockColorIds rebuilt.");
            }

            EnsureLegacyActiveBlocksArrayFromSlots();
            return sanitized;
        }

        private void EnsureActiveBlockSlotsFromLegacyArray()
        {
            if (ActiveBlockSlots != null && ActiveBlockSlots.Length == 3)
                return;

            ActiveBlockSlots = new[] { -1, -1, -1 };
            if (ActiveBlocks == null)
                return;

            int count = Math.Min(3, ActiveBlocks.Length);
            for (int i = 0; i < count; i++)
                ActiveBlockSlots[i] = ActiveBlocks[i].Value;
        }

        private void EnsureActiveBlockColorIds()
        {
            if (ActiveBlockColorIds != null && ActiveBlockColorIds.Length == 3)
                return;

            ActiveBlockColorIds = new int[3];
        }

        private void EnsureLegacyActiveBlocksArrayFromSlots()
        {
            if (ActiveBlockSlots == null || ActiveBlockSlots.Length != 3)
            {
                ActiveBlocks = Array.Empty<ShapeId>();
                return;
            }

            int count = 0;
            for (int i = 0; i < ActiveBlockSlots.Length; i++)
            {
                if (ActiveBlockSlots[i] >= 0)
                    count++;
            }

            var shapeIds = new ShapeId[count];
            int writeIndex = 0;
            for (int i = 0; i < ActiveBlockSlots.Length; i++)
            {
                if (ActiveBlockSlots[i] >= 0)
                    shapeIds[writeIndex++] = new ShapeId(ActiveBlockSlots[i]);
            }

            ActiveBlocks = shapeIds;
        }
    }

    public readonly struct GameDataMigrationResult
    {
        public readonly int SourceVersion;
        public readonly int TargetVersion;
        public readonly bool Migrated;
        public readonly bool Sanitized;
        public readonly string Note;

        public GameDataMigrationResult(int sourceVersion, int targetVersion, bool migrated, bool sanitized, string note)
        {
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            Migrated = migrated;
            Sanitized = sanitized;
            Note = note;
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
