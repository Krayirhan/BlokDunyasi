using System;
using UnityEngine;
using Firebase.Auth;

namespace BlockPuzzle.Core.Social
{
    // Compatibility wrapper for existing scene references. Google/Play Games services are removed.
    public class GooglePlayGamesManager : MonoBehaviour
    {
        public static GooglePlayGamesManager Instance { get; private set; }

        public string CurrentPlayerID { get; private set; }
        public string CurrentPlayerName { get; private set; }
        public bool IsPlayGamesAuthenticated { get; private set; }
        public bool IsFirebaseAuthenticated { get; private set; }
        public string FirebaseUserId { get; private set; }
        public string FirebaseProviderId { get; private set; }
        public bool IsLoggedIn => IsFirebaseAuthenticated;
        public bool IsLoggedInWithGoogle => false;
        public bool IsSignInInProgress => false;
        public bool HasActiveSession => IsFirebaseAuthenticated;

        public event Action OnStateChanged;

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
        }

        private void Start()
        {
            HookFirebaseState();
            SyncFromFirebase(BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance?.Auth?.CurrentUser);
        }

        private void HookFirebaseState()
        {
            if (BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance == null) return;

            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance.OnUserLogin -= SyncFromFirebase;
            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance.OnUserLogin += SyncFromFirebase;
        }

        private void SyncFromFirebase(FirebaseUser user)
        {
            var firebase = BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance;
            bool loggedIn = user != null && firebase != null && firebase.HasCompletedGuestLogin;

            IsPlayGamesAuthenticated = false;
            IsFirebaseAuthenticated = loggedIn;
            CurrentPlayerID = loggedIn ? user.UserId : null;
            CurrentPlayerName = loggedIn ? firebase.Username : null;
            FirebaseUserId = loggedIn ? user.UserId : null;
            FirebaseProviderId = loggedIn ? "password" : null;

            if (loggedIn)
            {
                SyncLocalScoreToCloud();
            }

            OnStateChanged?.Invoke();
        }

        public void SignInToGooglePlayGames(bool silent = false)
        {
            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance?.SignInAnonymously();
        }

        public void SignOut()
        {
            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance?.SignOut();
            OnStateChanged?.Invoke();
        }

        public void SetAsGuest()
        {
            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance?.SignInAnonymously();
        }

        public async void SyncLocalScoreToCloud()
        {
            try
            {
                var dataProvider = new BlockPuzzle.UnityAdapter.UnityPlayerPrefsDataProvider();
                var stats = await dataProvider.LoadStatisticsAsync();

                if (stats != null && stats.HighScore > 0)
                {
                    PostScore(stats.HighScore);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Firebase sync: local score sync failed. " + ex.Message);
            }
        }

        public void PostScore(int score)
        {
            BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance?.PostScore(score);
        }

        public void ShowLeaderboardUI()
        {
            Debug.LogWarning("Leaderboard UI is unavailable.");
        }

        private void OnDestroy()
        {
            if (Instance == this && BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance != null)
            {
                BlockPuzzle.UnityAdapter.Social.FirebaseManager.Instance.OnUserLogin -= SyncFromFirebase;
            }
        }
    }
}
