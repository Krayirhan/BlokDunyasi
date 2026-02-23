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

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = color;
        }

        public void SetSortingOrder(int order)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sortingOrder = order;
        }
    }
}
