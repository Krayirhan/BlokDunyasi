// File: Tests/BlockPuzzle.Core.Tests/Persistence/GameDataTests.cs
using System;
using NUnit.Framework;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    public class GameDataTests
    {
        [Test]
        public void FromGameState_ToGameState_RestoresSlotsAndTimes()
        {
            var gameState = new GameState(8, 8);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(2, new ShapeId(5));
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

            var data = GameData.FromGameState(gameState, new SpawnerStats(0.2f, 0.5f, 0.5f, 10), randomSeed: 123);
            var restored = data.ToGameState();

            Assert.AreEqual(120, restored.Score);
            Assert.AreEqual(7, restored.MoveCount);
            Assert.AreEqual(4, restored.TotalLinesCleared);
            Assert.AreEqual(3, restored.ComboState.CurrentStreak);
            Assert.AreEqual(startTime, restored.StartTime);
            Assert.AreEqual(lastMoveTime, restored.LastMoveTime);
            Assert.IsTrue(restored.ActiveBlocks.HasBlockAt(0));
            Assert.IsFalse(restored.ActiveBlocks.HasBlockAt(1));
            Assert.IsTrue(restored.ActiveBlocks.HasBlockAt(2));
            Assert.AreEqual(ShapeLibrary.Single, restored.ActiveBlocks.GetShapeId(0));
            Assert.AreEqual(new ShapeId(5), restored.ActiveBlocks.GetShapeId(2));
        }
    }
}
