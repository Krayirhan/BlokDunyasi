using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Grid;

namespace BlockPuzzle.UnityAdapter.Blocks
{
    /// <summary>
    /// YENİ TEMİZ BLOCK TRAY SİSTEMİ
    /// NewSimpleBlock ile çalışır
    /// </summary>
    public class NewBlockTray : MonoBehaviour
    {
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private BlockSpriteConfig spriteConfig;

        [Header("=== REFERENCES ===")]
        [SerializeField] private SimpleGridView gridView;

        [Header("=== BLOCK SETTINGS (GRID MATCH) ===")]
        [SerializeField] private float blockCellSize = 0.5f;
        [SerializeField] private float blockCellSpacing = 0f;

        [Header("=== BLOCK VISUAL SETTINGS ===")]
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

        [Header("=== TRAY SCALE (for preview size) ===")]
        [Tooltip("Tray'deki blokların boyutu. 0.7 = grid'in %70'i")]
        [SerializeField] private float trayBlockScale = 0.7f;

        [Header("=== SLOT POSITIONS ===")]
        [SerializeField] private Vector3[] slotPositions = new Vector3[]
        {
            new Vector3(-2.5f, -4f, 0),
            new Vector3(0f, -4f, 0),
            new Vector3(2.5f, -4f, 0)
        };

        [Header("=== LAYOUT SETTINGS ===")]
        [SerializeField] private float slotGap = 0.4f;
        [SerializeField] private float trayHorizontalPadding = 0.4f;
        [SerializeField] private float trayVerticalPadding = 0.3f;
        [SerializeField] private float trayGapFromGrid = 0.4f;
        [SerializeField] [Range(0.2f, 1f)] private float minTrayScale = 0.35f;

        private readonly struct ShapeExtents
        {
            public readonly float Left;
            public readonly float Right;
            public readonly float Top;
            public readonly float Bottom;

            public float Width => Right - Left;
            public float Height => Top - Bottom;

            public ShapeExtents(float left, float right, float top, float bottom)
            {
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }
        }

        private NewSimpleBlock[] _blocks = new NewSimpleBlock[3];
        private readonly Queue<NewSimpleBlock> _blockPool = new Queue<NewSimpleBlock>();
        private ShapeDefinition[] _currentShapes;
        private ShapeDefinition[] _layoutShapes;
        private float _currentTrayScale = 1f;
        private GameBootstrap _gameBootstrap;
        private bool _hasCalculatedLayout;

        private void Awake()
        {
            if (gridView == null)
                gridView = FindFirstObjectByType<SimpleGridView>();
            
            if (_gameBootstrap == null)
                _gameBootstrap = FindFirstObjectByType<GameBootstrap>();

            // Subscribe to event in Awake to ensure we don't miss the first OnBlocksChanged
            // GameBootstrap.Start() may fire before NewBlockTray.Start()!
            GameBootstrap.OnBlocksChanged += OnBlocksChanged;
            Debug.Log("[NewBlockTray] Subscribed to OnBlocksChanged in Awake");
        }

        private void Start()
        {
            // CRITICAL FIX: Request current blocks in case we missed the initial event
            // This handles race condition where GameBootstrap.Start() fires before NewBlockTray subscribes
            RequestCurrentBlocks();
        }
        
        /// <summary>
        /// Request current block state from GameBootstrap.
        /// Handles the case where GameBootstrap already spawned blocks before we subscribed.
        /// </summary>
        private void RequestCurrentBlocks()
        {
            var bootstrap = FindFirstObjectByType<GameBootstrap>();
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
                
                if (!hasAnyBlock)
                {
                    Debug.Log("[NewBlockTray] No blocks yet - requesting current state from GameBootstrap");
                    // Trigger a refresh by getting current shapes from GameBootstrap
                    var currentShapes = bootstrap.GetCurrentShapes();
                    if (currentShapes != null && currentShapes.Length > 0)
                    {
                        Debug.Log($"[NewBlockTray] Got {currentShapes.Length} shapes from GameBootstrap, refreshing...");
                        OnBlocksChanged(currentShapes);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            GameBootstrap.OnBlocksChanged -= OnBlocksChanged;
        }

        private void OnBlocksChanged(ShapeDefinition[] shapes)
        {
            // Count non-null shapes for debugging
            int nonNullCount = 0;
            for (int i = 0; i < shapes.Length; i++)
            {
                if (shapes[i] != null) nonNullCount++;
            }
            Debug.Log($"[NewBlockTray.OnBlocksChanged] Received {shapes.Length} slots, {nonNullCount} non-null shapes");
            
            _currentShapes = shapes;
            
            // Keep slot positions stable while player consumes the current 3-block set.
            // Recalculate layout only for first render or when a full new set (3 blocks) arrives.
            if (!_hasCalculatedLayout || nonNullCount == 3)
            {
                if (nonNullCount == 3)
                    _layoutShapes = (ShapeDefinition[])shapes.Clone();

                var layoutSource = _layoutShapes ?? shapes;
                UpdateSlotLayout(layoutSource);
                _hasCalculatedLayout = true;
            }

            RefreshAllBlocks();
        }

        private void RefreshAllBlocks()
        {
            int activeCount = 0;

            Debug.Log($"[NewBlockTray.RefreshAllBlocks] Starting refresh. _currentShapes={((_currentShapes != null) ? _currentShapes.Length.ToString() : "NULL")}");

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
                    Debug.Log($"[NewBlockTray.RefreshAllBlocks] Slot {i} is NULL - no block active. Reason: {reason}");
                }
            }

            Debug.Log($"[NewBlockTray.RefreshAllBlocks] Active blocks: {activeCount}, pooled: {_blockPool.Count}");
        }

