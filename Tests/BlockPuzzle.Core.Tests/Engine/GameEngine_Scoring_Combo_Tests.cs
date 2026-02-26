using NUnit.Framework;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Tests.Engine
{
    [TestFixture]
    [Category("Integration")]
    public class GameEngineScoringComboTests
    {
        [Test]
        public void SingleMove_LineClear_ComboAndScoreAreUpdatedOnce()
        {
            // Arrange: 4x4 board with bottom row having 3/4 filled cells.
            var rng = new SeededRng(42);
            var engine = new GameEngine(rng, boardWidth: 4, boardHeight: 4);

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

            // Act: Place the single block to complete and clear one line.
            var result = engine.AttemptMove(0, new Int2(3, 0));

            // Assert: first clear must be streak 1 and +10 score (not double increment).
            Assert.IsTrue(result.Success, "Move should succeed.");
            Assert.AreEqual(1, result.LinesCleared, "Exactly one line should be cleared.");
            Assert.AreEqual(10, result.ScoreDelta, "Move result should report per-move score delta.");
            Assert.AreEqual(10, result.TotalScore, "Move result should report total score after move.");
            Assert.AreEqual(1, engine.CurrentState.ComboState.Streak, "Combo streak should be 1 on first clear.");
            Assert.AreEqual(10, engine.CurrentState.Score, "Score should be +10 for one cleared line with combo x1.0.");
        }

        [Test]
        [Category("Regression")]
        public void TwoConsecutiveLineClears_TotalLinesClearedAccumulatesWithoutDoubleCounting()
        {
            var rng = new SeededRng(123);
            var engine = new GameEngine(rng, boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];

            // Row 0: x=0,1,2 filled. x=3 will be cleared by move1.
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);

            // Row 1: x=0,1,2 filled. x=3 will be cleared by move2.
            cells[4] = CellState.Filled(1, 1);
            cells[5] = CellState.Filled(1, 1);
            cells[6] = CellState.Filled(1, 1);

            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(1, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);

            engine.LoadGame(state);

            var move1 = engine.AttemptMove(0, new Int2(3, 0));
            var move2 = engine.AttemptMove(1, new Int2(3, 1));

            Assert.IsTrue(move1.Success);
            Assert.IsTrue(move2.Success);
            Assert.AreEqual(1, move1.LinesCleared);
            Assert.AreEqual(1, move2.LinesCleared);
            Assert.AreEqual(2, engine.CurrentState.TotalLinesCleared,
                "Total lines cleared should be 2 after two single-line clears.");
        }

        [Test]
        public void PreviewMoveScore_ValidLineClear_ReturnsExpectedWithoutMutatingState()
        {
            var rng = new SeededRng(222);
            var engine = new GameEngine(rng, boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);
            state = state.WithScore(123);

            var combo = new ComboState();
            combo.SetStreak(2); // next clear should use streak=3 => combo x1.2
            state = state.WithComboState(combo);

            engine.LoadGame(state);

            var preview = engine.PreviewMoveScore(0, new Int2(3, 0));

            Assert.AreEqual(1, preview.LinesCleared);
            Assert.AreEqual(3, preview.ComboStreak);
            Assert.AreEqual(12, preview.ScoreDelta, "Expected 10 * 1.0 * 1.2 = 12.");

            // Preview must not mutate live game state.
            Assert.AreEqual(123, engine.CurrentState.Score);
            Assert.AreEqual(2, engine.CurrentState.ComboState.Streak);
            Assert.IsTrue(engine.CurrentState.Board.IsEmpty(3, 0));
            Assert.IsTrue(engine.CurrentState.ActiveBlocks.HasBlockAt(0));
        }

        [Test]
        [Category("Regression")]
        public void ScoreOverflow_IsClampedToIntMax()
        {
            var rng = new SeededRng(555);
            var engine = new GameEngine(rng, boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);
            state = state.WithScore(int.MaxValue - 1);

            engine.LoadGame(state);

            var move = engine.AttemptMove(0, new Int2(3, 0));

            Assert.IsTrue(move.Success);
            Assert.AreEqual(10, move.ScoreDelta);
            Assert.AreEqual(int.MaxValue, move.TotalScore);
            Assert.AreEqual(int.MaxValue, engine.CurrentState.Score);
        }
    }
}
