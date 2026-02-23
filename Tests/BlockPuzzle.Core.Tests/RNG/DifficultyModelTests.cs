// File: Tests/BlockPuzzle.Core.Tests/RNG/DifficultyModelTests.cs
using NUnit.Framework;
using BlockPuzzle.Core.RNG;

namespace BlockPuzzle.Core.Tests.RNG
{
    [TestFixture]
    public class DifficultyModelTests
    {
        [Test]
        public void RecordPlacement_HighSuccessRate_IncreasesDifficulty()
        {
            var model = new DifficultyModel(initialDifficulty: 0.3f, historySize: 5);
            float initial = model.DifficultyLevel;

            for (int i = 0; i < 5; i++)
            {
                model.RecordPlacement(true);
            }

            Assert.Greater(model.DifficultyLevel, initial);
        }

        [Test]
        public void RecordPlacement_LowSuccessRate_DecreasesDifficulty()
        {
            var model = new DifficultyModel(initialDifficulty: 0.7f, historySize: 5);
            for (int i = 0; i < 5; i++)
            {
                model.RecordPlacement(true);
            }

            float afterSuccess = model.DifficultyLevel;

            for (int i = 0; i < 5; i++)
            {
                model.RecordPlacement(false);
            }

            Assert.Less(model.DifficultyLevel, afterSuccess);
        }
    }
}
