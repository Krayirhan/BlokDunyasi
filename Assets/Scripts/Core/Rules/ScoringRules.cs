// File: Core/Rules/ScoringRules.cs
using System;

namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Defines the scoring rules for the Blok Dünyası game.
    /// 
    /// Scoring Formula:
    /// - Base: 10 points per cleared line
    /// - Line Clear Multiplier: 1 + (simultaneous_lines - 1) * 0.5
    /// - Combo Multiplier: Applied from ComboState
    /// - Final Score = Base * LineClearMultiplier * ComboMultiplier
    /// 
    /// Examples:
    /// - 1 line: 10 * 1.0 = 10 points
    /// - 2 lines: 20 * 1.5 = 30 points  
    /// - 3 lines: 30 * 2.0 = 60 points
    /// - Plus combo multipliers on top
    /// </summary>
    public static class ScoringRules
    {
        private static ScoreConfig _defaultConfig = ScoreConfig.Default;

        /// <summary>
        /// Active default score configuration.
        /// </summary>
        public static ScoreConfig DefaultConfig => _defaultConfig;

        /// <summary>
        /// Replaces the default score configuration used by overloads without explicit config.
        /// </summary>
        public static void SetDefaultConfig(ScoreConfig config)
        {
            _defaultConfig = config ?? ScoreConfig.Default;
        }
        
        /// <summary>
        /// Calculates score for a move with line clears.
        /// </summary>
        /// <param name="linesCleared">Number of lines cleared this move</param>
        /// <param name="comboState">Current combo state</param>
        /// <returns>Score result with detailed breakdown</returns>
        /// <exception cref="ArgumentNullException">If comboState is null</exception>
        public static ScoreResult CalculateScore(int linesCleared, ComboState comboState)
        {
            return CalculateScore(linesCleared, comboState, _defaultConfig);
        }

        /// <summary>
        /// Calculates score for a move with line clears using a specific score configuration.
        /// </summary>
        /// <param name="linesCleared">Number of lines cleared this move</param>
        /// <param name="comboState">Current combo state</param>
        /// <param name="scoreConfig">Score formula config</param>
        /// <returns>Score result with detailed breakdown</returns>
        /// <exception cref="ArgumentNullException">If comboState or scoreConfig is null</exception>
        public static ScoreResult CalculateScore(int linesCleared, ComboState comboState, ScoreConfig scoreConfig)
        {
            if (comboState == null)
                throw new ArgumentNullException(nameof(comboState));
            if (scoreConfig == null)
                throw new ArgumentNullException(nameof(scoreConfig));
            
            if (linesCleared < 0)
                throw new ArgumentException("Lines cleared cannot be negative", nameof(linesCleared));
            
            // No lines cleared = no score
            if (linesCleared == 0)
            {
                return ScoreResult.Empty;
            }
            
            // Calculate base score with saturation for defensive safety.
            long baseScoreLong = (long)linesCleared * scoreConfig.BasePointsPerLine;
            int baseScore = baseScoreLong > int.MaxValue ? int.MaxValue : (int)baseScoreLong;
            
            // Line clear multiplier for simultaneous clears
            float lineClearMultiplier = scoreConfig.EvaluateLineMultiplier(linesCleared);
            float comboMultiplier = scoreConfig.EvaluateComboMultiplier(comboState.Streak);
            
            // Apply multipliers
            float totalMultiplier = lineClearMultiplier * comboMultiplier;
            if (totalMultiplier < 0f || float.IsNaN(totalMultiplier) || float.IsInfinity(totalMultiplier))
                throw new InvalidOperationException("Calculated score multiplier is invalid.");

            double rawScore = baseScore * (double)totalMultiplier;
            long roundedScore = scoreConfig.RoundingMode switch
            {
                ScoreRoundingMode.Floor => (long)Math.Floor(rawScore),
                ScoreRoundingMode.Ceiling => (long)Math.Ceiling(rawScore),
                ScoreRoundingMode.Truncate => (long)Math.Truncate(rawScore),
                _ => (long)Math.Round(rawScore)
            };
            int finalScore = roundedScore <= 0L
                ? 0
                : roundedScore >= int.MaxValue ? int.MaxValue : (int)roundedScore;
            
            return new ScoreResult(
                scoreDelta: finalScore,
                linesCleared: linesCleared,
                comboStreak: comboState.Streak,
                comboMultiplier: comboMultiplier,
                baseScore: baseScore,
                lineClearMultiplier: lineClearMultiplier,
                formulaVersion: scoreConfig.FormulaVersion
            );
        }
    }
}
