using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Device-specific layout configuration for responsive UI
    /// Stores spacing, padding, and scale values for different device types
    /// </summary>
    [CreateAssetMenu(fileName = "DeviceLayoutProfile", menuName = "BlockPuzzle/UI/Device Layout Profile")]
    public class DeviceLayoutProfile : ScriptableObject
    {
        public enum DeviceType
        {
            SmallPhone,      // 480x800 - 4.5" and below
            PhonePortrait,   // 720x1280 - 5" standard
            PhoneFHD,        // 1080x1920 - 5.5" flagship
            PhoneModern,     // 1080x2340+ - 19.5:9 ratio
            PhoneLandscape,  // Any phone in landscape
            TabletSmall,     // 1024x768 - iPad mini
            TabletStandard,  // 1920x1080 - standard tablet
            TabletLarge      // 2048x1536+ - iPad Pro, large tablets
        }

        [SerializeField] public DeviceType deviceType;
        [SerializeField] public string deviceName = "Generic";

        [Header("Screen Detection")]
        [SerializeField] public float minWidth = 480f;
        [SerializeField] public float maxWidth = 3000f;
        [SerializeField] public float minHeight = 800f;
        [SerializeField] public float maxHeight = 3000f;
        [SerializeField] public float minAspectRatio = 0.5f;
        [SerializeField] public float maxAspectRatio = 2.5f;
        [SerializeField] public float minDiagonalInches = 0f;
        [SerializeField] public float maxDiagonalInches = 15f;

        [Header("Spacing & Padding")]
        [SerializeField] public float verticalSpacing = 12f;
        [SerializeField] public float horizontalSpacing = 16f;
        [SerializeField] public float logoSlotPaddingH = 12f;
        [SerializeField] public float logoSlotPaddingV = 12f;
        [SerializeField] public float badgeSlotPaddingH = 8f;
        [SerializeField] public float badgeSlotPaddingV = 8f;
        [SerializeField] public float playSlotPaddingH = 16f;
        [SerializeField] public float playSlotPaddingV = 16f;
        [SerializeField] public float authSlotPaddingH = 8f;
        [SerializeField] public float authSlotPaddingV = 8f;

        [Header("Padding Formula")]
        [SerializeField] public float horizontalPaddingMultiplier = 0.05f;
        [SerializeField] public float topPaddingMultiplier = 0.035f;
        [SerializeField] public float bottomPaddingMultiplier = 0.04f;
        [SerializeField] public float minHorizontalPadding = 18f;
        [SerializeField] public float maxHorizontalPadding = 42f;
        [SerializeField] public float minTopPadding = 18f;
        [SerializeField] public float maxTopPadding = 36f;
        [SerializeField] public float minBottomPadding = 20f;
        [SerializeField] public float maxBottomPadding = 42f;

        [Header("Scale Settings")]
        [SerializeField] public float minScale = 0.65f;
        [SerializeField] public float maxScale = 1f;
        [SerializeField] public bool enableAutoScale = true;

        [Header("Game Screen Layout (Responsive Heights) - Vertical Space Distribution")]
        [SerializeField] public float headerHeightRatio = 0.15f; 
        // ▲ Header area ratio (safe area UI: score, home, settings, trophy)
        // Typical: 0.12-0.18 (12-18% of total screen height)
        // Adjusts Inspector: safe area top padding + logo + badge area
        
        [SerializeField] public float boardHeightRatio = 0.55f;  
        // ▲ Board/grid ratio (playable game area in world space)
        // Typical: 0.50-0.65 (50-65% of total screen height)
        // Expands on tablets, shrinks on ultra-wide phones
        
        [SerializeField] public float trayHeightRatio = 0.15f;   
        // ▲ Tray ratio (block selection area in world space)
        // Typical: 0.12-0.20 (12-20% of total screen height)
        // Must fit 3 draggable blocks + padding
        
        [SerializeField] public float bottomHeightRatio = 0.15f; 
        // ▲ Bottom area ratio (safe area UI: progress and reward bars)
        // Typical: 0.12-0.18 (12-18% of total screen height)
        // Reserves space for gesture bar + safe area bottom
        
        [SerializeField] public float trayBlockScalePercent = 0.7f; 
        // ▲ Tray block size as percentage of board cell size
        // Range: 0.6-0.8 (blocks 60-80% of grid cell)
        // Prevents tray blocks appearing too large or small relative to grid

        [Header("Game Screen Spacing - World Space (Units)")]
        [SerializeField] public float boardTrayGap = 0.4f; 
        // ▲ Gap between board bottom and tray top (world units)
        // Prevents visual overlap and text clipping
        // Typical: 0.3-0.6 (should scale with board size)
        
        [SerializeField] public float sidePadding = 0.4f; 
        // ▲ Left/right padding around board in world space
        // Prevents edge blocks hitting camera bounds
        // Typical: 0.3-0.6 (scales with camera viewport)
        
        [SerializeField] public float topSafePaddingExtra = 0f; 
        // ▲ Extra safe area padding above header (world units, if applicable)
        // Use to avoid notch/Dynamic Island interference
        // Typical: 0-0.5 (most devices:0, high-notch:0.3-0.5)
        
        [SerializeField] public float bottomSafePaddingExtra = 0f; 
        // ▲ Extra safe area padding below bottom area (world units, if applicable)
        // Use to avoid gesture bar interference
        // Typical: 0-0.5 (most devices:0, large-gestures:0.2-0.4)

        [Header("Button Hit Area")]
        [SerializeField] public float buttonInteractionRadius = 1.2f;
        [SerializeField] public bool enforceTightHitArea = true;

        [Header("Debug")]
        [SerializeField] public bool showDebugInfo = false;
        [SerializeField] public Color debugColorFrame = Color.cyan;
        [SerializeField] public Color debugColorPadding = Color.yellow;
        [SerializeField] public Color debugColorElement = Color.green;

        /// <summary>
        /// Checks if this profile matches the given screen dimensions
        /// </summary>
        public bool MatchesScreen(float screenWidth, float screenHeight, float diagonalInches)
        {
            float aspect = screenWidth / Mathf.Max(1f, screenHeight);

            return screenWidth >= minWidth && screenWidth <= maxWidth &&
                   screenHeight >= minHeight && screenHeight <= maxHeight &&
                   aspect >= minAspectRatio && aspect <= maxAspectRatio &&
                   diagonalInches >= minDiagonalInches && diagonalInches <= maxDiagonalInches;
        }

        /// <summary>
        /// Calculate edge padding based on screen size
        /// </summary>
        public Vector4 CalculateEdgePadding(float safeWidth, float safeHeight)
        {
            float horizontalPadding = Mathf.Clamp(
                safeWidth * horizontalPaddingMultiplier,
                minHorizontalPadding,
                maxHorizontalPadding
            );

            float topPadding = Mathf.Clamp(
                safeHeight * topPaddingMultiplier,
                minTopPadding,
                maxTopPadding
            );

            float bottomPadding = Mathf.Clamp(
                safeHeight * bottomPaddingMultiplier,
                minBottomPadding,
                maxBottomPadding
            );

            // Return as: left, top, right, bottom
            return new Vector4(horizontalPadding, topPadding, horizontalPadding, bottomPadding);
        }

        /// <summary>
        /// Get description for inspector
        /// </summary>
        public override string ToString()
        {
            return $"{deviceName} ({deviceType}): {minWidth}-{maxWidth}px, " +
                   $"Aspect {minAspectRatio:F2}-{maxAspectRatio:F2}, " +
                   $"Spacing: {verticalSpacing}x{horizontalSpacing}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate profile settings in editor
        /// </summary>
        private void OnValidate()
        {
            minWidth = Mathf.Max(300f, minWidth);
            maxWidth = Mathf.Max(minWidth, maxWidth);
            minHeight = Mathf.Max(400f, minHeight);
            maxHeight = Mathf.Max(minHeight, maxHeight);

            minAspectRatio = Mathf.Max(0.3f, minAspectRatio);
            maxAspectRatio = Mathf.Max(minAspectRatio + 0.1f, maxAspectRatio);

            minScale = Mathf.Clamp(minScale, 0.3f, 1f);
            maxScale = Mathf.Clamp(maxScale, minScale, 1f);

            verticalSpacing = Mathf.Max(2f, verticalSpacing);
            horizontalSpacing = Mathf.Max(2f, horizontalSpacing);
        }
#endif
    }
}
