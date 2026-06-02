using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Owns backdrop and border renderer lifecycle for <see cref="SimpleGridView"/>.
    /// </summary>
    internal sealed class GridBackdropView
    {
        private SpriteRenderer _boardBackdropRenderer;
        private SpriteRenderer _boardBackdropBorderRenderer;

        public void Reset()
        {
            _boardBackdropRenderer = null;
            _boardBackdropBorderRenderer = null;
        }

        public void EnsureBoardBackdrop(
            Transform parent,
            int width,
            int height,
            float totalCellSize,
            float cellSize,
            bool showBoardBackdrop,
            bool showBoardBackdropBorder,
            float paddingInCells,
            float borderThicknessInCells,
            int sortingOrder,
            Vector2 offset,
            Color backdropColor,
            Color borderColor,
            Sprite backdropSprite,
            Sprite borderSprite)
        {
            if (width <= 0 || height <= 0)
                return;

            if (!showBoardBackdrop)
            {
                SetBackdropRendererEnabled(_boardBackdropRenderer, false);
                SetBackdropRendererEnabled(_boardBackdropBorderRenderer, false);
                return;
            }

            float step = totalCellSize;
            float boardWidth = ((width - 1) * step) + cellSize;
            float boardHeight = ((height - 1) * step) + cellSize;
            float paddingWorld = paddingInCells * step;

            var backdrop = GetOrCreateBackdropRenderer(parent, ref _boardBackdropRenderer, "BoardBackdrop", sortingOrder);
            backdrop.sprite = backdropSprite;
            backdrop.color = backdropColor;
            backdrop.enabled = true;
            backdrop.transform.localPosition = (Vector3)offset;
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = new Vector3(boardWidth + paddingWorld * 2f, boardHeight + paddingWorld * 2f, 1f);

            if (!showBoardBackdropBorder || borderThicknessInCells <= 0f)
            {
                SetBackdropRendererEnabled(_boardBackdropBorderRenderer, false);
                return;
            }

            float borderThicknessWorld = borderThicknessInCells * step;
            var border = GetOrCreateBackdropRenderer(parent, ref _boardBackdropBorderRenderer, "BoardBackdropBorder", sortingOrder - 1);
            border.sprite = borderSprite;
            border.color = borderColor;
            border.enabled = true;
            border.transform.localPosition = (Vector3)offset;
            border.transform.localRotation = Quaternion.identity;
            border.transform.localScale = new Vector3(
                boardWidth + paddingWorld * 2f + borderThicknessWorld * 2f,
                boardHeight + paddingWorld * 2f + borderThicknessWorld * 2f,
                1f);
        }

        private static SpriteRenderer GetOrCreateBackdropRenderer(Transform parent, ref SpriteRenderer renderer, string objectName, int sortingOrder)
        {
            if (renderer == null)
            {
                var obj = new GameObject(objectName);
                obj.transform.SetParent(parent, false);
                renderer = obj.AddComponent<SpriteRenderer>();
            }

            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void SetBackdropRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }
}
