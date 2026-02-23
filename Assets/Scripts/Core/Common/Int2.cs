using System;

namespace BlockPuzzle.Core.Common
{
    /// <summary>
    /// 2D integer vector for grid positions and offsets.
    /// Pure C# implementation without Unity dependencies.
    /// </summary>
    [Serializable]
    public struct Int2 : IEquatable<Int2>
    {
        public readonly int X;
        public readonly int Y;
        
        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public static readonly Int2 Zero = new Int2(0, 0);
        public static readonly Int2 One = new Int2(1, 1);
        public static readonly Int2 UnitX = new Int2(1, 0);
        public static readonly Int2 UnitY = new Int2(0, 1);
        
        public static Int2 operator +(Int2 a, Int2 b) => new Int2(a.X + b.X, a.Y + b.Y);
        public static Int2 operator -(Int2 a, Int2 b) => new Int2(a.X - b.X, a.Y - b.Y);
        public static Int2 operator *(Int2 a, int b) => new Int2(a.X * b, a.Y * b);
        public static bool operator ==(Int2 a, Int2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Int2 a, Int2 b) => !(a == b);
        
        public bool Equals(Int2 other) => this == other;
        public override bool Equals(object obj) => obj is Int2 other && Equals(other);
        public override int GetHashCode() => X * 31 + Y;
        public override string ToString() => $"({X}, {Y})";
    }
}