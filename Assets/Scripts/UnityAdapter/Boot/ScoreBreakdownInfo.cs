using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.UnityAdapter.Boot
{
    /// <summary>
    /// Public score breakdown payload for UI and debug tooling.
    /// </summary>
    public readonly struct ScoreBreakdownInfo
    {
        public readonly int ScoreDelta;
        public readonly int TotalScore;
        public readonly int BaseScore;
        public readonly int LinesCleared;
        public readonly int ComboStreak;
        public readonly float ComboMultiplier;
        public readonly float LineClearMultiplier;
        public readonly float TotalMultiplier;
        public readonly int FormulaVersion;
        public readonly bool IsNewBest;

        public ScoreBreakdownInfo(ScoreResult scoreResult, int totalScore, bool isNewBest)
        {
            ScoreDelta = scoreResult.ScoreDelta;
            TotalScore = totalScore;
            BaseScore = scoreResult.BaseScore;
            LinesCleared = scoreResult.LinesCleared;
            ComboStreak = scoreResult.ComboStreak;
            ComboMultiplier = scoreResult.ComboMultiplier;
            LineClearMultiplier = scoreResult.LineClearMultiplier;
            TotalMultiplier = scoreResult.TotalMultiplier;
            FormulaVersion = scoreResult.FormulaVersion;
            IsNewBest = isNewBest;
        }
    }
}
