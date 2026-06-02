// File: Core/Rules/ScoringRules.cs
using System;

namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Defines the scoring rules for the Blok Dünyası game.
    /// 
    /// Scoring behavior is fully config-driven via <see cref="ScoreConfig"/>:
    /// - Line clear score uses base points per line with line + combo multipliers.
    /// - Legal no-clear placements can still award placement points.
    /// - Rounding mode and formula version are also taken from config.
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
                return CalculatePlacementScore(comboState, scoreConfig);
            }
            
            // Calculate base score with saturation for defensive safety.
            long baseScoreLong = (long)linesCleared * scoreConfig.BasePointsPerLine;
            if (linesCleared >= 3)
                baseScoreLong += scoreConfig.MultiLineFinisherBonus;
            if (comboState.Streak >= 6)
                baseScoreLong += scoreConfig.HighComboClearBonus;
            int baseScore = baseScoreLong > int.MaxValue ? int.MaxValue : (int)baseScoreLong;
            
            // Line clear multiplier for simultaneous clears
            float lineClearMultiplier = scoreConfig.EvaluateLineMultiplier(linesCleared);
            float comboMultiplier = scoreConfig.EvaluateComboMultiplier(comboState.Streak);
            
            int finalScore = CalculateFinalScore(baseScore, lineClearMultiplier, comboMultiplier, scoreConfig);
            
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

        /// <summary>
        /// Calculates score for a legal placement that does not clear lines.
        /// Keeps score flow active to avoid dead turns while preserving combo reset behavior.
        /// </summary>
        public static ScoreResult CalculatePlacementScore(ComboState comboState)
        {
            return CalculatePlacementScore(comboState, placedCellCount: 1, _defaultConfig);
        }

        /// <summary>
        /// Calculates score for a legal placement that does not clear lines using a specific config.
        /// </summary>
        public static ScoreResult CalculatePlacementScore(ComboState comboState, ScoreConfig scoreConfig)
        {
            return CalculatePlacementScore(comboState, placedCellCount: 1, scoreConfig);
        }

        /// <summary>
        /// Calculates score for a legal placement that does not clear lines using a specific config and placed cell count.
        /// </summary>
        public static ScoreResult CalculatePlacementScore(ComboState comboState, int placedCellCount, ScoreConfig scoreConfig)
        {
            if (comboState == null)
                throw new ArgumentNullException(nameof(comboState));
            if (scoreConfig == null)
                throw new ArgumentNullException(nameof(scoreConfig));

            int safePlacedCellCount = placedCellCount <= 0 ? 1 : placedCellCount;
            bool isHighRiskPlacement = comboState.Streak >= 2;
            long riskBonus = isHighRiskPlacement ? scoreConfig.HighRiskPlacementBonus : 0L;
            long placementBaseLong = scoreConfig.BasePointsPerPlacement +
                                     ((long)scoreConfig.BasePointsPerPlacedCell * safePlacedCellCount) +
                                     riskBonus;
            int baseScore = placementBaseLong >= int.MaxValue ? int.MaxValue : (int)placementBaseLong;

            if (baseScore <= 0)
            {
                return new ScoreResult(
                    scoreDelta: 0,
                    linesCleared: 0,
                    comboStreak: comboState.Streak,
                    comboMultiplier: scoreConfig.EvaluateComboMultiplier(comboState.Streak),
                    baseScore: 0,
                    lineClearMultiplier: 1.0f,
                    formulaVersion: scoreConfig.FormulaVersion);
            }

            float placementComboMultiplier = 1.0f + (comboState.Streak * scoreConfig.PlacementComboStepMultiplier);
            if (placementComboMultiplier > scoreConfig.PlacementComboMaxMultiplier)
                placementComboMultiplier = scoreConfig.PlacementComboMaxMultiplier;

            int finalScore = CalculateFinalScore(baseScore, lineClearMultiplier: 1.0f, comboMultiplier: placementComboMultiplier, scoreConfig);
            return new ScoreResult(
                scoreDelta: finalScore,
                linesCleared: 0,
                comboStreak: comboState.Streak,
                comboMultiplier: placementComboMultiplier,
                baseScore: baseScore,
                lineClearMultiplier: 1.0f,
                formulaVersion: scoreConfig.FormulaVersion);
        }

        private static int CalculateFinalScore(int baseScore, float lineClearMultiplier, float comboMultiplier, ScoreConfig scoreConfig)
        {
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

            if (roundedScore <= 0L)
                return 0;

            return roundedScore >= int.MaxValue ? int.MaxValue : (int)roundedScore;
        }
    }
}
