using System;
using System.Threading.Tasks;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Boot
{
    internal sealed class GameSaveManager
    {
        private readonly IGameStatePersistence _gameStatePersistence;
        private readonly string _saveKey;

        public GameSaveManager(IGameStatePersistence gameStatePersistence, string saveKey)
        {
            _gameStatePersistence = gameStatePersistence;
            _saveKey = saveKey;
        }

        public async Task<bool> TryLoadSavedGameAsync(Func<GameData, bool> applyLoadedGameData)
        {
            if (_gameStatePersistence == null || applyLoadedGameData == null)
                return false;

            GameData data;
            try
            {
                data = await _gameStatePersistence.LoadGameDataAsync(_saveKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Failed to load saved game: {ex.Message}");
                return false;
            }

            if (data == null)
                return false;

            if (data.IsGameOver)
            {
                await ClearSavedGameAsync();
                return false;
            }

            if (!applyLoadedGameData(data))
            {
                await ClearSavedGameAsync();
                return false;
            }

            return true;
        }

        public async Task SaveGameIfNeededAsync(GameEngine gameEngine, GameState currentGameState, int currentSeed, int scoreFormulaVersion)
        {
            if (_gameStatePersistence == null || gameEngine == null || currentGameState == null)
                return;

            if (currentGameState.IsGameOver || gameEngine.IsGameOver())
                return;

            try
            {
                var stats = gameEngine.BlockSpawner.GetStats();
                var data = GameData.FromGameState(currentGameState, stats, currentSeed, scoreFormulaVersion);
                await _gameStatePersistence.SaveGameDataAsync(_saveKey, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Failed to save game: {ex.Message}");
            }
        }

        public async Task ClearSavedGameAsync()
        {
            if (_gameStatePersistence == null)
                return;

            try
            {
                await _gameStatePersistence.DeleteGameDataAsync(_saveKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Failed to clear saved game: {ex.Message}");
            }
        }
    }
}
