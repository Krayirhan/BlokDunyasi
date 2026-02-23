// File: Core/Board/PlacementSearchResult.cs
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Result of a placement search operation.
    /// Indicates whether a valid placement was found and provides the details.
    /// </summary>
    public readonly struct PlacementSearchResult
    {
        /// <summary>
        /// Whether a valid placement was found.
        /// </summary>
        public readonly bool HasPlacement;
        
        /// <summary>
        /// Index of the shape that can be placed (valid only if HasPlacement is true).
        /// </summary>
        public readonly int ShapeIndex;
        
        /// <summary>
        /// Anchor position where the shape can be placed (valid only if HasPlacement is true).
        /// </summary>
        public readonly Int2 Anchor;
        
        /// <summary>
        /// Creates a successful search result.
        /// </summary>
        /// <param name="shapeIndex">Index of the placeable shape</param>
        /// <param name="anchor">Anchor position for placement</param>
        public PlacementSearchResult(int shapeIndex, Int2 anchor)
        {
            HasPlacement = true;
            ShapeIndex = shapeIndex;
            Anchor = anchor;
        }
        
        /// <summary>
        /// Result indicating no valid placement was found.
        /// </summary>
        public static readonly PlacementSearchResult None = default(PlacementSearchResult);
        
        public override string ToString()
        {
            return HasPlacement 
                ? $"Placement(Shape:{ShapeIndex}, Anchor:{Anchor})"
                : "NoPlacement";
        }
    }
}