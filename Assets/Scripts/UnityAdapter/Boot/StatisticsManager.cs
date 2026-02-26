using System;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Boot
{
    internal sealed class StatisticsManager
    {
        private readonly IStatisticsPersistence _statisticsPersistence;

        public StatisticsManager(IStatisticsPersistence statisticsPersistence)
        {
            _statisticsPersistence = statisticsPersistence;
        }

        public void RecordGameSession(GameState gameState, int finalScore, int sessionHighestCombo)
        {
            if (_statisticsPersistence == null || gameState == null)
                return;

            try
            {
                var stats = _statisticsPersistence.LoadStatisticsAsync().GetAwaiter().GetResult() ?? GameStatistics.CreateDefault();
                var elapsedTime = gameState.GetElapsedTime();
                var blocksPlaced = gameState.MoveCount;
                var linesCleared = gameState.TotalLinesCleared;

                stats.RecordGameSession(finalScore, elapsedTime, blocksPlaced, linesCleared, sessionHighestCombo);
                _statisticsPersistence.SaveStatisticsAsync(stats).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to record statistics: {ex.Message}");
            }
        }

        public void RecordMove(int moveScore, int linesCleared)
        {
            if (_statisticsPersistence == null)
                return;

            if (moveScore <= 0 && linesCleared <= 0)
                return;

            try
            {
                var stats = _statisticsPersistence.LoadStatisticsAsync().GetAwaiter().GetResult() ?? GameStatistics.CreateDefault();
                stats.RecordMove(moveScore, linesCleared);
                _statisticsPersistence.SaveStatisticsAsync(stats).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to record move statistics: {ex.Message}");
            }
        }
    }
}
