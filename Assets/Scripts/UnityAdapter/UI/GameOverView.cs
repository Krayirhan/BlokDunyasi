// File: UnityAdapter/UI/GameOverView.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockPuzzle.UnityAdapter.Boot;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// GameOver ekranını yönetir.
    /// Script GameOverPanel üzerinde duruyorsa panel kapalıyken Start() çalışmayabilir;
    /// bu yüzden event subscribe ve button binding Awake() içinde yapılır.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI newBestText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "OyunEkranı";

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private GameBootstrap _gameBootstrap;
        private CanvasGroup _canvasGroup;
        private bool _subscribed;
        private bool CanLog => verboseLogs && Debug.isDebugBuild;

        private void Awake()
        {
            // 1) Panel referansı yoksa "kendi GameObject'i" panel kabul et
            if (gameOverPanel == null)
                gameOverPanel = this.gameObject;

            // 2) CanvasGroup garanti
            _canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();

            // 3) Bootstrap bul
            EnsureBootstrap();

            // 4) UI referanslarını (opsiyonel) otomatik bul
            AutoWireIfMissing();

            // 5) Event'lere subscribe
            SubscribeOnce();

            // 6) Button listener'ları
            SetupButtonListeners();

            // 7) Başlangıçta paneli gizle
            HideGameOverScreenImmediate();

            // 8) UI altyapısını diagnostik et
            DiagnoseUiPipeline("Awake");
        }

        private void OnDestroy()
        {
            UnsubscribeOnce();
            RemoveButtonListeners();
        }

        // -------------------------------------------------------
        // Subscriptions
        // -------------------------------------------------------

        private void SubscribeOnce()
        {
            if (_subscribed) return;

            GameBootstrap.OnGameOver += OnGameOver;
            GameBootstrap.OnGameStarted += OnGameStarted;
            _subscribed = true;

            if (CanLog)
                Debug.Log("[GameOverView] Subscribed to GameBootstrap.OnGameOver / OnGameStarted");
        }

        private void UnsubscribeOnce()
        {
            if (!_subscribed) return;

            GameBootstrap.OnGameOver -= OnGameOver;
            GameBootstrap.OnGameStarted -= OnGameStarted;
            _subscribed = false;

            if (CanLog)
                Debug.Log("[GameOverView] Unsubscribed events");
        }

        // -------------------------------------------------------
        // UI wiring / diagnostics
        // -------------------------------------------------------

        private void EnsureBootstrap()
        {
            if (_gameBootstrap == null)
                _gameBootstrap = FindFirstObjectByType<GameBootstrap>();

            if (CanLog)
                Debug.Log($"[GameOverView] Bootstrap: {(_gameBootstrap != null ? "FOUND" : "NULL")}");
        }

        private void AutoWireIfMissing()
        {
            if (gameOverPanel == null) return;

            if (finalScoreText == null)
                finalScoreText = FindTMP(gameOverPanel.transform, "FinalScoreText");

            if (bestScoreText == null)
                bestScoreText = FindTMP(gameOverPanel.transform, "BestScoreText");

            if (newBestText == null)
                newBestText = FindTMP(gameOverPanel.transform, "NewBestText");

            if (restartButton == null)
                restartButton = FindButton(gameOverPanel.transform, "RestartButton");

            if (mainMenuButton == null)
                mainMenuButton = FindButton(gameOverPanel.transform, "MainMenuButton");

            if (CanLog)
            {
                Debug.Log(
                    $"[GameOverView] AutoWire -> finalScoreText={(finalScoreText != null)}, bestScoreText={(bestScoreText != null)}, newBestText={(newBestText != null)}, " +
                    $"restartButton={(restartButton != null)}, mainMenuButton={(mainMenuButton != null)}"
                );
            }
        }

        private void DiagnoseUiPipeline(string where)
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                Debug.LogError($"[GameOverView] ({where}) EventSystem YOK! UI butonları çalışmaz.");
            }
            else if (CanLog)
            {
                Debug.Log($"[GameOverView] ({where}) EventSystem: {es.name} (current={EventSystem.current?.name})");
            }

            var canvas = gameOverPanel != null ? gameOverPanel.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                Debug.LogError($"[GameOverView] ({where}) Canvas bulunamadı! GameOverPanel bir Canvas altında olmalı.");
            }
            else if (CanLog)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log(
                    $"[GameOverView] ({where}) Canvas={canvas.name}, RenderMode={canvas.renderMode}, SortingOrder={canvas.sortingOrder}, " +
                    $"GraphicRaycaster={(raycaster != null ? "OK" : "MISSING")}"
                );
            }
        }

        private void SetupButtonListeners()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() =>
                {
                if (CanLog) Debug.Log("[GameOverView] RestartButton CLICK received");
                    RestartGame();
                });

                if (CanLog) Debug.Log("[GameOverView] RestartButton listener added (click -> RestartGame)");
            }
            else
            {
                Debug.LogWarning("[GameOverView] RestartButton NULL. Inspector'dan bağla veya isimle bulunamadı.");
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() =>
                {
                if (CanLog) Debug.Log("[GameOverView] MainMenuButton CLICK received");
                    ReturnToMainMenu();
                });

                if (CanLog) Debug.Log("[GameOverView] MainMenuButton listener added (click -> ReturnToMainMenu)");
            }
            else
            {
                Debug.LogWarning("[GameOverView] MainMenuButton NULL. Inspector'dan bağla veya isimle bulunamadı.");
            }
        }

        private void RemoveButtonListeners()
        {
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
        }

        // -------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------

        private void OnGameStarted()
        {
            if (CanLog) Debug.Log("[GameOverView] OnGameStarted -> hide panel");
            HideGameOverScreenImmediate();
        }

        private void OnGameOver(int finalScore)
        {
            if (CanLog)
                Debug.Log($"[GameOverView] OnGameOver RECEIVED! FinalScore: {finalScore}");

            ShowGameOverScreen(finalScore);
        }

        // -------------------------------------------------------
        // Show / hide
        // -------------------------------------------------------

        public void ShowGameOver(int finalScore, bool? isNewBestOverride = null)
        {
            ShowGameOverScreen(finalScore, isNewBestOverride);
        }

        public void HideGameOver()
        {
            HideGameOverScreenImmediate();
        }

        private void ShowGameOverScreen(int finalScore, bool? isNewBestOverride = null)
        {
            if (gameOverPanel == null)
            {
                Debug.LogError("[GameOverView] gameOverPanel is NULL!");
                return;
            }

            EnsureBootstrap();

            // Paneli aç
            gameOverPanel.SetActive(true);

            // Görünürlük ve input tuzaklarını kaldır
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            gameOverPanel.transform.localScale = Vector3.one;

            // Skor bilgileri
            int bestScore = _gameBootstrap != null ? _gameBootstrap.BestScore : finalScore;
            bool isNewBest = isNewBestOverride ?? (finalScore >= bestScore && finalScore > 0);

            if (finalScoreText != null) finalScoreText.text = $"Skor: {finalScore}";
            if (bestScoreText != null) bestScoreText.text = $"En İyi: {bestScore}";

            if (newBestText != null)
            {
                newBestText.gameObject.SetActive(isNewBest);
                if (isNewBest) newBestText.text = "YENİ REKOR!";
            }

            DiagnoseUiPipeline("ShowGameOverScreen");

            if (CanLog)
                Debug.Log($"[GameOverView] Panel activated. activeInHierarchy={gameOverPanel.activeInHierarchy}, alpha={_canvasGroup.alpha}");
        }

        private void HideGameOverScreenImmediate()
        {
            if (gameOverPanel == null) return;

            gameOverPanel.SetActive(false);

            // Bir dahaki açılışta alpha 0 / raycast kilidi olmasın
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        // -------------------------------------------------------
        // Actions
        // -------------------------------------------------------

        /// <summary>
        /// Restart: Oyun sahnesini yeniden yükler (en sağlam reset).
        /// </summary>
        public void RestartGame()
        {
            if (CanLog) Debug.Log("[GameOverView] RestartGame called");

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[GameOverView] Restart FAILED: Scene '{gameSceneName}' yüklenemiyor. Build Settings'e ekli mi?");
                return;
            }

            HideGameOverScreenImmediate();

            if (CanLog) Debug.Log($"[GameOverView] Loading game scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Main menu: MainMenu sahnesini yükler.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (CanLog) Debug.Log("[GameOverView] ReturnToMainMenu called");

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[GameOverView] MainMenu load FAILED: Scene '{mainMenuSceneName}' yüklenemiyor. Build Settings'e ekli mi?");
                return;
            }

            HideGameOverScreenImmediate();

            if (CanLog) Debug.Log($"[GameOverView] Loading main menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // -------------------------------------------------------
        // Helpers (deep search)
        // -------------------------------------------------------

        private TextMeshProUGUI FindTMP(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button FindButton(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
