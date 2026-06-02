using System;
using UnityEngine;

namespace BlockPuzzle.Core.Social
{
    public class ScoreValidator
    {
        // Temel skor doğrulama kuralları (Anti-cheat)
        public static bool ValidateScore(int claimedScore, int totalMoves, int maxComboReached)
        {
            if (claimedScore < 0) return false;
            if (totalMoves == 0 && claimedScore > 0) return false;

            // Hipotetik olarak 1 hamlede alınabilecek en yüksek skor 1000 olsun. 
            // Anti-cheat kontrolü:
            int theoreticalMax = totalMoves * 150 + (maxComboReached * 50); 
            if (claimedScore > theoreticalMax)
            {
                Debug.LogWarning($"Score validation failed! Score {claimedScore} is too high for {totalMoves} moves.");
                return false;
            }

            return true;
        }
    }
}