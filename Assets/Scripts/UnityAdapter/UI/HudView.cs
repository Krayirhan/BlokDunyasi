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

        [Header("Score Breakdown Debug")]
        [SerializeField] private bool showScoreBreakdownDebug = false;
        [SerializeField] private GameObject scoreBreakdownPanel;
        [SerializeField] private TextMeshProUGUI scoreBreakdownText;

        [Header("Score Reactive Glow")]
        [SerializeField] private bool enableScoreReactiveGlow = true;
        [SerializeField] [Min(1f)] private float glowNormalizationMaxScoreDelta = 120f;
        [SerializeField] private AnimationCurve glowIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] [Range(0f, 1f)] private float glowMinAlpha = 0f;
        [SerializeField] [Range(0f, 1f)] private float glowMaxAlpha = 0.75f;
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] [Min(0.05f)] private float glowDuration = 0.2f;

        [Header("Responsive Layout")]
        [SerializeField] private bool applySafeAreaLayout = true;
        [SerializeField] private Vector2 topPadding = new Vector2(40f, 40f);
        [SerializeField] private Vector2 bottomPadding = new Vector2(40f, 40f);
        [SerializeField] private Vector2 comboTopPadding = new Vector2(40f, 120f);
        
        // State tracking
        private int _displayedScore = 0;
        private int _targetScore = 0;
        private Coroutine _scoreCountAnimation;
        private Coroutine _scoreGlowAnimation;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;
        
        private void Start()
        {
            // Subscribe to game events
            GameBootstrap.OnScoreChanged += OnScoreChanged;
            GameBootstrap.OnScoreBreakdown += OnScoreBreakdown;
            GameBootstrap.OnGameStarted += OnGameStarted;
            GameBootstrap.OnGameOver += OnGameOver;
            
            // Initialize display
            InitializeDisplay();
            ApplyScoreBreakdownDebugVisibility();
            ApplyResponsiveHudLayout(force: true);
        }

        private void Update()
        {
            if (!applySafeAreaLayout)
                return;

            if (HasScreenChanged())
            {
                ApplyResponsiveHudLayout(force: true);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameBootstrap.OnScoreChanged -= OnScoreChanged;
            GameBootstrap.OnScoreBreakdown -= OnScoreBreakdown;
            GameBootstrap.OnGameStarted -= OnGameStarted;
            GameBootstrap.OnGameOver -= OnGameOver;
            
            // Stop animations
            if (_scoreCountAnimation != null)
            {
                StopCoroutine(_scoreCountAnimation);
            }

            if (_scoreGlowAnimation != null)
            {
                StopCoroutine(_scoreGlowAnimation);
            }
        }
        
        private void InitializeDisplay()
        {
            UpdateScoreDisplay(0, 0, false);
            UpdateScoreBreakdownDebug(default);
            UpdateGameInfo();
        }
        
        private void OnGameStarted()
        {
            UpdateScoreBreakdownDebug(default);
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

        private void OnScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            UpdateScoreBreakdownDebug(breakdown);
            PlayScoreReactiveGlow(breakdown.ScoreDelta);
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

        private void ApplyScoreBreakdownDebugVisibility()
        {
            if (scoreBreakdownPanel != null)
            {
                scoreBreakdownPanel.SetActive(showScoreBreakdownDebug);
            }
            else if (scoreBreakdownText != null)
            {
                scoreBreakdownText.gameObject.SetActive(showScoreBreakdownDebug);
            }
        }

        private void UpdateScoreBreakdownDebug(ScoreBreakdownInfo breakdown)
        {
            ApplyScoreBreakdownDebugVisibility();

            if (!showScoreBreakdownDebug || scoreBreakdownText == null)
                return;

            if (breakdown.FormulaVersion <= 0)
            {
                scoreBreakdownText.text = "No score breakdown yet";
                return;
            }

            scoreBreakdownText.text =
                $"v{breakdown.FormulaVersion} | base:{breakdown.BaseScore} " +
                $"line x{breakdown.LineClearMultiplier:F2} combo x{breakdown.ComboMultiplier:F2} " +
                $"=> +{breakdown.ScoreDelta} (total {breakdown.TotalScore})";
        }

        private void PlayScoreReactiveGlow(int scoreDelta)
        {
            if (!enableScoreReactiveGlow || currentScoreText == null)
                return;

            float intensity = EvaluateGlowIntensity(scoreDelta);
            if (intensity <= 0f)
                return;

            if (_scoreGlowAnimation != null)
                StopCoroutine(_scoreGlowAnimation);

            _scoreGlowAnimation = StartCoroutine(AnimateScoreGlow(intensity));
        }

        private float EvaluateGlowIntensity(int scoreDelta)
        {
            float normalized = scoreDelta <= 0
                ? 0f
                : Mathf.Clamp01(scoreDelta / Mathf.Max(1f, glowNormalizationMaxScoreDelta));

            float curved = glowIntensityCurve != null
                ? Mathf.Clamp01(glowIntensityCurve.Evaluate(normalized))
                : normalized;

            return Mathf.Clamp01(Mathf.Lerp(glowMinAlpha, glowMaxAlpha, curved));
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

        private System.Collections.IEnumerator AnimateScoreGlow(float intensity)
        {
            if (currentScoreText == null)
                yield break;

            Color original = currentScoreText.color;
            Color target = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Clamp01(intensity));

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, glowDuration);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float alphaT = 1f - t;
                currentScoreText.color = Color.Lerp(original, target, alphaT);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentScoreText.color = original;
            _scoreGlowAnimation = null;
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

        public void SetScoreBreakdownDebugVisible(bool visible)
        {
            showScoreBreakdownDebug = visible;
            ApplyScoreBreakdownDebugVisibility();
        }
        
        private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            string originalText = gameStatusText.text;
            gameStatusText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            gameStatusText.text = originalText;
        }

        private bool HasScreenChanged()
        {
            Rect safeArea = Screen.safeArea;
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                return true;

            const float threshold = 0.5f;
            return Mathf.Abs(safeArea.x - _lastSafeArea.x) > threshold ||
                   Mathf.Abs(safeArea.y - _lastSafeArea.y) > threshold ||
                   Mathf.Abs(safeArea.width - _lastSafeArea.width) > threshold ||
                   Mathf.Abs(safeArea.height - _lastSafeArea.height) > threshold;
        }

        private void CacheScreenState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
        }

        private void ApplyResponsiveHudLayout(bool force)
        {
            if (!applySafeAreaLayout)
                return;

            if (!force && !HasScreenChanged())
                return;

            StretchHudPanelToSafeArea();

            if (currentScoreText != null)
                SetToTopLeft(currentScoreText.rectTransform, topPadding);

            if (comboText != null)
                SetToTopRight(comboText.rectTransform, comboTopPadding);

            if (turnCountText != null)
                SetToBottomRight(turnCountText.rectTransform, bottomPadding);

            if (gameStatusText != null)
                SetToBottomLeft(gameStatusText.rectTransform, bottomPadding);

            CacheScreenState();
        }

        private void SetToTopLeft(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(padding.x, -padding.y);
        }

        private void SetToTopRight(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-padding.x, -padding.y);
        }

        private void SetToBottomRight(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-padding.x, padding.y);
        }

        private void SetToBottomLeft(RectTransform rect, Vector2 padding)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(padding.x, padding.y);
        }

        private void StretchHudPanelToSafeArea()
        {
            var panel = transform as RectTransform;
            if (panel == null)
                return;

            Rect safe = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);

            Vector2 min = new Vector2(safe.xMin / width, safe.yMin / height);
            Vector2 max = new Vector2(safe.xMax / width, safe.yMax / height);

            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.localScale = Vector3.one;
            panel.localRotation = Quaternion.identity;
        }

        private void OnValidate()
        {
            glowNormalizationMaxScoreDelta = Mathf.Max(1f, glowNormalizationMaxScoreDelta);
            glowDuration = Mathf.Max(0.05f, glowDuration);
            glowMinAlpha = Mathf.Clamp01(glowMinAlpha);
            glowMaxAlpha = Mathf.Clamp01(glowMaxAlpha);
            if (glowMaxAlpha < glowMinAlpha)
                glowMaxAlpha = glowMinAlpha;
        }
    }
}
