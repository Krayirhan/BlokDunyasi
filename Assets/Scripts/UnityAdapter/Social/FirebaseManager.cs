using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Firestore;
using BlockPuzzle.UnityAdapter.Privacy;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Social
{
    public class FirebaseManager : MonoBehaviour
    {
        private const string UsersCollection = "users";
        private const string UsernamesCollection = "usernames";
        private const string PublicLeaderboardCollection = "leaderboard_public";
        private const string PrefUsername = "BlokDunyasi_Username";
        private const string PrefNormalizedUsername = "BlokDunyasi_NormalizedUsername";
        private const string AccountEmailDomain = "blokdunyasi.local";
        private const float StartupInitializationDelaySeconds = 0.5f;

        public static FirebaseManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public FirebaseAuth Auth { get; private set; }
        public FirebaseUser CurrentUser { get; private set; }
        public string Username { get; private set; }
        public string NormalizedUsername { get; private set; }
        public bool HasCompletedGuestLogin => Auth != null
            && Auth.CurrentUser != null
            && !Auth.CurrentUser.IsAnonymous
            && !string.IsNullOrEmpty(NormalizedUsername);

        public event Action OnFirebaseInitialized;
        public event Action<FirebaseUser> OnUserLogin;
        private Coroutine _initializationRoutine;
        private bool _subscribedToConsent;
        private bool? _lastAppliedAnalyticsCollectionState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Username = PlayerPrefs.GetString(PrefUsername, string.Empty);
                NormalizedUsername = PlayerPrefs.GetString(PrefNormalizedUsername, string.Empty);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _initializationRoutine = StartCoroutine(InitializeFirebaseDeferred());
        }

        private IEnumerator InitializeFirebaseDeferred()
        {
            if (StartupInitializationDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(StartupInitializationDelaySeconds);

            InitializeFirebase();
            _initializationRoutine = null;
        }

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"Firebase: Initialization failed. status={dependencyStatus}");
                    return;
                }

                IsInitialized = true;
                Auth = FirebaseAuth.DefaultInstance;

                ApplyAnalyticsCollectionState(ConsentGate.CanCollectAnalytics, force: true);
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                SubscribeToConsentState();

                Auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);

                Debug.Log("Firebase: Initialized successfully.");

                CheckVersion();
                OnFirebaseInitialized?.Invoke();
            });
        }

        private void OnDestroy()
        {
            if (_initializationRoutine != null)
            {
                StopCoroutine(_initializationRoutine);
                _initializationRoutine = null;
            }

            if (_subscribedToConsent)
            {
                ConsentGate.ConsentStateChanged -= HandleConsentStateChanged;
                _subscribedToConsent = false;
            }
        }

        private void SubscribeToConsentState()
        {
            if (_subscribedToConsent)
                return;

            ConsentGate.ConsentStateChanged += HandleConsentStateChanged;
            _subscribedToConsent = true;
        }

        private void HandleConsentStateChanged(ConsentState state)
        {
            if (!IsInitialized)
                return;

            ApplyAnalyticsCollectionState(state == ConsentState.Accepted);
        }

        private void ApplyAnalyticsCollectionState(bool enabled, bool force = false)
        {
            if (!force && _lastAppliedAnalyticsCollectionState.HasValue && _lastAppliedAnalyticsCollectionState.Value == enabled)
                return;

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
            _lastAppliedAnalyticsCollectionState = enabled;
        }

        private void CheckVersion()
        {
            if (!IsInitialized) return;

            FirebaseFirestore.DefaultInstance.Collection("configs").Document("app").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                var snapshot = task.Result;
                if (!snapshot.Exists) return;

                var data = snapshot.ToDictionary();
                if (!data.ContainsKey("minVersion")) return;

                int minVersion = Convert.ToInt32(data["minVersion"]);
                int currentVersionCode = 4;

                if (currentVersionCode < minVersion)
                {
                    ShowUpdatePopup();
                }
            });
        }

        private void ShowUpdatePopup()
        {
            Debug.LogWarning("Firebase: Update required popup will be shown.");

#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject builder = new AndroidJavaObject("android.app.AlertDialog$Builder", currentActivity);
                builder.Call<AndroidJavaObject>("setTitle", "Guncelleme Mevcut");
                builder.Call<AndroidJavaObject>("setMessage", "Oyunun yeni ve daha kararli bir surumu mevcut. Devam etmek icin lutfen guncelleyin.");
                builder.Call<AndroidJavaObject>("setCancelable", false);
                builder.Call<AndroidJavaObject>("setPositiveButton", "Guncelle", new AndroidDialogClickListener());
                builder.Call<AndroidJavaObject>("show");
            }));
