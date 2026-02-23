using System;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Efficient line detection using pre-computed row/column counts.
    /// Achieves O(width + height) performance by leveraging BoardState's count tracking.
    /// Reuses internal buffer to avoid allocations on repeated calls.
    /// </summary>
    public static class LineDetector
    {
        // Reusable result buffer to avoid allocations
        private static LineDetectResult _cachedResult;
        
        /// <summary>
        /// Detects all full rows and columns on the board.
        /// Performance: O(width + height) due to count-based detection.
        /// Allocation: Zero after first call (reuses internal buffer).
        /// </summary>
        public static LineDetectResult DetectFullLines(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            
            // Initialize or reuse cached result buffer
            if (_cachedResult == null)
            {
                _cachedResult = new LineDetectResult(board.Width, board.Height);
            }
            else
            {
                _cachedResult.Clear();
            }
            
            // Detect full rows: O(height)
            for (int y = 0; y < board.Height; y++)
            {
                // İlk önce stored count kontrol et
                if (board.GetRowCount(y) == board.Width)
                {
                    // Double-check: Gerçekten tüm hücreler dolu mu?
                    int actualCount = 0;
                    for (int x = 0; x < board.Width; x++)
                    {
                        if (!board.IsEmpty(x, y))
                            actualCount++;
                    }
                    
                    // Sadece gerçekten tam dolu olan satırları ekle
                    if (actualCount == board.Width)
                    {
                        _cachedResult.FullRows[_cachedResult.FullRowCount++] = y;
                    }
                }
            }
            
            // Detect full columns: O(width)
            for (int x = 0; x < board.Width; x++)
            {
                // İlk önce stored count kontrol et
                if (board.GetColCount(x) == board.Height)
                {
                    // Double-check: Gerçekten tüm hücreler dolu mu?
                    int actualCount = 0;
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (!board.IsEmpty(x, y))
                            actualCount++;
                    }
                    
                    // Sadece gerçekten tam dolu olan sütunları ekle
                    if (actualCount == board.Height)
                    {
                        _cachedResult.FullColumns[_cachedResult.FullColumnCount++] = x;
                    }
                }
            }
            
            return _cachedResult;
        }
        
        /// <summary>
        /// Checks if any lines are full without allocating result lists.
        /// Useful for quick checks without needing the actual line indices.
        /// </summary>
        public static bool HasAnyFullLines(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            
            // Check rows - double-check actual cell count
            for (int y = 0; y < board.Height; y++)
            {
                if (board.GetRowCount(y) == board.Width)
                {
                    // Verify actual count
                    int actualCount = 0;
                    for (int x = 0; x < board.Width; x++)
                    {
                        if (!board.IsEmpty(x, y))
                            actualCount++;
                    }
                    
                    if (actualCount == board.Width)
                        return true;
                }
            }
            
            // Check columns - double-check actual cell count
            for (int x = 0; x < board.Width; x++)
            {
                if (board.GetColCount(x) == board.Height)
                {
                    // Verify actual count
                    int actualCount = 0;
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (!board.IsEmpty(x, y))
                            actualCount++;
                    }
                    
                    if (actualCount == board.Height)
                        return true;
                }
            }
            
            return false;
        }
    }
}
