// File: Core/Board/ShapeBounds.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Represents the bounding box of a shape defined by its offsets.
    /// Used for optimizing placement search by restricting anchor positions.
    /// </summary>
    public readonly struct ShapeBounds
    {
        /// <summary>
        /// Minimum X offset in the shape.
        /// </summary>
        public readonly int MinDx;
        
        /// <summary>
        /// Maximum X offset in the shape.
        /// </summary>
        public readonly int MaxDx;
        
        /// <summary>
        /// Minimum Y offset in the shape.
        /// </summary>
        public readonly int MinDy;
        
        /// <summary>
        /// Maximum Y offset in the shape.
        /// </summary>
        public readonly int MaxDy;
        
        /// <summary>
        /// Creates shape bounds with the specified values.
        /// </summary>
        /// <param name="minDx">Minimum X offset</param>
        /// <param name="maxDx">Maximum X offset</param>
        /// <param name="minDy">Minimum Y offset</param>
        /// <param name="maxDy">Maximum Y offset</param>
        public ShapeBounds(int minDx, int maxDx, int minDy, int maxDy)
        {
            MinDx = minDx;
            MaxDx = maxDx;
            MinDy = minDy;
            MaxDy = maxDy;
        }
        
        /// <summary>
        /// Computes the bounding box for a shape from its offsets.
        /// </summary>
        /// <param name="offsets">Shape offsets relative to anchor</param>
        /// <returns>Bounding box containing all offsets</returns>
        /// <exception cref="ArgumentNullException">If offsets is null</exception>
        /// <exception cref="ArgumentException">If offsets is empty</exception>
        public static ShapeBounds FromOffsets(IReadOnlyList<Int2> offsets)
        {
            if (offsets == null)
                throw new ArgumentNullException(nameof(offsets));
            if (offsets.Count == 0)
                throw new ArgumentException("Offsets cannot be empty", nameof(offsets));
            
            int minDx = offsets[0].X;
            int maxDx = offsets[0].X;
            int minDy = offsets[0].Y;
            int maxDy = offsets[0].Y;
            
            for (int i = 1; i < offsets.Count; i++)
            {
                Int2 offset = offsets[i];
                
                if (offset.X < minDx) minDx = offset.X;
                if (offset.X > maxDx) maxDx = offset.X;
                if (offset.Y < minDy) minDy = offset.Y;
                if (offset.Y > maxDy) maxDy = offset.Y;
            }
            
            return new ShapeBounds(minDx, maxDx, minDy, maxDy);
        }
        
        /// <summary>
        /// Gets the valid range for anchor X positions on a board.
        /// </summary>
        /// <param name="boardWidth">Width of the board</param>
        /// <returns>Tuple of (minAnchorX, maxAnchorX) inclusive</returns>
        public (int minAnchorX, int maxAnchorX) GetAnchorXRange(int boardWidth)
        {
            int minAnchorX = -MinDx;
            int maxAnchorX = boardWidth - 1 - MaxDx;
            return (minAnchorX, maxAnchorX);
        }
        
        /// <summary>
        /// Gets the valid range for anchor Y positions on a board.
        /// </summary>
        /// <param name="boardHeight">Height of the board</param>
        /// <returns>Tuple of (minAnchorY, maxAnchorY) inclusive</returns>
        public (int minAnchorY, int maxAnchorY) GetAnchorYRange(int boardHeight)
        {
            int minAnchorY = -MinDy;
            int maxAnchorY = boardHeight - 1 - MaxDy;
            return (minAnchorY, maxAnchorY);
        }
        
        public override string ToString()
        {
            return $"ShapeBounds(dx:[{MinDx}, {MaxDx}], dy:[{MinDy}, {MaxDy}])";
        }
    }
}