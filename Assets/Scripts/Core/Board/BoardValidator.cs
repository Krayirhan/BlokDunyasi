using System;
using System.Diagnostics;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Debug validation utilities for BoardState integrity.
    /// These methods perform O(width * height) validation by
    /// re-scanning the entire board, so they should only be used for debugging.
    /// </summary>
    public static class BoardValidator
    {
        /// <summary>
        /// Validates that the board's row and column counts match the actual
        /// number of filled cells in each row and column.
        /// </summary>
        public static void ValidateCountsOrThrow(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            
            // Recompute row and column counts from actual board state
            int[] actualRowCounts = new int[board.Height];
            int[] actualColCounts = new int[board.Width];
            
            // Scan all cells and count filled cells per row/column
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsEmpty(x, y))
                    {
                        actualRowCounts[y]++;
                        actualColCounts[x]++;
                    }
                }
            }
            
            // Compare recomputed counts with board's stored counts
            for (int y = 0; y < board.Height; y++)
            {
                int storedRowCount = board.GetRowCount(y);
                int actualRowCount = actualRowCounts[y];
                
                if (storedRowCount != actualRowCount)
                {
                    throw new InvalidOperationException(
                        $"Row count mismatch at row {y}: stored={storedRowCount}, actual={actualRowCount}. " +
                        $"Board state may be corrupted.");
                }
            }
            
            for (int x = 0; x < board.Width; x++)
            {
                int storedColCount = board.GetColCount(x);
                int actualColCount = actualColCounts[x];
                
                if (storedColCount != actualColCount)
                {
                    throw new InvalidOperationException(
                        $"Column count mismatch at column {x}: stored={storedColCount}, actual={actualColCount}. " +
                        $"Board state may be corrupted.");
                }
            }
        }
        
        /// <summary>
        /// Validates all board invariants comprehensively.
        /// This is expensive and should only be used in testing.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ValidateAllInvariants(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            
            // Validate dimensions
            if (board.Width <= 0)
                throw new InvalidOperationException($"Invalid board width: {board.Width}");
            if (board.Height <= 0)
                throw new InvalidOperationException($"Invalid board height: {board.Height}");
            
            // Validate counts
            ValidateCountsOrThrow(board);
        }
        
        /// <summary>
        /// Validates that coordinates are within board bounds.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ValidateCoordinates(BoardState board, int x, int y)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            
            if (x < 0 || x >= board.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x),
                    $"X coordinate {x} is out of bounds for board width {board.Width}");
            }
            
            if (y < 0 || y >= board.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y),
                    $"Y coordinate {y} is out of bounds for board height {board.Height}");
            }
        }
        
        /// <summary>
        /// Checks if the board state is internally consistent (non-throwing version).
        /// </summary>
        public static bool IsValid(BoardState board)
        {
            try
            {
                ValidateCountsOrThrow(board);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets diagnostic information about the board state.
        /// </summary>
        public static string GetDiagnosticInfo(BoardState board)
        {
            if (board == null)
                return "Board is null";
            
            int totalFilled = 0;
            for (int y = 0; y < board.Height; y++)
            {
                totalFilled += board.GetRowCount(y);
            }
            
            int fullRows = 0;
            int fullCols = 0;
            
            for (int y = 0; y < board.Height; y++)
            {
                if (board.GetRowCount(y) == board.Width) fullRows++;
            }
            
            for (int x = 0; x < board.Width; x++)
            {
                if (board.GetColCount(x) == board.Height) fullCols++;
            }
            
            return $"Board {board.Width}x{board.Height}: " +
                   $"{totalFilled} filled cells, " +
                   $"{fullRows} full rows, " +
                   $"{fullCols} full columns";
        }
    }
}
