using UnityEngine;
using System.Collections.Generic;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Initializes DeviceProfileSelector with available profiles at startup.
    /// Should be called early in game initialization (e.g., from GameBootstrap or dedicated manager).
    /// </summary>
    public class DeviceProfileInitializer
    {
        private static DeviceProfileSelector _selector;

        /// <summary>
        /// Initialize profile system. Call this early in game startup.
        /// </summary>
        public static void InitializeProfiles()
        {
            // Check if selector already exists
            _selector = Object.FindFirstObjectByType<DeviceProfileSelector>();

            if (_selector != null)
            {
                Debug.Log("[DeviceProfileInitializer] DeviceProfileSelector already exists in scene.");
                return;
            }

            // Create new selector
            GameObject selectorGO = new GameObject("DeviceProfileSelector");
            _selector = selectorGO.AddComponent<DeviceProfileSelector>();

            // Load profiles from Resources
            var profiles = Resources.LoadAll<DeviceLayoutProfile>("DeviceProfiles");
            if (profiles == null || profiles.Length == 0)
            {
                Debug.LogError("[DeviceProfileInitializer] No profiles found in Resources/DeviceProfiles. " +
                    "Use menu: BlockPuzzle/Profiles/Create Default Profiles");
                return;
            }

            // Sort profiles by aspect ratio for efficient matching (ascending)
            System.Array.Sort(profiles, (a, b) => a.minAspectRatio.CompareTo(b.minAspectRatio));

            // Set profiles on selector using reflection (since we can't access public setters directly)
            SetProfilesOnSelector(_selector, profiles);

            // Find fallback profile (GenericFallback should be included)
            var fallbackProfile = FindProfileByName(profiles, "GenericFallback");
            if (fallbackProfile != null)
            {
                SetFallbackOnSelector(_selector, fallbackProfile);
                Debug.Log("[DeviceProfileInitializer] Fallback profile set to GenericFallback");
            }

            Debug.Log($"[DeviceProfileInitializer] Initialized with {profiles.Length} profile(s)");

            // Get current profile (triggers debug logging)
            _selector.GetCurrentProfile();

            // Don't destroy on load so it persists across scenes
            Object.DontDestroyOnLoad(selectorGO);
        }

        /// <summary>
        /// Get currently selected profile.
        /// </summary>
        public static DeviceLayoutProfile GetCurrentProfile()
        {
            if (_selector == null)
            {
                _selector = Object.FindFirstObjectByType<DeviceProfileSelector>();
            }

            return _selector != null ? _selector.GetCurrentProfile() : null;
        }

        /// <summary>
        /// Alternative: Get profile by name.
        /// </summary>
        public static DeviceLayoutProfile GetProfileByName(string name)
        {
            var profiles = Resources.LoadAll<DeviceLayoutProfile>("DeviceProfiles");
            return FindProfileByName(profiles, name);
        }

        private static DeviceLayoutProfile FindProfileByName(DeviceLayoutProfile[] profiles, string name)
        {
            foreach (var profile in profiles)
            {
                if (profile.deviceName == name)
                    return profile;
            }
            return null;
        }

        private static void SetProfilesOnSelector(DeviceProfileSelector selector, DeviceLayoutProfile[] profiles)
        {
            // Use reflection to set private availableProfiles field
            var field = typeof(DeviceProfileSelector).GetField("availableProfiles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(selector, profiles);
            }
        }

        private static void SetFallbackOnSelector(DeviceProfileSelector selector, DeviceLayoutProfile fallback)
        {
            // Use reflection to set private fallbackProfile field
            var field = typeof(DeviceProfileSelector).GetField("fallbackProfile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(selector, fallback);
            }
        }
    }
}