        private void CreateBlockAt(int slotIndex, ShapeDefinition shape)
        {
            // Safety check for slotPositions array
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

            block.cellSize = blockCellSize * _currentTrayScale;
            block.cellSpacing = blockCellSpacing;
            
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
            block.ResetBlock();
            
            // Yeni blok oluştuysa ve colorId atanmışsa, ActiveBlocks'a set et
            if (block.ColorId > 0 && _gameBootstrap != null && _gameBootstrap.CurrentState != null)
            {
                _gameBootstrap.CurrentState.ActiveBlocks.SetColorId(slotIndex, block.ColorId);
            }
            
            // Verify block was created successfully
            Debug.Log($"[NewBlockTray.CreateBlockAt] Block created: name={blockObj.name}, position={blockObj.transform.position}, scale={blockObj.transform.localScale}, activeInHierarchy={blockObj.activeInHierarchy}, _blocks[{slotIndex}]={(block != null ? "SET" : "NULL")}");
        }

        private void ApplyBlockVisualSettings(NewSimpleBlock block)
        {
            if (block == null)
                return;

            block.ApplyVisualSettings(
                blockColors,
                trayBlockBrightness,
                dragBrightnessMultiplier,
                normalAlpha,
                dragAlpha);
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

            block.gameObject.SetActive(false);
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

            UpdateSlotLayout(layoutSource);
            RefreshAllBlocks();
        }

        // === PUBLIC API ===

        public NewSimpleBlock GetBlock(int index)
        {
            return (index >= 0 && index < 3) ? _blocks[index] : null;
        }

        public NewSimpleBlock GetBlockAtPosition(Vector2 worldPos, float radius = 1f)
        {
            NewSimpleBlock bestBlock = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                var block = _blocks[i];
                if (block != null && !block.IsUsed)
                {
                    var bounds = block.GetBounds();
                    bounds.Expand(radius * 2f);

                    float boundsDistance = bounds.SqrDistance(worldPos);
                    if (boundsDistance <= radius * radius)
                    {
                        float dist = block.GetClosestCellDistance(worldPos);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestBlock = block;
                        }
                    }
                }
            }
            return bestBlock;
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
            var cam = Camera.main;
            if (cam == null)
                return;

            if (slotPositions == null || slotPositions.Length != 3)
                slotPositions = new Vector3[3];

            float cellStep = blockCellSize + blockCellSpacing;
            var extents = new ShapeExtents[3];
            float totalWidthUnits = 0f;
            float maxTop = float.MinValue;
            float minBottom = float.MaxValue;
            float maxHeightUnits = 0f;

            for (int i = 0; i < 3; i++)
            {
                ShapeDefinition shape = (shapes != null && i < shapes.Length) ? shapes[i] : null;
                extents[i] = GetShapeExtents(shape, cellStep);

                totalWidthUnits += extents[i].Width;
                maxTop = Mathf.Max(maxTop, extents[i].Top);
                minBottom = Mathf.Min(minBottom, extents[i].Bottom);
                maxHeightUnits = Mathf.Max(maxHeightUnits, extents[i].Height);
            }

            if (totalWidthUnits <= 0f)
                totalWidthUnits = blockCellSize * 3f;

            if (maxHeightUnits <= 0f)
                maxHeightUnits = blockCellSize;

