using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Defines the scoring rules for the Blok Dünyası game (Version 3.0).
    /// </summary>
    public static class ScoringRules
    {
        private static ScoreConfig _defaultConfig = ScoreConfig.Default;

        public static ScoreConfig DefaultConfig => _defaultConfig;

        public static void SetDefaultConfig(ScoreConfig config)
        {
            _defaultConfig = config ?? ScoreConfig.Default;
        }

        /// <summary>
        /// Calculates score and updates combo state for a move.
        /// </summary>
        public static ScoreBreakdown CalculateMoveScore(
            BoardState board,
            int placedCellCount,
            int linesCleared,
            IReadOnlyList<Int2> placedPositions,
            GameMode mode,
            ref int comboCount,
            ref bool graceUsed
        )
        {
            int riskBonus = 0;
            bool isEdge = false;
            bool isCorner = false;

            if (board != null && placedPositions != null && placedPositions.Count > 0)
            {
                riskBonus = CalculateRiskBonus(board, placedPositions, out isEdge, out isCorner);
            }

            int placementScore = 0;
            int lineClearScore = 0;
            int comboBonus = 0;
            bool usedGrace = false;
            bool comboBroken = false;

            if (linesCleared > 0)
            {
                comboCount += 1;
                graceUsed = false;
                lineClearScore = GetLineClearBaseScore(linesCleared);
                
                int effectiveCombo = Math.Min(comboCount, 25);
                comboBonus = effectiveCombo * 100 * linesCleared;

                if (mode == GameMode.Zen)
                {
                    lineClearScore = (int)Math.Floor(lineClearScore * 0.7f);
                    comboBonus = (int)Math.Floor(comboBonus * 0.7f);
                }
            }
            else
            {
                placementScore = placedCellCount * 25;
                if (mode == GameMode.Zen)
                {
                    placementScore *= 2;
                }

                if (comboCount > 0)
                {
                    if (!graceUsed)
                    {
                        graceUsed = true;
                        usedGrace = true;
                    }
                    else
                    {
                        comboCount = 0;
                        graceUsed = false;
                        comboBroken = true;
                    }
                }
            }

            int totalGained = placementScore + lineClearScore + comboBonus + riskBonus;

            return new ScoreBreakdown
            {
                PlacementScore = placementScore,
                LineClearScore = lineClearScore,
                ComboBonus = comboBonus,
                RiskBonus = riskBonus,
                TotalGained = totalGained,
                LinesCleared = linesCleared,
                ComboCount = comboCount,
                UsedGrace = usedGrace,
                ComboBroken = comboBroken,
                IsEdgeBonus = isEdge,
                IsCornerBonus = isCorner
            };
        }

        /// <summary>
        /// Gets the base score for the number of lines cleared.
        /// </summary>
        public static int GetLineClearBaseScore(int linesCleared)
        {
            if (linesCleared <= 0) return 0;
            return linesCleared switch
            {
                1 => 300,
                2 => 800,
                3 => 1400,
                4 => 2200,
                _ => 3000
            };
        }

        /// <summary>
        /// Calculates risk bonus (edge or corner) for placed cells.
        /// </summary>
        public static int CalculateRiskBonus(BoardState board, IReadOnlyList<Int2> placedPositions, out bool isEdge, out bool isCorner)
        {
            isEdge = false;
            isCorner = false;

            if (board == null || placedPositions == null || placedPositions.Count == 0)
                return 0;

            bool touchesHorizontal = false;
            bool touchesVertical = false;
            int edgeTouchCount = 0;

            for (int i = 0; i < placedPositions.Count; i++)
            {
                int x = placedPositions[i].X;
                int y = placedPositions[i].Y;

                bool onHoriz = (x == 0 || x == board.Width - 1);
                bool onVert = (y == 0 || y == board.Height - 1);

                if (onHoriz) touchesHorizontal = true;
                if (onVert) touchesVertical = true;

                if (onHoriz || onVert)
                {
                    edgeTouchCount++;
                }
            }

            if (touchesHorizontal && touchesVertical)
            {
                isCorner = true;
                return 40;
            }

            if (edgeTouchCount >= 2)
            {
                isEdge = true;
                return 20;
            }

            return 0;
        }

        // Backward compatibility overloads for testing and old API calls:
        public static ScoreResult CalculateScore(int linesCleared, ComboState comboState)
        {
            int comboCount = comboState.Streak;
            bool graceUsed = comboState.GraceUsed;
            var breakdown = CalculateMoveScore(null, 0, linesCleared, null, GameMode.Classic, ref comboCount, ref graceUsed);
            
            // Apply ref modifications back to comboState:
            if (linesCleared > 0)
            {
                comboState.IncrementCombo();
            }
            else
            {
                comboState.ConsumeNonClearMove();
            }

            return new ScoreResult(breakdown);
        }

        public static ScoreResult CalculateScore(int linesCleared, ComboState comboState, ScoreConfig scoreConfig)
        {
            int comboCount = comboState.Streak;
            bool graceUsed = comboState.GraceUsed;
            var breakdown = CalculateMoveScore(null, 0, linesCleared, null, GameMode.Classic, ref comboCount, ref graceUsed);
            
            // Apply ref modifications back to comboState:
            if (linesCleared > 0)
            {
                comboState.IncrementCombo();
            }
            else
            {
                comboState.ConsumeNonClearMove();
            }

            return new ScoreResult(breakdown, scoreConfig != null ? scoreConfig.FormulaVersion : ScoreConfig.DefaultFormulaVersion);
        }

        public static ScoreResult CalculatePlacementScore(ComboState comboState)
        {
            return CalculatePlacementScore(comboState, 1, _defaultConfig);
        }

        public static ScoreResult CalculatePlacementScore(ComboState comboState, ScoreConfig scoreConfig)
        {
            return CalculatePlacementScore(comboState, 1, scoreConfig);
        }

        public static ScoreResult CalculatePlacementScore(ComboState comboState, int placedCellCount, ScoreConfig scoreConfig)
        {
            int comboCount = comboState.Streak;
            bool graceUsed = comboState.GraceUsed;
            var breakdown = CalculateMoveScore(null, placedCellCount, 0, null, GameMode.Classic, ref comboCount, ref graceUsed);
            
            // Apply ref modifications back to comboState:
            comboState.ConsumeNonClearMove();

            return new ScoreResult(breakdown);
        }
    }
}
