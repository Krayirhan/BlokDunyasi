using UnityEngine;

namespace BlockPuzzle.Core.LiveOps
{
    [CreateAssetMenu(fileName = "NewSeasonalEvent", menuName = "BlockPuzzle/LiveOps/Seasonal Event Config")]
    public class SeasonalEventConfig : ScriptableObject
    {
        public string eventId; // e.g. "halloween_2026", "winter_holidays"
        public string displayName;
        public bool isActive;
        
        [Header("Event Overrides")]
        public string forcedThemeId; 
        public float scoreMultiplier = 1.0f;
        public int bonusStartingCoins = 0;
        
        [Header("Mission & Ads Policy")]
        public int extraDailyMissions = 0;
        public int reducedAdIntervalConfig = 0;

        public bool IsEventActive()
        {
            // In a real scenario, this would query a backend server's timestamp
            return isActive;
        }
    }
}