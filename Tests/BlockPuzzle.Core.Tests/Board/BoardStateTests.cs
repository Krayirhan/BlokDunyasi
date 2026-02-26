// File: Tests/BlockPuzzle.Core.Tests/Board/BoardStateTests.cs
using NUnit.Framework;
using System;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.Core.Tests.Board
{
    [TestFixture]
    [Category("Unit")]
    public class BoardStateTests
    {
        private BoardState _board;

        [SetUp]
        public void Setup()
        {
            _board = new BoardState(4, 4); // 4x4 board for testing
        }

        [Test]
        public void Constructor_ValidDimensions_CreatesEmptyBoard()
        {
            // Act
            var board = new BoardState(8, 6);
            
            // Assert
            Assert.AreEqual(8, board.Width);
            Assert.AreEqual(6, board.Height);
            
            // Verify all cells are empty
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Assert.IsTrue(board.IsEmpty(x, y));
                    Assert.AreEqual(0, board.GetRowCount(y));
                }
            }
            for (int x = 0; x < 8; x++)
            {
                Assert.AreEqual(0, board.GetColCount(x));
            }
        }

        [Test]
        public void Constructor_InvalidDimensions_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new BoardState(0, 4));
            Assert.Throws<ArgumentException>(() => new BoardState(4, 0));
            Assert.Throws<ArgumentException>(() => new BoardState(-1, 4));
            Assert.Throws<ArgumentException>(() => new BoardState(4, -1));
        }

        [Test]
        public void FillCell_EmptyCell_FillsAndIncrementsCount()
        {
            // Act
            _board.FillCell(1, 2, blockId: 5, colorId: 3);
            
            // Assert
            Assert.IsFalse(_board.IsEmpty(1, 2));
            var cell = _board.GetCell(1, 2);
            Assert.AreEqual(5, cell.BlockId);
            Assert.AreEqual(3, cell.ColorId);
            
            // Verify counts updated
            Assert.AreEqual(1, _board.GetRowCount(2));
            Assert.AreEqual(1, _board.GetColCount(1));
        }

        [Test]
        public void FillCell_AlreadyFilled_ThrowsException()
        {
            // Arrange
            _board.FillCell(1, 2, blockId: 5, colorId: 3);
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _board.FillCell(1, 2, blockId: 6, colorId: 4));
        }

        [Test]
        public void ClearCell_FilledCell_ClearsAndDecrementsCount()
        {
            // Arrange
            _board.FillCell(1, 2, blockId: 5, colorId: 3);
            
            // Act
            _board.ClearCell(1, 2);
            
            // Assert
            Assert.IsTrue(_board.IsEmpty(1, 2));
            
            // Verify counts decremented
            Assert.AreEqual(0, _board.GetRowCount(2));
            Assert.AreEqual(0, _board.GetColCount(1));
        }

        [Test]
        public void ClearCell_EmptyCell_NoOpSafe()
        {
            // Act (should not throw)
            _board.ClearCell(1, 2);
            
            // Assert
            Assert.IsTrue(_board.IsEmpty(1, 2));
            Assert.AreEqual(0, _board.GetRowCount(2));
            Assert.AreEqual(0, _board.GetColCount(1));
        }

        [Test]
        public void GetRowCount_MultipleCellsInRow_ReturnsCorrectCount()
        {
            // Arrange
            _board.FillCell(0, 1, blockId: 1, colorId: 1);
            _board.FillCell(2, 1, blockId: 2, colorId: 2);
            _board.FillCell(3, 1, blockId: 3, colorId: 3);
            
            // Act & Assert
            Assert.AreEqual(3, _board.GetRowCount(1));
            Assert.AreEqual(0, _board.GetRowCount(0));
            Assert.AreEqual(0, _board.GetRowCount(2));
        }

        [Test]
        public void GetColCount_MultipleCellsInColumn_ReturnsCorrectCount()
        {
            // Arrange
            _board.FillCell(2, 0, blockId: 1, colorId: 1);
            _board.FillCell(2, 1, blockId: 2, colorId: 2);
            _board.FillCell(2, 3, blockId: 3, colorId: 3);
            
            // Act & Assert
            Assert.AreEqual(3, _board.GetColCount(2));
            Assert.AreEqual(0, _board.GetColCount(0));
            Assert.AreEqual(0, _board.GetColCount(1));
            Assert.AreEqual(0, _board.GetColCount(3));
        }

        [Test]
        public void Reset_FilledBoard_ClearsAllCellsAndCounts()
        {
            // Arrange
            _board.FillCell(0, 0, blockId: 1, colorId: 1);
            _board.FillCell(1, 1, blockId: 2, colorId: 2);
            _board.FillCell(2, 2, blockId: 3, colorId: 3);
            
            // Act
            _board.Reset();
            
            // Assert
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    Assert.IsTrue(_board.IsEmpty(x, y));
                }
                Assert.AreEqual(0, _board.GetRowCount(y));
            }
            
            for (int x = 0; x < _board.Width; x++)
            {
                Assert.AreEqual(0, _board.GetColCount(x));
            }
        }

        [Test]
        public void ValidateCountsOrThrow_ValidBoard_DoesNotThrow()
        {
            // Arrange
            _board.FillCell(0, 0, blockId: 1, colorId: 1);
            _board.FillCell(1, 0, blockId: 2, colorId: 2);
            _board.FillCell(0, 1, blockId: 3, colorId: 3);
            
            // Act & Assert (should not throw)
            _board.ValidateCountsOrThrow();
        }

        [Test]
        public void GetCell_OutOfBounds_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetCell(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetCell(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetCell(4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetCell(0, 4));
        }

        [Test]
        public void IsInBounds_VariousCoordinates_ReturnsCorrectResult()
        {
            Assert.IsTrue(_board.IsInBounds(0, 0));
            Assert.IsTrue(_board.IsInBounds(3, 3));
            Assert.IsTrue(_board.IsInBounds(1, 2));
            
            Assert.IsFalse(_board.IsInBounds(-1, 0));
            Assert.IsFalse(_board.IsInBounds(0, -1));
            Assert.IsFalse(_board.IsInBounds(4, 0));
            Assert.IsFalse(_board.IsInBounds(0, 4));
        }

        [Test]
        public void GetRowCount_OutOfBounds_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetRowCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetRowCount(4));
        }

        [Test]
        public void GetColCount_OutOfBounds_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetColCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.GetColCount(4));
        }
    }
}
