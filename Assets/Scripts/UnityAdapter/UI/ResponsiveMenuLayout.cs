using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// ResponsiveMenuLayout Component
    /// Handles main menu responsive layout based on profile and device metrics.
    /// Encapsulates layout positioning for:
    /// - LogoArea (top section)
    /// - CenterActionArea (Play button - main CTA)
    /// - SecondaryButtonsArea (Continue, Scores, etc.)
    /// - BottomRewardArea (Reward, Daily, Progress)
    /// </summary>
    public class ResponsiveMenuLayout : MonoBehaviour
    {
        [SerializeField] private bool debugLayout = false;
        [SerializeField] private RectTransform safeAreaRoot;
        
        // Section roots
        [SerializeField] private RectTransform logoArea;
        [SerializeField] private RectTransform centerActionArea;
        [SerializeField] private RectTransform secondaryButtonsArea;
        [SerializeField] private RectTransform bottomRewardArea;
        
        // Cached values
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private DeviceLayoutProfile _currentProfile;
        private bool _initialized = false;

        private void OnEnable()
        {
            ResolveReferences();
            ApplyLayout(force: true);
        }

        private void Update()
        {
            if (!HasScreenMetricsChanged())
                return;

            ApplyLayout(force: false);
        }

        /// <summary>
        /// Manual layout refresh (call when profile changes or rotation occurs)
        /// </summary>
        public void Refresh()
        {
            ApplyLayout(force: true);
        }

        private void ResolveReferences()
        {
            if (safeAreaRoot == null)
            {
                safeAreaRoot = GetComponentInParent<RectTransform>();
                if (safeAreaRoot != null && safeAreaRoot.name != "__MainMenuSafeAreaRoot")
                {
                    // Try to find it in scene
                    safeAreaRoot = FindFirstObjectByType<MainMenuController>()?.GetComponent<RectTransform>();
                }
            }

            _initialized = safeAreaRoot != null;
        }

        private bool HasScreenMetricsChanged()
        {
            if (!_initialized)
                return true;

            int currentWidth = Screen.width;
            int currentHeight = Screen.height;
            Rect currentSafeArea = Screen.safeArea;

            bool changed = (currentWidth != _lastScreenWidth) ||
                          (currentHeight != _lastScreenHeight) ||
                          (currentSafeArea != _lastSafeArea);

            return changed;
        }

        private void CacheScreenMetrics()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        private void ApplyLayout(bool force)
        {
            if (!_initialized)
                return;

            if (!force && !HasScreenMetricsChanged())
                return;

            // Get current profile
            try
            {
                _currentProfile = DeviceProfileInitializer.GetCurrentProfile();
            }
            catch
            {
                _currentProfile = null;
            }

            if (_currentProfile == null)
            {
                Debug.LogWarning("[ResponsiveMenuLayout] No profile available");
                return;
            }

            // Get layout config
            UILayoutConfig config = UILayoutConfig.Instance;
            if (config == null)
            {
                Debug.LogWarning("[ResponsiveMenuLayout] UILayoutConfig not available");
                return;
            }

            float screenHeight = Screen.height;
            float screenWidth = Screen.width;

            // Apply safe area to root
            ApplySafeAreaToRoot(safeAreaRoot);

            // Calculate section heights
            float headerHeight = config.GetHeaderHeight(screenHeight);
            float boardHeight = config.GetBoardHeight(screenHeight);
            float trayHeight = config.GetTrayHeight(screenHeight);
            float bottomHeight = config.GetBottomHeight(screenHeight);

            // Position logo area (top)
            if (logoArea != null)
            {
                LayoutArea(logoArea, 0f, headerHeight, screenWidth);
            }

            // Position center action area (Play button)
            if (centerActionArea != null)
            {
                float playTopPosition = headerHeight;
                LayoutArea(centerActionArea, playTopPosition, boardHeight, screenWidth);
            }

            // Position secondary buttons area
            if (secondaryButtonsArea != null)
            {
                float secondaryTopPosition = headerHeight + boardHeight;
                float secondaryHeight = config.GetSecondaryButtonsHeight(screenHeight);
                LayoutArea(secondaryButtonsArea, secondaryTopPosition, secondaryHeight, screenWidth);
            }

            // Position bottom reward area
            if (bottomRewardArea != null)
            {
                float bottomTopPosition = headerHeight + boardHeight + trayHeight;
                LayoutArea(bottomRewardArea, bottomTopPosition, bottomHeight, screenWidth);
            }

            CacheScreenMetrics();

            if (debugLayout)
                LogLayoutInfo(config, screenHeight);
        }

        /// <summary>
        /// Position a layout area (stretches to full width, sets Y position and height)
        /// </summary>
        private void LayoutArea(RectTransform rect, float topYPosition, float height, float width)
        {
            if (rect == null)
                return;

            // Stretch horizontally, position vertically
            rect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
            rect.anchorMax = new Vector2(1f, 1f); // Top-right anchor
            rect.pivot = new Vector2(0.5f, 1f);  // Top-center pivot

            rect.offsetMin = new Vector2(0f, -topYPosition - height); // Bottom-left offset
            rect.offsetMax = new Vector2(0f, -topYPosition); // Bottom-right offset

            rect.localScale = Vector3.one;
        }

        private static void ApplySafeAreaToRoot(RectTransform rect)
        {
            if (rect == null)
                return;

            Rect safeArea = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);

            rect.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            rect.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void LogLayoutInfo(UILayoutConfig config, float screenHeight)
        {
            Debug.Log($@"[ResponsiveMenuLayout] Applied Layout:
Logo Area Height: {config.GetHeaderHeight(screenHeight):F1}px
Play Button Area Height: {config.GetBoardHeight(screenHeight):F1}px
Secondary Area Height: {config.GetSecondaryButtonsHeight(screenHeight):F1}px
Bottom Reward Area Height: {config.GetBottomHeight(screenHeight):F1}px
Total: {config.GetHeaderHeight(screenHeight) + config.GetBoardHeight(screenHeight) + config.GetTrayHeight(screenHeight) + config.GetBottomHeight(screenHeight):F1}px");
        }
    }
}
