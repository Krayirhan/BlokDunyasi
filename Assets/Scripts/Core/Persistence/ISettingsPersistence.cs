using System.Threading.Tasks;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persists and restores player/game settings.
    /// </summary>
    public interface ISettingsPersistence
    {
        Task SaveSettingsAsync(GameSettings settings);
        Task<GameSettings> LoadSettingsAsync();
    }
}
