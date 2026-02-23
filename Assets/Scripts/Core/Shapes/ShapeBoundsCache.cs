// File: Core/Shapes/ShapeBoundsCache.cs
using System;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Shapes
{
    /// <summary>
    /// Cached bounding box information for a shape.
    /// Computed once to optimize placement search operations.
    /// 
    /// Unity-friendly: immutable value type, no heap allocations after creation.
    /// </summary>
    public readonly struct ShapeBoundsCache
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
        /// Width of the shape's bounding box.
        /// </summary>
        public int Width => MaxDx - MinDx + 1;
        
        /// <summary>
        /// Height of the shape's bounding box.
        /// </summary>
        public int Height => MaxDy - MinDy + 1;
        
        /// <summary>
        /// Creates a shape bounds cache with the specified bounds.
        /// </summary>
        /// <param name="minDx">Minimum X offset</param>
        /// <param name="maxDx">Maximum X offset</param>
        /// <param name="minDy">Minimum Y offset</param>
        /// <param name="maxDy">Maximum Y offset</param>
        private ShapeBoundsCache(int minDx, int maxDx, int minDy, int maxDy)
        {
            MinDx = minDx;
            MaxDx = maxDx;
            MinDy = minDy;
            MaxDy = maxDy;
        }
        
        /// <summary>
        /// Creates a bounds cache for the specified shape.
        /// Computes the bounding box of all offsets.
        /// </summary>
        /// <param name="shape">Shape to compute bounds for</param>
        /// <returns>Cached bounds information</returns>
        /// <exception cref="ArgumentNullException">If shape is null</exception>
        public static ShapeBoundsCache Create(ShapeDefinition shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            
            var offsets = shape.Offsets;
            if (offsets.Length == 0)
                throw new ArgumentException("Shape must have at least one offset");
            
            // Initialize with first offset
            int minDx = offsets[0].X;
            int maxDx = offsets[0].X;
            int minDy = offsets[0].Y;
            int maxDy = offsets[0].Y;
            
            // Find min/max bounds
            for (int i = 1; i < offsets.Length; i++)
            {
                Int2 offset = offsets[i];
                
                if (offset.X < minDx) minDx = offset.X;
                if (offset.X > maxDx) maxDx = offset.X;
                if (offset.Y < minDy) minDy = offset.Y;
                if (offset.Y > maxDy) maxDy = offset.Y;
            }
            
            return new ShapeBoundsCache(minDx, maxDx, minDy, maxDy);
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
            return $"Bounds(dx:[{MinDx}, {MaxDx}], dy:[{MinDy}, {MaxDy}], size:{Width}x{Height})";
        }
    }
}