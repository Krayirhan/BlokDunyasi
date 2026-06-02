#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Tests.Core
{
    public class CoreLogicTests
    {
        [Test]
        public void PlacementEngine_CanPlace_EmptyBoard_ReturnsSuccess()
        {
            var board = new BoardState(8, 8);
            var offsets = new List<Int2> { new Int2(0, 0), new Int2(1, 0), new Int2(0, 1) };
            
            var result = PlacementEngine.CanPlace(board, 2, 2, offsets);
            
            Assert.AreEqual(PlacementResult.Success, result);
        }

        [Test]
        public void PlacementEngine_CanPlace_OutOfBounds_ReturnsOutOfBounds()
        {
            var board = new BoardState(8, 8);
            var offsets = new List<Int2> { new Int2(0, 0), new Int2(1, 0) };
            
            var result = PlacementEngine.CanPlace(board, 7, 7, offsets);
            
            Assert.AreEqual(PlacementResult.OutOfBounds, result);
        }

        [Test]
        public void PlacementEngine_CanPlace_Overlap_ReturnsOverlap()
        {
            var board = new BoardState(8, 8);
            var singleCell = new List<Int2> { new Int2(0, 0) };
            PlacementEngine.PlaceAtomic(board, 3, 3, singleCell, 1, 1);
            
            var offsets = new List<Int2> { new Int2(0, 0) };
            
            var result = PlacementEngine.CanPlace(board, 3, 3, offsets);
            
            Assert.AreEqual(PlacementResult.Collision, result);
        }

        [Test]
        public void LineDetector_DetectFullLines_ReturnsCorrectLines()
        {
            var board = new BoardState(4, 4);
            var singleCell = new List<Int2> { new Int2(0, 0) };
            for(int x = 0; x < 4; x++) PlacementEngine.PlaceAtomic(board, x, 1, singleCell, 1, 1);
            for(int y = 0; y < 4; y++) 
            {
                if(y != 1)
                    PlacementEngine.PlaceAtomic(board, 2, y, singleCell, 1, 1);
            }

            var result = LineDetector.DetectFullLines(board);

            Assert.IsTrue(result.HasFullLines);
            Assert.AreEqual(1, result.FullRowCount);
            Assert.AreEqual(1, result.FullRows[0]);
            Assert.AreEqual(1, result.FullColumnCount);
            Assert.AreEqual(2, result.FullColumns[0]);
        }

        [Test]
        public void LineClearer_ClearLines_RemovesCellsAndReturnsCount()
        {
            var board = new BoardState(4, 4);
            var singleCell = new List<Int2> { new Int2(0, 0) };
            for(int x = 0; x < 4; x++) PlacementEngine.PlaceAtomic(board, x, 1, singleCell, 1, 1);
            for(int y = 0; y < 4; y++) 
            {
                if(y != 1) PlacementEngine.PlaceAtomic(board, 2, y, singleCell, 1, 1);
            }

            var rows = new List<int> { 1 };
            var cols = new List<int> { 2 };

            var clearResult = LineClearer.ClearLines(board, rows, cols);

            Assert.AreEqual(7, clearResult.ClearedCellCount);
            Assert.IsTrue(board.IsEmpty(0, 1));
            Assert.IsTrue(board.IsEmpty(2, 3));
        }
    }
}
#endif
