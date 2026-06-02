using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Persistence;
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
        private const string RichRootName = "RichScoresRoot";
        private const string MarkerName = "ScoresPodiumV2";
        private static readonly CultureInfo ScoreCulture = CultureInfo.GetCultureInfo("tr-TR");

        [Header("Legacy")]
        [SerializeField] private TextMeshProUGUI[] entries;
        [SerializeField] private bool useWeeklyLeaderboard = false;
        [SerializeField] private string emptyEntryText = "-";
        [SerializeField] [Range(1, 20)] private int maxEntries = 6;
        [SerializeField] private float singleEntryAnchoredX;

        [Header("Remote Leaderboard")]
        [SerializeField] private bool includeCurrentPlayerWhenMissing = true;
        [SerializeField] private string loadingText = "Yukleniyor...";
        [SerializeField] private string currentPlayerLabel = "Sen";
        [SerializeField] private Color currentPlayerColor = new Color(1f, 0.86f, 0.32f, 1f);

        private readonly List<LeaderboardRow> _cachedRows = new List<LeaderboardRow>();
        private readonly List<ListRowUi> _listRows = new List<ListRowUi>();
        private BestScoreStore _bestScoreStore;
        private bool _refreshInProgress;
        private bool _listenersBound;
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
            EnsureStores();
            EnsureHierarchy();
        }

        private void OnEnable()
        {
            EnsureStores();
            EnsureHierarchy();
            BindHomeButton();

            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Subscribe(this);

            if (Application.isPlaying)
            {
                Refresh();
                RefreshLocalizedTexts();
            }
            else
            {
                ApplyRows(BuildPreviewRows());
                RefreshLocalizedTexts();
            }
        }

        private void OnDisable()
        {
            _listenersBound = false;
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Unsubscribe(this);
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            RefreshLocalizedTexts();
            if (Application.isPlaying)
            {
                Refresh();
            }
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
            ApplyRows(BuildPreviewRows());
        }
