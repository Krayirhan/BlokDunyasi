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

        private NewSimpleBlock[] _blocks = new NewSimpleBlock[3];
        private ShapeDefinition[] _currentShapes;
        private float _currentTrayScale = 1f;

        private void Awake()
        {
            if (gridView == null)
                gridView = FindFirstObjectByType<SimpleGridView>();

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
            UpdateSlotLayout(shapes);
            RefreshAllBlocks();
        }

        private void RefreshAllBlocks()
        {
            int createdCount = 0;
            
            Debug.Log($"[NewBlockTray.RefreshAllBlocks] Starting refresh. _currentShapes={((_currentShapes != null) ? _currentShapes.Length.ToString() : "NULL")}");
            
            for (int i = 0; i < 3; i++)
            {
                if (_blocks[i] != null)
                {
                    Debug.Log($"[NewBlockTray.RefreshAllBlocks] Destroying old block at slot {i}: {_blocks[i].gameObject.name}");
                    Destroy(_blocks[i].gameObject);
                    _blocks[i] = null;
                }

                if (_currentShapes != null && i < _currentShapes.Length && _currentShapes[i] != null)
                {
                    CreateBlockAt(i, _currentShapes[i]);
                    createdCount++;
                }
                else
                {
                    string reason = _currentShapes == null ? "_currentShapes is NULL" :
                                    i >= _currentShapes.Length ? $"index {i} >= Length {_currentShapes.Length}" :
                                    "_currentShapes[i] is NULL";
                    Debug.Log($"[NewBlockTray.RefreshAllBlocks] Slot {i} is NULL - no block created. Reason: {reason}");
                }
            }
            
            Debug.Log($"[NewBlockTray.RefreshAllBlocks] Created {createdCount} blocks total");
            
            // Final verification - count actual blocks in _blocks array
            int actualBlockCount = 0;
            for (int i = 0; i < 3; i++)
            {
                if (_blocks[i] != null)
                {
                    actualBlockCount++;
                    Debug.Log($"[NewBlockTray.RefreshAllBlocks] VERIFY: _blocks[{i}] = {_blocks[i].gameObject.name}, IsUsed={_blocks[i].IsUsed}, position={_blocks[i].transform.position}");
                }
            }
            Debug.Log($"[NewBlockTray.RefreshAllBlocks] FINAL: {actualBlockCount} blocks in _blocks array");
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
            Debug.Log($"[NewBlockTray.CreateBlockAt] Creating block at slot {slotIndex}: {shape.Name} (ShapeId: {shape.Id}) at position {pos}");
            
            var blockObj = new GameObject($"Block_{slotIndex}_{shape.Name}");
            blockObj.transform.SetParent(transform);
            blockObj.transform.position = pos;
            blockObj.transform.localScale = Vector3.one * _currentTrayScale;

            var block = blockObj.AddComponent<NewSimpleBlock>();
            block.cellSize = blockCellSize;
            block.cellSpacing = blockCellSpacing;
            block.Initialize(shape, spriteConfig, slotIndex);

            _blocks[slotIndex] = block;
            
            // Verify block was created successfully
            Debug.Log($"[NewBlockTray.CreateBlockAt] Block created: name={blockObj.name}, position={blockObj.transform.position}, scale={blockObj.transform.localScale}, activeInHierarchy={blockObj.activeInHierarchy}, _blocks[{slotIndex}]={(block != null ? "SET" : "NULL")}");
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

            int maxWidthCells = 1;
            int maxHeightCells = 1;

            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    if (shape == null || shape.Offsets == null || shape.Offsets.Length == 0)
                        continue;

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

                    int widthCells = maxX - minX + 1;
                    int heightCells = maxY - minY + 1;

                    if (widthCells > maxWidthCells) maxWidthCells = widthCells;
                    if (heightCells > maxHeightCells) maxHeightCells = heightCells;
                }
            }

            float cellStep = blockCellSize + blockCellSpacing;
            float maxWidthUnits = maxWidthCells * cellStep;
            float maxHeightUnits = maxHeightCells * cellStep;

            float cameraHalfWidth = cam.orthographicSize * cam.aspect;
            float cameraWidth = cameraHalfWidth * 2f;
            float availableWidth = cameraWidth - (trayHorizontalPadding * 2f) - (slotGap * 2f);
            float maxScale = availableWidth / (3f * maxWidthUnits);

            if (maxScale <= 0f || float.IsNaN(maxScale))
            {
                _currentTrayScale = minTrayScale;
            }
            else
            {
                _currentTrayScale = Mathf.Min(trayBlockScale, maxScale);
                if (_currentTrayScale < minTrayScale)
                    _currentTrayScale = maxScale;
            }

            _currentTrayScale = Mathf.Max(_currentTrayScale, 0.1f);

            float blockWidth = maxWidthUnits * _currentTrayScale;
            float totalWidth = (3f * blockWidth) + (2f * slotGap);
            float leftX = cam.transform.position.x - (totalWidth * 0.5f) + (blockWidth * 0.5f);

            float trayY = GetTrayY(cam, maxHeightUnits * _currentTrayScale);

            slotPositions[0] = new Vector3(leftX, trayY, 0f);
            slotPositions[1] = new Vector3(leftX + blockWidth + slotGap, trayY, 0f);
            slotPositions[2] = new Vector3(leftX + (blockWidth + slotGap) * 2f, trayY, 0f);
        }

        private float GetTrayY(Camera cam, float blockHeight)
        {
            float cameraBottom = cam.transform.position.y - cam.orthographicSize;
            float minY = cameraBottom + trayVerticalPadding + (blockHeight * 0.5f);

            if (gridView != null && gridView.Width > 0 && gridView.Height > 0)
            {
                Vector3 bottomCell = gridView.GetWorldPosition(0, gridView.Height - 1);
                float gridBottom = bottomCell.y - (gridView.TotalCellSize * 0.5f);
                float desired = gridBottom - trayGapFromGrid - (blockHeight * 0.5f);
                return Mathf.Max(minY, desired);
            }

            return minY;
        }
    }
}
