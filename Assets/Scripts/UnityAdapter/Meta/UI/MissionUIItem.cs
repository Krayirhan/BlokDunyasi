using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Meta.Missions;

namespace BlockPuzzle.UnityAdapter.Meta.UI
{
    public class MissionUIItem : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject completedCheckmark;

        private string currentMissionId;

        public void Setup(MissionDefinition mission, MissionProgress progress)
        {
            currentMissionId = mission.id;
            titleText.text = mission.title;
            
            float progressPercentage = Mathf.Clamp01((float)progress.currentAmount / mission.targetAmount);
            progressBarFill.fillAmount = progressPercentage;
            progressText.text = $"{progress.currentAmount} / {mission.targetAmount}";

            bool isCompletable = progress.currentAmount >= mission.targetAmount;
            bool isAlreadyClaimed = progress.isClaimed;

            if (isAlreadyClaimed)
            {
                claimButton.gameObject.SetActive(false);
                completedCheckmark.SetActive(true);
            }
            else
            {
                completedCheckmark.SetActive(false);
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = isCompletable;
            }

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => OnClaimClicked());
        }

        private void OnClaimClicked()
        {
            if (MissionManager.Instance != null && MissionManager.Instance.ClaimReward(currentMissionId))
            {
                claimButton.gameObject.SetActive(false);
                completedCheckmark.SetActive(true);
            }
        }
    }
}