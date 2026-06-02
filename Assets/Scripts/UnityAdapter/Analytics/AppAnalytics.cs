using System;
using UnityEngine;
using BlockPuzzle.UnityAdapter.Privacy;

namespace BlockPuzzle.UnityAdapter.Analytics
{
    /// <summary>
    /// Lightweight app/UI analytics channel for non-gameplay events.
    /// Kept separate from gameplay scoring telemetry so session/logger code can evolve independently.
    /// </summary>
    public static class AppAnalytics
    {
        private const string FirstOpenRecordedKey = "analytics_first_open_recorded";

        public static event Action<AppAnalyticsEventData> EventTracked;

        public static void TrackFirstOpenIfNeeded()
        {
            if (!ConsentGate.CanCollectAnalytics)
                return;

            if (PlayerPrefs.GetInt(FirstOpenRecordedKey, 0) == 1)
                return;

            PlayerPrefs.SetInt(FirstOpenRecordedKey, 1);
            PlayerPrefs.Save();

            Track(
                AnalyticsEventName.FirstOpen,
                category: "app",
                context: "lifecycle");
        }

        public static void TrackModeSelected(string launchMode, string gameMode)
        {
            Track(
                AnalyticsEventName.ModeSelected,
                category: "app",
                context: launchMode,
                gameMode: gameMode);
        }

        public static void TrackSettingsChanged(string settingKey, string value)
        {
            Track(
                AnalyticsEventName.SettingsChanged,
                category: "ui",
                context: "settings",
                label: settingKey,
                stringValue: value);
        }

        public static void TrackTutorialStarted(string tutorialId = "default")
        {
            Track(
                AnalyticsEventName.TutorialStarted,
                category: "tutorial",
                context: tutorialId);
        }

        public static void TrackTutorialCompleted(string tutorialId = "default")
        {
            Track(
                AnalyticsEventName.TutorialCompleted,
                category: "tutorial",
                context: tutorialId);
        }

        public static void TrackOnboardingSpawnProfileApplied(int moveCount, bool tutorialActive)
        {
            Track(
                AnalyticsEventName.OnboardingSpawnProfileApplied,
                category: "tutorial",
                context: tutorialActive ? "tutorial_active" : "first_session",
                intValue: moveCount,
                boolValue: tutorialActive);
        }

        public static void TrackGameOverGuidanceShown(string guidanceCode)
        {
            Track(
                AnalyticsEventName.GameOverGuidanceShown,
                category: "ui",
                context: "game_over",
                label: guidanceCode);
        }

        public static void TrackBoardRiskHintShown(string riskCode)
        {
            Track(
                AnalyticsEventName.BoardRiskHintShown,
                category: "ui",
                context: "preview",
                label: riskCode);
        }

        public static void TrackGameOverRiskSnapshotShown(string snapshotCode)
        {
            Track(
                AnalyticsEventName.GameOverRiskSnapshotShown,
                category: "ui",
                context: "game_over",
                label: snapshotCode);
        }

        public static void Track(
            string eventName,
            string category,
            string context = "",
            string label = "",
            string gameMode = "",
            string stringValue = "",
            int intValue = 0,
            bool? boolValue = null)
        {
            if (!ConsentGate.CanCollectAnalytics)
                return;

            var payload = new AppAnalyticsEventData(
                eventName: eventName,
                category: category,
                timestampUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                context: context,
                label: label,
                gameMode: gameMode,
                stringValue: stringValue,
                intValue: intValue,
                hasBoolValue: boolValue.HasValue,
                boolValue: boolValue.GetValueOrDefault());

            EventTracked?.Invoke(payload);
        }
    }

    public readonly struct AppAnalyticsEventData
    {
        public readonly string EventName;
        public readonly string Category;
        public readonly long TimestampUnixMs;
        public readonly string Context;
        public readonly string Label;
        public readonly string GameMode;
        public readonly string StringValue;
        public readonly int IntValue;
        public readonly bool HasBoolValue;
        public readonly bool BoolValue;

        public AppAnalyticsEventData(
            string eventName,
            string category,
            long timestampUnixMs,
            string context,
            string label,
            string gameMode,
            string stringValue,
            int intValue,
            bool hasBoolValue,
            bool boolValue)
        {
            EventName = eventName ?? string.Empty;
            Category = category ?? string.Empty;
            TimestampUnixMs = timestampUnixMs;
            Context = context ?? string.Empty;
            Label = label ?? string.Empty;
            GameMode = gameMode ?? string.Empty;
            StringValue = stringValue ?? string.Empty;
            IntValue = intValue;
            HasBoolValue = hasBoolValue;
            BoolValue = boolValue;
        }
    }
}
