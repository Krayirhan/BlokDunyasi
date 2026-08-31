// File: Core/Rules/ScoreResult.cs
namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Result of a scoring calculation for a move.
    /// Contains all scoring-related information for UI and game state updates.
    /// </summary>
    public readonly struct ScoreResult
    {
        /// <summary>
        /// Points added to the score this move.
        /// </summary>
        public readonly int ScoreDelta;
        
        /// <summary>
        /// Total number of lines cleared this move.
        /// </summary>
        public readonly int LinesCleared;
        
        /// <summary>
        /// Current combo streak after this move.
        /// </summary>
        public readonly int ComboStreak;
        
        /// <summary>
        /// Combo multiplier applied to this move.
        /// </summary>
        public readonly float ComboMultiplier;
        
        /// <summary>
        /// Base score before multipliers.
        /// </summary>
        public readonly int BaseScore;
        
        /// <summary>
        /// Line clear multiplier (based on simultaneous clears).
        /// </summary>
        public readonly float LineClearMultiplier;

        /// <summary>
        /// Score formula version used for this calculation.
        /// </summary>
        public readonly int FormulaVersion;

        /// <summary>
        /// Detailed score breakdown for scoring 3.0.
        /// </summary>
        public readonly ScoreBreakdown Breakdown;

        /// <summary>
        /// Total multiplier applied to the base score.
        /// </summary>
        public float TotalMultiplier => ComboMultiplier * LineClearMultiplier;
        
        /// <summary>
        /// Creates a score result.
        /// </summary>
        /// <param name="scoreDelta">Points added this move</param>
        /// <param name="linesCleared">Lines cleared this move</param>
        /// <param name="comboStreak">Current combo streak</param>
        /// <param name="comboMultiplier">Combo multiplier applied</param>
        /// <param name="baseScore">Base score before multipliers</param>
        /// <param name="lineClearMultiplier">Line clear multiplier</param>
        /// <param name="formulaVersion">Score formula version</param>
        public ScoreResult(int scoreDelta, int linesCleared, int comboStreak, 
            float comboMultiplier, int baseScore, float lineClearMultiplier, int formulaVersion = ScoreConfig.DefaultFormulaVersion)
        {
            ScoreDelta = scoreDelta;
            LinesCleared = linesCleared;
            ComboStreak = comboStreak;
            ComboMultiplier = comboMultiplier;
            BaseScore = baseScore;
            LineClearMultiplier = lineClearMultiplier;
            FormulaVersion = formulaVersion;
            Breakdown = new ScoreBreakdown
            {
                PlacementScore = linesCleared == 0 ? scoreDelta : 0,
                LineClearScore = linesCleared > 0 ? baseScore : 0,
                ComboBonus = 0,
                RiskBonus = 0,
                TotalGained = scoreDelta,
                LinesCleared = linesCleared,
                ComboCount = comboStreak,
                UsedGrace = false,
                ComboBroken = false,
                IsEdgeBonus = false,
                IsCornerBonus = false
            };
        }

        /// <summary>
        /// Creates a score result from a 3.0 ScoreBreakdown.
        /// </summary>
        public ScoreResult(ScoreBreakdown breakdown, int formulaVersion = ScoreConfig.DefaultFormulaVersion)
        {
            ScoreDelta = breakdown.TotalGained;
            LinesCleared = breakdown.LinesCleared;
            ComboStreak = breakdown.ComboCount;
            ComboMultiplier = 1.0f;
            BaseScore = breakdown.LineClearScore + breakdown.PlacementScore;
            LineClearMultiplier = 1.0f;
            FormulaVersion = formulaVersion;
            Breakdown = breakdown;
        }
        
        /// <summary>
        /// Empty score result for moves with no scoring.
        /// </summary>
        public static readonly ScoreResult Empty = new ScoreResult(0, 0, 0, 1.0f, 0, 1.0f, ScoreConfig.DefaultFormulaVersion);
        
        public override string ToString()
        {
            if (ScoreDelta == 0)
                return "No score";
            
            return $"+{ScoreDelta} pts ({LinesCleared} lines, x{ComboMultiplier:F1} combo, v{FormulaVersion})";
        }
    }
}
