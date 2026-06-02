// File: Core/RNG/BlockSpawner.cs
using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.Core.Board;

namespace BlockPuzzle.Core.RNG
{
    /// <summary>
    /// Manages spawning of block sets with adaptive difficulty and safety checks.
    /// Ensures players always have at least one placeable block when possible.
    /// </summary>
    public class BlockSpawner
    {
        private const int RecentHistorySize = 9;
        private const int MaxSameShapePerSet = 2;
        private const int EasyShapeMaxCellCount = 2;
        private const int MaxSetsWithoutEasyShape = 2;
        private const int DefaultMiniBagSize = 9;
        private const int MinMiniBagSize = 6;
        private const int StruggleSafeShapeMinCount = 2;

        private readonly SeededRng _rng;
        private readonly DifficultyModel _difficultyModel;
        private readonly WeightedPicker<ShapeId> _shapePicker;
        private readonly Queue<ShapeId> _recentSpawnHistory;
        private readonly Queue<ShapeId> _miniBag;
        private int _setsWithoutEasyShape;
        
        /// <summary>
        /// Number of blocks to spawn per set (typically 3 for Blok Dünyası).
        /// </summary>
        public int BlocksPerSet { get; set; } = 3;
        
        /// <summary>
        /// Maximum attempts to generate a valid block set before fallback.
        /// </summary>
        public int MaxGenerationAttempts { get; set; } = 5;
        
        /// <summary>
        /// Whether to apply safety checks to ensure at least one block is placeable.
        /// </summary>
        public bool UseSafetyChecks { get; set; } = true;

        /// <summary>
        /// Enables a second-pass board health evaluation so sets are not only placeable,
        /// but also leave a reasonable continuation path.
        /// </summary>
        public bool UseFutureSolvabilityChecks { get; set; } = true;

        /// <summary>
        /// Enables mini-bag based distribution for smoother and fairer block cadence.
        /// </summary>
        public bool UseMiniBag { get; set; } = true;

        /// <summary>
        /// Target size of the mini-bag pool.
        /// </summary>
        public int MiniBagSize { get; set; } = DefaultMiniBagSize;
        
        /// <summary>
        /// Current difficulty model for adaptive spawning.
        /// </summary>
        public DifficultyModel DifficultyModel => _difficultyModel;

        public float MinFutureOpenAreaScore { get; set; } = 0.22f;
        public int MinLargestEmptyRectangleArea { get; set; } = 6;
        
        public BlockSpawner(SeededRng rng, DifficultyModel difficultyModel = null)
        {
            _rng = rng;
            _difficultyModel = difficultyModel ?? new DifficultyModel();
            _shapePicker = new WeightedPicker<ShapeId>();
            _recentSpawnHistory = new Queue<ShapeId>(RecentHistorySize);
            _miniBag = new Queue<ShapeId>(DefaultMiniBagSize);
            _setsWithoutEasyShape = 0;
            
            InitializeShapeWeights();
        }
        
        /// <summary>
        /// Spawns a new set of blocks based on current difficulty and board state.
        /// </summary>
        /// <param name="boardState">Current board state for safety checks</param>
        /// <returns>Array of shape IDs for the new block set</returns>
        public ShapeId[] SpawnBlockSet(BoardState boardState)
        {
            UpdateShapeWeights();
            
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                var blockSet = GenerateBlockSet(boardState);
                
                // CRITICAL FIX: Validate all ShapeIds exist in ShapeLibrary before returning
                blockSet = ValidateAndFixBlockSet(blockSet);
                
                if (!UseSafetyChecks || IsBlockSetHealthy(blockSet, boardState))
                {
                    TrackSpawnHistory(blockSet);
                    return blockSet;
                }
            }
            
            // Fallback: generate a guaranteed safe set
            var safeSet = GenerateSafeBlockSet(boardState);
            safeSet = ValidateAndFixBlockSet(safeSet);
            TrackSpawnHistory(safeSet);
            return safeSet;
        }
        
