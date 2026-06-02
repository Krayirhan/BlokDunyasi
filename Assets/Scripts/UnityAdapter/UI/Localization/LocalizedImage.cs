using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public sealed class LocalizedImage : MonoBehaviour, ILanguageListener
    {
        [SerializeField] private Image imageComponent;
        [SerializeField] private Sprite turkishSprite;
        [SerializeField] private Sprite englishSprite;
        [SerializeField] private Sprite koreanSprite;
        [SerializeField] private bool applyNativeSize;

        private bool _subscribed;

        private void Awake()
        {
            ResolveImage();
        }

        private void OnEnable()
        {
            ResolveImage();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
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
            if (imageComponent == null)
                return;

            Sprite sprite = ResolveSprite(language);
            if (sprite == null)
                return;

            imageComponent.sprite = sprite;
            if (applyNativeSize)
                imageComponent.SetNativeSize();
        }

        private Sprite ResolveSprite(LanguageManager.Language language)
        {
            if (language == LanguageManager.Language.English)
                return englishSprite != null ? englishSprite : (turkishSprite != null ? turkishSprite : koreanSprite);

            if (language == LanguageManager.Language.Korean)
                return koreanSprite != null ? koreanSprite : (englishSprite != null ? englishSprite : turkishSprite);

            return turkishSprite != null ? turkishSprite : (englishSprite != null ? englishSprite : koreanSprite);
        }

        private void ResolveImage()
        {
            if (imageComponent == null)
                imageComponent = GetComponent<Image>();
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
            ResolveImage();
        }
#endif
    }
}
