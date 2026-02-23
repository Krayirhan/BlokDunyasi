// File: Core/RNG/WeightedPicker.cs
using System;
using System.Collections.Generic;

namespace BlockPuzzle.Core.RNG
{
    /// <summary>
    /// Generic weighted selection utility using cumulative distribution.
    /// Thread-safe for read operations, not thread-safe for modifications.
    /// </summary>
    /// <typeparam name="T">Type of items to select from</typeparam>
    public class WeightedPicker<T>
    {
        private readonly List<WeightedItem<T>> _items;
        private float _totalWeight;
        
        /// <summary>
        /// Number of items in the picker.
        /// </summary>
        public int Count => _items.Count;
        
        /// <summary>
        /// Total weight of all items.
        /// </summary>
        public float TotalWeight => _totalWeight;
        
        /// <summary>
        /// True if picker has no items.
        /// </summary>
        public bool IsEmpty => _items.Count == 0;
        
        public WeightedPicker()
        {
            _items = new List<WeightedItem<T>>();
            _totalWeight = 0f;
        }
        
        /// <summary>
        /// Adds an item with the specified weight.
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="weight">Weight (must be positive)</param>
        public void Add(T item, float weight)
        {
            if (weight <= 0f)
                throw new ArgumentException("Weight must be positive", nameof(weight));
                
            _items.Add(new WeightedItem<T>(item, weight));
            _totalWeight += weight;
        }
        
        /// <summary>
        /// Adds multiple items with equal weights.
        /// </summary>
        /// <param name="items">Items to add</param>
        /// <param name="weight">Weight for each item</param>
        public void AddRange(IEnumerable<T> items, float weight = 1f)
        {
            foreach (var item in items)
            {
                Add(item, weight);
            }
        }
        
        /// <summary>
        /// Removes all items from the picker.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _totalWeight = 0f;
        }
        
        /// <summary>
        /// Picks a random item based on weights.
        /// </summary>
        /// <param name="rng">Random number generator</param>
        /// <returns>Selected item</returns>
        /// <exception cref="InvalidOperationException">If picker is empty</exception>
        public T Pick(SeededRng rng)
        {
            if (IsEmpty)
                throw new InvalidOperationException("Cannot pick from empty WeightedPicker");
                
            var target = rng.NextFloat(_totalWeight);
            var cumulative = 0f;
            
            // Linear search through cumulative weights
            foreach (var item in _items)
            {
                cumulative += item.Weight;
                if (target < cumulative)
                    return item.Value;
            }
            
            // Fallback to last item (handles floating-point precision issues)
            return _items[_items.Count - 1].Value;
        }
        
        /// <summary>
        /// Picks multiple items without replacement.
        /// </summary>
        /// <param name="rng">Random number generator</param>
        /// <param name="count">Number of items to pick</param>
        /// <returns>Array of selected items</returns>
        public T[] PickMultiple(SeededRng rng, int count)
        {
            if (count <= 0)
                return new T[0];
                
            if (count >= _items.Count)
            {
                // Return all items in random order
                var result = new T[_items.Count];
                var indices = new int[_items.Count];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = i;
                    
                // Fisher-Yates shuffle
                for (int i = indices.Length - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }
                
                for (int i = 0; i < result.Length; i++)
                    result[i] = _items[indices[i]].Value;
                    
                return result;
            }
            
            // Create temporary picker and remove selected items
            var tempPicker = new WeightedPicker<T>();
            foreach (var item in _items)
            {
                tempPicker.Add(item.Value, item.Weight);
            }
            
            var selected = new T[count];
            for (int i = 0; i < count; i++)
            {
                selected[i] = tempPicker.Pick(rng);
                tempPicker.Remove(selected[i]);
            }
            
            return selected;
        }
        
        /// <summary>
        /// Removes the first occurrence of an item.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>True if item was found and removed</returns>
        public bool Remove(T item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_items[i].Value, item))
                {
                    _totalWeight -= _items[i].Weight;
                    _items.RemoveAt(i);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all items with their weights.
        /// </summary>
        /// <returns>Read-only collection of weighted items</returns>
        public IReadOnlyList<WeightedItem<T>> GetItems()
        {
            return _items.AsReadOnly();
        }
        
        /// <summary>
        /// Creates a copy of this picker.
        /// </summary>
        /// <returns>New picker with same items and weights</returns>
        public WeightedPicker<T> Clone()
        {
            var clone = new WeightedPicker<T>();
            foreach (var item in _items)
            {
                clone.Add(item.Value, item.Weight);
            }
            return clone;
        }
    }
    
    /// <summary>
    /// Represents an item with an associated weight.
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    public readonly struct WeightedItem<T>
    {
        public readonly T Value;
        public readonly float Weight;
        
        public WeightedItem(T value, float weight)
        {
            Value = value;
            Weight = weight;
        }
        
        public override string ToString()
        {
            return $"{Value} (weight: {Weight})";
        }
    }
}