namespace BlockPuzzle.UnityAdapter.Analytics
{
    /// <summary>
    /// Canonical analytics event names used by gameplay telemetry.
    /// </summary>
    public static class AnalyticsEventName
    {
        public const string FirstOpen = "first_open";
        public const string ModeSelected = "mode_selected";
        public const string SettingsChanged = "settings_changed";
        public const string TutorialStarted = "tutorial_started";
        public const string TutorialCompleted = "tutorial_completed";
        public const string OnboardingSpawnProfileApplied = "onboarding_spawn_profile_applied";
        public const string GameOverGuidanceShown = "gameover_guidance_shown";
        public const string BoardRiskHintShown = "board_risk_hint_shown";
        public const string GameOverRiskSnapshotShown = "gameover_risk_snapshot_shown";
        public const string MoveScored = "move_scored";
        public const string LineCleared = "line_cleared";
        public const string ComboChanged = "combo_changed";
        public const string BestScoreUpdated = "best_score_updated";
        public const string SessionSummary = "session_summary";
        public const string MissionCompleted = "mission_completed";
    }
}
