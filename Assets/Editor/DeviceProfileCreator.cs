#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BlockPuzzle.UnityAdapter.UI;
using System.IO;

/// <summary>
/// Editor helper to create default DeviceLayoutProfile assets.
/// Generates 8 profile SO files in Assets/Resources/DeviceProfiles/
/// </summary>
public class DeviceProfileCreator
{
    private const string PROFILE_FOLDER = "Assets/Resources/DeviceProfiles";

    [MenuItem("BlockPuzzle/Profiles/Create Default Profiles")]
    public static void CreateDefaultProfiles()
    {
        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(PROFILE_FOLDER))
        {
            string guid = AssetDatabase.CreateFolder("Assets/Resources", "DeviceProfiles");
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError("[DeviceProfileCreator] Failed to create DeviceProfiles folder");
                return;
            }
        }

        int count = 0;

        // SmallPhone 9:16 (iPhone SE)
        count += CreateProfile("SmallPhone_9_16",
            minAspect: 0.55f, maxAspect: 0.57f,
            headerRatio: 0.16f, boardRatio: 0.54f, trayRatio: 0.14f, bottomRatio: 0.16f,
            trayBlockScale: 0.65f, boardTrayGap: 0.3f) ? 1 : 0;

        // Phone 9:18 (iPhone 12 standard)
        count += CreateProfile("Phone_9_18",
            minAspect: 0.495f, maxAspect: 0.515f,
            headerRatio: 0.15f, boardRatio: 0.56f, trayRatio: 0.14f, bottomRatio: 0.15f,
            trayBlockScale: 0.68f, boardTrayGap: 0.35f) ? 1 : 0;

        // Phone 9:19.5 (iPhone 13/14 standard)
        count += CreateProfile("Phone_9_19_5",
            minAspect: 0.45f, maxAspect: 0.465f,
            headerRatio: 0.14f, boardRatio: 0.58f, trayRatio: 0.14f, bottomRatio: 0.14f,
            trayBlockScale: 0.70f, boardTrayGap: 0.38f) ? 1 : 0;

        // Phone 9:20 (iPhone 14 Pro / OnePlus 11)
        count += CreateProfile("Phone_9_20",
            minAspect: 0.44f, maxAspect: 0.46f,
            headerRatio: 0.13f, boardRatio: 0.60f, trayRatio: 0.13f, bottomRatio: 0.14f,
            trayBlockScale: 0.72f, boardTrayGap: 0.40f) ? 1 : 0;

        // PhoneTall 9:21 (Samsung S24 Ultra)
        count += CreateProfile("PhoneTall_9_21",
            minAspect: 0.42f, maxAspect: 0.44f,
            headerRatio: 0.12f, boardRatio: 0.62f, trayRatio: 0.12f, bottomRatio: 0.14f,
            trayBlockScale: 0.75f, boardTrayGap: 0.42f) ? 1 : 0;

        // Tablet 4:3 (iPad mini)
        count += CreateProfile("Tablet_4_3",
            minAspect: 0.73f, maxAspect: 0.77f,
            headerRatio: 0.12f, boardRatio: 0.62f, trayRatio: 0.12f, bottomRatio: 0.14f,
            trayBlockScale: 0.70f, boardTrayGap: 0.45f) ? 1 : 0;

        // Tablet 3:2 (iPad Air)
        count += CreateProfile("Tablet_3_2",
            minAspect: 0.62f, maxAspect: 0.70f,
            headerRatio: 0.12f, boardRatio: 0.62f, trayRatio: 0.12f, bottomRatio: 0.14f,
            trayBlockScale: 0.70f, boardTrayGap: 0.45f) ? 1 : 0;

        // GenericFallback (catch-all)
        count += CreateProfile("GenericFallback",
            minAspect: 0.3f, maxAspect: 2.5f,
            headerRatio: 0.14f, boardRatio: 0.58f, trayRatio: 0.14f, bottomRatio: 0.14f,
            trayBlockScale: 0.70f, boardTrayGap: 0.40f) ? 1 : 0;

        Debug.Log($"[DeviceProfileCreator] Successfully created {count} profile(s) in {PROFILE_FOLDER}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool CreateProfile(string profileName, float minAspect, float maxAspect,
        float headerRatio, float boardRatio, float trayRatio, float bottomRatio,
        float trayBlockScale, float boardTrayGap)
    {
        string filename = $"{PROFILE_FOLDER}/{profileName}.asset";

        // Skip if already exists
        if (File.Exists(filename))
        {
            Debug.Log($"[DeviceProfileCreator] Profile '{profileName}' already exists, skipping.");
            return false;
        }

        var profile = ScriptableObject.CreateInstance<DeviceLayoutProfile>();

        // Set basic info
        profile.deviceName = profileName;
        profile.minAspectRatio = minAspect;
        profile.maxAspectRatio = maxAspect;

        // Set layout ratios
        profile.headerHeightRatio = headerRatio;
        profile.boardHeightRatio = boardRatio;
        profile.trayHeightRatio = trayRatio;
        profile.bottomHeightRatio = bottomRatio;

        // Set spacing
        profile.trayBlockScalePercent = trayBlockScale;
        profile.boardTrayGap = boardTrayGap;
        profile.sidePadding = 0.4f;

        // Set defaults for other fields
        profile.verticalSpacing = 12f;
        profile.horizontalSpacing = 16f;
        profile.minScale = 0.65f;
        profile.maxScale = 1f;

        // Save asset
        AssetDatabase.CreateAsset(profile, filename);
        Debug.Log($"[DeviceProfileCreator] Created profile: {profileName}");

        return true;
    }

    [MenuItem("BlockPuzzle/Profiles/Delete All Profiles")]
    public static void DeleteAllProfiles()
    {
        if (!AssetDatabase.IsValidFolder(PROFILE_FOLDER))
        {
            Debug.Log("[DeviceProfileCreator] Profile folder does not exist.");
            return;
        }

        AssetDatabase.DeleteAsset(PROFILE_FOLDER);
        Debug.Log("[DeviceProfileCreator] Deleted all profiles.");
        AssetDatabase.Refresh();
    }

    [MenuItem("BlockPuzzle/Profiles/Log Existing Profiles")]
    public static void LogExistingProfiles()
    {
        var profiles = Resources.LoadAll<DeviceLayoutProfile>("DeviceProfiles");
        Debug.Log($"[DeviceProfileCreator] Found {profiles.Length} profile(s):");

        foreach (var profile in profiles)
        {
            Debug.Log($"  • {profile.deviceName}: {profile.minAspectRatio:F3} - {profile.maxAspectRatio:F3}");
        }
    }
}
#endif
