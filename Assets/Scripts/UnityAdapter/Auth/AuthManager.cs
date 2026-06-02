using System;
using UnityEngine;
using BlockPuzzle.Core.Social;

namespace BlockPuzzle.UnityAdapter.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        public string PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsSignedIn { get; private set; }
        public bool IsPlayGamesSignedIn { get; private set; }
        public bool IsFirebaseSignedIn { get; private set; }
        public string FirebaseUserId { get; private set; }
        public string FirebaseProviderId { get; private set; }

        public event Action<bool> OnAuthStateChanged;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                TryHookGooglePlayManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SignInSilently()
        {
            SyncFromGooglePlayManager();
        }

        public bool SignInInteractive()
        {
            var gpgs = GooglePlayGamesManager.Instance;
            if (gpgs == null)
            {
                Debug.LogWarning("AuthManager: Guest account manager not found. Sign-in not started.");
                return false;
            }

            gpgs.SignInToGooglePlayGames(false);
            return true;
        }

        public void SignOut()
        {
            var gpgs = GooglePlayGamesManager.Instance;
            if (gpgs != null)
            {
                gpgs.SignOut();
                return;
            }

            SetState(false, false, false, null, null, null, null);
        }

        private void SetState(
            bool signedIn,
            bool playGamesSignedIn,
            bool firebaseSignedIn,
            string playerId,
            string playerName,
            string firebaseUserId,
            string firebaseProviderId)
        {
            IsSignedIn = signedIn;
            IsPlayGamesSignedIn = playGamesSignedIn;
            IsFirebaseSignedIn = firebaseSignedIn;
            PlayerId = playerId;
            PlayerName = playerName;
            FirebaseUserId = firebaseUserId;
            FirebaseProviderId = firebaseProviderId;
            OnAuthStateChanged?.Invoke(IsSignedIn);
        }

        private void TryHookGooglePlayManager()
        {
            var gpgs = GooglePlayGamesManager.Instance;
            if (gpgs == null) return;

            gpgs.OnStateChanged -= SyncFromGooglePlayManager;
            gpgs.OnStateChanged += SyncFromGooglePlayManager;
            SyncFromGooglePlayManager();
        }

        private void SyncFromGooglePlayManager()
        {
            var gpgs = GooglePlayGamesManager.Instance;
            if (gpgs == null)
            {
                SetState(false, false, false, null, null, null, null);
                return;
            }

            SetState(
                gpgs.IsLoggedIn,
                gpgs.IsPlayGamesAuthenticated,
                gpgs.IsFirebaseAuthenticated,
                gpgs.CurrentPlayerID,
                gpgs.CurrentPlayerName,
                gpgs.FirebaseUserId,
                gpgs.FirebaseProviderId);
        }

        private void OnDestroy()
        {
            var gpgs = GooglePlayGamesManager.Instance;
            if (gpgs != null)
            {
                gpgs.OnStateChanged -= SyncFromGooglePlayManager;
            }
        }
    }
}
