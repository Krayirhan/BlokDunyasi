using System;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Represents the current state of the game board.
    /// Uses 1D array storage for performance with row/column count tracking.
    /// 
    /// Grid origin: bottom-left (0,0), x increases right, y increases up.
    /// 
    /// Invariants:
    /// - _rowCounts[y] == number of filled cells in row y
    /// - _colCounts[x] == number of filled cells in column x
    /// - _cells.Length == Width * Height
    /// </summary>
    public class BoardState
    {
        public int Width { get; }
        public int Height { get; }
        
        private CellState[] _cells;     // length = Width * Height
        private int[] _rowCounts;       // length = Height
        private int[] _colCounts;       // length = Width
        
        /// <summary>
        /// Creates a new empty board with the specified dimensions.
        /// </summary>
        /// <param name="width">Board width (default 8)</param>
        /// <param name="height">Board height (default 8)</param>
        public BoardState(int width = 8, int height = 8)
        {
            if (width <= 0) throw new ArgumentException("Width must be positive");
            if (height <= 0) throw new ArgumentException("Height must be positive");
            
            Width = width;
            Height = height;
            
            _cells = new CellState[width * height];
            _rowCounts = new int[height];
            _colCounts = new int[width];
            
            // Initialize all cells as empty
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = CellState.Empty;
            }
        }
        
        /// <summary>
        /// Gets the cell state at the specified 1D index.
        /// </summary>
        /// <param name="index">1D array index</param>
        /// <returns>Cell state</returns>
        public CellState GetCellByIndex(int index)
        {
            if (index < 0 || index >= _cells.Length)
                throw new ArgumentOutOfRangeException($"Index {index} is out of bounds for board size {Width}x{Height}");
            
            return _cells[index];
        }
        
        /// <summary>
        /// Gets the cell state at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate (0 to Width-1)</param>
        /// <param name="y">Y coordinate (0 to Height-1)</param>
        /// <returns>Cell state</returns>
        public CellState GetCell(int x, int y)
        {
            if (!IsInBounds(x, y))
                throw new ArgumentOutOfRangeException($"Position ({x},{y}) is out of bounds for {Width}x{Height} board");
            
            int index = ToIndex(x, y);
            return _cells[index];
        }
        
        /// <summary>
        /// Checks if a cell is empty at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if cell is empty</returns>
        public bool IsEmpty(int x, int y)
        {
            return GetCell(x, y).IsEmpty;
        }
        
        /// <summary>
        /// Checks if a cell is occupied at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if cell is occupied (not empty)</returns>
        public bool IsOccupied(int x, int y)
        {
            return !IsEmpty(x, y);
        }
        
        /// <summary>
        /// Checks if coordinates are within board bounds.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
        
        /// <summary>
        /// Fills a cell with the specified block and color (internal use only).
        /// Throws if cell is already filled.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="blockId">Block ID (must be > 0)</param>
        /// <param name="colorId">Color ID (must be > 0)</param>
        internal void FillCell(int x, int y, int blockId, int colorId)
        {
            if (!IsInBounds(x, y))
                throw new ArgumentOutOfRangeException($"Position ({x},{y}) is out of bounds");
            
            int index = ToIndex(x, y);
            CellState currentCell = _cells[index];
            
            if (!currentCell.IsEmpty)
                throw new InvalidOperationException($"Cell at ({x},{y}) is already filled");
            
            _cells[index] = CellState.Filled(blockId, colorId);
            
            // Update counts (cell was empty, now filled)
            _rowCounts[y]++;
            _colCounts[x]++;
        }
        
        /// <summary>
        /// Clears a cell (sets to empty) (internal use only).
        /// Safe to call on already empty cells (no-op).
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        internal void ClearCell(int x, int y)
        {
            if (!IsInBounds(x, y))
                throw new ArgumentOutOfRangeException($"Position ({x},{y}) is out of bounds");
            
            int index = ToIndex(x, y);
            CellState currentCell = _cells[index];
            
            // Safe no-op: clearing an empty cell does nothing
            if (currentCell.IsEmpty)
                return;
            
            _cells[index] = CellState.Empty;
            
            // Update counts (cell was filled, now empty)
            _rowCounts[y]--;
            _colCounts[x]--;
        }
        
        /// <summary>
        /// Gets the number of filled cells in the specified row.
        /// </summary>
        /// <param name="y">Row index (0 to Height-1)</param>
        /// <returns>Number of filled cells in the row</returns>
        public int GetRowCount(int y)
        {
            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y), $"Row {y} is out of range [0, {Height - 1}]");
            
            return _rowCounts[y];
        }
        
        /// <summary>
        /// Gets the number of filled cells in the specified column.
        /// </summary>
        /// <param name="x">Column index (0 to Width-1)</param>
        /// <returns>Number of filled cells in the column</returns>
        public int GetColCount(int x)
        {
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x), $"Column {x} is out of range [0, {Width - 1}]");
            
            return _colCounts[x];
        }
        
        /// <summary>
        /// Converts 2D coordinates to 1D array index.
        /// Origin: bottom-left (0,0), x right, y up.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>1D array index</returns>
        internal int ToIndex(int x, int y)
        {
            return y * Width + x;
        }
        
        /// <summary>
        /// Converts 1D array index to 2D coordinates.
        /// </summary>
        /// <param name="index">1D array index</param>
        /// <param name="x">Output X coordinate</param>
        /// <param name="y">Output Y coordinate</param>
        internal void FromIndex(int index, out int x, out int y)
        {
            x = index % Width;
            y = index / Width;
        }
        
        /// <summary>
        /// Clears all cells and resets the board to empty state.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = CellState.Empty;
            }
            
            for (int i = 0; i < _rowCounts.Length; i++)
            {
                _rowCounts[i] = 0;
            }
            
            for (int i = 0; i < _colCounts.Length; i++)
            {
                _colCounts[i] = 0;
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this board state.
        /// </summary>
        /// <returns>New BoardState with copied data</returns>
        public BoardState Clone()
        {
            var clone = new BoardState(Width, Height);
            Array.Copy(_cells, clone._cells, _cells.Length);
            Array.Copy(_rowCounts, clone._rowCounts, _rowCounts.Length);
            Array.Copy(_colCounts, clone._colCounts, _colCounts.Length);
            return clone;
        }
        
        /// <summary>
        /// Gets a copy of all cell states.
        /// </summary>
        /// <returns>Array of CellState for persistence</returns>
        public CellState[] GetCells()
        {
            var copy = new CellState[_cells.Length];
            Array.Copy(_cells, copy, _cells.Length);
            return copy;
        }
        
        /// <summary>
        /// Sets all cells from an array (for deserialization).
        /// </summary>
        /// <param name="cells">Cell array to copy from</param>
        public void SetCells(CellState[] cells)
        {
            if (cells == null || cells.Length != _cells.Length)
                throw new ArgumentException($"Cells array must have length {_cells.Length}");
            
            Array.Copy(cells, _cells, _cells.Length);
            RecalculateCounts();
        }
        
        /// <summary>
        /// Recalculates row and column counts from cell data.
        /// </summary>
        private void RecalculateCounts()
        {
            Array.Clear(_rowCounts, 0, _rowCounts.Length);
            Array.Clear(_colCounts, 0, _colCounts.Length);
            
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!GetCell(x, y).IsEmpty)
                    {
                        _rowCounts[y]++;
                        _colCounts[x]++;
                    }
                }
            }
        }
        
