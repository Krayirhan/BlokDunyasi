// File: Tests/BlockPuzzle.Core.Tests/Persistence/GameDataTests.cs
using System;
using NUnit.Framework;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    [Category("Unit")]
    public class GameDataTests
    {
        [Test]
        public void FromGameState_ToGameState_RestoresSlotsAndTimes()
        {
            var gameState = new GameState(8, 8);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(2, new ShapeId(5));
            activeBlocks.SetColorId(0, 3);
            activeBlocks.SetColorId(2, 7);
            gameState = gameState.WithActiveBlocks(activeBlocks);

            gameState = gameState.WithScore(120);

            var combo = new ComboState();
            combo.SetStreak(3);
            gameState = gameState.WithComboState(combo);

            gameState = gameState.WithMoveCount(7);
            gameState = gameState.WithTotalLinesCleared(4);

            var startTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var lastMoveTime = new DateTime(2024, 1, 1, 12, 5, 0, DateTimeKind.Utc);
            gameState = gameState.WithStartTime(startTime);
            gameState = gameState.WithLastMoveTime(lastMoveTime);

            var data = GameData.FromGameState(
                gameState,
                new SpawnerStats(0.2f, 0.5f, 0.5f, 10, new[] { true, false, true }),
                randomSeed: 123,
                scoreFormulaVersion: 4);
            var restored = data.ToGameState();

            Assert.AreEqual(GameData.CurrentSaveVersion, data.SaveVersion);
            Assert.AreEqual(120, restored.Score);
            Assert.AreEqual(7, restored.MoveCount);
            Assert.AreEqual(4, restored.TotalLinesCleared);
            Assert.AreEqual(3, restored.ComboState.CurrentStreak);
            Assert.AreEqual(1, restored.ComboState.GraceMovesRemaining);
            Assert.AreEqual(4, data.ScoreFormulaVersion);
            Assert.AreEqual(startTime, restored.StartTime);
            Assert.AreEqual(lastMoveTime, restored.LastMoveTime);
            Assert.IsTrue(restored.ActiveBlocks.HasBlockAt(0));
            Assert.IsFalse(restored.ActiveBlocks.HasBlockAt(1));
            Assert.IsTrue(restored.ActiveBlocks.HasBlockAt(2));
            Assert.AreEqual(ShapeLibrary.Single, restored.ActiveBlocks.GetShapeId(0));
            Assert.AreEqual(new ShapeId(5), restored.ActiveBlocks.GetShapeId(2));
            Assert.IsTrue(restored.ActiveBlocks.TryGetColorId(0, out int slot0Color));
            Assert.IsTrue(restored.ActiveBlocks.TryGetColorId(2, out int slot2Color));
            Assert.AreEqual(3, slot0Color);
            Assert.AreEqual(7, slot2Color);
            Assert.NotNull(data.SpawnerData.RecentPlacementHistory);
            CollectionAssert.AreEqual(new[] { true, false, true }, data.SpawnerData.RecentPlacementHistory);
        }

        [Test]
        public void FromGameState_CapturesSnapshot_NotAffectedByLaterStateMutation()
        {
            var gameState = new GameState(4, 4);

            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 3);
            gameState.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetColorId(0, 6);
            gameState = gameState.WithActiveBlocks(activeBlocks);

            var combo = new ComboState();
            combo.SetState(2, 1);
            gameState = gameState.WithComboState(combo);

            var data = GameData.FromGameState(
                gameState,
                new SpawnerStats(0.1f, 0.2f, 0.3f, 4, new[] { true, true, false }),
                randomSeed: 321,
                scoreFormulaVersion: 8);

            gameState.Board.SetCells(new CellState[16]);
            gameState.ActiveBlocks.RemoveBlock(0);
            gameState.ComboState.ConsumeNonClearMove();
            gameState.ComboState.ConsumeNonClearMove();

            Assert.IsFalse(data.BoardCells[0].IsEmpty);
            Assert.AreEqual(ShapeLibrary.Single.Value, data.ActiveBlockSlots[0]);
            Assert.AreEqual(6, data.ActiveBlockColorIds[0]);
            Assert.AreEqual(2, data.ComboStreak);
            Assert.AreEqual(1, data.ComboGraceMovesRemaining);
        }

        [Test]
        public void MigrateToCurrentInPlace_V1Save_PreservesBoardScoreAndBuildsSlots()
        {
            var cells = new CellState[16];
            cells[0] = CellState.Filled(4, 2);

            var data = new GameData
            {
                SaveVersion = 1,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = cells,
                Score = 125,
                ComboStreak = 2,
                ComboGraceMovesRemaining = 0,
                ActiveBlocks = new[] { ShapeLibrary.Single, new ShapeId(5) },
                ActiveBlockSlots = null,
                ActiveBlockColorIds = null
            };

            var migration = data.MigrateToCurrentInPlace();

            Assert.IsTrue(migration.Migrated);
            Assert.AreEqual(GameData.CurrentSaveVersion, data.SaveVersion);
            Assert.AreEqual(125, data.Score);
            Assert.IsFalse(data.BoardCells[0].IsEmpty);
            CollectionAssert.AreEqual(new[] { ShapeLibrary.Single.Value, 5, -1 }, data.ActiveBlockSlots);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, data.ActiveBlockColorIds);
            Assert.AreEqual(1, data.ComboGraceMovesRemaining);
        }

        [Test]
        public void MigrateToCurrentInPlace_V2Save_PreservesSlotsAndAddsMissingFields()
        {
            var data = new GameData
            {
                SaveVersion = 2,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[16],
                Score = 80,
                ComboStreak = 1,
                ComboGraceMovesRemaining = 0,
                ActiveBlockSlots = new[] { ShapeLibrary.Single.Value, -1, 8 },
                ActiveBlockColorIds = null,
                ScoreFormulaVersion = 0
            };

            var migration = data.MigrateToCurrentInPlace();

            Assert.IsTrue(migration.Migrated);
            Assert.AreEqual(GameData.CurrentSaveVersion, data.SaveVersion);
            CollectionAssert.AreEqual(new[] { ShapeLibrary.Single.Value, -1, 8 }, data.ActiveBlockSlots);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, data.ActiveBlockColorIds);
            Assert.AreEqual(ScoreConfig.DefaultFormulaVersion, data.ScoreFormulaVersion);
            Assert.AreEqual(1, data.ComboGraceMovesRemaining);
        }

        [Test]
        public void MigrateToCurrentInPlace_V3Save_OnlyNormalizesToCurrentVersion()
        {
            var data = new GameData
            {
                SaveVersion = 3,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[16],
                Score = 40,
                ActiveBlockSlots = new[] { -1, ShapeLibrary.Single.Value, -1 },
                ActiveBlockColorIds = new[] { 0, 7, 0 },
                ScoreFormulaVersion = 5
            };

            var migration = data.MigrateToCurrentInPlace();

            Assert.IsTrue(migration.Migrated);
            Assert.AreEqual(GameData.CurrentSaveVersion, data.SaveVersion);
            Assert.AreEqual(40, data.Score);
            CollectionAssert.AreEqual(new[] { -1, ShapeLibrary.Single.Value, -1 }, data.ActiveBlockSlots);
            CollectionAssert.AreEqual(new[] { 0, 7, 0 }, data.ActiveBlockColorIds);
            Assert.AreEqual(5, data.ScoreFormulaVersion);
        }

        [Test]
        public void MigrateToCurrentInPlace_MissingFields_AreDefaultedWithoutCrash()
        {
            var data = new GameData
            {
                SaveVersion = 1,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = null,
                ActiveBlocks = null,
                ActiveBlockSlots = null,
                ActiveBlockColorIds = null,
                SpawnerData = null,
                ScoreFormulaVersion = 0
            };

            var migration = data.MigrateToCurrentInPlace();

            Assert.IsTrue(migration.Sanitized);
            Assert.AreEqual(GameData.CurrentSaveVersion, data.SaveVersion);
            Assert.NotNull(data.BoardCells);
            Assert.AreEqual(16, data.BoardCells.Length);
            CollectionAssert.AreEqual(new[] { -1, -1, -1 }, data.ActiveBlockSlots);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, data.ActiveBlockColorIds);
            Assert.NotNull(data.ActiveBlocks);
            Assert.NotNull(data.SpawnerData);
            Assert.NotNull(data.SpawnerData.RecentPlacementHistory);
            Assert.AreEqual(ScoreConfig.DefaultFormulaVersion, data.ScoreFormulaVersion);
        }

        [Test]
        public void MigrateToCurrentInPlace_InvalidBoardData_SanitizesToEmptyBoardOfExpectedSize()
        {
            var data = new GameData
            {
                SaveVersion = 3,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[3]
            };

            var migration = data.MigrateToCurrentInPlace();

            Assert.IsTrue(migration.Sanitized);
            Assert.AreEqual(16, data.BoardCells.Length);
            for (int i = 0; i < data.BoardCells.Length; i++)
                Assert.IsTrue(data.BoardCells[i].IsEmpty);
        }

        [Test]
        public void MigrateToCurrentInPlace_FutureVersion_Throws()
        {
            var data = new GameData
            {
                SaveVersion = GameData.CurrentSaveVersion + 1,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[16]
            };

            Assert.Throws<NotSupportedException>(() => data.MigrateToCurrentInPlace());
        }
    }
}
