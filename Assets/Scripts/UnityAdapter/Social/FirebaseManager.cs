using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
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
        private const string WeeklyHighScoreField = "weeklyHighScore";
        private const string WeeklyScoreWeekIdField = "weeklyScoreWeekId";

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
        /// <summary>
        /// Fired when the user's highScore is changed remotely in Firestore.
        /// Parameter is the new highScore value.
        /// </summary>
        public event Action<int> OnRemoteScoreChanged;

        private Coroutine _initializationRoutine;
        private bool _subscribedToConsent;
        private bool? _lastAppliedAnalyticsCollectionState;
        private ListenerRegistration _scoreListener;
        private ListenerRegistration _publicLeaderboardScoreListener;
        private int _lastKnownRemoteScore = -1;
        private int _lastKnownPublicLeaderboardScore = -1;
        private bool _isMirroringRemoteScore;
        private int _pendingMirrorScore = -1;
        private string _pendingMirrorUserId;
        private string _pendingMirrorCollection;

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
            Debug.Log("[FirebaseManager] Starting Firebase dependency check...");
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                Debug.Log($"[FirebaseManager] Dependency check task completed. Status: {task.Status}");
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[FirebaseManager] Dependency check task failed: {task.Exception}");
                    return;
                }

                var dependencyStatus = task.Result;
                Debug.Log($"[FirebaseManager] Dependency status result: {dependencyStatus}");

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
            StopScoreListener();

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
                StopScoreListener();
                OnUserLogin?.Invoke(null);
                return;
            }

            Debug.Log($"Firebase: Auth user detected. anonymous={CurrentUser.IsAnonymous}");
            if (!string.IsNullOrEmpty(NormalizedUsername))
            {
                CreateUserInFirestore(CurrentUser, Username, NormalizedUsername);
            }
            else if (!CurrentUser.IsAnonymous)
            {
                HydrateUsernameFromFirestoreAsync(CurrentUser);
            }

            StartScoreListener(CurrentUser.UserId);
            OnUserLogin?.Invoke(CurrentUser);
        }

        private async void HydrateUsernameFromFirestoreAsync(FirebaseUser user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserId))
                return;

            try
            {
                DocumentSnapshot snapshot = await FirebaseFirestore.DefaultInstance
                    .Collection(UsersCollection)
                    .Document(user.UserId)
                    .GetSnapshotAsync();

                if (snapshot == null || !snapshot.Exists)
                    return;

                Dictionary<string, object> data = snapshot.ToDictionary();
                string username = ReadFieldString(data, "username");
                string normalized = ReadFieldString(data, "normalizedUsername");

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(normalized))
                    return;

                SaveLocalUsername(username, normalized);
                Debug.Log($"Firebase: Username hydrated from Firestore: {username}");
                CreateUserInFirestore(user, username, normalized);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Firebase: Username hydration failed. {ex.Message}");
            }
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
                int existingWeeklyHighScore = 0;
                string existingWeeklyWeekId = string.Empty;
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

                    existingWeeklyHighScore = ReadDictionaryInt(existingData, WeeklyHighScoreField, 0);
                    existingWeeklyWeekId = ReadFieldString(existingData, WeeklyScoreWeekIdField);
                }

                docRef.SetAsync(BuildUserData(user, username, normalizedUsername), SetOptions.MergeAll).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("Firebase: User write to Firestore failed.");
                        return;
                    }

                    string currentWeekId = GetCurrentUtcWeekId();
                    int weeklyHighScore = ResolveWeeklyScore(existingWeeklyHighScore, existingWeeklyWeekId, currentWeekId, 0);
                    SyncPublicLeaderboardProfile(user, username, normalizedUsername, existingHighScore, weeklyHighScore, currentWeekId);
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

        public void PostScore(int score, int rescueCount = 0, string gameMode = "Classic")
        {
            if (!IsInitialized || Auth == null)
                return;

            if (Auth.CurrentUser == null)
            {
                Debug.Log("Firebase: Score post skipped because there is no authenticated user session.");
                return;
            }

            if (Auth.CurrentUser.IsAnonymous)
            {
                Debug.Log("Firebase: Score post skipped for anonymous user.");
                return;
            }

            string userId = Auth.CurrentUser.UserId;
            var docRef = FirebaseFirestore.DefaultInstance.Collection(UsersCollection).Document(userId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
            {
                int existingScore = 0;
                int existingWeeklyScore = 0;
                string existingWeeklyWeekId = string.Empty;
                string resolvedUsername = Username;
                string resolvedNormalized = NormalizedUsername;

                if (!readTask.IsCanceled && !readTask.IsFaulted && readTask.Result != null && readTask.Result.Exists)
                {
                    var data = readTask.Result.ToDictionary();
                    if (data.TryGetValue("highScore", out object scoreValue) && scoreValue != null)
                    {
                        try { existingScore = Convert.ToInt32(scoreValue); } catch { }
                    }

                    existingWeeklyScore = ReadDictionaryInt(data, WeeklyHighScoreField, 0);
                    existingWeeklyWeekId = ReadFieldString(data, WeeklyScoreWeekIdField);

                    // If local username is missing, recover it from the users doc
                    if (string.IsNullOrWhiteSpace(resolvedUsername))
                        resolvedUsername = ReadFieldString(data, "username");
                    if (string.IsNullOrWhiteSpace(resolvedNormalized))
                        resolvedNormalized = ReadFieldString(data, "normalizedUsername");

                    // Persist recovered username locally so future calls don't need to re-fetch
                    if (!string.IsNullOrWhiteSpace(resolvedUsername) && !string.IsNullOrWhiteSpace(resolvedNormalized)
                        && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(NormalizedUsername)))
                    {
                        SaveLocalUsername(resolvedUsername, resolvedNormalized);
                    }
                }

                string currentWeekId = GetCurrentUtcWeekId();
                int scoreToWrite = Math.Max(existingScore, score);
                int weeklyScoreToWrite = ResolveWeeklyScore(existingWeeklyScore, existingWeeklyWeekId, currentWeekId, score);
                string displayName = ResolveLeaderboardUsername(resolvedUsername);
                string normalized = string.IsNullOrWhiteSpace(resolvedNormalized)
                    ? NormalizeUsername(displayName)
                    : resolvedNormalized;

                var userData = new Dictionary<string, object>
                {
                    { "highScore", scoreToWrite },
                    { WeeklyHighScoreField, weeklyScoreToWrite },
                    { WeeklyScoreWeekIdField, currentWeekId },
                    { "lastPlayed", FieldValue.ServerTimestamp },
                    { "isGuest", Auth.CurrentUser.IsAnonymous },
                    { "username", displayName },
                    { "normalizedUsername", normalized },
                    { "providerId", Auth.CurrentUser.IsAnonymous ? "anonymous" : "password" },
                    { "rescueCount", rescueCount },
                    { "gameMode", gameMode }
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

                    SyncPublicLeaderboardProfile(Auth.CurrentUser, displayName, normalized, scoreToWrite, weeklyScoreToWrite, currentWeekId, rescueCount, gameMode);
                    Debug.Log("Firebase: Score written to Firestore.");
                });
            });
        }

        public async Task<FirebaseUser> EnsureAuthenticatedForLeaderboardAsync()
        {
            if (!IsInitialized || Auth == null)
                return null;

            if (Auth.CurrentUser != null)
            {
                CurrentUser = Auth.CurrentUser;
                return CurrentUser;
            }

            try
            {
                await Auth.SignInAnonymouslyAsync();
                CurrentUser = Auth.CurrentUser;
                OnUserLogin?.Invoke(CurrentUser);
                return CurrentUser;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Firebase: Anonymous leaderboard auth failed. {ex.Message}");
                return null;
            }
        }

        private void SyncPublicLeaderboardProfile(FirebaseUser user, string username, string normalizedUsername, int highScore, int weeklyHighScore, string weeklyWeekId, int rescueCount = 0, string gameMode = "Classic")
        {
            if (user == null || !IsInitialized)
                return;

            string displayName = ResolveLeaderboardUsername(username);
            if (user.IsAnonymous || string.IsNullOrWhiteSpace(displayName))
                return;

            var publicDoc = FirebaseFirestore.DefaultInstance.Collection(PublicLeaderboardCollection).Document(user.UserId);
            string normalized = string.IsNullOrWhiteSpace(normalizedUsername)
                ? NormalizeUsername(displayName)
                : normalizedUsername;

            var publicData = new Dictionary<string, object>
            {
                { "userId", user.UserId },
                { "username", displayName },
                { "normalizedUsername", normalized },
                { "highScore", Mathf.Max(0, highScore) },
                { WeeklyHighScoreField, Mathf.Max(0, weeklyHighScore) },
                { WeeklyScoreWeekIdField, weeklyWeekId ?? string.Empty },
                { "isGuest", user.IsAnonymous },
                { "providerId", user.IsAnonymous ? "anonymous" : "password" },
                { "updatedAt", FieldValue.ServerTimestamp },
                { "rescueCount", rescueCount },
                { "gameMode", gameMode }
            };

            publicDoc.SetAsync(publicData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Firebase: Public leaderboard write failed.");
                }
            });
        }

        private static string ResolveLeaderboardUsername(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
                return username.Trim();

            return string.Empty;
        }

        // ────────────────────────────────────────────────────────────────
        // Real-time Firestore score listener
        // ────────────────────────────────────────────────────────────────

        private void StartScoreListener(string userId)
        {
            StopScoreListener();

            if (string.IsNullOrEmpty(userId) || !IsInitialized)
                return;

            _lastKnownRemoteScore = -1;
            _lastKnownPublicLeaderboardScore = -1;
            var docRef = FirebaseFirestore.DefaultInstance
                .Collection(UsersCollection)
                .Document(userId);

            _scoreListener = docRef.Listen(snapshot =>
            {
                if (snapshot == null || !snapshot.Exists)
                    return;

                var data = snapshot.ToDictionary();
                if (!data.TryGetValue("highScore", out object scoreValue) || scoreValue == null)
                    return;

                int remoteScore;
                try
                {
                    remoteScore = Convert.ToInt32(scoreValue);
                }
                catch
                {
                    return;
                }

                MirrorRemoteScoreAcrossCollections(userId, remoteScore, UsersCollection);
                EmitRemoteScoreChanged(remoteScore, ref _lastKnownRemoteScore, "users");
            });

            var publicDocRef = FirebaseFirestore.DefaultInstance
                .Collection(PublicLeaderboardCollection)
                .Document(userId);

            _publicLeaderboardScoreListener = publicDocRef.Listen(snapshot =>
            {
                if (snapshot == null || !snapshot.Exists)
                    return;

                var data = snapshot.ToDictionary();
                if (!data.TryGetValue("highScore", out object scoreValue) || scoreValue == null)
                    return;

                int remoteScore;
                try
                {
                    remoteScore = Convert.ToInt32(scoreValue);
                }
                catch
                {
                    return;
                }

                MirrorRemoteScoreAcrossCollections(userId, remoteScore, PublicLeaderboardCollection);
                EmitRemoteScoreChanged(remoteScore, ref _lastKnownPublicLeaderboardScore, "leaderboard_public");
            });

            Debug.Log($"Firebase: Score listener started for user {userId}");
        }

        private void StopScoreListener()
        {
            if (_scoreListener != null)
            {
                _scoreListener.Stop();
                _scoreListener = null;
            }

            if (_publicLeaderboardScoreListener != null)
            {
                _publicLeaderboardScoreListener.Stop();
                _publicLeaderboardScoreListener = null;
            }

            _lastKnownRemoteScore = -1;
            _lastKnownPublicLeaderboardScore = -1;
            Debug.Log("Firebase: Score listener stopped.");
        }

        private void EmitRemoteScoreChanged(int remoteScore, ref int lastKnownScore, string source)
        {
            if (lastKnownScore == remoteScore)
                return;

            bool isFirstCallback = lastKnownScore < 0;
            lastKnownScore = remoteScore;

            if (isFirstCallback)
                return;

            Debug.Log($"Firebase: Remote score changed from {source} to {remoteScore}");
            OnRemoteScoreChanged?.Invoke(remoteScore);
        }

        private async void MirrorRemoteScoreAcrossCollections(string userId, int remoteScore, string sourceCollection)
        {
            if (string.IsNullOrWhiteSpace(userId) || remoteScore < 0 || !IsInitialized)
                return;

            if (_isMirroringRemoteScore)
            {
                // A mirror is in flight — save this score so the finally block re-applies it
                _pendingMirrorScore = remoteScore;
                _pendingMirrorUserId = userId;
                _pendingMirrorCollection = sourceCollection;
                return;
            }

            try
            {
                _isMirroringRemoteScore = true;
                _pendingMirrorScore = -1;

                if (string.Equals(sourceCollection, UsersCollection, StringComparison.Ordinal))
                    await MirrorUsersScoreToPublicAsync(userId, remoteScore);
                else if (string.Equals(sourceCollection, PublicLeaderboardCollection, StringComparison.Ordinal))
                    await MirrorPublicScoreToUsersAsync(userId, remoteScore);
            }
            catch (Exception ex)
            {
                Exception inner = ex is AggregateException agg ? agg.Flatten().InnerException ?? ex : ex;
                Debug.LogWarning($"Firebase: Score mirror failed. {inner.Message}");
            }
            finally
            {
                _isMirroringRemoteScore = false;

                // Re-run if a score change arrived while we were mirroring
                if (_pendingMirrorScore >= 0)
                {
                    int pending = _pendingMirrorScore;
                    string pendingUser = _pendingMirrorUserId;
                    string pendingCol = _pendingMirrorCollection;
                    _pendingMirrorScore = -1;
                    MirrorRemoteScoreAcrossCollections(pendingUser, pending, pendingCol);
                }
            }
        }

        private async Task MirrorUsersScoreToPublicAsync(string userId, int remoteScore)
        {
            DocumentReference publicDoc = FirebaseFirestore.DefaultInstance
                .Collection(PublicLeaderboardCollection)
                .Document(userId);

            DocumentSnapshot publicSnapshot = await publicDoc.GetSnapshotAsync();
            int publicScore = ReadHighScore(publicSnapshot);
            if (publicScore == remoteScore)
                return;

            // Prefer the full sync path when we have a username — it writes all profile fields
            string resolvedUsername = Username;
            string resolvedNormalized = NormalizedUsername;
            DocumentSnapshot usersSnapshot = null;

            if (CurrentUser != null && !CurrentUser.IsAnonymous && CurrentUser.UserId == userId)
            {
                // If local username is missing, try to read it from the users doc we're mirroring from
                if (string.IsNullOrWhiteSpace(resolvedUsername))
                {
                    usersSnapshot = await FirebaseFirestore.DefaultInstance
                        .Collection(UsersCollection)
                        .Document(userId)
                        .GetSnapshotAsync();

                    if (usersSnapshot != null && usersSnapshot.Exists)
                    {
                        Dictionary<string, object> usersData = usersSnapshot.ToDictionary();
                        resolvedUsername = ReadFieldString(usersData, "username");
                        resolvedNormalized = ReadFieldString(usersData, "normalizedUsername");

                        if (!string.IsNullOrWhiteSpace(resolvedUsername) && !string.IsNullOrWhiteSpace(resolvedNormalized))
                            SaveLocalUsername(resolvedUsername, resolvedNormalized);
                    }
                }

                string displayName = ResolveLeaderboardUsername(resolvedUsername);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    if (usersSnapshot == null)
                    {
                        usersSnapshot = await FirebaseFirestore.DefaultInstance
                            .Collection(UsersCollection)
                            .Document(userId)
                            .GetSnapshotAsync();
                    }

                    int weeklyScore = ResolveWeeklyFieldsFromUsersSnapshot(usersSnapshot, remoteScore, out string weekId);
                    SyncPublicLeaderboardProfile(CurrentUser, displayName, resolvedNormalized, remoteScore, weeklyScore, weekId);
                    return;
                }
            }

            // Fallback: patch score only — keep existing profile fields via MergeAll
            var patch = new Dictionary<string, object>
            {
                { "userId", userId },
                { "highScore", Mathf.Max(0, remoteScore) },
                { WeeklyHighScoreField, Mathf.Max(0, remoteScore) },
                { WeeklyScoreWeekIdField, GetCurrentUtcWeekId() },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            await publicDoc.SetAsync(patch, SetOptions.MergeAll);
        }

        private async Task MirrorPublicScoreToUsersAsync(string userId, int remoteScore)
        {
            DocumentReference userDoc = FirebaseFirestore.DefaultInstance
                .Collection(UsersCollection)
                .Document(userId);

            DocumentSnapshot userSnapshot = await userDoc.GetSnapshotAsync();
            int userScore = ReadHighScore(userSnapshot);
            if (userScore == remoteScore)
                return;

            var patch = new Dictionary<string, object>
            {
                { "highScore", Mathf.Max(0, remoteScore) },
                { WeeklyHighScoreField, Mathf.Max(0, remoteScore) },
                { WeeklyScoreWeekIdField, GetCurrentUtcWeekId() },
                { "lastPlayed", FieldValue.ServerTimestamp }
            };

            await userDoc.SetAsync(patch, SetOptions.MergeAll);
        }

        private static int ReadHighScore(DocumentSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Exists)
                return 0;

            Dictionary<string, object> data = snapshot.ToDictionary();
            if (data == null || !data.TryGetValue("highScore", out object raw) || raw == null)
                return 0;

            try
            {
                return Convert.ToInt32(raw);
            }
            catch
            {
                return 0;
            }
        }

        private static int ResolveWeeklyFieldsFromUsersSnapshot(DocumentSnapshot usersSnapshot, int fallbackScore, out string weekId)
        {
            weekId = GetCurrentUtcWeekId();
            if (usersSnapshot == null || !usersSnapshot.Exists)
                return Mathf.Max(0, fallbackScore);

            Dictionary<string, object> usersData = usersSnapshot.ToDictionary();
            string storedWeekId = ReadFieldString(usersData, WeeklyScoreWeekIdField);
            int storedWeekly = ReadDictionaryInt(usersData, WeeklyHighScoreField, 0);

            if (!string.IsNullOrWhiteSpace(storedWeekId))
                weekId = storedWeekId;

            return ResolveWeeklyScore(storedWeekly, storedWeekId, weekId, fallbackScore);
        }

        private static int ResolveWeeklyScore(int existingWeeklyScore, string existingWeekId, string currentWeekId, int fallbackScore)
        {
            if (string.Equals(existingWeekId, currentWeekId, StringComparison.Ordinal))
                return Math.Max(existingWeeklyScore, fallbackScore);

            return Math.Max(0, fallbackScore);
        }

        private static string GetCurrentUtcWeekId()
        {
            DateTime utcNow = DateTime.UtcNow;
            int offsetToMonday = ((int)utcNow.DayOfWeek + 6) % 7;
            DateTime weekStart = utcNow.Date.AddDays(-offsetToMonday);
            return weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static int ReadDictionaryInt(Dictionary<string, object> data, string key, int fallback)
        {
            if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
                return fallback;

            try
            {
                return Convert.ToInt32(raw);
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadFieldString(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
                return string.Empty;

            return raw.ToString();
        }
    }
}
