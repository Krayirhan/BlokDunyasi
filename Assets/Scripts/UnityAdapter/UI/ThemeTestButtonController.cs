using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ThemeTestButtonController : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private string buttonText = "TEMA";
        [SerializeField] private bool verboseLogs = true;

        private GameSceneThemeController _themeController;
        private RectTransform _rectTransform;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);

            _rectTransform = transform as RectTransform;
            EnsureLabel();

            if (label != null && !string.IsNullOrWhiteSpace(buttonText))
                label.text = buttonText;

            if (backgroundImage != null)
                backgroundImage.color = new Color(0.07f, 0.2f, 0.24f, 0.96f);

            if (button != null)
            {
                button.onClick.RemoveListener(CycleTheme);
                button.onClick.AddListener(CycleTheme);
            }
        }

        private void Update()
        {
            bool newInputPressed = Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
            bool legacyInputPressed = UnityEngine.Input.GetKeyDown(KeyCode.T);
            if (newInputPressed || legacyInputPressed)
                CycleTheme();
        }

        public void CycleTheme()
        {
            int currentThemeId = UISettingsProfile.GetThemeId();
            int nextThemeId = currentThemeId >= UISettingsProfile.ThemeClassic &&
                              currentThemeId <= UISettingsProfile.ThemeWood
                ? (currentThemeId + 1) % 4
                : UISettingsProfile.ThemeClassic;

            if (_themeController == null)
                _themeController = GameSceneThemeController.GetOrCreateRuntimeController();

            if (_themeController != null)
                _themeController.ApplyManualThemeById(nextThemeId);
            else
                UISettingsProfile.SetThemeId(nextThemeId);

            if (verboseLogs)
                Debug.Log($"[ThemeTestButtonController] Theme changed: {currentThemeId} -> {nextThemeId}, controllerFound={_themeController != null}");
        }

        private void EnsureLabel()
        {
            if (label != null)
                return;

            var labelGo = new GameObject("ThemeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(transform, false);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.97f, 0.94f, 0.84f, 1f);
            text.raycastTarget = false;
            label = text;
        }
    }
}
