using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Rules;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Game
{
    [TestFixture]
    [Category("Unit")]
    public class MoveResultTests
    {
        [Test]
        public void CreateSuccess_PopulatesScoringAndSpawnFields()
        {
            var scoreResult = new ScoreResult(
                scoreDelta: 42,
                linesCleared: 2,
                comboStreak: 3,
                comboMultiplier: 1.2f,
                baseScore: 35,
                lineClearMultiplier: 1.5f,
                formulaVersion: 7);

            var result = MoveResult.CreateSuccess(totalScore: 150, scoreResult: scoreResult, triggersSpawn: true);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(150, result.TotalScore);
            Assert.AreEqual(42, result.ScoreDelta);
            Assert.AreEqual(2, result.LinesCleared);
            Assert.AreEqual(7, result.ScoreResult.FormulaVersion);
            Assert.IsTrue(result.TriggersSpawn);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(Int2.Zero, result.PlacementPosition);
            Assert.AreEqual(0, result.ShapeIndex);
        }

        [Test]
        public void Failed_ReturnsNonSuccessAndCarriesError()
        {
            const string expectedError = "custom failure";
            var result = MoveResult.Failed(expectedError);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(expectedError, result.ErrorMessage);
            Assert.AreEqual(0, result.TotalScore);
            Assert.AreEqual(0, result.ScoreDelta);
            Assert.AreEqual(-1, result.ShapeIndex);
            Assert.AreEqual(Int2.Zero, result.PlacementPosition);
        }

        [Test]
        public void StaticFailureResults_AreConsistent()
        {
            Assert.IsFalse(MoveResult.GameOverResult.Success);
            Assert.IsFalse(MoveResult.InvalidBlockIndex.Success);
            Assert.IsFalse(MoveResult.InvalidShape.Success);
            Assert.IsFalse(MoveResult.OutOfBounds.Success);
            Assert.IsFalse(MoveResult.CellsOccupied.Success);
            Assert.IsFalse(MoveResult.InvalidPlacement.Success);
        }
    }
}
