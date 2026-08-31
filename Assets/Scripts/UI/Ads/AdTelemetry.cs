using GoogleMobileAds.Api;
using System;

/// <summary>
/// Reklam lifecycle ve gelir eventlerini merkezi olarak dagitir.
/// </summary>
public static class AdTelemetry
{
    public static event Action<AdTelemetryRecord> OnEvent;

    public static void DispatchLifecycleEvent(string adFormat, string placement, string eventName, string rewardType = "", double rewardAmount = 0d)
    {
        Publish(new AdTelemetryRecord
        {
            Category = "ad_lifecycle",
            EventName = eventName,
            AdFormat = adFormat,
            Placement = placement,
            RewardType = rewardType,
            RewardAmount = rewardAmount,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public static void DispatchPaidEvent(string adFormat, string placement, AdValue adValue, string adSource)
    {
        Publish(new AdTelemetryRecord
        {
            Category = "ad_revenue",
            EventName = "paid",
            AdFormat = adFormat,
            Placement = placement,
            AdSource = adSource ?? string.Empty,
            RevenueValueMicros = adValue != null ? adValue.Value : 0L,
            CurrencyCode = adValue?.CurrencyCode ?? string.Empty,
            Precision = adValue != null ? adValue.Precision.ToString() : string.Empty,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public static void DispatchLoadFailed(string adFormat, string placement, string errorMessage)
    {
        Publish(new AdTelemetryRecord
        {
            Category = "ad_lifecycle",
            EventName = "load_failed",
            AdFormat = adFormat ?? string.Empty,
            Placement = placement ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    private static void Publish(AdTelemetryRecord record)
    {
        OnEvent?.Invoke(record);
    }
}

[Serializable]
public struct AdTelemetryRecord
{
    public string Category;
    public string EventName;
    public string AdFormat;
    public string Placement;
    public string AdSource;
    public long RevenueValueMicros;
    public string CurrencyCode;
    public string Precision;
    public string ErrorMessage;
    public string RewardType;
    public double RewardAmount;
    public long TimestampUnixMs;
}
