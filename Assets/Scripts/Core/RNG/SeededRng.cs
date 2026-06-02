namespace BlockPuzzle.Core.RNG
{
    /// <summary>
    /// Seeded random number generator for deterministic, reproducible randomness.
    /// Uses a cloneable SplitMix64 state so branching and replay logic can continue
    /// from the exact current RNG position.
    /// </summary>
    public class SeededRng
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;

        private ulong _initialState;
        private ulong _state;

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
            _initialState = NormalizeSeed(seed);
            _state = _initialState;
        }

        /// <summary>
        /// Creates a new SeededRng with a time-based seed.
        /// </summary>
        public SeededRng() : this(System.Environment.TickCount)
        {
        }

        private SeededRng(int seed, ulong initialState, ulong currentState)
        {
            Seed = seed;
            _initialState = initialState;
            _state = currentState;
        }

        /// <summary>
        /// Returns a random non-negative integer.
        /// </summary>
        public int Next()
        {
            return (int)(NextUInt32() & 0x7FFFFFFF);
        }

        /// <summary>
        /// Returns a random integer in [0, maxValue).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random int in [0, maxValue)</returns>
        public int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be non-negative.");

            if (maxValue == 0)
                return 0;

            return (int)NextUInt64Bounded((ulong)maxValue);
        }

        /// <summary>
        /// Returns a random integer in [minValue, maxValue).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound</param>
        /// <param name="maxValue">Exclusive upper bound</param>
        /// <returns>Random int in [minValue, maxValue)</returns>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new System.ArgumentOutOfRangeException(nameof(minValue), "minValue cannot be greater than maxValue.");

            if (minValue == maxValue)
                return minValue;

            ulong range = (ulong)((long)maxValue - minValue);
            return minValue + (int)NextUInt64Bounded(range);
        }

        /// <summary>
        /// Returns a random float in [0, 1).
        /// </summary>
        /// <returns>Random float in [0, 1)</returns>
        public float NextFloat()
        {
            return (float)NextDouble();
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
        /// Creates a new RNG that continues from this generator's current internal state.
        /// Advancing the clone does not mutate the original, and vice versa.
        /// </summary>
        /// <returns>New SeededRng with the same current internal state</returns>
        public SeededRng Clone()
        {
            return new SeededRng(Seed, _initialState, _state);
        }

        /// <summary>
        /// Re-seeds the RNG with a new seed value.
        /// </summary>
        /// <param name="newSeed">New seed value</param>
        public void Reseed(int newSeed)
        {
            Seed = newSeed;
            _initialState = NormalizeSeed(newSeed);
            _state = _initialState;
        }

        private static ulong NormalizeSeed(int seed)
        {
            ulong mixed = (uint)seed;
            mixed ^= 0xA0761D6478BD642FUL;
            mixed += 0xE7037ED1A0B428DBUL;
            return mixed;
        }

        private uint NextUInt32()
        {
            return (uint)(NextUInt64() >> 32);
        }

        private ulong NextUInt64()
        {
            _state += Gamma;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private ulong NextUInt64Bounded(ulong exclusiveMax)
        {
            if (exclusiveMax == 0UL)
                return 0UL;

            ulong threshold = unchecked((0UL - exclusiveMax) % exclusiveMax);
            while (true)
            {
                ulong candidate = NextUInt64();
                if (candidate >= threshold)
                    return candidate % exclusiveMax;
            }
        }

        private double NextDouble()
        {
            const double scale = 1.0 / (1UL << 53);
            return ((NextUInt64() >> 11) * scale);
        }
    }
}
