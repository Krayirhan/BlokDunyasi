using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.Audio;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.UI.Localization;
using Debug = BlockPuzzle.Core.Common.GameLogger;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    [ExecuteAlways]
    public class MainMenuSettingsPopupView : MonoBehaviour, ILanguageListener
    {
        private const string KoreanTmpFontResourcePath = "TMP/MalgunGothic_DynamicSDF";

        [Header("Popup References")]
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private RectTransform dimmer;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button closeButton;

        [Header("Interactive Toggles")]
        [SerializeField] private Button soundButton;
        [SerializeField] private GameObject soundOnImage;
        [SerializeField] private GameObject soundOffImage;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private GameObject vibrationOnImage;
        [SerializeField] private GameObject vibrationOffImage;
        [SerializeField] private Button replayTutorialButton;
        [SerializeField] private Button moreGamesButton;

        [Header("Settings Links")]
        [SerializeField] private string moreGamesUrl = "https://play.google.com/store/apps/dev?id=5261548971044985006&hl=tr";

        [Header("Auto Build")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private bool startHidden = true;

        public bool IsOpen => popupRoot != null && popupRoot.gameObject.activeSelf;
        private bool _editorHierarchyRefreshQueued;
        private TMP_FontAsset _koreanFont;

        private void Start()
        {
            if (startHidden && Application.isPlaying)
            {
                Debug.Log("[MainMenuSettingsPopupView] Closing popup in Start()");
                CloseImmediate();
            }
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            RefreshTexts();
            HideReplayTutorialRow();
        }

        private void RefreshTexts()
        {
            var lang = Application.isPlaying
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.Turkish;

            string titleText;
            string soundText;
            string vibrationText;
            string moreGamesText;

            if (lang == LanguageManager.Language.English)
            {
                titleText = "SETTINGS";
                soundText = "SOUND";
                vibrationText = "VIBRATION";
                moreGamesText = "MORE APPS";
            }
            else if (lang == LanguageManager.Language.Korean)
            {
                titleText = "설정";
                soundText = "소리";
                vibrationText = "진동";
                moreGamesText = "추가 앱";
            }
            else
            {
                titleText = "AYARLAR";
                soundText = "SES";
                vibrationText = "TİTREŞİM";
                moreGamesText = "DİĞER UYGULAMALARIM";
            }

            OverridePopupTextsForLanguage(lang, ref titleText, ref soundText, ref vibrationText, ref moreGamesText);

            SetChildText(panel, "TitleText",          titleText);
            SetRowLabelText("SoundRow",               soundText);
            SetRowLabelText("VibrationRow",           vibrationText);
            SetRowLabelText("MoreGamesRow",           moreGamesText);
            RefreshLocalizedFonts(lang);
        }

        private static void OverridePopupTextsForLanguage(LanguageManager.Language lang, ref string titleText, ref string soundText, ref string vibrationText, ref string moreGamesText)
        {
            if (lang == LanguageManager.Language.Korean)
            {
                titleText = "\uC124\uC815";
                soundText = "\uC18C\uB9AC";
                vibrationText = "\uC9C4\uB3D9";
                moreGamesText = "\uCD94\uAC00 \uC571";
            }
            else if (lang == LanguageManager.Language.Turkish)
            {
                titleText = "AYARLAR";
                soundText = "SES";
                vibrationText = "TITRESIM";
                moreGamesText = "DIGER UYGULAMALARIM";
            }
        }

        private void SetChildText(Transform parent, string childName, string text)
        {
            if (parent == null) return;
            var child = parent.Find(childName);
            if (child == null) return;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void SetRowLabelText(string rowName, string text)
        {
            if (panel == null) return;
            var row = panel.Find(rowName);
            if (row == null) return;
            SetChildText(row, "RowLabel", text);
        }

        private void RefreshLocalizedFonts(LanguageManager.Language lang)
        {
            if (panel == null)
                return;

            ApplyLocalizedFont(panel.Find("TitleText"), lang, 64f);
            ApplyLocalizedFont(panel.Find("SoundRow/RowLabel"), lang, 48f);
            ApplyLocalizedFont(panel.Find("VibrationRow/RowLabel"), lang, 48f);
            ApplyLocalizedFont(panel.Find("MoreGamesRow/RowLabel"), lang, 48f);
            ApplyLocalizedFont(panel.Find("TurkishButton/Text"), lang, 28f);
            ApplyLocalizedFont(panel.Find("EnglishButton/Text"), lang, 28f);
            ApplyLocalizedFont(panel.Find("KoreanButton/Text"), lang, 28f);
        }

        private void ApplyLocalizedFont(Transform target, LanguageManager.Language lang, float fontSize)
        {
            if (target == null)
                return;

            var text = target.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;

            // Skip font override for elements with LocalizedText — it manages its own font
            var localizedText = target.GetComponent<LocalizedText>();
            if (localizedText == null)
            {
                TMP_FontAsset font = ResolveFontForLanguage(lang);
                if (font != null)
                {
                    text.font = font;
                    if (font.material != null)
                        text.fontSharedMaterial = font.material;
                }
            }

            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(18f, fontSize * (lang == LanguageManager.Language.Korean ? 0.58f : 0.72f));
            text.fontSize = fontSize;
            text.overflowMode = TextOverflowModes.Ellipsis;

            try
            {
                text.ForceMeshUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenuSettingsPopupView] ForceMeshUpdate failed for {target.name}: {e.Message}");
            }
        }

        private TMP_FontAsset ResolveFontForLanguage(LanguageManager.Language lang)
        {
            if (lang == LanguageManager.Language.Korean)
            {
                if (_koreanFont == null)
                    _koreanFont = LocalizedFontUtility.ResolveTmpFont(lang, null);

                if (_koreanFont != null)
                    return _koreanFont;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private void Awake()
        {
            EnsureHierarchy();

            if (Application.isPlaying)
                LanguageManager.Instance.Subscribe(this);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (soundButton != null)
            {
                soundButton.onClick.RemoveListener(ToggleSound);
                soundButton.onClick.AddListener(ToggleSound);
            }

            if (vibrationButton != null)
            {
                vibrationButton.onClick.RemoveListener(ToggleVibration);
                vibrationButton.onClick.AddListener(ToggleVibration);
            }

            if (moreGamesButton != null)
            {
                moreGamesButton.onClick.RemoveListener(OpenMoreGames);
                moreGamesButton.onClick.AddListener(OpenMoreGames);
            }

            if (replayTutorialButton != null)
                replayTutorialButton.onClick.RemoveListener(ReplayTutorial);

            HideReplayTutorialRow();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
                LanguageManager.Instance.Unsubscribe(this);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                RefreshUI();
                RefreshTexts();
                HideReplayTutorialRow();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (_editorHierarchyRefreshQueued)
                return;

            _editorHierarchyRefreshQueued = true;
            EditorApplication.delayCall += RefreshHierarchyInEditor;
#endif
        }

#if UNITY_EDITOR
        private void RefreshHierarchyInEditor()
        {
            _editorHierarchyRefreshQueued = false;

            if (this == null || gameObject == null)
                return;

            EnsureHierarchy();
        }
#endif

        public void Open()
        {
            if (popupRoot == null)
                return;

            popupRoot.gameObject.SetActive(true);
            popupRoot.SetAsLastSibling(); // Ensure it's on top of other elements like adaptive banners
            
            if (Application.isPlaying)
            {
                RefreshUI();
                RefreshTexts();
                HideReplayTutorialRow();
            }
        }

        public void Close()
        {
            if (popupRoot == null)
                return;

            popupRoot.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        private void CloseImmediate()
        {
            if (popupRoot != null)
                popupRoot.gameObject.SetActive(false);
        }

        public void ToggleSound()
        {
            int current = PlayerPrefs.GetInt(SettingsKeys.SfxOn, 1);
            int next = current == 1 ? 0 : 1;
            PlayerPrefs.SetInt(SettingsKeys.SfxOn, next);
            PlayerPrefs.SetInt(SettingsKeys.MusicOn, next);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
                AudioManager.Instance.RefreshFromPrefs();

            RefreshUI();
        }

        public void ToggleVibration()
        {
            int current = PlayerPrefs.GetInt(SettingsKeys.Vibration, 1);
            int next = current == 1 ? 0 : 1;
            PlayerPrefs.SetInt(SettingsKeys.Vibration, next);
            PlayerPrefs.Save();

            RefreshUI();
        }

        public void OpenMoreGames()
        {
            if (!string.IsNullOrEmpty(moreGamesUrl))
                Application.OpenURL(moreGamesUrl);
        }

        public void ReplayTutorial()
        {
            Debug.Log("[MainMenuSettingsPopupView] Tutorial replay is disabled.");
        }

        private void RefreshUI()
        {
            bool soundOn = PlayerPrefs.GetInt(SettingsKeys.SfxOn, 1) == 1;   
            if (soundOnImage != null)
                soundOnImage.SetActive(soundOn);
            if (soundOffImage != null)
                soundOffImage.SetActive(!soundOn);

            bool vibrOn = PlayerPrefs.GetInt(SettingsKeys.Vibration, 1) == 1;
            if (vibrationOnImage != null)
                vibrationOnImage.SetActive(vibrOn);
            if (vibrationOffImage != null)
                vibrationOffImage.SetActive(!vibrOn);
        }

        private void EnsureHierarchy()
        {
            if (!autoBuildIfMissing)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (canvas == null)
            {
                Debug.LogWarning("[MainMenuSettingsPopupView] Canvas not found in parents.");
                return;
            }

            if (transform.parent != canvas.transform)
                transform.SetParent(canvas.transform, false);

            if (popupRoot == null)
                popupRoot = EnsureRectTransform(canvas.transform, "SettingsPopupRoot", true, out _);

            StretchToParent(popupRoot);

            if (dimmer == null)
                dimmer = EnsureRectTransform(popupRoot, "Dimmer", true, out _);

            StretchToParent(dimmer);
            var dimmerImage = EnsureImage(dimmer.gameObject, out bool dimNew);
            if (dimNew) dimmerImage.color = new Color(0f, 0f, 0f, 0.65f);

            if (panel == null)
                panel = EnsureRectTransform(popupRoot, "Panel", true, out bool panelNew);

            if (panel.GetComponent<Image>() == null)
            {
                panel.anchorMin = new Vector2(0.5f, 0.5f);
                panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(720f, 980f);
                panel.anchoredPosition = Vector2.zero;

                var panelImage = EnsureImage(panel.gameObject, out bool pImgNew);
                if (pImgNew) panelImage.color = new Color(0.1f, 0.15f, 0.22f, 0.96f);
            }

            EnsureLabel(panel, "TitleText", "AYARLAR", new Vector2(0f, -88f));  

            if (closeButton == null)
            {
                var closeRect = EnsureRectTransform(panel, "CloseButton", true, out bool cNew);
                if (cNew)
                {
                    closeRect.anchorMin = new Vector2(1f, 1f);
                    closeRect.anchorMax = new Vector2(1f, 1f);
                    closeRect.pivot = new Vector2(1f, 1f);
                    closeRect.sizeDelta = new Vector2(110f, 110f);
                    closeRect.anchoredPosition = new Vector2(-28f, -28f);

                    var closeImage = EnsureImage(closeRect.gameObject, out bool cImgNew);
                    if (cImgNew) closeImage.color = new Color(0.2f, 0.3f, 0.45f, 1f);
                }
            }
            
            BuildToggleImageRow(panel, "SoundRow", "SES", -250f, ref soundButton, ref soundOnImage, ref soundOffImage);
            BuildToggleImageRow(panel, "VibrationRow", "TİTREŞİM", -400f, ref vibrationButton, ref vibrationOnImage, ref vibrationOffImage);

            HideReplayTutorialRow();
            BuildActionRow(panel, "MoreGamesRow", "DİĞER UYGULAMALARIM", -600f, ref moreGamesButton);
            EnsureLanguageButtons(panel);

            if (!Application.isPlaying)
            {
                RefreshUI();
                RefreshTexts();
            }
        }

        private static void DeleteChildIfFound(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                DestroyImmediate(child.gameObject);
        }

        private void HideReplayTutorialRow()
        {
            if (replayTutorialButton != null)
            {
                replayTutorialButton.onClick.RemoveListener(ReplayTutorial);
                replayTutorialButton.gameObject.SetActive(false);
            }

            if (panel == null) return;
            var row = panel.Find("ReplayTutorialRow");
            if (row != null)
                row.gameObject.SetActive(false);
        }

        private static void BuildToggleImageRow(Transform parent, string rowName, string labelText, float y, ref Button btn, ref GameObject onImage, ref GameObject offImage)
        {
            var itemRect = EnsureRectTransform(parent, rowName, false, out bool itemNew);
            if (itemNew)
            {
                itemRect.anchorMin = new Vector2(0.5f, 1f);
                itemRect.anchorMax = new Vector2(0.5f, 1f);
                itemRect.pivot = new Vector2(0.5f, 1f);
                itemRect.sizeDelta = new Vector2(620f, 120f);
                itemRect.anchoredPosition = new Vector2(0f, y);

                var image = EnsureImage(itemRect.gameObject, out bool isImgNew);
                if(isImgNew) image.color = new Color(0.17f, 0.25f, 0.35f, 0.5f);
            }

            var labelRect = EnsureRectTransform(itemRect, "RowLabel", false, out bool labelNew);
            if (labelNew)
            {
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0.6f, 1f);
                labelRect.offsetMin = new Vector2(40f, 0f);
                labelRect.offsetMax = Vector2.zero;

                var label = labelRect.GetComponent<TextMeshProUGUI>();
                if (label == null) label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.text = labelText;
                label.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
                label.fontSize = 48f;
                label.color = Color.white;
            }

            var btnRect = EnsureRectTransform(itemRect, "ActionBtn", false, out bool btnNew);
            if (btnNew)
            {
                btnRect.anchorMin = new Vector2(1f, 0.5f);
                btnRect.anchorMax = new Vector2(1f, 0.5f);
                btnRect.pivot = new Vector2(1f, 0.5f);
                btnRect.sizeDelta = new Vector2(90f, 90f);
                btnRect.anchoredPosition = new Vector2(-40f, 0f);

                var btnBg = EnsureImage(btnRect.gameObject, out bool isBtnBgNew);
                if(isBtnBgNew) btnBg.color = new Color(1f, 1f, 1f, 0f); // Transparent background
            }

            if (btn == null)
            {
                btn = btnRect.GetComponent<Button>();
                if (btn == null) btn = btnRect.gameObject.AddComponent<Button>();
            }

            var onRect = EnsureRectTransform(btnRect, "OnImage", false, out bool onNew);
            if (onNew)
            {
                StretchToParent(onRect);
                var onImg = EnsureImage(onRect.gameObject, out bool isOnImgNew);
                if(isOnImgNew) onImg.color = Color.white;
            }
            if (onImage == null) onImage = onRect.gameObject;

            var offRect = EnsureRectTransform(btnRect, "OffImage", false, out bool offNew);
            if (offNew)
            {
                StretchToParent(offRect);
                var offImg = EnsureImage(offRect.gameObject, out bool isOffImgNew);
                if(isOffImgNew) offImg.color = Color.white;
            }
            if (offImage == null) offImage = offRect.gameObject;
        }

        private static void BuildToggleRow(Transform parent, string rowName, string labelText, float y, ref Button btn, ref TextMeshProUGUI statusTxt)
        {
            var itemRect = EnsureRectTransform(parent, rowName, false);
            itemRect.anchorMin = new Vector2(0.5f, 1f);
            itemRect.anchorMax = new Vector2(0.5f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(620f, 120f);
            itemRect.anchoredPosition = new Vector2(0f, y);

            var image = EnsureImage(itemRect.gameObject, out bool isImgNew);
            if(isImgNew) image.color = new Color(0.17f, 0.25f, 0.35f, 0.5f);

            var labelRect = EnsureRectTransform(itemRect, "RowLabel", false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.6f, 1f);
            labelRect.offsetMin = new Vector2(40f, 0f);
            labelRect.offsetMax = Vector2.zero;

            var label = labelRect.GetComponent<TextMeshProUGUI>();
            if (label == null) label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            label.fontSize = 48f;
            label.color = Color.white;

            var btnRect = EnsureRectTransform(itemRect, "ActionBtn", false);
            btnRect.anchorMin = new Vector2(1f, 0.5f);
            btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(220f, 80f);
            btnRect.anchoredPosition = new Vector2(-20f, 0f);

            var btnBg = EnsureImage(btnRect.gameObject, out bool isBtnBgNew);
            if(isBtnBgNew) btnBg.color = new Color(0.2f, 0.3f, 0.45f, 1f);

            if (btn == null)
            {
                btn = btnRect.GetComponent<Button>();
                if (btn == null) btn = btnRect.gameObject.AddComponent<Button>();
            }

            var btnLabelRect = EnsureRectTransform(btnRect, "StatusText", false);
            StretchToParent(btnLabelRect);

            if (statusTxt == null)
            {
                statusTxt = btnLabelRect.GetComponent<TextMeshProUGUI>();
                if (statusTxt == null) statusTxt = btnLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
            }
            statusTxt.alignment = TextAlignmentOptions.Center;
            statusTxt.fontSize = 36f;
        }

        private static void BuildActionRow(Transform parent, string rowName, string labelText, float y, ref Button btn)
        {
            var btnRect = EnsureRectTransform(parent, rowName, false, out bool isBtnNew);
            if (isBtnNew)
            {
                btnRect.anchorMin = new Vector2(0.5f, 1f);
                btnRect.anchorMax = new Vector2(0.5f, 1f);
                btnRect.pivot = new Vector2(0.5f, 1f);
                btnRect.sizeDelta = new Vector2(620f, 120f);
                btnRect.anchoredPosition = new Vector2(0f, y);
            }

            var btnBg = EnsureImage(btnRect.gameObject, out bool isBtnBgNew);
            if(isBtnBgNew) btnBg.color = new Color(0.15f, 0.45f, 0.25f, 0.9f);

            if (btn == null)
            {
                btn = btnRect.GetComponent<Button>();
                if (btn == null) btn = btnRect.gameObject.AddComponent<Button>();
            }

            var btnLabelRect = EnsureRectTransform(btnRect, "RowLabel", false, out bool isLabelRectNew);
            if (isLabelRectNew)
            {
                StretchToParent(btnLabelRect);
            }

            var btnLabel = btnLabelRect.GetComponent<TextMeshProUGUI>();
            bool isLabelNew = (btnLabel == null);
            if (isLabelNew) btnLabel = btnLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (isLabelNew)
            {
                btnLabel.text = labelText;
                btnLabel.alignment = TextAlignmentOptions.Center;
                btnLabel.fontSize = 48f;
                btnLabel.color = Color.white;
            }
        }

        private static RectTransform EnsureRectTransform(Transform parent, string name, bool topMost, out bool isNew)
        {
            var child = parent.Find(name);
            RectTransform rect;
            isNew = (child == null);

            if (child != null)
            {
                rect = child as RectTransform;
                if (rect == null)
                    rect = child.gameObject.AddComponent<RectTransform>();      
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                rect = go.GetComponent<RectTransform>();
            }

            if (topMost && Application.isPlaying)
                rect.SetAsLastSibling();

            return rect;
        }

        private static RectTransform EnsureRectTransform(Transform parent, string name, bool topMost)
        {
            return EnsureRectTransform(parent, name, topMost, out _);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Image EnsureImage(GameObject gameObject, out bool isNew)
        {
            var image = gameObject.GetComponent<Image>();
            isNew = (image == null);
            if (image == null)
                image = gameObject.AddComponent<Image>();

            return image;
        }

        private static Image EnsureImage(GameObject gameObject)
        {
            return EnsureImage(gameObject, out _);
        }

        private static void EnsureLabel(Transform parent, string name, string text, Vector2 anchoredPos)
        {
            var labelRect = EnsureRectTransform(parent, name, true, out bool isNew);
            if (isNew)
            {
                labelRect.anchorMin = new Vector2(0.5f, 1f);
                labelRect.anchorMax = new Vector2(0.5f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.sizeDelta = new Vector2(520f, 120f);
                labelRect.anchoredPosition = anchoredPos;
            }

            var tmp = labelRect.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = labelRect.gameObject.AddComponent<TextMeshProUGUI>();     
                tmp.text = text;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 64f;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }
        }

        private void EnsureLanguageButtons(RectTransform panel)
        {
            if (panel == null) return;

            LanguageButton trBtn = null;
            LanguageButton enBtn = null;
            LanguageButton koBtn = null;

            var existingButtons = panel.GetComponentsInChildren<LanguageButton>(true);
            foreach (var b in existingButtons)
            {
                if (b.transform.parent != panel) continue;

                var targetField = typeof(LanguageButton).GetField("targetLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    var lang = (LanguageManager.Language)targetField.GetValue(b);
                    if (lang == LanguageManager.Language.Turkish) trBtn = b;
                    else if (lang == LanguageManager.Language.English) enBtn = b;
                    else if (lang == LanguageManager.Language.Korean) koBtn = b;
                }
            }

            if (trBtn == null)
            {
                var trGO = new GameObject("TurkishButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LanguageButton));
                trGO.transform.SetParent(panel, false);
                trBtn = trGO.GetComponent<LanguageButton>();
                
                var targetField = typeof(LanguageButton).GetField("targetLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null) targetField.SetValue(trBtn, LanguageManager.Language.Turkish);

                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(trGO.transform, false);
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "Türkçe";
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 28f;

                var loc = txtGO.AddComponent<LocalizedText>();
                loc.Configure(txt, "Türkçe", "Turkish", "터키어", false);
            }

            if (enBtn == null)
            {
                var enGO = new GameObject("EnglishButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LanguageButton));
                enGO.transform.SetParent(panel, false);
                enBtn = enGO.GetComponent<LanguageButton>();
                
                var targetField = typeof(LanguageButton).GetField("targetLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null) targetField.SetValue(enBtn, LanguageManager.Language.English);

                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(enGO.transform, false);
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "English";
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 28f;

                var loc = txtGO.AddComponent<LocalizedText>();
                loc.Configure(txt, "İngilizce", "English", "영어", false);
            }

            if (koBtn == null)
            {
                var koGO = new GameObject("KoreanButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LanguageButton));
                koGO.transform.SetParent(panel, false);
                koBtn = koGO.GetComponent<LanguageButton>();
                
                var targetField = typeof(LanguageButton).GetField("targetLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null) targetField.SetValue(koBtn, LanguageManager.Language.Korean);

                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(koGO.transform, false);
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "한국어";
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 28f;

                var loc = txtGO.AddComponent<LocalizedText>();
                loc.Configure(txt, "Korece", "Korean", "한국어", false);
            }

            ConfigureLanguageButtonObject(trBtn, "Türkçe", "Turkish", "터키어");
            ConfigureLanguageButtonObject(enBtn, "İngilizce", "English", "영어");
            ConfigureLanguageButtonObject(koBtn, "Korece", "Korean", "한국어");

            float buttonsY = -750f;
            float buttonWidth = 190f;
            float buttonHeight = 70f;

            PositionLanguageButton(trBtn.GetComponent<RectTransform>(), -210f, buttonsY, buttonWidth, buttonHeight);
            PositionLanguageButton(enBtn.GetComponent<RectTransform>(), 0f, buttonsY, buttonWidth, buttonHeight);
            PositionLanguageButton(koBtn.GetComponent<RectTransform>(), 210f, buttonsY, buttonWidth, buttonHeight);
        }

        private void ConfigureLanguageButtonObject(LanguageButton langBtn, string trText, string enText, string koText)
        {
            var go = langBtn.gameObject;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            var buttonField = typeof(LanguageButton).GetField("button", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buttonField != null) buttonField.SetValue(langBtn, btn);

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();

            img.color = new Color(0.13f, 0.43f, 0.98f, 1f);

            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.82f, 0.88f, 1f, 0.94f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var txtTransform = go.transform.Find("Text");
            if (txtTransform != null)
            {
                var txt = txtTransform.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 28f;
                    txt.color = Color.white;
                }

                var loc = txtTransform.GetComponent<LocalizedText>();
                if (loc == null) loc = txtTransform.gameObject.AddComponent<LocalizedText>();
                loc.Configure(txt, trText, enText, koText, false);

                var rect = txtTransform.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
            }

            // Font management is handled by LocalizedText components on the button labels
        }

        private void PositionLanguageButton(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
        }
    }
}
