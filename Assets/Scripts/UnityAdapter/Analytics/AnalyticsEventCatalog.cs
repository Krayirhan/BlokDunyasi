using System.Collections.Generic;

namespace BlockPuzzle.UnityAdapter.Analytics
{
    /// <summary>
    /// Single source of truth for canonical analytics event names.
    /// </summary>
    public static class AnalyticsEventCatalog
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            AnalyticsEventName.FirstOpen,
            AnalyticsEventName.ModeSelected,
            AnalyticsEventName.SettingsChanged,
            AnalyticsEventName.TutorialStarted,
            AnalyticsEventName.TutorialCompleted,
            AnalyticsEventName.OnboardingSpawnProfileApplied,
            AnalyticsEventName.GameOverGuidanceShown,
            AnalyticsEventName.BoardRiskHintShown,
            AnalyticsEventName.GameOverRiskSnapshotShown,
            AnalyticsEventName.MoveScored,
            AnalyticsEventName.LineCleared,
            AnalyticsEventName.ComboChanged,
            AnalyticsEventName.BestScoreUpdated,
            AnalyticsEventName.SessionSummary,
            AnalyticsEventName.MissionCompleted,
            "session_start",
            "session_end",
            "continue_offer_shown",
            "continue_clicked",
            "continue_success"
        };

        public static bool IsKnown(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            for (int i = 0; i < All.Count; i++)
            {
                if (string.Equals(All[i], eventName, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
