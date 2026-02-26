// File: Core/Persistence/IGameDataProvider.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Backward-compatible composite contract for all persistence concerns.
    /// </summary>
    public interface IGameDataProvider : IGameStatePersistence, ISettingsPersistence, IStatisticsPersistence
    {
        // Intentionally empty: composed from focused interfaces.
    }
}
