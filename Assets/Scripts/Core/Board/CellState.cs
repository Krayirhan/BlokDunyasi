using System;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Represents the state of a single cell in the game board.
    /// Uses struct for performance with large arrays.
    /// 
    /// Invariants:
    /// - When BlockId=0: cell is empty
    /// - When BlockId>0: cell is filled with colorId>0
    /// </summary>
    [Serializable]
    public struct CellState : IEquatable<CellState>
    {
        /// <summary>
        /// ID of the block type placed in this cell (0 if empty).
        /// </summary>
        public readonly int BlockId;
        
        /// <summary>
        /// Color ID of the block placed in this cell (0 if empty).
        /// </summary>
        public readonly int ColorId;
        
        /// <summary>
        /// Whether this cell is empty (no block placed).
        /// </summary>
        public bool IsEmpty => BlockId == 0;
        
        /// <summary>
        /// Creates a filled cell with the specified block and color.
        /// </summary>
        /// <param name="blockId">Block type ID (must be > 0)</param>
        /// <param name="colorId">Color ID (must be > 0)</param>
        public CellState(int blockId, int colorId)
        {
            if (blockId <= 0) throw new ArgumentException("BlockId must be positive for filled cells");
            if (colorId <= 0) throw new ArgumentException("ColorId must be positive for filled cells");
            
            BlockId = blockId;
            ColorId = colorId;
        }
        
        /// <summary>
        /// Predefined empty cell constant.
        /// Default struct values: IsEmpty=false, BlockId=0, ColorId=0
        /// We treat BlockId=0 as empty.
        /// </summary>
        public static readonly CellState Empty = default;
        
        /// <summary>
        /// Creates a filled cell.
        /// </summary>
        public static CellState Filled(int blockId, int colorId) => new CellState(blockId, colorId);
        
        public bool Equals(CellState other)
        {
            return BlockId == other.BlockId && 
                   ColorId == other.ColorId;
        }
        
        public override bool Equals(object obj) => obj is CellState other && Equals(other);
        
        public override int GetHashCode()
        {
            return HashCode.Combine(BlockId, ColorId);
        }
        
        public static bool operator ==(CellState left, CellState right) => left.Equals(right);
        public static bool operator !=(CellState left, CellState right) => !left.Equals(right);
        
        public override string ToString()
        {
            return IsEmpty ? "Empty" : $"Block({BlockId}, Color:{ColorId})";
        }
    }
}