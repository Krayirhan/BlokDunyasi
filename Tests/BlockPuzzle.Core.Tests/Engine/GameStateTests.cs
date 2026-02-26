using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Shapes;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Engine
{
    [TestFixture]
    [Category("Unit")]
    public class GameStateTests
    {
        [Test]
        public void Constructor_InitializesExpectedDefaults()
        {
            var state = new GameState(boardWidth: 8, boardHeight: 9);

            Assert.AreEqual(8, state.Board.Width);
            Assert.AreEqual(9, state.Board.Height);
            Assert.AreEqual(0, state.Score);
            Assert.AreEqual(0, state.Combo);
            Assert.AreEqual(0, state.MoveCount);
            Assert.AreEqual(0, state.TotalLinesCleared);
            Assert.IsFalse(state.IsGameOver);
            Assert.IsTrue(state.ActiveBlocks.IsEmpty);
        }

        [Test]
        public void WithScore_NegativeValue_ClampsToZero()
        {
            var state = new GameState(4, 4).WithScore(50);

            var updated = state.WithScore(-10);

            Assert.AreEqual(50, state.Score, "Original state must stay immutable.");
            Assert.AreEqual(0, updated.Score);
        }

        [Test]
        public void WithLinesCleared_AddsToExistingTotal()
        {
            var state = new GameState(4, 4).WithTotalLinesCleared(3);

            var updated = state.WithLinesCleared(2);

            Assert.AreEqual(3, state.TotalLinesCleared, "Original state must stay immutable.");
            Assert.AreEqual(5, updated.TotalLinesCleared);
        }

        [Test]
        public void WithIncrementedMoveCount_IncrementsAndUpdatesLastMoveTime()
        {
            var before = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
            var state = new GameState(4, 4).WithLastMoveTime(before);

            var updated = state.WithIncrementedMoveCount();

            Assert.AreEqual(0, state.MoveCount, "Original state must stay immutable.");
            Assert.AreEqual(1, updated.MoveCount);
            Assert.GreaterOrEqual(updated.LastMoveTime, before);
        }

        [Test]
        public void Clone_CreatesDeepCopyOfBoardAndActiveBlocks()
        {
            var original = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 2);
            original.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetColorId(0, 5);
            original = original.WithActiveBlocks(activeBlocks);

            var clone = original.Clone();
            clone.Board.SetCells(new CellState[16]);
            clone.ActiveBlocks.RemoveBlock(0);

            Assert.IsTrue(original.Board.IsOccupied(0, 0));
            Assert.IsTrue(original.ActiveBlocks.HasBlockAt(0));
            Assert.IsFalse(clone.Board.IsOccupied(0, 0));
            Assert.IsFalse(clone.ActiveBlocks.HasBlockAt(0));
        }
    }
}
