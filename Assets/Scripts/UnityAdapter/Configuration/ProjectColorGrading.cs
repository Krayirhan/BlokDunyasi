using UnityEngine;
namespace BlockPuzzle.UnityAdapter.Configuration
{
    /// <summary>
    /// Legacy compatibility shim. Color grading is disabled; Apply returns the input unchanged.
    /// </summary>
    [ExecuteAlways]
    public class ProjectColorGrading : MonoBehaviour
    {
        private static int _settingsVersion;

        public static int SettingsVersion => _settingsVersion;

        public static Color Apply(Color source)
        {
            return source;
        }

        public static void SetExternalUiAdjustments(
            float saturationMultiplier,
            float valueMultiplier,
            float contrastMultiplier,
            bool enableOverride)
        {
            MarkSettingsChangedStatic();
        }

        private void Awake()
        {
            MarkSettingsChanged();
        }

        private void OnEnable()
        {
            MarkSettingsChanged();
        }

        private void OnValidate()
        {
            MarkSettingsChanged();
        }

        private void MarkSettingsChanged()
        {
            MarkSettingsChangedStatic();
        }

        private static void MarkSettingsChangedStatic()
        {
            unchecked
            {
                _settingsVersion++;
            }
        }
    }
}
