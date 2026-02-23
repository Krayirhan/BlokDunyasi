using UnityEngine;
using System.Collections.Generic;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.UnityAdapter.Input
{
    /// <summary>
    /// YENİ TEMİZ PREVIEW SİSTEMİ
    /// 
    /// Temel Prensipler:
    /// 1. Preview hücreleri grid hücrelerinin TAM ÜSTÜNE yerleşir
    /// 2. Anchor (0,0) + tüm offset'ler gösterilir
    /// 3. Yeşil = yerleştirilebilir, Kırmızı = yerleştirilemez
    /// </summary>
    public class NewPreviewSystem : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private SimpleGridView gridView;

        [Header("=== PREVIEW COLORS ===")]
        [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.3f, 0.6f);
        [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);

        [Header("=== VISUAL SETTINGS ===")]
        [SerializeField] private int sortingOrder = 100;

        // Preview hücreleri
        private readonly List<SpriteRenderer> _previewCells = new List<SpriteRenderer>();
        private ShapeDefinition _currentShape;
        private bool _isActive;
        private Int2 _lastAnchor;
        private Sprite _cellSprite;

        private void Awake()
        {
            if (gridView == null)
                gridView = FindFirstObjectByType<SimpleGridView>();

            // Varsayılan sprite oluştur
            _cellSprite = CreateSquareSprite();
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        /// <summary>
        /// Shape için preview başlat
        /// </summary>
        public void StartPreview(ShapeDefinition shape)
        {
            if (shape == null) return;

            _currentShape = shape;
            _isActive = true;

            // Yeterli preview hücresi oluştur
            EnsurePreviewCells(shape.Offsets.Length);
            
            // Başlangıçta gizle
            HideAllCells();

            Debug.Log($"[NewPreviewSystem] Started preview for shape: {shape.Name} with {shape.Offsets.Length} cells");
        }

        /// <summary>
        /// Preview'u belirtilen anchor pozisyonunda göster
        /// </summary>
        public void ShowPreview(Int2 anchor, bool isValid)
        {
            if (!_isActive || _currentShape == null || gridView == null)
                return;

            _lastAnchor = anchor;
            Color color = isValid ? validColor : invalidColor;

            var offsets = _currentShape.Offsets;
            float cellSize = gridView.CellSize;
            float scale = cellSize * 0.95f; // Biraz küçük göster

            for (int i = 0; i < offsets.Length && i < _previewCells.Count; i++)
            {
                var offset = offsets[i];
                int cellX = anchor.X + offset.X;
                int cellY = anchor.Y + offset.Y;

                // Grid dünya pozisyonunu al
                Vector3 worldPos = gridView.GetWorldPosition(cellX, cellY);
                worldPos.z = -1f; // Gridin önünde

                var cell = _previewCells[i];
                cell.transform.position = worldPos;
                cell.transform.localScale = new Vector3(scale, scale, 1f);
                cell.color = color;
                cell.enabled = true;
            }

            // Fazla hücreleri gizle
            for (int i = offsets.Length; i < _previewCells.Count; i++)
            {
                _previewCells[i].enabled = false;
            }
        }

        /// <summary>
        /// Preview'u gizle
        /// </summary>
        public void HidePreview()
        {
            HideAllCells();
        }

        /// <summary>
        /// Preview'u sonlandır
        /// </summary>
        public void EndPreview(bool wasPlaced)
        {
            _isActive = false;
            _currentShape = null;
            HideAllCells();

            if (wasPlaced)
                Debug.Log("[NewPreviewSystem] Preview ended - block placed");
        }

        private void EnsurePreviewCells(int count)
        {
            while (_previewCells.Count < count)
            {
                var go = new GameObject($"PreviewCell_{_previewCells.Count}");
                go.transform.SetParent(transform);
                
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _cellSprite;
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
                
                _previewCells.Add(sr);
            }
        }

        private void HideAllCells()
        {
            foreach (var cell in _previewCells)
            {
                if (cell != null)
                    cell.enabled = false;
            }
        }

        // Editor'da test için
        private void OnDrawGizmos()
        {
            if (!_isActive || gridView == null || _currentShape == null)
                return;

            Gizmos.color = Color.yellow;
            foreach (var offset in _currentShape.Offsets)
            {
                int cellX = _lastAnchor.X + offset.X;
                int cellY = _lastAnchor.Y + offset.Y;
                Vector3 worldPos = gridView.GetWorldPosition(cellX, cellY);
                Gizmos.DrawWireCube(worldPos, Vector3.one * gridView.CellSize * 0.9f);
            }
        }
    }
}
