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
        public void DetectFullLines_BackToBackConvenienceCalls_DoNotMutateEarlierResult()
        {
            var boardA = new BoardState(4, 4);
            for (int x = 0; x < 4; x++)
                boardA.FillCell(x, 1, blockId: 1, colorId: 1);

            var boardB = new BoardState(4, 4);
            for (int y = 0; y < 4; y++)
                boardB.FillCell(2, y, blockId: 2, colorId: 1);

            var resultA = LineDetector.DetectFullLines(boardA);
            var resultB = LineDetector.DetectFullLines(boardB);

            Assert.AreNotSame(resultA, resultB);
            Assert.AreEqual(1, resultA.FullRowCount);
            Assert.AreEqual(0, resultA.FullColumnCount);
            Assert.AreEqual(1, resultA.FullRows[0]);
            Assert.AreEqual(0, resultB.FullRowCount);
            Assert.AreEqual(1, resultB.FullColumnCount);
            Assert.AreEqual(2, resultB.FullColumns[0]);
        }

        [Test]
        public void DetectFullLines_CallerOwnedSeparateResults_RemainIndependent()
        {
            var boardA = new BoardState(5, 5);
            var boardB = new BoardState(5, 5);

            for (int x = 0; x < 5; x++)
                boardA.FillCell(x, 3, blockId: 1, colorId: 1);

            for (int y = 0; y < 5; y++)
                boardB.FillCell(4, y, blockId: 2, colorId: 1);

            var resultA = new LineDetectResult(1, 1);
            var resultB = new LineDetectResult(1, 1);

            LineDetector.DetectFullLines(boardA, resultA);
            LineDetector.DetectFullLines(boardB, resultB);

            Assert.AreEqual(1, resultA.FullRowCount);
            Assert.AreEqual(0, resultA.FullColumnCount);
            Assert.AreEqual(3, resultA.FullRows[0]);
            Assert.AreEqual(0, resultB.FullRowCount);
            Assert.AreEqual(1, resultB.FullColumnCount);
            Assert.AreEqual(4, resultB.FullColumns[0]);
        }

        [Test]
        public void DetectFullLines_ReusingSameResult_ClearsStaleCountsAndResizesSafely()
        {
            var boardA = new BoardState(4, 4);
            var boardB = new BoardState(6, 6);

            for (int x = 0; x < 4; x++)
                boardA.FillCell(x, 0, blockId: 1, colorId: 1);

            for (int y = 0; y < 6; y++)
                boardB.FillCell(5, y, blockId: 2, colorId: 1);

            var reusable = new LineDetectResult(1, 1);

            LineDetector.DetectFullLines(boardA, reusable);
            Assert.AreEqual(1, reusable.FullRowCount);
            Assert.AreEqual(0, reusable.FullColumnCount);
            Assert.GreaterOrEqual(reusable.FullRows.Length, 4);
            Assert.GreaterOrEqual(reusable.FullColumns.Length, 4);

            LineDetector.DetectFullLines(boardB, reusable);
            Assert.AreEqual(0, reusable.FullRowCount);
            Assert.AreEqual(1, reusable.FullColumnCount);
            Assert.AreEqual(5, reusable.FullColumns[0]);
            Assert.GreaterOrEqual(reusable.FullRows.Length, 6);
            Assert.GreaterOrEqual(reusable.FullColumns.Length, 6);
        }

        [Test]
        public void DetectFullLines_NestedStyleSecondCall_DoesNotCorruptFirstResult()
        {
            var boardA = new BoardState(4, 4);
            var boardB = new BoardState(4, 4);

            for (int x = 0; x < 4; x++)
                boardA.FillCell(x, 2, blockId: 1, colorId: 1);

            for (int y = 0; y < 4; y++)
                boardB.FillCell(1, y, blockId: 2, colorId: 1);

            var resultA = LineDetector.DetectFullLines(boardA);
            int firstRow = resultA.FullRows[0];

            _ = LineDetector.DetectFullLines(boardB);

            Assert.AreEqual(1, resultA.FullRowCount);
            Assert.AreEqual(0, resultA.FullColumnCount);
            Assert.AreEqual(2, firstRow);
            Assert.AreEqual(2, resultA.FullRows[0]);
        }

        [Test]
        public void DetectFullLines_NoLines_ReturnsEmptyResult()
        {
            var board = new BoardState(4, 4);
            board.FillCell(0, 0, blockId: 1, colorId: 1);
            board.FillCell(1, 1, blockId: 2, colorId: 1);

            var result = LineDetector.DetectFullLines(board);

            Assert.AreEqual(0, result.FullRowCount);
            Assert.AreEqual(0, result.FullColumnCount);
            Assert.IsFalse(result.HasFullLines);
            Assert.IsFalse(LineDetector.HasAnyFullLines(board));
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
