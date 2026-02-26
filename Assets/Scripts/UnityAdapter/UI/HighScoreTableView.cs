using UnityEngine;
using TMPro;
using BlockPuzzle.UnityAdapter;
using BlockPuzzle.Core.Persistence;

namespace BlockPuzzle.UnityAdapter.UI
{
    public class HighScoreTableView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI[] entries;
        [SerializeField] private string entryFormat = "{0}. {1}";
        [SerializeField] private bool showRankPrefix = false;
        [SerializeField] private string emptyEntryText = "-";
        [SerializeField] [Range(1, 10)] private int maxEntries = 5;
        [SerializeField] private bool centerSingleEntry = true;
        [SerializeField] private float singleEntryAnchoredX = 0f;

        private UnityPlayerPrefsDataProvider _dataProvider;

        private void Awake()
        {
            _dataProvider = new UnityPlayerPrefsDataProvider();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (entries == null || entries.Length == 0 || _dataProvider == null)
                return;

            var stats = _dataProvider.LoadStatisticsAsync().GetAwaiter().GetResult() ?? GameStatistics.CreateDefault();
            var scores = stats.GetTopScores(maxEntries);

            int count = Mathf.Min(maxEntries, entries.Length);
            bool singleEntryMode = count == 1;
            for (int i = 0; i < count; i++)
            {
                string text;
                if (i < scores.Count)
                {
                    text = showRankPrefix
                        ? string.Format(entryFormat, i + 1, scores[i])
                        : scores[i].ToString();
                }
                else
                {
                    text = emptyEntryText;
                }

                if (entries[i] != null)
                {
                    if (singleEntryMode && centerSingleEntry)
                    {
                        var rect = entries[i].rectTransform;
                        var anchored = rect.anchoredPosition;
                        anchored.x = singleEntryAnchoredX;
                        rect.anchoredPosition = anchored;
                        entries[i].alignment = TextAlignmentOptions.Center;
                    }

                    entries[i].text = text;
                }
            }
        }
    }
}
