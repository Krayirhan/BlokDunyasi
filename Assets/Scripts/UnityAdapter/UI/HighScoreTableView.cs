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
        [SerializeField] private string emptyEntryText = "-";
        [SerializeField] [Range(1, 10)] private int maxEntries = 5;

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
            for (int i = 0; i < count; i++)
            {
                var text = i < scores.Count
                    ? string.Format(entryFormat, i + 1, scores[i])
                    : emptyEntryText;

                if (entries[i] != null)
                    entries[i].text = text;
            }
        }
    }
}
