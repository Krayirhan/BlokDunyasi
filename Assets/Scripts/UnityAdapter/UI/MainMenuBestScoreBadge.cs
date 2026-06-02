using System.Globalization;
using System.Text;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.UnityAdapter.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MainMenuBestScoreBadge : MonoBehaviour, ILanguageListener
    {
        [Header("References")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Image outerFrameImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image topBorderImage;
        [SerializeField] private Image bottomBorderImage;
        [SerializeField] private HorizontalLayoutGroup contentLayout;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI separatorText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private RectTransform legacyHitZone;

        [Header("Content")]
        [SerializeField] private string turkishLabel = "En Iyi";
        [SerializeField] private string englishLabel = "Best";
        [SerializeField] private string koreanLabel = "최고";
        [SerializeField] private string separatorSymbol = "\u2022";
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private bool hideLegacyVisualChildren = true;

        [Header("Preview")]
        [SerializeField] private LanguageManager.Language editorPreviewLanguage = LanguageManager.Language.English;
        [SerializeField] private int previewScore = 5079;

        [Header("Sizing")]
        [SerializeField] private bool driveGeneratedLayout = false;
        [SerializeField] private bool driveHostSize = false;
        [SerializeField] private bool driveLegacyHitZone = false;
        [SerializeField] private Vector2 badgeSize = new Vector2(272f, 50f);
        [SerializeField] private Vector2 borderThickness = new Vector2(1f, 1f);
        [SerializeField] private Vector2 contentPadding = new Vector2(20f, 10f);
        [SerializeField] private float itemSpacing = 12f;
        [SerializeField] private Vector2 iconSize = new Vector2(18f, 18f);

        [Header("Colors")]
        [SerializeField] private Color borderColor = new Color32(47, 67, 112, 255);
        [SerializeField] private Color fillColor = new Color32(26, 40, 82, 255);
        [SerializeField] private Color iconColor = new Color32(255, 193, 59, 255);
        [SerializeField] private Color labelColor = new Color32(160, 206, 255, 255);
        [SerializeField] private Color separatorColor = new Color32(83, 113, 164, 255);
        [SerializeField] private Color scoreColor = Color.white;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset labelFont;
        [SerializeField] private TMP_FontAsset scoreFont;
        [SerializeField] private float labelFontSize = 18f;
        [SerializeField] private float separatorFontSize = 16f;
        [SerializeField] private float scoreFontSize = 22f;
        [SerializeField] private FontStyles labelFontStyle = FontStyles.Bold;
        [SerializeField] private FontStyles scoreFontStyle = FontStyles.Bold;

        [Header("Formatting")]
        [SerializeField] private bool useGroupedThousands = true;
        [SerializeField] private string turkishThousandsSeparator = ".";
        [SerializeField] private string englishThousandsSeparator = ",";

        private const string FillObjectName = "BadgeFill";
        private const string ContentObjectName = "BadgeContent";
        private const string TopBorderObjectName = "BadgeTopBorder";
        private const string BottomBorderObjectName = "BadgeBottomBorder";
        private const string IconObjectName = "BadgeIcon";
        private const string LabelObjectName = "BadgeLabel";
        private const string SeparatorObjectName = "BadgeSeparator";
        private const string ScoreObjectName = "BadgeScore";

        private static Sprite _solidSprite;
        private BestScoreStore _bestScoreStore;
        private bool _createdFillImage;
        private bool _createdTopBorderImage;
        private bool _createdBottomBorderImage;
        private bool _createdContentLayout;
        private bool _createdIconImage;
        private bool _createdLabelText;
        private bool _createdSeparatorText;
        private bool _createdScoreText;

        private void Reset()
        {
            EnsureHierarchy();
            ApplyVisuals();
            RefreshScore();
        }

        private void Awake()
        {
            EnsureHierarchy();
            EnsureDataSource();
            ApplyVisuals();
            RefreshScore();
        }

        private void OnEnable()
        {
            EnsureHierarchy();
            EnsureDataSource();
            ApplyVisuals();
            RefreshScore();

            if (Application.isPlaying)
                LanguageManager.Instance.Subscribe(this);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                LanguageManager.Instance.Unsubscribe(this);
        }

        private void OnValidate()
        {
            badgeSize.x = Mathf.Max(80f, badgeSize.x);
            badgeSize.y = Mathf.Max(24f, badgeSize.y);
            borderThickness.x = Mathf.Max(0f, borderThickness.x);
            borderThickness.y = Mathf.Max(0f, borderThickness.y);
            contentPadding.x = Mathf.Max(0f, contentPadding.x);
            contentPadding.y = Mathf.Max(0f, contentPadding.y);
            itemSpacing = Mathf.Max(0f, itemSpacing);
            iconSize.x = Mathf.Max(8f, iconSize.x);
            iconSize.y = Mathf.Max(8f, iconSize.y);
            labelFontSize = Mathf.Max(6f, labelFontSize);
            separatorFontSize = Mathf.Max(6f, separatorFontSize);
            scoreFontSize = Mathf.Max(6f, scoreFontSize);

            EnsureHierarchy();
            ApplyVisuals();
            RefreshScore();
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            RefreshTexts(newLanguage);
        }

        private void EnsureDataSource()
        {
            if (_bestScoreStore == null)
                _bestScoreStore = new BestScoreStore(new PlayerPrefsStorage());
        }

        private void EnsureHierarchy()
        {
            RectTransform hostRect = transform as RectTransform;
            if (hostRect == null)
                return;

            if (hostButton == null)
                hostButton = GetComponent<Button>();

            if (outerFrameImage == null)
                outerFrameImage = GetComponent<Image>();

            if (fillImage == null)
            {
                fillImage = GetOrCreateImage(hostRect, FillObjectName);
                _createdFillImage = true;
            }

            if (topBorderImage == null)
            {
                topBorderImage = GetOrCreateImage(hostRect, TopBorderObjectName);
                _createdTopBorderImage = true;
            }

            if (bottomBorderImage == null)
            {
                bottomBorderImage = GetOrCreateImage(hostRect, BottomBorderObjectName);
                _createdBottomBorderImage = true;
            }

            RectTransform fillRect = fillImage.rectTransform;
            RectTransform contentRect = GetOrCreateRect(fillRect, ContentObjectName);

            if (contentLayout == null)
            {
                contentLayout = contentRect.GetComponent<HorizontalLayoutGroup>();
                if (contentLayout == null)
                {
                    contentLayout = contentRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                    _createdContentLayout = true;
                }
            }

            if (iconImage == null)
            {
                iconImage = GetOrCreateImage(contentRect, IconObjectName);
                _createdIconImage = true;
            }

            if (labelText == null)
            {
                labelText = GetOrCreateText(contentRect, LabelObjectName);
                _createdLabelText = true;
            }

            if (separatorText == null)
            {
                separatorText = GetOrCreateText(contentRect, SeparatorObjectName);
                _createdSeparatorText = true;
            }

            if (scoreText == null)
            {
                scoreText = GetOrCreateText(contentRect, ScoreObjectName);
                _createdScoreText = true;
            }

            if (legacyHitZone == null)
            {
                Transform hitZone = transform.Find("Hitscore");
                if (hitZone != null)
                    legacyHitZone = hitZone as RectTransform;
            }

            if (hideLegacyVisualChildren)
                HideLegacyVisuals();
        }

        private void ApplyVisuals()
        {
            RectTransform hostRect = transform as RectTransform;
            if (hostRect == null || fillImage == null || contentLayout == null || iconImage == null || labelText == null || separatorText == null || scoreText == null)
                return;

            if (driveHostSize)
                hostRect.sizeDelta = badgeSize;

            bool shouldDriveLayout = driveGeneratedLayout ||
                                     _createdFillImage ||
                                     _createdTopBorderImage ||
                                     _createdBottomBorderImage ||
                                     _createdContentLayout ||
                                     _createdIconImage ||
                                     _createdLabelText ||
                                     _createdSeparatorText ||
                                     _createdScoreText;

            if (outerFrameImage != null && shouldDriveLayout)
            {
                outerFrameImage.sprite = GetSolidSprite();
                outerFrameImage.type = Image.Type.Simple;
                outerFrameImage.color = fillColor;
                outerFrameImage.raycastTarget = true;
            }

            if (shouldDriveLayout)
            {
                fillImage.sprite = GetSolidSprite();
                fillImage.type = Image.Type.Simple;
                fillImage.color = fillColor;
                fillImage.raycastTarget = false;
            }

            RectTransform fillRect = fillImage.rectTransform;
            if (shouldDriveLayout)
            {
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.pivot = new Vector2(0.5f, 0.5f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillRect.SetAsFirstSibling();
            }

            if (shouldDriveLayout)
            {
                ApplyHorizontalBorder(topBorderImage, true);
                ApplyHorizontalBorder(bottomBorderImage, false);
            }

            RectTransform contentRect = contentLayout.transform as RectTransform;
            if (contentRect != null && shouldDriveLayout)
            {
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.offsetMin = new Vector2(contentPadding.x, contentPadding.y);
                contentRect.offsetMax = new Vector2(-contentPadding.x, -contentPadding.y);
            }

            if (shouldDriveLayout)
            {
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.childControlHeight = false;
                contentLayout.childControlWidth = false;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;
                contentLayout.spacing = itemSpacing;
                contentLayout.padding = new RectOffset(0, 0, 0, 0);
            }

            contentLayout.enabled = shouldDriveLayout;

            if (shouldDriveLayout)
            {
                ApplyIconVisuals();
                ApplyTextVisual(labelText, labelFont, labelFontSize, labelFontStyle, labelColor);
                ApplyTextVisual(separatorText, labelFont, separatorFontSize, FontStyles.Bold, separatorColor);
                ApplyTextVisual(scoreText, scoreFont, scoreFontSize, scoreFontStyle, scoreColor);
            }

            ApplyLegacyHitZone();

            separatorText.text = separatorSymbol;
            RefreshTexts(Application.isPlaying ? LanguageManager.Instance.CurrentLanguage : editorPreviewLanguage);

            if (shouldDriveLayout)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(hostRect);
                if (contentRect != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            ResetCreationFlags();
        }

        private void ApplyIconVisuals()
        {
            if (iconImage == null)
                return;

            if (iconSprite == null)
            {
                Transform legacyIcon = transform.Find("ScoreIcon");
                if (legacyIcon != null)
                {
                    Image legacyImage = legacyIcon.GetComponent<Image>();
                    if (legacyImage != null && legacyImage.sprite != null)
                        iconSprite = legacyImage.sprite;
                }
            }

            iconImage.sprite = iconSprite;
            iconImage.color = iconColor;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = iconSize;

            LayoutElement layout = iconImage.GetComponent<LayoutElement>();
            if (layout == null)
                layout = iconImage.gameObject.AddComponent<LayoutElement>();

            layout.minWidth = iconSize.x;
            layout.minHeight = iconSize.y;
            layout.preferredWidth = iconSize.x;
            layout.preferredHeight = iconSize.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private void ApplyTextVisual(TextMeshProUGUI text, TMP_FontAsset font, float fontSize, FontStyles fontStyle, Color color)
        {
            if (text == null)
                return;

            if (font != null)
                text.font = font;

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(6f, fontSize * 0.65f);
            text.fontSizeMax = Mathf.Max(fontSize, fontSize * 1.05f);
            text.alignment = TextAlignmentOptions.Midline;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void RefreshScore()
        {
            if (scoreText == null)
                return;

            int score = Application.isPlaying ? GetBestScore() : previewScore;
            LanguageManager.Language language = Application.isPlaying ? LanguageManager.Instance.CurrentLanguage : editorPreviewLanguage;
            scoreText.text = FormatScore(score, language);
        }

        private void RefreshTexts(LanguageManager.Language language)
        {
            if (labelText != null)
            {
                if (language == LanguageManager.Language.English)
                    labelText.text = englishLabel;
                else if (language == LanguageManager.Language.Korean)
                    labelText.text = koreanLabel;
                else
                    labelText.text = turkishLabel;
            }

            if (separatorText != null)
                separatorText.text = separatorSymbol;

            RefreshScore();
        }

        private int GetBestScore()
        {
            EnsureDataSource();
            return _bestScoreStore != null ? _bestScoreStore.GetBestScore() : 0;
        }

        private string FormatScore(int score, LanguageManager.Language language)
        {
            score = Mathf.Max(0, score);
            if (!useGroupedThousands)
                return score.ToString(CultureInfo.InvariantCulture);

            string raw = score.ToString(CultureInfo.InvariantCulture);
            string separator = (language == LanguageManager.Language.English || language == LanguageManager.Language.Korean)
                ? englishThousandsSeparator
                : turkishThousandsSeparator;

            if (string.IsNullOrEmpty(separator))
                separator = ",";

            var builder = new StringBuilder(raw);
            for (int i = builder.Length - 3; i > 0; i -= 3)
                builder.Insert(i, separator);

            return builder.ToString();
        }

        private void HideLegacyVisuals()
        {
            HideLegacyChild("Text");
            HideLegacyChild("ScoreIcon");
            HideLegacyChild("Image");
        }

        private void ApplyHorizontalBorder(Image borderImage, bool isTop)
        {
            if (borderImage == null)
                return;

            borderImage.sprite = GetSolidSprite();
            borderImage.type = Image.Type.Simple;
            borderImage.color = borderColor;
            borderImage.raycastTarget = false;

            RectTransform borderRect = borderImage.rectTransform;
            borderRect.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
            borderRect.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
            borderRect.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(0f, borderThickness.y);
            borderRect.SetAsLastSibling();
        }

        private void ApplyLegacyHitZone()
        {
            if (legacyHitZone == null || !driveLegacyHitZone)
                return;

            legacyHitZone.anchorMin = new Vector2(0.5f, 0.5f);
            legacyHitZone.anchorMax = new Vector2(0.5f, 0.5f);
            legacyHitZone.pivot = new Vector2(0.5f, 0.5f);
            legacyHitZone.anchoredPosition = Vector2.zero;
            legacyHitZone.sizeDelta = badgeSize + new Vector2(12f, 10f);
        }


        private void HideLegacyChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            if (child == fillImage?.transform ||
                child == contentLayout?.transform ||
                child == iconImage?.transform ||
                child == labelText?.transform ||
                child == separatorText?.transform ||
                child == scoreText?.transform ||
                child == legacyHitZone)
            {
                return;
            }

            if (child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }

        private void ResetCreationFlags()
        {
            _createdFillImage = false;
            _createdTopBorderImage = false;
            _createdBottomBorderImage = false;
            _createdContentLayout = false;
            _createdIconImage = false;
            _createdLabelText = false;
            _createdSeparatorText = false;
            _createdScoreText = false;
        }

        private static RectTransform GetOrCreateRect(RectTransform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                var go = new GameObject(objectName, typeof(RectTransform));
                child = go.transform;
                child.SetParent(parent, false);
            }

            return child as RectTransform;
        }

        private static Image GetOrCreateImage(RectTransform parent, string objectName)
        {
            RectTransform rect = GetOrCreateRect(parent, objectName);
            Image image = rect.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();

            return image;
        }

        private static TextMeshProUGUI GetOrCreateText(RectTransform parent, string objectName)
        {
            RectTransform rect = GetOrCreateRect(parent, objectName);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            return text;
        }

        private static Sprite GetSolidSprite()
        {
            if (_solidSprite != null)
                return _solidSprite;

            const int size = 8;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();

            _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _solidSprite.name = "MainMenuBestScoreBadgeSolid";
            _solidSprite.hideFlags = HideFlags.HideAndDontSave;
            return _solidSprite;
        }
    }
}
