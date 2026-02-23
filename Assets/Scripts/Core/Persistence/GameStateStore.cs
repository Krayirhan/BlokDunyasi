// File: Core/Persistence/GameStateStore.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persistent storage for game state (save/load functionality).
    /// </summary>
    public sealed class GameStateStore
    {
        private const string GAME_STATE_KEY = "BlokDunyasi_GameState";
        private readonly IStorageProvider _storage;
        private readonly IJsonSerializer _serializer;
        
        public GameStateStore(IStorageProvider storage, IJsonSerializer serializer)
        {
            _storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
        }
        
        public bool HasSavedGame()
        {
            return HasSavedGame(null);
        }

        public bool HasSavedGame(string key)
        {
            return _storage.HasKey(GetKey(key));
        }

        public void SaveGame(GameData gameData)
        {
            SaveGame(gameData, null);
        }

        public void SaveGame(GameData gameData, string key)
        {
            if (gameData == null) return;

            var json = _serializer.Serialize(gameData);
            _storage.SaveString(GetKey(key), json);
            _storage.Save();
        }

        public GameData LoadGame()
        {
            return LoadGame(null);
        }

        public GameData LoadGame(string key)
        {
            if (!HasSavedGame(key))
                return null;

            var json = _storage.LoadString(GetKey(key));
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return _serializer.Deserialize<GameData>(json);
            }
            catch
            {
                return null;
            }
        }

        public void ClearSavedGame()
        {
            ClearSavedGame(null);
        }

        public void ClearSavedGame(string key)
        {
            _storage.DeleteKey(GetKey(key));
            _storage.Save();
        }

        private static string GetKey(string key)
        {
            return string.IsNullOrEmpty(key) ? GAME_STATE_KEY : key;
        }
    }
}
