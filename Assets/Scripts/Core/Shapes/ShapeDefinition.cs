// File: Core/Shapes/ShapeDefinition.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Shapes
{
    /// <summary>
    /// Immutable definition of a shape with its ID, name, and cell offsets.
    /// 
    /// Invariants:
    /// - Must contain at least one offset
    /// - Must include (0,0) as the anchor point
    /// - No duplicate offsets allowed
    /// - Offsets represent relative positions from the anchor
    /// </summary>
    public sealed class ShapeDefinition
    {
        /// <summary>
        /// Unique identifier for this shape.
        /// </summary>
        public ShapeId Id { get; }
        
        /// <summary>
        /// Human-readable name of the shape.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Array of cell offsets relative to the anchor point (0,0).
        /// </summary>
        public Int2[] Offsets { get; }
        
        /// <summary>
        /// Creates a new shape definition.
        /// </summary>
        /// <param name="id">Unique shape identifier</param>
        /// <param name="name">Shape name</param>
        /// <param name="offsets">Cell offsets relative to anchor</param>
        /// <exception cref="ArgumentNullException">If name or offsets is null</exception>
        /// <exception cref="ArgumentException">If validation fails</exception>
        public ShapeDefinition(ShapeId id, string name, Int2[] offsets)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (offsets == null)
                throw new ArgumentNullException(nameof(offsets));
            
            ValidateOffsets(offsets);
            
            Id = id;
            Name = name;
            Offsets = (Int2[])offsets.Clone(); // Defensive copy
        }
        
        /// <summary>
        /// Gets the offsets as a read-only list.
        /// </summary>
        /// <returns>Read-only view of the shape offsets</returns>
        public IReadOnlyList<Int2> GetOffsets()
        {
            return Offsets;
        }
        
        /// <summary>
        /// Validates that the offsets meet shape requirements.
        /// </summary>
        /// <param name="offsets">Offsets to validate</param>
        /// <exception cref="ArgumentException">If validation fails</exception>
        private static void ValidateOffsets(Int2[] offsets)
        {
            if (offsets.Length == 0)
                throw new ArgumentException("Shape must contain at least one offset", nameof(offsets));
            
            // Must contain (0,0) as anchor point
            bool hasAnchor = false;
            for (int i = 0; i < offsets.Length; i++)
            {
                if (offsets[i].X == 0 && offsets[i].Y == 0)
                {
                    hasAnchor = true;
                    break;
                }
            }
            
            if (!hasAnchor)
                throw new ArgumentException("Shape must contain (0,0) as anchor point", nameof(offsets));
            
            // Check for duplicates - avoid LINQ for performance
            for (int i = 0; i < offsets.Length; i++)
            {
                for (int j = i + 1; j < offsets.Length; j++)
                {
                    if (offsets[i] == offsets[j])
                    {
                        throw new ArgumentException($"Duplicate offset found: {offsets[i]}", nameof(offsets));
                    }
                }
            }
        }
        
        public override string ToString()
        {
            return $"{Name} (ID: {Id}, {Offsets.Length} cells)";
        }
    }
}