using NUnit.Framework;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Tests.Engine
{
    [TestFixture]
    [Category("Integration")]
    public class GameEngineFullFlowTests
    {
        [Test]
        public void ThreeMoves_ClearsLine_ResetsCombo_ThenSpawnsNextSet()
        {
            var rng = new SeededRng(7);
            var engine = new GameEngine(rng, boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var activeBlocks = new ActiveBlocks();
            activeBlocks.SetBlockAt(0, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(1, ShapeLibrary.Single);
            activeBlocks.SetBlockAt(2, ShapeLibrary.Single);
            state = state.WithActiveBlocks(activeBlocks);

            engine.LoadGame(state);

            var move1 = engine.AttemptMove(0, new Int2(3, 0)); // line clear
            var move2 = engine.AttemptMove(1, new Int2(0, 0)); // no clear
            var move3 = engine.AttemptMove(2, new Int2(1, 0)); // consumes last slot -> spawn

            Assert.IsTrue(move1.Success);
            Assert.AreEqual(1, move1.LinesCleared);
            Assert.IsTrue(move2.Success);
            Assert.AreEqual(0, move2.LinesCleared);
            Assert.IsTrue(move3.Success);
            Assert.IsTrue(move3.TriggersSpawn);

            Assert.AreEqual(10, engine.CurrentState.Score);
            Assert.AreEqual(0, engine.CurrentState.ComboState.Streak);
            Assert.AreEqual(3, engine.CurrentState.MoveCount);
            Assert.IsTrue(engine.CurrentState.ActiveBlocks.IsFull);
            Assert.AreEqual(3, engine.CurrentState.ActiveBlocks.Count);
            Assert.IsFalse(engine.CurrentState.IsGameOver);
        }
    }
}
