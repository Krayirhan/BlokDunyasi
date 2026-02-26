using UnityEngine;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Components;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Basit, temiz grid sistemi. Tek sorumluluk: Grid görselleştirme ve world<->grid dönüşümü.
    /// </summary>
    public class SimpleGridView : MonoBehaviour
    {
        [Header("📁 CONFIGURATION")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        [Header("⚙️ GRID SETTINGS")]
        [SerializeField] [Range(0.3f, 1.0f)] private float cellSize = 0.5f;
        [SerializeField] [Range(0f, 0.2f)] private float cellSpacing = 0f;

        [Header("🎨 COLORS")]
        [SerializeField] private Color emptyCellColor = new Color(0.12f, 0.15f, 0.25f, 1f);
        [SerializeField] private Color filledCellColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Header("🌈 BLOCK COLOR PALETTE (Match SimpleBlock)")]
        [SerializeField] private Color[] blockColors =
        {
            new Color(0.9f, 0.3f, 0.8f),
            new Color(0.5f, 0.3f, 0.9f),
            new Color(0.2f, 0.7f, 1f),
            new Color(1f, 0.5f, 0.2f),
            new Color(0.3f, 0.8f, 0.4f),
            new Color(1f, 0.2f, 0.2f),
            new Color(0.9f, 0.8f, 0.1f),
            new Color(0.7f, 0.4f, 1f)
        };
        [Header("BLOCK LIGHTING (Inspector)")]
        [SerializeField] [Range(0.5f, 2.0f)] private float placedBlockBrightness = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float placedBlockAlpha = 1.0f;

        [Header("LINE CLEAR PREVIEW")]
        [SerializeField] private Color linePreviewTintFilled = new Color(1f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color linePreviewTintEmpty = new Color(0.45f, 0.8f, 1f, 1f);
        [SerializeField] [Range(0f, 1f)] private float linePreviewBlendFilled = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float linePreviewBlendEmpty = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float linePreviewValueBoost = 0.08f;

        [Header("LINE PREVIEW OUTER GLOW")]
        [SerializeField] private bool enableLinePreviewOuterGlow = true;
        [SerializeField] private Color linePreviewGlowColor = new Color(1f, 0.95f, 0.35f, 0.6f);
        [SerializeField] [Range(1f, 1.6f)] private float linePreviewGlowScale = 1.2f;

        [Header("BOARD BACKDROP")]
        [SerializeField] private bool showBoardBackdrop = true;
        [SerializeField] private Color boardBackdropColor = new Color(0.06f, 0.1f, 0.2f, 0.55f);
        [SerializeField] [Range(0f, 2f)] private float boardBackdropPaddingInCells = 0.35f;
        [SerializeField] private bool showBoardBackdropBorder = true;
        [SerializeField] private Color boardBackdropBorderColor = new Color(0.75f, 0.9f, 1f, 0.12f);
        [SerializeField] [Range(0f, 1f)] private float boardBackdropBorderThicknessInCells = 0.12f;
        [SerializeField] [Range(-20, 20)] private int boardBackdropSortingOrder = -2;

        private SimpleCell[,] _cells;
        private SpriteRenderer[,] _linePreviewGlow;
        private SpriteRenderer _boardBackdropRenderer;
        private SpriteRenderer _boardBackdropBorderRenderer;
        private int _width;
        private int _height;
        private bool _isInitialized;
        private BoardState _lastBoardState;
        private Sprite _cachedDefaultSquareSprite;

        // Highlight state
        private readonly HashSet<(int x, int y)> _highlightedCells = new HashSet<(int, int)>();

        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;
        public float TotalCellSize => cellSize + cellSpacing;
        public int Width => _width;
        public int Height => _height;

        public Vector3 GetWorldPosition(int x, int y)
        {
            float totalSize = cellSize + cellSpacing;
            return transform.position + new Vector3(
                (x - (_width - 1) * 0.5f) * totalSize,
                ((_height - 1) * 0.5f - y) * totalSize,
                0
            );
        }

        public bool GetGridPosition(Vector3 worldPos, out int x, out int y)
        {
            Vector3 localPos = worldPos - transform.position;
            float totalSize = cellSize + cellSpacing;

            x = Mathf.RoundToInt(localPos.x / totalSize + (_width - 1) * 0.5f);
            y = Mathf.RoundToInt((_height - 1) * 0.5f - localPos.y / totalSize);

            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private void Start()
        {
            GameBootstrap.OnBoardChanged += OnBoardChanged;
        }

        private void OnDestroy()
        {
            GameBootstrap.OnBoardChanged -= OnBoardChanged;
        }

        private void OnBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            _lastBoardState = boardState;
            if (!_isInitialized)
            {
                InitializeGrid(boardState);
            }

            UpdateGrid(boardState);
        }

        private void InitializeGrid(BoardState boardState)
        {
            _width = boardState.Width;
            _height = boardState.Height;
            _cells = new SimpleCell[_width, _height];
            _linePreviewGlow = new SpriteRenderer[_width, _height];
            _boardBackdropRenderer = null;
            _boardBackdropBorderRenderer = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    CreateCellAt(x, y);
                }
            }

            EnsureBoardBackdrop();
            _isInitialized = true;
            Debug.Log($"[SimpleGridView] Grid initialized: {_width}x{_height}");
        }

        private void CreateCellAt(int x, int y)
        {
            var cellObj = new GameObject($"Cell_{x}_{y}");
            cellObj.transform.SetParent(transform);
            cellObj.transform.position = GetWorldPosition(x, y);

            var spriteRenderer = cellObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1;

            var cell = cellObj.AddComponent<SimpleCell>();
            cell.SetSortingOrder(1);

            var glowObj = new GameObject("LinePreviewGlow");
            glowObj.transform.SetParent(cellObj.transform, false);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * linePreviewGlowScale;

            var glowRenderer = glowObj.AddComponent<SpriteRenderer>();
            glowRenderer.sortingOrder = 0;
            glowRenderer.color = linePreviewGlowColor;
            glowRenderer.enabled = false;

            Sprite emptySprite = spriteConfig?.EmptyCellSprite;
            if (emptySprite != null)
            {
                cell.SetSprite(emptySprite);
                cell.SetColor(Color.white);
                glowRenderer.sprite = emptySprite;

                float spriteWorldSize = emptySprite.bounds.size.x;
                if (spriteWorldSize > 0)
                {
                    float scale = cellSize / spriteWorldSize;
                    cellObj.transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    cellObj.transform.localScale = Vector3.one * cellSize;
                }
            }
            else
            {
                Sprite defaultSprite = GetDefaultSquareSprite();
                cell.SetSprite(defaultSprite);
                cell.SetColor(emptyCellColor);
                glowRenderer.sprite = defaultSprite;
                cellObj.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            }

            _cells[x, y] = cell;
            _linePreviewGlow[x, y] = glowRenderer;
        }

        private Sprite GetDefaultSquareSprite()
        {
            if (_cachedDefaultSquareSprite == null)
                _cachedDefaultSquareSprite = CreateDefaultSquareSprite();

            return _cachedDefaultSquareSprite;
        }

        private Sprite CreateDefaultSquareSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void EnsureBoardBackdrop()
        {
            if (!_isInitialized && (_width <= 0 || _height <= 0))
                return;

            if (_width <= 0 || _height <= 0)
                return;

            if (!showBoardBackdrop)
            {
                SetBackdropRendererEnabled(_boardBackdropRenderer, false);
                SetBackdropRendererEnabled(_boardBackdropBorderRenderer, false);
                return;
            }

            float step = cellSize + cellSpacing;
            float boardWidth = ((_width - 1) * step) + cellSize;
            float boardHeight = ((_height - 1) * step) + cellSize;
            float paddingWorld = boardBackdropPaddingInCells * step;

            var backdrop = GetOrCreateBackdropRenderer(
                ref _boardBackdropRenderer,
                "BoardBackdrop",
                boardBackdropSortingOrder);

            backdrop.sprite = GetDefaultSquareSprite();
            backdrop.color = boardBackdropColor;
            backdrop.enabled = true;
            backdrop.transform.localPosition = Vector3.zero;
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = new Vector3(
                boardWidth + (paddingWorld * 2f),
                boardHeight + (paddingWorld * 2f),
                1f);

            if (!showBoardBackdropBorder || boardBackdropBorderThicknessInCells <= 0f)
            {
                SetBackdropRendererEnabled(_boardBackdropBorderRenderer, false);
                return;
            }

            float borderThicknessWorld = boardBackdropBorderThicknessInCells * step;
            var border = GetOrCreateBackdropRenderer(
                ref _boardBackdropBorderRenderer,
                "BoardBackdropBorder",
                boardBackdropSortingOrder - 1);

            border.sprite = GetDefaultSquareSprite();
            border.color = boardBackdropBorderColor;
            border.enabled = true;
            border.transform.localPosition = Vector3.zero;
            border.transform.localRotation = Quaternion.identity;
            border.transform.localScale = new Vector3(
                boardWidth + (paddingWorld * 2f) + (borderThicknessWorld * 2f),
                boardHeight + (paddingWorld * 2f) + (borderThicknessWorld * 2f),
                1f);
        }

        private SpriteRenderer GetOrCreateBackdropRenderer(ref SpriteRenderer renderer, string objectName, int sortingOrder)
        {
            if (renderer == null)
            {
                var obj = new GameObject(objectName);
                obj.transform.SetParent(transform, false);
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

        private void UpdateGrid(BoardState boardState)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_cells[x, y] == null) continue;

                    bool isFilled = boardState.IsOccupied(x, y);
                    var cellTransform = _cells[x, y].transform;

                    if (isFilled)
                    {
                        var cellState = boardState.GetCell(x, y);
                        Color cellColor = GetBlockColor(cellState.ColorId);

                        Sprite filledSprite = spriteConfig?.GetBlockSpriteByColorId(cellState.ColorId);
                        if (filledSprite != null)
                        {
                            _cells[x, y].SetSprite(filledSprite);
                            _cells[x, y].SetColor(cellColor);
                            if (_linePreviewGlow[x, y] != null)
                                _linePreviewGlow[x, y].sprite = filledSprite;

                            float spriteWorldSize = filledSprite.bounds.size.x;
                            if (spriteWorldSize > 0)
                            {
                                float scale = cellSize / spriteWorldSize;
                                cellTransform.localScale = new Vector3(scale, scale, 1f);
                            }
                        }
                        else
                        {
                            _cells[x, y].SetColor(cellColor);
                        }
                    }
                    else
                    {
                        Sprite emptySprite = spriteConfig?.EmptyCellSprite;
                        if (emptySprite != null)
                        {
                            _cells[x, y].SetSprite(emptySprite);
                            _cells[x, y].SetColor(Color.white);
                            if (_linePreviewGlow[x, y] != null)
                                _linePreviewGlow[x, y].sprite = emptySprite;

                            float spriteWorldSize = emptySprite.bounds.size.x;
                            if (spriteWorldSize > 0)
                            {
                                float scale = cellSize / spriteWorldSize;
                                cellTransform.localScale = new Vector3(scale, scale, 1f);
                            }
                        }
                        else
                        {
                            _cells[x, y].SetColor(emptyCellColor);
                        }
                    }
                }
            }
        }

        private void OnValidate()
        {
            cellSize = Mathf.Clamp(cellSize, 0.3f, 1.0f);
            cellSpacing = Mathf.Clamp(cellSpacing, 0f, 0.2f);
            placedBlockBrightness = Mathf.Clamp(placedBlockBrightness, 0.5f, 2.0f);
            placedBlockAlpha = Mathf.Clamp01(placedBlockAlpha);
            linePreviewBlendFilled = Mathf.Clamp01(linePreviewBlendFilled);
            linePreviewBlendEmpty = Mathf.Clamp01(linePreviewBlendEmpty);
            linePreviewValueBoost = Mathf.Clamp01(linePreviewValueBoost);
            linePreviewGlowScale = Mathf.Clamp(linePreviewGlowScale, 1f, 1.6f);
            boardBackdropPaddingInCells = Mathf.Clamp(boardBackdropPaddingInCells, 0f, 2f);
            boardBackdropBorderThicknessInCells = Mathf.Clamp(boardBackdropBorderThicknessInCells, 0f, 1f);

            if (Application.isPlaying && _isInitialized && _lastBoardState != null)
            {
                UpdateGrid(_lastBoardState);
                EnsureBoardBackdrop();
                ApplyPreviewGlow();
            }
        }

        private Color GetBlockColor(int colorId)
        {
            if (blockColors == null || blockColors.Length == 0)
                return ApplyBlockLighting(new Color(1f, 0.4f, 0.8f));

            int colorIndex = (colorId - 1) % blockColors.Length;
            return ApplyBlockLighting(blockColors[colorIndex]);
        }

        private Color ApplyBlockLighting(Color baseColor)
        {
            Color lit = baseColor * placedBlockBrightness;
            lit.r = Mathf.Clamp01(lit.r);
            lit.g = Mathf.Clamp01(lit.g);
            lit.b = Mathf.Clamp01(lit.b);
            lit.a = placedBlockAlpha;
            return lit;
        }

        // ─── LINE HIGHLIGHT ───────────────────────────────────────────

        /// <summary>
        /// Belirtilen satır ve sütunlardaki hücreleri parlat (drag önizlemesi).
        /// </summary>
        public void HighlightLines(List<int> rows, List<int> cols)
        {
            if (!_isInitialized) return;

            var nextHighlighted = new HashSet<(int x, int y)>();

            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                if (row < 0 || row >= _height) continue;

                for (int x = 0; x < _width; x++)
                    nextHighlighted.Add((x, row));
            }

            for (int i = 0; i < cols.Count; i++)
            {
                int col = cols[i];
                if (col < 0 || col >= _width) continue;

                for (int y = 0; y < _height; y++)
                    nextHighlighted.Add((col, y));
            }

            if (nextHighlighted.Count == 0)
            {
                ClearLineHighlights();
                return;
            }

            if (_highlightedCells.SetEquals(nextHighlighted))
            {
                return;
            }

            RestoreHighlightedCells();
            _highlightedCells.Clear();

            foreach (var cell in nextHighlighted)
                _highlightedCells.Add(cell);
            
            ApplyPreviewHighlight();
            ApplyPreviewGlow();
        }

        /// <summary>
        /// Tüm highlight'ları temizle ve orijinal renklere geri dön.
        /// </summary>
        public void ClearLineHighlights()
        {
            RestoreHighlightedCells();

            _highlightedCells.Clear();
        }

        private void RestoreHighlightedCells()
        {
            if (!_isInitialized || _lastBoardState == null)
                return;

            foreach (var (x, y) in _highlightedCells)
            {
                RestoreCellColor(x, y);
                SetCellGlow(x, y, false);
            }
        }

        private void ApplyPreviewHighlight()
        {
            foreach (var (x, y) in _highlightedCells)
                ApplyPreviewHighlightToCell(x, y);
        }

        private void ApplyPreviewGlow()
        {
            foreach (var (x, y) in _highlightedCells)
                SetCellGlow(x, y, true);
        }

        private void ApplyPreviewHighlightToCell(int x, int y)
        {
            if (_cells == null || _lastBoardState == null) return;
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            if (_cells[x, y] == null) return;

            Color baseColor = GetBaseCellColor(x, y, out bool isFilled);
            Color tint = isFilled ? linePreviewTintFilled : linePreviewTintEmpty;
            float blend = isFilled ? linePreviewBlendFilled : linePreviewBlendEmpty;

            Color mixed = Color.Lerp(baseColor, tint, blend);
            mixed.r = Mathf.Clamp01(mixed.r * (1f + linePreviewValueBoost));
            mixed.g = Mathf.Clamp01(mixed.g * (1f + linePreviewValueBoost));
            mixed.b = Mathf.Clamp01(mixed.b * (1f + linePreviewValueBoost));
            mixed.a = baseColor.a;

            _cells[x, y].SetColor(mixed);
        }

        private void RestoreCellColor(int x, int y)
        {
            if (_cells == null || _lastBoardState == null) return;
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            if (_cells[x, y] == null) return;

            bool isFilled = _lastBoardState.IsOccupied(x, y);
            if (isFilled)
            {
                var cellState = _lastBoardState.GetCell(x, y);
                Color restored = GetBlockColor(cellState.ColorId);
                _cells[x, y].SetColor(restored);
                Sprite filledSprite = spriteConfig?.GetBlockSpriteByColorId(cellState.ColorId);
                if (filledSprite != null) _cells[x, y].SetSprite(filledSprite);
            }
            else
            {
                Sprite emptySprite = spriteConfig?.EmptyCellSprite;
                if (emptySprite != null)
                {
                    _cells[x, y].SetSprite(emptySprite);
                    _cells[x, y].SetColor(Color.white);
                }
                else
                {
                    _cells[x, y].SetColor(emptyCellColor);
                }
            }
        }

        private Color GetBaseCellColor(int x, int y, out bool isFilled)
        {
            isFilled = _lastBoardState.IsOccupied(x, y);
            if (isFilled)
            {
                var cellState = _lastBoardState.GetCell(x, y);
                return GetBlockColor(cellState.ColorId);
            }

            return spriteConfig?.EmptyCellSprite != null ? Color.white : emptyCellColor;
        }

        private void SetCellGlow(int x, int y, bool enabled)
        {
            if (_linePreviewGlow == null) return;
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            var glow = _linePreviewGlow[x, y];
            if (glow == null) return;

            if (!enabled || !enableLinePreviewOuterGlow)
            {
                glow.enabled = false;
                return;
            }

            var baseRenderer = _cells != null ? _cells[x, y]?.GetComponent<SpriteRenderer>() : null;
            if (baseRenderer != null && baseRenderer.sprite != null)
                glow.sprite = baseRenderer.sprite;

            bool isFilled = _lastBoardState != null && _lastBoardState.IsOccupied(x, y);
            Color tint = isFilled ? linePreviewTintFilled : linePreviewTintEmpty;
            Color glowColor = Color.Lerp(linePreviewGlowColor, tint, 0.35f);
            glowColor.a = linePreviewGlowColor.a;

            glow.color = glowColor;
            glow.transform.localScale = Vector3.one * linePreviewGlowScale;
            glow.enabled = true;
        }
    }
}
