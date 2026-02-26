using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.UnityAdapter.Input
{
    /// <summary>
    /// YENİ TEMİZ DRAG SİSTEMİ
    /// 
    /// Temel Prensipler:
    /// 1. Blok sürüklenirken, bloğun (0,0) anchor hücresi pointer'ın olduğu yere gider
    /// 2. Grid pozisyonu hesaplanırken visual lift KULLANILMAZ (sadece görsel)
    /// 3. Preview ve placement AYNI koordinatları kullanır
    /// 4. Tüm dönüşümler SimpleGridView üzerinden yapılır
    /// </summary>
    public class NewDragSystem : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private NewBlockTray blockTray;
        [SerializeField] private SimpleGridView gridView;
        [SerializeField] private NewPreviewSystem previewSystem;

        [Header("=== DRAG SETTINGS ===")]
        [SerializeField] private float pickRadius = 1.2f;
        [SerializeField] private float dragLiftY = 1.5f;
        
        [Header("=== TOUCH OFFSET (finger visibility) ===")]
        [SerializeField] private float fingerOffsetY = 0.8f;

        [Header("=== DROP LIFT (placement offset) ===")]
        [SerializeField] [Range(0f, 1f)] private float dropLiftBlend = 0f;

        // State
        private Camera _cam;
        private bool _cameraMissingLogged;
        private GameBootstrap _bootstrap;
        private NewSimpleBlock _draggedBlock;
        private bool _isDragging;
        
        // Pozisyon tracking
        private Vector2 _pointerWorld;      // Parmak/mouse dünya pozisyonu
        private Vector2 _blockLogicalPos;   // Bloğun mantıksal pozisyonu (grid hesaplama için)
        private Int2 _currentGridAnchor;    // Mevcut grid anchor pozisyonu
        private bool _isOverGrid;           // Grid üzerinde mi?

        private void Awake()
        {
            _cam = Camera.main;
            _cameraMissingLogged = false;
            _bootstrap = FindFirstObjectByType<GameBootstrap>();
            
            if (blockTray == null) blockTray = FindFirstObjectByType<NewBlockTray>();
            if (gridView == null) gridView = FindFirstObjectByType<SimpleGridView>();
            if (previewSystem == null) previewSystem = FindFirstObjectByType<NewPreviewSystem>();
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Update()
        {
            if (!TryEnsureCamera())
                return;
            // Input kaynağını seç
            if (Touch.activeTouches.Count > 0)
            {
                ProcessTouch(Touch.activeTouches[0]);
            }
            else if (Mouse.current != null)
            {
                ProcessMouse();
            }
        }

        private void ProcessTouch(Touch touch)
        {
            _pointerWorld = ScreenToWorld(touch.screenPosition);
            _pointerWorld.y += fingerOffsetY; // Parmak görünürlüğü için offset

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    TryPickupBlock();
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    UpdateDrag();
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    TryPlaceBlock();
                    break;
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    CancelDrag();
                    break;
            }
        }

        private void ProcessMouse()
        {
            var mouse = Mouse.current;
            _pointerWorld = ScreenToWorld(mouse.position.ReadValue());

            if (mouse.leftButton.wasPressedThisFrame)
                TryPickupBlock();
            else if (mouse.leftButton.isPressed)
                UpdateDrag();
            else if (mouse.leftButton.wasReleasedThisFrame)
                TryPlaceBlock();
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (!TryEnsureCamera())
                return Vector2.zero;
            Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
            return new Vector2(worldPos.x, worldPos.y);
        }
        private bool TryEnsureCamera()
        {
            if (_cam != null)
                return true;

            _cam = Camera.main;
            if (_cam != null)
            {
                _cameraMissingLogged = false;
                return true;
            }

            if (!_cameraMissingLogged)
            {
                Debug.LogError("[NewDragSystem] MainCamera not found. Drag/preview disabled until a camera with MainCamera tag exists.");
                _cameraMissingLogged = true;
            }

            if (_isDragging)
            {
                CancelDrag();
            }

            return false;
        }

        private void TryPickupBlock()
        {
            if (blockTray == null) return;

            var block = blockTray.GetBlockAtPosition(_pointerWorld, pickRadius);
            if (block == null || block.IsUsed) return;

            _draggedBlock = block;
            _isDragging = true;
            _blockLogicalPos = _pointerWorld;
            
            _draggedBlock.StartDrag();
            
            // Preview başlat
            if (previewSystem != null && _draggedBlock.BlockShape != null)
            {
                previewSystem.StartPreview(_draggedBlock.BlockShape);
            }

            UpdateDrag();
            
            Debug.Log($"[NewDragSystem] Picked up block: {_draggedBlock.BlockShape?.Name}");
        }

        private void UpdateDrag()
        {
            if (!_isDragging || _draggedBlock == null) return;

            // Mantıksal pozisyon = pointer pozisyonu (offset yok, anchor = pointer)
            _blockLogicalPos = _pointerWorld;

            // Visual pozisyon = mantıksal + lift
            Vector3 visualPos = new Vector3(_blockLogicalPos.x, _blockLogicalPos.y + dragLiftY, 0);
            _draggedBlock.UpdateDragPosition(visualPos);

            // Grid pozisyonunu hesapla (mantıksal pozisyon üzerinden, lift YOK)
            var placementPos = GetPlacementPosition(_blockLogicalPos);
            _isOverGrid = CalculateGridAnchor(placementPos, out _currentGridAnchor);

            // Preview güncelle
            if (previewSystem != null)
            {
                if (_isOverGrid && CanPlaceAtAnchor(_currentGridAnchor))
                {
                    previewSystem.ShowPreview(_currentGridAnchor, true);
                }
                else if (_isOverGrid)
                {
                    previewSystem.ShowPreview(_currentGridAnchor, false);
                }
                else
                {
                    previewSystem.HidePreview();
                }
            }

            // Satır/sütun highlight güncelle
            if (gridView != null)
            {
                if (_isOverGrid && CanPlaceAtAnchor(_currentGridAnchor))
                {
                    GetWouldClearLines(_currentGridAnchor, out var rows, out var cols);
                    if (rows.Count > 0 || cols.Count > 0)
                        gridView.HighlightLines(rows, cols);
                    else
                        gridView.ClearLineHighlights();
                }
                else
                {
                    gridView.ClearLineHighlights();
                }
            }
        }

        private Vector2 GetPlacementPosition(Vector2 logicalPos)
        {
            if (dropLiftBlend <= 0f)
                return logicalPos;

            return new Vector2(logicalPos.x, logicalPos.y + dragLiftY * dropLiftBlend);
        }

        /// <summary>
        /// Calculates grid anchor from pointer position.
        /// </summary>
        private bool CalculateGridAnchor(Vector2 pointerWorldPos, out Int2 anchor)
        {
            anchor = default;
            
            if (gridView == null) return false;

            // Pointer pozisyonunu doğrudan grid koordinatına çevir
            // Bu pozisyon = (0,0) anchor hücresinin grid pozisyonu
            if (!gridView.GetGridPosition(pointerWorldPos, out int gx, out int gy))
                return false;

            anchor = new Int2(gx, gy);
            return true;
        }

        private bool CanPlaceAtAnchor(Int2 anchor)
        {
            if (_bootstrap?.Engine == null || _draggedBlock?.BlockShape == null)
                return false;

            var board = _bootstrap.Engine.CurrentState.Board;
            var offsets = _draggedBlock.BlockShape.GetOffsets();
            
            return PlacementEngine.CanPlace(board, anchor.X, anchor.Y, offsets) == PlacementResult.Success;
        }

        private void TryPlaceBlock()
        {
            if (!_isDragging || _draggedBlock == null)
            {
                CancelDrag();
                return;
            }

            bool placed = false;
            int slotIndex = _draggedBlock.SlotIndex;

            if (_isOverGrid && _bootstrap != null)
            {
                placed = _bootstrap.TryPlaceBlock(slotIndex, _currentGridAnchor);
            }

            if (placed)
            {
                Debug.Log($"[NewDragSystem] Placed block at anchor ({_currentGridAnchor.X}, {_currentGridAnchor.Y})");
                // If a new set spawned immediately, the slot is refilled; don't hide the new block.
                bool slotRefilled = _bootstrap != null && _bootstrap.CurrentState != null &&
                                    _bootstrap.CurrentState.ActiveBlocks.HasBlockAt(slotIndex);
                if (!slotRefilled)
                {
                    blockTray?.MarkBlockAsUsed(slotIndex);
                }
                previewSystem?.EndPreview(true);
            }
            else
            {
                Debug.Log("[NewDragSystem] Placement failed, returning block");
                _draggedBlock.ReturnToOriginalPosition();
                previewSystem?.EndPreview(false);
            }

            gridView?.ClearLineHighlights();
            _draggedBlock = null;
            _isDragging = false;
        }

        private void CancelDrag()
        {
            if (_draggedBlock != null)
            {
                _draggedBlock.ReturnToOriginalPosition();
            }
            
            previewSystem?.EndPreview(false);
            gridView?.ClearLineHighlights();

            _draggedBlock = null;
            _isDragging = false;
        }

        /// <summary>
        /// Bloğu anchor pozisyonuna koyarsak hangi satır ve sütunlar temizlenir?
        /// Board'u değiştirmeden simüle eder.
        /// </summary>
        private void GetWouldClearLines(Int2 anchor, out List<int> fullRows, out List<int> fullCols)
        {
            fullRows = new List<int>();
            fullCols = new List<int>();

            if (_bootstrap?.Engine == null || _draggedBlock?.BlockShape == null) return;

            var board = _bootstrap.Engine.CurrentState.Board;
            var offsets = _draggedBlock.BlockShape.GetOffsets();

            // Şeklin dolduracağı hücreleri hesapla
            var shapeCells = new HashSet<(int, int)>();
            var touchedRows = new HashSet<int>();
            var touchedCols = new HashSet<int>();

            foreach (var offset in offsets)
            {
                int cx = anchor.X + offset.X;
                int cy = anchor.Y + offset.Y;

                // Sınır dışıysa zaten yerleştirilemez, çık
                if (cx < 0 || cx >= board.Width || cy < 0 || cy >= board.Height)
                    return;

                shapeCells.Add((cx, cy));
                touchedRows.Add(cy);
                touchedCols.Add(cx);
            }

            // Her dokunulan satırı kontrol et
            foreach (int row in touchedRows)
            {
                int filledCount = 0;
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.IsOccupied(x, row) || shapeCells.Contains((x, row)))
                        filledCount++;
                }
                if (filledCount == board.Width)
                    fullRows.Add(row);
            }

            // Her dokunulan sütunu kontrol et
            foreach (int col in touchedCols)
            {
                int filledCount = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.IsOccupied(col, y) || shapeCells.Contains((col, y)))
                        filledCount++;
                }
                if (filledCount == board.Height)
                    fullCols.Add(col);
            }
        }

        // Public accessors
        public bool IsDragging => _isDragging;
        public NewSimpleBlock DraggedBlock => _draggedBlock;
        public Int2 CurrentAnchor => _currentGridAnchor;
        public bool IsOverGrid => _isOverGrid;
    }
}
