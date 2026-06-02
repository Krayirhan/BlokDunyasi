using System;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Rules;
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

            Assert.AreEqual(50, state.Score, "WithScore must not mutate the source state instance.");
            Assert.AreEqual(0, updated.Score);
        }

        [Test]
        public void WithLinesCleared_AddsToExistingTotal()
        {
            var state = new GameState(4, 4).WithTotalLinesCleared(3);

            var updated = state.WithLinesCleared(2);

            Assert.AreEqual(3, state.TotalLinesCleared, "WithLinesCleared must not mutate the source state instance.");
            Assert.AreEqual(5, updated.TotalLinesCleared);
        }

        [Test]
        public void WithIncrementedMoveCount_IncrementsAndUpdatesLastMoveTime()
        {
            var before = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
            var state = new GameState(4, 4).WithLastMoveTime(before);

            var updated = state.WithIncrementedMoveCount();

            Assert.AreEqual(0, state.MoveCount, "WithIncrementedMoveCount must not mutate the source state instance.");
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

        [Test]
        public void Clone_CreatesDeepCopyOfComboState()
        {
            var combo = new ComboState();
            combo.SetState(3, 1);

            var original = new GameState(4, 4).WithComboState(combo);
            var clone = original.Clone();

            original.ComboState.ConsumeNonClearMove();
            original.ComboState.ConsumeNonClearMove();

            Assert.AreEqual(3, clone.ComboState.Streak);
            Assert.AreEqual(1, clone.ComboState.GraceMovesRemaining);
            Assert.AreEqual(0, original.ComboState.Streak);
        }

        [Test]
        public void CreateSnapshot_BoardMutationAfterSnapshot_DoesNotAffectSnapshot()
        {
            var original = new GameState(4, 4);
            var snapshot = original.CreateSnapshot();

            var cells = new CellState[16];
            cells[5] = CellState.Filled(1, 1);
            original.Board.SetCells(cells);

            Assert.IsTrue(original.Board.IsOccupied(1, 1));
            Assert.IsTrue(snapshot.Board.IsEmpty(1, 1));
        }

        [Test]
        public void CurrentState_IsLiveMutableState_AndSnapshotIsIsolated()
        {
            var engine = new GameEngine(new SeededRng(20260520), boardWidth: 4, boardHeight: 4);
            var state = new GameState(4, 4);

            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);
            engine.LoadGame(state);

            var snapshot = engine.GetStateSnapshot();
            var move = engine.AttemptMove(0, new BlockPuzzle.Core.Common.Int2(3, 0));

            Assert.IsTrue(move.Success);
            Assert.IsTrue(snapshot.Board.IsOccupied(0, 0));
            Assert.IsTrue(snapshot.Board.IsOccupied(1, 0));
            Assert.IsTrue(snapshot.Board.IsOccupied(2, 0));
            Assert.IsTrue(snapshot.ActiveBlocks.HasBlockAt(0));
            Assert.AreEqual(0, snapshot.Score);
            Assert.IsTrue(engine.CurrentState.Board.IsEmpty(0, 0));
            Assert.AreEqual(20, engine.CurrentState.Score);
        }

        [Test]
        public void LoadGame_ClonesInputState_AndPreventsExternalAliasing()
        {
            var engine = new GameEngine(new SeededRng(20260521), boardWidth: 4, boardHeight: 4);
            var input = new GameState(4, 4);

            var cells = new CellState[16];
            cells[0] = CellState.Filled(9, 9);
            input.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            input = input.WithActiveBlocks(activeBlocks);

            engine.LoadGame(input);

            input.ActiveBlocks.RemoveBlock(0);
            input.Board.SetCells(new CellState[16]);

            Assert.IsTrue(engine.CurrentState.ActiveBlocks.HasBlockAt(0));
            Assert.IsTrue(engine.CurrentState.Board.IsOccupied(0, 0));
        }
    }
}
