using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Social;
using BlockPuzzle.UnityAdapter.UI.Localization;
using Firebase.Firestore;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Social
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public string playerName;
        public int score;
        public int rank;
        public int rescueCount;
        public string gameMode;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        private const string PublicLeaderboardCollection = "leaderboard_public";
        private const string UsersCollection = "users";
        private const string WeeklyHighScoreField = "weeklyHighScore";

        public static LeaderboardManager Instance { get; private set; }

        public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;

        private readonly List<LeaderboardEntry> _pendingSubmissions = new List<LeaderboardEntry>();
        private List<LeaderboardEntry> _cachedWeeklyLeaderboard = new List<LeaderboardEntry>();

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
                return;
            }

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

                var handler = Delegate.CreateDelegate(evt.EventHandlerType, this, nameof(OnAuthStateChangedInternal));
                evt.AddEventHandler(inst, handler);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not subscribe to AuthManager events: {ex.Message}");
            }
        }

        private void OnAuthStateChangedInternal(bool signedIn)
        {
            if (signedIn)
                FlushPendingSubmissions();
        }

        public void SubmitScore(int score, int totalMoves, int maxCombo, int rescueCount = 0, string gameMode = "Classic")
        {
            if (!ScoreValidator.ValidateScore(score, totalMoves, maxCombo))
            {
                Debug.LogError("Cheat detected! Score rejected.");
                return;
            }

            var firebase = FirebaseManager.Instance;
            if (firebase != null && firebase.IsInitialized)
            {
                firebase.PostScore(score, rescueCount, gameMode);
                Debug.Log($"Score {score} sent to Firebase leaderboard.");
                return;
            }

            _pendingSubmissions.Add(new LeaderboardEntry { score = score, rescueCount = rescueCount, gameMode = gameMode });
            Debug.Log($"Score {score} queued for Firebase leaderboard.");
        }

        private void FlushPendingSubmissions()
        {
            if (_pendingSubmissions.Count == 0)
                return;

            var firebase = FirebaseManager.Instance;
            if (firebase == null || !firebase.IsInitialized)
                return;

            foreach (var entry in _pendingSubmissions)
                firebase.PostScore(entry.score, entry.rescueCount, entry.gameMode);

            _pendingSubmissions.Clear();
        }

        public float GetPlayerPercentile(int userScore, int totalPlayers)
        {
            List<LeaderboardEntry> source = _cachedWeeklyLeaderboard;
            if (source == null || source.Count == 0)
                return totalPlayers > 0 ? EstimatePercentileFromPlayerCount(userScore, totalPlayers) : 0f;

            int playersBelow = source.Count(entry => entry != null && entry.score < userScore);
            return Mathf.Clamp((playersBelow / (float)source.Count) * 100f, 0f, 100f);
        }

        public async Task<float> GetWeeklyPercentileAsync(int userScore)
        {
            List<LeaderboardEntry> weeklyRows = await FetchFirebaseLeaderboardAsync(weekly: true, attachCurrentPlayerPreview: false);
            _cachedWeeklyLeaderboard = weeklyRows ?? new List<LeaderboardEntry>();
            return GetPlayerPercentile(userScore, _cachedWeeklyLeaderboard.Count);
        }

        public async void FetchWeeklyLeaderboard()
        {
            _cachedWeeklyLeaderboard = await FetchFirebaseLeaderboardAsync(weekly: true, attachCurrentPlayerPreview: true);
            OnLeaderboardUpdated?.Invoke(_cachedWeeklyLeaderboard);
        }

        private static async Task<List<LeaderboardEntry>> FetchFirebaseLeaderboardAsync(bool weekly, bool attachCurrentPlayerPreview)
        {
            var rows = new List<LeaderboardEntry>();
            var firebase = FirebaseManager.Instance;
            if (firebase == null)
                return rows;

            int waitCycles = 0;
            while (!firebase.IsInitialized && waitCycles < 100)
            {
                waitCycles++;
                await Task.Delay(50);
            }

            if (!firebase.IsInitialized)
                return rows;

            await firebase.EnsureAuthenticatedForLeaderboardAsync();

            QuerySnapshot snapshot = await FirebaseFirestore.DefaultInstance
                .Collection(PublicLeaderboardCollection)
                .OrderByDescending(weekly ? WeeklyHighScoreField : "highScore")
                .GetSnapshotAsync();

            string currentUserId = firebase.CurrentUser?.UserId;
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (document == null || !document.Exists)
                    continue;

                Dictionary<string, object> data = document.ToDictionary();
                int score = ReadInt(data, weekly ? WeeklyHighScoreField : "highScore", 0);
                if (score <= 0)
                    continue;

                string username = ReadString(data, "username", ReadString(data, "normalizedUsername", string.Empty));
                bool isCurrentUser = string.Equals(document.Id, currentUserId, StringComparison.Ordinal);
                bool isGuest = ReadBool(data, "isGuest");
                if (!isCurrentUser && (isGuest || string.IsNullOrWhiteSpace(username)))
                    continue;

                rows.Add(new LeaderboardEntry
                {
                    playerId = document.Id,
                    playerName = isCurrentUser ? ResolveCurrentPlayerLabel() : username,
                    score = score
                });
            }

            if (attachCurrentPlayerPreview)
                await AttachCurrentPlayerPreviewRowAsync(rows, firebase, weekly);
            rows.Sort(CompareEntries);
            for (int i = 0; i < rows.Count; i++)
                rows[i].rank = i + 1;

            return rows;
        }

        private static async Task AttachCurrentPlayerPreviewRowAsync(List<LeaderboardEntry> rows, FirebaseManager firebase, bool weekly)
        {
            if (rows == null || firebase?.CurrentUser == null)
                return;

            string currentUserId = firebase.CurrentUser.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return;

            LeaderboardEntry existingCurrent = rows.FirstOrDefault(entry =>
                string.Equals(entry.playerId, currentUserId, StringComparison.Ordinal));

            if (existingCurrent != null)
            {
                existingCurrent.playerName = ResolveCurrentPlayerLabel();
                return;
            }

            if (!firebase.CurrentUser.IsAnonymous)
            {
                LeaderboardEntry remoteCurrent = await FetchCurrentUserRemoteEntryAsync(currentUserId, weekly);
                if (remoteCurrent != null)
                {
                    remoteCurrent.playerName = ResolveCurrentPlayerLabel();
                    rows.Add(remoteCurrent);
                    return;
                }
            }

            if (weekly)
                return;

            int localBestScore = GetLocalBestScoreStatic();
            if (localBestScore <= 0)
                return;

            rows.Add(new LeaderboardEntry
            {
                playerId = currentUserId,
                playerName = ResolveCurrentPlayerLabel(),
                score = localBestScore
            });
        }

        private static int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
        {
            int scoreCompare = b.score.CompareTo(a.score);
            if (scoreCompare != 0)
                return scoreCompare;

            return string.Compare(a.playerName, b.playerName, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<LeaderboardEntry> FetchCurrentUserRemoteEntryAsync(string currentUserId, bool weekly)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return null;

            DocumentSnapshot snapshot = await FirebaseFirestore.DefaultInstance
                .Collection(UsersCollection)
                .Document(currentUserId)
                .GetSnapshotAsync();

            if (snapshot == null || !snapshot.Exists)
                return null;

            Dictionary<string, object> data = snapshot.ToDictionary();
            int score = ReadInt(data, weekly ? WeeklyHighScoreField : "highScore", 0);
            if (score <= 0)
                return null;

            string username = ReadLeaderboardName(data);
            return new LeaderboardEntry
            {
                playerId = currentUserId,
                playerName = string.IsNullOrWhiteSpace(username) ? ResolveCurrentPlayerLabel() : username,
                score = score
            };
        }

        private static int ReadInt(Dictionary<string, object> data, string key, int fallback)
        {
            if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
                return fallback;

            try
            {
                return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadString(Dictionary<string, object> data, string key, string fallback)
        {
            if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
                return fallback;

            return raw.ToString();
        }

        private static string ReadLeaderboardName(Dictionary<string, object> data)
        {
            string username = ReadString(data, "username", string.Empty);
            if (!string.IsNullOrWhiteSpace(username))
                return username;

            string normalizedUsername = ReadString(data, "normalizedUsername", string.Empty);
            if (!string.IsNullOrWhiteSpace(normalizedUsername))
                return normalizedUsername;

            string displayName = ReadString(data, "displayName", string.Empty);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            string playerName = ReadString(data, "playerName", string.Empty);
            if (!string.IsNullOrWhiteSpace(playerName))
                return playerName;

            return ReadString(data, "name", string.Empty);
        }

        private static bool ReadBool(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
                return false;

            try
            {
                return Convert.ToBoolean(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }
        }

        private static int GetLocalBestScoreStatic()
        {
            return new BestScoreStore(new PlayerPrefsStorage()).GetBestScore();
        }

        private static float EstimatePercentileFromPlayerCount(int userScore, int totalPlayers)
        {
            return 0f;
        }

        private static string ResolveCurrentPlayerLabel()
        {
            if (LanguageManager.Instance == null)
                return "Sen";

            switch (LanguageManager.Instance.CurrentLanguage)
            {
                case LanguageManager.Language.Korean:
                    return "나";
                case LanguageManager.Language.English:
                    return "You";
                default:
                    return "Sen";
            }
        }
    }
}
