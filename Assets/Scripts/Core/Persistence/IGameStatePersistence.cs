using System.Threading.Tasks;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persists and restores active game session state.
    /// </summary>
    public interface IGameStatePersistence
    {
        Task SaveGameDataAsync(string key, GameData data);
        Task<GameData> LoadGameDataAsync(string key);
        Task<bool> HasGameDataAsync(string key);
        Task DeleteGameDataAsync(string key);
    }
}
