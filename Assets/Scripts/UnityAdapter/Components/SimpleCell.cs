using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Components
{
    /// <summary>
    /// Minimal cell wrapper for SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleCell : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private SpriteRenderer _borderRenderer;
        private SpriteRenderer _highlightRenderer;
        private int _sortingOrder = 1;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;

            if (_borderRenderer != null)
                _borderRenderer.sprite = sprite;

            if (_highlightRenderer != null)
                _highlightRenderer.sprite = sprite;
        }

        private Color _baseColor = Color.white;

        public void SetColor(Color color)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _baseColor = color;
            _sr.color = color;
        }

        public Color GetColor() => _baseColor;

        public void SetHighlight(bool active)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = _baseColor;

            if (!active)
            {
                if (_highlightRenderer != null)
                    _highlightRenderer.enabled = false;
                return;
            }

            EnsureHighlightRenderer();
            _highlightRenderer.sprite = _sr.sprite;
            _highlightRenderer.color = new Color(1f, 1f, 1f, _baseColor.a);
            _highlightRenderer.sortingOrder = _sortingOrder + 1;
            _highlightRenderer.transform.localPosition = Vector3.zero;
            _highlightRenderer.transform.localRotation = Quaternion.identity;
            _highlightRenderer.transform.localScale = Vector3.one;
            _highlightRenderer.enabled = true;
        }

        public void SetSortingOrder(int order)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sortingOrder = order;
            _sr.sortingOrder = order;

            if (_borderRenderer != null)
                _borderRenderer.sortingOrder = order - 1;

            if (_highlightRenderer != null)
                _highlightRenderer.sortingOrder = order + 1;
        }

        public void SetSharedMaterial(Material material)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sharedMaterial = material;
        }

        public void SetBorderStyle(bool enabled, Color color, float thicknessRatio)
        {
            if (!enabled || thicknessRatio <= 0f)
            {
                if (_borderRenderer != null)
                    _borderRenderer.enabled = false;
                return;
            }

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            EnsureBorderRenderer();

            _borderRenderer.sprite = _sr.sprite;
            _borderRenderer.color = color;
            _borderRenderer.sortingOrder = _sortingOrder - 1;
            _borderRenderer.enabled = true;

            float scaleMultiplier = 1f + Mathf.Max(0f, thicknessRatio * 2f);
            _borderRenderer.transform.localPosition = Vector3.zero;
            _borderRenderer.transform.localRotation = Quaternion.identity;
            _borderRenderer.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
        }

        private void EnsureBorderRenderer()
        {
            if (_borderRenderer != null)
                return;

            Transform borderTransform = transform.Find("Border");
            if (borderTransform != null)
                _borderRenderer = borderTransform.GetComponent<SpriteRenderer>();

            if (_borderRenderer != null)
                return;

            var borderObject = new GameObject("Border");
            borderObject.transform.SetParent(transform, false);
            _borderRenderer = borderObject.AddComponent<SpriteRenderer>();
        }

        private void EnsureHighlightRenderer()
        {
            if (_highlightRenderer != null)
                return;

            Transform highlightTransform = transform.Find("Highlight");
            if (highlightTransform != null)
                _highlightRenderer = highlightTransform.GetComponent<SpriteRenderer>();

            if (_highlightRenderer != null)
                return;

            var highlightObject = new GameObject("Highlight");
            highlightObject.transform.SetParent(transform, false);
            _highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
            _highlightRenderer.enabled = false;
        }
    }
}