            float cameraHalfWidth = cam.orthographicSize * cam.aspect;
            float cameraWidth = cameraHalfWidth * 2f;
            float safeAreaWidth = Mathf.Max(0.1f, cameraWidth - (trayHorizontalPadding * 2f));
            float widthBudgetForBlocks = Mathf.Max(0.1f, safeAreaWidth - (slotGap * 2f));
            float maxScaleByWidth = widthBudgetForBlocks / totalWidthUnits;

            float maxScaleByHeight = float.PositiveInfinity;
            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                Vector3 bottomCell = gridView.GetWorldPosition(0, gridView.Height - 1);
                float gridBottom = bottomCell.y - (gridView.TotalCellSize * 0.5f);
                float cameraBottom = cam.transform.position.y - cam.orthographicSize;
                float availableHeight = (gridBottom - trayGapFromGrid) - (cameraBottom + trayVerticalPadding);

                if (availableHeight > 0f)
                    maxScaleByHeight = availableHeight / maxHeightUnits;
                else
                    maxScaleByHeight = 0.1f;
            }

            float fitScale = Mathf.Min(maxScaleByWidth, maxScaleByHeight);
            if (float.IsNaN(fitScale) || fitScale <= 0f)
                fitScale = 0.1f;

            _currentTrayScale = Mathf.Min(trayBlockScale, fitScale);
            if (_currentTrayScale < minTrayScale && fitScale >= minTrayScale)
                _currentTrayScale = minTrayScale;
            _currentTrayScale = Mathf.Max(_currentTrayScale, 0.1f);

            float scaledTotalWidth = (totalWidthUnits * _currentTrayScale) + (slotGap * 2f);
            float safeLeft = cam.transform.position.x - cameraHalfWidth + trayHorizontalPadding;
            float leftBound = safeLeft + Mathf.Max(0f, (safeAreaWidth - scaledTotalWidth) * 0.5f);

            float trayY = GetTrayY(cam, maxTop * _currentTrayScale, minBottom * _currentTrayScale);

            float currentLeft = leftBound;
            for (int i = 0; i < 3; i++)
            {
                float anchorX = currentLeft - (extents[i].Left * _currentTrayScale);
                slotPositions[i] = new Vector3(anchorX, trayY, 0f);
                currentLeft += (extents[i].Width * _currentTrayScale);
                if (i < 2)
                    currentLeft += slotGap;
            }
        }

        private ShapeExtents GetShapeExtents(ShapeDefinition shape, float cellStep)
        {
            float halfCell = blockCellSize * 0.5f;

            if (shape == null || shape.Offsets == null || shape.Offsets.Length == 0)
                return new ShapeExtents(-halfCell, halfCell, halfCell, -halfCell);

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var offset in shape.Offsets)
            {
                minX = Mathf.Min(minX, offset.X);
                maxX = Mathf.Max(maxX, offset.X);
                minY = Mathf.Min(minY, offset.Y);
                maxY = Mathf.Max(maxY, offset.Y);
            }

            float left = (minX * cellStep) - halfCell;
            float right = (maxX * cellStep) + halfCell;
            float top = (-minY * cellStep) + halfCell;
            float bottom = (-maxY * cellStep) - halfCell;

            if (right <= left)
                right = left + blockCellSize;
            if (top <= bottom)
                top = bottom + blockCellSize;

            return new ShapeExtents(left, right, top, bottom);
        }

        private float GetTrayY(Camera cam, float topExtent, float bottomExtent)
        {
            float cameraBottom = cam.transform.position.y - cam.orthographicSize;
            float minAnchorY = (cameraBottom + trayVerticalPadding) - bottomExtent;

            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                Vector3 bottomCell = gridView.GetWorldPosition(0, gridView.Height - 1);
                float gridBottom = bottomCell.y - (gridView.TotalCellSize * 0.5f);
                float maxAnchorY = (gridBottom - trayGapFromGrid) - topExtent;
                if (maxAnchorY >= minAnchorY)
                    return maxAnchorY;
            }

            return minAnchorY;
        }

        private void OnValidate()
        {
            trayBlockBrightness = Mathf.Clamp(trayBlockBrightness, 0.5f, 2.0f);
            dragBrightnessMultiplier = Mathf.Clamp(dragBrightnessMultiplier, 1.0f, 2.0f);
            normalAlpha = Mathf.Clamp01(normalAlpha);
            dragAlpha = Mathf.Clamp01(dragAlpha);

            if (!Application.isPlaying)
                return;

            for (int i = 0; i < _blocks.Length; i++)
                ApplyBlockVisualSettings(_blocks[i]);
        }
    }
}
