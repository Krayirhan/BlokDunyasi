// File: UnityAdapter/UI/TargetGoalSystem.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.UI.Localization;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Gameplay HUD owner for target / goal progress state and its direct progress-bar rendering.
    /// Goal increases by goalIncrement each time target is reached.
    /// HudView should trigger progress refresh but should not own the same target/progress UI elements.
    /// </summary>
    public class TargetGoalSystem : MonoBehaviour
    {
        [Header("Goal Configuration")]
        [SerializeField] [Min(1)] private int initialGoal = 500;
        [SerializeField] [Min(1)] private int goalIncrement = 500;
        [SerializeField] [Min(1)] private int weeklyGoalMultiplier = 5;
        
        [Header("UI References")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color progressBarColor = new Color(1f, 1f, 0f, 1f); // Yellow
        [SerializeField] private bool useShortFormat = true; // 510/1000 instead of 510,000/1,000,000
        
        // State
        private int _currentGoal = 500;
        private int _totalScoreInSession = 0;
        private int _progressInCurrentGoal = 0;
        private int _lastRecordedScore;
        private bool _weeklyCompletedInSession;

        public int CurrentGoal => _currentGoal;
        public int CurrentProgress => _progressInCurrentGoal;
        public int CurrentGoalProgress => _progressInCurrentGoal;
        public int TotalScoreInSession => _totalScoreInSession;
        public int WeeklyGoal => initialGoal * Mathf.Max(1, weeklyGoalMultiplier);

        private void Start()
        {
            _currentGoal = initialGoal;
            _progressInCurrentGoal = 0;
            _totalScoreInSession = 0;
            _lastRecordedScore = 0;
            _weeklyCompletedInSession = false;
            UpdateDisplay();
        }

        /// <summary>
        /// Update progress based on current session score.
        /// Called from HudView when score changes.
        /// </summary>
        public MissionProgressResult UpdateProgress(int currentSessionScore)
        {
            int previousScore = _lastRecordedScore;
            _totalScoreInSession = Mathf.Max(0, currentSessionScore);
            bool dailyCompleted = false;
            bool weeklyCompleted = false;
            int progressDelta = Mathf.Max(0, _totalScoreInSession - previousScore);

            if (_totalScoreInSession < _lastRecordedScore)
            {
                _currentGoal = initialGoal;
                _weeklyCompletedInSession = false;
            }

            while (_totalScoreInSession >= _currentGoal)
            {
                dailyCompleted = true;
                CompleteGoal();
            }

            _progressInCurrentGoal = Mathf.Clamp(_totalScoreInSession, 0, _currentGoal);
            if (!_weeklyCompletedInSession && _totalScoreInSession >= WeeklyGoal)
            {
                weeklyCompleted = true;
                _weeklyCompletedInSession = true;
            }

            _lastRecordedScore = _totalScoreInSession;

            UpdateDisplay();
            return new MissionProgressResult(progressDelta, dailyCompleted, weeklyCompleted);
        }

        private void CompleteGoal()
        {
            _currentGoal += goalIncrement;
            _progressInCurrentGoal = 0;

            Debug.Log($"[TargetGoalSystem] Goal completed! New goal: {_currentGoal}");
        }

        /// <summary>
        /// Reset the entire system (e.g., on new game).
        /// </summary>
        public void Reset()
        {
            _currentGoal = initialGoal;
            _progressInCurrentGoal = 0;
            _totalScoreInSession = 0;
            _lastRecordedScore = 0;
            _weeklyCompletedInSession = false;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            // Update progress bar
            if (progressBar != null)
            {
                int requiredScoreForCurrentGoal = Mathf.Max(1, _currentGoal);
                float fillAmount = Mathf.Clamp01((float)_progressInCurrentGoal / requiredScoreForCurrentGoal);
                progressBar.fillAmount = fillAmount;
                progressBar.color = progressBarColor;
            }

            // Update target text (goal info)
            if (targetText != null)
            {
                targetText.text =
                    $"{TrEn("Gunluk Hedef", "Daily Goal")}: {_currentGoal:N0}  |  " +
                    $"{TrEn("Haftalik", "Weekly")}: {WeeklyGoal:N0}";
            }

            // Update progress text with short format
            if (progressText != null)
            {
                int requiredScoreForCurrentGoal = Mathf.Max(1, _currentGoal);
                progressText.text = useShortFormat
                    ? $"{_progressInCurrentGoal}/{requiredScoreForCurrentGoal}"
                    : $"{_progressInCurrentGoal:N0}/{requiredScoreForCurrentGoal:N0}";
            }
        }

        private static LanguageManager.Language GetCurrentLanguage()
        {
            if (!Application.isPlaying)
            {
                return (LanguageManager.Language)PlayerPrefs.GetInt(SettingsKeys.SelectedLanguage, (int)LanguageManager.Language.Turkish);
            }
            return LanguageManager.Instance.CurrentLanguage;
        }

        private static readonly Dictionary<string, string> KoreanTranslations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Gunluk Hedef", "일일 목표" },
            { "Daily Goal", "일일 목표" },
            { "Haftalik", "주간 목표" },
            { "Weekly", "주간 목표" }
        };

        private static string TrEn(string turkish, string english)
        {
            var lang = GetCurrentLanguage();
            if (lang == LanguageManager.Language.Korean)
            {
                if (!string.IsNullOrEmpty(english) && KoreanTranslations.TryGetValue(english, out string koTranslation))
                    return koTranslation;
                if (!string.IsNullOrEmpty(turkish) && KoreanTranslations.TryGetValue(turkish, out string koTranslation2))
                    return koTranslation2;
                return !string.IsNullOrEmpty(english) ? english : turkish;
            }
            return lang == LanguageManager.Language.English ? english : turkish;
        }

        public readonly struct MissionProgressResult
        {
            public readonly int ProgressDelta;
            public readonly bool DailyCompleted;
            public readonly bool WeeklyCompleted;

            public MissionProgressResult(int progressDelta, bool dailyCompleted, bool weeklyCompleted)
            {
                ProgressDelta = progressDelta;
                DailyCompleted = dailyCompleted;
                WeeklyCompleted = weeklyCompleted;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (initialGoal < 1) initialGoal = 1;
            if (goalIncrement < 1) goalIncrement = 1;
        }
#endif
    }
}
