using GoogleMobileAds.Api;
using BlockPuzzle.UnityAdapter.Privacy;

/// <summary>
/// Builds AdRequest instances, marking them non-personalized when ads were
/// started without a confirmed UMP consent decision.
/// </summary>
public static class AdRequestFactory
{
    public static AdRequest Create()
    {
        var request = new AdRequest();
        if (ConsentGate.RequireNonPersonalizedAds)
        {
            request.Extras.Add("npa", "1");
        }

        return request;
    }
}
