using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Configuration
{
    [CreateAssetMenu(fileName = "GameplayFeatureFlags", menuName = "Blok Dunyasi/Gameplay Feature Flags")]
    public class GameplayFeatureFlagsAsset : ScriptableObject
    {
        [Header("Sprint 0 Flags")]
        [SerializeField] private bool enableModes = true;
        [SerializeField] private bool enableMissions = true;
        [SerializeField] private bool enableRescueToken = true;
        [SerializeField] private bool enableExtendedTelemetry = true;
        [SerializeField] private bool enableTutorial = true;

        public bool EnableModes => enableModes;
        public bool EnableMissions => enableMissions;
        public bool EnableRescueToken => enableRescueToken;
        public bool EnableExtendedTelemetry => enableExtendedTelemetry;
        public bool EnableTutorial => enableTutorial;
    }
}
