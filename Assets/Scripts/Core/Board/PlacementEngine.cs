using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Result of a placement validation or placement attempt.
    /// </summary>
    public enum PlacementResult
    {
        /// <summary>
        /// Placement is valid and successful.
        /// </summary>
        Success,
        
        /// <summary>
        /// One or more target cells are out of bounds.
        /// </summary>
        OutOfBounds,
        
        /// <summary>
        /// One or more target cells are already occupied.
        /// </summary>
        Collision
    }
    
    /// <summary>
    /// Static engine for shape placement validation and atomic placement operations.
    /// PlaceAtomic is truly atomic: either all cells are filled or none are modified.
    /// </summary>
    public static class PlacementEngine
    {
        /// <summary>
        /// Validates whether a shape can be placed at the given anchor position.
        /// </summary>
        public static PlacementResult CanPlace(
            BoardState board, 
            int ax, int ay, 
            IReadOnlyList<Int2> offsets)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (offsets == null)
                throw new ArgumentNullException(nameof(offsets));
            if (offsets.Count == 0)
                throw new ArgumentException("Offsets cannot be empty", nameof(offsets));

            // Check each target position
            for (int i = 0; i < offsets.Count; i++)
            {
                Int2 offset = offsets[i];
                int targetX = ax + offset.X;
                int targetY = ay + offset.Y;
                
                // Check bounds first
                if (!board.IsInBounds(targetX, targetY))
                {
                    return PlacementResult.OutOfBounds;
                }
                
                // Check collision
                if (!board.IsEmpty(targetX, targetY))
                {
                    return PlacementResult.Collision;
                }
            }
            
            return PlacementResult.Success;
        }
        
        /// <summary>
        /// Atomically places a shape on the board if validation succeeds.
        /// </summary>
        public static PlacementResult PlaceAtomic(
            BoardState board,
            int ax, int ay,
            IReadOnlyList<Int2> offsets,
            int blockId, int colorId,
            out int placedCellCount)
        {
            placedCellCount = 0;
            
            // First validate the placement
            PlacementResult validation = CanPlace(board, ax, ay, offsets);
            if (validation != PlacementResult.Success)
            {
                return validation;
            }
            
            // Track filled positions for rollback safety
            var filledPositions = new List<Int2>(offsets.Count);
            
            try
            {
                // Fill all cells atomically
                for (int i = 0; i < offsets.Count; i++)
                {
                    Int2 offset = offsets[i];
                    int targetX = ax + offset.X;
                    int targetY = ay + offset.Y;
                    
                    board.FillCell(targetX, targetY, blockId, colorId);
                    filledPositions.Add(new Int2(targetX, targetY));
                }
                
                placedCellCount = offsets.Count;
                return PlacementResult.Success;
            }
            catch
            {
                // Rollback: clear any cells we managed to fill
                for (int i = 0; i < filledPositions.Count; i++)
                {
                    var pos = filledPositions[i];
                    try
                    {
                        board.ClearCell(pos.X, pos.Y);
                    }
                    catch
                    {
                        // Even rollback failed - severe state corruption
                    }
                }
                
                placedCellCount = 0;
                throw;
            }
        }
        
        /// <summary>
        /// Atomically places a shape on the board if valid (convenience overload).
        /// </summary>
        public static PlacementResult PlaceAtomic(BoardState board, int anchorX, int anchorY,
            IReadOnlyList<Int2> shapeOffsets, int blockId, int colorId)
        {
            return PlaceAtomic(board, anchorX, anchorY, shapeOffsets, blockId, colorId, out _);
        }
        
        /// <summary>
        /// Atomically places a shape on the board if valid (Int2 anchor overload).
        /// </summary>
        public static PlacementResult PlaceAtomic(BoardState board, Int2 anchor,
            IReadOnlyList<Int2> shapeOffsets, int blockId, int colorId)
        {
            return PlaceAtomic(board, anchor.X, anchor.Y, shapeOffsets, blockId, colorId, out _);
        }
        
        /// <summary>
        /// Gets the list of target positions for a shape placement (for preview purposes).
        /// </summary>
        public static List<Int2> GetTargetPositions(int anchorX, int anchorY, 
            IReadOnlyList<Int2> shapeOffsets)
        {
            var positions = new List<Int2>(shapeOffsets.Count);
            
            for (int i = 0; i < shapeOffsets.Count; i++)
            {
                Int2 offset = shapeOffsets[i];
                positions.Add(new Int2(anchorX + offset.X, anchorY + offset.Y));
            }
            
            return positions;
        }
        
        /// <summary>
        /// Gets the list of target positions for a shape placement (Int2 anchor overload).
        /// </summary>
        public static List<Int2> GetTargetPositions(Int2 anchor, IReadOnlyList<Int2> shapeOffsets)
        {
            return GetTargetPositions(anchor.X, anchor.Y, shapeOffsets);
        }
        
        /// <summary>
        /// Fills a pre-allocated list with target positions to avoid allocation.
        /// </summary>
        public static void GetTargetPositions(
            int ax, int ay,
            IReadOnlyList<Int2> offsets,
            IList<Int2> outPositions)
        {
            if (offsets == null)
                throw new ArgumentNullException(nameof(offsets));
            if (outPositions == null)
                throw new ArgumentNullException(nameof(outPositions));
            
            outPositions.Clear();
            
            for (int i = 0; i < offsets.Count; i++)
            {
                Int2 offset = offsets[i];
                outPositions.Add(new Int2(ax + offset.X, ay + offset.Y));
            }
        }
    }
}
