using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public sealed class LanguageButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private LanguageManager.Language targetLanguage = LanguageManager.Language.Turkish;

        private void Awake()
        {
            ResolveButton();
        }

        private void OnEnable()
        {
            ResolveButton();
            if (button != null)
            {
                button.onClick.RemoveListener(SetLanguage);
                button.onClick.AddListener(SetLanguage);
            }
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(SetLanguage);
        }

        private void SetLanguage()
        {
            LanguageManager.Instance.SetLanguage(targetLanguage, true);
        }

        private void ResolveButton()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveButton();
        }
#endif
    }
}
