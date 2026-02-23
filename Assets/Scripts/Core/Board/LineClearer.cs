using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Result of a line clearing operation.
    /// </summary>
    public readonly struct ClearResult
    {
        /// <summary>
        /// Total number of cells that were cleared.
        /// </summary>
        public readonly int ClearedCellCount;
        
        /// <summary>
        /// Positions of all cleared cells (for animation purposes).
        /// </summary>
        public readonly IReadOnlyList<Int2> ClearedPositions;
        
        public ClearResult(int clearedCellCount, IReadOnlyList<Int2> clearedPositions)
        {
            ClearedCellCount = clearedCellCount;
            ClearedPositions = clearedPositions ?? throw new ArgumentNullException(nameof(clearedPositions));
        }
        
        /// <summary>
        /// Empty result when no lines were cleared.
        /// </summary>
        public static readonly ClearResult Empty = new ClearResult(0, new Int2[0]);
    }
    
    /// <summary>
    /// Handles clearing of full rows and columns from the board.
    /// Uses a temporary boolean array to mark cells for clearing,
    /// ensuring cells at row/column intersections are only cleared once.
    /// </summary>
    public static class LineClearer
    {
        /// <summary>
        /// Clears all cells in the specified full rows and columns.
        /// Cells that appear in both a full row and full column
        /// are marked once and cleared once to ensure proper count management.
        /// </summary>
        public static ClearResult ClearLines(
            BoardState board,
            IReadOnlyList<int> fullRows,
            IReadOnlyList<int> fullCols)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (fullRows == null)
                throw new ArgumentNullException(nameof(fullRows));
            if (fullCols == null)
                throw new ArgumentNullException(nameof(fullCols));
            
            // Early exit if nothing to clear
            if (fullRows.Count == 0 && fullCols.Count == 0)
            {
                return ClearResult.Empty;
            }
            
            // Use boolean array to mark cells for clearing (avoids duplicates)
            bool[] cellsToClear = new bool[board.Width * board.Height];
            
            // Mark all cells in full rows
            for (int rowIndex = 0; rowIndex < fullRows.Count; rowIndex++)
            {
                int y = fullRows[rowIndex];
                if (y < 0 || y >= board.Height)
                    throw new ArgumentOutOfRangeException(nameof(fullRows), $"Row {y} is out of bounds");
                
                for (int x = 0; x < board.Width; x++)
                {
                    int index = board.ToIndex(x, y);
                    cellsToClear[index] = true;
                }
            }
            
            // Mark all cells in full columns
            for (int colIndex = 0; colIndex < fullCols.Count; colIndex++)
            {
                int x = fullCols[colIndex];
                if (x < 0 || x >= board.Width)
                    throw new ArgumentOutOfRangeException(nameof(fullCols), $"Column {x} is out of bounds");
                
                for (int y = 0; y < board.Height; y++)
                {
                    int index = board.ToIndex(x, y);
                    cellsToClear[index] = true;
                }
            }
            
            // Clear marked cells and collect positions
            var clearedPositions = new List<Int2>();
            
            for (int index = 0; index < cellsToClear.Length; index++)
            {
                if (cellsToClear[index])
                {
                    board.FromIndex(index, out int x, out int y);
                    
                    // Only clear if the cell is actually filled
                    if (!board.IsEmpty(x, y))
                    {
                        board.ClearCell(x, y);
                        clearedPositions.Add(new Int2(x, y));
                    }
                }
            }
            
            return new ClearResult(clearedPositions.Count, clearedPositions.ToArray());
        }
    }
}
