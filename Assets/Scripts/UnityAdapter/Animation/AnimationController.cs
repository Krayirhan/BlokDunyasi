using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Animation
{
    /// <summary>
    /// Merkez animasyon yöneticisi - Tüm oyun animasyonlarını kontrol eder.
    /// Coroutine tabanlı, DOTween'e bağımlı olmayan, saf Unity çözümü.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        [Header("=== PERFORMANCE ===")]
        [SerializeField] [Min(10)] private int maxConcurrentAnimations = 50;

        [Header("=== BLOCK BREAK FLASH ===")]
        [SerializeField] [Range(0f, 0.5f)] private float blockBreakFlashWhiteBlend = 0.12f;
        [SerializeField] [Range(0.01f, 0.12f)] private float blockBreakFlashDuration = 0.04f;

        [Header("=== CAMERA SHAKE TUNING ===")]
        [SerializeField] [Range(0.001f, 0.05f)] private float cameraShakePositionMultiplier = 0.01f;
        [SerializeField] [Range(1f, 60f)] private float cameraShakeNoiseFrequency = 24f;
        [SerializeField] [Range(1f, 40f)] private float cameraShakeDamping = 14f;
        [SerializeField] [Range(0.1f, 5f)] private float maxShakeIntensity = 2.5f;

        private static AnimationController _instance;
        public static AnimationController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AnimationController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AnimationController");
                        _instance = go.AddComponent<AnimationController>();
                    }
                }
                return _instance;
            }
        }

        // Animasyon state tracking
        private Dictionary<string, ActiveAnimation> _activeAnimations = new Dictionary<string, ActiveAnimation>();
        private Queue<Coroutine> _coroutinePool = new Queue<Coroutine>();
        private Dictionary<int, CameraShakeState> _cameraShakeStates = new Dictionary<int, CameraShakeState>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Block Animations

        /// <summary>
        /// Bloğun yerleştirilme animasyonu
        /// </summary>
        public void PlayBlockPlacementAnim(GameObject blockObject, Action onComplete = null)
        {
            if (blockObject == null) return;

            string animId = $"block_place_{blockObject.GetInstanceID()}";
            CancelAnimation(animId);

            Coroutine routine = StartCoroutine(BlockPlacementAnimRoutine(blockObject, 0.3f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = blockObject, Routine = routine };
        }

        private IEnumerator BlockPlacementAnimRoutine(GameObject blockObject, float duration, Action onComplete)
        {
            Transform target = blockObject.transform;
            Vector3 baseScale = target.localScale;
            Vector3 lastAppliedScale = baseScale;
            float elapsed = 0f;

            // Scale settle anim (1.0 → 0.95 → 1.0) = landing feel
            while (elapsed < duration * 0.5f)
            {
                if (blockObject == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                float eased = EaseOutQuad(t);
                float factor = Mathf.Lerp(1f, 0.95f, eased);
                ApplyRelativeScale(target, ref baseScale, ref lastAppliedScale, factor);
                yield return null;
            }

            // Return to original scale
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                if (blockObject == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                float eased = EaseInQuad(t);
                float factor = Mathf.Lerp(0.95f, 1f, eased);
                ApplyRelativeScale(target, ref baseScale, ref lastAppliedScale, factor);
                yield return null;
            }

            ApplyRelativeScale(target, ref baseScale, ref lastAppliedScale, 1f);
            onComplete?.Invoke();
        }

        private static void ApplyRelativeScale(
            Transform target,
            ref Vector3 baseScale,
            ref Vector3 lastAppliedScale,
            float nextFactor)
        {
            Vector3 currentScale = target.localScale;
            if (!Approximately(currentScale, lastAppliedScale))
                baseScale = currentScale;

            lastAppliedScale = new Vector3(baseScale.x * nextFactor, baseScale.y * nextFactor, baseScale.z);
            target.localScale = lastAppliedScale;
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.0001f &&
                   Mathf.Abs(a.y - b.y) < 0.0001f &&
                   Mathf.Abs(a.z - b.z) < 0.0001f;
        }

        /// <summary>
        /// Satır temizleme efekti - Cascade + particles
        /// </summary>
        public void PlayLineClearEffect(System.Collections.Generic.List<int> clearedRowIndices, float gridCellSize, Action onComplete = null)
        {
            if (clearedRowIndices == null || clearedRowIndices.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            string animId = $"line_clear_{Time.frameCount}";
            
            float highlightDuration = 0.2f;
            float burstDuration = 0.2f;
            float vanishDuration = 0.4f;
            float totalDuration = highlightDuration + burstDuration + vanishDuration;

            Debug.Log($"[AnimationController] Playing line clear for {clearedRowIndices.Count} rows");

            StartCoroutine(LineClearSequence(highlightDuration, burstDuration, vanishDuration, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, Duration = totalDuration };
        }

        private IEnumerator LineClearSequence(float highlightDur, float burstDur, float vanishDur, Action onComplete)
        {
            // Phase 1: Highlight (cells blink)
            yield return new WaitForSeconds(highlightDur);

            // Phase 2: Burst (visual pop)
            // TODO: Parçacık efekti burada eklenebilir
            yield return new WaitForSeconds(burstDur);

            // Phase 3: Vanish (fade + rotate)
            yield return new WaitForSeconds(vanishDur);

            onComplete?.Invoke();
        }

        #endregion

        #region Block Break Animation

        /// <summary>
        /// Profesyonel blok kırılma efekti - Squash + Flash + Particles + Fade
        /// </summary>
        public void PlayBlockBreakEffect(Transform blockTransform, Color blockColor, Action onComplete = null)
        {
            if (blockTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            string animId = $"block_break_{blockTransform.GetInstanceID()}";
            CancelAnimation(animId);

            StartCoroutine(BlockBreakSequence(blockTransform, blockColor, 0.25f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation 
            { 
                Id = animId, 
                TargetObject = blockTransform.gameObject,
                Duration = 0.25f
            };
        }

        private IEnumerator BlockBreakSequence(Transform blockTransform, Color blockColor, float totalDuration, Action onComplete)
        {
            Vector3 originalScale = blockTransform.localScale;
            SpriteRenderer spriteRenderer = blockTransform.GetComponent<SpriteRenderer>();
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            // Phase 1: Soft Squash (0.08s) - 1.0 → 0.85 (subtle, not harsh)
            float squashDuration = 0.08f;
            float elapsed = 0f;

            while (elapsed < squashDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (squashDuration * 0.5f);
                float eased = EaseInQuad(t);
                blockTransform.localScale = Vector3.Lerp(originalScale, originalScale * 0.85f, eased);
                yield return null;
            }

            // Soft squash recovery
            elapsed = 0f;
            while (elapsed < squashDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (squashDuration * 0.5f);
                float eased = EaseOutQuad(t);
                blockTransform.localScale = Vector3.Lerp(originalScale * 0.85f, originalScale, eased);
                yield return null;
            }

            blockTransform.localScale = originalScale;

            // Phase 2: Soft Glow Flash (0.05s) - Bright but not white
            if (spriteRenderer != null)
            {
                float flashDuration = blockBreakFlashDuration;
                Color brightColor = blockColor;
                brightColor = Color.Lerp(brightColor, Color.white, blockBreakFlashWhiteBlend);
                
                elapsed = 0f;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flashDuration;
                    spriteRenderer.color = Color.Lerp(originalColor, brightColor, t);
                    yield return null;
                }

                // Fade to transparent (0.12s)
                elapsed = 0f;
                float fadeDuration = 0.12f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    float eased = EaseInQuad(t);
                    Color fadeColor = originalColor;
                    fadeColor.a = Mathf.Lerp(1f, 0f, eased);
                    spriteRenderer.color = fadeColor;
                    yield return null;
                }

                // Final state
                Color finalColor = originalColor;
                finalColor.a = 0f;
                spriteRenderer.color = finalColor;
            }

            onComplete?.Invoke();
        }

        #endregion

        #region UI Animations

        /// <summary>
        /// Skor artış animasyonu + burst
        /// </summary>
        public void PlayScoreBurstAnim(UnityEngine.UI.Text scoreText, int scoreIncrease, Action onComplete = null)
        {
            if (scoreText == null) return;

            string animId = $"score_burst_{scoreText.GetInstanceID()}";
            CancelAnimation(animId);

            StartCoroutine(ScoreBurstAnimRoutine(scoreText.transform, 0.4f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = scoreText.gameObject };
        }

        private IEnumerator ScoreBurstAnimRoutine(Transform scoreTransform, float duration, Action onComplete)
        {
            Vector3 originalScale = scoreTransform.localScale;
            
            // Burst up
            float elapsed = 0f;
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.3f);
                float eased = EaseOutQuad(t);
                scoreTransform.localScale = Vector3.Lerp(originalScale, new Vector3(1.15f, 1.15f, 1f), eased);
                yield return null;
            }

            // Retract
            elapsed = 0f;
            while (elapsed < duration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.7f);
                float eased = EaseInQuad(t);
                scoreTransform.localScale = Vector3.Lerp(new Vector3(1.15f, 1.15f, 1f), originalScale, eased);
                yield return null;
            }

            scoreTransform.localScale = originalScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Combo badge animasyonu - shake + glow
        /// </summary>
        public void PlayComboBadgeAnim(GameObject comboBadge, int comboValue, Action onComplete = null)
        {
            if (comboBadge == null) return;

            string animId = $"combo_badge_{comboBadge.GetInstanceID()}";
            CancelAnimation(animId);

            StartCoroutine(ComboBadgeAnimRoutine(comboBadge, 0.5f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = comboBadge };
        }

        private IEnumerator ComboBadgeAnimRoutine(GameObject comboBadge, float duration, Action onComplete)
        {
            RectTransform rect = comboBadge.GetComponent<RectTransform>();
            Vector3 originalScale = comboBadge.transform.localScale;
            Vector2 originalPos = rect != null ? rect.anchoredPosition : Vector2.zero;

            // Shake effect
            int shakeCount = 6;
            float shakeDuration = duration / shakeCount;
            
            for (int i = 0; i < shakeCount; i++)
            {
                float offset = (i % 2 == 0 ? 1 : -1) * 2f;
                float shakeElapsed = 0f;
                
                while (shakeElapsed < shakeDuration)
                {
                    shakeElapsed += Time.deltaTime;
                    float t = shakeElapsed / shakeDuration;
                    if (rect != null)
                    {
                        rect.anchoredPosition = Vector2.Lerp(
                            originalPos + Vector2.right * ((i % 2 == 0 ? 1 : -1) * 2f),
                            originalPos + Vector2.right * (((i + 1) % 2 == 0 ? 1 : -1) * 2f),
                            t
                        );
                    }
                    yield return null;
                }
            }

            if (rect != null)
                rect.anchoredPosition = originalPos;

            // Scale pop at end
            float scaleElapsed = 0f;
            while (scaleElapsed < duration * 0.5f)
            {
                scaleElapsed += Time.deltaTime;
                float t = scaleElapsed / (duration * 0.5f);
                float eased = EaseOutElastic(t);
                comboBadge.transform.localScale = Vector3.Lerp(originalScale, new Vector3(1.2f, 1.2f, 1f), eased);
                yield return null;
            }

            // Retract
            scaleElapsed = 0f;
            while (scaleElapsed < duration * 0.5f)
            {
                scaleElapsed += Time.deltaTime;
                float t = scaleElapsed / (duration * 0.5f);
                float eased = EaseInQuad(t);
                comboBadge.transform.localScale = Vector3.Lerp(new Vector3(1.2f, 1.2f, 1f), originalScale, eased);
                yield return null;
            }

            comboBadge.transform.localScale = originalScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Panel slide-in animasyonu
        /// </summary>
        public void PlayPanelSlideInAnim(UnityEngine.CanvasGroup panel, Vector2 startPos, Vector2 endPos, Action onComplete = null)
        {
            if (panel == null) return;

            string animId = $"panel_slide_{panel.GetInstanceID()}";
            CancelAnimation(animId);

            StartCoroutine(PanelSlideInRoutine(panel, startPos, endPos, 0.3f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = panel.gameObject };
        }

        private IEnumerator PanelSlideInRoutine(UnityEngine.CanvasGroup panel, Vector2 startPos, Vector2 endPos, float duration, Action onComplete)
        {
            panel.alpha = 0;
            RectTransform rect = panel.GetComponent<RectTransform>();
            
            if (rect != null)
                rect.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseOutQuad(t);

                panel.alpha = eased;
                if (rect != null)
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

                yield return null;
            }

            panel.alpha = 1f;
            if (rect != null)
                rect.anchoredPosition = endPos;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Button hover animasyonu
        /// </summary>
        public void PlayButtonHoverAnim(GameObject button, bool isHovering, Action onComplete = null)
        {
            if (button == null) return;

            string animId = $"button_hover_{button.GetInstanceID()}";
            CancelAnimation(animId);

            float targetScale = isHovering ? 1.1f : 1f;
            StartCoroutine(ButtonHoverAnimRoutine(button.transform, targetScale, 0.15f, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = button };
        }

        private IEnumerator ButtonHoverAnimRoutine(Transform buttonTransform, float targetScale, float duration, Action onComplete)
        {
            Vector3 originalScale = buttonTransform.localScale;
            Vector3 targetScaleVec = new Vector3(targetScale, targetScale, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseOutQuad(t);
                buttonTransform.localScale = Vector3.Lerp(originalScale, targetScaleVec, eased);
                yield return null;
            }

            buttonTransform.localScale = targetScaleVec;
            onComplete?.Invoke();
        }

        #endregion

        #region VFX Animations

        /// <summary>
        /// Kamera shake - Yerleştirme feedback için
        /// </summary>
        public void PlayCameraShake(Camera camera, float duration = 0.1f, float intensity = 1f, Action onComplete = null)
        {
            if (camera == null)
            {
                return;
            }

            if (duration <= 0f || intensity <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            int cameraId = camera.GetInstanceID();
            if (!_cameraShakeStates.TryGetValue(cameraId, out var shakeState))
            {
                shakeState = new CameraShakeState();
                _cameraShakeStates[cameraId] = shakeState;
            }

            shakeState.Camera = camera;
            if (shakeState.Routine == null)
            {
                shakeState.BaseLocalPosition = camera.transform.localPosition;
                shakeState.NoiseSeedX = UnityEngine.Random.Range(0f, 1000f);
                shakeState.NoiseSeedY = UnityEngine.Random.Range(1000f, 2000f);
            }

            shakeState.RemainingTime = Mathf.Max(shakeState.RemainingTime, duration);
            shakeState.TotalTime = Mathf.Max(shakeState.TotalTime, shakeState.RemainingTime);
            shakeState.CurrentIntensity = Mathf.Clamp(
                Mathf.Max(shakeState.CurrentIntensity, intensity),
                0f,
                maxShakeIntensity);

            if (onComplete != null)
            {
                shakeState.OnComplete += onComplete;
            }

            if (shakeState.Routine == null)
            {
                shakeState.Routine = StartCoroutine(CameraShakeRoutine(cameraId));
            }
        }

        private IEnumerator CameraShakeRoutine(int cameraId)
        {
            if (!_cameraShakeStates.TryGetValue(cameraId, out var shakeState))
            {
                yield break;
            }

            while (shakeState.Camera != null && shakeState.RemainingTime > 0f)
            {
                shakeState.RemainingTime -= Time.deltaTime;

                float envelope = Mathf.Clamp01(shakeState.RemainingTime / Mathf.Max(0.0001f, shakeState.TotalTime));
                shakeState.CurrentIntensity = Mathf.Lerp(
                    shakeState.CurrentIntensity,
                    0f,
                    Time.deltaTime * cameraShakeDamping);

                float noiseTime = Time.time * cameraShakeNoiseFrequency;
                float noiseX = (Mathf.PerlinNoise(shakeState.NoiseSeedX, noiseTime) * 2f) - 1f;
                float noiseY = (Mathf.PerlinNoise(shakeState.NoiseSeedY, noiseTime) * 2f) - 1f;

                float amplitude = shakeState.CurrentIntensity * envelope * cameraShakePositionMultiplier;
                Vector3 offset = new Vector3(noiseX * amplitude, noiseY * amplitude, 0f);
                shakeState.Camera.transform.localPosition = shakeState.BaseLocalPosition + offset;

                yield return null;
            }

            if (shakeState.Camera != null)
            {
                shakeState.Camera.transform.localPosition = shakeState.BaseLocalPosition;
            }

            shakeState.OnComplete?.Invoke();
            shakeState.OnComplete = null;
            shakeState.Routine = null;
            shakeState.RemainingTime = 0f;
            shakeState.TotalTime = 0f;
            shakeState.CurrentIntensity = 0f;
        }

        /// <summary>
        /// Enhanced camera shake with combo scaling
        /// </summary>
        public void PlayCameraShakeWithCombo(Camera camera, int comboCount, float baseDuration = 0.15f, float baseIntensity = 0.3f, Action onComplete = null)
        {
            if (camera == null) return;

            // Scale shake based on combo
            int clampedCombo = Mathf.Clamp(comboCount, 0, 10);
            float durationMultiplier = 1f + (clampedCombo * 0.12f);
            float intensityMultiplier = 1f + (clampedCombo * 0.18f);
            
            float finalDuration = baseDuration * durationMultiplier;
            float finalIntensity = baseIntensity * intensityMultiplier;

            PlayCameraShake(camera, finalDuration, finalIntensity, onComplete);
            
            Debug.Log($"[AnimationController] Combo Screen Shake: Combo={comboCount}, Duration={finalDuration:F2}s, Intensity={finalIntensity:F2}");
        }

        /// <summary>
        /// Chromatic aberration flash - RGB splitting effect
        /// </summary>
        public void PlayChromaticFlash(float duration = 0.08f, float intensity = 1f)
        {
            StartCoroutine(ChromaticFlashRoutine(duration, intensity));
        }

        private IEnumerator ChromaticFlashRoutine(float duration, float intensity)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                yield break;
            }

            GameObject root = new GameObject("ChromaticFlash");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            float redBaseAlpha = 0.14f * intensity;
            float greenBaseAlpha = 0.12f * intensity;
            float blueBaseAlpha = 0.16f * intensity;

            Image red = CreateChromaticLayer(root.transform, new Color(1f, 0.25f, 0.25f, redBaseAlpha));
            Image green = CreateChromaticLayer(root.transform, new Color(0.25f, 1f, 0.25f, greenBaseAlpha));
            Image blue = CreateChromaticLayer(root.transform, new Color(0.25f, 0.35f, 1f, blueBaseAlpha));

            float elapsed = 0f;
            while (elapsed < duration && root != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fade = 1f - t;
                float offset = Mathf.Lerp(18f * intensity, 0f, t);

                SetChromaticLayer(red, new Vector2(-offset, 0f), fade, redBaseAlpha);
                SetChromaticLayer(green, new Vector2(0f, offset * 0.5f), fade, greenBaseAlpha);
                SetChromaticLayer(blue, new Vector2(offset, -offset * 0.35f), fade, blueBaseAlpha);

                yield return null;
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private Image CreateChromaticLayer(Transform parent, Color color)
        {
            GameObject layerObj = new GameObject("ChromaticLayer");
            layerObj.transform.SetParent(parent, false);

            RectTransform rect = layerObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = layerObj.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        private void SetChromaticLayer(Image image, Vector2 anchoredOffset, float alphaFactor, float baseAlpha)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = anchoredOffset;

            Color c = image.color;
            c.a = Mathf.Clamp01(baseAlpha * alphaFactor);
            image.color = c;
        }

        /// <summary>
        /// Floating text animasyonu (puan göstergesi)
        /// </summary>
        public void PlayFloatingTextAnim(GameObject floatingText, Vector3 startPos, Vector3 endPos, float duration = 1f, Action onComplete = null)
        {
            if (floatingText == null) return;

            if (HasReachedAnimationCapacity())
            {
                onComplete?.Invoke();
                return;
            }

            string animId = $"float_text_{floatingText.GetInstanceID()}";
            CancelAnimation(animId);

            StartCoroutine(FloatingTextAnimRoutine(floatingText, startPos, endPos, duration, () =>
            {
                _activeAnimations.Remove(animId);
                onComplete?.Invoke();
            }));

            _activeAnimations[animId] = new ActiveAnimation { Id = animId, TargetObject = floatingText, Duration = duration };
        }

        private IEnumerator FloatingTextAnimRoutine(GameObject floatingText, Vector3 startPos, Vector3 endPos, float duration, Action onComplete)
        {
            floatingText.transform.position = startPos;
            floatingText.transform.localScale = Vector3.zero;

            // Scale pop at start
            float scalePopDuration = duration * 0.2f;
            float elapsed = 0f;
            while (elapsed < scalePopDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scalePopDuration;
                float eased = EaseOutBack(t);
                floatingText.transform.localScale = Vector3.one * eased;
                yield return null;
            }

            floatingText.transform.localScale = Vector3.one;

            // Movement + fade
            float moveDuration = duration * 0.8f;
            elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                float eased = EaseOutQuad(t);
                floatingText.transform.position = Vector3.Lerp(startPos, endPos, eased);

                var cg = floatingText.GetComponent<UnityEngine.CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, t);
                }

                yield return null;
            }

            VFXEmitter.Instance.ReturnFloatingTextToPool(floatingText);
            onComplete?.Invoke();
        }

        #endregion

        #region Easing Functions

        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private float EaseInQuad(float t) => t * t;
        private float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        private float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        private float EaseInQuart(float t) => t * t * t * t;
        private bool HasReachedAnimationCapacity() => _activeAnimations.Count >= maxConcurrentAnimations;

        #endregion

        #region Utility Methods

        public void CancelAnimation(string animId)
        {
            if (_activeAnimations.TryGetValue(animId, out var anim))
            {
                if (anim.Routine != null)
                    StopCoroutine(anim.Routine);
                _activeAnimations.Remove(animId);
            }
        }

        public bool IsAnimationActive(string animId)
        {
            return _activeAnimations.ContainsKey(animId);
        }

        public void CancelAllAnimations()
        {
            foreach (var anim in _activeAnimations.Values)
            {
                if (anim.TargetObject != null)
                {
                    StopAllCoroutines();
                }
            }
            _activeAnimations.Clear();
        }

        #endregion

        private class ActiveAnimation
        {
            public string Id { get; set; }
            public GameObject TargetObject { get; set; }
            public float Duration { get; set; }
            public Coroutine Routine { get; set; }
        }

        private class CameraShakeState
        {
            public Camera Camera;
            public Vector3 BaseLocalPosition;
            public float RemainingTime;
            public float TotalTime;
            public float CurrentIntensity;
            public float NoiseSeedX;
            public float NoiseSeedY;
            public Coroutine Routine;
            public Action OnComplete;
        }
    }
}