#if DEBUG
        /// <summary>
        /// Validates that row/column counts are consistent with actual cell states.
        /// Throws if validation fails (debug builds only).
        /// </summary>
        internal void ValidateCountsOrThrow()
        {
            // Recompute counts from scratch
            int[] actualRowCounts = new int[Height];
            int[] actualColCounts = new int[Width];
            
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!GetCell(x, y).IsEmpty)
                    {
                        actualRowCounts[y]++;
                        actualColCounts[x]++;
                    }
                }
            }
            
            // Compare with stored counts
            for (int y = 0; y < Height; y++)
            {
                if (_rowCounts[y] != actualRowCounts[y])
                {
                    throw new InvalidOperationException(
                        $"Row count mismatch at row {y}: stored={_rowCounts[y]}, actual={actualRowCounts[y]}");
                }
            }
            
            for (int x = 0; x < Width; x++)
            {
                if (_colCounts[x] != actualColCounts[x])
                {
                    throw new InvalidOperationException(
                        $"Column count mismatch at column {x}: stored={_colCounts[x]}, actual={actualColCounts[x]}");
                }
            }
        }
#else
        /// <summary>
        /// Validates that row/column counts are consistent (no-op in release builds).
        /// </summary>
        internal void ValidateCountsOrThrow()
        {
            // No validation in release builds for performance
        }
#endif
    }
}