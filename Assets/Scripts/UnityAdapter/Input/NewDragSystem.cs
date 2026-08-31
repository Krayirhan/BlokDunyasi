using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Audio;
using BlockPuzzle.UnityAdapter.Animation;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BlockPuzzle.UnityAdapter.Input
{
    /// <summary>
    /// Advanced drag / placement controller for tray blocks.
    /// Preview, placement probe and visual snapping are configurable from the inspector.
    /// </summary>
    public class NewDragSystem : MonoBehaviour
    {
        [Header("REFERENCES")]
        [SerializeField] private Camera _cam;
        [SerializeField] private GameBootstrap _bootstrap;
        [SerializeField] private NewBlockTray blockTray;
        [SerializeField] private SimpleGridView gridView;

        [Header("DRAG")]
        [SerializeField] private float pickRadius = 1.2f;
        [SerializeField] private float dragLiftY = 1.5f;
        [SerializeField] [Range(0f, 2f)] private float boardMarginInCells = 0.18f;

        [Header("DRAG VISUAL SPACING")]
        [SerializeField] private bool overrideDragVisualCellSpacing = false;
        [SerializeField] [Range(0f, 0.5f)] private float dragVisualCellSpacing = 0.05f;

        [Header("VISUAL SNAP")]
        [SerializeField] [Range(0f, 1f)] private float touchGridSnapVisualBlend = 0f;
        [SerializeField] [Range(0f, 1f)] private float mouseGridSnapVisualBlend = 0f;
        [SerializeField] [Range(0.25f, 2f)] private float visualSnapRangeInCells = 0.8f;
        [Header("TOUCH")]
        [SerializeField] private float fingerOffsetY = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float touchDragLiftMultiplier = 0.35f;

        [Header("PICKUP PIVOT")]
        [Tooltip("Additional local offset applied on top of the block's auto-resolved bottom-center pickup point.")]
        [SerializeField] private Vector2 pickupPointLocalOffset = Vector2.zero;

        [Header("PLACEMENT TARGET")]
        [Tooltip("0 = finger/mouse point, 1 = grabbed cell visual target. Values above 1 push placement further upward.")]
        [FormerlySerializedAs("dropLiftBlend")]
        [SerializeField] [Range(0f, 2f)] private float placementPointWeight = 1f;
        [Tooltip("Additional vertical offset added to the placement target in grid-cell units.")]
        [SerializeField] [Range(-2f, 2f)] private float placementExtraOffsetInCells = 0.35f;
        [FormerlySerializedAs("placementProbeOffsetInCells")]
        [SerializeField] private Vector2 legacyPlacementProbeOffsetInCells = Vector2.zero;
        [FormerlySerializedAs("placementProbeWorldOffset")]
        [SerializeField] private Vector2 legacyPlacementProbeWorldOffset = Vector2.zero;
        [FormerlySerializedAs("placementAnchorOffsetCells")]
        [SerializeField] private Vector2 legacyPlacementAnchorOffsetCells = Vector2.zero;

        [Header("PLACEMENT ANCHOR")]
        [Tooltip("Manual cell offset for the resolved placement anchor. X: -left/+right, Y: -up/+down.")]
        [SerializeField] private Vector2Int placementAnchorOffsetCells = Vector2Int.zero;

        [Header("PREVIEW")]
        [SerializeField] private bool showPlacementGhost = true;
        [SerializeField] private bool showInvalidPlacementPreview = true;
        [SerializeField] private bool showLineClearPreview = true;

        private const int InvalidTouchId = -1;

        private PlacementEvaluator _placementEvaluator;
        private DragAnchorResolver _anchorResolver;
        private bool _inputLocked;
        private bool _usingTouchInput;
        private int _activeTouchId = InvalidTouchId;

        private NewSimpleBlock _draggedBlock;
        private bool _isDragging;
        private Int2 _grabbedCellOffset;
        private Vector3 _grabbedCellLocalPosition;
        private bool _highlightPreviewActive;

        private bool _hasCurrentPlacementCandidate;
        private bool _currentPlacementCanPlace;
        private Int2 _currentPlacementAnchor;
        // Committed drop anchor must match the last visible valid preview anchor.
        private bool _hasVisibleValidPreview;
        private Int2 _lastVisibleValidPreviewAnchor;
        private Int2 _lastProcessedAnchor = new Int2(-1, -1);

        private Vector2 _pointerScreen;
        private Vector2 _pointerWorld;
        private Vector2 _blockLogicalPos;
        private SimpleGridView _anchorResolverGridView;
        private float _anchorResolverMargin = float.NaN;
        private IReadOnlyList<Int2> _draggedShapeOffsets;

        private readonly List<int> _previewRows = new List<int>(4);
        private readonly List<int> _previewCols = new List<int>(4);
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);
        private PointerEventData _uiPointerEventData;
        private bool _loggedMissingDependencyWarning;
        private bool _loggedCameraFallbackWarning;

        public void InjectReferences(NewBlockTray tray, SimpleGridView view)
        {
            if (tray != null)
                blockTray = tray;

            if (view != null)
                gridView = view;

            ResolveRuntimeReferences();
        }

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            GameBootstrap.OnGameOver += HandleGameOver;
            GameBootstrap.OnGameStarted += HandleGameStarted;
            GameBootstrap.OnGameContinued += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameBootstrap.OnGameOver -= HandleGameOver;
            GameBootstrap.OnGameStarted -= HandleGameStarted;
            GameBootstrap.OnGameContinued -= HandleGameStarted;
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (_inputLocked && ShouldAutoUnlockInput())
                _inputLocked = false;

            if (_inputLocked)
            {
                if (_isDragging)
                    CancelDrag();
                return;
            }

            if (!TryEnsureCamera())
                return;

            if (TryProcessTouchInput())
                return;

            if (Mouse.current != null)
                ProcessMouse();
        }

        public void SyncAfterContinue()
        {
            HandleGameStarted();
        }

        private void ResolveRuntimeReferences()
        {
            if (_bootstrap == null)
            {
                _bootstrap = FindFirstObjectByType<GameBootstrap>();
                if (_bootstrap != null)
                    LogFallbackResolution(nameof(_bootstrap), _bootstrap.name);
            }

            if (blockTray == null)
            {
                blockTray = FindFirstObjectByType<NewBlockTray>();
                if (blockTray != null)
                    LogFallbackResolution(nameof(blockTray), blockTray.name);
            }

            if (gridView == null)
            {
                gridView = FindFirstObjectByType<SimpleGridView>();
                if (gridView != null)
                    LogFallbackResolution(nameof(gridView), gridView.name);
            }

            if (_placementEvaluator == null && _bootstrap != null)
                _placementEvaluator = new PlacementEvaluator(_bootstrap);

            if (gridView != null &&
                (_anchorResolver == null ||
                 !ReferenceEquals(gridView, _anchorResolverGridView) ||
                 !Mathf.Approximately(boardMarginInCells, _anchorResolverMargin)))
            {
                _anchorResolver = new DragAnchorResolver(gridView, boardMarginInCells);
                _anchorResolverGridView = gridView;
                _anchorResolverMargin = boardMarginInCells;
            }

            ValidateDependencies();
        }

        private bool ShouldAutoUnlockInput()
        {
            ResolveRuntimeReferences();
            return _bootstrap != null &&
                   _bootstrap.CurrentState != null &&
                   !_bootstrap.CurrentState.IsGameOver &&
                   _bootstrap.CurrentState.ActiveBlocks != null &&
                   !_bootstrap.CurrentState.ActiveBlocks.IsEmpty;
        }

        private bool TryEnsureCamera()
        {
            if (_cam != null)
                return true;

            _cam = Camera.main;
            if (_cam != null)
            {
                if (!_loggedCameraFallbackWarning)
                {
                    _loggedCameraFallbackWarning = true;
                    GameLogger.LogWarning("[NewDragSystem] gameplay camera is not wired in the inspector. Falling back to Camera.main.");
                }

                return true;
            }

            if (_isDragging)
                CancelDrag();

            return false;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_cam == null)
                _cam = TryAutoAssignSingleton<Camera>();

            if (_bootstrap == null)
                _bootstrap = TryAutoAssignSingleton<GameBootstrap>();

            if (blockTray == null)
                blockTray = TryAutoAssignSingleton<NewBlockTray>();

            if (gridView == null)
                gridView = TryAutoAssignSingleton<SimpleGridView>();
