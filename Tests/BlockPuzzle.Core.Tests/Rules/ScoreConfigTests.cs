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
                basePointsPerPlacement: 0,
                basePointsPerPlacedCell: 0,
                highRiskPlacementBonus: 0,
                multiLineFinisherBonus: 0,
                highComboClearBonus: 0,
                placementComboStepMultiplier: 0f,
                placementComboMaxMultiplier: 1f,
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
                basePointsPerPlacement: 0,
                basePointsPerPlacedCell: 0,
                highRiskPlacementBonus: 0,
                multiLineFinisherBonus: 0,
                highComboClearBonus: 0,
                placementComboStepMultiplier: 0f,
                placementComboMaxMultiplier: 1f,
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

            // base=800, combo=4*100*2 = 800 => 1600
            Assert.AreEqual(1600, result.ScoreDelta);
            Assert.AreEqual(5, result.FormulaVersion);
        }
    }
}
