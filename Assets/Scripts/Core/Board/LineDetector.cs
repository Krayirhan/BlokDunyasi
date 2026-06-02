using System;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Efficient line detection using pre-computed row/column counts.
    /// Achieves O(width + height) performance by leveraging BoardState's count tracking.
    /// </summary>
    public static class LineDetector
    {
        /// <summary>
        /// Detects all full rows and columns on the board.
        /// Performance: O(width + height) due to count-based detection.
        /// Allocation: creates a fresh result object for the caller.
        /// </summary>
        public static LineDetectResult DetectFullLines(BoardState board)
        {
            var result = new LineDetectResult(board?.Width ?? 0, board?.Height ?? 0);
            DetectFullLines(board, result);
            return result;
        }

        /// <summary>
        /// Detects all full rows and columns into a caller-owned reusable result.
        /// Performance: O(width + height) due to count-based detection.
        /// Allocation: zero when the provided result is already large enough.
        /// </summary>
        public static void DetectFullLines(BoardState board, LineDetectResult result)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            result.EnsureCapacity(board.Width, board.Height);
            result.Clear();

            for (int y = 0; y < board.Height; y++)
            {
                if (IsRowActuallyFull(board, y))
                    result.FullRows[result.FullRowCount++] = y;
            }

            for (int x = 0; x < board.Width; x++)
            {
                if (IsColumnActuallyFull(board, x))
                    result.FullColumns[result.FullColumnCount++] = x;
            }
        }

        /// <summary>
        /// Checks if any lines are full without creating a result object.
        /// Useful for quick checks without needing the actual line indices.
        /// </summary>
        public static bool HasAnyFullLines(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            for (int y = 0; y < board.Height; y++)
            {
                if (IsRowActuallyFull(board, y))
                    return true;
            }

            for (int x = 0; x < board.Width; x++)
            {
                if (IsColumnActuallyFull(board, x))
                    return true;
            }

            return false;
        }

        private static bool IsRowActuallyFull(BoardState board, int y)
        {
            if (board.GetRowCount(y) != board.Width)
                return false;

            int actualCount = 0;
            for (int x = 0; x < board.Width; x++)
            {
                if (!board.IsEmpty(x, y))
                    actualCount++;
            }

            return actualCount == board.Width;
        }

        private static bool IsColumnActuallyFull(BoardState board, int x)
        {
            if (board.GetColCount(x) != board.Height)
                return false;

            int actualCount = 0;
            for (int y = 0; y < board.Height; y++)
            {
                if (!board.IsEmpty(x, y))
                    actualCount++;
            }

            return actualCount == board.Height;
        }
    }
}
