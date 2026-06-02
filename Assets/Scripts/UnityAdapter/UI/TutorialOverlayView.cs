using BlockPuzzle.UnityAdapter.Boot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.UnityAdapter.UI.Localization;

namespace BlockPuzzle.UnityAdapter.UI
{
    public sealed class TutorialOverlayView : MonoBehaviour, ILanguageListener
    {
        private CanvasGroup _canvasGroup;
        private RectTransform _panel;
        private TMP_Text _stepText;
        private TMP_Text _titleText;
        private TMP_Text _descriptionText;
        private Button _skipButton;
        private TutorialStepPayload _currentPayload;

        private static void Initialize()
        {
            return;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildRuntimeUi();
            ApplyPayload(new TutorialStepPayload(false, 0, 0, string.Empty, string.Empty));
        }

        private void OnEnable()
        {
            GameBootstrap.OnTutorialStepChanged += ApplyPayload;
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Subscribe(this);
        }

        private void OnDisable()
        {
            GameBootstrap.OnTutorialStepChanged -= ApplyPayload;
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.Unsubscribe(this);
        }

        private void BuildRuntimeUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var panelObject = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            _panel = panelObject.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.08f, 0.73f);
            _panel.anchorMax = new Vector2(0.92f, 0.93f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            _stepText = CreateText("StepText", _panel, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            _stepText.rectTransform.anchorMin = new Vector2(0.06f, 0.72f);
            _stepText.rectTransform.anchorMax = new Vector2(0.94f, 0.92f);
            _stepText.color = new Color(1f, 0.84f, 0.35f, 1f);
            ConfigureResponsiveText(_stepText, 16f, 22f);

            _titleText = CreateText("TitleText", _panel, 34f, FontStyles.Bold, TextAlignmentOptions.Left);
            _titleText.rectTransform.anchorMin = new Vector2(0.06f, 0.40f);
            _titleText.rectTransform.anchorMax = new Vector2(0.94f, 0.76f);
            _titleText.color = Color.white;
            ConfigureResponsiveText(_titleText, 22f, 36f);

            _descriptionText = CreateText("DescriptionText", _panel, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _descriptionText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            _descriptionText.rectTransform.anchorMax = new Vector2(0.94f, 0.40f);
            _descriptionText.color = new Color(0.88f, 0.93f, 1f, 1f);
            ConfigureResponsiveText(_descriptionText, 18f, 26f);

            var skipObject = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            skipObject.transform.SetParent(_panel, false);
            var skipRect = skipObject.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.72f, 0.06f);
            skipRect.anchorMax = new Vector2(0.94f, 0.24f);
            skipRect.offsetMin = Vector2.zero;
            skipRect.offsetMax = Vector2.zero;

            var skipImage = skipObject.GetComponent<Image>();
            skipImage.color = new Color(1f, 1f, 1f, 0.14f);

            _skipButton = skipObject.GetComponent<Button>();
            _skipButton.onClick.AddListener(HandleSkipClicked);

            var skipLabel = CreateText("SkipLabel", skipRect, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
            skipLabel.rectTransform.anchorMin = Vector2.zero;
            skipLabel.rectTransform.anchorMax = Vector2.one;
            skipLabel.rectTransform.offsetMin = Vector2.zero;
            skipLabel.rectTransform.offsetMax = Vector2.zero;
            skipLabel.color = Color.white;
            skipLabel.text = "SKIP";
            ConfigureResponsiveText(skipLabel, 16f, 22f);
        }

        private static TMP_Text CreateText(string objectName, RectTransform parent, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = string.Empty;
            return text;
        }

        private static void ConfigureResponsiveText(TMP_Text text, float minSize, float maxSize)
        {
            if (text == null)
                return;

            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyPayload(TutorialStepPayload payload)
        {
            _currentPayload = payload;

            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = payload.Visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            if (_panel != null)
                _panel.gameObject.SetActive(payload.Visible);

            if (!payload.Visible)
                return;

            string stepPrefix;
            string skipLabelText;
            if (Localization.LanguageManager.Instance != null && Localization.LanguageManager.Instance.CurrentLanguage == Localization.LanguageManager.Language.Korean)
            {
                stepPrefix = "단계";
                skipLabelText = "건너뛰기";
            }
            else if (Localization.LanguageManager.Instance != null && Localization.LanguageManager.Instance.CurrentLanguage == Localization.LanguageManager.Language.English)
            {
                stepPrefix = "STEP";
                skipLabelText = "SKIP";
            }
            else
            {
                stepPrefix = "ADIM";
                skipLabelText = "GEÇ";
            }

            _stepText.text = $"{stepPrefix} {payload.StepIndex}/{payload.TotalSteps}";
            _titleText.text = payload.Title;
            _descriptionText.text = payload.Description;
            ApplyLocalizedFonts();

            if (_skipButton != null)
            {
                var skipLabel = _skipButton.transform.Find("SkipLabel")?.GetComponent<TMP_Text>();
                if (skipLabel != null)
                {
                    skipLabel.text = skipLabelText;
                }
            }
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            ApplyPayload(_currentPayload);
        }

        private void ApplyLocalizedFonts()
        {
            if (LanguageManager.Instance == null)
                return;

            var language = LanguageManager.Instance.CurrentLanguage;
            TMP_FontAsset font = LocalizedFontUtility.ResolveTmpFont(language, TMP_Settings.defaultFontAsset);

            if (_stepText != null) _stepText.font = font;
            if (_titleText != null) _titleText.font = font;
            if (_descriptionText != null) _descriptionText.font = font;

            if (_skipButton != null)
            {
                var skipLabel = _skipButton.transform.Find("SkipLabel")?.GetComponent<TMP_Text>();
                if (skipLabel != null)
                    skipLabel.font = font;
            }
        }

        private void HandleSkipClicked()
        {
            var bootstrap = FindFirstObjectByType<GameBootstrap>();
            bootstrap?.SkipActiveTutorial();
        }
    }
}
