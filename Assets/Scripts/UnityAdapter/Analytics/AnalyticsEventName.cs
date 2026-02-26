namespace BlockPuzzle.UnityAdapter.Analytics
{
    /// <summary>
    /// Canonical analytics event names used by gameplay telemetry.
    /// </summary>
    public static class AnalyticsEventName
    {
        public const string MoveScored = "move_scored";
        public const string LineCleared = "line_cleared";
        public const string ComboChanged = "combo_changed";
        public const string BestScoreUpdated = "best_score_updated";
    }
}
