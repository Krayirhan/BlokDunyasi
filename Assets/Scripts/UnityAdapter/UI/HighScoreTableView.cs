using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.UnityAdapter;
using BlockPuzzle.UnityAdapter.Social;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.UnityAdapter.UI.Localization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// Scores screen render owner for header, podium and leaderboard list presentation.
    /// Progress/reward state ownership does not live in this component.
    /// </summary>
    public class HighScoreTableView : MonoBehaviour, ILanguageListener
    {
        private const string PublicLeaderboardCollection = "leaderboard_public";
        private const string UsersCollection = "users";
        private const string RichRootName = "RichScoresRoot";
        private const string MarkerName = "ScoresPodiumV11";
        private static readonly CultureInfo ScoreCulture = CultureInfo.GetCultureInfo("tr-TR");

        [Header("Legacy")]
        [SerializeField] private TextMeshProUGUI[] entries;
        [SerializeField] private bool useWeeklyLeaderboard = false;
        [SerializeField] private string emptyEntryText = "-";
        [SerializeField] [Range(1, 20)] private int maxEntries = 6;
        [SerializeField] private float singleEntryAnchoredX;

        [Header("Remote Leaderboard")]
        [SerializeField] private string loadingText = "Yukleniyor...";
        [SerializeField] private string currentPlayerLabel = "Sen";
        [SerializeField] private Color currentPlayerColor = new Color(1f, 0.86f, 0.32f, 1f);

        private const float RetryDelay = 3f;
        private const int MaxRetries = 5;

        private readonly List<LeaderboardRow> _cachedRows = new List<LeaderboardRow>();
        private readonly List<ListRowUi> _listRows = new List<ListRowUi>();
        private readonly List<Coroutine> _retryCoroutines = new List<Coroutine>();
        private BestScoreStore _bestScoreStore;
        private bool _refreshInProgress;
        private bool _listenersBound;
        private Coroutine _bootstrapCoroutine;
        private int _retryCount;
        private RectTransform _rectTransform;
        private RectTransform _richRoot;
        private Button _homeButton;
        private TMP_FontAsset _titleFont;
        private TMP_FontAsset _bodyFont;
        private PodiumSlotUi _firstSlot;
        private PodiumSlotUi _secondSlot;
        private PodiumSlotUi _thirdSlot;

        private sealed class LeaderboardRow
        {
            public string UserId;
            public string PlayerName;
            public int Score;
            public int Rank;
            public bool IsCurrentPlayer;
        }

        private sealed class PodiumSlotUi
        {
            public RectTransform Root;
            public Image Ring;
            public Image AvatarFill;
            public TextMeshProUGUI InitialsText;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI ScoreText;
            public Image PodiumFill;
            public TextMeshProUGUI RankText;
        }

        private sealed class ListRowUi
        {
            public RectTransform Root;
            public Image Background;
            public Image RankBadge;
            public TextMeshProUGUI RankText;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI ScoreText;
        }

        private void Awake()
        {
            EnsureHierarchy();
        }

        private void OnEnable()
        {
            EnsureHierarchy();
            BindHomeButton();

            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Subscribe(this);

            if (Application.isPlaying)
            {
                _retryCount = 0;
                _refreshInProgress = false;

                // Subscribe synchronously so we never miss an event that fires before the coroutine runs
                SubscribeFirebaseEvents();

                FirebaseManager firebase = FirebaseManager.Instance;
                if (firebase != null && firebase.IsInitialized)
                {
                    // Firebase already ready — refresh immediately, no bootstrap needed
                    Refresh();
                }
                else if (firebase == null)
                {
                    // FirebaseManager not in scene yet — poll for it via coroutine
                    StartBootstrap();
                }
                // else: firebase exists but initializing — HandleFirebaseInitialized will call Refresh()

                RefreshLocalizedTexts();
            }
            else
            {
                ApplyRows(new List<LeaderboardRow>());
                RefreshLocalizedTexts();
            }
        }

        private void OnDisable()
        {
            _listenersBound = false;
            _refreshInProgress = false;
            StopBootstrap();
            StopAllRetryCoroutines();
            UnsubscribeFirebaseEvents();
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Unsubscribe(this);
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            RefreshLocalizedTexts();
            if (Application.isPlaying)
                Refresh();
        }

        // ── Bootstrap ────────────────────────────────────────────────────

        private void StartBootstrap()
        {
            StopBootstrap();
            _bootstrapCoroutine = StartCoroutine(BootstrapCoroutine());
        }

        private void StopBootstrap()
        {
            if (_bootstrapCoroutine != null)
            {
                StopCoroutine(_bootstrapCoroutine);
                _bootstrapCoroutine = null;
            }
        }

        private IEnumerator BootstrapCoroutine()
        {
            // Only called when FirebaseManager.Instance was null at OnEnable time.
            // Poll until the singleton appears (max 10s).
            float waited = 0f;
            while (FirebaseManager.Instance == null && waited < 10f)
            {
                yield return new WaitForSeconds(0.1f);
                waited += 0.1f;
            }

            // Subscribe now that the instance may be available
            SubscribeFirebaseEvents();

            FirebaseManager firebase = FirebaseManager.Instance;
            if (firebase == null)
            {
                Debug.LogWarning("[HighScoreTableView] FirebaseManager not found after 10s — scheduling retry.");
                _bootstrapCoroutine = null;
                // Schedule a retry: Refresh() returns early if still not ready, but keeps retrying
                if (_retryCount < MaxRetries)
                {
                    _retryCount++;
                    ScheduleRetry();
                }
                yield break;
            }

            if (firebase.IsInitialized)
                Refresh();
            // else: HandleFirebaseInitialized will fire once SDK is ready

            _bootstrapCoroutine = null;
        }

        // ── Firebase event subscriptions ─────────────────────────────────

        private void SubscribeFirebaseEvents()
        {
            if (!Application.isPlaying)
                return;

            FirebaseManager firebase = FirebaseManager.Instance;
            if (firebase == null)
                return;

            firebase.OnFirebaseInitialized -= HandleFirebaseInitialized;
            firebase.OnFirebaseInitialized += HandleFirebaseInitialized;
            firebase.OnRemoteScoreChanged -= HandleRemoteScoreChanged;
            firebase.OnRemoteScoreChanged += HandleRemoteScoreChanged;
            firebase.OnUserLogin -= HandleUserLoginChanged;
            firebase.OnUserLogin += HandleUserLoginChanged;
        }

        private void UnsubscribeFirebaseEvents()
        {
            FirebaseManager firebase = FirebaseManager.Instance;
            if (firebase == null)
                return;

            firebase.OnFirebaseInitialized -= HandleFirebaseInitialized;
            firebase.OnRemoteScoreChanged -= HandleRemoteScoreChanged;
            firebase.OnUserLogin -= HandleUserLoginChanged;
        }

        private void HandleFirebaseInitialized()
        {
            _retryCount = 0;
            Refresh();
        }

        private void HandleRemoteScoreChanged(int _) => Refresh();

        private void HandleUserLoginChanged(Firebase.Auth.FirebaseUser _)
        {
            _retryCount = 0;
            Refresh();
        }

        private void RefreshLocalizedTexts()
        {
            if (_richRoot == null)
                return;

            if (LanguageManager.Instance == null)
                return;

            var lang = LanguageManager.Instance.CurrentLanguage;
            
            // Localize title text
            var titleText = FindByPath<TextMeshProUGUI>(_richRoot, "SafeRoot/ContentRoot/Header/TitleText");
            if (titleText != null)
            {
                if (lang == LanguageManager.Language.Korean) titleText.text = "최고 점수";
                else if (lang == LanguageManager.Language.English) titleText.text = "High Scores";
                else titleText.text = "En Yüksek Skorlar";
            }

            // Localize home button label
            var homeLabel = FindByPath<TextMeshProUGUI>(_richRoot, "SafeRoot/ContentRoot/HomeButton/LabelText");
            if (homeLabel != null)
            {
                if (lang == LanguageManager.Language.Korean) homeLabel.text = "뒤로";
                else if (lang == LanguageManager.Language.English) homeLabel.text = "Back";
                else homeLabel.text = "Geri";
            }

            // Localize podium loading/waiting labels
            if (_firstSlot != null && _firstSlot.NameText != null)
            {
                string text = _firstSlot.NameText.text;
                if (text == "Bekleniyor" || text == "Waiting" || text == "대기 중")
                    _firstSlot.NameText.text = lang == LanguageManager.Language.Korean ? "대기 중" : (lang == LanguageManager.Language.English ? "Waiting" : "Bekleniyor");
                else if (text == "Yukleniyor..." || text == "Loading..." || text == "로딩 중...")
                    _firstSlot.NameText.text = lang == LanguageManager.Language.Korean ? "로딩 중..." : (lang == LanguageManager.Language.English ? "Loading..." : "Yükleniyor...");
            }
            if (_secondSlot != null && _secondSlot.NameText != null)
            {
                string text = _secondSlot.NameText.text;
                if (text == "Bekleniyor" || text == "Waiting" || text == "대기 중")
                    _secondSlot.NameText.text = lang == LanguageManager.Language.Korean ? "대기 중" : (lang == LanguageManager.Language.English ? "Waiting" : "Bekleniyor");
                else if (text == "Yukleniyor..." || text == "Loading..." || text == "로딩 중...")
                    _secondSlot.NameText.text = lang == LanguageManager.Language.Korean ? "로딩 중..." : (lang == LanguageManager.Language.English ? "Loading..." : "Yükleniyor...");
            }
            if (_thirdSlot != null && _thirdSlot.NameText != null)
            {
                string text = _thirdSlot.NameText.text;
                if (text == "Bekleniyor" || text == "Waiting" || text == "대기 중")
                    _thirdSlot.NameText.text = lang == LanguageManager.Language.Korean ? "대기 중" : (lang == LanguageManager.Language.English ? "Waiting" : "Bekleniyor");
                else if (text == "Yukleniyor..." || text == "Loading..." || text == "로딩 중...")
                    _thirdSlot.NameText.text = lang == LanguageManager.Language.Korean ? "로딩 중..." : (lang == LanguageManager.Language.English ? "Loading..." : "Yükleniyor...");
            }
        }

        private void OnValidate()
        {
            maxEntries = Mathf.Clamp(maxEntries, 6, 20);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                EnsureHierarchy();
                return;
            }

            EditorApplication.delayCall -= HandleEditorRefresh;
            EditorApplication.delayCall += HandleEditorRefresh;
#endif
        }

