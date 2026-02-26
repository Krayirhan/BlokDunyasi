using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Regression
{
    [TestFixture]
    [Category("Regression")]
    public class CriticalBugsRegressionTests
    {
        [Test]
        public void Bug_P0_001_TotalLinesCleared_ShouldNotDoubleCount()
        {
            var engine = new GameEngine(new SeededRng(9001), boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            cells[4] = CellState.Filled(2, 1);
            cells[5] = CellState.Filled(2, 1);
            cells[6] = CellState.Filled(2, 1);
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
            Assert.AreEqual(2, engine.CurrentState.TotalLinesCleared);
        }

        [Test]
        public void Bug_P0_002_ScoreOverflow_ShouldClampToIntMax()
        {
            var engine = new GameEngine(new SeededRng(9002), boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4).WithScore(int.MaxValue - 1);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);
            engine.LoadGame(state);

            var move = engine.AttemptMove(0, new Int2(3, 0));

            Assert.IsTrue(move.Success);
            Assert.AreEqual(10, move.ScoreDelta);
            Assert.AreEqual(int.MaxValue, move.TotalScore);
            Assert.AreEqual(int.MaxValue, engine.CurrentState.Score);
        }
    }
}
