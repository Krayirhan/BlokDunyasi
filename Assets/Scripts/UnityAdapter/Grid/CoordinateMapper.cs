using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Explicit coordinate mapper that isolates world-to-grid and grid-to-world logic.
    /// 
    /// COORDINATE CONVENTION:
    /// The entire game (BoardState, SimpleGridView, Input, and Shapes) uses a consistent
    /// TOP-LEFT origin system.
    /// - (0,0) is the top-left cell.
    /// - X increases to the right.
    /// - Y increases DOWNWARDS.
    /// </summary>
    public static class CoordinateMapper
    {
        /// <summary>
        /// Converts a logical grid coordinate (where 0,0 is Top-Left) to a Unity World Position.
        /// </summary>
        public static Vector3 GridToWorldPosition(int x, int y, int width, int height, float totalCellSize, Vector3 gridOrigin)
        {
            // Center of the grid is (width-1)/2, (height-1)/2
            // Since y=0 is top, it should be physically higher in world space.
            // Unity's Y goes UP. So y=0 -> positive world Y. y=max -> negative world Y.
            float localX = (x - (width - 1) * 0.5f) * totalCellSize;
            float localY = ((height - 1) * 0.5f - y) * totalCellSize;
            
            return gridOrigin + new Vector3(localX, localY, 0f);
        }

        /// <summary>
        /// Converts a Unity World Position to a logical grid coordinate (where 0,0 is Top-Left).
        /// Returns true if the position falls within the grid bounds.
        /// </summary>
        public static bool WorldToGridPosition(Vector3 worldPos, int width, int height, float totalCellSize, Vector3 gridOrigin, out int x, out int y)
        {
            Vector3 localPos = worldPos - gridOrigin;
            
            x = Mathf.RoundToInt(localPos.x / totalCellSize + (width - 1) * 0.5f);
            y = Mathf.RoundToInt(((height - 1) * 0.5f - localPos.y) / totalCellSize);

            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// Calculates an expanded grid coordinate (allowing positions slightly outside the main grid bounds).
        /// Used for edge snapping in the drag system.
        /// </summary>
        public static void WorldToExpandedGridPosition(Vector3 worldPos, int width, int height, float totalCellSize, Vector3 gridOrigin, out int x, out int y)
        {
            Vector3 localPos = worldPos - gridOrigin;
            
            float gridX = localPos.x / totalCellSize + (width - 1) * 0.5f;
            float gridY = ((height - 1) * 0.5f - localPos.y) / totalCellSize;

            x = Mathf.Clamp(Mathf.RoundToInt(gridX), 0, width - 1);
            y = Mathf.Clamp(Mathf.RoundToInt(gridY), 0, height - 1);
        }
    }
}
