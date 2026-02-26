using System;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using UnityEngine;

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

        public bool TryLoadSavedGame(Func<GameData, bool> applyLoadedGameData)
        {
            if (_gameStatePersistence == null || applyLoadedGameData == null)
                return false;

            GameData data;
            try
            {
                data = _gameStatePersistence.LoadGameDataAsync(_saveKey).GetAwaiter().GetResult();
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
                ClearSavedGame();
                return false;
            }

            if (!applyLoadedGameData(data))
            {
                ClearSavedGame();
                return false;
            }

            return true;
        }

        public void SaveGameIfNeeded(GameEngine gameEngine, GameState currentGameState, int currentSeed, int scoreFormulaVersion)
        {
            if (_gameStatePersistence == null || gameEngine == null || currentGameState == null)
                return;

            if (currentGameState.IsGameOver || gameEngine.IsGameOver())
                return;

            try
            {
                var stats = gameEngine.BlockSpawner.GetStats();
                var data = GameData.FromGameState(currentGameState, stats, currentSeed, scoreFormulaVersion);
                _gameStatePersistence.SaveGameDataAsync(_saveKey, data).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Failed to save game: {ex.Message}");
            }
        }

        public void ClearSavedGame()
        {
            if (_gameStatePersistence == null)
                return;

            try
            {
                _gameStatePersistence.DeleteGameDataAsync(_saveKey).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Failed to clear saved game: {ex.Message}");
            }
        }
    }
}
