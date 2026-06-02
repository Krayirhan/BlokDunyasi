using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.Boot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using GameplayBootstrap = BlockPuzzle.UnityAdapter.Boot.GameBootstrap;
using Debug = BlockPuzzle.Core.Common.GameLogger;
using BlockPuzzle.UnityAdapter.Privacy;

/// <summary>
/// Oyun ici gameplay ve reklam eventlerini JSONL olarak cihaz depolamasina yazar.
/// Disa acik analytics hattina gecene kadar tanilama ve tuning icin kullanilir.
/// </summary>
public class AnalyticsEventFileLogger : MonoBehaviour
{
    private const bool EnableFileLoggingInStoreBuilds = false;
    private static AnalyticsEventFileLogger _instance;
    private string _sessionId;
    private string _filePath;
    private bool _hookedAdEvents;
    private readonly List<string> _pendingLines = new List<string>(32);
    private bool _analyticsFileLoggingEnabled = true;
    private bool _consentGranted;
    private float _nextFlushTime;
    private float _nextAdHookRetryTime;
    private const float FlushIntervalSeconds = 1.5f;
    private const int ImmediateFlushBatchSize = 12;
    private const float AdHookRetryIntervalSeconds = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null)
            return;

        GameObject loggerGO = new GameObject("AnalyticsEventFileLogger");
        _instance = loggerGO.AddComponent<AnalyticsEventFileLogger>();
        DontDestroyOnLoad(loggerGO);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _sessionId = Guid.NewGuid().ToString("N");
        _filePath = Path.Combine(Application.persistentDataPath, "analytics_events.jsonl");
        var config = Resources.Load<AdMobRuntimeConfig>(AdMobRuntimeConfig.ResourcesPath);
        _analyticsFileLoggingEnabled = IsFileLoggingAllowedByBuild()
            && (config == null || config.EnableAnalyticsFileLogging);
        _consentGranted = ConsentGate.CanCollectAnalytics;
    }

    private void OnEnable()
    {
        GameplayBootstrap.OnAnalyticsEvent += HandleGameplayAnalyticsEvent;
        GameplayBootstrap.OnGameOver += HandleGameOverTelemetry;
        AppAnalytics.EventTracked += HandleAppAnalyticsEvent;
        AdTelemetry.OnEvent += HandleAdTelemetryEvent;
        ConsentGate.ConsentStateChanged += HandleConsentStateChanged;
        HookAdMobManagerEventsIfNeeded();
        if (_consentGranted)
            AppAnalytics.TrackFirstOpenIfNeeded();
        AppendRecord(new AnalyticsLogRecord
        {
            Category = "session",
            EventName = "session_start",
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    private void OnDisable()
    {
        GameplayBootstrap.OnAnalyticsEvent -= HandleGameplayAnalyticsEvent;
        GameplayBootstrap.OnGameOver -= HandleGameOverTelemetry;
        AppAnalytics.EventTracked -= HandleAppAnalyticsEvent;
        AdTelemetry.OnEvent -= HandleAdTelemetryEvent;
        ConsentGate.ConsentStateChanged -= HandleConsentStateChanged;
        FlushPendingRecords();
    }

    private void OnApplicationQuit()
    {
        AppendRecord(new AnalyticsLogRecord
        {
            Category = "session",
            EventName = "session_end",
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        FlushPendingRecords();
    }

    private void Update()
    {
        if (!_hookedAdEvents && Time.unscaledTime >= _nextAdHookRetryTime)
        {
            _nextAdHookRetryTime = Time.unscaledTime + AdHookRetryIntervalSeconds;
            HookAdMobManagerEventsIfNeeded();
        }

        if (!_analyticsFileLoggingEnabled || !_consentGranted || _pendingLines.Count == 0 || Time.unscaledTime < _nextFlushTime)
            return;

        FlushPendingRecords();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            FlushPendingRecords();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            FlushPendingRecords();
    }

    private void HookAdMobManagerEventsIfNeeded()
    {
        if (_hookedAdEvents)
            return;

        var manager = FindFirstObjectByType<AdMobManager>();
        if (manager == null)
            return;

        manager.OnBannerLoaded += () => AppendSimpleAdLifecycle("banner_loaded", "banner", "menu_banner");
        manager.OnBannerFailedToLoad += error => AppendSimpleAdLifecycle("banner_failed", "banner", "menu_banner", error);
        manager.OnInterstitialLoaded += () => AppendSimpleAdLifecycle("interstitial_loaded", "interstitial", "gameover_interstitial");
        manager.OnInterstitialFailedToLoad += error => AppendSimpleAdLifecycle("interstitial_failed", "interstitial", "gameover_interstitial", error);
        manager.OnRewardedAdLoaded += () => AppendSimpleAdLifecycle("rewarded_loaded", "rewarded", "continue_rewarded");
        manager.OnRewardedAdFailedToLoad += error => AppendSimpleAdLifecycle("rewarded_failed", "rewarded", "continue_rewarded", error);

        _hookedAdEvents = true;
    }

    private void HandleGameplayAnalyticsEvent(AnalyticsEventData payload)
    {
        AppendRecord(new AnalyticsLogRecord
        {
            Category = "gameplay",
            EventName = payload.EventName,
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = payload.TimestampUnixMs,
            SchemaVersion = payload.SchemaVersion,
            ScoreFormulaVersion = payload.ScoreFormulaVersion,
            SessionMoveCount = payload.SessionMoveCount,
            TotalScore = payload.TotalScore,
            ScoreDelta = payload.ScoreDelta,
            LinesCleared = payload.LinesCleared,
            ComboBefore = payload.ComboBefore,
            ComboAfter = payload.ComboAfter,
            BestScore = payload.BestScore,
            IsNewBest = payload.IsNewBest,
            IsScoreAnomaly = payload.IsScoreAnomaly,
            ScoreAnomalyCode = payload.ScoreAnomalyCode,
            GameMode = payload.GameMode,
            DailyMissionCompletions = payload.DailyMissionCompletions,
            WeeklyMissionCompletions = payload.WeeklyMissionCompletions
        });
    }

    private void HandleGameOverTelemetry(int finalScore)
    {
        var bootstrap = FindFirstObjectByType<GameplayBootstrap>();
        var state = bootstrap != null ? bootstrap.CurrentState : null;
        AppendRecord(new AnalyticsLogRecord
        {
            Category = "gameplay",
            EventName = AnalyticsEventName.SessionSummary,
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TotalScore = finalScore,
            SessionMoveCount = state?.MoveCount ?? 0,
            LinesCleared = state?.TotalLinesCleared ?? 0,
            ComboAfter = state?.Combo ?? 0
        });
    }

    private void HandleAppAnalyticsEvent(AppAnalyticsEventData payload)
    {
        AppendRecord(new AnalyticsLogRecord
        {
            Category = payload.Category,
            EventName = payload.EventName,
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = payload.TimestampUnixMs,
            Context = payload.Context,
            Label = payload.Label,
            GameMode = payload.GameMode,
            StringValue = payload.StringValue,
            IntValue = payload.IntValue,
            HasBoolValue = payload.HasBoolValue,
            BoolValue = payload.BoolValue
        });
    }

    private void HandleConsentStateChanged(ConsentState state)
    {
        _consentGranted = state == ConsentState.Accepted;
        if (_consentGranted)
            AppAnalytics.TrackFirstOpenIfNeeded();
    }

    private void HandleAdTelemetryEvent(AdTelemetryRecord record)
    {
        AppendRecord(new AnalyticsLogRecord
        {
            Category = record.Category,
            EventName = record.EventName,
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = record.TimestampUnixMs,
            AdFormat = record.AdFormat,
            Placement = record.Placement,
            AdSource = record.AdSource,
            RevenueValueMicros = record.RevenueValueMicros,
            CurrencyCode = record.CurrencyCode,
            Precision = record.Precision,
            RewardType = record.RewardType,
            RewardAmount = record.RewardAmount
        });
    }

    private void AppendSimpleAdLifecycle(string eventName, string adFormat, string placement, string error = "")
    {
        AppendRecord(new AnalyticsLogRecord
        {
            Category = "ad_lifecycle",
            EventName = eventName,
            SessionId = _sessionId,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AdFormat = adFormat,
            Placement = placement,
            ErrorMessage = error
        });
    }

    private void AppendRecord(AnalyticsLogRecord record)
    {
        if (!_analyticsFileLoggingEnabled)
            return;

        if (!_consentGranted)
            return;

        try
        {
            _pendingLines.Add(JsonUtility.ToJson(record));
            if (_pendingLines.Count >= ImmediateFlushBatchSize)
            {
                FlushPendingRecords();
            }
            else
            {
                _nextFlushTime = Time.unscaledTime + FlushIntervalSeconds;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AnalyticsEventFileLogger] Event write failed: {ex.Message}");
        }
    }

    private void FlushPendingRecords()
    {
        if (!_analyticsFileLoggingEnabled || _pendingLines.Count == 0)
            return;

        try
        {
            var builder = new StringBuilder(_pendingLines.Count * 160);
            for (int i = 0; i < _pendingLines.Count; i++)
                builder.AppendLine(_pendingLines[i]);

            File.AppendAllText(_filePath, builder.ToString());
            _pendingLines.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AnalyticsEventFileLogger] Event flush failed: {ex.Message}");
        }
    }

    private static bool IsFileLoggingAllowedByBuild()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return EnableFileLoggingInStoreBuilds;
#endif
    }
}

[Serializable]
public class AnalyticsLogRecord
{
    public string Category;
    public string EventName;
    public string SessionId;
    public string SceneName;
    public long TimestampUnixMs;
    public int SchemaVersion;
    public int ScoreFormulaVersion;
    public int SessionMoveCount;
    public int TotalScore;
    public int ScoreDelta;
    public int LinesCleared;
    public int ComboBefore;
    public int ComboAfter;
    public int BestScore;
    public bool IsNewBest;
    public bool IsScoreAnomaly;
    public string ScoreAnomalyCode;
    public string AdFormat;
    public string Placement;
    public string AdSource;
    public long RevenueValueMicros;
    public string CurrencyCode;
    public string Precision;
    public string RewardType;
    public double RewardAmount;
    public string ErrorMessage;
    public string GameMode;
    public int DailyMissionCompletions;
    public int WeeklyMissionCompletions;
    public string Context;
    public string Label;
    public string StringValue;
    public int IntValue;
    public bool HasBoolValue;
    public bool BoolValue;
}
