using UnityEngine;
using System.Collections.Generic;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Configuration;

namespace BlockPuzzle.UnityAdapter.Blocks
{
    /// <summary>
    /// YENİ TEMİZ BLOCK SİSTEMİ
    /// 
    /// KRİTİK FARK: 
    /// - Eski sistem: Şekil görsel olarak MERKEZLENİYORDU (centerOffset)
    /// - Yeni sistem: (0,0) ANCHOR hücresi block transform'un MERKEZİNDE
    /// 
    /// Bu sayede: pointer pozisyonu = block pozisyonu = anchor grid pozisyonu
    /// Hiç offset hesaplaması gerekmez!
    /// </summary>
    public class NewSimpleBlock : MonoBehaviour
    {
        [Header("=== GRID MATCH SETTINGS ===")]
        [SerializeField] public float cellSize = 0.5f;
        [SerializeField] public float cellSpacing = 0f;

        [Header("=== VISUAL SETTINGS ===")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 dragScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private int normalSortingOrder = 5;
        [SerializeField] private int dragSortingOrder = 15;

        [Header("=== COLOR PALETTE ===")]
        [SerializeField] private Color[] blockColors = new Color[]
        {
            new Color(0.9f, 0.3f, 0.8f),  // Magenta
            new Color(0.5f, 0.3f, 0.9f),  // Purple
            new Color(0.2f, 0.7f, 1f),    // Cyan
            new Color(1f, 0.5f, 0.2f),    // Orange
            new Color(0.3f, 0.8f, 0.4f),  // Green
            new Color(1f, 0.2f, 0.2f),    // Red
            new Color(0.9f, 0.8f, 0.1f),  // Yellow
            new Color(0.7f, 0.4f, 1f)     // Violet
        };

        [Header("=== SPRITE CONFIG ===")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        // Public properties
        public ShapeDefinition BlockShape { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsUsed { get; private set; }
        public int SlotIndex { get; private set; }
        public Vector3 OriginalPosition { get; private set; }

        // Cell tracking
        private readonly List<GameObject> _cellObjects = new List<GameObject>();
        private Sprite _defaultSprite;

        public void Initialize(ShapeDefinition shape, BlockSpriteConfig config, int slotIndex = 0)
        {
            if (shape == null)
            {
                Debug.LogError("[NewSimpleBlock] Shape is null!");
                return;
            }

            BlockShape = shape;
            spriteConfig = config;
            SlotIndex = slotIndex;
            IsUsed = false;
            OriginalPosition = transform.position;

            CreateCellsAroundAnchor();

            Debug.Log($"[NewSimpleBlock] Initialized: {shape.Name} with {shape.Offsets.Length} cells at slot {slotIndex}");
        }

        /// <summary>
        /// Hücreleri ANCHOR (0,0) MERKEZLİ oluştur.
        /// (0,0) offset'li hücre block transform'un tam merkezinde olacak.
        /// </summary>
        private void CreateCellsAroundAnchor()
        {
            ClearCells();

            if (BlockShape?.Offsets == null) return;

            float step = cellSize + cellSpacing;
            Color color = GetBlockColor();
            Sprite sprite = GetCellSprite();

            foreach (var offset in BlockShape.Offsets)
            {
                // (0,0) anchor transform merkezinde, diğerleri offset'e göre
                // X pozitif = sağa, Y pozitif = aşağı (Unity Y ekseni ters)
                Vector3 localPos = new Vector3(
                    offset.X * step,      // X: offset.X * step
                    -offset.Y * step,     // Y: offset.Y negatif (grid Y aşağı)
                    0f
                );

                var cellObj = CreateCell($"Cell_{offset.X}_{offset.Y}", localPos, color, sprite);
                _cellObjects.Add(cellObj);
            }
        }

        private GameObject CreateCell(string name, Vector3 localPos, Color color, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = normalSortingOrder;

            // Sprite'ı cellSize'a göre ölçekle
            float spriteSize = sprite != null && sprite.bounds.size.x > 0 ? sprite.bounds.size.x : 1f;
            float scale = cellSize / spriteSize;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            return go;
        }

        private Sprite GetCellSprite()
        {
            if (spriteConfig != null)
            {
                var sprite = spriteConfig.GetBlockSprite(SlotIndex);
                if (sprite != null) return sprite;
            }

            if (_defaultSprite == null)
                _defaultSprite = CreateDefaultSprite();

            return _defaultSprite;
        }

        private Sprite CreateDefaultSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            var colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Color GetBlockColor()
        {
            if (blockColors == null || blockColors.Length == 0)
                return Color.magenta;
            return blockColors[SlotIndex % blockColors.Length];
        }

        // === DRAG OPERATIONS ===

        public void StartDrag()
        {
            if (IsDragging) return;
            IsDragging = true;
            ApplyDragVisuals(true);
        }

        public void EndDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            ApplyDragVisuals(false);
        }

        public void UpdateDragPosition(Vector3 worldPos)
        {
            transform.position = worldPos;
        }

        public void ReturnToOriginalPosition()
        {
            transform.position = OriginalPosition;
            EndDrag();
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            gameObject.SetActive(false);
        }

        public void ResetBlock()
        {
            IsUsed = false;
            transform.position = OriginalPosition;
            transform.localScale = normalScale;
            gameObject.SetActive(true);
            EndDrag();
        }

        private void ApplyDragVisuals(bool dragging)
        {
            transform.localScale = dragging ? dragScale : normalScale;
            int order = dragging ? dragSortingOrder : normalSortingOrder;
            Color baseColor = GetBlockColor();
            Color color = dragging ? (baseColor * 1.2f) : baseColor;
            color.a = dragging ? 0.9f : 1f;

            foreach (var cell in _cellObjects)
            {
                if (cell == null) continue;
                var sr = cell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = order;
                    sr.color = color;
                }
            }
        }

        private void ClearCells()
        {
            foreach (var cell in _cellObjects)
            {
                if (cell != null)
                    Destroy(cell);
            }
            _cellObjects.Clear();
        }

        // === PUBLIC HELPERS ===

        public Int2[] GetShapeOffsets()
        {
            return BlockShape?.Offsets ?? new Int2[0];
        }

        public Bounds GetBounds()
        {
            if (_cellObjects.Count == 0)
                return new Bounds(transform.position, Vector3.one * cellSize);

            Bounds bounds = new Bounds();
            bool hasBounds = false;

            foreach (var cell in _cellObjects)
            {
                if (cell == null) continue;
                var sr = cell.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (!hasBounds)
                {
                    bounds = sr.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(sr.bounds);
                }
            }

            if (!hasBounds)
                return new Bounds(transform.position, Vector3.one * cellSize);

            return bounds;
        }

        public bool ContainsPoint(Vector2 worldPos, float radius)
        {
            foreach (var cell in _cellObjects)
            {
                if (cell == null) continue;
                float dist = Vector2.Distance(worldPos, cell.transform.position);
                if (dist <= radius)
                    return true;
            }
            return false;
        }

        public float GetClosestCellDistance(Vector2 worldPos)
        {
            float best = float.MaxValue;
            foreach (var cell in _cellObjects)
            {
                if (cell == null) continue;
                float dist = Vector2.Distance(worldPos, cell.transform.position);
                if (dist < best)
                    best = dist;
            }
            return best;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Anchor noktasını göster
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);

            // Bounds'u göster
            Gizmos.color = IsDragging ? Color.yellow : Color.cyan;
            var bounds = GetBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
