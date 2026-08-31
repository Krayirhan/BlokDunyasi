using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Grid;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Blocks
{
    public readonly struct ShapeExtents
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public float Width => Right - Left;
        public float Height => Top - Bottom;

        public ShapeExtents(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }

    public readonly struct TrayLayoutResult
    {
        public readonly Vector3[] SlotPositions;
        public readonly float TrayScale;
        public readonly bool UsedFallback;
        public readonly string Reason;
        public readonly Camera LayoutCamera;

        public TrayLayoutResult(Vector3[] slotPositions, float trayScale, bool usedFallback, string reason, Camera layoutCamera)
        {
            SlotPositions = slotPositions;
            TrayScale = trayScale;
            UsedFallback = usedFallback;
            Reason = reason;
            LayoutCamera = layoutCamera;
        }
    }

    public readonly struct TrayLayoutConfig
    {
        public readonly float BoardCellSize;
        public readonly float BoardCellSpacing;
        public readonly float TrayBlockScale;
        public readonly float SlotGap;
        public readonly float TrayHorizontalPadding;
        public readonly float TrayVerticalPadding;
        public readonly float TrayGapFromGrid;
        public readonly float TrayVerticalOffset;
        public readonly float MinTrayScale;
        public readonly bool HasResponsiveOverride;
        public readonly float ResponsiveWidth;
        public readonly float ResponsiveHeight;
        public readonly Vector3 ResponsiveCenter;

        public TrayLayoutConfig(
            float boardCellSize, float boardCellSpacing, float trayBlockScale,
            float slotGap, float trayHorizontalPadding, float trayVerticalPadding,
            float trayGapFromGrid, float trayVerticalOffset, float minTrayScale,
            bool hasResponsiveOverride, float responsiveWidth, float responsiveHeight, Vector3 responsiveCenter)
        {
            BoardCellSize = boardCellSize;
            BoardCellSpacing = boardCellSpacing;
            TrayBlockScale = trayBlockScale;
            SlotGap = slotGap;
            TrayHorizontalPadding = trayHorizontalPadding;
            TrayVerticalPadding = trayVerticalPadding;
            TrayGapFromGrid = trayGapFromGrid;
            TrayVerticalOffset = trayVerticalOffset;
            MinTrayScale = minTrayScale;
            HasResponsiveOverride = hasResponsiveOverride;
            ResponsiveWidth = responsiveWidth;
            ResponsiveHeight = responsiveHeight;
            ResponsiveCenter = responsiveCenter;
        }
    }

    public static class TrayLayoutCalculator
    {
        public static TrayLayoutResult Calculate(
            ShapeDefinition[] shapes,
            TrayLayoutConfig config,
            Camera layoutCamera,
            SimpleGridView gridView,
            Vector3[] fallbackSlotPositions,
            Vector3 defaultPosition,
            string reason)
        {
            Vector3[] calculatedSlots = new Vector3[3];
            float baseTrayCellSize = config.BoardCellSize * config.TrayBlockScale;
            float baseTrayCellSpacing = config.BoardCellSpacing * config.TrayBlockScale;
            float cellStep = baseTrayCellSize + baseTrayCellSpacing;
            var extents = new ShapeExtents[3];

            for (int i = 0; i < 3; i++)
            {
                ShapeDefinition shape = (shapes != null && i < shapes.Length) ? shapes[i] : null;
                extents[i] = GetShapeExtents(shape, cellStep, baseTrayCellSize);
            }

            if (config.HasResponsiveOverride)
            {
                float contentWidth = Mathf.Max(0.1f, config.ResponsiveWidth - (config.TrayHorizontalPadding * 2f));
                float contentHeight = Mathf.Max(0.1f, config.ResponsiveHeight - (config.TrayVerticalPadding * 2f));
                float slotWidth = Mathf.Max(0.1f, (contentWidth - (config.SlotGap * 2f)) / 3f);

                float fitScale = 1f;
                for (int i = 0; i < 3; i++)
                {
                    float widthScale = slotWidth / Mathf.Max(0.01f, extents[i].Width);
                    float heightScale = contentHeight / Mathf.Max(0.01f, extents[i].Height);
                    fitScale = Mathf.Min(fitScale, Mathf.Min(widthScale, heightScale));
                }

                float appliedScale = Mathf.Clamp(fitScale, config.MinTrayScale, 1f);
                float leftEdge = config.ResponsiveCenter.x - (contentWidth * 0.5f);
                float firstSlotCenterX = leftEdge + (slotWidth * 0.5f);
                float slotY = config.ResponsiveCenter.y + config.TrayVerticalOffset;

                for (int i = 0; i < 3; i++)
                {
                    float slotCenterX = firstSlotCenterX + (i * (slotWidth + config.SlotGap));
                    float shapeCenterX = (extents[i].Left + extents[i].Right) * 0.5f;
                    float shapeCenterY = (extents[i].Top + extents[i].Bottom) * 0.5f;
                    calculatedSlots[i] = new Vector3(
                        slotCenterX - (shapeCenterX * appliedScale),
                        slotY - (shapeCenterY * appliedScale),
                        0f);
                }

                return new TrayLayoutResult(calculatedSlots, appliedScale, false, reason, null);
            }

            if (layoutCamera == null)
            {
                float fallbackScale = Mathf.Max(config.MinTrayScale, 1f);
                for (int i = 0; i < calculatedSlots.Length; i++)
                    calculatedSlots[i] = fallbackSlotPositions != null && i < fallbackSlotPositions.Length
                        ? fallbackSlotPositions[i]
                        : defaultPosition;

                return new TrayLayoutResult(calculatedSlots, fallbackScale, true, reason, null);
            }

            float totalWidthUnits = 0f;
            float maxTop = float.MinValue;
            float minBottom = float.MaxValue;
            float maxHeightUnits = 0f;

            for (int i = 0; i < 3; i++)
            {
                totalWidthUnits += extents[i].Width;
                maxTop = Mathf.Max(maxTop, extents[i].Top);
                minBottom = Mathf.Min(minBottom, extents[i].Bottom);
                maxHeightUnits = Mathf.Max(maxHeightUnits, extents[i].Height);
            }

            if (totalWidthUnits <= 0f)
                totalWidthUnits = baseTrayCellSize * 3f;

            if (maxHeightUnits <= 0f)
                maxHeightUnits = baseTrayCellSize;

            float cameraHalfWidth = layoutCamera.orthographicSize * layoutCamera.aspect;
            float cameraWidth = cameraHalfWidth * 2f;
            float safeAreaWidth = Mathf.Max(0.1f, cameraWidth - (config.TrayHorizontalPadding * 2f));
            float widthBudgetForBlocks = Mathf.Max(0.1f, safeAreaWidth - (config.SlotGap * 2f));
            float maxScaleByWidth = widthBudgetForBlocks / totalWidthUnits;

            float maxScaleByHeight = float.PositiveInfinity;
            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                Vector3 bottomCell = gridView.GetWorldPosition(0, gridView.Height - 1);
                float gridBottom = bottomCell.y - (gridView.TotalCellSize * 0.5f);
                float cameraBottom = layoutCamera.transform.position.y - layoutCamera.orthographicSize;
                float availableHeight = (gridBottom - config.TrayGapFromGrid) - (cameraBottom + config.TrayVerticalPadding);

                if (availableHeight > 0f)
                    maxScaleByHeight = availableHeight / maxHeightUnits;
                else
                    maxScaleByHeight = 0.1f;
            }

            float worldFitScale = Mathf.Min(maxScaleByWidth, maxScaleByHeight);
            if (float.IsNaN(worldFitScale) || worldFitScale <= 0f)
                worldFitScale = 0.1f;

            float worldAppliedScale = Mathf.Clamp(worldFitScale, config.MinTrayScale, 1f);
            float scaledTotalWidth = (totalWidthUnits * worldAppliedScale) + (config.SlotGap * 2f);
            float safeLeft = layoutCamera.transform.position.x - cameraHalfWidth + config.TrayHorizontalPadding;
            float availableSpace = Mathf.Max(0f, safeAreaWidth - scaledTotalWidth);
            float extraSpacing = availableSpace / 4f;
            float trayY = GetTrayY(layoutCamera, maxTop * worldAppliedScale, minBottom * worldAppliedScale, config, defaultPosition, gridView);

            float currentLeft = safeLeft + extraSpacing;
            for (int i = 0; i < 3; i++)
            {
                float anchorX = currentLeft - (extents[i].Left * worldAppliedScale);
                calculatedSlots[i] = new Vector3(anchorX, trayY, 0f);
                currentLeft += extents[i].Width * worldAppliedScale;
                if (i < 2)
                    currentLeft += config.SlotGap + extraSpacing;
            }

            return new TrayLayoutResult(calculatedSlots, worldAppliedScale, false, reason, layoutCamera);
        }

        public static ShapeExtents GetShapeExtents(ShapeDefinition shape, float cellStep, float baseCellSize)
        {
            float halfCell = baseCellSize * 0.5f;

            if (shape == null || shape.Offsets == null || shape.Offsets.Length == 0)
                return new ShapeExtents(-halfCell, halfCell, halfCell, -halfCell);

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var offset in shape.Offsets)
            {
                minX = Mathf.Min(minX, offset.X);
                maxX = Mathf.Max(maxX, offset.X);
                minY = Mathf.Min(minY, offset.Y);
                maxY = Mathf.Max(maxY, offset.Y);
            }

            float left = (minX * cellStep) - halfCell;
            float right = (maxX * cellStep) + halfCell;
            float top = (-minY * cellStep) + halfCell;
            float bottom = (-maxY * cellStep) - halfCell;

            if (right <= left)
                right = left + baseCellSize;
            if (top <= bottom)
                top = bottom + baseCellSize;

            return new ShapeExtents(left, right, top, bottom);
        }

        private static float GetTrayY(Camera cam, float maxTop, float minBottom, TrayLayoutConfig config, Vector3 defaultPosition, SimpleGridView gridView)
        {
            float cameraBottom = cam.transform.position.y - cam.orthographicSize;
            float minAnchorY = (cameraBottom + config.TrayVerticalPadding) - minBottom;
            float manualOffsetY = defaultPosition.y;

            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                Vector3 bottomCell = gridView.GetWorldPosition(0, gridView.Height - 1);
                float gridBottom = bottomCell.y - (gridView.TotalCellSize * 0.5f);
                float maxAnchorY = (gridBottom - config.TrayGapFromGrid) - maxTop;

                if (maxAnchorY >= minAnchorY)
                    return Mathf.Lerp(minAnchorY, maxAnchorY, 0.5f) + config.TrayVerticalOffset + manualOffsetY;
            }

            return minAnchorY + config.TrayVerticalOffset + manualOffsetY;
        }
    }
}
