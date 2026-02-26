// File: Core/RNG/BlockSpawner.cs
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
        private readonly SeededRng _rng;
        private readonly DifficultyModel _difficultyModel;
        private readonly WeightedPicker<ShapeId> _shapePicker;
        
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
        /// Current difficulty model for adaptive spawning.
        /// </summary>
        public DifficultyModel DifficultyModel => _difficultyModel;
        
        public BlockSpawner(SeededRng rng, DifficultyModel difficultyModel = null)
        {
            _rng = rng;
            _difficultyModel = difficultyModel ?? new DifficultyModel();
            _shapePicker = new WeightedPicker<ShapeId>();
            
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
                var blockSet = GenerateBlockSet();
                
                // CRITICAL FIX: Validate all ShapeIds exist in ShapeLibrary before returning
                blockSet = ValidateAndFixBlockSet(blockSet);
                
                if (!UseSafetyChecks || IsBlockSetSafe(blockSet, boardState))
                {
                    return blockSet;
                }
            }
            
            // Fallback: generate a guaranteed safe set
            var safeSet = GenerateSafeBlockSet(boardState);
            return ValidateAndFixBlockSet(safeSet);
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
        
        private ShapeId[] GenerateBlockSet()
        {
            var blockSet = new ShapeId[BlocksPerSet];
            
            for (int i = 0; i < BlocksPerSet; i++)
            {
                blockSet[i] = _shapePicker.Pick(_rng);
            }
            
            // Apply extra challenge if suggested by difficulty model
            if (_difficultyModel.ShouldAddExtraChallenge() && BlocksPerSet > 1)
            {
                // Replace the last block with a more challenging one
                blockSet[BlocksPerSet - 1] = GetChallengeShape();
            }
            
            return blockSet;
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
