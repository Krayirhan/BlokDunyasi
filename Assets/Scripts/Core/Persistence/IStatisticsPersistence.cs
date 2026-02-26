using System.Threading.Tasks;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persists and restores long-term statistics.
    /// </summary>
    public interface IStatisticsPersistence
    {
        Task SaveStatisticsAsync(GameStatistics statistics);
        Task<GameStatistics> LoadStatisticsAsync();
    }
}
