// File: Core/Engine/GameConfig.cs
namespace BlockPuzzle.Core.Engine
{
    /// <summary>
    /// Configuration for game engine initialization.
    /// </summary>
    public class GameConfig
    {
        /// <summary>
        /// Width of the game board.
        /// </summary>
        public int BoardWidth { get; set; } = 8;
        
        /// <summary>
        /// Height of the game board.
        /// </summary>
        public int BoardHeight { get; set; } = 8;
        
        /// <summary>
        /// Number of blocks to spawn each round.
        /// </summary>
        public int BlocksPerSpawn { get; set; } = 3;
        
        /// <summary>
        /// Random seed for deterministic gameplay (-1 for random).
        /// </summary>
        public int RandomSeed { get; set; } = -1;
        
        /// <summary>
        /// Creates a default configuration.
        /// </summary>
        public static GameConfig Default => new GameConfig();
    }
}
