// File: UnityAdapter/UI/HudView.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockPuzzle.UnityAdapter.Boot;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Manages the heads-up display showing score, best score, and game information.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        
        [Header("Game Info")]
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI gameStatusText;
        
        [Header("Animation")]
        [SerializeField] private float scoreCountDuration = 0.5f;
        [SerializeField] private AnimationCurve scoreCountCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // State tracking
        private int _displayedScore = 0;
        private int _targetScore = 0;
        private Coroutine _scoreCountAnimation;
        
        private void Start()
        {
            // Subscribe to game events
            GameBootstrap.OnScoreChanged += OnScoreChanged;
            GameBootstrap.OnGameStarted += OnGameStarted;
            GameBootstrap.OnGameOver += OnGameOver;
            
            // Initialize display
            InitializeDisplay();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameBootstrap.OnScoreChanged -= OnScoreChanged;
            GameBootstrap.OnGameStarted -= OnGameStarted;
            GameBootstrap.OnGameOver -= OnGameOver;
            
            // Stop animations
            if (_scoreCountAnimation != null)
            {
                StopCoroutine(_scoreCountAnimation);
            }
        }
        
        private void InitializeDisplay()
        {
            UpdateScoreDisplay(0, 0, false);
            UpdateGameInfo();
        }
        
        private void OnGameStarted()
        {
            UpdateGameInfo();
            if (gameStatusText != null)
            {
                gameStatusText.text = "Oynuyor";
            }
        }
        
        private void OnScoreChanged(int currentScore, int bestScore, bool isNewBest)
        {
            UpdateScoreDisplay(currentScore, bestScore, isNewBest);
            UpdateGameInfo();
        }
        
        private void OnGameOver(int finalScore)
        {
            if (gameStatusText != null)
            {
                gameStatusText.text = "Oyun Bitti";
            }
            UpdateGameInfo();
        }
        
        private void UpdateScoreDisplay(int currentScore, int bestScore, bool isNewBest)
        {
            // Animate score counting
            _targetScore = currentScore;
            
            if (_scoreCountAnimation != null)
            {
                StopCoroutine(_scoreCountAnimation);
            }
            _scoreCountAnimation = StartCoroutine(AnimateScoreCount());
            
            // Update best score
            if (bestScoreText != null)
            {
                bestScoreText.text = $"En İyi: {bestScore:N0}";
                
                if (isNewBest)
                {
                    // Flash animation for new best
                    StartCoroutine(FlashNewBest());
                }
            }
        }
        
        private void UpdateGameInfo()
        {
            var gameBootstrap = FindFirstObjectByType<GameBootstrap>();
            if (gameBootstrap == null) return;
            
            // Update turn count (use move count from game state)
            if (turnCountText != null)
            {
                var moveCount = gameBootstrap.CurrentState?.MoveCount ?? 0;
                turnCountText.text = $"Hamle: {moveCount}";
            }
            
            // Update combo info
            if (comboText != null)
            {
                var gameState = gameBootstrap.CurrentState;
                if (gameState != null)
                {
                    int comboStreak = gameState.Combo;
                    if (comboStreak > 1)
                    {
                        comboText.text = $"Combo x{comboStreak}";
                        comboText.gameObject.SetActive(true);
                    }
                    else
                    {
                        comboText.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        private System.Collections.IEnumerator AnimateScoreCount()
        {
            int startScore = _displayedScore;
            int endScore = _targetScore;
            
            if (startScore == endScore)
            {
                yield break;
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < scoreCountDuration)
            {
                float t = elapsedTime / scoreCountDuration;
                float curveValue = scoreCountCurve.Evaluate(t);
                
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, curveValue));
                
                if (currentScoreText != null)
                {
                    currentScoreText.text = $"Skor: {_displayedScore:N0}";
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final value
            _displayedScore = endScore;
            if (currentScoreText != null)
            {
                currentScoreText.text = $"Skor: {_displayedScore:N0}";
            }
        }
        
        private System.Collections.IEnumerator FlashNewBest()
        {
            if (bestScoreText == null) yield break;
            
            Color originalColor = bestScoreText.color;
            Color flashColor = Color.yellow;
            
            // Flash sequence
            for (int i = 0; i < 3; i++)
            {
                bestScoreText.color = flashColor;
                yield return new WaitForSeconds(0.1f);
                bestScoreText.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// Shows a temporary message on the HUD.
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="duration">How long to show the message</param>
        public void ShowMessage(string message, float duration = 2f)
        {
            if (gameStatusText != null)
            {
                StartCoroutine(ShowTemporaryMessage(message, duration));
            }
        }
        
        private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            string originalText = gameStatusText.text;
            gameStatusText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            gameStatusText.text = originalText;
        }
    }
}