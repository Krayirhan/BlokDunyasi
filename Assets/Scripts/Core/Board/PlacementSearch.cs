// File: Core/Board/PlacementSearch.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Optimized search engine for finding valid shape placements on the board.
    /// Used for game-over detection and placement validation.
    /// 
    /// Complexity:
    /// - Without bounds optimization: O(shapes * board_area * shape_size)
    /// - With bounds optimization: O(shapes * valid_anchor_positions * shape_size)
    /// 
    /// The bounds optimization significantly reduces the search space by only
    /// checking anchor positions where the shape could theoretically fit.
    /// </summary>
    public static class PlacementSearch
    {
        /// <summary>
        /// Checks if any of the given shapes can be placed anywhere on the board.
        /// Early exits on the first valid placement found.
        /// 
        /// This is optimized for game-over detection where we only need to know
        /// if ANY placement is possible.
        /// </summary>
        /// <param name="board">Board to search on</param>
        /// <param name="shapes">Collection of shapes to test</param>
        /// <returns>True if any shape can be placed anywhere on the board</returns>
        /// <exception cref="ArgumentNullException">If board or shapes is null</exception>
        public static bool HasAnyValidPlacement(BoardState board, IReadOnlyList<IReadOnlyList<Int2>> shapes)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (shapes == null)
                throw new ArgumentNullException(nameof(shapes));
            
            // Try each shape
            for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                var shapeOffsets = shapes[shapeIndex];
                if (shapeOffsets == null || shapeOffsets.Count == 0)
                    continue;
                
                // Compute bounds for this shape to optimize search space
                ShapeBounds bounds = ShapeBounds.FromOffsets(shapeOffsets);
                (int minAx, int maxAx) = bounds.GetAnchorXRange(board.Width);
                (int minAy, int maxAy) = bounds.GetAnchorYRange(board.Height);
                
                // Skip shape if it can't fit on the board at all
                if (minAx > maxAx || minAy > maxAy)
                    continue;
                
                // Search all valid anchor positions for this shape
                for (int ay = minAy; ay <= maxAy; ay++)
                {
                    for (int ax = minAx; ax <= maxAx; ax++)
                    {
                        if (PlacementEngine.CanPlace(board, ax, ay, shapeOffsets) == PlacementResult.Success)
                        {
                            return true; // Early exit - found valid placement
                        }
                    }
                }
            }
            
            return false; // No valid placements found
        }
        
        /// <summary>
        /// Checks if a single shape has any valid placement positions on the board.
        /// Early exits on the first valid position found.
        /// </summary>
        /// <param name="board">Board to search on</param>
        /// <param name="shape">Single shape to test</param>
        /// <returns>True if the shape can be placed anywhere on the board</returns>
        /// <exception cref="ArgumentNullException">If board or shape is null</exception>
        public static bool HasAnyValidPlacement(BoardState board, ShapeDefinition shape)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (shape?.Offsets == null)
                return false;
                
            // Compute bounds for this shape to optimize search space
            ShapeBounds bounds = ShapeBounds.FromOffsets(shape.Offsets);
            (int minAx, int maxAx) = bounds.GetAnchorXRange(board.Width);
            (int minAy, int maxAy) = bounds.GetAnchorYRange(board.Height);
            
            // Skip shape if it can't fit on the board at all
            if (minAx > maxAx || minAy > maxAy)
                return false;
                
            // Search all valid anchor positions for this shape
            for (int ay = minAy; ay <= maxAy; ay++)
            {
                for (int ax = minAx; ax <= maxAx; ax++)
                {
                    if (PlacementEngine.CanPlace(board, ax, ay, shape.Offsets) == PlacementResult.Success)
                    {
                        return true; // Early exit - found valid placement
                    }
                }
            }
            
            return false; // No valid placements found
        }
        
        /// <summary>
        /// Finds the first valid placement for any of the given shapes.
        /// Returns detailed information about the placement found.
        /// 
        /// Search order: shapes in list order, then anchor positions top-to-bottom, left-to-right.
        /// </summary>
        /// <param name="board">Board to search on</param>
        /// <param name="shapes">Collection of shapes to test</param>
        /// <returns>Search result with placement details or None if no placement found</returns>
        /// <exception cref="ArgumentNullException">If board or shapes is null</exception>
        public static PlacementSearchResult FindFirstValidPlacement(BoardState board, IReadOnlyList<IReadOnlyList<Int2>> shapes)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (shapes == null)
                throw new ArgumentNullException(nameof(shapes));
            
            // Try each shape
            for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                var shapeOffsets = shapes[shapeIndex];
                if (shapeOffsets == null || shapeOffsets.Count == 0)
                    continue;
                
                // Compute bounds for this shape to optimize search space
                ShapeBounds bounds = ShapeBounds.FromOffsets(shapeOffsets);
                (int minAx, int maxAx) = bounds.GetAnchorXRange(board.Width);
                (int minAy, int maxAy) = bounds.GetAnchorYRange(board.Height);
                
                // Skip shape if it can't fit on the board at all
                if (minAx > maxAx || minAy > maxAy)
                    continue;
                
                // Search all valid anchor positions for this shape
                // Search top-to-bottom, left-to-right for consistent behavior
                for (int ay = maxAy; ay >= minAy; ay--)
                {
                    for (int ax = minAx; ax <= maxAx; ax++)
                    {
                        if (PlacementEngine.CanPlace(board, ax, ay, shapeOffsets) == PlacementResult.Success)
                        {
                            return new PlacementSearchResult(shapeIndex, new Int2(ax, ay));
                        }
                    }
                }
            }
            
            return PlacementSearchResult.None; // No valid placements found
        }
        
        /// <summary>
        /// Finds all valid placement positions for a single shape on the board.
        /// </summary>
        /// <param name="board">Board to search on</param>
        /// <param name="shape">Shape to find placements for</param>
        /// <returns>Array of valid anchor positions</returns>
        /// <exception cref="ArgumentNullException">If board or shape is null</exception>
        public static Int2[] FindValidPlacements(BoardState board, ShapeDefinition shape)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (shape?.Offsets == null)
                return new Int2[0];
                
            var validPlacements = new List<Int2>();
            
            // Compute bounds for this shape to optimize search space
            ShapeBounds bounds = ShapeBounds.FromOffsets(shape.Offsets);
            (int minAx, int maxAx) = bounds.GetAnchorXRange(board.Width);
            (int minAy, int maxAy) = bounds.GetAnchorYRange(board.Height);
            
            // Skip shape if it can't fit on the board at all
            if (minAx > maxAx || minAy > maxAy)
                return validPlacements.ToArray();
                
            // Search all valid anchor positions for this shape
            for (int ay = minAy; ay <= maxAy; ay++)
            {
                for (int ax = minAx; ax <= maxAx; ax++)
                {
                    if (PlacementEngine.CanPlace(board, ax, ay, shape.Offsets) == PlacementResult.Success)
                    {
                        validPlacements.Add(new Int2(ax, ay));
                    }
                }
            }
            
            return validPlacements.ToArray();
        }
    }
}