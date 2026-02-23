using System;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Static coordinate utilities for board operations.
    /// Provides allocation-free coordinate conversion and enumeration utilities.
    /// 
    /// Grid coordinate system:
    /// - Origin: bottom-left (0,0)
    /// - X axis: increases to the right
    /// - Y axis: increases upward
    /// - Board size: width x height
    /// </summary>
    public static class BoardCoord
    {
        /// <summary>
        /// Converts 2D board coordinates to 1D array index.
        /// Formula: index = y * width + x
        /// </summary>
        public static int ToIndex(int x, int y, int width, int height)
        {
            if (x < 0 || x >= width)
                throw new ArgumentOutOfRangeException(nameof(x), $"X coordinate {x} is out of range [0, {width - 1}]");
            if (y < 0 || y >= height)
                throw new ArgumentOutOfRangeException(nameof(y), $"Y coordinate {y} is out of range [0, {height - 1}]");
            
            return y * width + x;
        }
        
        /// <summary>
        /// Converts 1D array index to 2D board coordinates.
        /// Formula: x = index % width, y = index / width
        /// </summary>
        public static void FromIndex(int index, int width, int height, out int x, out int y)
        {
            int maxIndex = width * height - 1;
            if (index < 0 || index > maxIndex)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range [0, {maxIndex}]");
            
            x = index % width;
            y = index / width;
        }
        
        /// <summary>
        /// Checks if the specified coordinates are within board bounds.
        /// </summary>
        public static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        /// <summary>
        /// Enumerates all indices in a row using a callback (allocation-free).
        /// </summary>
        public static void EnumerateRow(int y, int width, int height, Action<int> visitIndex)
        {
            if (y < 0 || y >= height)
                throw new ArgumentOutOfRangeException(nameof(y), $"Row {y} is out of range [0, {height - 1}]");
            if (visitIndex == null)
                throw new ArgumentNullException(nameof(visitIndex));
            
            int startIndex = y * width;
            for (int x = 0; x < width; x++)
            {
                visitIndex(startIndex + x);
            }
        }
        
        /// <summary>
        /// Enumerates all indices in a column using a callback (allocation-free).
        /// </summary>
        public static void EnumerateColumn(int x, int width, int height, Action<int> visitIndex)
        {
            if (x < 0 || x >= width)
                throw new ArgumentOutOfRangeException(nameof(x), $"Column {x} is out of range [0, {width - 1}]");
            if (visitIndex == null)
                throw new ArgumentNullException(nameof(visitIndex));
            
            for (int y = 0; y < height; y++)
            {
                visitIndex(y * width + x);
            }
        }
    }
}
