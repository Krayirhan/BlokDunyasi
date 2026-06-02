using System;
using System.Collections;
using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Owns line-clear feedback visuals and line-clear preview highlights for <see cref="SimpleGridView"/>.
    /// </summary>
    internal sealed class GridLineClearFx
    {
        private readonly List<SpriteRenderer> _lineClearPreviewRenderers = new List<SpriteRenderer>(20);
        private readonly Stack<SpriteRenderer> _lineClearFxPool = new Stack<SpriteRenderer>(8);
        private int _activeLineClearPreviewCount;
        private float _lineClearPreviewPulseTime;

        public bool HasActiveLineClearPreview => _activeLineClearPreviewCount > 0;

        public void Reset()
        {
            _lineClearPreviewRenderers.Clear();
            _lineClearFxPool.Clear();
            _activeLineClearPreviewCount = 0;
            _lineClearPreviewPulseTime = 0f;
        }

        public IEnumerator PlayLineClear(
            Transform parent,
            Int2[] clearedPositions,
            float cellSize,
            Func<int, int, Transform> getCellTransform,
            Func<Sprite> getEffectSprite,
            Func<Sprite, float, float> getSpriteScale)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            var dummies = new List<SpriteRenderer>(clearedPositions.Length);

            foreach (var pos in clearedPositions)
            {
                var cellTransform = getCellTransform(pos.X, pos.Y);
                if (cellTransform == null)
                    continue;

                var sr = AcquireLineClearFxRenderer(parent);
                sr.sprite = getEffectSprite();
                sr.color = Color.white;
                sr.sortingOrder = 20;
                sr.transform.position = cellTransform.position;
                sr.transform.rotation = Quaternion.identity;
                sr.gameObject.SetActive(true);
                sr.enabled = true;

                float scale = getSpriteScale(sr.sprite, cellSize);
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                dummies.Add(sr);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float size = cellSize * (1f + t * 0.3f);
                float alpha = 1f - t;

                foreach (var sr in dummies)
                {
                    if (sr == null)
                        continue;

                    float scale = getSpriteScale(sr.sprite, size);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }

                yield return null;
            }

            foreach (var sr in dummies)
            {
                if (sr != null)
                    ReleaseLineClearFxRenderer(sr);
            }
        }

        public void HighlightLines(
            Transform parent,
            int width,
            int height,
            Func<int, int, bool> hasCell,
            Func<int, int, Vector3> getWorldPosition,
            Func<Sprite> getPreviewSprite,
            Func<Sprite, float, float> getSpriteScale,
            float cellSize,
            float baseScale,
            float minAlpha,
            Color previewColor,
            int sortingOrder,
            List<int> rows,
            List<int> cols)
        {
            ClearLineHighlights();

            int previewIndex = 0;
            if (rows != null)
            {
                foreach (int y in rows)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!hasCell(x, y))
                            continue;

                        ShowLineClearPreviewCell(parent, previewIndex++, x, y, getWorldPosition, getPreviewSprite, getSpriteScale, cellSize, baseScale, minAlpha, previewColor, sortingOrder);
                    }
                }
            }

            if (cols != null)
            {
                foreach (int x in cols)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (!hasCell(x, y))
                            continue;

                        bool alreadyShown = rows != null && rows.Contains(y);
                        if (alreadyShown)
                            continue;

                        ShowLineClearPreviewCell(parent, previewIndex++, x, y, getWorldPosition, getPreviewSprite, getSpriteScale, cellSize, baseScale, minAlpha, previewColor, sortingOrder);
                    }
                }
            }

            _activeLineClearPreviewCount = previewIndex;
        }

        public void ClearLineHighlights()
        {
            _activeLineClearPreviewCount = 0;
            for (int i = 0; i < _lineClearPreviewRenderers.Count; i++)
            {
                SpriteRenderer renderer = _lineClearPreviewRenderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = false;
                renderer.gameObject.SetActive(false);
            }
        }

        public void UpdateLineClearPreviewAnimation(
            float deltaTime,
            bool enablePulse,
            float pulseSpeed,
            float minAlpha,
            float maxAlpha,
            float baseScale,
            float pulseScale,
            Color previewColor,
            float cellSize,
            Func<Sprite, float, float> getSpriteScale)
        {
            if (_activeLineClearPreviewCount <= 0)
                return;

            _lineClearPreviewPulseTime += deltaTime * Mathf.Max(0f, pulseSpeed);
            float pulse = enablePulse ? (Mathf.Sin(_lineClearPreviewPulseTime) * 0.5f) + 0.5f : 0f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            float targetSize = cellSize * (baseScale + (pulseScale * pulse));

            for (int i = 0; i < _activeLineClearPreviewCount && i < _lineClearPreviewRenderers.Count; i++)
            {
                SpriteRenderer renderer = _lineClearPreviewRenderers[i];
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                    continue;

                renderer.color = new Color(previewColor.r, previewColor.g, previewColor.b, alpha);
                float scale = getSpriteScale(renderer.sprite, targetSize);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void ShowLineClearPreviewCell(
            Transform parent,
            int index,
            int x,
            int y,
            Func<int, int, Vector3> getWorldPosition,
            Func<Sprite> getPreviewSprite,
            Func<Sprite, float, float> getSpriteScale,
            float cellSize,
            float baseScale,
            float minAlpha,
            Color previewColor,
            int sortingOrder)
        {
            SpriteRenderer renderer = GetOrCreateLineClearPreviewRenderer(parent, index);
            Sprite sprite = getPreviewSprite();
            renderer.sprite = sprite;
            renderer.color = new Color(previewColor.r, previewColor.g, previewColor.b, minAlpha);
            renderer.sortingOrder = sortingOrder;
            renderer.transform.position = getWorldPosition(x, y);
            renderer.transform.rotation = Quaternion.identity;

            float scale = getSpriteScale(sprite, cellSize * baseScale);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.gameObject.SetActive(true);
            renderer.enabled = true;
        }

        private SpriteRenderer GetOrCreateLineClearPreviewRenderer(Transform parent, int index)
        {
            while (_lineClearPreviewRenderers.Count <= index)
            {
                var obj = new GameObject("LineClearPreviewCell");
                obj.transform.SetParent(parent, false);
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                obj.SetActive(false);
                _lineClearPreviewRenderers.Add(renderer);
            }

            return _lineClearPreviewRenderers[index];
        }

        private SpriteRenderer AcquireLineClearFxRenderer(Transform parent)
        {
            if (_lineClearFxPool.Count > 0)
            {
                SpriteRenderer pooled = _lineClearFxPool.Pop();
                if (pooled != null)
                {
                    pooled.transform.SetParent(parent, false);
                    return pooled;
                }
            }

            var go = new GameObject("LineClearFx");
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private void ReleaseLineClearFxRenderer(SpriteRenderer renderer)
        {
            renderer.sprite = null;
            renderer.enabled = false;
            renderer.gameObject.SetActive(false);
            _lineClearFxPool.Push(renderer);
        }
    }
}
