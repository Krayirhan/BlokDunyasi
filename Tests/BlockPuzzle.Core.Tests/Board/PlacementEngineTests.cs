using NUnit.Framework;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;

namespace BlockPuzzle.Core.Tests.Board
{
    [TestFixture]
    [Category("Unit")]
    public class PlacementEngineTests
    {
        [Test]
        public void CanPlace_EmptyBoard_ReturnsSuccess()
        {
            var board = new BoardState(4, 4);
            var offsets = new[] { new Int2(0, 0), new Int2(1, 0) };

            var result = PlacementEngine.CanPlace(board, 1, 1, offsets);

            Assert.AreEqual(PlacementResult.Success, result);
        }

        [Test]
        public void CanPlace_OutOfBounds_ReturnsOutOfBounds()
        {
            var board = new BoardState(4, 4);
            var offsets = new[] { new Int2(0, 0), new Int2(1, 0) };

            var result = PlacementEngine.CanPlace(board, 3, 1, offsets);

            Assert.AreEqual(PlacementResult.OutOfBounds, result);
        }

        [Test]
        public void CanPlace_Collision_ReturnsCollision()
        {
            var board = new BoardState(4, 4);
            board.FillCell(1, 1, blockId: 1, colorId: 1);
            var offsets = new[] { new Int2(0, 0), new Int2(1, 0) };

            var result = PlacementEngine.CanPlace(board, 1, 1, offsets);

            Assert.AreEqual(PlacementResult.Collision, result);
        }

        [Test]
        public void PlaceAtomic_Success_FillsCellsAndReturnsPlacedCount()
        {
            var board = new BoardState(4, 4);
            var offsets = new[] { new Int2(0, 0), new Int2(1, 0), new Int2(1, 1) };

            var result = PlacementEngine.PlaceAtomic(board, 1, 1, offsets, blockId: 7, colorId: 3, out int placedCount);

            Assert.AreEqual(PlacementResult.Success, result);
            Assert.AreEqual(3, placedCount);
            Assert.IsTrue(board.IsOccupied(1, 1));
            Assert.IsTrue(board.IsOccupied(2, 1));
            Assert.IsTrue(board.IsOccupied(2, 2));
        }

        [Test]
        public void PlaceAtomic_Failure_DoesNotMutateBoard()
        {
            var board = new BoardState(4, 4);
            board.FillCell(2, 1, blockId: 9, colorId: 2);
            int beforeRowCount = board.GetRowCount(1);
            int beforeColCount = board.GetColCount(2);
            var offsets = new[] { new Int2(0, 0), new Int2(1, 0) };

            var result = PlacementEngine.PlaceAtomic(board, 1, 1, offsets, blockId: 7, colorId: 3, out int placedCount);

            Assert.AreEqual(PlacementResult.Collision, result);
            Assert.AreEqual(0, placedCount);
            Assert.AreEqual(beforeRowCount, board.GetRowCount(1));
            Assert.AreEqual(beforeColCount, board.GetColCount(2));
            Assert.IsTrue(board.IsOccupied(2, 1));
            Assert.IsFalse(board.IsOccupied(1, 1));
        }
    }
}
