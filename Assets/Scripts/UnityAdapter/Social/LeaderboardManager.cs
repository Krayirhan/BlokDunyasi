using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Social;
namespace BlockPuzzle.UnityAdapter.Social
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public string playerName;
        public int score;
        public int rank;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }
        
        public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;
        private List<LeaderboardEntry> _pendingSubmissions = new List<LeaderboardEntry>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

                // Note: AuthManager is optional at compile-time. We'll resolve it
                // reflectively when available and subscribe to auth state changes
                // so we can flush pending submissions when user signs in.
                TrySubscribeAuthStateChanged();
        }

            private void TrySubscribeAuthStateChanged()
            {
                try
                {
                    var authType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthManager, BlockPuzzleUnityAdapter");
                    if (authType == null) return;

                    var instProp = authType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var inst = instProp?.GetValue(null, null);
                    if (inst == null) return;

                    var evt = authType.GetEvent("OnAuthStateChanged");
                    if (evt == null) return;

                    var handler = Delegate.CreateDelegate(evt.EventHandlerType, this, "OnAuthStateChangedInternal");
                    evt.AddEventHandler(inst, handler);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not subscribe to AuthManager events: {ex.Message}");
                }
            }

            // Called via reflection when AuthManager signals a change.
            private void OnAuthStateChangedInternal(bool signedIn)
            {
                if (signedIn)
                {
                    FlushPendingSubmissions();
                }
            }


        public void SubmitScore(int score, int totalMoves, int maxCombo)
        {
            if (!ScoreValidator.ValidateScore(score, totalMoves, maxCombo))
            {
                Debug.LogError("Cheat detected! Score rejected.");
                return;
            }

            Debug.Log($"Score {score} validated for submission.");

            bool isSignedIn = false;
            string playerId = "guest";
            string playerName = "Guest";

            var gpgs = BlockPuzzle.Core.Social.GooglePlayGamesManager.Instance;
            if (gpgs != null)
            {
                isSignedIn = gpgs.IsLoggedIn;
                playerId = gpgs.CurrentPlayerID ?? playerId;
                playerName = gpgs.CurrentPlayerName ?? playerName;
            }

            var entry = new LeaderboardEntry { playerId = playerId, playerName = playerName, score = score };

            if (isSignedIn && TrySendToPlatform(score))
            {
                Debug.Log("Score sent to platform leaderboard.");
            }
            else
            {
                _pendingSubmissions.Add(entry);
                Debug.Log("Queued score for later submission (not signed in or no backend).");
            }
        }

        private bool TrySendToPlatform(int score)
        {
            try
            {
                if (BlockPuzzle.Core.Social.GooglePlayGamesManager.Instance != null)
                {
                    BlockPuzzle.Core.Social.GooglePlayGamesManager.Instance.PostScore(score);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Leaderboard platform send failed: {ex.Message}");
            }

            return false;
        }

        private void FlushPendingSubmissions()
        {
            if (_pendingSubmissions.Count == 0) return;

            Debug.Log($"Flushing {_pendingSubmissions.Count} pending leaderboard submissions...");
            var sent = new List<LeaderboardEntry>();

            foreach (var e in _pendingSubmissions)
            {
                if (TrySendToPlatform(e.score))
                    sent.Add(e);
            }

            foreach (var s in sent) _pendingSubmissions.Remove(s);
        }

        public float GetPlayerPercentile(int userScore, int totalPlayers)
        {
            // Örnek percentile (yüzdelik dilim) hesaplaması
            // backend'den gerçek rank verisi geldiğinde hesabı güncelleyin
            if (totalPlayers == 0) return 100f;
            int simulatedRank = Mathf.Max(1, totalPlayers - (userScore / 10)); // Tamamen örnek simule edilmiş rank
            
            float percentile = (1.0f - ((float)simulatedRank / totalPlayers)) * 100f;
            return Mathf.Clamp(percentile, 1f, 99f);
        }

        public void FetchWeeklyLeaderboard()
        {
            // Mock veriler:
            List<LeaderboardEntry> list = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "BlockMaster", score = 15400 },
                new LeaderboardEntry { rank = 2, playerName = "PuzzleKing", score = 12200 },
                new LeaderboardEntry { rank = 3, playerName = "You", score = 9800 }
            };

            OnLeaderboardUpdated?.Invoke(list);
        }
    }
}