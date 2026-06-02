using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Social;

namespace BlockPuzzle.UnityAdapter.Social.UI
{
    public class ShareSummaryUI : MonoBehaviour
    {
        [SerializeField] private Text percentileText;

        public void SetupSummary(int finalScore, int totalPlayers)
        {
            if (LeaderboardManager.Instance != null)
            {
                float percentile = LeaderboardManager.Instance.GetPlayerPercentile(finalScore, totalPlayers);
                percentileText.text = $"You beat {percentile:F1}% of the players this week!";
            }
        }

        public void OnShareButtonClicked(int score)
        {
            string message = "";
            if (DailyChallengeManager.Instance != null && DailyChallengeManager.Instance.IsPlayingDailyChallenge)
            {
                message = $"I scored {score} points in the Daily Block Challenge today! Can you beat my fixed-seed run? #BlockPuzzle";
            }
            else
            {
                message = $"I just reached {score} points in Block Puzzle! Try to beat my score! #BlockPuzzle";
            }

            Debug.Log($"[Simulated Native Share]: {message}");
            
            // Gerçek cihaz entegrasyonu için:
            // NativeShare eklentisi veya Application.OpenURL kullanabilirsiniz.
            GUIUtility.systemCopyBuffer = message;
        }
    }
}