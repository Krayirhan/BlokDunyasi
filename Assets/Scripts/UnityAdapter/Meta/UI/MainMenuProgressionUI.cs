using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Meta.Missions;
using BlockPuzzle.Core.Meta;

namespace BlockPuzzle.UnityAdapter.Meta.UI
{
    /// <summary>
    /// Render owner for main-menu streak and claimable reward summary.
    /// Flow, navigation and reward state ownership stay outside this component.
    /// </summary>
    public class MainMenuProgressionUI : MonoBehaviour
    {
        [SerializeField] private Text streakText;
        [SerializeField] private Text completableMissionsText;
        [SerializeField] private GameObject notificationBadge;

        private void OnEnable()
        {
            UpdateProgressionCard();
        }

        public void UpdateProgressionCard()
        {
            UpdateStreakLabel();
            UpdateClaimableRewardSummary();
        }

        private void UpdateStreakLabel()
        {
            if (AchievementManager.Instance == null || streakText == null)
                return;

            int streak = AchievementManager.Instance.GetCurrentStreak();
            streakText.text = $"\U0001F525 {streak} Day Streak";
        }

        private void UpdateClaimableRewardSummary()
        {
            int unclaimCount = CountClaimableMissionRewards();

            if (notificationBadge != null)
                notificationBadge.SetActive(unclaimCount > 0);

            if (completableMissionsText == null)
                return;

            completableMissionsText.text = unclaimCount > 0
                ? $"{unclaimCount} Claimable Rewards!"
                : "Check Daily Missions!";
        }

        private static int CountClaimableMissionRewards()
        {
            if (MissionManager.Instance == null)
                return 0;

            int unclaimCount = 0;
            foreach (var mission in MissionManager.Instance.activeMissions)
            {
                var progress = MissionManager.Instance.GetProgress(mission.id);
                if (progress != null && !progress.isClaimed && progress.currentAmount >= mission.targetAmount)
                    unclaimCount++;
            }

            return unclaimCount;
        }
    }
}
