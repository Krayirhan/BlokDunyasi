using System;

namespace BlockPuzzle.UnityAdapter.Analytics
{
    /// <summary>
    /// Versioned analytics payload emitted by gameplay events.
    /// </summary>
    public readonly struct AnalyticsEventData
    {
        public readonly string EventName;
        public readonly int SchemaVersion;
        public readonly int ScoreFormulaVersion;
        public readonly int SessionMoveCount;
        public readonly int TotalScore;
        public readonly int ScoreDelta;
        public readonly int LinesCleared;
        public readonly int ComboBefore;
        public readonly int ComboAfter;
        public readonly int BestScore;
        public readonly bool IsNewBest;
        public readonly bool IsScoreAnomaly;
        public readonly string ScoreAnomalyCode;
        public readonly long TimestampUnixMs;

        public AnalyticsEventData(
            string eventName,
            int schemaVersion,
            int scoreFormulaVersion,
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
            long timestampUnixMs)
        {
            EventName = eventName ?? string.Empty;
            SchemaVersion = schemaVersion;
            ScoreFormulaVersion = scoreFormulaVersion;
            SessionMoveCount = sessionMoveCount;
            TotalScore = totalScore;
            ScoreDelta = scoreDelta;
            LinesCleared = linesCleared;
            ComboBefore = comboBefore;
            ComboAfter = comboAfter;
            BestScore = bestScore;
            IsNewBest = isNewBest;
            IsScoreAnomaly = isScoreAnomaly;
            ScoreAnomalyCode = scoreAnomalyCode ?? string.Empty;
            TimestampUnixMs = timestampUnixMs;
        }

        public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(TimestampUnixMs);
    }
}
