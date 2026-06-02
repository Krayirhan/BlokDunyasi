using System.Collections.Generic;
using BlockPuzzle.Core.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    public class LanguageManager : MonoBehaviour
    {
        public enum Language
        {
            Turkish,
            English,
            Korean
        }

        private const string LanguagePrefsKey = SettingsKeys.SelectedLanguage;
        private static LanguageManager _instance;

        private readonly List<ILanguageListener> _listeners = new List<ILanguageListener>();
        private Language _currentLanguage = Language.Turkish;

        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LanguageManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[LanguageManager]");
                        _instance = go.AddComponent<LanguageManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        public Language CurrentLanguage
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        public string CurrentLanguageName
        {
            get
            {
                if (_currentLanguage == Language.Korean)
                    return "\uD55C\uAD6D\uC5B4";

                if (_currentLanguage == Language.English)
                    return "English";

                return "Turkce";
            }
        }

        private void Awake()
        {
            var allManagers = FindObjectsByType<LanguageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allManagers.Length; i++)
            {
                var candidate = allManagers[i];
                if (candidate != null && candidate != this)
                {
                    _instance = this;
                    Destroy(candidate.gameObject);
                }
            }

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLanguagePreference();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            LocalizedFontUtility.ApplyForLanguage(_currentLanguage);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void LoadLanguagePreference()
        {
            if (PlayerPrefs.HasKey(SettingsKeys.Language))
            {
                _currentLanguage = FromLanguageCode(PlayerPrefs.GetString(SettingsKeys.Language, "tr"));
            }
            else if (PlayerPrefs.HasKey(LanguagePrefsKey))
            {
                int savedLang = PlayerPrefs.GetInt(LanguagePrefsKey, (int)Language.Turkish);
                _currentLanguage = savedLang >= 0 && savedLang <= (int)Language.Korean
                    ? (Language)savedLang
                    : Language.Turkish;
            }
            else
            {
                _currentLanguage = DetectSystemLanguage();
            }

            SaveLanguagePreference(_currentLanguage);
            PlayerPrefs.Save();
            Debug.Log($"[LanguageManager] LoadLanguagePreference: {_currentLanguage}");
        }

        public void SetLanguage(Language language)
        {
            SetLanguage(language, false);
        }

        public void SetLanguage(Language language, bool forceRefresh)
        {
            if (_currentLanguage == language && !forceRefresh)
                return;

            _currentLanguage = language;
            SaveLanguagePreference(language);
            PlayerPrefs.Save();

            LocalizedFontUtility.ApplyForLanguage(_currentLanguage);
            NotifyListeners();
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[LanguageManager] Language changed to: {language}");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LocalizedFontUtility.ApplyForLanguage(_currentLanguage);
            NotifyListeners();
            Canvas.ForceUpdateCanvases();
        }

        private void NotifyListeners()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] == null)
                {
                    _listeners.RemoveAt(i);
                }
                else
                {
                    try
                    {
                        _listeners[i].OnLanguageChanged(_currentLanguage);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        public void Subscribe(ILanguageListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
                listener.OnLanguageChanged(_currentLanguage);
            }
        }

        public void Unsubscribe(ILanguageListener listener)
        {
            if (listener != null)
                _listeners.Remove(listener);
        }

        private static void SaveLanguagePreference(Language language)
        {
            PlayerPrefs.SetInt(LanguagePrefsKey, (int)language);
            PlayerPrefs.SetString(SettingsKeys.Language, ToLanguageCode(language));
        }

        private static Language FromLanguageCode(string languageCode)
        {
            if (string.Equals(languageCode, "en", System.StringComparison.OrdinalIgnoreCase))
                return Language.English;

            if (string.Equals(languageCode, "ko", System.StringComparison.OrdinalIgnoreCase))
                return Language.Korean;

            return Language.Turkish;
        }

        private static string ToLanguageCode(Language language)
        {
            if (language == Language.English)
                return "en";

            if (language == Language.Korean)
                return "ko";

            return "tr";
        }

        private static Language DetectSystemLanguage()
        {
            if (Application.systemLanguage == SystemLanguage.Korean)
                return Language.Korean;

            if (Application.systemLanguage == SystemLanguage.Turkish)
                return Language.Turkish;

            return Language.English;
        }
    }

    public interface ILanguageListener
    {
        void OnLanguageChanged(LanguageManager.Language newLanguage);
    }
}
