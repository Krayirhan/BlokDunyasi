using UnityEngine;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// ResponsiveGameLayout - Manages game screen layout across regions:
    /// - HeaderArea (safe area UI - score, home, settings, trophy)
    /// - Board (world space - game grid)
    /// - Tray (world space - block selection)
    /// - BottomArea (safe area UI - progress, reward bars)
    /// 
    /// Orchestrates responsive positioning preventing overlap and ensuring
    /// all elements respect safe areas and device constraints.
    /// Current repo state: this acts as an explicit scene-side layout helper and
    /// validation model when wired, but ScreenLayoutManager remains the active
    /// runtime coordinator invoked by GameBootstrap.
    /// </summary>
    [ExecuteAlways]
    public class ResponsiveGameLayout : MonoBehaviour
    {
        [SerializeField] private bool debugLayout = false;
        [SerializeField] private Color debugHeaderColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color debugBoardColor = new Color(0f, 0f, 1f, 0.2f);
        [SerializeField] private Color debugTrayColor = new Color(1f, 0f, 0f, 0.2f);
        [SerializeField] private Color debugBottomColor = new Color(1f, 1f, 0f, 0.2f);

        // UI References (Canvas space)
        [SerializeField] private RectTransform gameScreenRoot;
        [SerializeField] private RectTransform headerArea;
        [SerializeField] private RectTransform bottomArea;

        // World space references
        [SerializeField] private Transform boardRoot;
        [SerializeField] private SimpleGridView gridView;
        [SerializeField] private Transform blockTrayRoot;
        [SerializeField] private NewBlockTray blockTray;

        // Layout References
        [SerializeField] private Camera mainCamera;

        // Cached layout data
        private CameraBounds _cameraBounds;
        private LayoutAreas _layoutAreas;
        private bool _hasCameraBounds;
        private bool _hasLayoutAreas;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        private bool _initialized = false;
        private bool _loggedDependencyWarning;

        /// <summary>
        /// Cached bounds for debug visualization
        /// </summary>
        public CameraBounds CurrentCameraBounds => _cameraBounds;
        public LayoutAreas CurrentLayoutAreas => _layoutAreas;

        private void OnEnable()
        {
            ResolveReferences();
            RecalculateLayout(force: true);
        }

        private void Update()
        {
            if (!_initialized)
            {
                ResolveReferences();
                RecalculateLayout(force: true);
                return;
            }

            if (!HasScreenMetricsChanged())
                return;

            RecalculateLayout(force: false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugLayout || !Application.isPlaying)
                return;

            DrawDebugBounds();
        }

        /// <summary>
        /// Manual refresh when screen rotates or device changes
        /// </summary>
        public void Refresh()
        {
            RecalculateLayout(force: true);
        }

        private void ResolveReferences()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera != null)
                    GameLogger.LogWarning("[ResponsiveGameLayout] mainCamera is not wired in the inspector. Falling back to Camera.main.");
            }

            if (gameScreenRoot == null)
            {
                // Try to find GameScreen Canvas root
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    gameScreenRoot = canvas.GetComponent<RectTransform>();
                    GameLogger.LogWarning("[ResponsiveGameLayout] gameScreenRoot was resolved via runtime lookup. Inspector wiring is the preferred production path.");
                }
            }

            if (gridView == null)
            {
                gridView = FindFirstObjectByType<SimpleGridView>();
                if (gridView != null)
                    GameLogger.LogWarning("[ResponsiveGameLayout] gridView was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (blockTray == null)
            {
                blockTray = FindFirstObjectByType<NewBlockTray>();
                if (blockTray != null)
                    GameLogger.LogWarning("[ResponsiveGameLayout] blockTray was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (boardRoot == null && gridView != null)
                boardRoot = gridView.transform.parent ?? gridView.transform;

            if (blockTrayRoot == null && blockTray != null)
                blockTrayRoot = blockTray.transform.parent ?? blockTray.transform;

            _initialized = mainCamera != null && gameScreenRoot != null && gridView != null;

            if (!_initialized && !_loggedDependencyWarning)
            {
                _loggedDependencyWarning = true;
                GameLogger.LogWarning("[ResponsiveGameLayout] Could not resolve all required references");
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (mainCamera == null)
                mainCamera = TryAutoAssignSingleton<Camera>();

            if (gameScreenRoot == null)
            {
                Canvas canvas = TryAutoAssignSingleton<Canvas>();
                if (canvas != null)
                    gameScreenRoot = canvas.GetComponent<RectTransform>();
            }

            if (gridView == null)
                gridView = TryAutoAssignSingleton<SimpleGridView>();

            if (blockTray == null)
                blockTray = TryAutoAssignSingleton<NewBlockTray>();

            if (boardRoot == null && gridView != null)
                boardRoot = gridView.transform.parent ?? gridView.transform;

            if (blockTrayRoot == null && blockTray != null)
                blockTrayRoot = blockTray.transform.parent ?? blockTray.transform;
#endif
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        private bool HasScreenMetricsChanged()
        {
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

        private void RecalculateLayout(bool force)
        {
            if (!_initialized)
                return;

            if (!force && !HasScreenMetricsChanged())
                return;

            // Step 1: Calculate camera visible bounds in world space
            CalculateCameraBounds();

            // Step 2: Get profile and layout config
            DeviceLayoutProfile profile = GetCurrentProfile();
            UILayoutConfig config = UILayoutConfig.Instance;

            // Step 3: Calculate layout areas
            CalculateLayoutAreas(profile, config);

            // Step 4: Position world-space elements (board, tray)
            PositionWorldElements();

            // Step 5: Position UI elements (header, bottom)
            PositionUIElements();

            // Step 6: Validate no overlap
            ValidateLayout();

            CacheScreenMetrics();

            if (debugLayout)
                LogLayoutInfo();
        }

        private void CalculateCameraBounds()
        {
            if (mainCamera == null)
            {
                _hasCameraBounds = false;
                return;
            }

            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            Vector3 camPos = mainCamera.transform.position;
            float left = camPos.x - (cameraWidth * 0.5f);
            float right = camPos.x + (cameraWidth * 0.5f);
            float top = camPos.y + mainCamera.orthographicSize;
            float bottom = camPos.y - mainCamera.orthographicSize;

            _cameraBounds = new CameraBounds
            {
                Width = cameraWidth,
                Height = cameraHeight,
                Left = left,
                Right = right,
                Top = top,
                Bottom = bottom,
                Center = camPos
            };
            _hasCameraBounds = true;
        }

        private void CalculateLayoutAreas(DeviceLayoutProfile profile, UILayoutConfig config)
        {
            if (profile == null || config == null)
            {
                _hasLayoutAreas = false;
                return;
            }

            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            Rect safeArea = Screen.safeArea;

            // Calculate UI safe area dimensions (in screen coordinates)
            float safeAreaHeight = safeArea.height;
            float safeAreaWidth = safeArea.width;

            // Get layout ratios from profile
            float headerRatio = profile.headerHeightRatio;
            float bottomRatio = profile.bottomHeightRatio;
            float boardRatio = profile.boardHeightRatio;
            float trayRatio = profile.trayHeightRatio;

            // Calculate screen-space heights (for UI positioning)
            float headerHeight = screenHeight * headerRatio;
            float bottomHeight = screenHeight * bottomRatio;

            // Calculate world-space positions for board and tray based on camera bounds
            // The board and tray should fit within camera visible area
            float boardWorldTop, boardWorldBottom, trayWorldTop, trayWorldBottom;

            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                // Get board dimensions
                float boardWorldWidth = gridView.Width * gridView.CellSize + (gridView.Width - 1) * gridView.CellSpacing;
                float boardWorldHeight = gridView.Height * gridView.CellSize + (gridView.Height - 1) * gridView.CellSpacing;

                // For now, assume board is roughly in center
                // More precise positioning would come from SimpleGridView's transform
                boardWorldTop = boardWorldHeight * 0.5f;
                boardWorldBottom = -boardWorldHeight * 0.5f;

                // Tray positioning below board with gap
                float trayGap = profile.boardTrayGap;
                float trayHeight = profile.trayHeightRatio * _cameraBounds.Height;

                trayWorldTop = boardWorldBottom - trayGap;
                trayWorldBottom = trayWorldTop - trayHeight;
            }
            else
            {
                // Fallback positioning (should rarely happen)
                boardWorldTop = 2f;
                boardWorldBottom = -2f;
                trayWorldTop = -2.4f;
                trayWorldBottom = -4.5f;
            }

            // Safe area padding (world space)
            float topSafePadding = profile.topSafePaddingExtra;
            float bottomSafePadding = profile.bottomSafePaddingExtra;
            float sidePadding = profile.sidePadding;

            _layoutAreas = new LayoutAreas
            {
                // Screen space (UI)
                HeaderScreenTop = screenHeight,
                HeaderScreenBottom = screenHeight - headerHeight,
                BottomScreenTop = bottomHeight,
                BottomScreenBottom = 0f,

                // Safe area (UI)
                HeaderSafeTop = safeArea.yMax,
                HeaderSafeBottom = safeArea.yMax - headerHeight,
                BottomSafeTop = safeArea.yMin + bottomHeight,
                BottomSafeBottom = safeArea.yMin,

                // World space
                BoardWorldTop = boardWorldTop,
                BoardWorldBottom = boardWorldBottom,
                BoardWorldLeft = _cameraBounds.Left + sidePadding,
                BoardWorldRight = _cameraBounds.Right - sidePadding,

                TrayWorldTop = trayWorldTop,
                TrayWorldBottom = trayWorldBottom,
                TrayWorldLeft = _cameraBounds.Left + sidePadding,
                TrayWorldRight = _cameraBounds.Right - sidePadding,

                // Safe area world space equivalents
                SafeAreaPadding = new Vector4(sidePadding, topSafePadding, sidePadding, bottomSafePadding)
            };
            _hasLayoutAreas = true;
        }

        private void PositionWorldElements()
        {
            if (!_hasLayoutAreas)
                return;

            // Board: Center horizontally in camera view
            if (boardRoot != null && gridView != null)
            {
                // Board is typically already positioned by SimpleGridView
                // Ensure it's within safe bounds
                float boardCenterX = (_layoutAreas.BoardWorldLeft + _layoutAreas.BoardWorldRight) * 0.5f;
                Vector3 boardPos = boardRoot.position;
                boardPos.x = boardCenterX;
                boardRoot.position = boardPos;
            }

            // Tray: Position below board with calculated Y
            if (blockTrayRoot != null)
            {
                float trayCenterY = (_layoutAreas.TrayWorldTop + _layoutAreas.TrayWorldBottom) * 0.5f;
                float trayCenterX = (_layoutAreas.TrayWorldLeft + _layoutAreas.TrayWorldRight) * 0.5f;

                Vector3 trayPos = blockTrayRoot.position;
                trayPos.x = trayCenterX;
                trayPos.y = trayCenterY;
                blockTrayRoot.position = trayPos;
            }

            // Refresh tray layout for new screen size
            if (blockTray != null)
            {
                blockTray.RefreshLayoutForScreenChange();
            }
        }

        private void PositionUIElements()
        {
            if (!_hasLayoutAreas || gameScreenRoot == null)
                return;

            // Header Area: Top safe area
            if (headerArea != null)
            {
                float screenHeight = Screen.height;
                float headerHeight = _layoutAreas.HeaderScreenTop - _layoutAreas.HeaderScreenBottom;

                headerArea.anchorMin = new Vector2(0f, 1f);
                headerArea.anchorMax = new Vector2(1f, 1f);
                headerArea.pivot = new Vector2(0.5f, 1f);
                headerArea.offsetMin = new Vector2(0f, -headerHeight);
                headerArea.offsetMax = new Vector2(0f, 0f);
            }

            // Bottom Area: Bottom safe area
            if (bottomArea != null)
            {
                float bottomHeight = _layoutAreas.BottomScreenTop - _layoutAreas.BottomScreenBottom;

                bottomArea.anchorMin = new Vector2(0f, 0f);
                bottomArea.anchorMax = new Vector2(1f, 0f);
                bottomArea.pivot = new Vector2(0.5f, 0f);
                bottomArea.offsetMin = new Vector2(0f, 0f);
                bottomArea.offsetMax = new Vector2(0f, bottomHeight);
            }
        }

        private void ValidateLayout()
        {
            if (!_hasLayoutAreas || !_hasCameraBounds)
                return;

            // Check for overlaps
            bool boardTrayOverlap = _layoutAreas.TrayWorldTop > _layoutAreas.BoardWorldBottom;
            if (boardTrayOverlap)
            {
                GameLogger.LogWarning("[ResponsiveGameLayout] WARNING: Tray overlaps with board! Tray bottom needs adjustment.");
            }

            // Check board fits in camera
            bool boardFitsHorizontally = (_layoutAreas.BoardWorldRight - _layoutAreas.BoardWorldLeft) <= _cameraBounds.Width;
            if (!boardFitsHorizontally)
            {
                GameLogger.LogWarning("[ResponsiveGameLayout] WARNING: Board width exceeds camera view width");
            }

            bool boardFitsVertically = (_layoutAreas.BoardWorldTop - _layoutAreas.BoardWorldBottom) <= _cameraBounds.Height;
            if (!boardFitsVertically)
            {
                GameLogger.LogWarning("[ResponsiveGameLayout] WARNING: Board height exceeds camera view height");
            }

            // Check header in safe area
            if (_layoutAreas.HeaderScreenBottom < Screen.safeArea.yMin)
            {
                GameLogger.LogWarning("[ResponsiveGameLayout] WARNING: Header extends below safe area bottom");
            }

            // Check bottom in safe area
            if (_layoutAreas.BottomScreenTop > Screen.safeArea.yMax)
            {
                GameLogger.LogWarning("[ResponsiveGameLayout] WARNING: Bottom area extends above safe area top");
            }
        }

        private DeviceLayoutProfile GetCurrentProfile()
        {
            try
            {
                return DeviceProfileInitializer.GetCurrentProfile();
            }
            catch
            {
                return null;
            }
        }

        private void DrawDebugBounds()
        {
            if (!_hasCameraBounds)
                return;

            // Draw camera bounds
            Vector3 camTopLeft = new Vector3(_cameraBounds.Left, _cameraBounds.Top, 0f);
            Vector3 camTopRight = new Vector3(_cameraBounds.Right, _cameraBounds.Top, 0f);
            Vector3 camBottomLeft = new Vector3(_cameraBounds.Left, _cameraBounds.Bottom, 0f);
            Vector3 camBottomRight = new Vector3(_cameraBounds.Right, _cameraBounds.Bottom, 0f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(camTopLeft, camTopRight);
            Gizmos.DrawLine(camTopRight, camBottomRight);
            Gizmos.DrawLine(camBottomRight, camBottomLeft);
            Gizmos.DrawLine(camBottomLeft, camTopLeft);

            // Draw board area
            if (_hasLayoutAreas)
            {
                Vector3 boardTL = new Vector3(_layoutAreas.BoardWorldLeft, _layoutAreas.BoardWorldTop, 0f);
                Vector3 boardTR = new Vector3(_layoutAreas.BoardWorldRight, _layoutAreas.BoardWorldTop, 0f);
                Vector3 boardBL = new Vector3(_layoutAreas.BoardWorldLeft, _layoutAreas.BoardWorldBottom, 0f);
                Vector3 boardBR = new Vector3(_layoutAreas.BoardWorldRight, _layoutAreas.BoardWorldBottom, 0f);

                Gizmos.color = debugBoardColor;
                Gizmos.DrawLine(boardTL, boardTR);
                Gizmos.DrawLine(boardTR, boardBR);
                Gizmos.DrawLine(boardBR, boardBL);
                Gizmos.DrawLine(boardBL, boardTL);

                // Draw tray area
                Vector3 trayTL = new Vector3(_layoutAreas.TrayWorldLeft, _layoutAreas.TrayWorldTop, 0f);
                Vector3 trayTR = new Vector3(_layoutAreas.TrayWorldRight, _layoutAreas.TrayWorldTop, 0f);
                Vector3 trayBL = new Vector3(_layoutAreas.TrayWorldLeft, _layoutAreas.TrayWorldBottom, 0f);
                Vector3 trayBR = new Vector3(_layoutAreas.TrayWorldRight, _layoutAreas.TrayWorldBottom, 0f);

                Gizmos.color = debugTrayColor;
                Gizmos.DrawLine(trayTL, trayTR);
                Gizmos.DrawLine(trayTR, trayBR);
                Gizmos.DrawLine(trayBR, trayBL);
                Gizmos.DrawLine(trayBL, trayTL);
            }
        }

        private void LogLayoutInfo()
        {
            if (!_hasCameraBounds || !_hasLayoutAreas)
                return;

            GameLogger.Log($@"[ResponsiveGameLayout] Layout Info:
=== CAMERA VIEW ===
Width x Height: {_cameraBounds.Width:F2} x {_cameraBounds.Height:F2}
Bounds: L:{_cameraBounds.Left:F2} R:{_cameraBounds.Right:F2} T:{_cameraBounds.Top:F2} B:{_cameraBounds.Bottom:F2}

=== BOARD AREA (World) ===
Y Range: {_layoutAreas.BoardWorldTop:F2} to {_layoutAreas.BoardWorldBottom:F2}
X Range: {_layoutAreas.BoardWorldLeft:F2} to {_layoutAreas.BoardWorldRight:F2}
Size: {_layoutAreas.BoardWorldRight - _layoutAreas.BoardWorldLeft:F2} x {_layoutAreas.BoardWorldTop - _layoutAreas.BoardWorldBottom:F2}

=== TRAY AREA (World) ===
Y Range: {_layoutAreas.TrayWorldTop:F2} to {_layoutAreas.TrayWorldBottom:F2}
X Range: {_layoutAreas.TrayWorldLeft:F2} to {_layoutAreas.TrayWorldRight:F2}
Size: {_layoutAreas.TrayWorldRight - _layoutAreas.TrayWorldLeft:F2} x {_layoutAreas.TrayWorldTop - _layoutAreas.TrayWorldBottom:F2}

=== UI AREAS (Screen) ===
Header: {_layoutAreas.HeaderScreenBottom:F0}px - {_layoutAreas.HeaderScreenTop:F0}px (height: {_layoutAreas.HeaderScreenTop - _layoutAreas.HeaderScreenBottom:F0}px)
Bottom: {_layoutAreas.BottomScreenBottom:F0}px - {_layoutAreas.BottomScreenTop:F0}px (height: {_layoutAreas.BottomScreenTop - _layoutAreas.BottomScreenBottom:F0}px)

=== GAP CHECK ===
Board Bottom: {_layoutAreas.BoardWorldBottom:F2}
Tray Top: {_layoutAreas.TrayWorldTop:F2}
Gap: {_layoutAreas.TrayWorldTop - _layoutAreas.BoardWorldBottom:F2}
Overlap: {_layoutAreas.TrayWorldTop > _layoutAreas.BoardWorldBottom}");
        }

        /// <summary>
        /// Cached camera visible bounds in world space
        /// </summary>
        public struct CameraBounds
        {
            public float Width;
            public float Height;
            public float Left;
            public float Right;
            public float Top;
            public float Bottom;
            public Vector3 Center;
        }

        /// <summary>
        /// Cached layout area positions and bounds
        /// </summary>
        public struct LayoutAreas
        {
            // Screen-space UI areas
            public float HeaderScreenTop;
            public float HeaderScreenBottom;
            public float BottomScreenTop;
            public float BottomScreenBottom;

            // Safe-area UI bounds
            public float HeaderSafeTop;
            public float HeaderSafeBottom;
            public float BottomSafeTop;
            public float BottomSafeBottom;

            // World-space board area
            public float BoardWorldTop;
            public float BoardWorldBottom;
            public float BoardWorldLeft;
            public float BoardWorldRight;

            // World-space tray area
            public float TrayWorldTop;
            public float TrayWorldBottom;
            public float TrayWorldLeft;
            public float TrayWorldRight;

            // Safe area padding
            public Vector4 SafeAreaPadding; // (left, top, right, bottom)
        }
    }
}
