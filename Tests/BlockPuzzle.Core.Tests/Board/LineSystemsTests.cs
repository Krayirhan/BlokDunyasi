using NUnit.Framework;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.Core.Tests.Board
{
    [TestFixture]
    [Category("Unit")]
    public class LineSystemsTests
    {
        [Test]
        public void DetectFullLines_FullRowAndColumn_AreReported()
        {
            var board = new BoardState(4, 4);

            for (int x = 0; x < 4; x++)
                board.FillCell(x, 1, blockId: 1, colorId: 1);

            for (int y = 0; y < 4; y++)
                if (board.IsEmpty(2, y))
                    board.FillCell(2, y, blockId: 2, colorId: 1);

            var result = LineDetector.DetectFullLines(board);

            Assert.IsTrue(result.HasFullLines);
            Assert.AreEqual(1, result.FullRowCount);
            Assert.AreEqual(1, result.FullColumnCount);
            Assert.AreEqual(1, result.FullRows[0]);
            Assert.AreEqual(2, result.FullColumns[0]);
        }

        [Test]
        public void ClearLines_RowAndColumn_IntersectionClearedOnce()
        {
            var board = new BoardState(4, 4);

            for (int x = 0; x < 4; x++)
                board.FillCell(x, 2, blockId: 1, colorId: 1);

            for (int y = 0; y < 4; y++)
                if (board.IsEmpty(1, y))
                    board.FillCell(1, y, blockId: 2, colorId: 1);

            var clear = LineClearer.ClearLines(board, new[] { 2 }, new[] { 1 });

            // 4 cells in row + 4 cells in column - 1 intersection
            Assert.AreEqual(7, clear.ClearedCellCount);
            Assert.AreEqual(0, board.GetRowCount(2));
            Assert.AreEqual(0, board.GetColCount(1));
            Assert.IsTrue(board.IsEmpty(1, 2));
        }
    }
}