#endif
        }

        private class AndroidDialogClickListener : AndroidJavaProxy
        {
            public AndroidDialogClickListener() : base("android.content.DialogInterface$OnClickListener") { }

            public void onClick(AndroidJavaObject dialog, int which)
            {
                Application.OpenURL("market://details?id=" + Application.identifier);
                Application.Quit();
            }
        }

        private void AuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (Auth == null || Auth.CurrentUser == CurrentUser) return;

            CurrentUser = Auth.CurrentUser;
            if (CurrentUser == null)
            {
                OnUserLogin?.Invoke(null);
                return;
            }

            Debug.Log($"Firebase: Auth user detected. anonymous={CurrentUser.IsAnonymous}");
            if (!string.IsNullOrEmpty(NormalizedUsername))
            {
                CreateUserInFirestore(CurrentUser, Username, NormalizedUsername);
            }

            OnUserLogin?.Invoke(CurrentUser);
        }

        public void SignInAnonymously()
        {
            SignInOrContinueGuest(null);
        }

        public void SignInOrContinueGuest(Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false, "Firebase hazir degil.");
                return;
            }

            if (Auth.CurrentUser != null && Auth.CurrentUser.IsAnonymous)
            {
                if (!string.IsNullOrEmpty(NormalizedUsername))
                {
                    CreateUserInFirestore(Auth.CurrentUser, Username, NormalizedUsername);
                }

                OnUserLogin?.Invoke(Auth.CurrentUser);
                onComplete?.Invoke(true, null);
                return;
            }

            if (Auth.CurrentUser != null && !Auth.CurrentUser.IsAnonymous)
            {
                Auth.SignOut();
            }

            Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Firebase: Anonymous sign-in failed.");
                    onComplete?.Invoke(false, "Misafir girisi basarisiz.");
                    return;
                }

                FirebaseUser newUser = Auth.CurrentUser;
                CurrentUser = newUser;
                Debug.Log("Firebase: Anonymous sign-in succeeded.");

                if (!string.IsNullOrEmpty(NormalizedUsername))
                {
                    CreateUserInFirestore(newUser, Username, NormalizedUsername);
                }

                OnUserLogin?.Invoke(newUser);
                onComplete?.Invoke(true, null);
            });
        }

        public void RegisterGuestUsername(string requestedUsername, Action<bool, string> onComplete)
        {
            onComplete?.Invoke(false, "Sifreli kayit gerekli.");
        }

        public void RegisterAccount(string requestedUsername, string password, Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false, "Firebase hazir degil.");
                return;
            }

            if (!string.IsNullOrEmpty(NormalizedUsername) && Auth.CurrentUser != null && !Auth.CurrentUser.IsAnonymous)
            {
                onComplete?.Invoke(true, null);
                return;
            }

            string validationError = ValidateUsername(requestedUsername, out string cleanUsername, out string normalized);
            if (!string.IsNullOrEmpty(validationError))
            {
                onComplete?.Invoke(false, validationError);
                return;
            }

            string passwordError = ValidatePassword(password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                onComplete?.Invoke(false, passwordError);
                return;
            }

            RegisterAccountInternal(cleanUsername, normalized, password, onComplete);
        }

        public void LoginAccount(string requestedUsername, string password, Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false, "Firebase hazir degil.");
                return;
            }

            string validationError = ValidateUsername(requestedUsername, out string cleanUsername, out string normalized);
            if (!string.IsNullOrEmpty(validationError))
            {
                onComplete?.Invoke(false, validationError);
                return;
            }

            string passwordError = ValidatePassword(password);
            if (!string.IsNullOrEmpty(passwordError))
            {
                onComplete?.Invoke(false, passwordError);
                return;
            }

            string email = BuildAccountEmail(normalized);
            Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Firebase: Username/password sign-in failed.");
                    onComplete?.Invoke(false, "Kullanici adi veya sifre hatali.");
                    return;
                }

                FirebaseUser user = Auth.CurrentUser;
                SaveLocalUsername(cleanUsername, normalized);
                CurrentUser = user;
                CreateUserInFirestore(user, cleanUsername, normalized);
                EnsureUsernameDocument(user, cleanUsername, normalized);
                OnUserLogin?.Invoke(user);
                onComplete?.Invoke(true, null);
            });
        }

        private void RegisterAccountInternal(string cleanUsername, string normalized, string password, Action<bool, string> onComplete)
        {
            CreateOrLinkPasswordAccount(cleanUsername, normalized, password, onComplete);
        }

        private void CreateOrLinkPasswordAccount(string cleanUsername, string normalized, string password, Action<bool, string> onComplete)
        {
            string email = BuildAccountEmail(normalized);
            Credential credential = EmailAuthProvider.GetCredential(email, password);

            if (Auth.CurrentUser != null && Auth.CurrentUser.IsAnonymous)
            {
                Auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("Firebase: Anonymous account link with password failed.");
                        onComplete?.Invoke(false, "Bu kullanici adi alinmis veya sifre gecersiz.");
                        return;
                    }

                    CompletePasswordAccount(Auth.CurrentUser, cleanUsername, normalized, onComplete);
                });
                return;
            }

            Auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Firebase: Password account creation failed.");
                    onComplete?.Invoke(false, "Bu kullanici adi alinmis veya sifre gecersiz.");
                    return;
                }

                CompletePasswordAccount(Auth.CurrentUser, cleanUsername, normalized, onComplete);
            });
        }

        private void CompletePasswordAccount(FirebaseUser user, string cleanUsername, string normalized, Action<bool, string> onComplete)
        {
            if (user == null)
            {
                onComplete?.Invoke(false, "Kullanici oturumu bulunamadi.");
                return;
            }

            SaveLocalUsername(cleanUsername, normalized);
            CurrentUser = user;
            CreateUserInFirestore(user, cleanUsername, normalized);
            EnsureUsernameDocument(user, cleanUsername, normalized);
            OnUserLogin?.Invoke(user);
            onComplete?.Invoke(true, null);
        }

        private void EnsureUsernameDocument(FirebaseUser user, string cleanUsername, string normalized)
        {
            if (user == null || string.IsNullOrEmpty(normalized)) return;

            var usernameData = new Dictionary<string, object>
            {
                { "uid", user.UserId },
                { "username", cleanUsername },
                { "normalizedUsername", normalized },
                { "createdAt", FieldValue.ServerTimestamp }
            };

            FirebaseFirestore.DefaultInstance.Collection(UsernamesCollection).Document(normalized)
                .SetAsync(usernameData, SetOptions.MergeAll)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("Firebase: Username document write failed.");
                    }
                });
        }

        private void ReserveUsername(FirebaseUser user, string cleanUsername, string normalized, Action<bool, string> onComplete)
        {
            if (user == null)
            {
                onComplete?.Invoke(false, "Kullanici oturumu bulunamadi.");
                return;
            }

            var firestore = FirebaseFirestore.DefaultInstance;
            var usernameRef = firestore.Collection(UsernamesCollection).Document(normalized);
            var userRef = firestore.Collection(UsersCollection).Document(user.UserId);
            bool alreadyOwnedByThisUser = false;

            firestore.RunTransactionAsync(transaction =>
            {
                return transaction.GetSnapshotAsync(usernameRef).ContinueWith(snapshotTask =>
                {
                    var usernameSnapshot = snapshotTask.Result;
                    if (usernameSnapshot.Exists)
                    {
                        var existingData = usernameSnapshot.ToDictionary();
                        if (!existingData.TryGetValue("uid", out object existingUid) || existingUid == null || existingUid.ToString() != user.UserId)
                        {
                            throw new InvalidOperationException("Bu kullanici adi alinmis.");
                        }

                        alreadyOwnedByThisUser = true;
                    }

                    var usernameData = new Dictionary<string, object>
                    {
                        { "uid", user.UserId },
                        { "username", cleanUsername },
                        { "normalizedUsername", normalized },
                        { "createdAt", FieldValue.ServerTimestamp }
                    };

                    var userData = BuildUserData(user, cleanUsername, normalized);

                    if (!alreadyOwnedByThisUser)
                    {
                        transaction.Set(usernameRef, usernameData);
                    }

                    transaction.Set(userRef, userData, SetOptions.MergeAll);
                    return true;
                });
            }).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string message = IsUsernameTakenError(task.Exception) ? "Bu kullanici adi alinmis." : "Kullanici adi kaydedilemedi.";
                    Debug.LogError("Firebase: Username reservation failed.");
                    onComplete?.Invoke(false, message);
                    return;
                }

                SaveLocalUsername(cleanUsername, normalized);
                CurrentUser = user;
                OnUserLogin?.Invoke(user);
                Debug.Log("Firebase: Username reserved.");
                onComplete?.Invoke(true, null);
            });
        }

        private static bool IsUsernameTakenError(AggregateException exception)
        {
            if (exception == null) return false;

            foreach (var inner in exception.Flatten().InnerExceptions)
            {
                if (inner is InvalidOperationException && inner.Message.Contains("alinmis"))
                {
                    return true;
                }
            }

            return false;
        }

        public void CreateUserInFirestore(FirebaseUser user)
        {
            if (string.IsNullOrEmpty(NormalizedUsername)) return;
            CreateUserInFirestore(user, Username, NormalizedUsername);
        }

        private void CreateUserInFirestore(FirebaseUser user, string username, string normalizedUsername)
        {
            if (user == null || !IsInitialized || string.IsNullOrEmpty(normalizedUsername)) return;

            var docRef = FirebaseFirestore.DefaultInstance.Collection(UsersCollection).Document(user.UserId);
            docRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
            {
                int existingHighScore = 0;
                if (!readTask.IsCanceled && !readTask.IsFaulted && readTask.Result != null && readTask.Result.Exists)
                {
                    var existingData = readTask.Result.ToDictionary();
                    if (existingData.TryGetValue("highScore", out object scoreValue) && scoreValue != null)
                    {
                        try
                        {
                            existingHighScore = Convert.ToInt32(scoreValue);
                        }
                        catch
                        {
                            existingHighScore = 0;
                        }
                    }
                }

                docRef.SetAsync(BuildUserData(user, username, normalizedUsername), SetOptions.MergeAll).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("Firebase: User write to Firestore failed.");
                        return;
                    }

                    SyncPublicLeaderboardProfile(user, username, normalizedUsername, existingHighScore);
                    Debug.Log("Firebase: User document written to Firestore.");
                });
            });
        }

        private static Dictionary<string, object> BuildUserData(FirebaseUser user, string username, string normalizedUsername)
        {
            var userData = new Dictionary<string, object>
            {
                { "lastLogin", FieldValue.ServerTimestamp },
                { "isGuest", user.IsAnonymous },
                { "userId", user.UserId },
                { "username", username },
                { "normalizedUsername", normalizedUsername },
                { "providerId", user.IsAnonymous ? "anonymous" : "password" }
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                userData["accountEmail"] = user.Email;
            }

            return userData;
        }

        private void SaveLocalUsername(string username, string normalizedUsername)
        {
            Username = username;
            NormalizedUsername = normalizedUsername;
            PlayerPrefs.SetString(PrefUsername, username);
            PlayerPrefs.SetString(PrefNormalizedUsername, normalizedUsername);
            PlayerPrefs.Save();
        }

        private static string ValidateUsername(string requestedUsername, out string cleanUsername, out string normalized)
        {
            cleanUsername = (requestedUsername ?? string.Empty).Trim();
            normalized = NormalizeUsername(cleanUsername);

            if (cleanUsername.Length < 3)
            {
                return "Kullanici adi en az 3 karakter olmali.";
            }

            if (cleanUsername.Length > 16)
            {
                return "Kullanici adi en fazla 16 karakter olmali.";
            }

            if (normalized.Length != cleanUsername.Length)
            {
                return "Sadece harf, rakam ve alt cizgi kullan.";
            }

            return null;
        }

        private static string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                return "Sifre en az 6 karakter olmali.";
            }

            if (password.Length > 64)
            {
                return "Sifre en fazla 64 karakter olmali.";
            }

            return null;
        }

        private static string BuildAccountEmail(string normalizedUsername)
        {
            return $"{normalizedUsername}@{AccountEmailDomain}";
        }

        private static string NormalizeUsername(string value)
        {
            var builder = new StringBuilder();
            foreach (char c in value.ToLowerInvariant())
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public void SignOut()
        {
            if (!IsInitialized || Auth == null)
            {
                Debug.LogWarning("Firebase: Sign-out skipped because Firebase is not ready.");
                return;
            }

            if (Auth.CurrentUser == null)
            {
                CurrentUser = null;
                SaveLocalUsername(string.Empty, string.Empty);
                OnUserLogin?.Invoke(null);
                return;
            }

            Debug.Log("Firebase: Signing out current user.");
            Auth.SignOut();
            CurrentUser = null;
            SaveLocalUsername(string.Empty, string.Empty);
            OnUserLogin?.Invoke(null);
        }

        public void PostScore(int score)
        {
            if (!IsInitialized || Auth.CurrentUser == null || string.IsNullOrEmpty(NormalizedUsername)) return;

            string userId = Auth.CurrentUser.UserId;
            var docRef = FirebaseFirestore.DefaultInstance.Collection(UsersCollection).Document(userId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
            {
                int existingScore = 0;
                if (!readTask.IsCanceled && !readTask.IsFaulted && readTask.Result.Exists)
                {
                    var data = readTask.Result.ToDictionary();
                    if (data.TryGetValue("highScore", out object scoreValue))
                    {
                        existingScore = Convert.ToInt32(scoreValue);
                    }
                }

                int scoreToWrite = Math.Max(existingScore, score);
                var userData = new Dictionary<string, object>
                {
                    { "highScore", scoreToWrite },
                    { "lastPlayed", FieldValue.ServerTimestamp },
                    { "isGuest", Auth.CurrentUser.IsAnonymous },
                    { "username", Username },
                    { "normalizedUsername", NormalizedUsername },
                    { "providerId", Auth.CurrentUser.IsAnonymous ? "anonymous" : "password" }
                };

                if (!string.IsNullOrEmpty(Auth.CurrentUser.Email))
                {
                    userData["accountEmail"] = Auth.CurrentUser.Email;
                }

                docRef.SetAsync(userData, SetOptions.MergeAll).ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsCanceled || writeTask.IsFaulted)
                    {
                        Debug.LogError("Firebase: Score write to Firestore failed.");
                        return;
                    }

                    SyncPublicLeaderboardProfile(Auth.CurrentUser, Username, NormalizedUsername, scoreToWrite);
                    Debug.Log("Firebase: Score written to Firestore.");
                });
            });
        }

        private void SyncPublicLeaderboardProfile(FirebaseUser user, string username, string normalizedUsername, int highScore)
        {
            if (user == null || !IsInitialized || string.IsNullOrWhiteSpace(normalizedUsername))
                return;

            var publicDoc = FirebaseFirestore.DefaultInstance.Collection(PublicLeaderboardCollection).Document(user.UserId);
            if (user.IsAnonymous)
            {
                publicDoc.DeleteAsync().ContinueWithOnMainThread(task =>
                {
                    if (!task.IsCanceled && !task.IsFaulted)
                        return;

                    Debug.LogWarning("Firebase: Public leaderboard cleanup failed.");
                });
                return;
            }

            var publicData = new Dictionary<string, object>
            {
                { "userId", user.UserId },
                { "username", username },
                { "normalizedUsername", normalizedUsername },
                { "highScore", Mathf.Max(0, highScore) },
                { "providerId", "password" },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            publicDoc.SetAsync(publicData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Firebase: Public leaderboard write failed.");
                }
            });
        }
    }
}