#endif
        }

        private void ValidateDependencies()
        {
            if (_loggedMissingDependencyWarning)
                return;

            if (_bootstrap != null && blockTray != null && gridView != null)
                return;

            _loggedMissingDependencyWarning = true;
            GameLogger.LogWarning(
                $"[NewDragSystem] Scene wiring missing. Required references should be assigned in the inspector. " +
                $"bootstrap={(_bootstrap != null)}, blockTray={(blockTray != null)}, gridView={(gridView != null)}");
        }

        private void LogFallbackResolution(string dependencyName, string resolvedObjectName)
        {
            GameLogger.LogWarning(
                $"[NewDragSystem] {dependencyName} was resolved via runtime lookup ({resolvedObjectName}). " +
                "Inspector wiring is the preferred production path.");
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        private bool TryProcessTouchInput()
        {
            var touches = Touch.activeTouches;
            if (touches.Count == 0)
            {
                if (_usingTouchInput && _isDragging)
                    CancelDrag();

                ClearPointerTracking();
                return _usingTouchInput;
            }

            _usingTouchInput = true;

            if (_activeTouchId != InvalidTouchId)
            {
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].touchId == _activeTouchId)
                    {
                        ProcessTouch(touches[i]);
                        return true;
                    }
                }

                if (_isDragging)
                    CancelDrag();

                ClearPointerTracking();
                return true;
            }

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                    continue;

                if (IsScreenPositionOverUi(touch.screenPosition))
                    continue;

                ProcessTouch(touch);
                return true;
            }

            return true;
        }

        private void ProcessTouch(Touch touch)
        {
            _pointerScreen = touch.screenPosition;
            _pointerWorld = ScreenToWorld(_pointerScreen);

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    _activeTouchId = touch.touchId;
                    TryPickupBlock();
                    if (!_isDragging)
                        ClearPointerTracking();
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (touch.touchId == _activeTouchId)
                        UpdateDrag();
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    if (touch.touchId == _activeTouchId)
                    {
                        TryPlaceBlock();
                        ClearPointerTracking();
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (touch.touchId == _activeTouchId)
                    {
                        CancelDrag();
                        ClearPointerTracking();
                    }
                    break;
            }
        }

        private void ProcessMouse()
        {
            _usingTouchInput = false;
            _pointerScreen = Mouse.current.position.ReadValue();
            _pointerWorld = ScreenToWorld(_pointerScreen);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!IsScreenPositionOverUi(_pointerScreen))
                    TryPickupBlock();
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                UpdateDrag();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                TryPlaceBlock();
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (!TryEnsureCamera())
                return Vector2.zero;

            Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
            return new Vector2(worldPos.x, worldPos.y);
        }

        private void TryPickupBlock()
        {
            ResolveRuntimeReferences();
            if (_inputLocked || blockTray == null)
                return;

            var block = blockTray.GetBlockAtPosition(_pointerWorld, pickRadius);
            if (block == null || block.IsUsed || block.BlockShape == null)
                return;

            _draggedBlock = block;
            _isDragging = true;
            _blockLogicalPos = _pointerWorld;
            _draggedShapeOffsets = _draggedBlock.BlockShape.GetOffsets();
            _grabbedCellOffset = Int2.Zero;

            float dragCellSpacing = overrideDragVisualCellSpacing
                ? dragVisualCellSpacing
                : blockTray.BoardCellSpacing;
            _draggedBlock.BeginDragVisual(blockTray.BoardCellSize, dragCellSpacing);
            CacheDraggedBlockGeometry();
            ClearCurrentPlacementCandidate();
            ClearPreviewVisuals();
            _lastProcessedAnchor = new Int2(-1, -1);

            UpdateDrag();
        }

        private void UpdateDrag()
        {
            if (_inputLocked)
            {
                CancelDrag();
                return;
            }

            if (!_isDragging || _draggedBlock == null || gridView == null || _bootstrap?.Engine?.CurrentState?.Board == null)
                return;

            _blockLogicalPos = _pointerWorld;

            float visualOffsetY = GetCurrentVisualOffsetY();
            Vector2 pickupPivotVisualTarget = GetPickupPivotVisualTarget(visualOffsetY);
            Vector3 freeVisualPos = GetDraggedBlockWorldPosition(pickupPivotVisualTarget);

            Vector2 placementPos = GetPlacementPosition(pickupPivotVisualTarget);
            bool hasPreviewAnchor = TryResolvePlacementAnchor(placementPos, out Int2 previewAnchor);

            bool anchorChanged = !hasPreviewAnchor || !_lastProcessedAnchor.Equals(previewAnchor);
            if (anchorChanged)
            {
                if (hasPreviewAnchor)
                {
                    bool canPlace = CanPlaceAtAnchor(previewAnchor);
                    SetCurrentPlacementCandidate(previewAnchor, canPlace);
                    UpdateVisibleValidPreviewAnchor(previewAnchor, canPlace);
                    UpdatePreview(previewAnchor, canPlace);
                    _lastProcessedAnchor = previewAnchor;
                }
                else
                {
                    ClearCurrentPlacementCandidate();
                    ClearVisibleValidPreviewAnchor();
                    ClearPreviewVisuals();
                    _lastProcessedAnchor = new Int2(-1, -1);
                }
            }

            Vector3 finalVisualPos = ResolveVisualDragPosition(freeVisualPos, previewAnchor, hasPreviewAnchor, visualOffsetY);
            _draggedBlock.UpdateDragPosition(finalVisualPos);
        }

        private void UpdatePreview(Int2 previewAnchor, bool canPlace)
        {
            if (showPlacementGhost)
            {
                if (canPlace || showInvalidPlacementPreview)
                    gridView.SetPlacementPreview(previewAnchor, _draggedShapeOffsets, canPlace, _draggedBlock.BlockColor, _draggedBlock.ColorId);
                else
                    gridView.ClearPlacementPreview();
            }
            else
            {
                gridView.ClearPlacementPreview();
            }

            if (showLineClearPreview && canPlace && _placementEvaluator != null)
            {
                _placementEvaluator.EvaluateLinesToClear(_draggedBlock.BlockShape, previewAnchor, _previewRows, _previewCols, _draggedShapeOffsets);
                if (_previewRows.Count > 0 || _previewCols.Count > 0)
                {
                    gridView.HighlightLines(_previewRows, _previewCols);
                    _highlightPreviewActive = true;
                }
                else
                {
                    ClearHighlightPreviewIfNeeded();
                }
            }
            else
            {
                ClearHighlightPreviewIfNeeded();
            }
        }

        private bool TryResolvePlacementAnchor(Vector2 placementWorld, out Int2 anchor)
        {
            anchor = default;
            if (_anchorResolver == null)
                return false;

            Vector2 anchorProbeWorld = placementWorld - (Vector2)_grabbedCellLocalPosition;

            bool hasStableAnchor = _hasCurrentPlacementCandidate;
            Int2 currentAnchor = _currentPlacementAnchor;

            if (!_anchorResolver.TryResolveAnchor(anchorProbeWorld, hasStableAnchor, currentAnchor, out Int2 resolvedAnchor))
                return false;

            anchor = ApplyPlacementAnchorOffset(resolvedAnchor);
            return true;
        }

        private Int2 ApplyPlacementAnchorOffset(Int2 anchor)
        {
            return new Int2(
                anchor.X + placementAnchorOffsetCells.x - Mathf.RoundToInt(legacyPlacementAnchorOffsetCells.x),
                anchor.Y + placementAnchorOffsetCells.y - Mathf.RoundToInt(legacyPlacementAnchorOffsetCells.y));
        }

        private bool CanPlaceAtAnchor(Int2 anchor)
        {
            return _bootstrap?.Engine?.CurrentState?.Board != null &&
                   _draggedShapeOffsets != null &&
                   PlacementEngine.CanPlace(_bootstrap.Engine.CurrentState.Board, anchor.X, anchor.Y, _draggedShapeOffsets) == PlacementResult.Success;
        }

        private float GetCurrentVisualOffsetY()
        {
            float cell = gridView != null ? gridView.TotalCellSize : 1f;
            if (_usingTouchInput)
                return Mathf.Max(fingerOffsetY, dragLiftY * touchDragLiftMultiplier, cell * 0.45f);

            return Mathf.Min(dragLiftY, Mathf.Max(cell * 0.45f, dragLiftY * 0.45f));
        }

        private Vector2 GetPickupPivotVisualTarget(float visualOffsetY)
        {
            return new Vector2(_blockLogicalPos.x, _blockLogicalPos.y + visualOffsetY);
        }

        private Vector3 GetDraggedBlockWorldPosition(Vector2 grabbedCellVisualTarget)
        {
            if (_draggedBlock == null)
                return new Vector3(grabbedCellVisualTarget.x, grabbedCellVisualTarget.y, 0f);

            return new Vector3(
                grabbedCellVisualTarget.x - _grabbedCellLocalPosition.x,
                grabbedCellVisualTarget.y - _grabbedCellLocalPosition.y,
                0f);
        }

        private Vector2 GetPlacementPosition(Vector2 pickupPivotVisualTarget)
        {
            float cell = gridView != null ? gridView.TotalCellSize : 1f;
            Vector2 weightedBase = Vector2.LerpUnclamped(_pointerWorld, pickupPivotVisualTarget, placementPointWeight);

            return weightedBase +
                   new Vector2(
                       legacyPlacementProbeOffsetInCells.x * cell + legacyPlacementProbeWorldOffset.x,
                       (placementExtraOffsetInCells + legacyPlacementProbeOffsetInCells.y) * cell +
                       legacyPlacementProbeWorldOffset.y);
        }

        private Vector3 ResolveVisualDragPosition(Vector3 freeVisualPos, Int2 previewAnchor, bool hasPreviewAnchor, float visualOffsetY)
        {
            if (!hasPreviewAnchor || gridView == null)
                return freeVisualPos;

            float blend = _usingTouchInput ? touchGridSnapVisualBlend : mouseGridSnapVisualBlend;
            if (blend <= 0f)
                return freeVisualPos;

            Vector3 anchorWorldPos = gridView.GetWorldPosition(
                previewAnchor.X + _grabbedCellOffset.X,
                previewAnchor.Y + _grabbedCellOffset.Y);

            Vector2 placementProbe = GetPlacementPosition(GetPickupPivotVisualTarget(visualOffsetY));
            float maxSnapDistance = gridView.TotalCellSize * Mathf.Max(0.01f, visualSnapRangeInCells);
            float distanceToAnchor = Vector2.Distance(placementProbe, anchorWorldPos);
            float snapInfluence = 1f - Mathf.Clamp01(distanceToAnchor / maxSnapDistance);
            snapInfluence *= snapInfluence;
            blend *= snapInfluence;

            if (blend <= 0.001f)
                return freeVisualPos;

            Vector3 snappedVisualPos = GetDraggedBlockWorldPosition(
                new Vector2(
                    anchorWorldPos.x + _grabbedCellLocalPosition.x,
                    anchorWorldPos.y + _grabbedCellLocalPosition.y));

            return Vector3.Lerp(freeVisualPos, snappedVisualPos, blend);
        }

        private void CacheDraggedBlockGeometry()
        {
            if (_draggedBlock == null)
            {
                _grabbedCellLocalPosition = Vector3.zero;
                return;
            }

            _grabbedCellLocalPosition = GetBottomCenterPickupLocalPosition() +
                                        new Vector3(pickupPointLocalOffset.x, pickupPointLocalOffset.y, 0f);
        }

        private Vector3 GetBottomCenterPickupLocalPosition()
        {
            if (_draggedBlock == null || _draggedShapeOffsets == null || _draggedShapeOffsets.Count == 0)
                return Vector3.zero;

            float halfCell = _draggedBlock.cellSize * 0.5f;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;

            for (int i = 0; i < _draggedShapeOffsets.Count; i++)
            {
                Vector3 localCellCenter = _draggedBlock.GetLocalCellPosition(_draggedShapeOffsets[i]);
                minX = Mathf.Min(minX, localCellCenter.x - halfCell);
                maxX = Mathf.Max(maxX, localCellCenter.x + halfCell);
                minY = Mathf.Min(minY, localCellCenter.y - halfCell);
            }

            if (minX == float.MaxValue || maxX == float.MinValue || minY == float.MaxValue)
                return Vector3.zero;

            return new Vector3((minX + maxX) * 0.5f, minY, 0f);
        }

        private void TryPlaceBlock()
        {
            if (_inputLocked)
            {
                CancelDrag();
                return;
            }

            if (!_isDragging || _draggedBlock == null)
            {
                CancelDrag();
                return;
            }

            bool hasCommitAnchor = TryGetCommittedDropAnchor(out Int2 commitAnchor);
            bool commitAnchorStillValid = hasCommitAnchor && CanPlaceAtAnchor(commitAnchor);

            if (hasCommitAnchor && !commitAnchorStillValid)
            {
                Debug.LogWarning($"[NewDragSystem] Rejecting drop because committed preview anchor {commitAnchor} is no longer valid at release.");
            }

            int placedSlotIndex = _draggedBlock.SlotIndex;
            bool placed = _bootstrap != null &&
                          hasCommitAnchor &&
                          commitAnchorStillValid &&
                          _bootstrap.TryPlaceBlock(placedSlotIndex, commitAnchor);

            if (placed)
            {
                EmitPlacementFeedback(_draggedBlock, commitAnchor);

                bool slotRefilled = _bootstrap != null &&
                                    _bootstrap.CurrentState != null &&
                                    _bootstrap.CurrentState.ActiveBlocks.HasBlockAt(placedSlotIndex);

                if (!slotRefilled)
                    blockTray?.MarkBlockAsUsed(placedSlotIndex);
            }
            else
            {
                AudioManager.Instance?.PlayInvalidDrop();
                float trayCellSize = blockTray != null ? blockTray.TrayCellSize : _draggedBlock.cellSize;
                float trayCellSpacing = blockTray != null ? blockTray.TrayCellSpacing : 0f;
                _draggedBlock.RejectPlacement(trayCellSize, trayCellSpacing);
            }

            FinishDragState();
        }

        private void EmitPlacementFeedback(NewSimpleBlock block, Int2 anchor)
        {
            if (block == null || gridView == null || VFXEmitter.Instance == null || _draggedShapeOffsets == null)
                return;

            Color blockColor = block.BlockColor;
            float cellSize = gridView.CellSize;

            for (int i = 0; i < _draggedShapeOffsets.Count; i++)
            {
                Int2 offset = _draggedShapeOffsets[i];
                Vector3 cellPosition = gridView.GetWorldPosition(anchor.X + offset.X, anchor.Y + offset.Y);
                Transform cellTransform = gridView.GetCellTransform(anchor.X + offset.X, anchor.Y + offset.Y);
                if (cellTransform != null)
                    AnimationController.Instance.PlayBlockPlacementAnim(cellTransform.gameObject);
                VFXEmitter.Instance.EmitPlacementDust(cellPosition, cellSize, blockColor);
            }
        }

        private void CancelDrag()
        {
            RestoreDraggedBlockToTray();
            FinishDragState();
        }

        private void RestoreDraggedBlockToTray()
        {
            if (_draggedBlock == null)
                return;

            if (blockTray != null)
                _draggedBlock.ReturnToTrayVisual(blockTray.TrayCellSize, blockTray.TrayCellSpacing);

            _draggedBlock.ReturnToOriginalPosition();
        }

        private void FinishDragState()
        {
            ClearPreviewVisuals();
            _draggedBlock = null;
            _isDragging = false;
            _draggedShapeOffsets = null;
            _grabbedCellOffset = Int2.Zero;
            _grabbedCellLocalPosition = Vector3.zero;
            _blockLogicalPos = Vector2.zero;
            ClearCurrentPlacementCandidate();
            ClearVisibleValidPreviewAnchor();
            _lastProcessedAnchor = new Int2(-1, -1);
        }

        private void ClearPreviewVisuals()
        {
            gridView?.ClearPlacementPreview();
            ClearHighlightPreviewIfNeeded();
            _previewRows.Clear();
            _previewCols.Clear();
        }

        private void SetCurrentPlacementCandidate(Int2 anchor, bool canPlace)
        {
            _hasCurrentPlacementCandidate = true;
            _currentPlacementAnchor = anchor;
            _currentPlacementCanPlace = canPlace;
        }

        private void UpdateVisibleValidPreviewAnchor(Int2 anchor, bool canPlace)
        {
            if (canPlace)
            {
                _hasVisibleValidPreview = true;
                _lastVisibleValidPreviewAnchor = anchor;
                return;
            }

            ClearVisibleValidPreviewAnchor();
        }

        private void ClearCurrentPlacementCandidate()
        {
            _hasCurrentPlacementCandidate = false;
            _currentPlacementCanPlace = false;
            _currentPlacementAnchor = default;
        }

        private void ClearVisibleValidPreviewAnchor()
        {
            _hasVisibleValidPreview = false;
            _lastVisibleValidPreviewAnchor = default;
        }

        private bool TryGetCommittedDropAnchor(out Int2 anchor)
        {
            if (_hasVisibleValidPreview)
            {
                anchor = _lastVisibleValidPreviewAnchor;
                return true;
            }

            anchor = default;
            return false;
        }

        private void ClearHighlightPreviewIfNeeded()
        {
            if (!_highlightPreviewActive)
                return;

            gridView?.ClearLineHighlights();
            _highlightPreviewActive = false;
        }

        private void HandleGameOver(int finalScore)
        {
            _inputLocked = true;
            CancelDrag();
            ClearPointerTracking();
        }

        private void HandleGameStarted()
        {
            ResolveRuntimeReferences();
            _inputLocked = false;
            ClearPointerTracking();
            FinishDragState();
        }

        private void ClearPointerTracking()
        {
            _activeTouchId = InvalidTouchId;
            _usingTouchInput = false;
        }

        private bool IsScreenPositionOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            if (_uiPointerEventData == null)
                _uiPointerEventData = new PointerEventData(EventSystem.current);

            _uiPointerEventData.position = screenPosition;
            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(_uiPointerEventData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }
    }
}
