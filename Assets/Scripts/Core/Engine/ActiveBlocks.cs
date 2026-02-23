// File: Core/Engine/ActiveBlocks.cs
using System;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.Core.Engine
{
    /// <summary>
    /// Manages the set of currently active (unplaced) blocks available to the player.
    /// Uses FIXED 3 SLOTS - slot index never changes, null means slot is empty.
    /// </summary>
    [Serializable]
    public class ActiveBlocks
    {
        // FIXED 3 SLOTS - index = slot position, null = empty slot
        private readonly ShapeId?[] _slots = new ShapeId?[3];
        
        /// <summary>
        /// Number of active blocks currently available (non-null slots).
        /// </summary>
        public int Count => _slots.Count(s => s.HasValue);
        
        /// <summary>
        /// True if no blocks are currently active.
        /// </summary>
        public bool IsEmpty => Count == 0;
        
        /// <summary>
        /// True if all slots have blocks.
        /// </summary>
        public bool IsFull => Count == 3;
        
        public ActiveBlocks()
        {
            // All slots start empty
            for (int i = 0; i < 3; i++)
            {
                _slots[i] = null;
            }
        }
        
        /// <summary>
        /// Gets an active block by SLOT index.
        /// </summary>
        public ActiveBlock GetBlock(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} out of range [0, 3)");
            
            if (!_slots[slotIndex].HasValue)
                throw new InvalidOperationException($"Slot {slotIndex} is empty");
                
            return new ActiveBlock(_slots[slotIndex].Value, slotIndex);
        }
        
        /// <summary>
        /// Gets the shape ID of a block at slot.
        /// </summary>
        public ShapeId GetShapeId(int slotIndex)
        {
            return GetBlock(slotIndex).ShapeId;
        }
        
        /// <summary>
        /// Checks if a block exists at the given SLOT.
        /// </summary>
        public bool HasBlockAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3)
                return false;
            return _slots[slotIndex].HasValue;
        }
        
        /// <summary>
        /// Adds a new active block to the first empty slot.
        /// </summary>
        public void AddBlock(ShapeId shapeId)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!_slots[i].HasValue)
                {
                    _slots[i] = shapeId;
                    return;
                }
            }
            throw new InvalidOperationException("Cannot add block: All slots are full");
        }
        
        /// <summary>
        /// Sets a block at a specific slot.
        /// </summary>
        public void SetBlockAt(int slotIndex, ShapeId shapeId)
        {
            if (slotIndex < 0 || slotIndex >= 3)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            _slots[slotIndex] = shapeId;
        }
        
        /// <summary>
        /// Removes an active block by SLOT index.
        /// Slot becomes empty (null), other slots stay unchanged.
        /// </summary>
        public void RemoveBlock(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} out of range [0, 3)");
            
            _slots[slotIndex] = null;
            // NOTE: Other slots KEEP their positions!
        }
        
        /// <summary>
        /// Sets the active blocks from an array of shape IDs.
        /// CRITICAL: Must receive exactly 3 elements for a full set.
        /// </summary>
        public void SetBlocks(ShapeId[] shapeIds)
        {
            if (shapeIds == null)
                throw new ArgumentNullException(nameof(shapeIds));
            
            if (shapeIds.Length > 3)
                throw new ArgumentException("Cannot set more than 3 active blocks", nameof(shapeIds));
            
            // WARNING: If less than 3 elements provided, some slots will be empty!
            if (shapeIds.Length < 3)
            {
                System.Diagnostics.Debug.WriteLine($"[ActiveBlocks.SetBlocks] WARNING: Received only {shapeIds.Length} shapeIds, expected 3. Some slots will be empty!");
            }
            
            Clear();
            for (int i = 0; i < shapeIds.Length && i < 3; i++)
            {
                _slots[i] = shapeIds[i];
            }
            
            System.Diagnostics.Debug.WriteLine($"[ActiveBlocks.SetBlocks] Set {shapeIds.Length} blocks: {this}");
        }
        
        /// <summary>
        /// Clears all active blocks.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < 3; i++)
            {
                _slots[i] = null;
            }
        }
        
        /// <summary>
        /// Gets all shape IDs as an array (only non-null slots).
        /// </summary>
        public ShapeId[] GetShapeIds()
        {
            return _slots.Where(s => s.HasValue).Select(s => s.Value).ToArray();
        }
        
        /// <summary>
        /// Gets all slots as shape IDs (null for empty slots).
        /// </summary>
        public ShapeId?[] GetSlots()
        {
            return (ShapeId?[])_slots.Clone();
        }

        /// <summary>
        /// Gets slot values as int array (length 3, -1 for empty).
        /// </summary>
        public int[] GetSlotIds()
        {
            var slots = new int[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = _slots[i].HasValue ? _slots[i].Value.Value : -1;
            }
            return slots;
        }
        
        /// <summary>
        /// Gets all active blocks as a list (only filled slots).
        /// </summary>
        public IReadOnlyList<ActiveBlock> GetBlocks()
        {
            var result = new List<ActiveBlock>();
            for (int i = 0; i < 3; i++)
            {
                if (_slots[i].HasValue)
                {
                    result.Add(new ActiveBlock(_slots[i].Value, i));
                }
            }
            return result;
        }
        
        /// <summary>
        /// Creates a copy of this ActiveBlocks instance.
        /// </summary>
        public ActiveBlocks Clone()
        {
            var clone = new ActiveBlocks();
            for (int i = 0; i < 3; i++)
            {
                clone._slots[i] = _slots[i];
            }
            return clone;
        }
        
        /// <summary>
        /// Checks if any active blocks can be placed on the given board.
        /// </summary>
        public bool HasPlaceableBlocks(BoardState boardState)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_slots[i].HasValue)
                {
                    if (ShapeLibrary.TryGetShape(_slots[i].Value, out var shape))
                    {
                        if (PlacementSearch.HasAnyValidPlacement(boardState, shape))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        public override string ToString()
        {
            if (IsEmpty)
                return "No active blocks";
            
            var slotStrings = new string[3];
            for (int i = 0; i < 3; i++)
            {
                slotStrings[i] = _slots[i].HasValue ? _slots[i].Value.ToString() : "empty";
            }
            return $"Slots: [{string.Join(", ", slotStrings)}]";
        }
    }
    
    /// <summary>
    /// Represents a single active block with its shape and SLOT index.
    /// </summary>
    [Serializable]
    public readonly struct ActiveBlock
    {
        public readonly ShapeId ShapeId;
        public readonly int Index;
        
        public ActiveBlock(ShapeId shapeId, int index)
        {
            ShapeId = shapeId;
            Index = index;
        }
        
        public ActiveBlock WithIndex(int newIndex)
        {
            return new ActiveBlock(ShapeId, newIndex);
        }
        
        public ShapeDefinition GetShapeDefinition()
        {
            ShapeLibrary.TryGetShape(ShapeId, out var shape);
            return shape;
        }
        
        public override string ToString()
        {
            return $"{ShapeId} (slot {Index})";
        }
    }
}