        /// <summary>
        /// Validates that all ShapeIds in the block set exist in ShapeLibrary.
        /// Replaces any invalid ShapeIds with Single block as fallback.
        /// </summary>
        private ShapeId[] ValidateAndFixBlockSet(ShapeId[] blockSet)
        {
            for (int i = 0; i < blockSet.Length; i++)
            {
                if (!ShapeLibrary.TryGetShape(blockSet[i], out _))
                {
                    // BUG DETECTION: This ShapeId is not in ShapeLibrary!
                    System.Diagnostics.Debug.WriteLine($"[BlockSpawner.ValidateAndFixBlockSet] CRITICAL: Generated ShapeId {blockSet[i]} does not exist in ShapeLibrary! Replacing with Single block.");
                    blockSet[i] = ShapeLibrary.Single;
                }
            }
            
            // GUARANTEE: Always return exactly BlocksPerSet (3) elements
            if (blockSet.Length != BlocksPerSet)
            {
                System.Diagnostics.Debug.WriteLine($"[BlockSpawner.ValidateAndFixBlockSet] CRITICAL: blockSet.Length={blockSet.Length}, expected {BlocksPerSet}! Fixing...");
                var fixedSet = new ShapeId[BlocksPerSet];
                for (int i = 0; i < BlocksPerSet; i++)
                {
                    fixedSet[i] = (i < blockSet.Length) ? blockSet[i] : ShapeLibrary.Single;
                }
                return fixedSet;
            }
            
            return blockSet;
        }


        
        /// <summary>
        /// Records the outcome of a placement for difficulty adaptation.
        /// </summary>
        /// <param name="wasSuccessful">True if placement was successful</param>
        public void RecordPlacement(bool wasSuccessful)
        {
            _difficultyModel.RecordPlacement(wasSuccessful);
        }

        /// <summary>
        /// Restores difficulty-related state for continue/load scenarios.
        /// </summary>
        public void RestoreDifficultyState(float difficultyLevel, int totalPlacements, float recentSuccessRate,
            float overallSuccessRate, bool[] recentPlacementHistory)
        {
            _difficultyModel.RestoreState(difficultyLevel, totalPlacements, recentSuccessRate, overallSuccessRate, recentPlacementHistory);
        }
        
