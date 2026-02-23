// File: Core/Persistence/SettingsStore.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persistent storage for game settings.
    /// </summary>
    public sealed class SettingsStore
    {
        private const string SETTINGS_KEY = "BlokDunyasi_Settings";
        private readonly IStorageProvider _storage;
        private readonly IJsonSerializer _serializer;
        
        public SettingsStore(IStorageProvider storage, IJsonSerializer serializer)
        {
            _storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
        }
        
        public void SaveSettings(GameSettings settings)
        {
            if (settings == null) return;
            
            var json = _serializer.Serialize(settings);
            _storage.SaveString(SETTINGS_KEY, json);
            _storage.Save();
        }
        
        public GameSettings LoadSettings()
        {
            if (!_storage.HasKey(SETTINGS_KEY))
                return GameSettings.Default;
            
            var json = _storage.LoadString(SETTINGS_KEY);
            if (string.IsNullOrEmpty(json))
                return GameSettings.Default;
            
            try
            {
                return _serializer.Deserialize<GameSettings>(json);
            }
            catch
            {
                return GameSettings.Default;
            }
        }
    }
}
