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
        /// <summary>
        /// Base points awarded per cleared line.
        /// </summary>
        public const int BasePointsPerLine = 10;
        
        /// <summary>
        /// Calculates score for a move with line clears.
        /// </summary>
        /// <param name="linesCleared">Number of lines cleared this move</param>
        /// <param name="comboState">Current combo state (will be updated)</param>
        /// <returns>Score result with detailed breakdown</returns>
        /// <exception cref="ArgumentNullException">If comboState is null</exception>
        public static ScoreResult CalculateScore(int linesCleared, ComboState comboState)
        {
            if (comboState == null)
                throw new ArgumentNullException(nameof(comboState));
            
            if (linesCleared < 0)
                throw new ArgumentException("Lines cleared cannot be negative", nameof(linesCleared));
            
            // Update combo state first
            comboState.UpdateCombo(linesCleared);
            
            // No lines cleared = no score
            if (linesCleared == 0)
            {
                return ScoreResult.Empty;
            }
            
            // Calculate base score
            int baseScore = linesCleared * BasePointsPerLine;
            
            // Line clear multiplier for simultaneous clears
            float lineClearMultiplier = CalculateLineClearMultiplier(linesCleared);
            
            // Apply multipliers
            float totalMultiplier = lineClearMultiplier * comboState.Multiplier;
            int finalScore = (int)Math.Round(baseScore * totalMultiplier);
            
            return new ScoreResult(
                scoreDelta: finalScore,
                linesCleared: linesCleared,
                comboStreak: comboState.Streak,
                comboMultiplier: comboState.Multiplier,
                baseScore: baseScore,
                lineClearMultiplier: lineClearMultiplier
            );
        }
        
        /// <summary>
        /// Calculates the multiplier for simultaneous line clears.
        /// Formula: 1.0 + (lines - 1) * 0.5
        /// This rewards clearing multiple lines at once.
        /// </summary>
        /// <param name="linesCleared">Number of lines cleared simultaneously</param>
        /// <returns>Multiplier for simultaneous clears</returns>
        private static float CalculateLineClearMultiplier(int linesCleared)
        {
            if (linesCleared <= 0) return 1.0f;
            return 1.0f + (linesCleared - 1) * 0.5f;
        }
    }
}