using UnityEngine;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// UILayoutConfig Singleton - Centralized UI layout configuration based on device profile.
    /// Provides layout ratios, spacing, and sizing values for all UI elements.
    /// Bridges DeviceLayoutProfile and actual UI layout calculations.
    /// </summary>
    public class UILayoutConfig : MonoBehaviour
    {
        private static UILayoutConfig _instance;
        
        [SerializeField] private bool debugLayout = false;

        private DeviceLayoutProfile _currentProfile;
        private bool _initialized = false;

        public static UILayoutConfig Instance => _instance;
        public static bool IsInitialized => _instance != null && _instance._initialized;

        /// <summary>
        /// Get current device profile (auto-loaded from DeviceProfileInitializer if available)
        /// </summary>
        public DeviceLayoutProfile CurrentProfile => _currentProfile;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Initialize config with device profile
        /// </summary>
        public void Initialize()
        {
            // Try to get profile from DeviceProfileInitializer
            try
            {
                _currentProfile = DeviceProfileInitializer.GetCurrentProfile();
            }
            catch
            {
                // DeviceProfileInitializer not available or not initialized yet
                _currentProfile = null;
            }

            _initialized = true;

            if (debugLayout)
                LogLayoutInfo();
        }

        /// <summary>
        /// Refresh profile (if screen rotates, device changes, etc.)
        /// </summary>
        public void RefreshProfile()
        {
            Initialize();
        }

        // ==================== MAIN MENU LAYOUT CALCULATIONS ====================

        /// <summary>
        /// Get header area height (top safe padding + logo + badge area)
        /// Returns: pixels height based on screen
        /// </summary>
        public float GetHeaderHeight(float screenHeight)
        {
            if (_currentProfile == null)
                return screenHeight * 0.15f; // Fallback

            // headerHeightRatio = percentage of screen for header (0.12-0.16)
            return screenHeight * _currentProfile.headerHeightRatio;
        }

        /// <summary>
        /// Get board area height (main content area - typically for game board on game screen)
        /// For main menu, this represents center content area
        /// </summary>
        public float GetBoardHeight(float screenHeight)
        {
            if (_currentProfile == null)
                return screenHeight * 0.55f; // Fallback

            return screenHeight * _currentProfile.boardHeightRatio;
        }

        /// <summary>
        /// Get tray/footer area height (bottom secondary content)
        /// For main menu, this is secondary buttons area
        /// </summary>
        public float GetTrayHeight(float screenHeight)
        {
            if (_currentProfile == null)
                return screenHeight * 0.15f; // Fallback

            return screenHeight * _currentProfile.trayHeightRatio;
        }

        /// <summary>
        /// Get bottom safe area padding height
        /// </summary>
        public float GetBottomHeight(float screenHeight)
        {
            if (_currentProfile == null)
                return screenHeight * 0.15f; // Fallback

            return screenHeight * _currentProfile.bottomHeightRatio;
        }

        /// <summary>
        /// Get main content scale (e.g., Play button size relative to board cell)
        /// Range: 0.65-0.75 (65-75% of base size)
        /// </summary>
        public float GetMainContentScale()
        {
            if (_currentProfile == null)
                return 0.70f; // Fallback

            return _currentProfile.trayBlockScalePercent;
        }

        /// <summary>
        /// Get spacing between major sections (board and tray, main and secondary areas)
        /// Range: 0.3-0.45 world-space units
        /// </summary>
        public float GetElementSpacing()
        {
            if (_currentProfile == null)
                return 0.4f; // Fallback

            return _currentProfile.boardTrayGap;
        }

        /// <summary>
        /// Get horizontal side padding (left/right margins)
        /// Typical: 0.4 world-space units
        /// </summary>
        public float GetSidePadding()
        {
            if (_currentProfile == null)
                return 0.4f; // Fallback

            return _currentProfile.sidePadding;
        }

        /// <summary>
        /// Get additional top safe area padding for critical UI (not in notch/gesture area)
        /// </summary>
        public float GetTopSafePaddingExtra()
        {
            if (_currentProfile == null)
                return 0f; // Fallback

            return _currentProfile.topSafePaddingExtra;
        }

        /// <summary>
        /// Get additional bottom safe area padding
        /// </summary>
        public float GetBottomSafePaddingExtra()
        {
            if (_currentProfile == null)
                return 0f; // Fallback

            return _currentProfile.bottomSafePaddingExtra;
        }

        // ==================== MAIN MENU SPECIFIC CALCULATIONS ====================

        /// <summary>
        /// Calculate logo area preferred height based on header ratio
        /// Typically: 0.4-0.5 of header height
        /// </summary>
        public float GetLogoAreaHeight(float screenHeight)
        {
            // Logo takes middle portion of header area
            float headerHeight = GetHeaderHeight(screenHeight);
            return Mathf.Clamp(headerHeight * 0.6f, 60f, 160f); // Min 60px, max 160px
        }

        /// <summary>
        /// Calculate Play button target size based on profile
        /// Play button should be prominent on all devices
        /// Typical: 100-160px button size
        /// </summary>
        public float GetPlayButtonTargetSize(float screenHeight)
        {
            // Scale Play button based on content scale and screen height
            float scale = GetMainContentScale(); // 0.65-0.75
            float baseSizePercentage = scale * 0.3f; // 20-25% of screen at typical scale
            return Mathf.Clamp(screenHeight * baseSizePercentage, 100f, 200f); // Min 100px, max 200px
        }

        /// <summary>
        /// Calculate secondary buttons row height
        /// Should allow multiple buttons (Continue, Scores, HowToPlay) side-by-side
        /// Typical: 60-90px
        /// </summary>
        public float GetSecondaryButtonsHeight(float screenHeight)
        {
            // Secondary area typically 40-50% of tray height
            float trayHeight = GetTrayHeight(screenHeight);
            return Mathf.Clamp(trayHeight * 0.45f, 48f, 90f); // Min 48px for touch, max 90px
        }

        /// <summary>
        /// Calculate center action area (Play button) height
        /// This is the main visual focus
        /// Typically uses board area for sizing
        /// </summary>
        public float GetCenterActionAreaHeight(float screenHeight)
        {
            // Play button gets primary vertical space (board area)
            float boardHeight = GetBoardHeight(screenHeight);
            return Mathf.Clamp(boardHeight, 200f, 500f); // Min 200px, max 500px (for ultra-tall displays)
        }

        /// <summary>
        /// Calculate bottom reward area height
        /// Shows reward/daily quest/progress info
        /// Typical: 60-100px
        /// </summary>
        public float GetBottomRewardAreaHeight(float screenHeight)
        {
            // Bottom area shows progress/reward/daily info
            // Should be compact to not compete with Play button
            return Mathf.Clamp(screenHeight * 0.08f, 50f, 100f); // Min 50px, max 100px
        }

        /// <summary>
        /// Calculate horizontal spacing between buttons in a row
        /// </summary>
        public float GetHorizontalButtonSpacing()
        {
            // Profile provides boardTrayGap as reference
            float gap = GetElementSpacing();
            return gap * 15f; // ~4.5-6.75 pixels in world space (scaled to screen)
        }

        /// <summary>
        /// Calculate vertical spacing between layout areas
        /// Logo > Play > Secondary > Reward areas
        /// </summary>
        public float GetVerticalAreaSpacing(float screenHeight)
        {
            // Proportional spacing based on screen height
            float spacing = screenHeight * 0.01f;
            return Mathf.Clamp(spacing, 8f, 24f); // 1% of screen, min 8px, max 24px
        }

        /// <summary>
        /// Get minimum touch target size (for button hit areas)
        /// Apple/Android guideline: 44pt (iOS) / 48dp (Android)
        /// Convert to pixels based on screen scale
        /// </summary>
        public float GetMinTouchTargetSize()
        {
            // Typically 44-48 pixels (scaled)
            // Adjust based on min scale to ensure accessibility
            return 48f;
        }

        /// <summary>
        /// Get logo safe area bounds (don't let it get too large on ultra-wide displays)
        /// </summary>
        public Vector2 GetLogoMaxSize(float screenWidth, float screenHeight)
        {
            float maxWidth = screenWidth * 0.8f;  // 80% of screen width max
            float maxHeight = GetLogoAreaHeight(screenHeight);
            return new Vector2(maxWidth, maxHeight);
        }

        /// <summary>
        /// Get logo safe area bounds (minimum visible on ultra-wide displays)
        /// </summary>
        public Vector2 GetLogoMinSize(float screenWidth)
        {
            return new Vector2(screenWidth * 0.4f, 40f); // At least 40% of width or 40px height
        }

        // ==================== UTILITY ====================

        private void LogLayoutInfo()
        {
            if (_currentProfile == null)
            {
                Debug.LogWarning("[UILayoutConfig] No profile available for layout debug");
                return;
            }

            float screenHeight = Screen.height;
            Debug.Log($@"[UILayoutConfig] Layout Info:
  Profile: {_currentProfile.deviceName}
  Screen: {Screen.width}x{Screen.height} ({Screen.dpi:F1} DPI)
  
  Header Height: {GetHeaderHeight(screenHeight):F1}px ({_currentProfile.headerHeightRatio * 100:F1}%)
  Board Height: {GetBoardHeight(screenHeight):F1}px ({_currentProfile.boardHeightRatio * 100:F1}%)
  Tray Height: {GetTrayHeight(screenHeight):F1}px ({_currentProfile.trayHeightRatio * 100:F1}%)
  Bottom Height: {GetBottomHeight(screenHeight):F1}px ({_currentProfile.bottomHeightRatio * 100:F1}%)
  
  Play Button Size: {GetPlayButtonTargetSize(screenHeight):F1}px
  Secondary Buttons Height: {GetSecondaryButtonsHeight(screenHeight):F1}px
  Content Scale: {GetMainContentScale() * 100:F1}%
  Element Spacing: {GetElementSpacing():F3}");
        }

        /// <summary>
        /// Manual utility to clamp value between min/max bounds
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
