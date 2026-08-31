using System;
using BlockPuzzle.Core.Rules;
using UnityEngine;

namespace BlockPuzzle.Core.Social
{
    public static class ScoreValidator
    {
        public static bool ValidateScore(int claimedScore, int totalMoves, int maxComboReached)
        {
            if (claimedScore < 0)
                return false;

            if (totalMoves < 0 || maxComboReached < 0)
                return false;

            if (totalMoves == 0 && claimedScore > 0)
                return false;

            ScoreConfig config = ScoringRules.DefaultConfig ?? ScoreConfig.Default;
            float maxLineMultiplier = config.EvaluateLineMultiplier(8);
            float maxComboMultiplier = config.EvaluateComboMultiplier(Math.Max(1, maxComboReached));

            int maxClearBase =
                (8 * config.BasePointsPerLine) +
                config.MultiLineFinisherBonus +
                config.HighComboClearBonus;

            int maxPlacementBase =
                config.BasePointsPerPlacement +
                (9 * config.BasePointsPerPlacedCell) +
                config.HighRiskPlacementBonus;

            int maxClearPerMove = Mathf.CeilToInt(maxClearBase * maxLineMultiplier * maxComboMultiplier);
            int maxPlacementPerMove = Mathf.CeilToInt(maxPlacementBase * config.PlacementComboMaxMultiplier);
            int theoreticalMax = totalMoves * Mathf.Max(maxClearPerMove, maxPlacementPerMove);

            if (claimedScore > theoreticalMax)
            {
                Debug.LogWarning($"Score validation failed. Score {claimedScore} exceeds the theoretical cap for {totalMoves} moves.");
                return false;
            }

            return true;
        }
    }
}
