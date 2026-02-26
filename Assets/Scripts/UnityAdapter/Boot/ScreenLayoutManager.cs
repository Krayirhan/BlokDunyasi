using BlockPuzzle.Core.Engine;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.Boot
{
    internal sealed class ScreenLayoutManager
    {
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;

        public bool HasScreenMetricsChanged()
        {
            Rect safeArea = Screen.safeArea;
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                return true;

            const float threshold = 0.5f;
            return Mathf.Abs(safeArea.x - _lastSafeArea.x) > threshold ||
                   Mathf.Abs(safeArea.y - _lastSafeArea.y) > threshold ||
                   Mathf.Abs(safeArea.width - _lastSafeArea.width) > threshold ||
                   Mathf.Abs(safeArea.height - _lastSafeArea.height) > threshold;
        }

        public void ApplyResponsiveLayout(
            Vector3 cameraPosition,
            float baseCameraSize,
            bool lockPortraitOrientation,
            float cameraHorizontalPadding,
            float minAdaptiveCameraSize,
            float maxAdaptiveCameraSize,
            Vector2 canvasReferenceResolution,
            float canvasMatchWidthOrHeight,
            int fallbackBoardWidth,
            GameState currentGameState,
            bool forceTrayRefresh)
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            if (lockPortraitOrientation && Application.isMobilePlatform)
            {
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToPortrait = true;
                Screen.orientation = ScreenOrientation.Portrait;
            }

            float safeAspect = GetSafeAspectRatio();
            float boardWorldWidth = EstimateBoardWorldWidth(fallbackBoardWidth, currentGameState);

            float requiredHalfWidth = (boardWorldWidth * 0.5f) + cameraHorizontalPadding;
            float sizeForWidth = requiredHalfWidth / Mathf.Max(0.01f, safeAspect);
            float targetSize = Mathf.Max(baseCameraSize, sizeForWidth, minAdaptiveCameraSize);
            targetSize = Mathf.Clamp(targetSize, minAdaptiveCameraSize, maxAdaptiveCameraSize);

            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = targetSize;

            ConfigureCanvasScalers(canvasReferenceResolution, canvasMatchWidthOrHeight);

            if (forceTrayRefresh)
            {
                var tray = Object.FindFirstObjectByType<NewBlockTray>();
                tray?.RefreshLayoutForScreenChange();
            }

            CacheScreenMetrics();
        }

        private void CacheScreenMetrics()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        private static float EstimateBoardWorldWidth(int fallbackBoardWidth, GameState currentGameState)
        {
            int logicalBoardWidth = currentGameState?.Board?.Width ?? fallbackBoardWidth;
            logicalBoardWidth = Mathf.Max(1, logicalBoardWidth);

            var gridView = Object.FindFirstObjectByType<SimpleGridView>();
            float cellSizeWorld = gridView != null ? gridView.CellSize : 0.7f;
            float spacingWorld = gridView != null ? gridView.CellSpacing : 0.05f;

            float step = cellSizeWorld + spacingWorld;
            return ((logicalBoardWidth - 1) * step) + cellSizeWorld;
        }

        private static float GetSafeAspectRatio()
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea.width > 0f && safeArea.height > 0f)
                return safeArea.width / safeArea.height;

            if (Screen.height <= 0)
                return 9f / 16f;

            return (float)Screen.width / Screen.height;
        }

        private static void ConfigureCanvasScalers(Vector2 referenceResolution, float matchWidthOrHeight)
        {
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scalers.Length; i++)
            {
                var scaler = scalers[i];
                if (scaler == null)
                    continue;

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = referenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = matchWidthOrHeight;
            }
        }
    }
}
