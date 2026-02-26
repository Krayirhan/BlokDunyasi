using System.Linq;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Engine
{
    [TestFixture]
    [Category("Integration")]
    public class GameEngineEndToEndIntegrationTests
    {
        [Test]
        public void MoveSimulation_LineClearThenComboThenScoreIncrease_ThenGameOver()
        {
            var engine = new GameEngine(new SeededRng(20260225), boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            cells[4] = CellState.Filled(2, 2);
            cells[5] = CellState.Filled(2, 2);
            cells[6] = CellState.Filled(2, 2);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(1, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(2, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);

            engine.LoadGame(state);

            var move1 = engine.AttemptMove(0, new Int2(3, 0));
            var move2 = engine.AttemptMove(1, new Int2(3, 1));

            Assert.IsTrue(move1.Success);
            Assert.AreEqual(1, move1.LinesCleared);
            Assert.AreEqual(10, move1.ScoreDelta);

            Assert.IsTrue(move2.Success);
            Assert.AreEqual(1, move2.LinesCleared);
            Assert.AreEqual(11, move2.ScoreDelta, "Second clear should apply combo multiplier.");

            Assert.AreEqual(21, engine.CurrentState.Score);
            Assert.AreEqual(2, engine.CurrentState.ComboState.Streak);

            // Prepare an explicit no-placement state to validate game-over detection.
            var fullBoard = Enumerable.Repeat(CellState.Filled(9, 1), 16).ToArray();
            engine.CurrentState.Board.SetCells(fullBoard);

            Assert.IsTrue(engine.IsGameOver(), "A full board with remaining blocks should be game over.");
        }
    }
}
