using UnityEngine;
using BlockPuzzle.UnityAdapter.UI;

namespace BlokDunyasiTools
{
    /// <summary>
    /// Test script to validate device layout configurations
    /// Usage: Attach to a GameObject and run in the editor or as runtime test
    /// </summary>
    public class DeviceLayoutTester : MonoBehaviour
    {
        [SerializeField] private DeviceLayoutProfile[] profilesUnderTest;
        [SerializeField] private bool runTestOnAwake = true;
        [SerializeField] private bool logDetailedResults = true;

        private struct TestResolution
        {
            public int width;
            public int height;
            public string name;
            public float diagonalDpi;
        }

        private static readonly TestResolution[] TestResolutions = new[]
        {
            new TestResolution { width = 480, height = 800, name = "Small Phone", diagonalDpi = 326f },
            new TestResolution { width = 720, height = 1280, name = "Phone Portrait", diagonalDpi = 294f },
            new TestResolution { width = 1080, height = 1920, name = "Phone FHD", diagonalDpi = 441f },
            new TestResolution { width = 1080, height = 2340, name = "Phone Modern 19.5:9", diagonalDpi = 403f },
            new TestResolution { width = 1440, height = 2560, name = "Phone 2K", diagonalDpi = 577f },
            new TestResolution { width = 1920, height = 1080, name = "Phone Landscape", diagonalDpi = 441f },
            new TestResolution { width = 1024, height = 768, name = "Tablet Small", diagonalDpi = 163f },
            new TestResolution { width = 1920, height = 1080, name = "Tablet Standard", diagonalDpi = 217f },
            new TestResolution { width = 2048, height = 1536, name = "iPad Pro", diagonalDpi = 264f },
            new TestResolution { width = 2560, height = 1440, name = "Tablet Large Landscape", diagonalDpi = 217f },
        };

        private void Awake()
        {
            if (runTestOnAwake)
                RunDeviceLayoutTests();
        }

        public void RunDeviceLayoutTests()
        {
            if (profilesUnderTest == null || profilesUnderTest.Length == 0)
            {
                Debug.LogWarning("[DeviceLayoutTester] No profiles assigned for testing!");
                return;
            }

            Debug.Log("╔════════════════════════════════════════════════════════════════");
            Debug.Log("║         DEVICE LAYOUT PROFILE TEST RESULTS");
            Debug.Log("╠════════════════════════════════════════════════════════════════");

            int profilesMatched = 0;
            int totalTests = TestResolutions.Length;

            for (int i = 0; i < TestResolutions.Length; i++)
            {
                var testRes = TestResolutions[i];
                bool matched = false;

                for (int p = 0; p < profilesUnderTest.Length; p++)
                {
                    var profile = profilesUnderTest[p];
                    if (profile == null)
                        continue;

                    float aspect = testRes.width / (float)testRes.height;
                    float diagonalInches = CalculateDiagonalInches(testRes.width, testRes.height, testRes.diagonalDpi);

                    if (profile.MatchesScreen(testRes.width, testRes.height, diagonalInches))
                    {
                        Debug.Log($"║ ✓ {testRes.name,-35} → {profile.deviceName}");
                        
                        if (logDetailedResults)
                        {
                            Debug.Log($"║   Resolution: {testRes.width}x{testRes.height} | Aspect: {aspect:F2}:1 | Diagonal: {diagonalInches:F2}\"");
                            Debug.Log($"║   Spacing: {profile.verticalSpacing}px (V) × {profile.horizontalSpacing}px (H)");
                            Debug.Log($"║   Min Scale: {profile.minScale:F2} | Max Scale: {profile.maxScale:F2}");
                        }

                        matched = true;
                        profilesMatched++;
                        break;
                    }
                }

                if (!matched)
                {
                    Debug.LogWarning($"║ ✗ {testRes.name,-35} → NO MATCH FOUND!");
                }
            }

            Debug.Log("╠════════════════════════════════════════════════════════════════");
            Debug.Log($"║ Results: {profilesMatched}/{totalTests} test resolutions matched");
            float matchPercentage = (profilesMatched / (float)totalTests) * 100f;
            Debug.Log($"║ Coverage: {matchPercentage:F1}%");
            Debug.Log("╚════════════════════════════════════════════════════════════════");

            if (matchPercentage < 100f)
                Debug.LogError("[DeviceLayoutTester] Some resolutions are not covered! Consider adding more profiles.");
        }

        public void LogProfileDetails()
        {
            if (profilesUnderTest == null || profilesUnderTest.Length == 0)
            {
                Debug.LogWarning("[DeviceLayoutTester] No profiles to display!");
                return;
            }

            Debug.Log("╔════════════════════════════════════════════════════════════════");
            Debug.Log("║              DEVICE PROFILE CONFIGURATION");
            Debug.Log("╠════════════════════════════════════════════════════════════════");

            for (int i = 0; i < profilesUnderTest.Length; i++)
            {
                var profile = profilesUnderTest[i];
                if (profile == null)
                    continue;

                Debug.Log($"║ [{i}] {profile.deviceName} ({profile.deviceType})");
                Debug.Log($"║     Screen Range: {profile.minWidth:F0}-{profile.maxWidth:F0}px (W) × {profile.minHeight:F0}-{profile.maxHeight:F0}px (H)");
                Debug.Log($"║     Aspect Ratio: {profile.minAspectRatio:F2}:1 - {profile.maxAspectRatio:F2}:1");
                Debug.Log($"║     Size: {profile.minDiagonalInches:F1}\"-{profile.maxDiagonalInches:F1}\"");
                Debug.Log($"║     Spacing: {profile.verticalSpacing}px (V) × {profile.horizontalSpacing}px (H)");
                Debug.Log($"║     Scale Range: {profile.minScale:F2} - {profile.maxScale:F2}");
                Debug.Log($"║     Hit Area Multiplier: {profile.buttonInteractionRadius:F2}x");
                Debug.Log("║");
            }

            Debug.Log("╚════════════════════════════════════════════════════════════════");
        }

        private static float CalculateDiagonalInches(int width, int height, float dpi)
        {
            float diagonalPixels = Mathf.Sqrt(width * width + height * height);
            return diagonalPixels / dpi;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("BlokDunyasi/Test/Run Device Layout Tests")]
        private static void MenuRunTests()
        {
            var testObject = FindFirstObjectByType<DeviceLayoutTester>();
            if (testObject == null)
            {
                var go = new GameObject("DeviceLayoutTester");
                testObject = go.AddComponent<DeviceLayoutTester>();
            }

            // Load all profiles
            var profiles = Resources.LoadAll<DeviceLayoutProfile>("UI/DeviceProfiles");
            testObject.profilesUnderTest = profiles;

            testObject.RunDeviceLayoutTests();
            testObject.LogProfileDetails();
        }
#endif
    }
}
