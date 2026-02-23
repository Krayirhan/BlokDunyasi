// File: Core/Shapes/ShapeId.cs
using System;

namespace BlockPuzzle.Core.Shapes
{
    /// <summary>
    /// Unique identifier for a shape type.
    /// Immutable value type for efficient comparison and hashing.
    /// </summary>
    public readonly struct ShapeId : IEquatable<ShapeId>
    {
        /// <summary>
        /// The unique numeric value of this shape ID.
        /// </summary>
        public readonly int Value;
        
        /// <summary>
        /// Creates a new ShapeId with the specified value.
        /// </summary>
        /// <param name="value">Unique numeric identifier</param>
        public ShapeId(int value)
        {
            Value = value;
        }
        
        /// <summary>
        /// Determines whether this ShapeId equals another ShapeId.
        /// </summary>
        public bool Equals(ShapeId other)
        {
            return Value == other.Value;
        }
        
        /// <summary>
        /// Determines whether this ShapeId equals another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ShapeId other && Equals(other);
        }
        
        /// <summary>
        /// Returns a hash code for this ShapeId.
        /// </summary>
        public override int GetHashCode()
        {
            return Value;
        }
        
        /// <summary>
        /// Returns a string representation of this ShapeId.
        /// </summary>
        public override string ToString()
        {
            return $"ShapeId({Value})";
        }
        
        /// <summary>
        /// Equality operator for ShapeId.
        /// </summary>
        public static bool operator ==(ShapeId left, ShapeId right)
        {
            return left.Equals(right);
        }
        
        /// <summary>
        /// Inequality operator for ShapeId.
        /// </summary>
        public static bool operator !=(ShapeId left, ShapeId right)
        {
            return !left.Equals(right);
        }
    }
}