using NUnit.Framework;
using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Tests.Rules
{
    [TestFixture]
    [Category("Unit")]
    public class ScoreConfigTests
    {
        [Test]
        public void EvaluateLineMultiplier_UsesLinearInterpolation()
        {
            var config = new ScoreConfig(
                formulaVersion: 2,
                basePointsPerLine: 10,
                roundingMode: ScoreRoundingMode.Nearest,
                lineMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(3, 2.0f)
                },
                comboMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(2, 1.2f)
                });

            float valueAt2 = config.EvaluateLineMultiplier(2);

            Assert.AreEqual(1.5f, valueAt2, 0.0001f);
        }

        [Test]
        public void CalculateScore_UsesCustomConfigAndFormulaVersion()
        {
            var combo = new ComboState();
            combo.SetStreak(3);

            var config = new ScoreConfig(
                formulaVersion: 5,
                basePointsPerLine: 20,
                roundingMode: ScoreRoundingMode.Floor,
                lineMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(2, 1.4f)
                },
                comboMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(3, 1.5f)
                });

            var result = ScoringRules.CalculateScore(2, combo, config);

            // base=40, line=1.4, combo=1.5 => 83.999... and Floor => 83
            Assert.AreEqual(83, result.ScoreDelta);
            Assert.AreEqual(5, result.FormulaVersion);
            Assert.AreEqual(40, result.BaseScore);
            Assert.AreEqual(1.4f, result.LineClearMultiplier, 0.0001f);
            Assert.AreEqual(1.5f, result.ComboMultiplier, 0.0001f);
        }
    }
}