#endif

        public async void Refresh()
        {
            EnsureHierarchy();

            if (_refreshInProgress)
                return;

            _refreshInProgress = true;
            SetLoadingState();

            try
            {
                List<LeaderboardRow> rows = await BuildLeaderboardRowsAsync();
                _cachedRows.Clear();
                _cachedRows.AddRange(rows);
                ApplyRows(_cachedRows);
            }
            catch (Exception ex)
            {
                if (IsPermissionDenied(ex))
                    Debug.Log("[HighScoreTableView] Firestore leaderboard read is blocked by rules. Showing local fallback.");
                else
                    Debug.LogWarning($"[HighScoreTableView] Leaderboard refresh failed: {ex.Message}");

                ApplyRows(BuildLocalFallbackRows());
            }
            finally
            {
                _refreshInProgress = false;
            }
        }

        private void EnsureStores()
        {
            if (_bestScoreStore == null)
                _bestScoreStore = new BestScoreStore(new PlayerPrefsStorage());
        }

        private void EnsureHierarchy()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            _titleFont = Resources.Load<TMP_FontAsset>("TMP/LuckiestGuy-Regular Combo SDF") ?? TMP_Settings.defaultFontAsset;
            _bodyFont = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : _titleFont;

            // SafeAreaFitter currently lives outside the UnityAdapter asmdef, so avoid a hard type reference here.
            if (GetComponent("SafeAreaFitter") == null)
                Stretch(_rectTransform);
            _rectTransform.SetAsLastSibling();

            DisableLegacyChildren();

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
            EnsureHomeButtonLayout();
            BindHomeButton();
            RefreshLocalizedTexts();
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
            Stretch(safeRoot, 26f, 26f, 26f, 26f);

            RectTransform content = CreateRect("ContentRoot", safeRoot);
            Stretch(content, 42f, 42f, 24f, 24f);

            BuildHeader(content);
            BuildPodium(content);
            BuildList(content);
            BuildHomeButton(content);
        }

        private void BuildBackdrop(RectTransform root)
        {
            Image baseFill = CreateImage("BaseFill", root, new Color(0.03f, 0.1f, 0.25f, 1f), false, false, 0.04f);
            Stretch(baseFill.rectTransform);

            Image innerTint = CreateImage("InnerTint", root, new Color(0.02f, 0.16f, 0.43f, 0.92f), false, false, 0.04f);
            Stretch(innerTint.rectTransform, 12f, 12f, 12f, 12f);

            CreateGlow(root, "TopGlow", new Vector2(0f, 840f), new Vector2(900f, 760f), new Color(0.11f, 0.42f, 0.98f, 0.26f));
            CreateGlow(root, "BottomGlow", new Vector2(0f, -780f), new Vector2(920f, 640f), new Color(0.13f, 0.18f, 0.52f, 0.34f));
            CreateGlow(root, "AccentGlowLeft", new Vector2(-300f, 120f), new Vector2(360f, 360f), new Color(0.08f, 0.86f, 1f, 0.12f));
            CreateGlow(root, "AccentGlowRight", new Vector2(320f, -120f), new Vector2(340f, 340f), new Color(1f, 0.7f, 0.18f, 0.1f));

            BuildGrid(root);
            BuildDecorSquare(root, "DecorA", new Vector2(-410f, 790f), 86f, -17f, new Color(0.15f, 0.28f, 0.72f, 0.7f));
            BuildDecorSquare(root, "DecorB", new Vector2(406f, 708f), 52f, 12f, new Color(0.2f, 0.68f, 0.42f, 0.66f));
            BuildDecorSquare(root, "DecorC", new Vector2(-420f, 300f), 46f, 18f, new Color(0.47f, 0.24f, 0.82f, 0.58f));
            BuildDecorSquare(root, "DecorD", new Vector2(430f, 260f), 58f, -13f, new Color(0.66f, 0.54f, 0.16f, 0.54f));
            BuildDecorSquare(root, "DecorE", new Vector2(-445f, -930f), 34f, 0f, new Color(0.09f, 0.58f, 0.86f, 0.48f));
            BuildDecorSquare(root, "DecorF", new Vector2(432f, -772f), 32f, 11f, new Color(0.42f, 0.2f, 0.74f, 0.44f));
        }

        private void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateRect("Header", parent);
            SetRect(header, new Vector2(0.5f, 1f), new Vector2(820f, 260f), new Vector2(0f, -76f));

            TextMeshProUGUI trophyText = CreateText("TrophyText", header, "\u2605", _bodyFont, 90f, new Color(1f, 0.78f, 0.16f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(trophyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(140f, 90f), new Vector2(0f, -10f));
            AddShadow(trophyText.gameObject, new Color(0.58f, 0.28f, 0.02f, 0.5f), new Vector2(0f, -5f));

            TextMeshProUGUI titleText = CreateText("TitleText", header, "En Yuksek Skorlar", _bodyFont, 54f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 34f;
            titleText.fontSizeMax = 56f;
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(820f, 104f), new Vector2(0f, -118f));
            AddShadow(titleText.gameObject, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -4f));
        }

        private void BuildPodium(RectTransform parent)
        {
            RectTransform podiumRoot = CreateRect("PodiumRoot", parent);
            SetRect(podiumRoot, new Vector2(0.5f, 0.5f), new Vector2(860f, 640f), new Vector2(0f, 110f));

            _secondSlot = BuildPodiumSlot(podiumRoot, "SecondSlot", new Vector2(-250f, -22f), new Vector2(220f, 360f), 2, new Color(0.83f, 0.86f, 0.93f), new Color(0.36f, 0.48f, 0.76f), new Color(0.66f, 0.7f, 0.8f));
            _firstSlot = BuildPodiumSlot(podiumRoot, "FirstSlot", new Vector2(0f, 54f), new Vector2(250f, 456f), 1, new Color(1f, 0.8f, 0.08f), new Color(0.95f, 0.66f, 0.08f), new Color(1f, 0.86f, 0.28f));
            _thirdSlot = BuildPodiumSlot(podiumRoot, "ThirdSlot", new Vector2(250f, -4f), new Vector2(220f, 320f), 3, new Color(1f, 0.62f, 0.12f), new Color(0.72f, 0.4f, 0.12f), new Color(0.92f, 0.62f, 0.22f));
        }

        private PodiumSlotUi BuildPodiumSlot(RectTransform parent, string name, Vector2 position, Vector2 pedestalSize, int rank, Color ringColor, Color pedestalColor, Color glowColor)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(260f, 520f), position);

            Image avatarGlow = CreateCircleImage("AvatarGlow", root, new Color(glowColor.r, glowColor.g, glowColor.b, rank == 1 ? 0.22f : 0.16f), 1024, 4f);
            SetRect(avatarGlow.rectTransform, new Vector2(0.5f, 1f), new Vector2(rank == 1 ? 182f : 160f, rank == 1 ? 182f : 160f), new Vector2(0f, -20f));

            Image ring = CreateCircleImage("AvatarRing", root, ringColor, 1024, 2f);
            SetRect(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(rank == 1 ? 148f : 132f, rank == 1 ? 148f : 132f), new Vector2(0f, -28f));
            AddShadow(ring.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -6f));

            Image avatarFill = CreateCircleImage("AvatarFill", ring.rectTransform, new Color(0.07f, 0.2f, 0.49f, 1f), 1024, 2f);
            Stretch(avatarFill.rectTransform, 8f, 8f, 8f, 8f);

            TextMeshProUGUI initialsText = CreateText("InitialsText", ring.rectTransform, "--", _bodyFont, rank == 1 ? 48f : 42f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(initialsText.rectTransform);

            TextMeshProUGUI nameText = CreateText("NameText", root, "Bekleniyor", _bodyFont, 30f, new Color(0.89f, 0.94f, 1f, 0.96f), FontStyles.Bold, TextAlignmentOptions.Center);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 32f;
            SetRect(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(220f, 54f), new Vector2(0f, -186f));

            TextMeshProUGUI scoreText = CreateText("ScoreText", root, "0", _bodyFont, rank == 1 ? 50f : 42f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMin = 22f;
            scoreText.fontSizeMax = rank == 1 ? 52f : 42f;
            SetRect(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(220f, 56f), new Vector2(0f, -232f));
            AddShadow(scoreText.gameObject, new Color(0f, 0f, 0f, 0.26f), new Vector2(0f, -3f));

            Image pedestal = CreateImage("Pedestal", root, pedestalColor, false, true, 0.08f);
            SetRect(pedestal.rectTransform, new Vector2(0.5f, 0f), pedestalSize, new Vector2(0f, 0f));
            AddShadow(pedestal.gameObject, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -10f));

            Image pedestalGloss = CreateImage("PedestalGloss", pedestal.rectTransform, new Color(1f, 1f, 1f, 0.18f), false, true, 0.08f);
            Stretch(pedestalGloss.rectTransform, 8f, 8f, 8f, pedestalSize.y * 0.56f);

            TextMeshProUGUI rankText = CreateText("RankText", pedestal.rectTransform, rank.ToString(CultureInfo.InvariantCulture), _titleFont, rank == 1 ? 92f : 72f, new Color(1f, 0.98f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(rankText.rectTransform);
            AddShadow(rankText.gameObject, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -4f));

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
            SetRect(listRoot, new Vector2(0.5f, 0f), new Vector2(860f, 420f), new Vector2(0f, 132f), new Vector2(0.5f, 0f));

            int rowCount = Mathf.Max(3, Mathf.Max(6, maxEntries) - 3);
            for (int i = 0; i < rowCount; i++)
            {
                float y = -i * 114f;
                _listRows.Add(BuildListRow(listRoot, $"Row{i + 4}", new Vector2(0f, y)));
            }
        }

        private ListRowUi BuildListRow(RectTransform parent, string name, Vector2 position)
        {
            RectTransform root = CreateRect(name, parent);
            SetRect(root, new Vector2(0.5f, 1f), new Vector2(820f, 92f), position, new Vector2(0.5f, 1f));

            Image background = CreateImage("Background", root, new Color(0.03f, 0.13f, 0.35f, 0.92f), false, true, 0.12f);
            Stretch(background.rectTransform);
            AddShadow(background.gameObject, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -6f));

            Outline outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.34f, 0.74f, 0.48f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;

            Image rankBadge = CreateImage("RankBadge", root, new Color(0.06f, 0.18f, 0.42f, 1f), false, true, 0.26f);
            SetRect(rankBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(98f, 62f), new Vector2(28f, 0f), new Vector2(0f, 0.5f));

            TextMeshProUGUI rankText = CreateText("RankText", rankBadge.rectTransform, "-", _bodyFont, 34f, new Color(0.69f, 0.83f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(rankText.rectTransform);

            TextMeshProUGUI nameText = CreateText("NameText", root, "-", _bodyFont, 32f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 20f;
            nameText.fontSizeMax = 32f;
            SetRect(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(410f, 48f), new Vector2(168f, 0f), new Vector2(0f, 0.5f));

            TextMeshProUGUI scoreText = CreateText("ScoreText", root, "-", _bodyFont, 32f, new Color(0.98f, 0.99f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Right);
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMin = 20f;
            scoreText.fontSizeMax = 32f;
            SetRect(scoreText.rectTransform, new Vector2(1f, 0.5f), new Vector2(180f, 48f), new Vector2(-32f, 0f), new Vector2(1f, 0.5f));

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
            SetRect(buttonRoot, new Vector2(0f, 1f), new Vector2(220f, 84f), new Vector2(18f, -18f), new Vector2(0f, 1f));

            Image glow = CreateImage("ButtonGlow", buttonRoot, new Color(0.08f, 0.42f, 1f, 0.2f), false, false, 0.4f);
            SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(252f, 120f), Vector2.zero);

            Image buttonImage = CreateImage("ButtonFace", buttonRoot, new Color(0.13f, 0.43f, 0.98f, 1f), true, true, 0.24f);
            Stretch(buttonImage.rectTransform);
            AddShadow(buttonImage.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -10f));

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.82f, 0.88f, 1f, 0.94f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = buttonImage;

            TextMeshProUGUI iconText = CreateText("IconText", buttonRoot, "\u2190", _bodyFont, 42f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(iconText.rectTransform, new Vector2(0f, 0.5f), new Vector2(42f, 42f), new Vector2(26f, 0f), new Vector2(0f, 0.5f));
            AddShadow(iconText.gameObject, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -4f));

            TextMeshProUGUI labelText = CreateText("LabelText", buttonRoot, "Geri", _bodyFont, 26f, new Color(0.88f, 0.94f, 1f, 0.96f), FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(112f, 36f), new Vector2(76f, 0f), new Vector2(0f, 0.5f));

            _homeButton = button;
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
            if (_homeButton == null || _listenersBound)
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
                SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(220f, 84f), new Vector2(18f, -18f), new Vector2(0f, 1f));

            Transform glow = _homeButton.transform.Find("ButtonGlow");
            if (glow is RectTransform glowRect)
                SetRect(glowRect, new Vector2(0.5f, 0.5f), new Vector2(252f, 120f), Vector2.zero);

            Transform icon = _homeButton.transform.Find("IconText");
            if (icon != null)
            {
                TextMeshProUGUI iconText = icon.GetComponent<TextMeshProUGUI>();
                if (iconText != null)
                {
                    iconText.text = "\u2190";
                    SetRect(iconText.rectTransform, new Vector2(0f, 0.5f), new Vector2(42f, 42f), new Vector2(26f, 0f), new Vector2(0f, 0.5f));
                }
            }

            Transform label = _homeButton.transform.Find("LabelText");
            if (label != null)
            {
                TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.text = "Geri";
                    labelText.alignment = TextAlignmentOptions.Left;
                    SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(112f, 36f), new Vector2(76f, 0f), new Vector2(0f, 0.5f));
                }
            }
        }

        private void LoadMainMenu()
        {
            if (!Application.isPlaying)
                return;

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
            int localBestScore = _bestScoreStore != null ? _bestScoreStore.GetBestScore() : 0;
            FirebaseManager firebase = FirebaseManager.Instance;
            string currentUserId = firebase?.CurrentUser?.UserId;
            bool isRegisteredUser = firebase != null &&
                                    firebase.IsInitialized &&
                                    firebase.CurrentUser != null &&
                                    !firebase.CurrentUser.IsAnonymous &&
                                    !string.IsNullOrWhiteSpace(firebase.NormalizedUsername);

            SortAndRank(remoteRows);

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                LeaderboardRow existingCurrentRow = remoteRows.FirstOrDefault(row =>
                    string.Equals(row.UserId, currentUserId, StringComparison.Ordinal));

                if (existingCurrentRow != null)
                {
                    existingCurrentRow.IsCurrentPlayer = true;
                    existingCurrentRow.PlayerName = ResolveCurrentPlayerLabel();
                }
                else if (includeCurrentPlayerWhenMissing && localBestScore > 0)
                {
                    var currentRow = new LeaderboardRow
                    {
                        UserId = currentUserId,
                        PlayerName = ResolveCurrentPlayerLabel(),
                        Score = localBestScore,
                        IsCurrentPlayer = true
                    };

                    InsertCurrentPlayerRow(remoteRows, currentRow, isRegisteredUser);
                }
            }
            else if (includeCurrentPlayerWhenMissing && localBestScore > 0)
            {
                InsertCurrentPlayerRow(remoteRows, new LeaderboardRow
                {
                    UserId = "__local__",
                    PlayerName = ResolveCurrentPlayerLabel(),
                    Score = localBestScore,
                    IsCurrentPlayer = true
                }, false);
            }

            return BuildVisibleRows(remoteRows);
        }

        private async Task<List<LeaderboardRow>> FetchRegisteredLeaderboardRowsAsync()
        {
            var rows = new List<LeaderboardRow>();
            FirebaseManager firebase = FirebaseManager.Instance;
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

            Query query = FirebaseFirestore.DefaultInstance
                .Collection(PublicLeaderboardCollection)
                .OrderByDescending(useWeeklyLeaderboard ? "weeklyHighScore" : "highScore");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (document == null || !document.Exists)
                    continue;

                Dictionary<string, object> data = document.ToDictionary();
                if (!TryGetScore(data, useWeeklyLeaderboard ? "weeklyHighScore" : "highScore", out int score) || score <= 0)
                    continue;

                string username = GetUsername(data);
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                rows.Add(new LeaderboardRow
                {
                    UserId = document.Id,
                    PlayerName = username,
                    Score = score
                });
            }

            return rows;
        }

        private void InsertCurrentPlayerRow(List<LeaderboardRow> rows, LeaderboardRow currentRow, bool isRegisteredUser)
        {
            if (rows == null || currentRow == null)
                return;

            int insertIndex = rows.FindIndex(row => row.Score < currentRow.Score);
            if (insertIndex < 0)
                insertIndex = rows.Count;

            if (!isRegisteredUser)
                currentRow.UserId = "__local_guest__";

            rows.Insert(insertIndex, currentRow);
            SortAndRank(rows);
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

        private List<LeaderboardRow> BuildLocalFallbackRows()
        {
            int localBestScore = _bestScoreStore != null ? _bestScoreStore.GetBestScore() : 0;
            if (localBestScore <= 0)
                return BuildPreviewRows();

            return new List<LeaderboardRow>
            {
                new LeaderboardRow
                {
                    UserId = "__local_fallback__",
                    PlayerName = ResolveCurrentPlayerLabel(),
                    Score = localBestScore,
                    Rank = 1,
                    IsCurrentPlayer = true
                }
            };
        }

        private List<LeaderboardRow> BuildPreviewRows()
        {
            string current = ResolveCurrentPlayerLabel();
            return new List<LeaderboardRow>
            {
                new LeaderboardRow { PlayerName = "Mehmet A.", Score = 5079, Rank = 1 },
                new LeaderboardRow { PlayerName = "Ayse K.", Score = 4210, Rank = 2 },
                new LeaderboardRow { PlayerName = "Can T.", Score = 3850, Rank = 3 },
                new LeaderboardRow { PlayerName = "Ece Y.", Score = 3441, Rank = 4 },
                new LeaderboardRow { PlayerName = current, Score = 3120, Rank = 5, IsCurrentPlayer = true },
                new LeaderboardRow { PlayerName = "Burak K.", Score = 2980, Rank = 6 }
            };
        }

        private void ApplyRows(List<LeaderboardRow> rows)
        {
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

            if (row == null)
            {
                ApplyEmptyPodiumSlot(slot, rank, isLoading);
                return;
            }

            slot.Root.gameObject.SetActive(true);
            slot.InitialsText.text = GetInitials(row.IsCurrentPlayer ? ResolveCurrentPlayerLabel() : row.PlayerName);
            slot.NameText.text = row.IsCurrentPlayer ? ResolveCurrentPlayerLabel() : ShortenName(row.PlayerName);
            slot.ScoreText.text = FormatScore(row.Score);
            slot.RankText.text = row.Rank.ToString(CultureInfo.InvariantCulture);

            bool isCurrent = row.IsCurrentPlayer;
            if (slot.Ring != null)
                slot.Ring.color = isCurrent ? currentPlayerColor : GetPodiumRingColor(rank);
            if (slot.AvatarFill != null)
                slot.AvatarFill.color = isCurrent ? new Color(0.12f, 0.26f, 0.54f, 1f) : new Color(0.07f, 0.2f, 0.49f, 1f);
            if (slot.NameText != null)
                slot.NameText.color = isCurrent ? currentPlayerColor : new Color(0.89f, 0.94f, 1f, 0.96f);
            if (slot.PodiumFill != null)
                slot.PodiumFill.color = GetPodiumFillColor(rank);
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
            slot.InitialsText.text = "--";
            slot.NameText.text = isLoading && rank == 1 ? GetLoadingText() : emptyEntryText;
            slot.ScoreText.text = isLoading ? "..." : emptyEntryText;
            slot.RankText.text = rank.ToString(CultureInfo.InvariantCulture);

            if (slot.Ring != null)
                slot.Ring.color = GetPodiumRingColor(rank);
            if (slot.AvatarFill != null)
                slot.AvatarFill.color = new Color(0.07f, 0.2f, 0.49f, 1f);
            if (slot.NameText != null)
                slot.NameText.color = new Color(0.89f, 0.94f, 1f, 0.68f);
            if (slot.ScoreText != null)
                slot.ScoreText.color = new Color(0.98f, 0.99f, 1f, 0.58f);
            if (slot.PodiumFill != null)
                slot.PodiumFill.color = GetPodiumFillColor(rank);
        }

        private void SetRowVisuals(ListRowUi rowUi, bool isCurrentPlayer)
        {
            if (rowUi == null)
                return;

            if (rowUi.Background != null)
                rowUi.Background.color = isCurrentPlayer ? new Color(0.13f, 0.22f, 0.46f, 0.98f) : new Color(0.03f, 0.13f, 0.35f, 0.92f);
            if (rowUi.RankBadge != null)
                rowUi.RankBadge.color = isCurrentPlayer ? currentPlayerColor : new Color(0.06f, 0.18f, 0.42f, 1f);
            if (rowUi.RankText != null)
                rowUi.RankText.color = isCurrentPlayer ? new Color(0.17f, 0.15f, 0.08f, 1f) : new Color(0.69f, 0.83f, 1f, 1f);
            if (rowUi.NameText != null)
                rowUi.NameText.color = isCurrentPlayer ? currentPlayerColor : Color.white;
            if (rowUi.ScoreText != null)
                rowUi.ScoreText.color = isCurrentPlayer ? currentPlayerColor : new Color(0.98f, 0.99f, 1f, 1f);
        }

        private void ApplyEmptyListRow(ListRowUi rowUi, int rank)
        {
            if (rowUi == null || rowUi.Root == null)
                return;

            rowUi.Root.gameObject.SetActive(true);
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

        private static string GetUsername(Dictionary<string, object> data)
        {
            if (data == null)
                return string.Empty;

            if (data.TryGetValue("username", out object username) && username != null)
                return username.ToString();

            if (data.TryGetValue("normalizedUsername", out object normalized) && normalized != null)
                return normalized.ToString();

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
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
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
