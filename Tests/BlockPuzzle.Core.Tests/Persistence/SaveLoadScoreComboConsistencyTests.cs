using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    [Category("Integration")]
    public class SaveLoadScoreComboConsistencyTests
    {
        [Test]
        public void SaveLoadRoundTrip_PreservesScoreComboAndFormulaVersion()
        {
            var engine = new GameEngine(new SeededRng(20260226), boardWidth: 4, boardHeight: 4);
            var state = new GameState(4, 4);

            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            cells[4] = CellState.Filled(2, 2);
            cells[5] = CellState.Filled(2, 2);
            cells[6] = CellState.Filled(2, 2);
            state.Board.SetCells(cells);

            var active = new ActiveBlocks();
            active.SetBlockAt(0, ShapeLibrary.Single);
            active.SetBlockAt(1, ShapeLibrary.Single);
            state = state.WithActiveBlocks(active);
            engine.LoadGame(state);

            var move1 = engine.AttemptMove(0, new BlockPuzzle.Core.Common.Int2(3, 0));
            var move2 = engine.AttemptMove(1, new BlockPuzzle.Core.Common.Int2(3, 1));

            Assert.IsTrue(move1.Success);
            Assert.IsTrue(move2.Success);
            Assert.AreEqual(21, engine.CurrentState.Score);
            Assert.AreEqual(2, engine.CurrentState.ComboState.Streak);

            const int formulaVersion = 9;
            var snapshot = GameData.FromGameState(
                engine.CurrentState,
                engine.BlockSpawner.GetStats(),
                randomSeed: 1337,
                scoreFormulaVersion: formulaVersion);

            var restored = snapshot.ToGameState();

            Assert.AreEqual(engine.CurrentState.Score, restored.Score);
            Assert.AreEqual(engine.CurrentState.ComboState.Streak, restored.ComboState.Streak);
            Assert.AreEqual(engine.CurrentState.TotalLinesCleared, restored.TotalLinesCleared);
            Assert.AreEqual(engine.CurrentState.MoveCount, restored.MoveCount);
            Assert.AreEqual(formulaVersion, snapshot.ScoreFormulaVersion);
        }

        [Test]
        public void SaveLoadRoundTrip_AfterNoClearMove_KeepsComboResetState()
        {
            var engine = new GameEngine(new SeededRng(20260227), boardWidth: 4, boardHeight: 4);
            engine.StartNewGame(20260227);

            var state = new GameState(4, 4);
            var cells = new CellState[16];
            cells[0] = CellState.Filled(1, 1);
            cells[1] = CellState.Filled(1, 1);
            cells[2] = CellState.Filled(1, 1);
            state.Board.SetCells(cells);

            var active = new ActiveBlocks();
            active.SetBlockAt(0, ShapeLibrary.Single);
            active.SetBlockAt(1, ShapeLibrary.Single);
            state = state.WithActiveBlocks(active);
            engine.LoadGame(state);

            var clearMove = engine.AttemptMove(0, new BlockPuzzle.Core.Common.Int2(3, 0));
            var noClearMove = engine.AttemptMove(1, new BlockPuzzle.Core.Common.Int2(0, 0));

            Assert.IsTrue(clearMove.Success);
            Assert.IsTrue(noClearMove.Success);
            Assert.AreEqual(0, noClearMove.ScoreDelta);
            Assert.AreEqual(0, engine.CurrentState.ComboState.Streak);

            var snapshot = GameData.FromGameState(
                engine.CurrentState,
                engine.BlockSpawner.GetStats(),
                randomSeed: 1,
                scoreFormulaVersion: 3);

            var restored = snapshot.ToGameState();
            Assert.AreEqual(engine.CurrentState.Score, restored.Score);
            Assert.AreEqual(0, restored.ComboState.Streak);
        }
    }
}
