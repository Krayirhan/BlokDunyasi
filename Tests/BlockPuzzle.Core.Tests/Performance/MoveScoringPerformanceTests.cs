using System;
using System.Diagnostics;
using System.Linq;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Rules;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Performance
{
    [TestFixture]
    [Category("Performance")]
    public class MoveScoringPerformanceTests
    {
        private const int WarmupIterations = 200;
        private const int MeasuredIterations = 4000;
        private const double TargetAverageMsPerMove = 1.0d;
        private const double TargetP95MsPerMove = 2.5d;

        [Test]
        public void MoveScoreAndLineDetection_PerMoveBudget_IsWithinTarget()
        {
            var templateBoard = CreateNearLineClearBoard();
            var config = ScoreConfig.Default;
            var combo = new ComboState();

            for (int i = 0; i < WarmupIterations; i++)
            {
                combo.SetStreak((i % 6) + 1);
                ExecuteSingleMove(templateBoard, combo, config);
            }

            var samplesMs = new double[MeasuredIterations];
            for (int i = 0; i < MeasuredIterations; i++)
            {
                combo.SetStreak((i % 10) + 1);
                samplesMs[i] = ExecuteSingleMove(templateBoard, combo, config);
            }

            double averageMs = samplesMs.Average();
            double p95Ms = Percentile(samplesMs, 95);

            Assert.LessOrEqual(
                averageMs,
                TargetAverageMsPerMove,
                $"Average move scoring+line detection time exceeded target. Avg={averageMs:F4}ms, target={TargetAverageMsPerMove:F2}ms.");

            Assert.LessOrEqual(
                p95Ms,
                TargetP95MsPerMove,
                $"P95 move scoring+line detection time exceeded target. P95={p95Ms:F4}ms, target={TargetP95MsPerMove:F2}ms.");
        }

        private static double ExecuteSingleMove(BoardState templateBoard, ComboState combo, ScoreConfig config)
        {
            var board = templateBoard.Clone();

            long start = Stopwatch.GetTimestamp();

            var placement = PlacementEngine.PlaceAtomic(
                board,
                9,
                0,
                new[] { Int2.Zero },
                999,
                1,
                out _);

            if (placement != PlacementResult.Success)
                throw new InvalidOperationException("Performance template board produced invalid placement.");

            var lineResult = LineDetector.DetectFullLines(board);
            int linesCleared = lineResult.FullRowCount + lineResult.FullColumnCount;
            _ = ScoringRules.CalculateScore(linesCleared, combo, config);

            long end = Stopwatch.GetTimestamp();
            return (end - start) * 1000.0d / Stopwatch.Frequency;
        }

        private static BoardState CreateNearLineClearBoard()
        {
            var board = new BoardState(width: 10, height: 10);
            var cells = new CellState[100];
            for (int x = 0; x < 9; x++)
            {
                cells[x] = CellState.Filled(blockId: 10 + x, colorId: 1);
            }

            board.SetCells(cells);
            return board;
        }

        private static double Percentile(double[] values, int percentile)
        {
            if (values == null || values.Length == 0)
                return 0d;

            var sorted = (double[])values.Clone();
            Array.Sort(sorted);

            double rank = (percentile / 100d) * (sorted.Length - 1);
            int lower = (int)Math.Floor(rank);
            int upper = (int)Math.Ceiling(rank);

            if (lower == upper)
                return sorted[lower];

            double weight = rank - lower;
            return sorted[lower] * (1d - weight) + sorted[upper] * weight;
        }
    }
}
