using System.Linq;
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
            Assert.AreEqual(20, move.ScoreDelta);
            Assert.AreEqual(int.MaxValue, move.TotalScore);
            Assert.AreEqual(int.MaxValue, engine.CurrentState.Score);
        }

        [Test]
        public void Bug_P0_003_ContinueAfterGameOver_ShouldRestoreThreePlaceableBlocks()
        {
            var engine = new GameEngine(new SeededRng(9003), boardWidth: 4, boardHeight: 4);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellState.Filled(99, 1);

            cells[15] = CellState.Empty;
            state.Board.SetCells(cells);

            var blockedSet = new ActiveBlocks();
            blockedSet.SetBlockAt(0, new ShapeId(8)); // Square2x2
            blockedSet.SetBlockAt(1, new ShapeId(8));
            blockedSet.SetBlockAt(2, new ShapeId(8));

            state = state
                .WithActiveBlocks(blockedSet)
                .WithGameOver();

            engine.LoadGame(state);

            bool continued = engine.TryContinueAfterGameOver();

            Assert.IsTrue(continued);
            Assert.IsFalse(engine.CurrentState.IsGameOver);
            Assert.IsTrue(engine.CurrentState.ActiveBlocks.IsFull);

            for (int slot = 0; slot < 3; slot++)
            {
                Assert.IsTrue(engine.CurrentState.ActiveBlocks.HasBlockAt(slot), $"Slot {slot} should be filled.");

                var shapeId = engine.CurrentState.ActiveBlocks.GetShapeId(slot);
                Assert.IsTrue(ShapeLibrary.TryGetShape(shapeId, out var shape), $"Slot {slot} shape should exist in ShapeLibrary.");
                Assert.IsTrue(PlacementSearch.HasAnyValidPlacement(engine.CurrentState.Board, shape), $"Slot {slot} should be placeable after continue.");
            }
        }

        [Test]
        public void Bug_P0_004_ContinueAfterGameOver_ShouldPreferVariedBlocks_WhenBoardAllowsVariety()
        {
            var engine = new GameEngine(new SeededRng(9004), boardWidth: 8, boardHeight: 8);

            var state = new GameState(8, 8);
            var blockedSet = new ActiveBlocks();
            blockedSet.SetBlockAt(0, ShapeLibrary.Single);
            blockedSet.SetBlockAt(1, ShapeLibrary.Single);
            blockedSet.SetBlockAt(2, ShapeLibrary.Single);

            state = state
                .WithActiveBlocks(blockedSet)
                .WithGameOver();

            engine.LoadGame(state);

            bool continued = engine.TryContinueAfterGameOver();

            Assert.IsTrue(continued);
            Assert.IsFalse(engine.CurrentState.IsGameOver);
            Assert.IsTrue(engine.CurrentState.ActiveBlocks.IsFull);

            var shapeIds = engine.CurrentState.ActiveBlocks.GetShapeIds();
            Assert.GreaterOrEqual(shapeIds.Distinct().Count(), 2, "Continue should not collapse to three identical blocks on an open board.");

            for (int slot = 0; slot < 3; slot++)
            {
                var shapeId = engine.CurrentState.ActiveBlocks.GetShapeId(slot);
                Assert.IsTrue(ShapeLibrary.TryGetShape(shapeId, out var shape), $"Slot {slot} shape should exist in ShapeLibrary.");
                Assert.IsTrue(PlacementSearch.HasAnyValidPlacement(engine.CurrentState.Board, shape), $"Slot {slot} should be placeable after continue.");
            }
        }
    }
}
