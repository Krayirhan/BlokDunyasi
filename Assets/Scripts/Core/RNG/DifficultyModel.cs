// File: Core/RNG/DifficultyModel.cs
using System;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.Core.RNG
{
    /// <summary>
    /// Adaptive difficulty model that adjusts shape complexity based on player performance.
    /// Tracks metrics and modifies spawning weights to maintain engagement.
    /// </summary>
    public class DifficultyModel
    {
        /// <summary>
        /// Current difficulty level in [0, 1] where 0 = easiest, 1 = hardest.
        /// </summary>
        public float DifficultyLevel { get; private set; }
        
        /// <summary>
        /// Target success rate to maintain (0.6 = 60% successful placements).
        /// </summary>
        public float TargetSuccessRate { get; set; } = 0.6f;
        
        /// <summary>
        /// How quickly difficulty adjusts (0.1 = slow, 0.5 = fast).
        /// </summary>
        public float AdaptationRate { get; set; } = 0.2f;
        
        /// <summary>
        /// Minimum difficulty level (prevents game from becoming too easy).
        /// </summary>
        public float MinDifficulty { get; set; } = 0.1f;
        
        /// <summary>
        /// Maximum difficulty level (prevents game from becoming impossible).
        /// </summary>
        public float MaxDifficulty { get; set; } = 0.9f;
        
        // Performance tracking
        private int _totalPlacements;
        private int _successfulPlacements;
        private float _recentSuccessRate;
        private readonly CircularBuffer<bool> _recentPlacementHistory;
        
        /// <summary>
        /// Current success rate based on recent placement history.
        /// </summary>
        public float RecentSuccessRate => _recentSuccessRate;
        
        /// <summary>
        /// Total placements tracked by this model.
        /// </summary>
        public int TotalPlacements => _totalPlacements;
        
        /// <summary>
        /// Overall success rate across all tracked placements.
        /// </summary>
        public float OverallSuccessRate => _totalPlacements > 0 ? (float)_successfulPlacements / _totalPlacements : 0f;
        
        public DifficultyModel(float initialDifficulty = 0.3f, int historySize = 20)
        {
            DifficultyLevel = ClampDifficulty(initialDifficulty);
            _recentPlacementHistory = new CircularBuffer<bool>(historySize);
            _totalPlacements = 0;
            _successfulPlacements = 0;
            _recentSuccessRate = 0f;
        }
        
        /// <summary>
        /// Records the outcome of a placement attempt and updates difficulty.
        /// </summary>
        /// <param name="wasSuccessful">True if placement was successful</param>
        public void RecordPlacement(bool wasSuccessful)
        {
            _totalPlacements++;
            if (wasSuccessful)
                _successfulPlacements++;
                
            _recentPlacementHistory.Add(wasSuccessful);
            UpdateRecentSuccessRate();
            UpdateDifficulty();
        }
        
        /// <summary>
        /// Gets the weight multiplier for a shape based on current difficulty.
        /// Complex shapes get higher weights at higher difficulty levels.
        /// </summary>
        /// <param name="shapeId">Shape to get weight for</param>
        /// <returns>Weight multiplier in [0.1, 3.0]</returns>
        public float GetShapeWeightMultiplier(ShapeId shapeId)
        {
            var complexity = CalculateShapeComplexity(shapeId);
            
            // At low difficulty: favor simple shapes
            // At high difficulty: favor complex shapes
            var baseWeight = 1.0f;
            var complexityBonus = (complexity - 0.5f) * DifficultyLevel * 2.0f; // Range: -1.0 to +1.0
            
            var finalWeight = baseWeight + complexityBonus;
            return ClampFloat(finalWeight, 0.1f, 3.0f);
        }
        
        /// <summary>
        /// Suggests whether to add an extra challenging shape to the current spawn set.
        /// </summary>
        /// <returns>True if an extra challenge is recommended</returns>
        public bool ShouldAddExtraChallenge()
        {
            // Add challenges when player is performing well above target
            return _recentSuccessRate > TargetSuccessRate + 0.15f && DifficultyLevel < MaxDifficulty;
        }
        
        /// <summary>
        /// Resets difficulty to initial state (useful for new games).
        /// </summary>
        /// <param name="initialDifficulty">New initial difficulty</param>
        public void Reset(float initialDifficulty = 0.3f)
        {
            DifficultyLevel = ClampDifficulty(initialDifficulty);
            _totalPlacements = 0;
            _successfulPlacements = 0;
            _recentSuccessRate = 0f;
            _recentPlacementHistory.Clear();
        }

        /// <summary>
        /// Restores difficulty model state from persisted values.
        /// </summary>
        public void RestoreState(float difficultyLevel, int totalPlacements, float recentSuccessRate,
            float overallSuccessRate, bool[] recentPlacementHistory)
        {
            DifficultyLevel = ClampDifficulty(difficultyLevel);
            _totalPlacements = totalPlacements < 0 ? 0 : totalPlacements;
            _successfulPlacements = _totalPlacements > 0
                ? (int)Math.Round(overallSuccessRate * _totalPlacements)
                : 0;

            _recentPlacementHistory.Clear();
            if (recentPlacementHistory != null)
            {
                foreach (var success in recentPlacementHistory)
                {
                    _recentPlacementHistory.Add(success);
                }
            }

            if (_recentPlacementHistory.Count > 0)
                UpdateRecentSuccessRate();
            else
                _recentSuccessRate = ClampFloat(recentSuccessRate, 0f, 1f);
        }
        
        /// <summary>
        /// Creates a copy of this difficulty model for simulation purposes.
        /// </summary>
        /// <returns>New DifficultyModel with same state</returns>
        public DifficultyModel Clone()
        {
            var clone = new DifficultyModel(DifficultyLevel, _recentPlacementHistory.Capacity)
            {
                TargetSuccessRate = TargetSuccessRate,
                AdaptationRate = AdaptationRate,
                MinDifficulty = MinDifficulty,
                MaxDifficulty = MaxDifficulty,
                _totalPlacements = _totalPlacements,
                _successfulPlacements = _successfulPlacements,
                _recentSuccessRate = _recentSuccessRate
            };
            
            // Copy recent history
            foreach (var placement in _recentPlacementHistory)
            {
                clone._recentPlacementHistory.Add(placement);
            }
            
            return clone;
        }
        
        private void UpdateRecentSuccessRate()
        {
            if (_recentPlacementHistory.Count == 0)
            {
                _recentSuccessRate = 0f;
                return;
            }
            
            var successes = 0;
            foreach (var success in _recentPlacementHistory)
            {
                if (success) successes++;
            }
            
            _recentSuccessRate = (float)successes / _recentPlacementHistory.Count;
        }
        
        private void UpdateDifficulty()
        {
            if (_recentPlacementHistory.Count < 5) // Need some data before adjusting
                return;
                
            var successDelta = _recentSuccessRate - TargetSuccessRate;
            var adjustment = successDelta * AdaptationRate;
            
            DifficultyLevel = ClampDifficulty(DifficultyLevel + adjustment);
        }
        
        private float CalculateShapeComplexity(ShapeId shapeId)
        {
            // Get shape definition to calculate complexity
            if (!ShapeLibrary.TryGetShape(shapeId, out var shape))
                return 0.5f; // Default complexity for unknown shapes
            
            var cellCount = shape.Offsets.Length;
            var boundingArea = CalculateBoundingArea(shape);
            
            // Complexity factors:
            // 1. More cells = more complex
            // 2. Lower density (cells/bounding area) = more complex (irregular shapes)
            // 3. Certain shapes are inherently complex
            
            var cellComplexity = ClampFloat((cellCount - 1f) / 8f, 0f, 1f); // 1-9 cells mapped to 0-1
            var densityComplexity = cellCount > 1 ? ClampFloat(1f - (float)cellCount / boundingArea, 0f, 1f) : 0f;
            
            // Weighted average
            var complexity = (cellComplexity * 0.7f) + (densityComplexity * 0.3f);
            return ClampFloat(complexity, 0f, 1f);
        }
        
        private int CalculateBoundingArea(ShapeDefinition shape)
        {
            if (shape.Offsets.Length == 0)
                return 1;
                
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            foreach (var offset in shape.Offsets)
            {
                minX = System.Math.Min(minX, offset.X);
                maxX = System.Math.Max(maxX, offset.X);
                minY = System.Math.Min(minY, offset.Y);
                maxY = System.Math.Max(maxY, offset.Y);
            }
            
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            return width * height;
        }
        
        private float ClampDifficulty(float difficulty)
        {
            return ClampFloat(difficulty, MinDifficulty, MaxDifficulty);
        }
        
        private static float ClampFloat(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
    
    /// <summary>
    /// Simple circular buffer for tracking recent game events.
    /// </summary>
    /// <typeparam name="T">Type of items to store</typeparam>
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;
        
        public int Capacity => _buffer.Length;
        public int Count => _count;
        
        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
            _count = 0;
        }
        
        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            
            if (_count < _buffer.Length)
                _count++;
        }
        
        public void Clear()
        {
            _count = 0;
            _head = 0;
            System.Array.Clear(_buffer, 0, _buffer.Length);
        }
        
        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                var index = (_head - _count + i + _buffer.Length) % _buffer.Length;
                yield return _buffer[index];
            }
        }
    }
}
