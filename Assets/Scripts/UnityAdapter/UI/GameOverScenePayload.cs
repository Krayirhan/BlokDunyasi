using System;

namespace BlockPuzzle.UnityAdapter.UI
{
    internal readonly struct GameOverScenePayloadData
    {
        public readonly int FinalScore;
        public readonly int BestScore;
        public readonly bool IsNewBest;
        public readonly int SessionHighestCombo;
        public readonly int SessionBestMoveDelta;
        public readonly int TotalLinesCleared;
        public readonly int MoveCount;
        public readonly string GameSceneName;

        public GameOverScenePayloadData(
            int finalScore,
            int bestScore,
            bool isNewBest,
            int sessionHighestCombo,
            int sessionBestMoveDelta,
            int totalLinesCleared,
            int moveCount,
            string gameSceneName = null)
        {
            FinalScore = finalScore;
            BestScore = bestScore;
            IsNewBest = isNewBest;
            SessionHighestCombo = sessionHighestCombo;
            SessionBestMoveDelta = sessionBestMoveDelta;
            TotalLinesCleared = totalLinesCleared;
            MoveCount = moveCount;
            GameSceneName = gameSceneName ?? string.Empty;
        }
    }

    internal static class GameOverScenePayload
    {
        private static GameOverScenePayloadData? _current;

        public static void Set(GameOverScenePayloadData payload)
        {
            _current = payload;
        }

        public static bool TryGet(out GameOverScenePayloadData payload)
        {
            if (_current.HasValue)
            {
                payload = _current.Value;
                return true;
            }

            payload = default;
            return false;
        }

        public static void Clear()
        {
            _current = null;
        }
    }

    public static class RewardedAdBridge
    {
        private static Func<bool> _isReady;
        private static Action _show;

        public static event Action RewardedAdLoaded;
        public static event Action<string> RewardedAdFailedToLoad;
        public static event Action RewardedUserEarned;
        public static event Action RewardedAdClosed;

        public static bool HasProvider => _isReady != null && _show != null;

        public static void RegisterProvider(Func<bool> isReady, Action show)
        {
            _isReady = isReady;
            _show = show;
        }

        public static bool IsReady()
        {
            return _isReady != null && _isReady();
        }

        public static void Show()
        {
            _show?.Invoke();
        }

        public static void NotifyLoaded()
        {
            RewardedAdLoaded?.Invoke();
        }

        public static void NotifyFailedToLoad(string errorMessage)
        {
            RewardedAdFailedToLoad?.Invoke(errorMessage);
        }

        public static void NotifyUserEarned()
        {
            RewardedUserEarned?.Invoke();
        }

        public static void NotifyClosed()
        {
            RewardedAdClosed?.Invoke();
        }
    }
}
