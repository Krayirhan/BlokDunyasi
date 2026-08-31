using UnityEditor;
using UnityEngine;

public static class AdMobBuildValidator
{
    private const string ConfigAssetPath = "Assets/Resources/AdMobRuntimeConfig.asset";
    private const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

    [MenuItem("Blok Dunyasi/Ads/Validate Configuration")]
    public static void ValidateConfiguration()
    {
        var config = AssetDatabase.LoadAssetAtPath<AdMobRuntimeConfig>(ConfigAssetPath);
        if (config == null)
        {
            Debug.LogError("[AdMobBuildValidator] AdMobRuntimeConfig asset is missing.");
            return;
        }

        bool valid = true;
        if (!config.HasProductionAndroidAdUnitIds)
        {
            Debug.LogError("[AdMobBuildValidator] Android production ad unit IDs are incomplete.", config);
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(config.name))
        {
            Debug.LogError("[AdMobBuildValidator] Runtime config has no asset name.", config);
            valid = false;
        }

        TextAsset manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(AndroidManifestPath);
        if (manifest == null || !manifest.text.Contains("com.google.android.gms.ads.APPLICATION_ID"))
        {
            Debug.LogError("[AdMobBuildValidator] Android manifest has no AdMob APPLICATION_ID metadata.");
            valid = false;
        }

        if (manifest == null || !manifest.text.Contains("com.google.android.gms.permission.AD_ID"))
        {
            Debug.LogError("[AdMobBuildValidator] Android manifest has no AD_ID permission.");
            valid = false;
        }

        if (manifest == null || !manifest.text.Contains("android.permission.INTERNET"))
        {
            Debug.LogError("[AdMobBuildValidator] Android manifest has no INTERNET permission.");
            valid = false;
        }

        if (valid)
            Debug.Log("[AdMobBuildValidator] AdMob configuration passed static validation.");
    }

    [MenuItem("Blok Dunyasi/Ads/Reset Local Test State")]
    public static void ResetLocalTestState()
    {
        var manager = Object.FindFirstObjectByType<AdMobManager>();
        if (manager == null)
        {
            Debug.LogWarning("[AdMobBuildValidator] AdMobManager is not running. Start Play Mode first.");
            return;
        }

        manager.ResetAdTestingState();
    }
}
