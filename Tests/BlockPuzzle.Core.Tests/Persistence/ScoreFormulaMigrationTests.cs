using NUnit.Framework;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    [Category("Unit")]
    public class ScoreFormulaMigrationTests
    {
        [Test]
        public void MigrateInPlace_UpdatesVersionAndPreservesScore()
        {
            var data = new GameData
            {
                Score = 250,
                ComboStreak = 4,
                ScoreFormulaVersion = 1
            };

            var result = ScoreFormulaMigration.MigrateInPlace(data, targetFormulaVersion: 3);

            Assert.IsTrue(result.Migrated);
            Assert.AreEqual(1, result.SourceVersion);
            Assert.AreEqual(3, result.TargetVersion);
            Assert.AreEqual(250, data.Score);
            Assert.AreEqual(4, data.ComboStreak);
            Assert.AreEqual(3, data.ScoreFormulaVersion);
        }

        [Test]
        public void MigrateInPlace_SanitizesNegativeValues()
        {
            var data = new GameData
            {
                Score = -10,
                ComboStreak = -2,
                ScoreFormulaVersion = ScoreConfig.DefaultFormulaVersion
            };

            var result = ScoreFormulaMigration.MigrateInPlace(data, targetFormulaVersion: ScoreConfig.DefaultFormulaVersion);

            Assert.IsTrue(result.Migrated);
            Assert.AreEqual(0, data.Score);
            Assert.AreEqual(0, data.ComboStreak);
        }
    }
}
