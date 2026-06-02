using UnityEngine;
using System.Collections.Generic;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Selects appropriate DeviceLayoutProfile based on current screen metrics.
    /// Supports aspect ratio matching, fallback profiles, and debug output.
    /// </summary>
    public class DeviceProfileSelector : MonoBehaviour
    {
        [SerializeField] private DeviceLayoutProfile[] availableProfiles;
        [SerializeField] private DeviceLayoutProfile fallbackProfile;
        [SerializeField] private bool enableDebugLogging = true;

        private DeviceLayoutProfile _currentProfile;
        private float _lastScreenAspect;
        private static DeviceProfileSelector _instance;

        private void OnEnable()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Gets the profile that matches current screen metrics.
        /// Caches result and only recalculates if screen aspect changes.
        /// </summary>
        public DeviceLayoutProfile GetCurrentProfile()
        {
            float currentAspect = GetScreenAspectRatio();

            // Return cached profile if aspect hasn't changed significantly
            if (_currentProfile != null && Mathf.Abs(currentAspect - _lastScreenAspect) < 0.01f)
                return _currentProfile;

            _currentProfile = SelectProfileForAspect(currentAspect);
            _lastScreenAspect = currentAspect;

            if (enableDebugLogging)
            {
                LogProfileSelection();
            }

            return _currentProfile;
        }

        /// <summary>
        /// Finds the best matching profile for given aspect ratio.
        /// Returns fallback if no exact match found.
        /// </summary>
        private DeviceLayoutProfile SelectProfileForAspect(float aspect)
        {
            if (availableProfiles == null || availableProfiles.Length == 0)
            {
                Debug.LogWarning("[DeviceProfileSelector] No profiles available, using fallback.");
                return fallbackProfile;
            }

            // First pass: exact match
            foreach (var profile in availableProfiles)
            {
                if (aspect >= profile.minAspectRatio && aspect <= profile.maxAspectRatio)
                {
                    return profile;
                }
            }

            // Second pass: closest match (if exact not found)
            DeviceLayoutProfile closest = availableProfiles[0];
            float closestDist = Mathf.Abs(aspect - (closest.minAspectRatio + closest.maxAspectRatio) * 0.5f);

            for (int i = 1; i < availableProfiles.Length; i++)
            {
                float profileMidpoint = (availableProfiles[i].minAspectRatio + availableProfiles[i].maxAspectRatio) * 0.5f;
                float dist = Mathf.Abs(aspect - profileMidpoint);
                if (dist < closestDist)
                {
                    closest = availableProfiles[i];
                    closestDist = dist;
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[DeviceProfileSelector] Aspect {aspect:F3} not exact match, using closest: {closest.deviceName}");
            }

            return closest;
        }

        /// <summary>
        /// Calculates current screen aspect ratio.
        /// </summary>
        private float GetScreenAspectRatio()
        {
            Rect safeArea = Screen.safeArea;
            float width = Mathf.Max(1f, safeArea.width);
            float height = Mathf.Max(1f, safeArea.height);
            return width / height;
        }

        /// <summary>
        /// Logs current profile selection details.
        /// </summary>
        private void LogProfileSelection()
        {
            if (_currentProfile == null)
            {
                Debug.Log("[DeviceProfileSelector] No profile selected (fallback might be null)");
                return;
            }

            float aspect = GetScreenAspectRatio();
            string log = $"[DeviceProfileSelector] Profile Selected:\n" +
                        $"  Device: {_currentProfile.deviceName}\n" +
                        $"  Screen Aspect: {aspect:F3} ({Screen.width}x{Screen.height})\n" +
                        $"  Profile Range: {_currentProfile.minAspectRatio:F3} - {_currentProfile.maxAspectRatio:F3}\n" +
                        $"  Layout Ratios:\n" +
                        $"    Header: {_currentProfile.headerHeightRatio:F2}\n" +
                        $"    Board: {_currentProfile.boardHeightRatio:F2}\n" +
                        $"    Tray: {_currentProfile.trayHeightRatio:F2}\n" +
                        $"    Bottom: {_currentProfile.bottomHeightRatio:F2}\n" +
                        $"  Tray Block Scale: {_currentProfile.trayBlockScalePercent:F2}\n" +
                        $"  Board-Tray Gap: {_currentProfile.boardTrayGap:F2}";

            Debug.Log(log);
        }

        /// <summary>
        /// Static accessor for current profile (Singleton pattern).
        /// </summary>
        public static DeviceLayoutProfile GetProfile()
        {
            if (_instance != null)
                return _instance.GetCurrentProfile();

            Debug.LogWarning("[DeviceProfileSelector] No instance active, create one in scene.");
            return null;
        }

        /// <summary>
        /// Force profile refresh (call if screen orientation changes).
        /// </summary>
        public void RefreshProfile()
        {
            _lastScreenAspect = -1f; // Force recalculation
            GetCurrentProfile();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor validation method.
        /// </summary>
        private void OnValidate()
        {
            if (availableProfiles != null)
            {
                for (int i = 0; i < availableProfiles.Length; i++)
                {
                    if (availableProfiles[i] == null)
                    {
                        Debug.LogWarning($"[DeviceProfileSelector] Profile at index {i} is null");
                    }
                }
            }

            if (fallbackProfile == null)
            {
                Debug.LogWarning("[DeviceProfileSelector] Fallback profile is null. Assign a default profile.");
            }
        }
#endif
    }
}