#if UNITY_EDITOR
        private void HandleEditorRefresh()
        {
            if (this == null)
                return;

            EnsureHierarchy();
            ApplyRows(new List<LeaderboardRow>());
        }
#endif

        public async void Refresh()
        {
            // Refresh is async and can outlive a scene change. Unity overloads
            // == for destroyed objects, so this guard must be before any member access.
            if (this == null || !isActiveAndEnabled)
                return;

            if (_refreshInProgress)
                return;

            _refreshInProgress = true;

            try
            {
                EnsureHierarchy();
                SetLoadingState();

                List<LeaderboardRow> rows = await BuildLeaderboardRowsAsync();

                // The scene may have been unloaded while Firestore was awaiting.
                if (this == null || !isActiveAndEnabled)
                    return;

                _cachedRows.Clear();
                _cachedRows.AddRange(rows);
                ApplyRows(_cachedRows);

                // If we got no remote rows and retries remain, schedule another attempt.
                // This handles transient network errors that are caught silently inside the fetch.
                bool hasRemoteRows = rows.Any(r => !r.IsCurrentPlayer);
                if (!hasRemoteRows && _retryCount < MaxRetries)
                {
                    _retryCount++;
                    ScheduleRetry();
                }
                else if (hasRemoteRows)
                {
                    _retryCount = 0;
                }
            }
            catch (Exception ex)
            {
                if (this == null || !isActiveAndEnabled)
                    return;

                if (IsPermissionDenied(ex))
                    Debug.Log("[HighScoreTableView] Firestore leaderboard read is blocked by rules.");
                else
                    Debug.LogWarning($"[HighScoreTableView] Leaderboard refresh failed: {ex.Message}");

                ApplyRows(new List<LeaderboardRow>());

                if (_retryCount < MaxRetries)
                {
                    _retryCount++;
                    ScheduleRetry();
                }
            }
            finally
            {
                if (this != null)
                    _refreshInProgress = false;
            }
        }

        private void ScheduleRetry()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            Coroutine c = StartCoroutine(RetryAfterDelay());
            _retryCoroutines.Add(c);
        }

        private void StopAllRetryCoroutines()
        {
            foreach (Coroutine c in _retryCoroutines)
            {
                if (c != null)
                    StopCoroutine(c);
            }
            _retryCoroutines.Clear();
        }

        private IEnumerator RetryAfterDelay()
        {
            yield return new WaitForSeconds(RetryDelay);
            _retryCoroutines.RemoveAll(c => c == null);
            if (gameObject.activeInHierarchy)
                Refresh();
        }

        private void EnsureHierarchy()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            _titleFont = Resources.Load<TMP_FontAsset>("TMP/LuckiestGuy-Regular Combo SDF") ?? Localization.LocalizedFontUtility.GetDefaultLatinTmpFont() ?? TMP_Settings.defaultFontAsset;
            _bodyFont = Localization.LocalizedFontUtility.GetDefaultLatinTmpFont() ?? _titleFont ?? TMP_Settings.defaultFontAsset;

            var safeAreaFitter = GetComponent("SafeAreaFitter") as Behaviour;
            if (safeAreaFitter != null)
                safeAreaFitter.enabled = false;
            Stretch(_rectTransform);
            _rectTransform.SetAsLastSibling();

            DisableLegacyChildren();
            ResetCachedUiReferences();

            Transform existing = transform.Find(RichRootName);
            if (existing != null && existing.Find(MarkerName) != null)
            {
                _richRoot = existing as RectTransform;
            }
            else
            {
                if (existing != null)
                {
                    if (Application.isPlaying)
                        Destroy(existing.gameObject);
                    else
                        DestroyImmediate(existing.gameObject);
                }

                _richRoot = CreateRect(RichRootName, transform);
                Stretch(_richRoot);
                CreateRect(MarkerName, _richRoot).gameObject.SetActive(false);
                BuildRichLayout(_richRoot);
            }

            CacheReferences();
            EnsureBackdropSprite();
            EnsureHomeButtonLayout();
            BindHomeButton();
            RefreshLocalizedTexts();
        }

        private void ResetCachedUiReferences()
        {
            _richRoot = null;
            _homeButton = null;
            _firstSlot = null;
            _secondSlot = null;
            _thirdSlot = null;
            _listRows.Clear();
            _listenersBound = false;
        }

        private void DisableLegacyChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (string.Equals(child.name, RichRootName, StringComparison.Ordinal))
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        private void BuildRichLayout(RectTransform root)
        {
            BuildBackdrop(root);
            RectTransform safeRoot = CreateRect("SafeRoot", root);
            Stretch(safeRoot, 0f, 0f, 0f, 0f);

            RectTransform content = CreateRect("ContentRoot", safeRoot);
            Stretch(content, 24f, 24f, 96f, 40f);

            BuildHeader(content);
            BuildPodium(content);
            BuildList(content);
            BuildHomeButton(content);
        }

        private void BuildBackdrop(RectTransform root)
        {
            Image baseFill = CreateImage("BaseFill", root, Color.white, false, false, 0f);
            Stretch(baseFill.rectTransform, -400f, -400f, -600f, -600f);

            // Subtle dark overlay to let podium and text elements pop with clear contrast
            Image innerTint = CreateImage("InnerTint", root, new Color(0.02f, 0.08f, 0.22f, 0.45f), false, false, 0f);
            Stretch(innerTint.rectTransform, -400f, -400f, -600f, -600f);

            EnsureBackdropSprite();
        }

        private void EnsureBackdropSprite()
        {
            if (_richRoot == null) return;
            Transform baseFillTransform = _richRoot.Find("BaseFill");
            if (baseFillTransform != null)
            {
                Image img = baseFillTransform.GetComponent<Image>();
                if (img != null)
                {
                    Sprite bgSprite = null;
#if UNITY_EDITOR
                    bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/mainmenu_background.png");
#endif
                    if (bgSprite == null)
                        bgSprite = Resources.Load<Sprite>("mainmenu_background");

                    if (bgSprite != null)
                    {
                        img.sprite = bgSprite;
                        img.type = Image.Type.Simple;
                        img.color = Color.white;
                    }
                }
            }

            Transform innerTintTransform = _richRoot.Find("InnerTint");
            if (innerTintTransform != null)
            {
                Image tint = innerTintTransform.GetComponent<Image>();
                if (tint != null)
                {
                    tint.color = new Color(0.02f, 0.08f, 0.22f, 0.45f);
                }
            }
        }

        private static Sprite LoadLeaderboardSprite(string spriteName)
        {
#if UNITY_EDITOR
            string assetPath = $"Assets/Images/UI/Leaderboard/{spriteName}.png";
            var editorAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (editorAssets != null && editorAssets.Length > 0)
            {
                Sprite best = null;
                float maxArea = 0f;
                foreach (var obj in editorAssets)
                {
                    if (obj is Sprite s)
                    {
                        float area = s.rect.width * s.rect.height;
                        if (area > maxArea)
                        {
                            maxArea = area;
                            best = s;
                        }
                    }
                }
                if (best != null && maxArea > 2500f)
                    return best;
            }

            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
#endif
            Sprite[] resourcesSprites = Resources.LoadAll<Sprite>($"Leaderboard/{spriteName}");
            if (resourcesSprites != null && resourcesSprites.Length > 0)
            {
                Sprite best = null;
                float maxArea = 0f;
                foreach (var s in resourcesSprites)
                {
                    if (s != null)
                    {
                        float area = s.rect.width * s.rect.height;
                        if (area > maxArea)
                        {
                            maxArea = area;
                            best = s;
                        }
                    }
                }
                if (best != null && maxArea > 2500f)
                    return best;
            }

            Sprite singleSprite = Resources.Load<Sprite>($"Leaderboard/{spriteName}");
            if (singleSprite != null)
                return singleSprite;

            Texture2D resTex = Resources.Load<Texture2D>($"Leaderboard/{spriteName}");
            if (resTex != null)
            {
                return Sprite.Create(resTex, new Rect(0f, 0f, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            return null;
        }

        private void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateRect("Header", parent);
            SetRect(header, new Vector2(0.5f, 1f), new Vector2(860f, 180f), new Vector2(0f, 0f), new Vector2(0.5f, 1f));

            var trophySprite = LoadLeaderboardSprite("header_trophy");
            if (trophySprite != null)
            {
                Image trophyImg = CreateImage("TrophyImage", header, Color.white, false, false, 0f);
                trophyImg.sprite = trophySprite;
                trophyImg.type = Image.Type.Simple;
                trophyImg.preserveAspect = true;
                SetRect(trophyImg.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 110f), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
                AddShadow(trophyImg.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -4f));
            }
            else
            {
                TextMeshProUGUI trophyText = CreateText("TrophyText", header, "\u2605", _bodyFont, 80f, new Color(1f, 0.78f, 0.16f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(trophyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 80f), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
                AddShadow(trophyText.gameObject, new Color(0.58f, 0.28f, 0.02f, 0.5f), new Vector2(0f, -4f));
            }

            TextMeshProUGUI titleText = CreateText("TitleText", header, "En Yüksek Skorlar", _titleFont, 44f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 30f;
            titleText.fontSizeMax = 46f;
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(750f, 50f), new Vector2(0f, -120f), new Vector2(0.5f, 1f));
            AddShadow(titleText.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -4f));
        }

        private void BuildPodium(RectTransform parent)
        {
            RectTransform podiumRoot = CreateRect("PodiumRoot", parent);
            SetRect(podiumRoot, new Vector2(0.5f, 1f), new Vector2(940f, 720f), new Vector2(0f, -190f), new Vector2(0.5f, 1f));

            _secondSlot = BuildPodiumSlot(podiumRoot, "SecondSlot", new Vector2(-305f, 0f), new Vector2(226f, 290f), 2, new Color(0.83f, 0.86f, 0.93f), new Color(0.36f, 0.48f, 0.76f), new Color(0.66f, 0.7f, 0.8f));
            _firstSlot  = BuildPodiumSlot(podiumRoot, "FirstSlot",  new Vector2(0f, 0f),    new Vector2(270f, 380f), 1, new Color(1f, 0.8f, 0.08f),   new Color(0.95f, 0.66f, 0.08f), new Color(1f, 0.86f, 0.28f));
            _thirdSlot  = BuildPodiumSlot(podiumRoot, "ThirdSlot",  new Vector2(305f, 0f),  new Vector2(224f, 240f), 3, new Color(1f, 0.62f, 0.12f),  new Color(0.72f, 0.4f, 0.12f),  new Color(0.92f, 0.62f, 0.22f));
        }

        private PodiumSlotUi BuildPodiumSlot(RectTransform parent, string name, Vector2 position, Vector2 pedestalSize, int rank, Color ringColor, Color pedestalColor, Color glowColor)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0.5f, 0f), new Vector2(270f, 700f), position, new Vector2(0.5f, 0f));

            float top = pedestalSize.y;

            // 3D Podium Block Sprite (Using its native proportions)
            Image pedestal = CreateImage("Pedestal", root, Color.white, false, false, 0f);
            string podiumSpriteName = rank == 1 ? "podium_1st" : (rank == 2 ? "podium_2nd" : "podium_3rd");
            var pSprite = LoadLeaderboardSprite(podiumSpriteName);
            if (pSprite != null)
            {
                pedestal.sprite = pSprite;
                pedestal.type = Image.Type.Simple;
                pedestal.preserveAspect = true;
                pedestal.color = Color.white;
            }
            else
            {
                pedestal.color = pedestalColor;
            }
            SetRect(pedestal.rectTransform, new Vector2(0.5f, 0f), pedestalSize, new Vector2(0f, 0f), new Vector2(0.5f, 0f));
            AddShadow(pedestal.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -8f));

            // RankText is hidden when using 3D podium sprite because number is drawn in the art
            TextMeshProUGUI rankText = CreateText("RankText", pedestal.rectTransform, "", _titleFont, 1f, Color.clear, FontStyles.Bold, TextAlignmentOptions.Center);
            if (pSprite != null)
            {
                rankText.gameObject.SetActive(false);
            }
            else
            {
                rankText.text = rank.ToString(CultureInfo.InvariantCulture);
                rankText.fontSize = rank == 1 ? 92f : 72f;
                rankText.color = new Color(1f, 0.98f, 0.9f, 1f);
                Stretch(rankText.rectTransform);
            }

            // Player Score (Generous vertical spacing and wider box above pedestal)
            TextMeshProUGUI scoreText = CreateText("ScoreText", root, "0", _bodyFont, rank == 1 ? 40f : 34f, rank == 1 ? new Color(1f, 0.88f, 0.2f, 1f) : Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMin = 20f;
            scoreText.fontSizeMax = rank == 1 ? 42f : 36f;
            SetRect(scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(260f, 44f), new Vector2(0f, top + 14f), new Vector2(0.5f, 0f));
            AddShadow(scoreText.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -3f));

            // Player Name (Prominent, spacious and clearly readable)
            TextMeshProUGUI nameText = CreateText("NameText", root, "Bekleniyor", _bodyFont, rank == 1 ? 30f : 26f, new Color(0.9f, 0.95f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 32f;
            SetRect(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(260f, 40f), new Vector2(0f, top + 64f), new Vector2(0.5f, 0f));
            AddShadow(nameText.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -2f));

            // Avatar Ring / Frame (Resting comfortably above the name)
            Vector2 ringSize = rank == 1 ? new Vector2(140f, 140f) : new Vector2(120f, 120f);
            Image ring = CreateImage("AvatarRing", root, Color.white, false, false, 0f);
            SetRect(ring.rectTransform, new Vector2(0.5f, 0f), ringSize, new Vector2(0f, top + 114f), new Vector2(0.5f, 0f));

            string frameName = rank == 1 ? "frame_gold" : (rank == 2 ? "frame_silver" : "frame_bronze");
            var frameSprite = LoadLeaderboardSprite(frameName);
            if (frameSprite != null)
            {
                ring.sprite = frameSprite;
                ring.preserveAspect = true;
                ring.color = Color.white;
            }
            else
            {
                var goldFrameFallback = LoadLeaderboardSprite("frame_gold");
                if (goldFrameFallback != null)
                {
                    ring.sprite = goldFrameFallback;
                    ring.preserveAspect = true;
                    ring.color = rank == 2 ? new Color(0.85f, 0.9f, 1f) : (rank == 3 ? new Color(1f, 0.78f, 0.55f) : Color.white);
                }
                else
                {
                    ring.color = ringColor;
                }
            }

            Image avatarFill = CreateCircleImage("AvatarFill", ring.rectTransform, new Color(0.06f, 0.18f, 0.42f, 1f), 1024, 2f);
            Stretch(avatarFill.rectTransform, 14f, 14f, 14f, 14f);

            TextMeshProUGUI initialsText = CreateText("InitialsText", ring.rectTransform, "--", _bodyFont, rank == 1 ? 50f : 42f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(initialsText.rectTransform);

            // Crown for 1st place (Resting right on top of the golden frame)
            if (rank == 1)
            {
                var crownSprite = LoadLeaderboardSprite("crown_gold");
                if (crownSprite != null)
                {
                    Image crown = CreateImage("Crown", root, Color.white, false, false, 0f);
                    crown.sprite = crownSprite;
                    crown.type = Image.Type.Simple;
                    crown.preserveAspect = true;
                    SetRect(crown.rectTransform, new Vector2(0.5f, 0f), new Vector2(84f, 84f), new Vector2(0f, top + 114f + ringSize.y - 18f), new Vector2(0.5f, 0f));
                }
            }

            return new PodiumSlotUi
            {
                Root = root,
                Ring = ring,
                AvatarFill = avatarFill,
                InitialsText = initialsText,
                NameText = nameText,
                ScoreText = scoreText,
                PodiumFill = pedestal,
                RankText = rankText
            };
        }

        private void BuildList(RectTransform parent)
        {
            RectTransform listRoot = CreateRect("ListRoot", parent);
            SetRect(listRoot, new Vector2(0.5f, 1f), new Vector2(920f, 520f), new Vector2(0f, -940f), new Vector2(0.5f, 1f));

            int rowCount = Mathf.Max(3, Mathf.Max(6, maxEntries) - 3);
            for (int i = 0; i < rowCount; i++)
            {
                float y = -i * 156f;
                _listRows.Add(BuildListRow(listRoot, $"Row{i + 4}", new Vector2(0f, y)));
            }
        }

        private ListRowUi BuildListRow(RectTransform parent, string name, Vector2 position)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0.5f, 1f), new Vector2(890f, 138f), position, new Vector2(0.5f, 1f));

            Image background = CreateImage("Background", root, Color.white, false, false, 0f);
            var rowSprite = LoadLeaderboardSprite("row_bg_normal");
            if (rowSprite != null)
            {
                background.sprite = rowSprite;
                background.type = Image.Type.Simple;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.03f, 0.13f, 0.35f, 0.92f);
            }
            Stretch(background.rectTransform);
            AddShadow(background.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -6f));

            // Rank Badge: Concentric medallion perfectly centered inside the left rounded dome (x = 72)
            Image rankBadge = CreateCircleImage("RankBadge", root, new Color(0.04f, 0.14f, 0.34f, 1f), 256, 1.5f);
            SetRect(rankBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(74f, 74f), new Vector2(72f, 0f), new Vector2(0.5f, 0.5f));
            AddShadow(rankBadge.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -3f));

            // Badge border ring for high contrast
            Image rankRing = CreateCircleImage("RankRing", rankBadge.rectTransform, new Color(0.3f, 0.65f, 1f, 0.75f), 256, 1.5f);
            Stretch(rankRing.rectTransform);
            Image rankInner = CreateCircleImage("RankInner", rankBadge.rectTransform, new Color(0.04f, 0.14f, 0.34f, 1f), 256, 1.5f);
            Stretch(rankInner.rectTransform, 3f, 3f, 3f, 3f);

            // Rank Text: Centered with mathematical precision in the center of the medallion
            TextMeshProUGUI rankText = CreateText("RankText", rankBadge.rectTransform, "-", _titleFont, 36f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            rankText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rankText.verticalAlignment = VerticalAlignmentOptions.Middle;
            rankText.enableAutoSizing = true;
            rankText.fontSizeMin = 22f;
            rankText.fontSizeMax = 38f;
            SetRect(rankText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(68f, 68f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f));
            AddShadow(rankText.gameObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -2f));

            // Player Name: Generous width, left-aligned, vertically centered
            TextMeshProUGUI nameText = CreateText("NameText", root, "-", _bodyFont, 32f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            nameText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 20f;
            nameText.fontSizeMax = 34f;
            SetRect(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(450f, 54f), new Vector2(130f, 0f), new Vector2(0f, 0.5f));

            // Score Text: Right-aligned, vertically centered, bold white
            TextMeshProUGUI scoreText = CreateText("ScoreText", root, "-", _bodyFont, 34f, new Color(0.98f, 0.99f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Right);
            scoreText.verticalAlignment = VerticalAlignmentOptions.Middle;
            scoreText.horizontalAlignment = HorizontalAlignmentOptions.Right;
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMin = 22f;
            scoreText.fontSizeMax = 36f;
            SetRect(scoreText.rectTransform, new Vector2(1f, 0.5f), new Vector2(250f, 54f), new Vector2(-50f, 0f), new Vector2(1f, 0.5f));

            return new ListRowUi
            {
                Root = root,
                Background = background,
                RankBadge = rankBadge,
                RankText = rankText,
                NameText = nameText,
                ScoreText = scoreText
            };
        }

        private void BuildHomeButton(RectTransform parent)
        {
            RectTransform buttonRoot = CreateRect("HomeButton", parent);
            SetRect(buttonRoot, new Vector2(0f, 1f), new Vector2(96f, 96f), new Vector2(16f, -12f), new Vector2(0f, 1f));

            // Transparent hit area to guarantee clicks are always captured across the entire 96x96 rect
            Image hitArea = buttonRoot.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            Image buttonImage = CreateImage("ButtonFace", buttonRoot, Color.white, true, false, 0f);
            buttonImage.raycastTarget = true;
            var backSprite = LoadLeaderboardSprite("btn_back");
            if (backSprite != null)
            {
                buttonImage.sprite = backSprite;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = true;
            }
            else
            {
                buttonImage.color = new Color(0.13f, 0.43f, 0.98f, 1f);
            }
            Stretch(buttonImage.rectTransform);
            AddShadow(buttonImage.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -6f));

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = hitArea;

            _homeButton = button;
            BindHomeButton();
        }

        private void CacheReferences()
        {
            if (_richRoot == null)
                return;

            _homeButton = FindByPath<Button>(_richRoot, "SafeRoot/ContentRoot/HomeButton");
            _firstSlot = _firstSlot ?? FindPodiumSlot("SafeRoot/ContentRoot/PodiumRoot/FirstSlot");
            _secondSlot = _secondSlot ?? FindPodiumSlot("SafeRoot/ContentRoot/PodiumRoot/SecondSlot");
            _thirdSlot = _thirdSlot ?? FindPodiumSlot("SafeRoot/ContentRoot/PodiumRoot/ThirdSlot");

            if (_listRows.Count == 0)
            {
                Transform listRoot = _richRoot.Find("SafeRoot/ContentRoot/ListRoot");
                if (listRoot != null)
                {
                    for (int i = 0; i < listRoot.childCount; i++)
                    {
                        Transform row = listRoot.GetChild(i);
                        _listRows.Add(new ListRowUi
                        {
                            Root = row as RectTransform,
                            Background = row.Find("Background")?.GetComponent<Image>(),
                            RankBadge = row.Find("RankBadge")?.GetComponent<Image>(),
                            RankText = row.Find("RankBadge/RankText")?.GetComponent<TextMeshProUGUI>(),
                            NameText = row.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                            ScoreText = row.Find("ScoreText")?.GetComponent<TextMeshProUGUI>()
                        });
                    }
                }
            }
        }

        private PodiumSlotUi FindPodiumSlot(string path)
        {
            Transform slot = _richRoot.Find(path);
            if (slot == null)
                return null;

            return new PodiumSlotUi
            {
                Root = slot as RectTransform,
                Ring = slot.Find("AvatarRing")?.GetComponent<Image>(),
                AvatarFill = slot.Find("AvatarRing/AvatarFill")?.GetComponent<Image>(),
                InitialsText = slot.Find("AvatarRing/InitialsText")?.GetComponent<TextMeshProUGUI>(),
                NameText = slot.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                ScoreText = slot.Find("ScoreText")?.GetComponent<TextMeshProUGUI>(),
                PodiumFill = slot.Find("Pedestal")?.GetComponent<Image>(),
                RankText = slot.Find("Pedestal/RankText")?.GetComponent<TextMeshProUGUI>()
            };
        }

        private void BindHomeButton()
        {
            if (_homeButton == null)
                return;

            _homeButton.onClick.RemoveListener(LoadMainMenu);
            _homeButton.onClick.AddListener(LoadMainMenu);
            _listenersBound = true;
        }

        private void EnsureHomeButtonLayout()
        {
            if (_richRoot == null)
                return;

            if (_homeButton == null)
            {
                Transform contentRoot = _richRoot.Find("SafeRoot/ContentRoot");
                if (contentRoot != null)
                {
                    BuildHomeButton(contentRoot as RectTransform);
                    CacheReferences();
                }
            }

            if (_homeButton == null)
                return;

            RectTransform buttonRect = _homeButton.transform as RectTransform;
            if (buttonRect != null)
                SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(96f, 96f), new Vector2(16f, -12f), new Vector2(0f, 1f));

            Image hitArea = _homeButton.GetComponent<Image>();
            if (hitArea != null)
                hitArea.raycastTarget = true;

            var backSprite = LoadLeaderboardSprite("btn_back");
            Image buttonImage = _homeButton.GetComponentInChildren<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
                if (backSprite != null)
                {
                    buttonImage.sprite = backSprite;
                    buttonImage.type = Image.Type.Simple;
                    buttonImage.preserveAspect = true;
                    buttonImage.color = Color.white;
                }
            }

            Transform icon = _homeButton.transform.Find("IconText");
            if (icon != null)
                icon.gameObject.SetActive(false);

            Transform label = _homeButton.transform.Find("LabelText");
            if (label != null)
                label.gameObject.SetActive(false);

            BindHomeButton();
        }

        private void LoadMainMenu()
        {
            Debug.Log("[HighScoreTableView] Back button tapped -> loading scene: " + SceneCatalog.MainMenu);
            if (!Application.isPlaying)
                return;

            try
            {
                Audio.AudioManager.Instance?.PlayUiClick();
            }
            catch {}

            SceneManager.LoadScene(SceneCatalog.MainMenu);
        }

        private void SetLoadingState()
        {
            ApplyPodiumSlot(_firstSlot, null, true, 1);
            ApplyPodiumSlot(_secondSlot, null, true, 2);
            ApplyPodiumSlot(_thirdSlot, null, true, 3);

            for (int i = 0; i < _listRows.Count; i++)
            {
                ListRowUi row = _listRows[i];
                if (row == null)
                    continue;

                row.Root.gameObject.SetActive(true);
                row.RankText.text = (i + 4).ToString(CultureInfo.InvariantCulture);
                row.NameText.text = i == 0 ? loadingText : emptyEntryText;
                row.ScoreText.text = i == 0 ? "..." : emptyEntryText;
                SetRowVisuals(row, false);
            }
        }

        private async Task<List<LeaderboardRow>> BuildLeaderboardRowsAsync()
        {
            var remoteRows = await FetchRegisteredLeaderboardRowsAsync();
            FirebaseManager firebase = FirebaseManager.Instance;

            SortAndRank(remoteRows);
            await AttachCurrentPlayerRowAsync(remoteRows, firebase);

            // Always keep the Scores screen useful when Firebase has no public
            // document yet (especially for anonymous/local development users).
            // AttachCurrentPlayerRowAsync intentionally cannot do this while
            // Firebase is unavailable, so apply the local fallback here too.
            if (!remoteRows.Any(row => row.IsCurrentPlayer))
            {
                int localBestScore = GetLocalBestScore();
                string localUserId = firebase?.CurrentUser?.UserId;
                if (localBestScore > 0)
                {
                    remoteRows.Add(new LeaderboardRow
                    {
                        UserId = string.IsNullOrWhiteSpace(localUserId) ? "local-player" : localUserId,
                        PlayerName = ResolveCurrentPlayerLabel(),
                        Score = localBestScore,
                        IsCurrentPlayer = true
                    });
                    SortAndRank(remoteRows);
                }
            }

            return BuildVisibleRows(remoteRows);
        }

        private async Task AttachCurrentPlayerRowAsync(List<LeaderboardRow> rows, FirebaseManager firebase)
        {
            if (rows == null || firebase?.CurrentUser == null)
                return;

            string currentUserId = firebase.CurrentUser.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return;

            LeaderboardRow existingCurrentRow = rows.FirstOrDefault(row =>
                string.Equals(row.UserId, currentUserId, StringComparison.Ordinal));

            if (existingCurrentRow != null)
            {
                existingCurrentRow.IsCurrentPlayer = true;
                existingCurrentRow.PlayerName = ResolveCurrentPlayerLabel();
                return;
            }

            if (useWeeklyLeaderboard)
                return;

            if (!firebase.CurrentUser.IsAnonymous)
            {
                try
                {
                    LeaderboardRow remoteCurrentRow = await FetchCurrentUserRemoteRowAsync(currentUserId);
                    if (remoteCurrentRow != null)
                    {
                        remoteCurrentRow.IsCurrentPlayer = true;
                        remoteCurrentRow.PlayerName = ResolveCurrentPlayerLabel();
                        rows.Add(remoteCurrentRow);
                        SortAndRank(rows);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HighScoreTableView] Failed to fetch current user remote score: {ex.Message}");
                }
            }

            int localBestScore = GetLocalBestScore();
            if (localBestScore <= 0)
                return;

            rows.Add(new LeaderboardRow
            {
                UserId = currentUserId,
                PlayerName = ResolveCurrentPlayerLabel(),
                Score = localBestScore,
                IsCurrentPlayer = true
            });

            SortAndRank(rows);
        }

        private async Task<List<LeaderboardRow>> FetchRegisteredLeaderboardRowsAsync()
        {
            var rows = new List<LeaderboardRow>();
            FirebaseManager firebase = FirebaseManager.Instance;
            if (firebase == null)
                return rows;

            if (!firebase.IsInitialized)
            {
                // Firebase not ready yet — HandleFirebaseInitialized will re-trigger Refresh()
                return rows;
            }

            try
            {
                await firebase.EnsureAuthenticatedForLeaderboardAsync();

                Query query = FirebaseFirestore.DefaultInstance
                    .Collection(PublicLeaderboardCollection)
                    .OrderByDescending(useWeeklyLeaderboard ? "weeklyHighScore" : "highScore");

                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                string currentUserId = firebase.CurrentUser?.UserId;
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document == null || !document.Exists)
                        continue;

                    Dictionary<string, object> data = document.ToDictionary();
                    if (!TryGetLeaderboardScore(data, useWeeklyLeaderboard, out int score) || score <= 0)
                        continue;

                    string username = GetUsername(data);
                    bool isCurrentUser = string.Equals(document.Id, currentUserId, StringComparison.Ordinal);
                    bool isGuest = TryGetBool(data, "isGuest");
                    if (!isCurrentUser && (isGuest || string.IsNullOrWhiteSpace(username)))
                        continue;

                    rows.Add(new LeaderboardRow
                    {
                        UserId = document.Id,
                        PlayerName = username,
                        Score = score
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HighScoreTableView] Firestore remote fetch failed: {ex.Message}");
            }

            return rows;
        }

        private async Task<LeaderboardRow> FetchCurrentUserRemoteRowAsync(string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return null;

            try
            {
                DocumentSnapshot snapshot = await FirebaseFirestore.DefaultInstance
                    .Collection(UsersCollection)
                    .Document(currentUserId)
                    .GetSnapshotAsync();

                if (snapshot == null || !snapshot.Exists)
                    return null;

                Dictionary<string, object> data = snapshot.ToDictionary();
                if (!TryGetLeaderboardScore(data, useWeeklyLeaderboard, out int score) || score <= 0)
                    return null;

                string username = GetUsername(data);
                return new LeaderboardRow
                {
                    UserId = currentUserId,
                    PlayerName = string.IsNullOrWhiteSpace(username) ? ResolveCurrentPlayerLabel() : username,
                    Score = score,
                    IsCurrentPlayer = true
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HighScoreTableView] Error inside FetchCurrentUserRemoteRowAsync: {ex.Message}");
                return null;
            }
        }

        private List<LeaderboardRow> BuildVisibleRows(List<LeaderboardRow> rankedRows)
        {
            var visible = new List<LeaderboardRow>();
            if (rankedRows == null || rankedRows.Count == 0)
                return visible;

            LeaderboardRow currentRow = rankedRows.FirstOrDefault(row => row.IsCurrentPlayer);
            int limit = Mathf.Max(6, maxEntries);

            if (currentRow == null)
                return rankedRows.Take(limit).ToList();

            int currentIndex = rankedRows.IndexOf(currentRow);
            if (currentIndex >= 0 && currentIndex < limit)
                return rankedRows.Take(limit).ToList();

            visible.AddRange(rankedRows.Take(Mathf.Max(0, limit - 1)));
            visible.Add(currentRow);
            return visible.OrderBy(row => row.Rank).ToList();
        }

        private void ApplyRows(List<LeaderboardRow> rows)
        {
            if (this == null || !isActiveAndEnabled)
                return;

            EnsureHierarchy();
            rows ??= new List<LeaderboardRow>();

            LeaderboardRow first = rows.FirstOrDefault(r => r.Rank == 1);
            LeaderboardRow second = rows.FirstOrDefault(r => r.Rank == 2);
            LeaderboardRow third = rows.FirstOrDefault(r => r.Rank == 3);

            ApplyPodiumSlot(_firstSlot, first, false, 1);
            ApplyPodiumSlot(_secondSlot, second, false, 2);
            ApplyPodiumSlot(_thirdSlot, third, false, 3);

            List<LeaderboardRow> listRows = rows.Where(r => r.Rank >= 4).OrderBy(r => r.Rank).Take(_listRows.Count).ToList();
            for (int i = 0; i < _listRows.Count; i++)
            {
                ListRowUi rowUi = _listRows[i];
                if (rowUi == null)
                    continue;

                if (i < listRows.Count)
                {
                    LeaderboardRow row = listRows[i];
                    rowUi.Root.gameObject.SetActive(true);
                    rowUi.RankText.text = row.Rank.ToString(CultureInfo.InvariantCulture);
                    rowUi.NameText.text = row.IsCurrentPlayer ? ResolveCurrentPlayerLabel() : ShortenName(row.PlayerName);
                    rowUi.ScoreText.text = FormatScore(row.Score);
                    SetRowVisuals(rowUi, row.IsCurrentPlayer);
                }
                else
                {
                    ApplyEmptyListRow(rowUi, i + 4);
                }
            }
        }

        private void ApplyPodiumSlot(PodiumSlotUi slot, LeaderboardRow row, bool isLoading, int rank)
        {
            if (slot == null || slot.Root == null)
                return;

            slot.Root.gameObject.SetActive(true);
            if (slot.PodiumFill != null)
                slot.PodiumFill.gameObject.SetActive(true);

            if (row == null)
            {
                ApplyEmptyPodiumSlot(slot, rank, isLoading);
                return;
            }

            slot.InitialsText.text = GetInitials(row.IsCurrentPlayer ? ResolveCurrentPlayerLabel() : row.PlayerName);
            slot.NameText.text = row.IsCurrentPlayer ? ResolveCurrentPlayerLabel() : ShortenName(row.PlayerName);
            slot.ScoreText.text = FormatScore(row.Score);
            slot.RankText.text = row.Rank.ToString(CultureInfo.InvariantCulture);

            bool isCurrent = row.IsCurrentPlayer;
            if (slot.Ring != null)
            {
                if (slot.Ring.sprite == null)
                    slot.Ring.color = isCurrent ? currentPlayerColor : GetPodiumRingColor(rank);
                else
                    slot.Ring.color = isCurrent ? new Color(1f, 0.95f, 0.7f, 1f) : Color.white;
            }
            if (slot.AvatarFill != null)
                slot.AvatarFill.color = isCurrent ? new Color(0.12f, 0.26f, 0.54f, 1f) : new Color(0.07f, 0.2f, 0.49f, 1f);
            if (slot.NameText != null)
                slot.NameText.color = isCurrent ? currentPlayerColor : new Color(0.89f, 0.94f, 1f, 0.96f);
            if (slot.PodiumFill != null)
            {
                if (slot.PodiumFill.sprite == null)
                    slot.PodiumFill.color = GetPodiumFillColor(rank);
                else
                    slot.PodiumFill.color = Color.white;
            }
        }

        private string GetLoadingText()
        {
            if (LanguageManager.Instance == null) return "Yükleniyor...";
            var lang = LanguageManager.Instance.CurrentLanguage;
            if (lang == LanguageManager.Language.Korean) return "로딩 중...";
            if (lang == LanguageManager.Language.English) return "Loading...";
            return "Yükleniyor...";
        }

        private void ApplyEmptyPodiumSlot(PodiumSlotUi slot, int rank, bool isLoading)
        {
            if (slot == null || slot.Root == null)
                return;

            slot.Root.gameObject.SetActive(true);
            if (slot.PodiumFill != null)
                slot.PodiumFill.gameObject.SetActive(true);
            slot.InitialsText.text = "--";
            slot.NameText.text = isLoading && rank == 1 ? GetLoadingText() : emptyEntryText;
            slot.ScoreText.text = isLoading ? "..." : emptyEntryText;
            slot.RankText.text = rank.ToString(CultureInfo.InvariantCulture);

            if (slot.Ring != null && slot.Ring.sprite == null)
                slot.Ring.color = GetPodiumRingColor(rank);
            if (slot.AvatarFill != null)
                slot.AvatarFill.color = new Color(0.07f, 0.2f, 0.49f, 1f);
            if (slot.NameText != null)
                slot.NameText.color = new Color(0.89f, 0.94f, 1f, 0.68f);
            if (slot.ScoreText != null)
                slot.ScoreText.color = new Color(0.98f, 0.99f, 1f, 0.58f);
            if (slot.PodiumFill != null && slot.PodiumFill.sprite == null)
                slot.PodiumFill.color = GetPodiumFillColor(rank);
        }

        private void SetRowVisuals(ListRowUi rowUi, bool isCurrentPlayer)
        {
            if (rowUi == null)
                return;

            if (rowUi.Background != null)
            {
                var rowSprite = LoadLeaderboardSprite(isCurrentPlayer ? "row_bg_player" : "row_bg_normal");
                if (rowSprite != null)
                {
                    rowUi.Background.sprite = rowSprite;
                    rowUi.Background.type = Image.Type.Simple;
                    rowUi.Background.color = Color.white;
                }
                else
                {
                    rowUi.Background.color = isCurrentPlayer ? new Color(0.13f, 0.22f, 0.46f, 0.98f) : new Color(0.03f, 0.13f, 0.35f, 0.92f);
                }
            }
            if (rowUi.RankBadge != null)
                rowUi.RankBadge.color = isCurrentPlayer ? currentPlayerColor : new Color(0.06f, 0.18f, 0.42f, 0.85f);
            if (rowUi.RankText != null)
                rowUi.RankText.color = isCurrentPlayer ? new Color(0.17f, 0.15f, 0.08f, 1f) : new Color(0.85f, 0.93f, 1f, 1f);
            if (rowUi.NameText != null)
                rowUi.NameText.color = isCurrentPlayer ? currentPlayerColor : Color.white;
            if (rowUi.ScoreText != null)
                rowUi.ScoreText.color = isCurrentPlayer ? currentPlayerColor : new Color(0.98f, 0.99f, 1f, 1f);
        }

        private void ApplyEmptyListRow(ListRowUi rowUi, int rank)
        {
            if (rowUi == null || rowUi.Root == null)
                return;

            // Do not leave a misleading empty rank card on the finished screen.
            // LoadingState explicitly reactivates all rows while data is pending.
            rowUi.Root.gameObject.SetActive(false);
            rowUi.RankText.text = rank.ToString(CultureInfo.InvariantCulture);
            rowUi.NameText.text = emptyEntryText;
            rowUi.ScoreText.text = emptyEntryText;
            SetRowVisuals(rowUi, false);

            if (rowUi.NameText != null)
                rowUi.NameText.color = new Color(1f, 1f, 1f, 0.5f);
            if (rowUi.ScoreText != null)
                rowUi.ScoreText.color = new Color(0.98f, 0.99f, 1f, 0.42f);
        }

        private string ResolveCurrentPlayerLabel()
        {
            if (LanguageManager.Instance == null)
            {
                if (string.IsNullOrWhiteSpace(currentPlayerLabel))
                    return "Sen";
                return currentPlayerLabel;
            }

            var lang = LanguageManager.Instance.CurrentLanguage;
            if (lang == LanguageManager.Language.Korean) return "나";
            if (lang == LanguageManager.Language.English) return "You";
            return "Sen";
        }

        private static string ShortenName(string rawName)
        {
            string name = (rawName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return "-";

            string[] parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0]} {char.ToUpperInvariant(parts[1][0])}.";

            return name.Length > 14 ? name.Substring(0, 14) : name;
        }

        private static string GetInitials(string rawName)
        {
            string name = (rawName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return "--";

            string[] parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";

            if (parts[0].Length >= 2)
                return parts[0].Substring(0, 2).ToUpperInvariant();

            return parts[0].ToUpperInvariant();
        }

        private static string FormatScore(int score)
        {
            return score.ToString("N0", ScoreCulture);
        }

        private static Color GetPodiumRingColor(int rank)
        {
            switch (rank)
            {
                case 1: return new Color(1f, 0.8f, 0.08f, 1f);
                case 2: return new Color(0.83f, 0.86f, 0.93f, 1f);
                default: return new Color(1f, 0.62f, 0.12f, 1f);
            }
        }

        private static Color GetPodiumFillColor(int rank)
        {
            switch (rank)
            {
                case 1: return new Color(0.95f, 0.66f, 0.08f, 1f);
                case 2: return new Color(0.66f, 0.7f, 0.8f, 1f);
                default: return new Color(0.72f, 0.4f, 0.12f, 1f);
            }
        }

        private static void SortAndRank(List<LeaderboardRow> rows)
        {
            if (rows == null)
                return;

            rows.Sort((a, b) =>
            {
                int scoreCompare = b.Score.CompareTo(a.Score);
                if (scoreCompare != 0)
                    return scoreCompare;

                return string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < rows.Count; i++)
                rows[i].Rank = i + 1;
        }

        private static bool TryGetScore(Dictionary<string, object> data, string key, out int score)
        {
            score = 0;
            if (data == null || string.IsNullOrWhiteSpace(key) || !data.TryGetValue(key, out object raw) || raw == null)
                return false;

            try
            {
                score = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetLeaderboardScore(Dictionary<string, object> data, bool weekly, out int score)
        {
            return TryGetScore(data, weekly ? "weeklyHighScore" : "highScore", out score);
        }

        private int GetLocalBestScore()
        {
            if (_bestScoreStore == null)
                _bestScoreStore = new BestScoreStore(new PlayerPrefsStorage());

            return _bestScoreStore.GetBestScore();
        }

        private static bool TryGetBool(Dictionary<string, object> data, string key)
        {
            if (data == null || string.IsNullOrWhiteSpace(key) || !data.TryGetValue(key, out object raw) || raw == null)
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

        private static string GetUsername(Dictionary<string, object> data)
        {
            if (data == null)
                return string.Empty;

            if (data.TryGetValue("username", out object username) && username != null)
                return username.ToString();

            if (data.TryGetValue("normalizedUsername", out object normalized) && normalized != null)
                return normalized.ToString();

            if (data.TryGetValue("displayName", out object displayName) && displayName != null)
                return displayName.ToString();

            if (data.TryGetValue("playerName", out object playerName) && playerName != null)
                return playerName.ToString();

            if (data.TryGetValue("name", out object name) && name != null)
                return name.ToString();

            return string.Empty;
        }

        private static bool IsPermissionDenied(Exception exception)
        {
            if (exception == null)
                return false;

            if (exception is FirebaseException firebaseException)
            {
                string message = firebaseException.Message ?? string.Empty;
                if (message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (exception is AggregateException aggregateException)
            {
                foreach (Exception inner in aggregateException.Flatten().InnerExceptions)
                {
                    if (IsPermissionDenied(inner))
                        return true;
                }
            }

            string exceptionMessage = exception.Message ?? string.Empty;
            return exceptionMessage.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   exceptionMessage.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget, bool rounded, float cornerRadius)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (rounded)
            {
                RoundedSpriteImage roundedSprite = rect.gameObject.AddComponent<RoundedSpriteImage>();
                roundedSprite.CornerRadius = cornerRadius;
                roundedSprite.SpriteBorder = Mathf.Clamp(cornerRadius * 0.7f, 0.08f, 0.3f);
                roundedSprite.Apply();
            }

            return image;
        }

        private static Image CreateCircleImage(string name, Transform parent, Color color, int textureSize, float edgeSoftness)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            CircleSpriteImage circle = rect.gameObject.AddComponent<CircleSpriteImage>();
            circle.TextureSize = textureSize;
            circle.EdgeSoftness = edgeSoftness;
            circle.Apply();
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateGlow(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image glow = CreateImage(name, parent, color, false, true, 0.5f);
            SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), size, position);
        }

        private static void BuildDecorSquare(RectTransform parent, string name, Vector2 position, float size, float rotationZ, Color color)
        {
            Image square = CreateImage(name, parent, color, false, true, 0.22f);
            SetRect(square.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size), position);
            square.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private static void BuildGrid(RectTransform parent)
        {
            RectTransform grid = CreateRect("Grid", parent);
            Stretch(grid);

            for (int i = 0; i < 10; i++)
            {
                Image vLine = CreateImage($"VLine{i}", grid, new Color(1f, 1f, 1f, 0.045f), false, false, 0f);
                RectTransform vRect = vLine.rectTransform;
                vRect.anchorMin = new Vector2((i + 1) / 11f, 0f);
                vRect.anchorMax = new Vector2((i + 1) / 11f, 1f);
                vRect.pivot = new Vector2(0.5f, 0.5f);
                vRect.sizeDelta = new Vector2(2f, 0f);

                Image hLine = CreateImage($"HLine{i}", grid, new Color(1f, 1f, 1f, 0.04f), false, false, 0f);
                RectTransform hRect = hLine.rectTransform;
                hRect.anchorMin = new Vector2(0f, (i + 1) / 11f);
                hRect.anchorMax = new Vector2(1f, (i + 1) / 11f);
                hRect.pivot = new Vector2(0.5f, 0.5f);
                hRect.sizeDelta = new Vector2(0f, 2f);
            }
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
                shadow = target.AddComponent<Shadow>();

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            SetRect(rect, anchor, size, anchoredPosition, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 anchoredPosition, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static T FindByPath<T>(Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

#if UNITY_EDITOR
        [ContextMenu("Rebuild Rich Layout")]
        public void RebuildRichLayoutMenu()
        {
            Transform existing = transform.Find(RichRootName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            _richRoot = null;
            EnsureHierarchy();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [UnityEditor.MenuItem("Tools/BlokDunyasi/UI/Rebuild Scores Layout")]
        public static void RebuildAllScoresLayoutMenu()
        {
            var views = UnityEngine.Object.FindObjectsByType<HighScoreTableView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (views != null && views.Length > 0)
            {
                foreach (var v in views)
                {
                    v.RebuildRichLayoutMenu();
                }
                Debug.Log("[HighScoreTableView] Rebuilt layout for " + views.Length + " HighScoreTableView instances.");
            }
            else
            {
                Debug.LogWarning("[HighScoreTableView] No HighScoreTableView found in active scene.");
            }
        }
#endif
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed partial class CircleSpriteImage : MonoBehaviour
    {
        [SerializeField] [Min(64)] private int textureSize = 1024;
        [SerializeField] [Range(0.5f, 8f)] private float edgeSoftness = 2f;
        [SerializeField] private bool autoApply = true;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private Image _image;

        public int TextureSize
        {
            get => textureSize;
            set
            {
                textureSize = Mathf.Max(64, value);
                Apply();
            }
        }

        public float EdgeSoftness
        {
            get => edgeSoftness;
            set
            {
                edgeSoftness = Mathf.Clamp(value, 0.5f, 8f);
                Apply();
            }
        }

        private void Awake()
        {
            EnsureImage();
            if (autoApply)
                Apply();
        }

        private void OnEnable()
        {
            if (autoApply)
                Apply();
        }

        private void OnValidate()
        {
            if (autoApply)
                Apply();
        }

        public void Apply()
        {
            EnsureImage();
            if (_image == null)
                return;

            int size = Mathf.Max(64, textureSize);
            float softness = Mathf.Clamp(edgeSoftness, 0.5f, 8f);
            string key = $"circle_{size}_{Mathf.RoundToInt(softness * 100f)}";

            if (!SpriteCache.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = BuildSprite(size, softness);
                SpriteCache[key] = sprite;
            }

            _image.sprite = sprite;
            _image.type = Image.Type.Simple;
            _image.preserveAspect = true;
        }

        private void EnsureImage()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }

        private static Sprite BuildSprite(int size, float softness)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = $"CircleSprite_{size}_{Mathf.RoundToInt(softness * 100f)}";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float distance = Vector2.Distance(point, center);
                    float alpha = Mathf.Clamp01((radius - distance) / softness);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
