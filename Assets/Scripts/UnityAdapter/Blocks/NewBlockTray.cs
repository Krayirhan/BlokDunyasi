using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.UI;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Blocks
{
    /// <summary>
    /// YENİ TEMİZ BLOCK TRAY SİSTEMİ
    /// NewSimpleBlock ile çalışır
    /// </summary>
    public class NewBlockTray : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        [Header("References")]
        [SerializeField] private GameBootstrap _gameBootstrap;
        [SerializeField] private SimpleGridView gridView;
        [SerializeField] private Camera trayLayoutCamera;

        [Header("Board Metric Fallbacks")]
        [Tooltip("Used only when gridView is unavailable.")]
        [SerializeField] private float blockCellSize = 0.5f;
        [Tooltip("Used only when gridView is unavailable.")]
        [SerializeField] private float blockCellSpacing = 0f;

        [Header("Tray Metrics")]
        [Tooltip("Tray'deki bloklar arasındaki mesafe. Board'dan bağımsızdır.")]
        [SerializeField] private float trayCellSpacing = 0.05f;

        [Header("Block Visuals")]
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
        [SerializeField] [Range(0.5f, 2.0f)] private float trayBlockBrightness = 1.0f;
        [SerializeField] [Range(1.0f, 2.0f)] private float dragBrightnessMultiplier = 1.2f;
        [SerializeField] [Range(0f, 1f)] private float normalAlpha = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float dragAlpha = 0.9f;

        [Header("Responsive Scale")]
        [Tooltip("Tray'deki blokların sabit önizleme boyutu. 0.85 = grid hücresinin %85'i")]
        [SerializeField] [Range(0.5f, 0.9f)] private float trayBlockScale = 0.85f;

        [Header("Slot Debug Fallback")]
        [Tooltip("Runtime responsive layout overwrites these positions.")]
        [SerializeField] private Vector3[] slotPositions = new Vector3[]
        {
            new Vector3(-2.5f, -4f, 0),
            new Vector3(0f, -4f, 0),
            new Vector3(2.5f, -4f, 0)
        };

        [Header("Layout")]
        [SerializeField] private float slotGap = 0.4f;
        [SerializeField] private float trayHorizontalPadding = 0.4f;
        [SerializeField] private float trayVerticalPadding = 0.3f;
        [SerializeField] private float trayGapFromGrid = 0.4f;
        [SerializeField] private float trayVerticalOffset = 0f;
        [SerializeField] [Range(0.2f, 1f)] private float minTrayScale = 0.35f;

        [Header("Tray Backdrop")]
        [SerializeField] private bool showTrayBackdrop = false;
        [SerializeField] private bool showTrayBackdropBorder = true;
        [SerializeField] private Color trayBackdropColor = new Color(0.11f, 0.2f, 0.44f, 0.9f);
        [SerializeField] private Color trayBackdropBorderColor = new Color(0.56f, 0.72f, 1f, 0.24f);
        [SerializeField] [Range(0.1f, 2f)] private float trayBackdropHorizontalPadding = 0.7f;
        [SerializeField] [Range(0.1f, 2f)] private float trayBackdropVerticalPadding = 0.55f;
        [SerializeField] private Vector2 trayBackdropSizeAdjustInWorld = Vector2.zero;
        [SerializeField] private Vector2 trayBackdropOffset = Vector2.zero;
        [SerializeField] [Range(0f, 0.5f)] private float trayBackdropCornerRadius = 0.14f;
        [SerializeField] [Range(0f, 0.4f)] private float trayBackdropBorderThicknessInWorld = 0.08f;
        [SerializeField] private int trayBackdropSortingOrder = 4;
        [SerializeField] private int trayBackdropBorderSortingOrder = 5;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        [HideInInspector] [SerializeField] private bool driveManualUiBackdrop = false;
        [HideInInspector] [SerializeField] private RectTransform manualUiBackdrop;
        [HideInInspector] [SerializeField] private Canvas manualUiBackdropCanvas;
        [HideInInspector] [SerializeField] private Image manualUiBackdropImage;
        [HideInInspector] [SerializeField] private bool autoFitManualUiBackdrop = true;
        [HideInInspector] [SerializeField] private bool autoAnchorManualUiBackdrop = true;
        [HideInInspector] [SerializeField] private bool useSlotEnvelopeForAutoBackdrop = true;
        [HideInInspector] [SerializeField] private bool useGeneratedUiBackdropSprite = true;
        [HideInInspector] [SerializeField] private bool useGeneratedUiBackdropBorder = true;
        [HideInInspector] [SerializeField] private Image manualUiBackdropBorderImage;
        [HideInInspector] [SerializeField] private Color manualUiBackdropColor = new Color(0.11f, 0.2f, 0.44f, 0.9f);
        [HideInInspector] [SerializeField] private Color manualUiBackdropBorderColor = new Color(0.56f, 0.72f, 1f, 0.24f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropPadding = new Vector2(90f, 64f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropOffset = Vector2.zero;
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropBorderPadding = new Vector2(10f, 10f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropSizeOverride = new Vector2(860f, 330f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropBorderSizeOverride = new Vector2(880f, 350f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropPositionOverride = new Vector2(0f, -640f);
        [HideInInspector] [SerializeField] private Vector2 manualUiBackdropBorderOffset = Vector2.zero;
        [HideInInspector] [SerializeField] [Range(0f, 0.5f)] private float manualUiBackdropCornerRadius = 0.14f;

        private NewSimpleBlock[] _blocks = new NewSimpleBlock[3];
        private readonly Queue<NewSimpleBlock> _blockPool = new Queue<NewSimpleBlock>();
        private ShapeDefinition[] _currentShapes;
        private ShapeDefinition[] _layoutShapes;
        private float _currentFitScale = 1f;
        private bool _hasCalculatedLayout;
        private bool _isResyncing;
        private int _desyncFrameCount;
        private int _lastVisualSettingsVersion = -1;
        private SpriteRenderer _trayBackdropRenderer;
        private SpriteRenderer _trayBackdropBorderRenderer;
        private Sprite _defaultBackdropSprite;
        private readonly Dictionary<int, Sprite> _roundedBackdropSpriteCache = new Dictionary<int, Sprite>();
        private Sprite _generatedUiBackdropSprite;
        private readonly Dictionary<int, Sprite> _generatedUiRoundedBackdropSpriteCache = new Dictionary<int, Sprite>();
        private Camera _lastTrayLayoutCamera;
        private bool _hasResponsiveLayoutOverride;
        private float _responsiveTrayWidth;
        private float _responsiveTrayHeight;
        private Vector3 _responsiveTrayCenter;
        private bool _loggedDependencyWarning;
        private bool _loggedCameraFallbackWarning;
        private string _lastLayoutReason = "uninitialized";
        private bool _lastLayoutUsedFallback;
        private float _nextDesyncCheckTime;
        private const float DesyncCheckIntervalSeconds = 0.25f;

        public float BoardCellSize => gridView != null ? gridView.CellSize : blockCellSize;
        public float BoardCellSpacing => gridView != null ? gridView.CellSpacing : blockCellSpacing;
        public float TrayCellSize => BoardCellSize * trayBlockScale * _currentFitScale;
        public float TrayCellSpacing => BoardCellSpacing * trayBlockScale * _currentFitScale;
        public float TrayBlockBrightness => trayBlockBrightness;
        public float NormalAlpha => normalAlpha;

        private void LogVerbose(string message)
        {
            if (verboseLogs)
                Debug.Log(message);
        }

        public Color[] GetPreferredPalette()
        {
            return UISettingsProfile.GetPreferredBlockPalette(blockColors);
        }

        public void ApplyThemeSpriteConfig(BlockSpriteConfig config)
        {
            spriteConfig = config;

            for (int i = 0; i < _blocks.Length; i++)
            {
                if (_blocks[i] == null)
                    continue;

                _blocks[i].SetSpriteConfig(spriteConfig, true);
                ApplyBlockVisualSettings(_blocks[i]);
            }
        }

        public void InjectGridView(SimpleGridView view)
        {
            if (view != null)
                gridView = view;
        }

        private void Awake()
        {
            CleanupGeneratedTrayChildren(false);
            CleanupBackdropRenderers(false);
            ResolveRuntimeReferences();

            if (Application.isPlaying)
            {
                // Subscribe to event in Awake to ensure we don't miss the first OnBlocksChanged
                // GameBootstrap.Start() may fire before NewBlockTray.Start()!
                GameBootstrap.OnBlocksChanged += OnBlocksChanged;
                GameBootstrap.OnGameContinued += HandleGameContinued;
                _lastVisualSettingsVersion = ProjectColorGrading.SettingsVersion;
                LogVerbose("[NewBlockTray] Subscribed to OnBlocksChanged in Awake");
            }
        }

        private void Start()
        {
            // CRITICAL FIX: Request current blocks in case we missed the initial event
            // This handles race condition where GameBootstrap.Start() fires before NewBlockTray subscribes
            RequestCurrentBlocks(false);
        }

        private void LateUpdate()
        {
            if (_lastVisualSettingsVersion != ProjectColorGrading.SettingsVersion)
            {
                _lastVisualSettingsVersion = ProjectColorGrading.SettingsVersion;
                RefreshBlockVisuals();
            }

            if (_isResyncing)
                return;

            if (Time.unscaledTime < _nextDesyncCheckTime)
                return;

            _nextDesyncCheckTime = Time.unscaledTime + DesyncCheckIntervalSeconds;

            if (!NeedsResyncWithGameState())
            {
                _desyncFrameCount = 0;
                return;
            }

            _desyncFrameCount++;
            if (_desyncFrameCount < 2)
                return;

            ForceResyncFromCurrentState("runtime desync");
            _desyncFrameCount = 0;
        }
        
        /// <summary>
        /// Request current block state from GameBootstrap.
        /// Handles the case where GameBootstrap already spawned blocks before we subscribed.
        /// </summary>
        private void RequestCurrentBlocks(bool force)
        {
            ResolveRuntimeReferences();
            var bootstrap = _gameBootstrap;
            if (bootstrap != null)
            {
                // Check if we already have blocks (event was received)
                bool hasAnyBlock = false;
                for (int i = 0; i < 3; i++)
                {
                    if (_blocks[i] != null)
                    {
                        hasAnyBlock = true;
                        break;
                    }
                }
                
                if (force || !hasAnyBlock)
                {
                    LogVerbose("[NewBlockTray] No blocks yet - requesting current state from GameBootstrap");
                    // Trigger a refresh by getting current shapes from GameBootstrap
                    var currentShapes = bootstrap.GetCurrentShapes();
                    if (currentShapes != null && currentShapes.Length > 0)
                    {
                        LogVerbose($"[NewBlockTray] Got {currentShapes.Length} shapes from GameBootstrap, refreshing...");
                        OnBlocksChanged(currentShapes);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;

            GameBootstrap.OnBlocksChanged -= OnBlocksChanged;
            GameBootstrap.OnGameContinued -= HandleGameContinued;
        }

        private void HandleGameContinued()
        {
            ForceResyncFromCurrentState("game continued");
        }

        public void SyncFromCurrentState()
        {
            // Keep the original three-slot template while the current set is consumed.
            ForceResyncFromCurrentState("direct bootstrap sync", true);
        }

        private void OnBlocksChanged(ShapeDefinition[] shapes)
        {
            // Count non-null shapes for debugging
            int nonNullCount = 0;
            for (int i = 0; i < shapes.Length; i++)
            {
                if (shapes[i] != null) nonNullCount++;
            }
            LogVerbose($"[NewBlockTray.OnBlocksChanged] Received {shapes.Length} slots, {nonNullCount} non-null shapes");
            
            _currentShapes = shapes;
            
            // Keep slot positions stable while player consumes the current 3-block set.
            // Recalculate layout only for first render or when a full new set (3 blocks) arrives.
            if (!_hasCalculatedLayout || nonNullCount == 3)
            {
                if (nonNullCount == 3)
                    _layoutShapes = (ShapeDefinition[])shapes.Clone();

                var layoutSource = _layoutShapes ?? shapes;
                string layoutReason = !_hasCalculatedLayout ? "initial tray layout" : "new full block set";
                ApplyCanonicalLayout(layoutSource, layoutReason);
                _hasCalculatedLayout = true;
            }

            RefreshAllBlocks();
        }

        private void ForceResyncFromCurrentState(string reason, bool preserveCurrentSetLayout = false)
        {
            if (_isResyncing)
                return;

            _isResyncing = true;

            try
            {
                LogVerbose($"[NewBlockTray] Resyncing tray from current state. Reason: {reason}");
                ClearTrayBlocks();
                if (!preserveCurrentSetLayout || _layoutShapes == null)
                {
                    _layoutShapes = null;
                    _hasCalculatedLayout = false;
                }
                RequestCurrentBlocks(true);
            }
            finally
            {
                _isResyncing = false;
            }
        }

        private void ClearTrayBlocks()
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                if (_blocks[i] == null)
                    continue;

                ReleaseBlock(_blocks[i]);
                _blocks[i] = null;
            }

            _currentShapes = null;
        }

        private bool NeedsResyncWithGameState()
        {
            ResolveRuntimeReferences();

            var shapes = _gameBootstrap?.GetCurrentShapes();
            if (shapes == null)
                return false;

            int expectedCount = 0;
            int actualCount = 0;

            for (int i = 0; i < 3; i++)
            {
                bool hasExpected = i < shapes.Length && shapes[i] != null;
                bool hasActual = _blocks[i] != null && !_blocks[i].IsUsed && _blocks[i].BlockShape != null;

                if (hasExpected)
                    expectedCount++;

                if (hasActual)
                    actualCount++;

                if (hasExpected != hasActual)
                    return true;

                if (!hasExpected)
                    continue;

                if (_blocks[i].BlockShape.Id != shapes[i].Id)
                    return true;
            }

            return expectedCount != actualCount;
        }

        private void RefreshAllBlocks()
        {
            int activeCount = 0;

            LogVerbose($"[NewBlockTray.RefreshAllBlocks] Starting refresh. _currentShapes={((_currentShapes != null) ? _currentShapes.Length.ToString() : "NULL")}");

            for (int i = 0; i < 3; i++)
            {
                bool hasShape = _currentShapes != null && i < _currentShapes.Length && _currentShapes[i] != null;
                if (hasShape)
                {
                    CreateBlockAt(i, _currentShapes[i]);
                    activeCount++;
                }
                else
                {
                    if (_blocks[i] != null)
                    {
                        ReleaseBlock(_blocks[i]);
                        _blocks[i] = null;
                    }

                    string reason = _currentShapes == null ? "_currentShapes is NULL" :
                                    i >= _currentShapes.Length ? $"index {i} >= Length {_currentShapes.Length}" :
                                    "_currentShapes[i] is NULL";
                    LogVerbose($"[NewBlockTray.RefreshAllBlocks] Slot {i} is NULL - no block active. Reason: {reason}");
                }
            }

            LogVerbose($"[NewBlockTray.RefreshAllBlocks] Active blocks: {activeCount}, pooled: {_blockPool.Count}");
        }

        private void CreateBlockAt(int slotIndex, ShapeDefinition shape)
        {
            EnsureCanonicalLayoutForSlotCreation(shape, slotIndex);

            if (slotPositions == null || slotIndex >= slotPositions.Length)
            {
                Debug.LogError($"[NewBlockTray.CreateBlockAt] ERROR: slotPositions is null or slotIndex {slotIndex} out of range!");
                return;
            }
            
            Vector3 pos = slotPositions[slotIndex];
            var block = _blocks[slotIndex];
            if (block == null)
            {
                block = AcquireBlock();
                _blocks[slotIndex] = block;
            }

            ApplyBlockVisualSettings(block);

            var blockObj = block.gameObject;
            blockObj.name = $"Block_{slotIndex}_{shape.Name}";
            blockObj.transform.SetParent(transform, false);
            blockObj.transform.position = pos;
            blockObj.transform.localScale = Vector3.one;
            blockObj.SetActive(true);

            // GameBootstrap'tan mevcut colorId'yi al
            int existingColorId = -1;
            if (_gameBootstrap != null &&
                _gameBootstrap.CurrentState != null &&
                _gameBootstrap.CurrentState.ActiveBlocks.TryGetColorId(slotIndex, out int savedColorId))
            {
                existingColorId = savedColorId;
            }
            
            // ColorId'yi geç: varsa mevcut, yoksa -1 (random olacak)
            block.Initialize(shape, spriteConfig, slotIndex, existingColorId);
            block.SetCellMetrics(TrayCellSize, TrayCellSpacing, true);
            block.SetOriginalPosition(pos);
            block.ResetBlock();
            
            // Yeni blok oluştuysa ve colorId atanmışsa, ActiveBlocks'a set et
            if (block.ColorId > 0 && _gameBootstrap != null && _gameBootstrap.CurrentState != null)
            {
                _gameBootstrap.CurrentState.ActiveBlocks.SetColorId(slotIndex, block.ColorId);
            }
            
            // Verify block was created successfully
            LogVerbose($"[NewBlockTray.CreateBlockAt] Block created: name={blockObj.name}, position={blockObj.transform.position}, scale={blockObj.transform.localScale}, activeInHierarchy={blockObj.activeInHierarchy}, _blocks[{slotIndex}]={(block != null ? "SET" : "NULL")}");
        }

        private void ApplyBlockVisualSettings(NewSimpleBlock block)
        {
            if (block == null)
                return;

            Color[] palette = UISettingsProfile.GetPreferredBlockPalette(blockColors);
            block.ApplyVisualSettings(
                palette,
                trayBlockBrightness,
                dragBrightnessMultiplier,
                normalAlpha,
                dragAlpha);
        }

        private void RefreshBlockVisuals()
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                if (_blocks[i] != null)
                    ApplyBlockVisualSettings(_blocks[i]);
            }
        }

        private NewSimpleBlock AcquireBlock()
        {
            if (_blockPool.Count > 0)
            {
                var pooled = _blockPool.Dequeue();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            var blockObj = new GameObject("PooledBlock");
            blockObj.transform.SetParent(transform, false);
            return blockObj.AddComponent<NewSimpleBlock>();
        }

        private void ReleaseBlock(NewSimpleBlock block)
        {
            if (block == null)
                return;

            block.PrepareForPool();
            _blockPool.Enqueue(block);
        }

        /// <summary>
        /// Recomputes tray layout for current screen size/orientation.
        /// Keeps slot template stable during a 3-block set and rebuilds active block visuals.
        /// </summary>
        public void RefreshLayoutForScreenChange()
        {
            var layoutSource = _layoutShapes ?? _currentShapes;
            if (layoutSource == null)
                return;

            ApplyCanonicalLayout(layoutSource, "screen change");
            ApplyCurrentLayoutToActiveBlocks();
        }

        // === PUBLIC API ===

        public NewSimpleBlock GetBlock(int index)
        {
            return (index >= 0 && index < 3) ? _blocks[index] : null;
        }

        public Vector3 GetSlotPosition(int slotIndex)
        {
            if (slotPositions != null && slotIndex >= 0 && slotIndex < slotPositions.Length)
                return slotPositions[slotIndex];

            return transform.position;
        }

        public void ApplyResponsiveLayout(float trayWidth, float trayHeight, Vector3 trayCenter)
        {
            _hasResponsiveLayoutOverride = true;
            _responsiveTrayWidth = Mathf.Max(0.1f, trayWidth);
            _responsiveTrayHeight = Mathf.Max(0.1f, trayHeight);
            _responsiveTrayCenter = trayCenter;

            var layoutSource = _layoutShapes ?? _currentShapes;
            if (layoutSource != null)
                ApplyCanonicalLayout(layoutSource, "responsive layout override");

            ApplyCurrentLayoutToActiveBlocks();
        }

        public NewSimpleBlock GetBlockAtPosition(Vector2 worldPos, float radius = 1f)
        {
            NewSimpleBlock bestBlock = null;
            float bestDistance = float.MaxValue;
            NewSimpleBlock fallbackBlock = null;
            float fallbackDistance = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                var block = _blocks[i];
                if (block != null && !block.IsUsed)
                {
                    var bounds = block.GetBounds();
                    float trayCellSize = Mathf.Max(0.01f, TrayCellSize);
                    float boundsPadding = Mathf.Min(radius * 0.35f, trayCellSize * 0.45f);
                    bounds.Expand(boundsPadding * 2f);

                    float boundsDistance = bounds.SqrDistance(worldPos);
                    float maxBoundsDistance = Mathf.Max(trayCellSize, boundsPadding);
                    if (boundsDistance <= maxBoundsDistance * maxBoundsDistance)
                    {
                        float anchorDistance = Vector2.Distance(worldPos, block.transform.position);
                        float fallbackSelectionDistance = Mathf.Max(trayCellSize * 1.15f, Mathf.Min(radius * 0.6f, trayCellSize * 1.45f));
                        if (anchorDistance <= fallbackSelectionDistance && anchorDistance < fallbackDistance)
                        {
                            fallbackDistance = anchorDistance;
                            fallbackBlock = block;
                        }

                        float dist = block.GetClosestCellDistance(worldPos);
                        float selectionDistance = Mathf.Max(trayCellSize * 0.9f, Mathf.Min(radius, trayCellSize * 1.15f));
                        if (dist <= selectionDistance && dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestBlock = block;
                        }
                    }
                }
            }

            return bestBlock != null ? bestBlock : fallbackBlock;
        }

        public void MarkBlockAsUsed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 3 && _blocks[slotIndex] != null)
                _blocks[slotIndex].MarkAsUsed();
        }

        public void ResetBlock(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 3 && _blocks[slotIndex] != null)
                _blocks[slotIndex].ResetBlock();
        }

        public int GetActiveBlockCount()
        {
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                if (_blocks[i] != null && !_blocks[i].IsUsed)
                    count++;
            }
            return count;
        }

        private void UpdateSlotLayout(ShapeDefinition[] shapes)
        {
            ApplyCanonicalLayout(shapes, "legacy update request");
        }

        private void EnsureCanonicalLayoutForSlotCreation(ShapeDefinition shape, int slotIndex)
        {
            bool hasSlotStorage = slotPositions != null && slotPositions.Length == 3;
            if (_hasCalculatedLayout && hasSlotStorage && slotIndex >= 0 && slotIndex < slotPositions.Length)
                return;

            ShapeDefinition[] layoutSource = _layoutShapes ?? _currentShapes;
            if (layoutSource == null || layoutSource.Length == 0)
            {
                layoutSource = new ShapeDefinition[3];
                if (slotIndex >= 0 && slotIndex < layoutSource.Length)
                    layoutSource[slotIndex] = shape;
            }

            ApplyCanonicalLayout(layoutSource, "slot creation");
            _hasCalculatedLayout = true;
        }

        private void ApplyCanonicalLayout(ShapeDefinition[] shapes, string reason)
        {
            var config = new TrayLayoutConfig(
                boardCellSize: BoardCellSize,
                boardCellSpacing: BoardCellSpacing,
                trayBlockScale: trayBlockScale,
                slotGap: slotGap,
                trayHorizontalPadding: trayHorizontalPadding,
                trayVerticalPadding: trayVerticalPadding,
                trayGapFromGrid: trayGapFromGrid,
                trayVerticalOffset: trayVerticalOffset,
                minTrayScale: minTrayScale,
                hasResponsiveOverride: _hasResponsiveLayoutOverride,
                responsiveWidth: _responsiveTrayWidth,
                responsiveHeight: _responsiveTrayHeight,
                responsiveCenter: _responsiveTrayCenter);

            ResolveRuntimeReferences();

            var result = TrayLayoutCalculator.Calculate(
                shapes,
                config,
                trayLayoutCamera,
                gridView,
                slotPositions,
                transform.position,
                reason);

            ApplyLayoutResult(result);
        }

        private void ApplyLayoutResult(TrayLayoutResult result)
        {
            if (slotPositions == null || slotPositions.Length != 3)
                slotPositions = new Vector3[3];

            for (int i = 0; i < slotPositions.Length; i++)
                slotPositions[i] = result.SlotPositions[i];

            _currentFitScale = result.TrayScale;
            _lastTrayLayoutCamera = result.LayoutCamera;
            _lastLayoutReason = result.Reason;
            _lastLayoutUsedFallback = result.UsedFallback;

            if (result.UsedFallback)
                LogVerbose($"[NewBlockTray] Applied fallback tray layout. Reason: {result.Reason}");

            RefreshBackdropFromCurrentSlots();
        }

        private void ApplyCurrentLayoutToActiveBlocks()
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                var block = _blocks[i];
                if (block == null)
                    continue;

                ApplyBlockVisualSettings(block);

                if (slotPositions != null && i < slotPositions.Length)
                {
                    Vector3 pos = slotPositions[i];
                    block.transform.position = pos;
                    block.SetOriginalPosition(pos);
                }

                block.SetCellMetrics(TrayCellSize, TrayCellSpacing, true);
            }

            RefreshBackdropFromCurrentSlots();
        }

        private void OnValidate()
        {
            trayBlockBrightness = Mathf.Clamp(trayBlockBrightness, 0.5f, 2.0f);
            dragBrightnessMultiplier = Mathf.Clamp(dragBrightnessMultiplier, 1.0f, 2.0f);
            normalAlpha = Mathf.Clamp01(normalAlpha);
            dragAlpha = Mathf.Clamp01(dragAlpha);
            trayBlockScale = Mathf.Clamp(trayBlockScale, 0.5f, 0.9f);
            minTrayScale = Mathf.Clamp(minTrayScale, 0.2f, 1f);
            slotGap = Mathf.Max(0f, slotGap);
            trayHorizontalPadding = Mathf.Max(0f, trayHorizontalPadding);
            trayVerticalPadding = Mathf.Max(0f, trayVerticalPadding);
            trayGapFromGrid = Mathf.Max(0f, trayGapFromGrid);
            trayCellSpacing = Mathf.Max(0f, trayCellSpacing);
            trayBackdropHorizontalPadding = Mathf.Max(0.1f, trayBackdropHorizontalPadding);
            trayBackdropVerticalPadding = Mathf.Max(0.1f, trayBackdropVerticalPadding);
            trayBackdropSizeAdjustInWorld.x = Mathf.Max(-10f, trayBackdropSizeAdjustInWorld.x);
            trayBackdropSizeAdjustInWorld.y = Mathf.Max(-10f, trayBackdropSizeAdjustInWorld.y);
            trayBackdropBorderThicknessInWorld = Mathf.Max(0f, trayBackdropBorderThicknessInWorld);
            manualUiBackdropPadding.x = Mathf.Max(0f, manualUiBackdropPadding.x);
            manualUiBackdropPadding.y = Mathf.Max(0f, manualUiBackdropPadding.y);
            manualUiBackdropBorderPadding.x = Mathf.Max(0f, manualUiBackdropBorderPadding.x);
            manualUiBackdropBorderPadding.y = Mathf.Max(0f, manualUiBackdropBorderPadding.y);
            manualUiBackdropSizeOverride.x = Mathf.Max(0f, manualUiBackdropSizeOverride.x);
            manualUiBackdropSizeOverride.y = Mathf.Max(0f, manualUiBackdropSizeOverride.y);
            manualUiBackdropBorderSizeOverride.x = Mathf.Max(0f, manualUiBackdropBorderSizeOverride.x);
            manualUiBackdropBorderSizeOverride.y = Mathf.Max(0f, manualUiBackdropBorderSizeOverride.y);

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (_gameBootstrap == null)
                    _gameBootstrap = TryAutoAssignSingleton<GameBootstrap>();

                if (gridView == null)
                    gridView = TryAutoAssignSingleton<SimpleGridView>();

                if (trayLayoutCamera == null)
                    trayLayoutCamera = TryAutoAssignSingleton<Camera>();
#endif
                CleanupGeneratedTrayChildren(true);
                CleanupBackdropRenderers(true);
                return;
            }

            RefreshLivePreviewFromInspector();
        }

        private void CleanupGeneratedTrayChildren(bool immediate)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (child.GetComponent<NewSimpleBlock>() == null)
                    continue;

                if (immediate)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
            }

            for (int i = 0; i < _blocks.Length; i++)
                _blocks[i] = null;

            _blockPool.Clear();
            _hasCalculatedLayout = false;
            _layoutShapes = null;
        }

        private void RefreshLivePreviewFromInspector()
        {
            if (gridView == null)
                gridView = FindFirstObjectByType<SimpleGridView>();

            var layoutSource = _layoutShapes ?? _currentShapes;
            if (layoutSource != null)
                ApplyCanonicalLayout(layoutSource, "inspector preview refresh");

            for (int i = 0; i < _blocks.Length; i++)
            {
                var block = _blocks[i];
                if (block == null)
                    continue;

                ApplyBlockVisualSettings(block);

                if (slotPositions != null && i < slotPositions.Length)
                {
                    Vector3 pos = slotPositions[i];
                    block.transform.position = pos;
                    block.SetOriginalPosition(pos);
                }

                block.SetCellMetrics(TrayCellSize, TrayCellSpacing, true);
            }

            RefreshBackdropFromCurrentSlots();
        }

        private void RefreshBackdropFromCurrentSlots()
        {
            if (slotPositions == null || slotPositions.Length == 0)
                return;

            ShapeDefinition[] layoutSource = _layoutShapes ?? _currentShapes;
            if (layoutSource == null)
                return;

            float baseTrayCellSize = BoardCellSize * trayBlockScale;
            float baseTrayCellSpacing = BoardCellSpacing * trayBlockScale;
            float cellStep = baseTrayCellSize + baseTrayCellSpacing;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            ShapeExtents[] extents = new ShapeExtents[3];

            for (int i = 0; i < 3; i++)
            {
                ShapeDefinition shape = i < layoutSource.Length ? layoutSource[i] : null;
                extents[i] = TrayLayoutCalculator.GetShapeExtents(shape, cellStep, baseTrayCellSize);
                Vector3 slot = slotPositions[i];
                minX = Mathf.Min(minX, slot.x + (extents[i].Left * _currentFitScale));
                maxX = Mathf.Max(maxX, slot.x + (extents[i].Right * _currentFitScale));
                minY = Mathf.Min(minY, slot.y + (extents[i].Bottom * _currentFitScale));
                maxY = Mathf.Max(maxY, slot.y + (extents[i].Top * _currentFitScale));
            }

            CleanupBackdropRenderers(Application.isPlaying == false);
        }

        private void ResolveRuntimeReferences()
        {
            if (gridView == null)
            {
                gridView = FindFirstObjectByType<SimpleGridView>();
                if (gridView != null)
                    LogRuntimeFallback(nameof(gridView), gridView.name);
            }

            if (_gameBootstrap == null)
            {
                _gameBootstrap = FindFirstObjectByType<GameBootstrap>();
                if (_gameBootstrap != null)
                    LogRuntimeFallback(nameof(_gameBootstrap), _gameBootstrap.name);
            }

            if (trayLayoutCamera == null)
            {
                trayLayoutCamera = Camera.main;
                if (trayLayoutCamera != null && !_loggedCameraFallbackWarning)
                {
                    _loggedCameraFallbackWarning = true;
                    Debug.LogWarning("[NewBlockTray] trayLayoutCamera is not wired in the inspector. Falling back to Camera.main.");
                }
            }

            if (!_loggedDependencyWarning && (_gameBootstrap == null || gridView == null))
            {
                _loggedDependencyWarning = true;
                Debug.LogWarning(
                    $"[NewBlockTray] Scene wiring missing. Required references should be assigned in the inspector. " +
                    $"bootstrap={(_gameBootstrap != null)}, gridView={(gridView != null)}");
            }
        }

        private void LogRuntimeFallback(string dependencyName, string resolvedObjectName)
        {
            Debug.LogWarning(
                $"[NewBlockTray] {dependencyName} was resolved via runtime lookup ({resolvedObjectName}). " +
                "Inspector wiring is the preferred production path.");
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        private void CleanupBackdropRenderers(bool immediate)
        {
            if (_trayBackdropRenderer != null)
            {
                if (immediate)
                    DestroyImmediate(_trayBackdropRenderer.gameObject);
                else
                    Destroy(_trayBackdropRenderer.gameObject);
                _trayBackdropRenderer = null;
            }

            if (_trayBackdropBorderRenderer != null)
            {
                if (immediate)
                    DestroyImmediate(_trayBackdropBorderRenderer.gameObject);
                else
                    Destroy(_trayBackdropBorderRenderer.gameObject);
                _trayBackdropBorderRenderer = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (child.name != "TrayBackdrop" && child.name != "TrayBackdropBorder")
                    continue;

                if (immediate)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
            }
        }

        private void GetBackdropBounds(ShapeExtents[] extents, float trayY, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;

            if (!useSlotEnvelopeForAutoBackdrop || slotPositions == null || slotPositions.Length < 3 || extents == null || extents.Length < 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    minX = Mathf.Min(minX, slotPositions[i].x + (extents[i].Left * _currentFitScale));
                    maxX = Mathf.Max(maxX, slotPositions[i].x + (extents[i].Right * _currentFitScale));
                    minY = Mathf.Min(minY, trayY + (extents[i].Bottom * _currentFitScale));
                    maxY = Mathf.Max(maxY, trayY + (extents[i].Top * _currentFitScale));
                }
                return;
            }

            float maxHalfWidth = 0f;
            float maxHalfHeight = 0f;
            for (int i = 0; i < 3; i++)
            {
                maxHalfWidth = Mathf.Max(maxHalfWidth, Mathf.Max(-extents[i].Left, extents[i].Right) * _currentFitScale);
                maxHalfHeight = Mathf.Max(maxHalfHeight, Mathf.Max(extents[i].Top, -extents[i].Bottom) * _currentFitScale);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector3 slot = slotPositions[i];
                minX = Mathf.Min(minX, slot.x - maxHalfWidth);
                maxX = Mathf.Max(maxX, slot.x + maxHalfWidth);
                minY = Mathf.Min(minY, slot.y - maxHalfHeight);
                maxY = Mathf.Max(maxY, slot.y + maxHalfHeight);
            }
        }

        private void UpdateTrayBackdrop(float minX, float maxX, float maxY, float minY)
        {
            if (!showTrayBackdrop && !showTrayBackdropBorder)
            {
                SetBackdropActive(_trayBackdropRenderer, false);
                SetBackdropActive(_trayBackdropBorderRenderer, false);
                return;
            }

            float width = Mathf.Max(0.01f, (maxX - minX) + (trayBackdropHorizontalPadding * 2f) + trayBackdropSizeAdjustInWorld.x);
            float height = Mathf.Max(0.01f, (maxY - minY) + (trayBackdropVerticalPadding * 2f) + trayBackdropSizeAdjustInWorld.y);
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f) + (Vector3)trayBackdropOffset;
            Sprite sprite = GetTrayBackdropSprite();

            if (showTrayBackdrop)
            {
                ApplyBackdropRenderer(
                    ref _trayBackdropRenderer,
                    "TrayBackdrop",
                    sprite,
                    trayBackdropColor,
                    trayBackdropSortingOrder,
                    center,
                    width,
                    height);
            }
            else
            {
                SetBackdropActive(_trayBackdropRenderer, false);
            }

            if (showTrayBackdropBorder && trayBackdropBorderThicknessInWorld > 0f)
            {
                ApplyBackdropRenderer(
                    ref _trayBackdropBorderRenderer,
                    "TrayBackdropBorder",
                    sprite,
                    trayBackdropBorderColor,
                    trayBackdropBorderSortingOrder,
                    center,
                    width + (trayBackdropBorderThicknessInWorld * 2f),
                    height + (trayBackdropBorderThicknessInWorld * 2f));
            }
            else
            {
                SetBackdropActive(_trayBackdropBorderRenderer, false);
            }
        }

        private void ApplyBackdropRenderer(
            ref SpriteRenderer renderer,
            string objectName,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 position,
            float width,
            float height)
        {
            if (renderer == null)
            {
                var go = new GameObject(objectName);
                go.transform.SetParent(transform, false);
                renderer = go.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.position = position;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = new Vector3(
                Mathf.Max(0.01f, width / Mathf.Max(0.0001f, sprite.bounds.size.x)),
                Mathf.Max(0.01f, height / Mathf.Max(0.0001f, sprite.bounds.size.y)),
                1f);
            renderer.enabled = true;
            renderer.gameObject.SetActive(true);
        }

        private void SetBackdropActive(SpriteRenderer renderer, bool active)
        {
            if (renderer == null)
                return;

            renderer.enabled = active;
            renderer.gameObject.SetActive(active);
        }

        private void UpdateManualUiBackdrop(float minX, float maxX, float maxY, float minY)
        {
            if (!driveManualUiBackdrop || manualUiBackdrop == null)
                return;

            ResolveRuntimeReferences();
            Camera cam = _lastTrayLayoutCamera != null ? _lastTrayLayoutCamera : trayLayoutCamera;
            if (cam == null)
                return;

            Canvas canvas = manualUiBackdropCanvas != null ? manualUiBackdropCanvas : manualUiBackdrop.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Vector3 bottomLeftWorld = new Vector3(minX, minY, 0f);
            Vector3 topRightWorld = new Vector3(maxX, maxY, 0f);
            Vector3 bottomLeftScreen = cam.WorldToScreenPoint(bottomLeftWorld);
            Vector3 topRightScreen = cam.WorldToScreenPoint(topRightWorld);

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, bottomLeftScreen, uiCamera, out Vector2 bottomLeftLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, topRightScreen, uiCamera, out Vector2 topRightLocal);

            Vector2 autoCenter = (bottomLeftLocal + topRightLocal) * 0.5f;
            Vector2 autoSize = new Vector2(
                Mathf.Abs(topRightLocal.x - bottomLeftLocal.x),
                Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
            Vector2 center = autoAnchorManualUiBackdrop
                ? autoCenter + manualUiBackdropOffset
                : manualUiBackdropPositionOverride;
            Vector2 size = autoFitManualUiBackdrop
                ? autoSize + manualUiBackdropPadding
                : manualUiBackdropSizeOverride;
            Vector2 borderSize = autoFitManualUiBackdrop
                ? autoSize + manualUiBackdropPadding + manualUiBackdropBorderPadding
                : manualUiBackdropBorderSizeOverride;

            manualUiBackdrop.anchorMin = new Vector2(0.5f, 0.5f);
            manualUiBackdrop.anchorMax = new Vector2(0.5f, 0.5f);
            manualUiBackdrop.pivot = new Vector2(0.5f, 0.5f);
            manualUiBackdrop.anchoredPosition = center;
            manualUiBackdrop.sizeDelta = size;

            if (manualUiBackdropImage == null)
                manualUiBackdropImage = manualUiBackdrop.GetComponent<Image>();

            if (manualUiBackdropImage != null)
            {
                if (useGeneratedUiBackdropSprite)
                {
                    manualUiBackdropImage.sprite = GetGeneratedUiBackdropSprite();
                    manualUiBackdropImage.type = Image.Type.Sliced;
                    manualUiBackdropImage.preserveAspect = false;
                }

                manualUiBackdropImage.color = manualUiBackdropColor;
                manualUiBackdropImage.enabled = true;
            }

            if (manualUiBackdropBorderImage != null)
            {
                RectTransform borderRect = manualUiBackdropBorderImage.rectTransform;
                borderRect.anchorMin = new Vector2(0.5f, 0.5f);
                borderRect.anchorMax = new Vector2(0.5f, 0.5f);
                borderRect.pivot = new Vector2(0.5f, 0.5f);
                borderRect.anchoredPosition = center + manualUiBackdropBorderOffset;
                borderRect.sizeDelta = borderSize;

                if (useGeneratedUiBackdropBorder)
                {
                    manualUiBackdropBorderImage.sprite = GetGeneratedUiBackdropSprite();
                    manualUiBackdropBorderImage.type = Image.Type.Sliced;
                    manualUiBackdropBorderImage.preserveAspect = false;
                }

                manualUiBackdropBorderImage.color = manualUiBackdropBorderColor;
                manualUiBackdropBorderImage.enabled = true;
            }
        }

        private Sprite GetGeneratedUiBackdropSprite()
        {
            if (manualUiBackdropCornerRadius <= 0f)
            {
                if (_generatedUiBackdropSprite != null)
                    return _generatedUiBackdropSprite;

                const int squareSize = 128;
                Texture2D squareTexture = new Texture2D(squareSize, squareSize, TextureFormat.RGBA32, false);
                squareTexture.hideFlags = HideFlags.HideAndDontSave;
                squareTexture.filterMode = FilterMode.Bilinear;
                Color[] pixels = new Color[squareSize * squareSize];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.white;
                squareTexture.SetPixels(pixels);
                squareTexture.Apply();
                _generatedUiBackdropSprite = Sprite.Create(
                    squareTexture,
                    new Rect(0, 0, squareSize, squareSize),
                    new Vector2(0.5f, 0.5f),
                    squareSize,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(squareSize * 0.25f, squareSize * 0.25f, squareSize * 0.25f, squareSize * 0.25f));
                _generatedUiBackdropSprite.name = "TrayUiBackdropSquare";
                _generatedUiBackdropSprite.hideFlags = HideFlags.HideAndDontSave;
                return _generatedUiBackdropSprite;
            }

            int cacheKey = Mathf.RoundToInt(manualUiBackdropCornerRadius * 1000f);
            if (_generatedUiRoundedBackdropSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            const int size = 1024;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            float radius = Mathf.Clamp01(manualUiBackdropCornerRadius) * size * 0.5f;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            float half = (size - 1) * 0.5f;
            float aaWidth = Mathf.Max(1.5f, size * 0.004f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float localX = Mathf.Abs(x - half);
                    float localY = Mathf.Abs(y - half);
                    float dx = Mathf.Max(0f, localX - (half - radius));
                    float dy = Mathf.Max(0f, localY - (half - radius));
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = 1f - Mathf.Clamp01((distance - radius + aaWidth) / aaWidth);
                    Color pixel = solid;
                    pixel.a = alpha;
                    texture.SetPixel(x, y, alpha > 0f ? pixel : clear);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.name = $"TrayUiBackdropRounded_{cacheKey}";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _generatedUiRoundedBackdropSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private Sprite GetTrayBackdropSprite()
        {
            if (trayBackdropCornerRadius <= 0f)
                return GetDefaultBackdropSprite();

            int cacheKey = Mathf.RoundToInt(trayBackdropCornerRadius * 1000f);
            if (_roundedBackdropSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            float radius = Mathf.Clamp01(trayBackdropCornerRadius) * size * 0.5f;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float localX = Mathf.Abs(x - half);
                    float localY = Mathf.Abs(y - half);
                    float dx = Mathf.Max(0f, localX - (half - radius));
                    float dy = Mathf.Max(0f, localY - (half - radius));
                    bool inside = (dx * dx) + (dy * dy) <= (radius * radius);
                    texture.SetPixel(x, y, inside ? solid : clear);
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = $"TrayBackdropRounded_{cacheKey}";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _roundedBackdropSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private Sprite GetDefaultBackdropSprite()
        {
            if (_defaultBackdropSprite != null)
                return _defaultBackdropSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            _defaultBackdropSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _defaultBackdropSprite.name = "TrayBackdropSquare";
            _defaultBackdropSprite.hideFlags = HideFlags.HideAndDontSave;
            return _defaultBackdropSprite;
        }
    }
}
