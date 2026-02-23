// File: Core/Persistence/StatisticsStore.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persistent storage for game statistics.
    /// </summary>
    public sealed class StatisticsStore
    {
        private const string STATISTICS_KEY = "BlokDunyasi_Statistics";
        private readonly IStorageProvider _storage;
        private readonly IJsonSerializer _serializer;

        public StatisticsStore(IStorageProvider storage, IJsonSerializer serializer)
        {
            _storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
        }

        public void SaveStatistics(GameStatistics statistics)
        {
            if (statistics == null) return;

            var json = _serializer.Serialize(statistics);
            _storage.SaveString(STATISTICS_KEY, json);
            _storage.Save();
        }

        public GameStatistics LoadStatistics()
        {
            if (!_storage.HasKey(STATISTICS_KEY))
                return GameStatistics.CreateDefault();

            var json = _storage.LoadString(STATISTICS_KEY);
            if (string.IsNullOrEmpty(json))
                return GameStatistics.CreateDefault();

            try
            {
                return _serializer.Deserialize<GameStatistics>(json);
            }
            catch
            {
                return GameStatistics.CreateDefault();
            }
        }
    }
}
