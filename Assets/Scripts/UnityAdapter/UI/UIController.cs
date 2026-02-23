// =============================================================================
// BLOK DÜNYASI - UI CONTROLLER
// Manages UI elements (score, game over, etc.)
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Controls all UI elements.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _bestScoreText;
        [SerializeField] private string _scoreFormat = "{0}";
        [SerializeField] private string _bestScoreFormat = "En İyi: {0}";

        [Header("Combo Display")]
        [SerializeField] private GameObject _comboPanel;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private float _comboDisplayDuration = 1.5f;

        [Header("Game Over Panel")]
        [SerializeField] private GameOverView _gameOverView;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private TextMeshProUGUI _newBestText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        [Header("In-Game Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pausePanel;

        // State
        private float _comboHideTime;
        private bool _showingCombo;

        // Events
        public event System.Action OnRestartClicked;
        public event System.Action OnMenuClicked;
        public event System.Action OnPauseClicked;
#pragma warning disable CS0067
        public event System.Action OnResumeClicked;
#pragma warning restore CS0067
        public event System.Action OnNewGameRequested;
        public event System.Action OnPauseRequested;

        private void Awake()
        {
            if (_gameOverView == null && _gameOverPanel != null)
            {
                _gameOverView = _gameOverPanel.GetComponent<GameOverView>();
            }

            // Setup button listeners
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(() => { OnRestartClicked?.Invoke(); OnNewGameRequested?.Invoke(); });
            }
            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(() => OnMenuClicked?.Invoke());
            }
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(() => { OnPauseClicked?.Invoke(); OnPauseRequested?.Invoke(); });
            }

            // Initial state
            HideGameOver();
            HideCombo();
            HidePause();
        }

        private void Update()
        {
            // Auto-hide combo
            if (_showingCombo && Time.time >= _comboHideTime)
            {
                HideCombo();
            }
        }

        /// <summary>
        /// Updates the score display.
        /// </summary>
        public void UpdateScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = string.Format(_scoreFormat, score);
            }
        }

        /// <summary>
        /// Updates the best score display.
        /// </summary>
        public void UpdateBestScore(int bestScore)
        {
            if (_bestScoreText != null)
            {
                _bestScoreText.text = string.Format(_bestScoreFormat, bestScore);
            }
        }

        /// <summary>
        /// Updates combo display (alias for ShowCombo).
        /// </summary>
        public void UpdateCombo(int comboLevel)
        {
            if (comboLevel > 1)
                ShowCombo(comboLevel);
            else
                HideCombo();
        }
        
        /// <summary>
        /// Shows combo notification.
        /// </summary>
        public void ShowCombo(int comboLevel, string text = null)
        {
            if (_comboPanel == null) return;

            _comboPanel.SetActive(true);
            _showingCombo = true;
            _comboHideTime = Time.time + _comboDisplayDuration;

            if (_comboText != null)
            {
                _comboText.text = text ?? GetComboText(comboLevel);
            }
        }

        /// <summary>
        /// Hides combo notification.
        /// </summary>
        public void HideCombo()
        {
            if (_comboPanel != null)
            {
                _comboPanel.SetActive(false);
            }
            _showingCombo = false;
        }

        /// <summary>
        /// Shows game over panel.
        /// </summary>
        public void ShowGameOver(int finalScore, bool isNewBest)
        {
            if (_gameOverView != null)
            {
                _gameOverView.ShowGameOver(finalScore, isNewBest);
                return;
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            if (_finalScoreText != null)
            {
                _finalScoreText.text = finalScore.ToString();
            }

            if (_newBestText != null)
            {
                _newBestText.gameObject.SetActive(isNewBest);
            }
        }

        /// <summary>
        /// Hides game over panel.
        /// </summary>
        public void HideGameOver()
        {
            if (_gameOverView != null)
            {
                _gameOverView.HideGameOver();
                return;
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows pause panel.
        /// </summary>
        public void ShowPause()
        {
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides pause panel.
        /// </summary>
        public void HidePause()
        {
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows floating score text at position.
        /// </summary>
        public void ShowFloatingScore(int points, Vector2 worldPos)
        {
            // TODO: Implement floating score text animation
            // This would create a temporary text that floats up and fades
        }

        /// <summary>
        /// Shows line clear effect text.
        /// </summary>
        public void ShowLineClearText(int lineCount)
        {
            if (_comboText != null)
            {
                string text = lineCount switch
                {
                    1 => "Temizlendi!",
                    2 => "Çift Temizlik!",
                    3 => "Üçlü Temizlik!",
                    4 => "Dörtlü Temizlik!",
                    _ => $"{lineCount}x Temizlik!"
                };
                ShowCombo(lineCount, text);
            }
        }

        private string GetComboText(int comboLevel)
        {
            return comboLevel switch
            {
                1 => "Kombo!",
                2 => "x2 Kombo!",
                3 => "x3 Kombo!",
                4 => "x4 Kombo!",
                _ => $"x{comboLevel} Kombo!"
            };
        }
    }
}
