#pragma warning disable 0414
using TMPro;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public sealed class LocalizedText : MonoBehaviour, ILanguageListener
    {
        [SerializeField] private TextMeshProUGUI textComponent;
        [SerializeField] private string turkishText = string.Empty;
        [SerializeField] private string englishText = string.Empty;
        [SerializeField] private string koreanText = string.Empty;
        [SerializeField] private bool useNameIfEmpty = true;
        [SerializeField] private bool useKoreanFont = true;

        private TMP_FontAsset _originalFont;
        private Material _originalMaterial;
        private bool _subscribed;

        private void Awake()
        {
            ResolveText();
            CacheOriginalFont();
        }

        private void OnEnable()
        {
            ResolveText();
            CacheOriginalFont();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(TextMeshProUGUI targetTextComponent, string trText, string enText, string koText = "", bool fallbackToObjectName = false)
        {
            textComponent = targetTextComponent != null ? targetTextComponent : GetComponent<TextMeshProUGUI>();
            turkishText = trText ?? string.Empty;
            englishText = enText ?? string.Empty;
            koreanText = koText ?? string.Empty;
            useNameIfEmpty = fallbackToObjectName;
            CacheOriginalFont();
            Refresh();
        }

        public void Configure(TextMeshProUGUI targetTextComponent, string trText, string enText, bool fallbackToObjectName)
        {
            Configure(targetTextComponent, trText, enText, string.Empty, fallbackToObjectName);
        }

        public void OnLanguageChanged(LanguageManager.Language newLanguage)
        {
            Refresh(newLanguage);
        }

        private void Refresh()
        {
            Refresh(LanguageManager.Instance.CurrentLanguage);
        }

        private void Refresh(LanguageManager.Language language)
        {
            if (textComponent == null)
                return;

            string value = ResolveValue(language);
            if (string.IsNullOrEmpty(value) && useNameIfEmpty)
                value = gameObject.name;

            textComponent.text = value;

            try
            {
                ApplyFont(language);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[LocalizedText] ApplyFont failed on '{gameObject.name}': {e.Message}");
            }

            textComponent.SetAllDirty();

            try
            {
                textComponent.ForceMeshUpdate();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[LocalizedText] ForceMeshUpdate failed on '{gameObject.name}': {e.Message}");
            }
        }

        private string ResolveValue(LanguageManager.Language language)
        {
            if (language == LanguageManager.Language.English)
                return string.IsNullOrEmpty(englishText) ? turkishText : englishText;

            if (language == LanguageManager.Language.Korean)
                return string.IsNullOrEmpty(koreanText) ? (string.IsNullOrEmpty(englishText) ? turkishText : englishText) : koreanText;

            return turkishText;
        }

        private void ApplyFont(LanguageManager.Language language)
        {
            if (textComponent == null)
                return;

            TMP_FontAsset font = LocalizedFontUtility.ResolveTmpFont(language, _originalFont);
            if (font != null)
            {
                textComponent.font = font;
                if (font.material != null)
                    textComponent.fontSharedMaterial = font.material;

                LocalizedFontUtility.EnsureFallback(font);
            }
        }

        private void ResolveText()
        {
            if (textComponent == null)
                textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void CacheOriginalFont()
        {
            if (textComponent == null)
                return;

            if (_originalFont == null)
                _originalFont = textComponent.font;

            if (_originalMaterial == null)
                _originalMaterial = textComponent.fontSharedMaterial;
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            LanguageManager.Instance.Subscribe(this);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            LanguageManager.Instance.Unsubscribe(this);
            _subscribed = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveText();
        }
#endif
    }
}
