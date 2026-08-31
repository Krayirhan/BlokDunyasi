using System;
using BlockPuzzle.Core.Game;
using BlockPuzzle.UnityAdapter.Analytics;

namespace BlockPuzzle.UnityAdapter.Boot
{
    public readonly struct AnalyticsSessionContext
    {
        public readonly string GameMode;
        public readonly int DailyMissionCompletions;
        public readonly int WeeklyMissionCompletions;
        public readonly int ScoreFormulaVersion;
        public readonly bool IsExtendedTelemetryEnabled;
        public readonly int SchemaVersion;

        public AnalyticsSessionContext(
            string gameMode,
            int dailyMissionCompletions,
            int weeklyMissionCompletions,
            int scoreFormulaVersion,
            bool isExtendedTelemetryEnabled,
            int schemaVersion)
        {
            GameMode = gameMode ?? string.Empty;
            DailyMissionCompletions = dailyMissionCompletions;
            WeeklyMissionCompletions = weeklyMissionCompletions;
            ScoreFormulaVersion = scoreFormulaVersion;
            IsExtendedTelemetryEnabled = isExtendedTelemetryEnabled;
            SchemaVersion = schemaVersion;
        }
    }

    public sealed class AnalyticsTelemetryService
    {
        public event Action<AnalyticsEventData> AnalyticsEvent;

        public void EmitGameplayTelemetry(
            MoveResult moveResult,
            int comboBeforeMove,
            int comboAfterMove,
            int totalScore,
            int bestScore,
            bool isNewBest,
            int sessionMoveCount,
            AnalyticsSessionContext context)
        {
            if (moveResult == null || !moveResult.Success)
                return;

            bool isScoreAnomaly = TryGetScoreAnomalyCode(
                scoreDelta: moveResult.ScoreDelta,
                linesCleared: moveResult.LinesCleared,
                totalScore: totalScore,
                comboBefore: comboBeforeMove,
                comboAfter: comboAfterMove,
                anomalyCode: out string anomalyCode);

            EmitAnalyticsEvent(
                AnalyticsEventName.MoveScored,
                sessionMoveCount,
                totalScore,
                moveResult.ScoreDelta,
                moveResult.LinesCleared,
                comboBeforeMove,
                comboAfterMove,
                bestScore,
                isNewBest,
                isScoreAnomaly,
                anomalyCode,
                context);

            if (moveResult.LinesCleared > 0)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.LineCleared,
                    sessionMoveCount,
                    totalScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    isNewBest,
                    isScoreAnomaly,
                    anomalyCode,
                    context);
            }

            if (comboBeforeMove != comboAfterMove)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.ComboChanged,
                    sessionMoveCount,
                    totalScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    isNewBest,
                    isScoreAnomaly,
                    anomalyCode,
                    context);
            }

            if (isNewBest)
            {
                EmitAnalyticsEvent(
                    AnalyticsEventName.BestScoreUpdated,
                    sessionMoveCount,
                    totalScore,
                    moveResult.ScoreDelta,
                    moveResult.LinesCleared,
                    comboBeforeMove,
                    comboAfterMove,
                    bestScore,
                    isNewBest: true,
                    isScoreAnomaly: isScoreAnomaly,
                    scoreAnomalyCode: anomalyCode,
                    context);
            }
        }

        public void EmitMissionCompletedEvent(
            int moveCount,
            int totalScore,
            int linesCleared,
            int combo,
            int bestScore,
            AnalyticsSessionContext context)
        {
            EmitAnalyticsEvent(
                AnalyticsEventName.MissionCompleted,
                moveCount,
                totalScore,
                scoreDelta: 0,
                linesCleared,
                comboBefore: combo,
                comboAfter: combo,
                bestScore,
                isNewBest: false,
                isScoreAnomaly: false,
                scoreAnomalyCode: string.Empty,
                context);
        }

        public static bool TryGetScoreAnomalyCode(
            int scoreDelta,
            int linesCleared,
            int totalScore,
            int comboBefore,
            int comboAfter,
            out string anomalyCode)
        {
            anomalyCode = string.Empty;

            if (scoreDelta < 0)
            {
                anomalyCode = "NEGATIVE_SCORE_DELTA";
                return true;
            }

            if (totalScore < 0)
            {
                anomalyCode = "NEGATIVE_TOTAL_SCORE";
                return true;
            }

            if (linesCleared < 0)
            {
                anomalyCode = "NEGATIVE_LINES_CLEARED";
                return true;
            }

            if (linesCleared > 0 && scoreDelta <= 0)
            {
                anomalyCode = "LINES_WITHOUT_SCORE";
                return true;
            }

            if (linesCleared > 0 && comboAfter <= comboBefore)
            {
                anomalyCode = "COMBO_NOT_INCREASED_AFTER_CLEAR";
                return true;
            }

            if (linesCleared == 0 && comboAfter > comboBefore)
            {
                anomalyCode = "COMBO_INCREASED_WITHOUT_CLEAR";
                return true;
            }

            return false;
        }

        private void EmitAnalyticsEvent(
            string eventName,
            int sessionMoveCount,
            int totalScore,
            int scoreDelta,
            int linesCleared,
            int comboBefore,
            int comboAfter,
            int bestScore,
            bool isNewBest,
            bool isScoreAnomaly,
            string scoreAnomalyCode,
            AnalyticsSessionContext context)
        {
            var payload = new AnalyticsEventData(
                eventName: eventName,
                schemaVersion: context.SchemaVersion,
                scoreFormulaVersion: context.ScoreFormulaVersion,
                sessionMoveCount: sessionMoveCount,
                totalScore: totalScore,
                scoreDelta: scoreDelta,
                linesCleared: linesCleared,
                comboBefore: comboBefore,
                comboAfter: comboAfter,
                bestScore: bestScore,
                isNewBest: isNewBest,
                isScoreAnomaly: isScoreAnomaly,
                scoreAnomalyCode: scoreAnomalyCode,
                timestampUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                gameMode: context.GameMode,
                dailyMissionCompletions: context.IsExtendedTelemetryEnabled ? context.DailyMissionCompletions : -1,
                weeklyMissionCompletions: context.IsExtendedTelemetryEnabled ? context.WeeklyMissionCompletions : -1);

            AnalyticsEvent?.Invoke(payload);
        }
    }
}
