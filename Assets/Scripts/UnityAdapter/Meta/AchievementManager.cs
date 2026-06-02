using UnityEngine;
using BlockPuzzle.Core.Meta.Missions;

namespace BlockPuzzle.Core.Meta
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        private int currentStreakDays = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ReportLinesCleared(int lines)
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReportProgress(MissionGoalType.ClearLines, lines);
            }
        }

        public void ReportBlocksPlaced(int blocks)
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReportProgress(MissionGoalType.PlaceBlocks, blocks);
            }
        }

        public void ReportScoreEarned(int score)
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReportProgress(MissionGoalType.EarnScore, score);
            }
        }

        public void ReportDailyLogin()
        {
            currentStreakDays++; // Real implementation would rely on date validation
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReportProgress(MissionGoalType.PlayDaysInARow, 1);
            }
        }

        public int GetCurrentStreak()
        {
            return currentStreakDays;
        }
    }
}