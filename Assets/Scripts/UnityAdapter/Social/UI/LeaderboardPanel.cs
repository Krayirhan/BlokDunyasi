using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlockPuzzle.Core.Social;

namespace BlockPuzzle.UnityAdapter.Social.UI
{
    public class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] private Text pvpStatusText;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab; // Needs script inside to map text fields

        private void OnEnable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated += UpdateList;
                LeaderboardManager.Instance.FetchWeeklyLeaderboard();
            }
        }

        private void OnDisable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated -= UpdateList;
            }
        }

        private void UpdateList(List<LeaderboardEntry> list)
        {
            foreach (Transform child in entryContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var entry in list)
            {
                GameObject obj = Instantiate(entryPrefab, entryContainer);
                Text[] texts = obj.GetComponentsInChildren<Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{entry.rank}";
                    texts[1].text = entry.playerName;
                    texts[2].text = entry.score.ToString();
                }
            }

            pvpStatusText.text = "Weekly Top Players";
        }
    }
}