using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.Auth
{
    public class AuthUIController : MonoBehaviour
    {
        [Header("Buttons")]
        public Button guestSignInButton;
        public Button playSignInButton;
        public Button signOutButton;

        [Header("Status")]
        public Text statusText;

        void Start()
        {
            EnsureAuthExists();

            if (playSignInButton != null) playSignInButton.onClick.AddListener(OnPlaySignIn);
            if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOut);

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnAuthStateChanged += HandleAuthStateChanged;
            }

            if (guestSignInButton != null)
            {
                guestSignInButton.interactable = false;
                guestSignInButton.gameObject.SetActive(false);
            }

            UpdateStatus();
        }

        void OnDestroy()
        {
            if (playSignInButton != null) playSignInButton.onClick.RemoveListener(OnPlaySignIn);
            if (signOutButton != null) signOutButton.onClick.RemoveListener(OnSignOut);

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnAuthStateChanged -= HandleAuthStateChanged;
            }
        }

        private void EnsureAuthExists()
        {
            if (AuthManager.Instance == null)
            {
                var go = new GameObject("AuthManager");
                go.AddComponent<AuthManager>();
            }
        }

        public void OnPlaySignIn()
        {
            EnsureAuthExists();
            var mgr = AuthManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("AuthManager not available.");
                return;
            }

            bool started = mgr.SignInInteractive();
            if (!started)
            {
                Debug.Log("Guest sign-in not started.");
                ShowToast("Guest sign-in not available.");
            }

            UpdateStatus();
        }

        public void OnSignOut()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.SignOut();
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (statusText == null) return;

            if (AuthManager.Instance == null)
            {
                statusText.text = "Not signed in";
                return;
            }

            var mgr = AuthManager.Instance;
            if (!mgr.IsPlayGamesSignedIn)
            {
                statusText.text = "Guest: not signed in";
                return;
            }

            if (!mgr.IsFirebaseSignedIn)
            {
                statusText.text =
                    $"Guest: {mgr.PlayerName}\n" +
                    $"GuestId: {mgr.PlayerId}\n" +
                    "Firebase: NOT LINKED";
                return;
            }

            statusText.text =
                $"Guest: {mgr.PlayerName}\n" +
                $"GuestId: {mgr.PlayerId}\n" +
                $"Firebase UID: {mgr.FirebaseUserId}\n" +
                $"Provider: {mgr.FirebaseProviderId}";
        }

        private void HandleAuthStateChanged(bool _)
        {
            UpdateStatus();
        }

        private void ShowToast(string message)
        {
            Debug.Log(message);
            if (statusText != null) statusText.text = message;
        }
    }
}
