// File: Core/Persistence/IGameDataProvider.cs
using System;
using System.Threading.Tasks;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Interface for persisting and loading game data.
    /// Abstracts away storage implementation (PlayerPrefs, files, cloud, etc.).
    /// </summary>
    public interface IGameDataProvider
    {
        /// <summary>
        /// Saves game state data asynchronously.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <param name="data">Game data to save</param>
        /// <returns>Task that completes when save is done</returns>
        Task SaveGameDataAsync(string key, GameData data);
        
        /// <summary>
        /// Loads game state data asynchronously.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>Loaded game data, or null if not found</returns>
        Task<GameData> LoadGameDataAsync(string key);
        
        /// <summary>
        /// Checks if game data exists for the given key.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>True if data exists</returns>
        Task<bool> HasGameDataAsync(string key);
        
        /// <summary>
        /// Deletes game data for the given key.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>Task that completes when deletion is done</returns>
        Task DeleteGameDataAsync(string key);
        
        /// <summary>
        /// Saves game settings/preferences.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>Task that completes when save is done</returns>
        Task SaveSettingsAsync(GameSettings settings);
        
        /// <summary>
        /// Loads game settings/preferences.
        /// </summary>
        /// <returns>Loaded settings, or default if not found</returns>
        Task<GameSettings> LoadSettingsAsync();
        
        /// <summary>
        /// Saves high scores and statistics.
        /// </summary>
        /// <param name="statistics">Statistics to save</param>
        /// <returns>Task that completes when save is done</returns>
        Task SaveStatisticsAsync(GameStatistics statistics);
        
        /// <summary>
        /// Loads high scores and statistics.
        /// </summary>
        /// <returns>Loaded statistics, or default if not found</returns>
        Task<GameStatistics> LoadStatisticsAsync();
    }
}