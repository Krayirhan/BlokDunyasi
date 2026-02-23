// File: UnityAdapter/UnityPlayerPrefsDataProvider.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using BlockPuzzle.Core.Persistence;

namespace BlockPuzzle.UnityAdapter
{
    /// <summary>
    /// Unity PlayerPrefs implementation of IGameDataProvider.
    /// Preferred Unity entrypoint; wraps GameStateStore/SettingsStore/StatisticsStore.
    /// </summary>
    public class UnityPlayerPrefsDataProvider : IGameDataProvider
    {
        private const string GAME_DATA_PREFIX = "BlockPuzzle_GameData_";
        private readonly GameStateStore _gameStateStore;
        private readonly SettingsStore _settingsStore;
        private readonly StatisticsStore _statisticsStore;

        public UnityPlayerPrefsDataProvider()
        {
            var storage = new PlayerPrefsStorage();
            var serializer = new UnityJsonSerializer();
            _gameStateStore = new GameStateStore(storage, serializer);
            _settingsStore = new SettingsStore(storage, serializer);
            _statisticsStore = new StatisticsStore(storage, serializer);
        }
        
        /// <summary>
        /// Saves game state data to PlayerPrefs.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <param name="data">Game data to save</param>
        /// <returns>Completed task</returns>
        public Task SaveGameDataAsync(string key, GameData data)
        {
            try
            {
                var fullKey = GAME_DATA_PREFIX + key;
                _gameStateStore.SaveGame(data, fullKey);
                Debug.Log($"[UnityPlayerPrefsDataProvider] Saved game data for key: {key}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to save game data: {e.Message}");
                throw;
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Loads game state data from PlayerPrefs.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>Loaded game data, or null if not found</returns>
        public Task<GameData> LoadGameDataAsync(string key)
        {
            try
            {
                var fullKey = GAME_DATA_PREFIX + key;
                if (!_gameStateStore.HasSavedGame(fullKey))
                    return Task.FromResult<GameData>(null);

                var data = _gameStateStore.LoadGame(fullKey);
                Debug.Log($"[UnityPlayerPrefsDataProvider] Loaded game data for key: {key}");
                return Task.FromResult(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to load game data: {e.Message}");
                return Task.FromResult<GameData>(null);
            }
        }
        
        /// <summary>
        /// Checks if game data exists for the given key.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>True if data exists</returns>
        public Task<bool> HasGameDataAsync(string key)
        {
            var fullKey = GAME_DATA_PREFIX + key;
            var hasKey = _gameStateStore.HasSavedGame(fullKey);
            Debug.Log($"[UnityPlayerPrefsDataProvider] Has game data for key {key}: {hasKey}");
            return Task.FromResult(hasKey);
        }
        
        /// <summary>
        /// Deletes game data for the given key.
        /// </summary>
        /// <param name="key">Storage key identifier</param>
        /// <returns>Completed task</returns>
        public Task DeleteGameDataAsync(string key)
        {
            try
            {
                var fullKey = GAME_DATA_PREFIX + key;
                _gameStateStore.ClearSavedGame(fullKey);
                Debug.Log($"[UnityPlayerPrefsDataProvider] Deleted game data for key: {key}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to delete game data: {e.Message}");
                throw;
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Saves game settings to PlayerPrefs.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>Completed task</returns>
        public Task SaveSettingsAsync(GameSettings settings)
        {
            try
            {
                settings.Validate(); // Ensure settings are in valid ranges
                _settingsStore.SaveSettings(settings);
                Debug.Log("[UnityPlayerPrefsDataProvider] Saved game settings");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to save settings: {e.Message}");
                throw;
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Loads game settings from PlayerPrefs.
        /// </summary>
        /// <returns>Loaded settings, or default if not found</returns>
        public Task<GameSettings> LoadSettingsAsync()
        {
            try
            {
                var settings = _settingsStore.LoadSettings();
                settings.Validate(); // Ensure loaded settings are valid

                Debug.Log("[UnityPlayerPrefsDataProvider] Loaded game settings");
                return Task.FromResult(settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to load settings: {e.Message}");
                return Task.FromResult(GameSettings.CreateDefault());
            }
        }
        
        /// <summary>
        /// Saves game statistics to PlayerPrefs.
        /// </summary>
        /// <param name="statistics">Statistics to save</param>
        /// <returns>Completed task</returns>
        public Task SaveStatisticsAsync(GameStatistics statistics)
        {
            try
            {
                _statisticsStore.SaveStatistics(statistics);
                Debug.Log("[UnityPlayerPrefsDataProvider] Saved game statistics");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to save statistics: {e.Message}");
                throw;
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Loads game statistics from PlayerPrefs.
        /// </summary>
        /// <returns>Loaded statistics, or default if not found</returns>
        public Task<GameStatistics> LoadStatisticsAsync()
        {
            try
            {
                var statistics = _statisticsStore.LoadStatistics();
                Debug.Log("[UnityPlayerPrefsDataProvider] Loaded game statistics");
                return Task.FromResult(statistics);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityPlayerPrefsDataProvider] Failed to load statistics: {e.Message}");
                return Task.FromResult(GameStatistics.CreateDefault());
            }
        }
    }
}
