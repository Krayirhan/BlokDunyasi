using System;
using System.Threading.Tasks;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Persistence;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Boot
{
    internal sealed class StatisticsManager
    {
        private readonly IStatisticsPersistence _statisticsPersistence;
        private GameStatistics _cachedStatistics;
        private bool _statisticsLoaded;
        private bool _statisticsDirty;
        private int _pendingMoveWrites;
        private const int MoveWriteBatchSize = 5;

        public StatisticsManager(IStatisticsPersistence statisticsPersistence)
        {
            _statisticsPersistence = statisticsPersistence;
        }

        public void RecordGameSession(GameState gameState, int finalScore, int sessionHighestCombo)
        {
            if (_statisticsPersistence == null || gameState == null)
                return;

            _ = RecordGameSessionAsync(gameState, finalScore, sessionHighestCombo);
        }

        public void RecordMove(int moveScore, int linesCleared)
        {
            if (_statisticsPersistence == null)
                return;

            if (moveScore <= 0 && linesCleared <= 0)
                return;

            _ = RecordMoveAsync(moveScore, linesCleared);
        }

        public void RecordMissionProgress(int progressDelta, bool dailyCompleted, bool weeklyCompleted)
        {
            if (_statisticsPersistence == null)
                return;

            _ = RecordMissionProgressAsync(progressDelta, dailyCompleted, weeklyCompleted);
        }

        public void FlushPendingStatistics()
        {
            if (_statisticsPersistence == null || !_statisticsDirty)
                return;

            _ = FlushPendingStatisticsAsync();
        }

        private async Task RecordGameSessionAsync(GameState gameState, int finalScore, int sessionHighestCombo)
        {
            try
            {
                var stats = await GetStatisticsAsync();
                var elapsedTime = gameState.GetElapsedTime();
                var blocksPlaced = gameState.MoveCount;
                var linesCleared = gameState.TotalLinesCleared;

                int currentHighScore = stats.HighScore;
                stats.RecordGameSession(finalScore, elapsedTime, blocksPlaced, linesCleared, sessionHighestCombo);
                _statisticsDirty = true;
                await PersistStatisticsAsync(force: true);

                if (finalScore >= currentHighScore && BlockPuzzle.Core.Social.GooglePlayGamesManager.Instance != null)
                    BlockPuzzle.Core.Social.GooglePlayGamesManager.Instance.PostScore(stats.HighScore);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to record statistics: {ex.Message}");
            }
        }

        private async Task RecordMoveAsync(int moveScore, int linesCleared)
        {
            try
            {
                var stats = await GetStatisticsAsync();
                stats.RecordMove(moveScore, linesCleared);
                _statisticsDirty = true;
                _pendingMoveWrites++;

                if (_pendingMoveWrites >= MoveWriteBatchSize)
                    await PersistStatisticsAsync(force: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to record move statistics: {ex.Message}");
            }
        }

        private async Task RecordMissionProgressAsync(int progressDelta, bool dailyCompleted, bool weeklyCompleted)
        {
            try
            {
                var stats = await GetStatisticsAsync();
                stats.RecordMissionProgress(progressDelta, dailyCompleted, weeklyCompleted);
                _statisticsDirty = true;
                await PersistStatisticsAsync(force: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to record mission progress: {ex.Message}");
            }
        }

        private async Task<GameStatistics> GetStatisticsAsync()
        {
            if (_statisticsLoaded)
                return _cachedStatistics ?? GameStatistics.CreateDefault();

            _cachedStatistics = await _statisticsPersistence.LoadStatisticsAsync() ?? GameStatistics.CreateDefault();
            _statisticsLoaded = true;
            return _cachedStatistics;
        }

        private async Task PersistStatisticsAsync(bool force)
        {
            if (!_statisticsDirty || _cachedStatistics == null)
                return;

            if (!force && _pendingMoveWrites < MoveWriteBatchSize)
                return;

            await _statisticsPersistence.SaveStatisticsAsync(_cachedStatistics);
            _statisticsDirty = false;
            _pendingMoveWrites = 0;
        }

        private async Task FlushPendingStatisticsAsync()
        {
            try
            {
                await PersistStatisticsAsync(force: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatisticsManager] Failed to flush pending statistics: {ex.Message}");
            }
        }
    }
}
