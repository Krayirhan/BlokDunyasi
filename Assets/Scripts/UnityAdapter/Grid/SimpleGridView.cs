using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Components;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Grid
{
    /// <summary>
    /// Facade/owner for grid rendering and world/grid coordinate bridge.
    /// Preview, line-clear FX and backdrop visuals are delegated to helper renderers.
    ///
    /// Important:
    /// - Board/grid convention is TOP-LEFT origin.
    /// - (0,0) is top-left.
    /// - X increases right.
    /// - Y increases downward.
    /// - Drag placement is handled by the input system; this view renders only the board.
    /// </summary>
    [ExecuteAlways]
    public class SimpleGridView : MonoBehaviour
    {
        [Header("CONFIGURATION")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        [Header("THEME")]
        [SerializeField] private ThemeConfig themeConfig;

        [Header("VISUAL SYNC")]
        [SerializeField] private bool mirrorTrayBlockVisuals = true;
        [SerializeField] private NewBlockTray sourceTray;

        [Header("SPRITE MODE")]
        [SerializeField] private bool preferAssignedBlockSprites = true;

        [Header("BLOCK COLOR PALETTE (Match SimpleBlock)")]
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
        [SerializeField] private bool useFlatEmptyCellFill = true;
        [SerializeField] private Color emptyCellColor = new Color(0.12f, 0.15f, 0.25f, 1f);
        [SerializeField] [Range(0.5f, 1.2f)] private float emptyCellScale = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float emptyCellAlpha = 1.0f;
        [SerializeField] private bool useRoundedEmptyCellSprite = true;
        [SerializeField] [Range(0f, 0.5f)] private float emptyCellCornerRadius = 0.22f;
        [SerializeField] [Range(0.5f, 1.2f)] private float filledCellScale = 1.0f;
        [SerializeField] private bool useFlatFilledCellTint = false;
        [SerializeField] private bool useFilledCellTintOverride = false;
        [SerializeField] private Color filledCellTintOverride = Color.white;
        [SerializeField] private bool useRoundedFilledCellSprite = false;
        [SerializeField] [Range(0f, 0.5f)] private float filledCellCornerRadius = 0.18f;
        [SerializeField] private bool showCellBorders = true;
        [SerializeField] private Color emptyCellBorderColor = new Color(0.3f, 0.38f, 0.55f, 0.65f);
        [SerializeField] private Color filledCellBorderColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] [Range(0f, 0.2f)] private float cellBorderThicknessInCells = 0.06f;

        [Header("Grid Layout")]
        [SerializeField] private Vector2 gridOffset = Vector2.zero;
        [SerializeField] [Range(0.2f, 2.0f)] private float cellSize = 0.95f;
        [SerializeField] [Range(0f, 0.5f)] private float cellSpacing = 0.05f;

        [Header("Backdrop")]
        [SerializeField] private bool showBoardBackdrop = true;
        [SerializeField] private Sprite customBackdropSprite;
        [SerializeField] private bool useRoundedBackdropSprite = true;
        [SerializeField] [Range(0f, 0.5f)] private float boardBackdropCornerRadius = 0.08f;
        [SerializeField] [Range(0f, 0.5f)] private float boardBackdropBorderCornerRadius = 0.1f;
        [SerializeField] private Color boardBackdropColor = new Color(0.06f, 0.1f, 0.2f, 0.55f);
        [SerializeField] [Range(-2f, 5f)] private float boardBackdropPaddingInCells = 0.35f;
        [SerializeField] private Vector2 backdropOffset = Vector2.zero;
        [SerializeField] private bool showBoardBackdropBorder = true;
        [SerializeField] private Color boardBackdropBorderColor = new Color(0.75f, 0.9f, 1f, 0.12f);
        [SerializeField] [Range(0f, 1f)] private float boardBackdropBorderThicknessInCells = 0.12f;
        [SerializeField] [Range(-20, 20)] private int boardBackdropSortingOrder = -2;

        [Header("Placement Preview")]
        [SerializeField] [Range(0.1f, 1f)] private float dropPreviewAlpha = 0.48f;
        [SerializeField] [Range(0.75f, 1.15f)] private float dropPreviewScale = 0.92f;
        [SerializeField] [Range(0.8f, 1.5f)] private float dropPreviewBrightness = 1.08f;
        [SerializeField] [Range(0f, 0.5f)] private float dropPreviewCornerRadius = 0.18f;
        [SerializeField] private int dropPreviewSortingOrder = 30;

        [Header("LINE CLEAR PREVIEW")]
        [SerializeField] private bool enableLineClearPreviewPulse = true;
        [SerializeField] private Color lineClearPreviewColor = new Color(0.75f, 0.95f, 1f, 1f);
        [SerializeField] [Range(0.05f, 0.8f)] private float lineClearPreviewMinAlpha = 0.16f;
        [SerializeField] [Range(0.05f, 0.9f)] private float lineClearPreviewMaxAlpha = 0.34f;
        [SerializeField] [Range(0.95f, 1.2f)] private float lineClearPreviewBaseScale = 1.07f;
        [SerializeField] [Range(0f, 0.12f)] private float lineClearPreviewPulseScale = 0.03f;
        [SerializeField] [Range(0.25f, 4f)] private float lineClearPreviewPulseSpeed = 2.2f;
        [SerializeField] private int lineClearPreviewSortingOrder = 4;

        private SimpleCell[,] _cells;
        private readonly GridBackdropView _backdropView = new GridBackdropView();
        private readonly GridPreviewRenderer _previewRenderer = new GridPreviewRenderer();
        private readonly GridLineClearFx _lineClearFx = new GridLineClearFx();
        private int _width;
        private int _height;
        private bool _isInitialized;
        private BoardState _lastBoardState;
        private Sprite _cachedDefaultSquareSprite;
        private readonly Dictionary<int, Sprite> _roundedSpriteCache = new Dictionary<int, Sprite>();
        private int _lastVisualSettingsVersion = -1;
        private bool _loggedSourceTrayFallbackWarning;
        private float _nextRuntimeReferenceResolveTime;
        private const float RuntimeReferenceResolveIntervalSeconds = 0.5f;
        private static readonly Color InvalidPlacementPreviewColor = new Color(1f, 0.25f, 0.55f, 1f);
        private const float ValidPlacementPreviewAlpha = 0.62f;
        private const float InvalidPlacementPreviewAlpha = 0.35f;
        private const float PlacementPreviewScale = 0.96f;
        private const int PlacementPreviewSortingOrder = 30;

        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;
        public float TotalCellSize => CellSize + CellSpacing;
        public Vector2 GridOffset => gridOffset;
        public int Width => _width;
        public int Height => _height;
        public BlockSpriteConfig SpriteConfig => spriteConfig;
        public Color EmptyCellColor => emptyCellColor;
        public bool ShowBoardBackdrop => showBoardBackdrop;
        public Color BoardBackdropColor => boardBackdropColor;
        public float BoardBackdropPaddingInCells => boardBackdropPaddingInCells;
        public bool ShowBoardBackdropBorder => showBoardBackdropBorder;
        public Color BoardBackdropBorderColor => boardBackdropBorderColor;
        public float BoardBackdropBorderThicknessInCells => boardBackdropBorderThicknessInCells;
        public int BoardBackdropSortingOrder => boardBackdropSortingOrder;
        public float FilledCellScale => filledCellScale;

        public void ApplyThemeSpriteConfig(BlockSpriteConfig config)
        {
            spriteConfig = config;

            if (_isInitialized)
                RefreshInspectorDrivenVisuals();
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            Vector3 origin = transform.position + (Vector3)GridOffset;
            return CoordinateMapper.GridToWorldPosition(x, y, _width, _height, TotalCellSize, origin);
        }

        public Transform GetCellTransform(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height || _cells == null || _cells[x, y] == null)
                return null;

            return _cells[x, y].transform;
        }

        public void ForceCellEmptyVisual(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height || _cells == null || _cells[x, y] == null)
                return;

            SetCellEmptyVisual(x, y, _cells[x, y].transform);
        }

        public bool GetGridPosition(Vector3 worldPos, out int x, out int y)
        {
            Vector3 origin = transform.position + (Vector3)GridOffset;
            return CoordinateMapper.WorldToGridPosition(worldPos, _width, _height, TotalCellSize, origin, out x, out y);
        }

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                GameBootstrap.OnBoardChanged += OnBoardChanged;
                _lastVisualSettingsVersion = ProjectColorGrading.SettingsVersion;
            }
            else
                ClearEditorGeneratedGrid();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                GameBootstrap.OnBoardChanged -= OnBoardChanged;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
                GameBootstrap.OnBoardChanged -= OnBoardChanged;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            TryResolveRuntimeReferences();

            if (_lineClearFx.HasActiveLineClearPreview)
                UpdateLineClearPreviewAnimation();

            if (_lastVisualSettingsVersion == ProjectColorGrading.SettingsVersion)
                return;

            _lastVisualSettingsVersion = ProjectColorGrading.SettingsVersion;

            if (_isInitialized)
                RefreshInspectorDrivenVisuals();
        }

        private void OnBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            ResolveRuntimeReferences();

            if (boardState == null)
                return;

            _lastBoardState = boardState;

            if (!_isInitialized || _width != boardState.Width || _height != boardState.Height)
                InitializeGrid(boardState);

            ClearDropPreview();
            UpdateGrid(boardState);

            if (Application.isPlaying && clearedPositions != null && clearedPositions.Length > 0)
                StartCoroutine(_lineClearFx.PlayLineClear(transform, clearedPositions, CellSize, GetCellTransform, GetEffectSprite, GetSpriteScale));
        }

        private void InitializeGrid(BoardState boardState)
        {
            ResetDelegatedVisuals();
            _width = boardState.Width;
            _height = boardState.Height;
            _cells = new SimpleCell[_width, _height];

            DestroyGridChildren(Application.isPlaying);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                    CreateCellAt(x, y);
            }

            EnsureBoardBackdrop();
            _isInitialized = true;
        }

        private void DestroyGridChildren(bool isPlaying)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private void CreateCellAt(int x, int y)
        {
            var cellObj = new GameObject($"Cell_{x}_{y}");
            cellObj.transform.SetParent(transform, false);
            cellObj.transform.position = GetWorldPosition(x, y);

            var spriteRenderer = cellObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1;

            var cell = cellObj.AddComponent<SimpleCell>();
            cell.SetSortingOrder(1);

            Sprite emptySprite = GetResolvedEmptyCellSprite();
            if (emptySprite != null)
            {
                cell.SetSprite(emptySprite);
                cell.SetColor(ResolveEmptyCellColor());
                float scale = GetSpriteScale(emptySprite, CellSize * emptyCellScale);
                cellObj.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                Sprite defaultSprite = GetDefaultSquareSprite();
                cell.SetSprite(defaultSprite);
                cell.SetColor(ResolveEmptyCellColor());
                float scale = GetSpriteScale(defaultSprite, CellSize * emptyCellScale);
                cellObj.transform.localScale = new Vector3(scale, scale, 1f);
            }

            cell.SetBorderStyle(showCellBorders, emptyCellBorderColor, cellBorderThicknessInCells);

            _cells[x, y] = cell;
        }

        private Sprite GetDefaultSquareSprite()
        {
            if (_cachedDefaultSquareSprite == null)
                _cachedDefaultSquareSprite = CreateDefaultSquareSprite();

            return _cachedDefaultSquareSprite;
        }

        private Sprite CreateDefaultSquareSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;

            texture.SetPixels(colors);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private Sprite GetRoundedSquareSprite(float normalizedRadius)
        {
            normalizedRadius = Mathf.Clamp01(normalizedRadius);
            int cacheKey = Mathf.RoundToInt(normalizedRadius * 1000f);
            if (_roundedSpriteCache.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
                return cachedSprite;

            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float radius = normalizedRadius * size * 0.5f;
            float innerMin = radius;
            float innerMax = size - radius - 1f;
            Color[] pixels = new Color[size * size];
            const float edgeSoftness = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float clampedX = Mathf.Clamp(x, innerMin, innerMax);
                    float clampedY = Mathf.Clamp(y, innerMin, innerMax);
                    float dx = x - clampedX;
                    float dy = y - clampedY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((distance - radius + edgeSoftness) / edgeSoftness);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _roundedSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private Sprite GetEmptyCellSprite()
        {
            if (themeConfig != null && themeConfig.CustomCellSprite != null)
                return themeConfig.CustomCellSprite;

            if (spriteConfig != null && spriteConfig.EmptyCellSprite != null)
                return spriteConfig.EmptyCellSprite;

            return null;
        }

        private Sprite GetResolvedEmptyCellSprite()
        {
            if (useRoundedEmptyCellSprite)
                return GetRoundedSquareSprite(emptyCellCornerRadius);

            if (useFlatEmptyCellFill)
                return GetDefaultSquareSprite();

            return GetEmptyCellSprite() ?? GetDefaultSquareSprite();
        }

        private Sprite GetEffectSprite()
        {
            if (spriteConfig != null && spriteConfig.EmptyCellSprite != null)
                return spriteConfig.EmptyCellSprite;

            if (themeConfig != null && themeConfig.CustomCellSprite != null)
                return themeConfig.CustomCellSprite;

            return GetDefaultSquareSprite();
        }

        private static float GetSpriteScale(Sprite sprite, float targetWorldSize)
        {
            float spriteWorldSize = sprite != null && sprite.bounds.size.x > 0f ? sprite.bounds.size.x : 1f;
            return targetWorldSize / spriteWorldSize;
        }

        private void EnsureBoardBackdrop()
        {
            _backdropView.EnsureBoardBackdrop(
                transform,
                _width,
                _height,
                TotalCellSize,
                CellSize,
                ShowBoardBackdrop,
                ShowBoardBackdropBorder,
                BoardBackdropPaddingInCells,
                BoardBackdropBorderThicknessInCells,
                BoardBackdropSortingOrder,
                backdropOffset,
                ApplyUiColor(BoardBackdropColor),
                ApplyUiColor(BoardBackdropBorderColor),
                GetBackdropSprite(),
                GetBackdropBorderSprite());
        }

        private Sprite GetBackdropSprite()
        {
            if (useRoundedBackdropSprite)
                return GetRoundedSquareSprite(boardBackdropCornerRadius);

            if (customBackdropSprite != null)
                return customBackdropSprite;

            if (themeConfig != null && themeConfig.CustomBackdropSprite != null)
                return themeConfig.CustomBackdropSprite;

            return GetDefaultSquareSprite();
        }

        private Sprite GetBackdropBorderSprite()
        {
            if (useRoundedBackdropSprite)
                return GetRoundedSquareSprite(boardBackdropBorderCornerRadius);

            return GetBackdropSprite();
        }

        private void UpdateGrid(BoardState boardState)
        {
            if (boardState == null || _cells == null)
                return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_cells[x, y] == null)
                        continue;

                    Transform cellTransform = _cells[x, y].transform;
                    cellTransform.position = GetWorldPosition(x, y);

                    if (boardState.IsOccupied(x, y))
                    {
                        var cellState = boardState.GetCell(x, y);
                        SetCellFilledVisual(x, y, cellState.ColorId, cellTransform);
                    }
                    else
                    {
                        SetCellEmptyVisual(x, y, cellTransform);
                    }
                }
            }
        }

        private void SetCellFilledVisual(int x, int y, int colorId, Transform cellTransform)
        {
            Color cellColor = ResolveFilledCellColor(colorId);
            Sprite filledSprite = GetFilledCellSprite(colorId);

            if (filledSprite != null)
            {
                _cells[x, y].SetSprite(filledSprite);
                _cells[x, y].SetColor(cellColor);
                float scale = GetSpriteScale(filledSprite, CellSize * filledCellScale);
                cellTransform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                _cells[x, y].SetSprite(GetDefaultSquareSprite());
                _cells[x, y].SetColor(cellColor);
                float scale = GetSpriteScale(GetDefaultSquareSprite(), CellSize * filledCellScale);
                cellTransform.localScale = new Vector3(scale, scale, 1f);
            }

            _cells[x, y].SetBorderStyle(false, filledCellBorderColor, cellBorderThicknessInCells);
        }

        private void SetCellEmptyVisual(int x, int y, Transform cellTransform)
        {
            Sprite emptySprite = GetResolvedEmptyCellSprite();
            Color resolvedEmptyColor = ResolveEmptyCellColor();
            if (emptySprite != null)
            {
                _cells[x, y].SetSprite(emptySprite);
                _cells[x, y].SetColor(resolvedEmptyColor);
                float scale = GetSpriteScale(emptySprite, CellSize * emptyCellScale);
                cellTransform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                Sprite defaultSprite = GetDefaultSquareSprite();
                _cells[x, y].SetSprite(defaultSprite);
                _cells[x, y].SetColor(resolvedEmptyColor);
                float scale = GetSpriteScale(defaultSprite, CellSize * emptyCellScale);
                cellTransform.localScale = new Vector3(scale, scale, 1f);
            }

            _cells[x, y].SetBorderStyle(showCellBorders, emptyCellBorderColor, cellBorderThicknessInCells);
        }

        [ContextMenu("Sync With Theme")]
        public void SyncWithTheme()
        {
            if (themeConfig == null)
                return;

            gridOffset = themeConfig.GridOffset;
            cellSize = themeConfig.CellSize;
            cellSpacing = themeConfig.CellSpacing;
            showBoardBackdrop = themeConfig.ShowBoardBackdrop;
            customBackdropSprite = themeConfig.CustomBackdropSprite;
            boardBackdropColor = themeConfig.BoardBackdropColor;
            boardBackdropPaddingInCells = themeConfig.BoardBackdropPaddingInCells;
            backdropOffset = themeConfig.BackdropOffset;
            showBoardBackdropBorder = themeConfig.ShowBoardBackdropBorder;
            boardBackdropBorderColor = themeConfig.ShowBoardBackdropBorder ? themeConfig.BoardBackdropBorderColor : boardBackdropBorderColor;
            boardBackdropBorderThicknessInCells = themeConfig.BoardBackdropBorderThicknessInCells;
            boardBackdropSortingOrder = themeConfig.BoardBackdropSortingOrder;

            if (Application.isPlaying && _lastBoardState != null)
                OnBoardChanged(_lastBoardState, null, 0);
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(0.05f, cellSize);
            cellSpacing = Mathf.Max(0f, cellSpacing);
            emptyCellCornerRadius = Mathf.Clamp01(emptyCellCornerRadius);
            filledCellCornerRadius = Mathf.Clamp01(filledCellCornerRadius);
            boardBackdropCornerRadius = Mathf.Clamp01(boardBackdropCornerRadius);
            boardBackdropBorderCornerRadius = Mathf.Clamp01(boardBackdropBorderCornerRadius);
            dropPreviewCornerRadius = Mathf.Clamp01(dropPreviewCornerRadius);
            boardBackdropPaddingInCells = Mathf.Clamp(boardBackdropPaddingInCells, -2f, 5f);
            boardBackdropBorderThicknessInCells = Mathf.Clamp01(boardBackdropBorderThicknessInCells);
            cellBorderThicknessInCells = Mathf.Clamp(cellBorderThicknessInCells, 0f, 0.2f);
            emptyCellScale = Mathf.Clamp(emptyCellScale, 0.5f, 1.2f);
            emptyCellAlpha = Mathf.Clamp01(emptyCellAlpha);
            filledCellScale = Mathf.Clamp(filledCellScale, 0.5f, 1.2f);
            placedBlockBrightness = Mathf.Clamp(placedBlockBrightness, 0.5f, 2.0f);
            placedBlockAlpha = Mathf.Clamp01(placedBlockAlpha);
            dropPreviewAlpha = Mathf.Clamp01(dropPreviewAlpha);
            dropPreviewScale = Mathf.Clamp(dropPreviewScale, 0.75f, 1.15f);
            dropPreviewBrightness = Mathf.Clamp(dropPreviewBrightness, 0.8f, 1.5f);
            _roundedSpriteCache.Clear();

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (mirrorTrayBlockVisuals && sourceTray == null)
                    sourceTray = TryAutoAssignSingleton<NewBlockTray>();
#endif
                ClearEditorGeneratedGrid();
                return;
            }

            if (_isInitialized)
                RefreshInspectorDrivenVisuals();
        }

        private void ClearEditorGeneratedGrid()
        {
            if (Application.isPlaying)
                return;

            if (transform.childCount > 0)
                DestroyGridChildren(false);

            _cells = null;
            ResetDelegatedVisuals();
            _isInitialized = false;
            _lastBoardState = null;
            _width = 0;
            _height = 0;
        }

        private void RefreshInspectorDrivenVisuals()
        {
            if (_cells != null)
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_cells[x, y] == null)
                            continue;

                        Transform cellTransform = _cells[x, y].transform;
                        cellTransform.position = GetWorldPosition(x, y);

                        var renderer = _cells[x, y].GetComponent<SpriteRenderer>();
                        Sprite sprite = renderer != null ? renderer.sprite : null;
                        if (sprite != null)
                        {
                            float scale = GetSpriteScale(sprite, CellSize);
                            cellTransform.localScale = new Vector3(scale, scale, 1f);
                        }
                    }
                }
            }

                if (_lastBoardState != null)
                    UpdateGrid(_lastBoardState);
            else if (_cells != null)
            {
                Sprite emptySprite = GetResolvedEmptyCellSprite();
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_cells[x, y] == null)
                            continue;

                        _cells[x, y].SetSprite(emptySprite);
                        _cells[x, y].SetColor(ResolveEmptyCellColor());
                        float scale = GetSpriteScale(emptySprite, CellSize * emptyCellScale);
                        _cells[x, y].transform.localScale = new Vector3(scale, scale, 1f);
                        _cells[x, y].SetBorderStyle(showCellBorders, emptyCellBorderColor, cellBorderThicknessInCells);
                    }
                }
            }

            EnsureBoardBackdrop();
            ClearDropPreview();
        }

        public Color GetBlockColor(int colorId)
        {
            return ApplyBlockLighting(GetPaletteColor(colorId));
        }

        private Color ApplyBlockLighting(Color baseColor)
        {
            Color result = baseColor;
            result.a = placedBlockAlpha;
            return result;
        }

        private Color ResolveFilledCellColor(int colorId)
        {
            if (HasAssignedBlockSprite(colorId))
            {
                if (useFilledCellTintOverride)
                {
                    Color overrideColor = filledCellTintOverride;
                    overrideColor.a = placedBlockAlpha;
                    return overrideColor;
                }

                return new Color(1f, 1f, 1f, placedBlockAlpha);
            }

            Color color = GetBlockColor(colorId);
            color.a = placedBlockAlpha;
            return color;
        }

        private Color ResolveEmptyCellColor()
        {
            Color color = EmptyCellColor;
            color.a *= emptyCellAlpha;
            return color;
        }

        public void EnsureBoardBackdropVisible()
        {
            if (!_isInitialized || _width <= 0 || _height <= 0)
                return;

            EnsureBoardBackdrop();
        }

        public void ApplyResponsiveLayout(float newCellSize, float newCellSpacing, Vector3 centerPosition)
        {
            cellSize = Mathf.Max(0.001f, newCellSize);
            cellSpacing = Mathf.Max(0f, newCellSpacing);
            transform.position = centerPosition;

            if (_isInitialized)
            {
                RefreshInspectorDrivenVisuals();
            }
            else if (_cells != null)
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (_cells[x, y] != null)
                            _cells[x, y].transform.position = GetWorldPosition(x, y);
                    }
                }

                EnsureBoardBackdrop();

                if (_lastBoardState != null)
                    UpdateGrid(_lastBoardState);
            }

            ClearPlacementPreview();
            ClearLineHighlights();
        }

        public void ShowDropPreview(Int2 anchor, IReadOnlyList<Int2> offsets, Color blockColor, int colorId = -1)
        {
            RenderPlacementGhost(anchor, offsets, blockColor, colorId, dropPreviewAlpha, dropPreviewScale, dropPreviewSortingOrder);
        }

        public void ClearDropPreview()
        {
            _previewRenderer.ClearPlacementPreview();
        }

        private Sprite GetDropPreviewSprite(int colorId)
        {
            // Theme block sprites can be large decorative panels (for example
            // the fabric/heart art). Placement ghosts must use the dedicated
            // cell-sized preview sprite instead of rendering that artwork.
            if (spriteConfig != null)
            {
                Sprite previewSprite = spriteConfig.GetPreviewSprite(true);
                if (previewSprite != null)
                    return previewSprite;
            }

            return GetDefaultSquareSprite();
        }

        private Color GetPaletteColor(int colorId)
        {
            Color[] palette = mirrorTrayBlockVisuals && sourceTray != null
                ? sourceTray.GetPreferredPalette()
                : UISettingsProfile.GetPreferredBlockPalette(blockColors);

            if (palette == null || palette.Length == 0)
                return new Color(1f, 0.4f, 0.8f);

            int colorIndex = (colorId - 1) % palette.Length;
            if (colorIndex < 0)
                colorIndex += palette.Length;

            return palette[colorIndex];
        }

        private static Color ApplyUiColor(Color color) => color;

        private Sprite GetFilledCellSprite(int colorId)
        {
            if (preferAssignedBlockSprites && spriteConfig != null)
            {
                Sprite assignedSprite = spriteConfig.GetBlockSpriteByColorId(colorId);
                if (assignedSprite != null)
                    return assignedSprite;
            }

            if (useFlatFilledCellTint)
            {
                if (useRoundedFilledCellSprite)
                    return GetRoundedSquareSprite(filledCellCornerRadius);

                return GetDefaultSquareSprite();
            }

            if (useRoundedFilledCellSprite)
                return GetRoundedSquareSprite(filledCellCornerRadius);

            if (spriteConfig != null)
            {
                Sprite coloredSprite = spriteConfig.GetBlockSpriteByColorId(colorId);
                if (coloredSprite != null)
                    return coloredSprite;

                if (spriteConfig.FilledCellSprite != null)
                    return spriteConfig.FilledCellSprite;
            }

            return GetDefaultSquareSprite();
        }

        private bool HasAssignedBlockSprite(int colorId)
        {
            return preferAssignedBlockSprites &&
                   spriteConfig != null &&
                   spriteConfig.GetBlockSpriteByColorId(colorId) != null;
        }

        private void ResolveRuntimeReferences()
        {
            if (!mirrorTrayBlockVisuals)
                return;

            if (sourceTray == null)
            {
                sourceTray = FindFirstObjectByType<NewBlockTray>();
                if (sourceTray != null && !_loggedSourceTrayFallbackWarning)
                {
                    _loggedSourceTrayFallbackWarning = true;
                    GameLogger.LogWarning("[SimpleGridView] sourceTray was resolved via runtime lookup. Inspector wiring is the preferred production path when mirrorTrayBlockVisuals is enabled.");
                }
            }

            if (sourceTray == null)
                return;

            placedBlockAlpha = sourceTray.NormalAlpha;
        }

        private void TryResolveRuntimeReferences()
        {
            if (!mirrorTrayBlockVisuals || sourceTray != null)
                return;

            if (Time.unscaledTime < _nextRuntimeReferenceResolveTime)
                return;

            _nextRuntimeReferenceResolveTime = Time.unscaledTime + RuntimeReferenceResolveIntervalSeconds;
            ResolveRuntimeReferences();
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        public void SetPlacementPreview(Int2 anchor, IReadOnlyList<Int2> offsets, bool canPlace, Color blockColor, int colorId = -1)
        {
            Color previewColor = canPlace ? blockColor : InvalidPlacementPreviewColor;
            float alpha = canPlace ? ValidPlacementPreviewAlpha : InvalidPlacementPreviewAlpha;
            RenderPlacementGhost(anchor, offsets, previewColor, colorId, alpha, PlacementPreviewScale, PlacementPreviewSortingOrder);
        }

        public void ClearPlacementPreview()
        {
            ClearDropPreview();
        }

        public void HighlightLines(List<int> rows, List<int> cols)
        {
            _lineClearFx.HighlightLines(
                transform,
                _width,
                _height,
                HasRenderableCell,
                GetWorldPosition,
                GetLineClearPreviewSprite,
                GetSpriteScale,
                CellSize,
                lineClearPreviewBaseScale,
                lineClearPreviewMinAlpha,
                lineClearPreviewColor,
                lineClearPreviewSortingOrder,
                rows,
                cols);
        }

        public void ClearLineHighlights()
        {
            _lineClearFx.ClearLineHighlights();
        }

        private void UpdateLineClearPreviewAnimation()
        {
            _lineClearFx.UpdateLineClearPreviewAnimation(
                Time.deltaTime,
                enableLineClearPreviewPulse,
                lineClearPreviewPulseSpeed,
                lineClearPreviewMinAlpha,
                lineClearPreviewMaxAlpha,
                lineClearPreviewBaseScale,
                lineClearPreviewPulseScale,
                lineClearPreviewColor,
                CellSize,
                GetSpriteScale);
        }

        private Sprite GetLineClearPreviewSprite()
        {
            if (useRoundedFilledCellSprite)
                return GetRoundedSquareSprite(filledCellCornerRadius);

            return GetDefaultSquareSprite();
        }

        private void RenderPlacementGhost(
            Int2 anchor,
            IReadOnlyList<Int2> offsets,
            Color previewColor,
            int colorId,
            float alpha,
            float scaleMultiplier,
            int sortingOrder)
        {
            Sprite previewSprite = offsets == null ? null : GetDropPreviewSprite(colorId);
            previewColor.a = alpha;
            _previewRenderer.RenderPlacementGhost(
                transform,
                _width,
                _height,
                anchor,
                offsets,
                previewColor,
                previewSprite,
                sortingOrder,
                CellSize * scaleMultiplier,
                GetWorldPosition,
                GetSpriteScale);
        }

        private bool HasRenderableCell(int x, int y)
        {
            return _cells != null &&
                   x >= 0 && x < _width &&
                   y >= 0 && y < _height &&
                   _cells[x, y] != null;
        }

        private void ResetDelegatedVisuals()
        {
            _backdropView.Reset();
            _previewRenderer.Reset();
            _lineClearFx.Reset();
        }
    }
}
