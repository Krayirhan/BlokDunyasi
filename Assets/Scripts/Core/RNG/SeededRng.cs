// File: Core/RNG/SeededRng.cs
namespace BlockPuzzle.Core.RNG
{
    /// <summary>
    /// Seeded random number generator for deterministic, reproducible randomness.
    /// Uses System.Random internally but provides a clean interface for game logic.
    /// </summary>
    public class SeededRng
    {
        private System.Random _random;
        
        /// <summary>
        /// Current seed value (for serialization/reproduction).
        /// </summary>
        public int Seed { get; private set; }
        
        /// <summary>
        /// Creates a new SeededRng with the given seed.
        /// </summary>
        /// <param name="seed">Seed for reproducible randomness</param>
        public SeededRng(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }
        
        /// <summary>
        /// Creates a new SeededRng with a time-based seed.
        /// </summary>
        public SeededRng() : this(System.Environment.TickCount)
        {
        }
        
        /// <summary>
        /// Returns a random integer in [0, maxValue).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random int in [0, maxValue)</returns>
        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }
        
        /// <summary>
        /// Returns a random integer in [minValue, maxValue).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound</param>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random int in [minValue, maxValue)</returns>
        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }
        
        /// <summary>
        /// Returns a random float in [0, 1).
        /// </summary>
        /// <returns>Random float in [0, 1)</returns>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }
        
        /// <summary>
        /// Returns a random float in [0, maxValue).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random float in [0, maxValue)</returns>
        public float NextFloat(float maxValue)
        {
            return NextFloat() * maxValue;
        }
        
        /// <summary>
        /// Returns a random float in [minValue, maxValue).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound</param>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random float in [minValue, maxValue)</returns>
        public float NextFloat(float minValue, float maxValue)
        {
            return minValue + NextFloat() * (maxValue - minValue);
        }
        
        /// <summary>
        /// Returns true with the given probability.
        /// </summary>
        /// <param name="probability">Probability in [0, 1]</param>
        /// <returns>True with given probability</returns>
        public bool NextBool(float probability = 0.5f)
        {
            return NextFloat() < probability;
        }
        
        /// <summary>
        /// Creates a clone with the same state for branching RNG.
        /// Useful for "what if" scenarios without affecting main RNG.
        /// </summary>
        /// <returns>New SeededRng with same internal state</returns>
        public SeededRng Clone()
        {
            // Note: System.Random doesn't provide state cloning.
            // This creates a new RNG from the same seed, which will 
            // have different state. For true state cloning, we'd need
            // a custom RNG implementation.
            return new SeededRng(Seed);
        }
        
        /// <summary>
        /// Re-seeds the RNG with a new seed value.
        /// </summary>
        /// <param name="newSeed">New seed value</param>
        public void Reseed(int newSeed)
        {
            Seed = newSeed;
            _random = new System.Random(newSeed);
        }
    }
}