        /// <summary>
        /// Checks if a block set has at least one placeable block on the given board.
        /// </summary>
        /// <param name="blockSet">Set of shape IDs to check</param>
        /// <param name="boardState">Current board state</param>
        /// <returns>True if at least one block can be placed</returns>
        public bool IsBlockSetSafe(ShapeId[] blockSet, BoardState boardState)
        {
            foreach (var shapeId in blockSet)
            {
                if (ShapeLibrary.TryGetShape(shapeId, out var shape))
                {
                    if (PlacementSearch.HasAnyValidPlacement(boardState, shape))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool IsBlockSetHealthy(ShapeId[] blockSet, BoardState boardState)
        {
            if (!IsBlockSetSafe(blockSet, boardState))
                return false;

            if (!UseFutureSolvabilityChecks || boardState == null)
                return true;

            var shapeDefinitions = ResolveShapes(blockSet);
            if (shapeDefinitions.Count == 0)
                return true;

            var baseline = BoardHeuristics.Evaluate(boardState, shapeDefinitions);
            float bestScore = float.MinValue;
            BoardHeuristics.Snapshot bestSnapshot = default;
            bool foundPreview = false;

            for (int i = 0; i < shapeDefinitions.Count; i++)
            {
                var shape = shapeDefinitions[i];
                if (shape == null)
                    continue;

                var placements = PlacementSearch.FindValidPlacements(boardState, shape);
                for (int placementIndex = 0; placementIndex < placements.Length; placementIndex++)
                {
                    var preview = BoardHeuristics.EvaluatePlacement(boardState, shape, placements[placementIndex], shapeDefinitions);
                    if (!preview.IsValid)
                        continue;

                    if (preview.CompositeScore > bestScore)
                    {
                        bestScore = preview.CompositeScore;
                        bestSnapshot = preview.Snapshot;
                        foundPreview = true;
                    }
                }
            }

            if (!foundPreview)
                return false;

            if (bestSnapshot.FutureOpenAreaScore < MinFutureOpenAreaScore)
                return false;

            if (bestSnapshot.LargestEmptyRectangleArea < MinLargestEmptyRectangleArea && bestSnapshot.EmptyCellCount < 18)
                return false;

            if (baseline.AvailableThreeByThreeCount > 0 &&
                bestSnapshot.AvailableThreeByThreeCount <= 0 &&
                !ContainsEasyShape(blockSet) &&
                !_difficultyModel.IsPlayerStruggling)
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Resets spawner state (useful for new games).
        /// </summary>
        /// <param name="seed">Optional new seed for RNG</param>
        public void Reset(int? seed = null)
        {
            if (seed.HasValue)
                _rng.Reseed(seed.Value);
                
            _difficultyModel.Reset();
            InitializeShapeWeights();
            _recentSpawnHistory.Clear();
            _miniBag.Clear();
            _setsWithoutEasyShape = 0;
        }
        
        /// <summary>
        /// Gets statistics about spawner performance.
        /// </summary>
        /// <returns>Spawner statistics</returns>
        public SpawnerStats GetStats()
        {
            return new SpawnerStats(
                _difficultyModel.DifficultyLevel,
                _difficultyModel.RecentSuccessRate,
                _difficultyModel.OverallSuccessRate,
                _difficultyModel.TotalPlacements,
                _difficultyModel.GetRecentPlacementHistorySnapshot()
            );
        }
        
        private void InitializeShapeWeights()
        {
            _shapePicker.Clear();
            
            // Add all shapes from library with base weights
            foreach (var shapeId in ShapeLibrary.GetAllShapeIds())
            {
                var baseWeight = GetBaseWeight(shapeId);
                _shapePicker.Add(shapeId, baseWeight);
            }
        }
        
        private void UpdateShapeWeights()
        {
            _shapePicker.Clear();
            
            foreach (var shapeId in ShapeLibrary.GetAllShapeIds())
            {
                var baseWeight = GetBaseWeight(shapeId);
                var difficultyMultiplier = _difficultyModel.GetShapeWeightMultiplier(shapeId);
                var finalWeight = baseWeight * difficultyMultiplier;
                
                _shapePicker.Add(shapeId, finalWeight);
            }
        }
        
        private float GetBaseWeight(ShapeId shapeId)
        {
            // Base weights for different shape types
            // Single blocks are most common, complex shapes are rarer
            if (ShapeLibrary.TryGetShape(shapeId, out var shape))
            {
                var cellCount = shape.Offsets.Length;
                
                return cellCount switch
                {
                    1 => 10f,       // Single blocks: very common
                    2 => 8f,        // Dominoes: common
                    3 => 6f,        // Triominoes: moderate
                    4 => 4f,        // Tetrominoes: moderate
                    5 => 2f,        // Pentominoes: rare
                    _ => 1f         // Larger shapes: very rare
                };
            }
            
            return 1f; // Default weight
        }
        
        private ShapeId[] GenerateBlockSet(BoardState boardState)
        {
            var blockSet = new ShapeId[BlocksPerSet];
            var occurrencesInSet = new Dictionary<ShapeId, int>();
            EnsureMiniBagFilled();
            
            for (int i = 0; i < BlocksPerSet; i++)
            {
                var selectedShape = UseMiniBag
                    ? TakeShapeFromMiniBag(occurrencesInSet)
                    : PickShapeWithFairness(occurrencesInSet);
                blockSet[i] = selectedShape;

                if (occurrencesInSet.TryGetValue(selectedShape, out int count))
                    occurrencesInSet[selectedShape] = count + 1;
                else
                    occurrencesInSet[selectedShape] = 1;
            }

            EnsureEasyShapePresence(blockSet, boardState);
            
            // Apply extra challenge if suggested by difficulty model
            if (_difficultyModel.ShouldAddExtraChallenge() && BlocksPerSet > 1)
            {
                // Soft-challenge: chance-based replacement only when set already has some safety.
                // This avoids deterministic difficulty spikes for high-performing players.
                bool setContainsEasyShape = ContainsEasyShape(blockSet);
                float challengeChance = setContainsEasyShape ? 0.35f : 0.18f;
                if (_rng.NextBool(challengeChance))
                {
                    // Replace the last block with a more challenging one
                    blockSet[BlocksPerSet - 1] = GetChallengeShape();
                }
            }
            
            return blockSet;
        }

        private ShapeId PickShapeWithFairness(Dictionary<ShapeId, int> occurrencesInSet)
        {
            var weightedCandidates = _shapePicker.GetItems();
            if (weightedCandidates == null || weightedCandidates.Count == 0)
                return ShapeLibrary.Single;

            var fairPicker = new WeightedPicker<ShapeId>();
            ShapeId fallbackShape = weightedCandidates[0].Value;

            foreach (var candidate in weightedCandidates)
            {
                int inSetCount = 0;
                if (occurrencesInSet != null)
                    occurrencesInSet.TryGetValue(candidate.Value, out inSetCount);

                if (inSetCount >= MaxSameShapePerSet)
                    continue;

                int recentCount = GetRecentOccurrenceCount(candidate.Value);
                float recencyPenalty = 1f / (1f + (recentCount * 0.6f) + (inSetCount * 0.9f));

                // Extra penalty for immediate repeats from the previous spawn.
                if (_recentSpawnHistory.Count > 0)
                {
                    var recentArray = _recentSpawnHistory.ToArray();
                    if (recentArray[recentArray.Length - 1].Equals(candidate.Value))
                        recencyPenalty *= 0.55f;
                }

                float adjustedWeight = Math.Max(0.15f, candidate.Weight * recencyPenalty);
                if (_difficultyModel.IsPlayerStruggling && ShapeLibrary.TryGetShape(candidate.Value, out var shape))
                {
                    bool easyShape = shape.Offsets.Length <= EasyShapeMaxCellCount;
                    if (easyShape)
                        adjustedWeight *= _difficultyModel.GetFairnessBoost();
                }
                fairPicker.Add(candidate.Value, adjustedWeight);
                fallbackShape = candidate.Value;
            }

            return fairPicker.IsEmpty ? fallbackShape : fairPicker.Pick(_rng);
        }

        private void EnsureMiniBagFilled()
        {
            if (!UseMiniBag)
                return;

            int targetSize = MiniBagSize < MinMiniBagSize ? MinMiniBagSize : MiniBagSize;
            targetSize = Math.Max(targetSize, BlocksPerSet * 2);

            while (_miniBag.Count < targetSize)
            {
                _miniBag.Enqueue(PickMiniBagShape());
            }
        }

        private ShapeId TakeShapeFromMiniBag(Dictionary<ShapeId, int> occurrencesInSet)
        {
            if (_miniBag.Count == 0)
                EnsureMiniBagFilled();

            if (_miniBag.Count == 0)
                return PickShapeWithFairness(occurrencesInSet);

            int scans = _miniBag.Count;
            for (int i = 0; i < scans; i++)
            {
                var candidate = _miniBag.Dequeue();

                int inSetCount = 0;
                if (occurrencesInSet != null)
                    occurrencesInSet.TryGetValue(candidate, out inSetCount);

                if (inSetCount < MaxSameShapePerSet)
                    return candidate;

                _miniBag.Enqueue(candidate);
            }

            return _miniBag.Dequeue();
        }

        private ShapeId PickMiniBagShape()
        {
            var weightedCandidates = _shapePicker.GetItems();
            if (weightedCandidates == null || weightedCandidates.Count == 0)
                return ShapeLibrary.Single;

            var bagPicker = new WeightedPicker<ShapeId>();
            ShapeId fallbackShape = weightedCandidates[0].Value;

            var bagCounts = new Dictionary<ShapeId, int>();
            foreach (var shapeId in _miniBag)
            {
                if (bagCounts.TryGetValue(shapeId, out int count))
                    bagCounts[shapeId] = count + 1;
                else
                    bagCounts[shapeId] = 1;
            }

            ShapeId lastBagShape = default;
            bool hasLastBagShape = false;
            if (_miniBag.Count > 0)
            {
                var bagSnapshot = _miniBag.ToArray();
                lastBagShape = bagSnapshot[bagSnapshot.Length - 1];
                hasLastBagShape = true;
            }

            foreach (var candidate in weightedCandidates)
            {
                int recentCount = GetRecentOccurrenceCount(candidate.Value);
                bagCounts.TryGetValue(candidate.Value, out int bagCount);

                float recencyPenalty = 1f / (1f + (recentCount * 0.45f));
                float bagSaturationPenalty = 1f / (1f + (bagCount * 0.75f));
                float duplicatePenalty = hasLastBagShape && lastBagShape.Equals(candidate.Value) ? 0.55f : 1f;

                float adjustedWeight = candidate.Weight * recencyPenalty * bagSaturationPenalty * duplicatePenalty;
                adjustedWeight = Math.Max(0.12f, adjustedWeight);

                bagPicker.Add(candidate.Value, adjustedWeight);
                fallbackShape = candidate.Value;
            }

            return bagPicker.IsEmpty ? fallbackShape : bagPicker.Pick(_rng);
        }

        private int GetRecentOccurrenceCount(ShapeId shapeId)
        {
            int count = 0;
            foreach (var recentShape in _recentSpawnHistory)
            {
                if (recentShape.Equals(shapeId))
                    count++;
            }

            return count;
        }

        private void EnsureEasyShapePresence(ShapeId[] blockSet, BoardState boardState)
        {
            if (blockSet == null || blockSet.Length == 0)
                return;

            int easyShapeCount = CountEasyShapes(blockSet);
            int requiredEasyCount = _difficultyModel.IsPlayerStruggling ? StruggleSafeShapeMinCount : 1;
            if (easyShapeCount >= requiredEasyCount)
                return;

            // Uses finalized history counter from previous sets.
            // If too many sets in a row had no easy shapes, enforce one now.
            if (_setsWithoutEasyShape < MaxSetsWithoutEasyShape && !_difficultyModel.IsPlayerStruggling)
                return;

            var easyCandidates = new List<ShapeId>();
            foreach (var shapeId in ShapeLibrary.GetAllShapeIds())
            {
                if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                    continue;

                if (shape.Offsets.Length > EasyShapeMaxCellCount)
                    continue;

                if (boardState != null && !PlacementSearch.HasAnyValidPlacement(boardState, shape))
                    continue;

                easyCandidates.Add(shapeId);
            }

            if (easyCandidates.Count == 0)
                return;

            int missingEasyShapes = requiredEasyCount - easyShapeCount;
            if (missingEasyShapes <= 0)
                return;

            for (int i = 0; i < missingEasyShapes; i++)
            {
                int replacementIndex = blockSet.Length - 1 - i;
                if (replacementIndex < 0)
                    break;

                blockSet[replacementIndex] = easyCandidates[_rng.Next(easyCandidates.Count)];
            }
        }

        private bool ContainsEasyShape(ShapeId[] blockSet)
        {
            if (blockSet == null)
                return false;

            for (int i = 0; i < blockSet.Length; i++)
            {
                if (!ShapeLibrary.TryGetShape(blockSet[i], out var shape))
                    continue;

                if (shape.Offsets.Length <= EasyShapeMaxCellCount)
                    return true;
            }

            return false;
        }

        private List<ShapeDefinition> ResolveShapes(ShapeId[] blockSet)
        {
            var shapes = new List<ShapeDefinition>(blockSet?.Length ?? 0);
            if (blockSet == null)
                return shapes;

            for (int i = 0; i < blockSet.Length; i++)
            {
                if (ShapeLibrary.TryGetShape(blockSet[i], out var shape))
                    shapes.Add(shape);
            }

            return shapes;
        }

        private int CountEasyShapes(ShapeId[] blockSet)
        {
            if (blockSet == null)
                return 0;

            int count = 0;
            for (int i = 0; i < blockSet.Length; i++)
            {
                if (!ShapeLibrary.TryGetShape(blockSet[i], out var shape))
                    continue;
                if (shape.Offsets.Length <= EasyShapeMaxCellCount)
                    count++;
            }

            return count;
        }

        private void TrackSpawnHistory(ShapeId[] blockSet)
        {
            if (blockSet == null)
                return;

            if (ContainsEasyShape(blockSet))
                _setsWithoutEasyShape = 0;
            else
                _setsWithoutEasyShape++;

            foreach (var shapeId in blockSet)
            {
                _recentSpawnHistory.Enqueue(shapeId);
                while (_recentSpawnHistory.Count > RecentHistorySize)
                    _recentSpawnHistory.Dequeue();
            }
        }
        
        private ShapeId GetChallengeShape()
        {
            // Create temporary picker with only complex shapes
            var challengePicker = new WeightedPicker<ShapeId>();
            
            foreach (var shapeId in ShapeLibrary.GetAllShapeIds())
            {
                if (ShapeLibrary.TryGetShape(shapeId, out var shape) && shape.Offsets.Length >= 4)
                {
                    challengePicker.Add(shapeId, 1f);
                }
            }
            
            return challengePicker.IsEmpty ? ShapeLibrary.GetAllShapeIds()[0] : challengePicker.Pick(_rng);
        }
        
        private ShapeId[] GenerateSafeBlockSet(BoardState boardState)
        {
            var safeBlocks = new List<ShapeId>();
            
            // Find shapes that can be placed on the current board
            foreach (var shapeId in ShapeLibrary.GetAllShapeIds())
            {
                if (ShapeLibrary.TryGetShape(shapeId, out var shape))
                {
                    if (PlacementSearch.HasAnyValidPlacement(boardState, shape))
                    {
                        safeBlocks.Add(shapeId);
                    }
                }
            }
            
            if (safeBlocks.Count == 0)
            {
                // Board is full or nearly full - return single blocks
                return new[] { ShapeLibrary.Single, ShapeLibrary.Single, ShapeLibrary.Single };
            }
            
            // Generate set with at least one safe block
            var blockSet = new ShapeId[BlocksPerSet];
            blockSet[0] = safeBlocks[_rng.Next(safeBlocks.Count)]; // Guaranteed safe
            
            for (int i = 1; i < BlocksPerSet; i++)
            {
                // Mix of safe and random blocks
                if (_rng.NextBool(0.7f) && safeBlocks.Count > 0)
                {
                    blockSet[i] = safeBlocks[_rng.Next(safeBlocks.Count)];
                }
                else
                {
                    blockSet[i] = _shapePicker.Pick(_rng);
                }
            }
            
            return blockSet;
        }


    }
    
    /// <summary>
    /// Statistics about spawner performance and state.
    /// </summary>
    public readonly struct SpawnerStats
    {
        public readonly float DifficultyLevel;
        public readonly float RecentSuccessRate;
        public readonly float OverallSuccessRate;
        public readonly int TotalPlacements;
        public readonly bool[] RecentPlacementHistory;
        
        public SpawnerStats(float difficultyLevel, float recentSuccessRate, 
            float overallSuccessRate, int totalPlacements, bool[] recentPlacementHistory = null)
        {
            DifficultyLevel = difficultyLevel;
            RecentSuccessRate = recentSuccessRate;
            OverallSuccessRate = overallSuccessRate;
            TotalPlacements = totalPlacements;
            RecentPlacementHistory = recentPlacementHistory == null ? null : (bool[])recentPlacementHistory.Clone();
        }
        
        public override string ToString()
        {
            return $"Difficulty: {DifficultyLevel:F2}, Success Rate: {RecentSuccessRate:F2} " +
                   $"(Overall: {OverallSuccessRate:F2}), Placements: {TotalPlacements}";
        }
    }
}
