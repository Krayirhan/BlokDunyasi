using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Rules;
using BlockPuzzle.UnityAdapter.Boot;
using NUnit.Framework;

namespace BlockPuzzle.UnityAdapter.Tests
{
    public sealed class AnalyticsTelemetryServiceTests
    {
        [TestCase(-1, 0, 10, 0, 0, "NEGATIVE_SCORE_DELTA")]
        [TestCase(1, -1, 10, 0, 0, "NEGATIVE_LINES_CLEARED")]
        [TestCase(0, 1, 10, 0, 1, "LINES_WITHOUT_SCORE")]
        [TestCase(1, 0, 10, 0, 1, "COMBO_INCREASED_WITHOUT_CLEAR")]
        public void TryGetScoreAnomalyCode_ReportsInvalidCombinations(
            int scoreDelta,
            int linesCleared,
            int totalScore,
            int comboBefore,
            int comboAfter,
            string expectedCode)
        {
            var isAnomaly = AnalyticsTelemetryService.TryGetScoreAnomalyCode(
                scoreDelta, linesCleared, totalScore, comboBefore, comboAfter,
                out var code);

            Assert.That(isAnomaly, Is.True);
            Assert.That(code, Is.EqualTo(expectedCode));
        }

        [Test]
        public void EmitGameplayTelemetry_EmitsMoveAndConditionalEvents()
        {
            var service = new AnalyticsTelemetryService();
            var count = 0;
            service.AnalyticsEvent += _ => count++;

            service.EmitGameplayTelemetry(
                MoveResult.CreateSuccess(25, new ScoreResult(25, 1, 1, 1f, 25, 1f), true),
                comboBeforeMove: 0,
                comboAfterMove: 1,
                totalScore: 25,
                bestScore: 25,
                isNewBest: true,
                sessionMoveCount: 1,
                context: new AnalyticsSessionContext("classic", 0, 0, 1, true, 1));

            Assert.That(count, Is.EqualTo(4));
        }
    }
}
