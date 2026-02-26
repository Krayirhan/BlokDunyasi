// File: Tests/BlockPuzzle.Core.Tests/Board/BoardCoordTests.cs
using NUnit.Framework;
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Tests.Board
{
    [TestFixture]
    [Category("Unit")]
    public class BoardCoordTests
    {
        [Test]
        public void ToIndex_ValidCoordinates_ReturnsCorrectIndex()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, BoardCoord.ToIndex(0, 0, 8, 8)); // Bottom-left corner
            Assert.AreEqual(7, BoardCoord.ToIndex(7, 0, 8, 8)); // Bottom-right corner
            Assert.AreEqual(56, BoardCoord.ToIndex(0, 7, 8, 8)); // Top-left corner
            Assert.AreEqual(63, BoardCoord.ToIndex(7, 7, 8, 8)); // Top-right corner
            Assert.AreEqual(26, BoardCoord.ToIndex(2, 3, 8, 8)); // Middle position: 3*8 + 2
        }

        [Test]
        public void FromIndex_ValidIndex_ReturnsCorrectCoordinates()
        {
            // Arrange & Act & Assert
            BoardCoord.FromIndex(0, 8, 8, out int x0, out int y0);
            Assert.AreEqual(0, x0);
            Assert.AreEqual(0, y0);

            BoardCoord.FromIndex(7, 8, 8, out int x1, out int y1);
            Assert.AreEqual(7, x1);
            Assert.AreEqual(0, y1);

            BoardCoord.FromIndex(56, 8, 8, out int x2, out int y2);
            Assert.AreEqual(0, x2);
            Assert.AreEqual(7, y2);

            BoardCoord.FromIndex(26, 8, 8, out int x3, out int y3);
            Assert.AreEqual(2, x3);
            Assert.AreEqual(3, y3);
        }

        [Test]
        public void ToIndex_FromIndex_RoundTrip_IsConsistent()
        {
            // Test round-trip consistency for all positions on 8x8 board
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int index = BoardCoord.ToIndex(x, y, 8, 8);
                    BoardCoord.FromIndex(index, 8, 8, out int backX, out int backY);
                    
                    Assert.AreEqual(x, backX, $"X mismatch for ({x},{y}) -> {index} -> ({backX},{backY})");
                    Assert.AreEqual(y, backY, $"Y mismatch for ({x},{y}) -> {index} -> ({backX},{backY})");
                }
            }
        }

        [Test]
        public void ToIndex_OutOfBounds_ThrowsException()
        {
            // Test X out of bounds
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardCoord.ToIndex(-1, 0, 8, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardCoord.ToIndex(8, 0, 8, 8));
            
            // Test Y out of bounds
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardCoord.ToIndex(0, -1, 8, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardCoord.ToIndex(0, 8, 8, 8));
        }

        [Test]
        public void FromIndex_OutOfBounds_ThrowsException()
        {
            // Test negative index
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.FromIndex(-1, 8, 8, out int x, out int y));
            
            // Test index too large
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.FromIndex(64, 8, 8, out int x, out int y));
        }

        [Test]
        public void IsInBounds_ValidCoordinates_ReturnsTrue()
        {
            Assert.IsTrue(BoardCoord.IsInBounds(0, 0, 8, 8));
            Assert.IsTrue(BoardCoord.IsInBounds(7, 7, 8, 8));
            Assert.IsTrue(BoardCoord.IsInBounds(3, 4, 8, 8));
        }

        [Test]
        public void IsInBounds_InvalidCoordinates_ReturnsFalse()
        {
            Assert.IsFalse(BoardCoord.IsInBounds(-1, 0, 8, 8));
            Assert.IsFalse(BoardCoord.IsInBounds(8, 0, 8, 8));
            Assert.IsFalse(BoardCoord.IsInBounds(0, -1, 8, 8));
            Assert.IsFalse(BoardCoord.IsInBounds(0, 8, 8, 8));
        }

        [Test]
        public void EnumerateRow_ValidRow_CallsVisitorForEachCell()
        {
            // Arrange
            var visitedIndices = new List<int>();
            
            // Act
            BoardCoord.EnumerateRow(2, 4, 4, index => visitedIndices.Add(index));
            
            // Assert
            CollectionAssert.AreEqual(new[] { 8, 9, 10, 11 }, visitedIndices); // Row 2: indices 8-11
        }

        [Test]
        public void EnumerateColumn_ValidColumn_CallsVisitorForEachCell()
        {
            // Arrange
            var visitedIndices = new List<int>();
            
            // Act
            BoardCoord.EnumerateColumn(1, 4, 4, index => visitedIndices.Add(index));
            
            // Assert
            CollectionAssert.AreEqual(new[] { 1, 5, 9, 13 }, visitedIndices); // Column 1: indices 1,5,9,13
        }

        [Test]
        public void EnumerateRow_OutOfBounds_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.EnumerateRow(-1, 4, 4, index => { }));
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.EnumerateRow(4, 4, 4, index => { }));
        }

        [Test]
        public void EnumerateColumn_OutOfBounds_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.EnumerateColumn(-1, 4, 4, index => { }));
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                BoardCoord.EnumerateColumn(4, 4, 4, index => { }));
        }

        [Test]
        public void EnumerateRow_NullVisitor_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                BoardCoord.EnumerateRow(0, 4, 4, null));
        }

        [Test]
        public void EnumerateColumn_NullVisitor_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                BoardCoord.EnumerateColumn(0, 4, 4, null));
        }
    }
}
