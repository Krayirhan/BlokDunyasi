using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.Board
{
    /// <summary>
    /// Lightweight board-health heuristics used by fairness, preview warnings and diagnostics.
    /// </summary>
    public static class BoardHeuristics
    {
        public readonly struct Snapshot
        {
            public readonly int EmptyCellCount;
            public readonly int LargestEmptyRectangleArea;
            public readonly int LargestConnectedEmptyRegion;
            public readonly int AvailableThreeByThreeCount;
            public readonly int PlaceableShapeCount;
            public readonly int EasyShapeCount;
            public readonly float FutureOpenAreaScore;

            public bool HasThreeByThreeSpace => AvailableThreeByThreeCount > 0;

            public Snapshot(
                int emptyCellCount,
                int largestEmptyRectangleArea,
                int largestConnectedEmptyRegion,
                int availableThreeByThreeCount,
                int placeableShapeCount,
                int easyShapeCount,
                float futureOpenAreaScore)
            {
                EmptyCellCount = emptyCellCount;
                LargestEmptyRectangleArea = largestEmptyRectangleArea;
                LargestConnectedEmptyRegion = largestConnectedEmptyRegion;
                AvailableThreeByThreeCount = availableThreeByThreeCount;
                PlaceableShapeCount = placeableShapeCount;
                EasyShapeCount = easyShapeCount;
                FutureOpenAreaScore = futureOpenAreaScore;
            }

            public float GetCompositeScore()
            {
                return
                    (EmptyCellCount * 0.24f) +
                    (LargestEmptyRectangleArea * 1.1f) +
                    (LargestConnectedEmptyRegion * 0.58f) +
                    (AvailableThreeByThreeCount * 7.5f) +
                    (PlaceableShapeCount * 4.2f) +
                    (EasyShapeCount * 1.75f) +
                    (FutureOpenAreaScore * 12f);
            }

            public override string ToString()
            {
                return $"empty={EmptyCellCount}, rect={LargestEmptyRectangleArea}, region={LargestConnectedEmptyRegion}, 3x3={AvailableThreeByThreeCount}, placeable={PlaceableShapeCount}, easy={EasyShapeCount}, future={FutureOpenAreaScore:F2}";
            }
        }

        public readonly struct PlacementPreview
        {
            public readonly bool IsValid;
            public readonly int LinesCleared;
            public readonly Snapshot Snapshot;
            public readonly float CompositeScore;

            public PlacementPreview(bool isValid, int linesCleared, Snapshot snapshot)
            {
                IsValid = isValid;
                LinesCleared = linesCleared;
                Snapshot = snapshot;
                CompositeScore = snapshot.GetCompositeScore() + (linesCleared * 5f);
            }
        }

        public static Snapshot Evaluate(BoardState board, IReadOnlyList<ShapeDefinition> candidateShapes = null)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            int emptyCellCount = CountEmptyCells(board);
            int largestEmptyRectangleArea = FindLargestEmptyRectangleArea(board);
            int largestConnectedEmptyRegion = FindLargestConnectedEmptyRegion(board);
            int availableThreeByThreeCount = CountAvailableSquares(board, 3);
            int placeableShapeCount = 0;
            int easyShapeCount = 0;

            if (candidateShapes != null)
            {
                for (int i = 0; i < candidateShapes.Count; i++)
                {
                    var shape = candidateShapes[i];
                    if (shape == null)
                        continue;

                    if (PlacementSearch.HasAnyValidPlacement(board, shape))
                    {
                        placeableShapeCount++;
                        if (shape.Offsets.Length <= 2)
                            easyShapeCount++;
                    }
                }
            }

            float futureOpenAreaScore = ComputeFutureOpenAreaScore(board, emptyCellCount, largestEmptyRectangleArea, largestConnectedEmptyRegion, availableThreeByThreeCount);
            return new Snapshot(
                emptyCellCount,
                largestEmptyRectangleArea,
                largestConnectedEmptyRegion,
                availableThreeByThreeCount,
                placeableShapeCount,
                easyShapeCount,
                futureOpenAreaScore);
        }

        public static PlacementPreview EvaluatePlacement(BoardState board, ShapeDefinition shape, Int2 anchor, IReadOnlyList<ShapeDefinition> futureShapes = null)
        {
            if (board == null || shape == null)
                return new PlacementPreview(false, 0, default);

            var previewBoard = board.Clone();
            var placement = PlacementEngine.PlaceAtomic(previewBoard, anchor.X, anchor.Y, shape.Offsets, blockId: 1, colorId: 1, out _);
            if (placement != PlacementResult.Success)
                return new PlacementPreview(false, 0, default);

            var lines = new LineDetectResult(previewBoard.Width, previewBoard.Height);
            LineDetector.DetectFullLines(previewBoard, lines);
            int linesCleared = lines.FullRowCount + lines.FullColumnCount;
            if (linesCleared > 0)
            {
                var rows = new int[lines.FullRowCount];
                var cols = new int[lines.FullColumnCount];
                Array.Copy(lines.FullRows, rows, lines.FullRowCount);
                Array.Copy(lines.FullColumns, cols, lines.FullColumnCount);
                LineClearer.ClearLines(previewBoard, rows, cols);
            }

            return new PlacementPreview(true, linesCleared, Evaluate(previewBoard, futureShapes));
        }

        public static float FindBestPlacementScore(BoardState board, ShapeDefinition shape, IReadOnlyList<ShapeDefinition> futureShapes = null)
        {
            if (board == null || shape == null)
                return float.MinValue;

            var placements = PlacementSearch.FindValidPlacements(board, shape);
            if (placements == null || placements.Length == 0)
                return float.MinValue;

            float bestScore = float.MinValue;
            for (int i = 0; i < placements.Length; i++)
            {
                var preview = EvaluatePlacement(board, shape, placements[i], futureShapes);
                if (!preview.IsValid)
                    continue;

                float score = preview.CompositeScore;
                if (preview.Snapshot.AvailableThreeByThreeCount <= 0 && shape.Offsets.Length >= 4)
                    score -= 12f;

                if (score > bestScore)
                    bestScore = score;
            }

            return bestScore;
        }

        public static int CountAvailableSquares(BoardState board, int squareSize)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (squareSize <= 0)
                return 0;

            int count = 0;
            int maxX = board.Width - squareSize;
            int maxY = board.Height - squareSize;
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    if (IsSquareAreaEmpty(board, x, y, squareSize))
                        count++;
                }
            }

            return count;
        }

        public static bool IsSquareAreaEmpty(BoardState board, int startX, int startY, int squareSize)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            for (int y = 0; y < squareSize; y++)
            {
                for (int x = 0; x < squareSize; x++)
                {
                    if (!board.IsInBounds(startX + x, startY + y) || board.IsOccupied(startX + x, startY + y))
                        return false;
                }
            }

            return true;
        }

        private static int CountEmptyCells(BoardState board)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.IsEmpty(x, y))
                        count++;
                }
            }

            return count;
        }

        private static int FindLargestEmptyRectangleArea(BoardState board)
        {
            int width = board.Width;
            int height = board.Height;
            var histogram = new int[width];
            int bestArea = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    histogram[x] = board.IsEmpty(x, y) ? histogram[x] + 1 : 0;

                bestArea = Math.Max(bestArea, FindLargestHistogramArea(histogram));
            }

            return bestArea;
        }

        private static int FindLargestHistogramArea(int[] heights)
        {
            var stack = new Stack<int>(heights.Length);
            int bestArea = 0;

            for (int i = 0; i <= heights.Length; i++)
            {
                int currentHeight = i == heights.Length ? 0 : heights[i];
                while (stack.Count > 0 && currentHeight < heights[stack.Peek()])
                {
                    int height = heights[stack.Pop()];
                    int left = stack.Count == 0 ? -1 : stack.Peek();
                    int width = i - left - 1;
                    bestArea = Math.Max(bestArea, height * width);
                }

                stack.Push(i);
            }

            return bestArea;
        }

        private static int FindLargestConnectedEmptyRegion(BoardState board)
        {
            int width = board.Width;
            int height = board.Height;
            var visited = new bool[width, height];
            var queue = new Queue<Int2>();
            int best = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (visited[x, y] || board.IsOccupied(x, y))
                        continue;

                    visited[x, y] = true;
                    queue.Enqueue(new Int2(x, y));
                    int regionSize = 0;

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        regionSize++;
                        EnqueueIfEmpty(board, current.X + 1, current.Y, visited, queue);
                        EnqueueIfEmpty(board, current.X - 1, current.Y, visited, queue);
                        EnqueueIfEmpty(board, current.X, current.Y + 1, visited, queue);
                        EnqueueIfEmpty(board, current.X, current.Y - 1, visited, queue);
                    }

                    best = Math.Max(best, regionSize);
                }
            }

            return best;
        }

        private static void EnqueueIfEmpty(BoardState board, int x, int y, bool[,] visited, Queue<Int2> queue)
        {
            if (!board.IsInBounds(x, y) || visited[x, y] || board.IsOccupied(x, y))
                return;

            visited[x, y] = true;
            queue.Enqueue(new Int2(x, y));
        }

        private static float ComputeFutureOpenAreaScore(
            BoardState board,
            int emptyCellCount,
            int largestEmptyRectangleArea,
            int largestConnectedEmptyRegion,
            int availableThreeByThreeCount)
        {
            int totalCells = Math.Max(1, board.Width * board.Height);
            float emptyRatio = emptyCellCount / (float)totalCells;
            float rectRatio = largestEmptyRectangleArea / (float)totalCells;
            float regionRatio = largestConnectedEmptyRegion / (float)totalCells;
            float threeByThreePressure = availableThreeByThreeCount > 0 ? 1f : 0f;

            return
                (emptyRatio * 0.32f) +
                (rectRatio * 0.28f) +
                (regionRatio * 0.22f) +
                (threeByThreePressure * 0.18f);
        }
    }
}
