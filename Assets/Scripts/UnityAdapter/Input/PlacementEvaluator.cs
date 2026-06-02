using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Boot;

namespace BlockPuzzle.UnityAdapter.Input
{
    /// <summary>
    /// Handles logical board evaluations used by drag preview.
    /// </summary>
    internal sealed class PlacementEvaluator
    {
        private readonly GameBootstrap _bootstrap;
        private readonly HashSet<(int, int)> _shapeCells = new HashSet<(int, int)>();
        private readonly HashSet<int> _touchedRows = new HashSet<int>();
        private readonly HashSet<int> _touchedCols = new HashSet<int>();

        public PlacementEvaluator(GameBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        public PlacementResult EvaluatePlacementResult(ShapeDefinition shape, Int2 anchor, IReadOnlyList<Int2> offsets = null)
        {
            if (_bootstrap?.Engine == null || shape == null)
                return PlacementResult.OutOfBounds;

            var board = _bootstrap.Engine.CurrentState.Board;
            var shapeOffsets = offsets ?? shape.GetOffsets();

            return PlacementEngine.CanPlace(board, anchor.X, anchor.Y, shapeOffsets);
        }

        public bool CanPlaceAtAnchor(ShapeDefinition shape, Int2 anchor, IReadOnlyList<Int2> offsets = null)
        {
            return EvaluatePlacementResult(shape, anchor, offsets) == PlacementResult.Success;
        }

        public void EvaluateLinesToClear(ShapeDefinition shape, Int2 anchor, List<int> fullRows, List<int> fullCols, IReadOnlyList<Int2> offsets = null)
        {
            fullRows.Clear();
            fullCols.Clear();

            if (_bootstrap?.Engine == null || shape == null)
                return;

            var board = _bootstrap.Engine.CurrentState.Board;
            var shapeOffsets = offsets ?? shape.GetOffsets();

            _shapeCells.Clear();
            _touchedRows.Clear();
            _touchedCols.Clear();

            foreach (var offset in shapeOffsets)
            {
                int cx = anchor.X + offset.X;
                int cy = anchor.Y + offset.Y;

                if (cx < 0 || cx >= board.Width || cy < 0 || cy >= board.Height)
                    return;

                _shapeCells.Add((cx, cy));
                _touchedRows.Add(cy);
                _touchedCols.Add(cx);
            }

            foreach (int row in _touchedRows)
            {
                int filledCount = 0;
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.IsOccupied(x, row) || _shapeCells.Contains((x, row)))
                        filledCount++;
                }

                if (filledCount == board.Width)
                    fullRows.Add(row);
            }

            foreach (int col in _touchedCols)
            {
                int filledCount = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.IsOccupied(col, y) || _shapeCells.Contains((col, y)))
                        filledCount++;
                }

                if (filledCount == board.Height)
                    fullCols.Add(col);
            }
        }
    }
}
