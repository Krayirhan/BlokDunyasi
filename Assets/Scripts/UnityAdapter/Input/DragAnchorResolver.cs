using UnityEngine;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Grid;

namespace BlockPuzzle.UnityAdapter.Input
{
    /// <summary>
    /// Handles geometry and grid proximity calculations for the drag system.
    /// Determines which logical grid anchor a given world position corresponds to.
    /// </summary>
    public class DragAnchorResolver
    {
        private readonly SimpleGridView _gridView;
        private readonly float _edgeSnapMargin;
        private readonly float _snapStickiness;

        public DragAnchorResolver(SimpleGridView gridView, float edgeSnapMargin = 0.18f, float snapStickiness = 0.03f)
        {
            _gridView = gridView;
            _edgeSnapMargin = edgeSnapMargin;
            _snapStickiness = snapStickiness;
        }

        public bool TryResolveAnchor(Vector2 pointerWorldPos, bool hasStableAnchor, Int2 currentAnchor, out Int2 resolvedAnchor)
        {
            resolvedAnchor = default;

            if (_gridView == null)
                return false;

            int gx, gy;
            if (!_gridView.GetGridPosition(pointerWorldPos, out gx, out gy) &&
                !TryGetExpandedGridAnchor(pointerWorldPos, out gx, out gy))
            {
                return false;
            }

            var candidate = new Int2(gx, gy);
            if (hasStableAnchor && ShouldKeepCurrentAnchor(pointerWorldPos, currentAnchor, candidate))
            {
                resolvedAnchor = currentAnchor;
                return true;
            }

            resolvedAnchor = candidate;
            return true;
        }

        private bool TryGetExpandedGridAnchor(Vector2 pointerWorldPos, out int gx, out int gy)
        {
            gx = 0;
            gy = 0;

            if (_gridView == null || !IsInsideExpandedBoardBounds(pointerWorldPos, _edgeSnapMargin))
                return false;

            float totalSize = _gridView.TotalCellSize;
            if (totalSize <= 0f)
                return false;

            Vector3 origin = _gridView.transform.position + (Vector3)_gridView.GridOffset;
            CoordinateMapper.WorldToExpandedGridPosition(pointerWorldPos, _gridView.Width, _gridView.Height, totalSize, origin, out gx, out gy);
            return true;
        }

        public bool IsNearAnchor(Vector2 worldPos, Int2 anchor, float distanceInCells)
        {
            if (_gridView == null)
                return false;

            float totalCellSize = _gridView.TotalCellSize;
            float maxDistance = totalCellSize * Mathf.Max(0f, distanceInCells);
            if (maxDistance <= 0f)
                return false;

            Vector3 anchorWorldPos = _gridView.GetWorldPosition(anchor.X, anchor.Y);
            return Vector2.Distance(worldPos, anchorWorldPos) <= maxDistance;
        }

        public bool IsInsideExpandedBoardBounds(Vector2 worldPos, float marginInCells)
        {
            if (_gridView == null || _gridView.Width <= 0 || _gridView.Height <= 0)
                return false;

            Vector3 topLeft = _gridView.GetWorldPosition(0, 0);
            Vector3 bottomRight = _gridView.GetWorldPosition(_gridView.Width - 1, _gridView.Height - 1);
            float halfCell = _gridView.TotalCellSize * 0.5f;
            float margin = _gridView.TotalCellSize * Mathf.Max(0f, marginInCells);

            // Since y increases downwards, TopLeft.y > BottomRight.y
            float minX = topLeft.x - halfCell - margin;
            float maxX = bottomRight.x + halfCell + margin;
            float maxY = topLeft.y + halfCell + margin;
            float minY = bottomRight.y - halfCell - margin;

            return worldPos.x >= minX && worldPos.x <= maxX &&
                   worldPos.y >= minY && worldPos.y <= maxY;
        }

        private bool ShouldKeepCurrentAnchor(Vector2 pointerWorldPos, Int2 currentAnchor, Int2 candidate)
        {
            if (candidate == currentAnchor || _gridView == null)
                return false;

            float totalCellSize = _gridView.TotalCellSize;
            if (totalCellSize <= 0f)
                return false;

            Vector3 currentAnchorWorld = _gridView.GetWorldPosition(currentAnchor.X, currentAnchor.Y);
            float stickyHalfExtent = (totalCellSize * 0.5f) + (totalCellSize * _snapStickiness);

            return Mathf.Abs(pointerWorldPos.x - currentAnchorWorld.x) <= stickyHalfExtent &&
                   Mathf.Abs(pointerWorldPos.y - currentAnchorWorld.y) <= stickyHalfExtent;
        }
    }
}
