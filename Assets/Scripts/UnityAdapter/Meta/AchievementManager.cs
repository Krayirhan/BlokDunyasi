using UnityEngine;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Meta.Missions;
using BlockPuzzle.UnityAdapter.Boot;

namespace BlockPuzzle.Core.Meta
{
    public class AchievementManager : MonoBehaviour
    {
        private static AchievementManager _instance;

        public static AchievementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var managerObject = new GameObject("AchievementManager");
                    _instance = managerObject.AddComponent<AchievementManager>();
                }

                return _instance;
            }
        }

        private int currentStreakDays = 0;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                GameBootstrap.OnBoardChanged += HandleBoardChanged;
                GameBootstrap.OnScoreBreakdown += HandleScoreBreakdown;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            GameBootstrap.OnBoardChanged -= HandleBoardChanged;
            GameBootstrap.OnScoreBreakdown -= HandleScoreBreakdown;
            _instance = null;
        }

        private void HandleBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            if (linesCleared > 0)
                ReportLinesCleared(linesCleared);
        }

        private void HandleScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            if (breakdown.ScoreDelta > 0)
                ReportScoreEarned(breakdown.ScoreDelta);
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
