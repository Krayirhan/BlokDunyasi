using UnityEngine;
using System.Collections.Generic;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Configuration;
using UnityEngine.Rendering;
using Debug = BlockPuzzle.Core.Common.GameLogger;

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
        // Statik Random instance - tüm bloklar aynı RNG'yi kullanır
        private static readonly System.Random _rng = new System.Random();

        [Header("=== GRID MATCH SETTINGS ===")]
        [SerializeField] public float cellSize = 0.5f;
        [SerializeField] public float cellSpacing = 0f;

        [Header("=== VISUAL SETTINGS ===")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 dragScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private int normalSortingOrder = 10;
        [SerializeField] private int dragSortingOrder = 100;

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

        [Header("=== LIGHTING (INSPECTOR) ===")]
        [SerializeField] [Range(0.5f, 2.0f)] private float trayBlockBrightness = 1.0f;
        [SerializeField] [Range(1.0f, 2.0f)] private float dragBrightnessMultiplier = 1.2f;
        [SerializeField] [Range(0f, 1f)] private float normalAlpha = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float dragAlpha = 0.9f;

        [Header("=== SPRITE CONFIG ===")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        // Public properties
        public ShapeDefinition BlockShape { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsUsed { get; private set; }
        public int SlotIndex { get; private set; }
        public Vector3 OriginalPosition { get; private set; }
        public int ColorId { get; private set; }

        public Color BlockColor
        {
            get
            {
                if (blockColors == null || blockColors.Length == 0) return Color.white;
                int index = (ColorId - 1) % blockColors.Length;
                if (index < 0) index = 0;
                return blockColors[index];
            }
        }

        // Cell tracking
        private readonly List<GameObject> _cellObjects = new List<GameObject>();
        private readonly Queue<GameObject> _cellPool = new Queue<GameObject>();
        private Sprite _defaultSprite;
        private int _lastColorGradingVersion = -1;
        private SortingGroup _sortingGroup;
        private Coroutine _dragScaleRoutine;

        public void Initialize(ShapeDefinition shape, BlockSpriteConfig config, int slotIndex = 0, int colorId = -1)
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
            EnsureSortingGroup();
            // ColorId: parametreden alınırsa o kullanılır, değilse rastgele atanır
            if (colorId > 0)
            {
                ColorId = colorId; // Verilen colorId kullan
            }
            else
            {
                int paletteSize = (blockColors != null && blockColors.Length > 0) ? blockColors.Length : 1;
                ColorId = 1 + _rng.Next(paletteSize); // Statik RNG kullan
            }

            CreateCellsAroundAnchor();
            _lastColorGradingVersion = ProjectColorGrading.SettingsVersion;

        }

        private void Update()
        {
            int version = ProjectColorGrading.SettingsVersion;
            if (_lastColorGradingVersion == version)
                return;

            _lastColorGradingVersion = version;
            RefreshVisuals();
        }

        public void ApplyVisualSettings(Color[] palette, float brightness, float dragBrightness, float idleAlpha, float draggingAlpha)
        {
            if (palette != null && palette.Length > 0)
            {
                if (blockColors == null || blockColors.Length != palette.Length)
                    blockColors = new Color[palette.Length];

                for (int i = 0; i < palette.Length; i++)
                    blockColors[i] = palette[i];
            }

            trayBlockBrightness = Mathf.Clamp(brightness, 0.5f, 2.0f);
            dragBrightnessMultiplier = Mathf.Clamp(dragBrightness, 1.0f, 2.0f);
            normalAlpha = Mathf.Clamp01(idleAlpha);
            dragAlpha = Mathf.Clamp01(draggingAlpha);

            RefreshVisuals();
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

                var cellObj = AcquireCell($"Cell_{offset.X}_{offset.Y}");
                ConfigureCell(cellObj, localPos, color, sprite);
                _cellObjects.Add(cellObj);
            }
        }

        private GameObject AcquireCell(string name)
        {
            GameObject go;
            if (_cellPool.Count > 0)
            {
                go = _cellPool.Dequeue();
            }
            else
            {
                go = new GameObject(name);
                go.AddComponent<SpriteRenderer>();
            }

            go.name = name;
            go.transform.SetParent(transform, false);
            go.SetActive(true);
            return go;
        }

        private void ConfigureCell(GameObject go, Vector3 localPos, Color color, Sprite sprite)
        {
            go.transform.localPosition = localPos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = normalSortingOrder;

            // Sprite'ı cellSize'a göre ölçekle
            float spriteSize = sprite != null && sprite.bounds.size.x > 0 ? sprite.bounds.size.x : 1f;
            float scale = cellSize / spriteSize;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetCellMetrics(float newCellSize, float newCellSpacing, bool rebuild)
        {
            cellSize = Mathf.Max(0.01f, newCellSize);
            cellSpacing = Mathf.Max(0f, newCellSpacing);

            if (rebuild && BlockShape != null)
                CreateCellsAroundAnchor();

            ApplyDragVisuals(IsDragging);
        }

        public void SetOriginalPosition(Vector3 position)
        {
            OriginalPosition = position;
        }

        public void SetSortingOrder(int order)
        {
            EnsureSortingGroup();
            if (_sortingGroup != null)
                _sortingGroup.sortingOrder = order;

            foreach (var cell in _cellObjects)
            {
                if (cell == null)
                    continue;

                var sr = cell.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = order;
            }
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            foreach (var cell in _cellObjects)
            {
                if (cell == null)
                    continue;

                var sr = cell.GetComponent<SpriteRenderer>();
                if (sr == null)
                    continue;

                Color color = sr.color;
                color.a = alpha;
                sr.color = color;
            }
        }

        public void BeginDragVisual(float boardCellSize, float boardCellSpacing)
        {
            SetCellMetrics(boardCellSize, boardCellSpacing, true);
            StartDrag();
            transform.localScale = Vector3.one;
            SetSortingOrder(Mathf.Max(101, dragSortingOrder));
        }

        public void ReturnToTrayVisual(float trayCellSize, float trayCellSpacing)
        {
            SetCellMetrics(trayCellSize, trayCellSpacing, true);
            EndDrag();
            transform.localScale = Vector3.one;
            SetSortingOrder(normalSortingOrder);
        }

        private Sprite GetCellSprite()
        {
            if (spriteConfig != null)
            {
                var sprite = spriteConfig.GetBlockSpriteByColorId(ColorId);
                if (sprite != null)
                    return sprite;
            }

            if (_defaultSprite == null)
                _defaultSprite = CreateDefaultSprite();

            return _defaultSprite;
        }

        private Sprite CreateDefaultSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Color GetBlockColor()
        {
            if (UsesAssignedSprite())
                return new Color(1f, 1f, 1f, 1f);

            return ApplyLighting(GetPaletteColor(), trayBlockBrightness, normalAlpha);
        }

        // === DRAG OPERATIONS ===

        public void StartDrag()
        {
            if (IsDragging) return;
            IsDragging = true;
            ApplyDragVisuals(true);
            StartScaleTransition(dragScale, 0.12f);
        }

        public void EndDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            ApplyDragVisuals(false);
            StartScaleTransition(normalScale, 0.1f);
        }

        private void StartScaleTransition(Vector3 targetScale, float duration)
        {
            if (_dragScaleRoutine != null)
                StopCoroutine(_dragScaleRoutine);

            _dragScaleRoutine = StartCoroutine(ScaleTransitionRoutine(targetScale, duration));
        }

        private System.Collections.IEnumerator ScaleTransitionRoutine(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            transform.localScale = targetScale;
            _dragScaleRoutine = null;
        }

        public void UpdateDragPosition(Vector3 worldPos)
        {
            transform.position = worldPos;
        }

        public bool TryGetClosestCellOffset(Vector2 worldPos, out Int2 closestOffset)
        {
            closestOffset = default;
            if (BlockShape?.Offsets == null || BlockShape.Offsets.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            foreach (var offset in BlockShape.Offsets)
            {
                Vector3 cellWorld = transform.TransformPoint(GetLocalCellPosition(offset));
                float distance = Vector2.Distance(worldPos, cellWorld);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closestOffset = offset;
                }
            }

            return true;
        }

        public Vector3 GetLocalCellPosition(Int2 offset)
        {
            float step = cellSize + cellSpacing;
            return new Vector3(offset.X * step, -offset.Y * step, 0f);
        }

        public void ReturnToOriginalPosition()
        {
            transform.position = OriginalPosition;
            transform.localScale = Vector3.one;
            EndDrag();
        }

        public void RejectPlacement(float trayCellSize, float trayCellSpacing)
        {
            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);
                
            _shakeRoutine = StartCoroutine(ShakeRoutine(trayCellSize, trayCellSpacing));
        }

        private Coroutine _shakeRoutine;
        private System.Collections.IEnumerator ShakeRoutine(float trayCellSize, float trayCellSpacing)
        {
            float duration = 0.25f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color originalColor = GetPaletteColor();
            Color redTint = Color.red;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Fast shake decreasing over time
                float offsetX = Mathf.Sin(elapsed * 60f) * 0.15f * (1f - t);
                transform.position = startPos + new Vector3(offsetX, 0, 0);

                // Blend color to red and back
                Color flashColor = Color.Lerp(redTint, ApplyLighting(originalColor, trayBlockBrightness, dragAlpha), t);
                foreach (var cell in _cellObjects)
                {
                    if (cell != null)
                    {
                        var sr = cell.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = flashColor;
                    }
                }
                
                yield return null;
            }

            ReturnToTrayVisual(trayCellSize, trayCellSpacing);
            ReturnToOriginalPosition();
            RefreshVisuals();
        }

        public void SetSpriteConfig(BlockSpriteConfig config, bool rebuildCells)
        {
            spriteConfig = config;

            if (rebuildCells && BlockShape != null)
                CreateCellsAroundAnchor();
            else
                RefreshVisuals();
        }

        public void MarkAsUsed()
        {
            EndDrag();
            IsUsed = true;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        public void ResetBlock()
        {
            IsUsed = false;
            transform.position = OriginalPosition;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            EndDrag();
        }

        public void PrepareForPool()
        {
            CancelTransientAnimations();
            IsUsed = false;
            transform.localScale = Vector3.one;
            IsDragging = false;
            ApplyDragVisuals(false);
            gameObject.SetActive(false);
        }

        private void CancelTransientAnimations()
        {
            if (_dragScaleRoutine != null)
            {
                StopCoroutine(_dragScaleRoutine);
                _dragScaleRoutine = null;
            }

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeRoutine = null;
            }
        }

        private void ApplyDragVisuals(bool dragging)
        {
            EnsureSortingGroup();
            transform.localScale = Vector3.one;
            int order = dragging ? dragSortingOrder : normalSortingOrder;
            if (dragging)
                order = Mathf.Max(101, order);
            if (_sortingGroup != null)
                _sortingGroup.sortingOrder = order;
            Color color;
            if (UsesAssignedSprite())
            {
                color = new Color(1f, 1f, 1f, dragging ? dragAlpha : normalAlpha);
            }
            else
            {
                Color paletteColor = GetPaletteColor();
                color = dragging
                    ? ApplyLighting(paletteColor, trayBlockBrightness * dragBrightnessMultiplier, dragAlpha)
                    : ApplyLighting(paletteColor, trayBlockBrightness, normalAlpha);
            }

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

        private void RefreshVisuals()
        {
            ApplyDragVisuals(IsDragging);
        }

        private Color GetPaletteColor()
        {
            if (blockColors == null || blockColors.Length == 0)
                return Color.magenta;

            int colorIndex = (ColorId - 1) % blockColors.Length;
            if (colorIndex < 0)
                colorIndex += blockColors.Length;

            return blockColors[colorIndex];
        }

        private Color ApplyLighting(Color baseColor, float brightness, float alpha)
        {
            Color result = baseColor;
            result.r *= brightness;
            result.g *= brightness;
            result.b *= brightness;
            result.a = alpha;
            return result;
        }

        private void ClearCells()
        {
            foreach (var cell in _cellObjects)
            {
                if (cell != null)
                {
                    cell.SetActive(false);
                    _cellPool.Enqueue(cell);
                }
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

        private void OnValidate()
        {
            trayBlockBrightness = Mathf.Clamp(trayBlockBrightness, 0.5f, 2.0f);
            dragBrightnessMultiplier = Mathf.Clamp(dragBrightnessMultiplier, 1.0f, 2.0f);
            normalAlpha = Mathf.Clamp01(normalAlpha);
            dragAlpha = Mathf.Clamp01(dragAlpha);

            if (Application.isPlaying)
            {
                RefreshVisuals();
            }
        }

        private void EnsureSortingGroup()
        {
            if (_sortingGroup == null || _sortingGroup.Equals(null))
                _sortingGroup = GetComponent<SortingGroup>();

            if (_sortingGroup == null || _sortingGroup.Equals(null))
                _sortingGroup = gameObject.AddComponent<SortingGroup>();

            if (_sortingGroup != null && !_sortingGroup.Equals(null))
                _sortingGroup.sortingOrder = IsDragging ? dragSortingOrder : normalSortingOrder;
        }

        private bool UsesAssignedSprite()
        {
            return spriteConfig != null &&
                   spriteConfig.GetBlockSpriteByColorId(ColorId) != null;
        }

    }
}
