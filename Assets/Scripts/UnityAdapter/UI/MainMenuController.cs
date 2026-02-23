using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.UnityAdapter;
using BlockPuzzle.UnityAdapter.Boot;

namespace BlockPuzzle.UnityAdapter.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Continue")]
        [SerializeField] private Button continueButton;
        [Header("Scene Names (Build Settings'te ekli olmalı)")]
        [SerializeField] private string gameSceneName = "OyunEkranı";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string settingsSceneName = "Settings";
        [SerializeField] private string scoresSceneName = "Scores";

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private const string SaveKey = "default";
        private UnityPlayerPrefsDataProvider _dataProvider;

        private void Awake()
        {
            _dataProvider = new UnityPlayerPrefsDataProvider();

            if (continueButton == null)
                continueButton = FindButton("ContinueButton");

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueGame);
                continueButton.onClick.AddListener(ContinueGame);
            }
        }

        private void Start()
        {
            RefreshContinueButton();
        }

        public void StartGame()
        {
            GameLaunchState.RequestNewGame();

            if (verboseLogs)
                Debug.Log($"[MainMenuController] StartGame clicked. Loading scene: {gameSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void ContinueGame()
        {
            if (!HasContinueGame())
            {
                if (verboseLogs)
                    Debug.Log("[MainMenuController] Continue requested but no saved game. Starting new game.");
                StartGame();
                return;
            }

            GameLaunchState.RequestContinue();

            if (verboseLogs)
                Debug.Log($"[MainMenuController] ContinueGame clicked. Loading scene: {gameSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void ReturnToMainMenu()
        {
            if (verboseLogs)
                Debug.Log($"[MainMenuController] ReturnToMainMenu clicked. Loading scene: {mainMenuSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{mainMenuSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void OpenSettings()
        {
            if (verboseLogs)
                Debug.Log($"[MainMenuController] OpenSettings clicked. Loading scene: {settingsSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(settingsSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{settingsSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(settingsSceneName);
        }

        public void OpenScores()
        {
            if (verboseLogs)
                Debug.Log($"[MainMenuController] OpenScores clicked. Loading scene: {scoresSceneName}");

            if (!Application.CanStreamedLevelBeLoaded(scoresSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{scoresSceneName}' cannot be loaded. Check Build Settings.");
                return;
            }

            SceneManager.LoadScene(scoresSceneName);
        }

        public void QuitGame()
        {
            if (verboseLogs) Debug.Log("[MainMenuController] Quit clicked");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool HasContinueGame()
        {
            if (_dataProvider == null)
                return false;

            var data = _dataProvider.LoadGameDataAsync(SaveKey).GetAwaiter().GetResult();
            return data != null && !data.IsGameOver;
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
                return;

            continueButton.interactable = HasContinueGame();
        }

        private static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }
    }
}
