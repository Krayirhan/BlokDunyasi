using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Owns placement preview ghost renderers for <see cref="SimpleGridView"/>.
    /// </summary>
    internal sealed class GridPreviewRenderer
    {
        private readonly List<SpriteRenderer> _dropPreviewRenderers = new List<SpriteRenderer>(8);

        public void Reset()
        {
            _dropPreviewRenderers.Clear();
        }

        public void RenderPlacementGhost(
            Transform parent,
            int width,
            int height,
            Int2 anchor,
            IReadOnlyList<Int2> offsets,
            Color previewColor,
            Sprite previewSprite,
            int sortingOrder,
            float targetCellSize,
            Func<int, int, Vector3> getWorldPosition,
            Func<Sprite, float, float> getSpriteScale)
        {
            ClearPlacementPreview();

            if (offsets == null || width <= 0 || height <= 0 || previewSprite == null)
                return;

            for (int i = 0; i < offsets.Count; i++)
            {
                Int2 offset = offsets[i];
                int x = anchor.X + offset.X;
                int y = anchor.Y + offset.Y;

                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                SpriteRenderer renderer = GetOrCreateDropPreviewRenderer(parent, i);
                renderer.sprite = previewSprite;
                renderer.color = previewColor;
                renderer.sortingOrder = sortingOrder;
                renderer.transform.position = getWorldPosition(x, y);
                renderer.transform.rotation = Quaternion.identity;

                float scale = getSpriteScale(previewSprite, targetCellSize);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
            }
        }

        public void ClearPlacementPreview()
        {
            for (int i = 0; i < _dropPreviewRenderers.Count; i++)
            {
                SpriteRenderer renderer = _dropPreviewRenderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = false;
                renderer.gameObject.SetActive(false);
            }
        }

        private SpriteRenderer GetOrCreateDropPreviewRenderer(Transform parent, int index)
        {
            while (_dropPreviewRenderers.Count <= index)
            {
                var obj = new GameObject("DropPreviewCell");
                obj.transform.SetParent(parent, false);
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                obj.SetActive(false);
                _dropPreviewRenderers.Add(renderer);
            }

            return _dropPreviewRenderers[index];
        }
    }
}
