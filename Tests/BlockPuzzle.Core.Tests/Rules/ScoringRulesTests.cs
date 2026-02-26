using NUnit.Framework;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Tests.Rules
{
    [TestFixture]
    [Category("Unit")]
    public class ScoringRulesTests
    {
        [Test]
        public void CalculateScore_OneLine_FirstCombo_ReturnsBaseScore()
        {
            var combo = new ComboState();
            combo.IncrementCombo(); // streak = 1, multiplier = 1.0

            var result = ScoringRules.CalculateScore(1, combo);

            Assert.AreEqual(10, result.ScoreDelta);
            Assert.AreEqual(1, result.LinesCleared);
            Assert.AreEqual(1, result.ComboStreak);
            Assert.AreEqual(1.0f, result.ComboMultiplier);
            Assert.AreEqual(1.0f, result.LineClearMultiplier);
        }

        [Test]
        public void CalculateScore_TwoLines_SecondCombo_AppliesBothMultipliers()
        {
            var combo = new ComboState();
            combo.IncrementCombo();
            combo.IncrementCombo(); // streak = 2, multiplier = 1.1

            var result = ScoringRules.CalculateScore(2, combo);

            // base=20, line-multiplier=1.5, combo=1.1 => 33
            Assert.AreEqual(33, result.ScoreDelta);
            Assert.AreEqual(2, result.LinesCleared);
            Assert.AreEqual(2, result.ComboStreak);
            Assert.AreEqual(1.1f, result.ComboMultiplier);
            Assert.AreEqual(1.5f, result.LineClearMultiplier);
        }

        [Test]
        public void CalculateScore_ZeroLines_ReturnsEmpty()
        {
            var combo = new ComboState();

            var result = ScoringRules.CalculateScore(0, combo);

            Assert.AreEqual(0, result.ScoreDelta);
            Assert.AreEqual(0, result.LinesCleared);
            Assert.AreEqual(0, result.ComboStreak);
        }

        [Test]
        public void CalculateScore_NegativeLines_Throws()
        {
            var combo = new ComboState();

            Assert.Throws<System.ArgumentException>(() => ScoringRules.CalculateScore(-1, combo));
        }
    }
}
