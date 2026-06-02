using UnityEngine;
using System.Collections.Generic;
using TMPro;
using BlockPuzzle.UnityAdapter.Configuration;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Animation
{
    #region Enums

    public enum ColorPresetType
    {
        PastelGlow,      // Current: 30% white blend
        ElectricBlue,    // Saturated blue + glow
        FireOrange,      // Warm orange, upward bias
        IceCrystal,      // Light blue, downward bias
        RainbowRandom    // Each particle random color
    }

    public enum SpeedRampType
    {
        Linear,          // Current: constant speed
        EaseOut,         // Fast start, slow end
        EaseIn,          // Slow start, fast end
        SpringBounce     // Physics bounce-back
    }

    public enum ParticleShapeType
    {
        Circle,
        Square,
        Diamond
    }

    #endregion

    /// <summary>
    /// VFX sistemi - Parçacık, glow, ışık efektleri
    /// Now: 20 features for 10/10 professional polish
    /// </summary>
    public class VFXEmitter : MonoBehaviour
    {
        [Header("=== PREFAB REFERENCES ===")]
        [SerializeField] private GameObject lineClearParticlePrefab;
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private GameObject dustParticlePrefab;
        [SerializeField] private GameObject shockwaveRingPrefab;

        [Header("=== POOL SETTINGS ===")]
        [SerializeField] private int maxParticlePoolSize = 220;
        [SerializeField] private int maxFloatingTextPoolSize = 20;
        [SerializeField] private bool persistAcrossScenes = false;

        [Header("=== PRESET ===")]
        [SerializeField] private VFXAnimationPreset vfxPreset = new VFXAnimationPreset();

        #region ===== TIER 1: CORE BLOCK BREAK SETTINGS =====

        [Header("=== [TIER 1] CORE PARTICLE SETTINGS ===")]
        [SerializeField] [Range(8, 32)] private int blockBreakParticleCount = 18;
        [SerializeField] [Range(0.08f, 0.3f)] private float blockBreakParticleSize = 0.12f;
        [SerializeField] [Range(2f, 5f)] private float blockBreakParticleSpeed = 3.5f;
        [SerializeField] [Range(0.2f, 0.6f)] private float blockBreakParticleLifetime = 0.35f;
        [SerializeField] [Range(0.1f, 0.5f)] private float blockBreakSizeVariation = 0.3f;
        [SerializeField] [Range(0, 200)] private int vfxSortingOrderBase = 24;

        #endregion

        #region ===== TIER 2: DUAL-LAYER PARTICLES =====

        [Header("=== [TIER 2] DUST LAYER (Slow, Large) ===")]
        [SerializeField] private bool enableDustLayer = true;
        [SerializeField] [Range(5, 20)] private int dustLayerCount = 12;
        [SerializeField] [Range(0.15f, 0.4f)] private float dustLayerSize = 0.2f;
        [SerializeField] [Range(0.5f, 2f)] private float dustLayerSpeed = 1.2f;
        [SerializeField] [Range(0.3f, 0.8f)] private float dustLayerLifetime = 0.5f;
        [SerializeField] [Range(0.1f, 0.3f)] private float dustLayerAlpha = 0.4f;

        #endregion

        #region ===== TIER 3: PARTICLE PHYSICS & MOTION =====

        [Header("=== [TIER 3] PARTICLE MOTION PHYSICS ===")]
        [SerializeField] private SpeedRampType speedRampType = SpeedRampType.Linear;
        [SerializeField] private bool enableGravityCurve = true;
        [SerializeField] [Range(0f, 5f)] private float gravityStrength = 1.5f;
        [SerializeField] private bool enableParticleRotation = true;
        [SerializeField] [Range(0f, 1080f)] private float rotationSpeed = 540f; // degrees/sec
        [SerializeField] private ParticleShapeType particleShape = ParticleShapeType.Circle;

        #endregion

        #region ===== TIER 4: VISUAL EFFECTS =====

        [Header("=== [TIER 4] SHOCKWAVE RING ===")]
        [SerializeField] private bool enableShockwaveRing = true;
        [SerializeField] [Range(0.05f, 0.5f)] private float shockwaveRingDuration = 0.2f;
        [SerializeField] [Range(0.5f, 3f)] private float shockwaveRingMaxScale = 2f;
        [SerializeField] [Range(0.1f, 0.5f)] private float shockwaveRingOutlineWidth = 0.2f;

        [Header("=== [TIER 4] CHROMATIC ABERRATION ===")]
        [SerializeField] private bool enableChromaticAberration = false;
        [SerializeField] [Range(0f, 2f)] private float chromaticIntensity = 1f;
        [SerializeField] [Range(0.02f, 0.15f)] private float chromaticDuration = 0.08f;

        [Header("=== [TIER 4] COLOR PRESETS ===")]
        [SerializeField] private ColorPresetType colorPreset = ColorPresetType.PastelGlow;
        [SerializeField] [Range(0f, 1f)] private float colorBlendWhite = 0.3f;

        #endregion

        #region ===== TIER 5: COMBO SYSTEMS =====

        [Header("=== [TIER 5] COMBO MULTIPLIER EFFECTS ===")]
        [SerializeField] private bool enableComboScaling = true;
        [SerializeField] private int comboThreshold1 = 3;  // Count threshold
        [SerializeField] private int comboThreshold2 = 6;
        [SerializeField] private float comboParticleMultiplier1 = 1.3f; // 30% more particles
        [SerializeField] private float comboParticleMultiplier2 = 1.6f; // 60% more particles
        [SerializeField] private float comboSpeedMultiplier = 1.2f;    // 20% faster
        [SerializeField] private float comboBrightnessBoost = 1.3f;    // 30% brighter
        [SerializeField] private bool enableCascadingEffects = true;   // Time-staggered multi-clears

        #endregion

        #region ===== TIER 6: ADVANCED FEATURES =====

        [Header("=== [TIER 6] REFERENCES & SCREEN SHAKE ===")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool enableScreenShakeSync = true;
        [SerializeField] [Range(0f, 1f)] private float shakeIntensityBase = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float shakeIntensityCombo = 0.8f;

        [Header("=== [TIER 6] SLOW-MOTION LINE CLEAR ===")]
        [SerializeField] private bool enableSlowMoLineClear = true;
        [SerializeField] [Range(0.2f, 0.8f)] private float slowMoTimeScale = 0.5f;
        [SerializeField] [Range(0.1f, 0.5f)] private float slowMoDuration = 0.3f;

        [Header("=== [TIER 6] MAGNETIC CORE EFFECT ===")]
        [SerializeField] private bool enableMagneticCore = true;
        [SerializeField] [Range(0f, 1f)] private float magneticCoreStartTime = 0.7f; // 70% through lifetime
        [SerializeField] [Range(0f, 10f)] private float magneticCorePullStrength = 3f;

        #endregion

        private static VFXEmitter _instance;
        public static VFXEmitter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VFXEmitter>();
                    if (_instance == null)
                    {
                        var go = new GameObject("VFXEmitter");
                        _instance = go.AddComponent<VFXEmitter>();
                    }
                }
                return _instance;
            }
        }

        // Obje pooling
        private Queue<GameObject> _particlePool = new Queue<GameObject>();
        private Queue<GameObject> _floatingTextPool = new Queue<GameObject>();
        private HashSet<GameObject> _activeParticles = new HashSet<GameObject>();
        private HashSet<GameObject> _activeFloatingTexts = new HashSet<GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            // Auto-find main camera if not assigned
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Pool oluştur
            InitPools();
        }

        private void OnDestroy()
        {
            // Pool'u temizle
            foreach (var particle in _particlePool)
            {
                if (particle != null)
                    Destroy(particle);
            }
            foreach (var floatText in _floatingTextPool)
            {
                if (floatText != null)
                    Destroy(floatText);
            }
            _particlePool.Clear();
            _floatingTextPool.Clear();
            _activeParticles.Clear();
            _activeFloatingTexts.Clear();
        }

        private void InitPools()
        {
            // Başlangıç particle pool'u
            for (int i = 0; i < maxParticlePoolSize; i++)
            {
                if (lineClearParticlePrefab == null) break;
                var particle = Instantiate(lineClearParticlePrefab);
                particle.SetActive(false);
                particle.transform.SetParent(transform);
                _particlePool.Enqueue(particle);
            }

            // Başlangıç floating text pool'u
            for (int i = 0; i < maxFloatingTextPoolSize; i++)
            {
                if (floatingTextPrefab == null) break;
                var floatText = Instantiate(floatingTextPrefab);
                floatText.SetActive(false);
                floatText.transform.SetParent(transform);
                _floatingTextPool.Enqueue(floatText);
            }

            Debug.Log($"[VFXEmitter] Pool initialized: {_particlePool.Count} particles, {_floatingTextPool.Count} texts");
        }

        #region Line Clear Effects

        /// <summary>
        /// Satır silme parçacık efekti
        /// </summary>
        public void EmitLineClearEffect(Vector3 cellPosition, Color cellColor, float gridCellSize)
        {
            int particleCount = Mathf.Max(vfxPreset.lineClearParticleCount, Mathf.RoundToInt(vfxPreset.lineClearParticleCount * 1.35f));
            float spawnRadius = Mathf.Max(0.03f, gridCellSize * 0.14f);
            float burstDistance = Mathf.Max(gridCellSize * 2.8f, 1.25f);
            float baseScale = Mathf.Max(0.06f, vfxPreset.lineClearParticleSize * 1.15f);

            for (int i = 0; i < particleCount; i++)
            {
                var particle = GetPooledParticle(lineClearParticlePrefab);
                if (particle == null) continue;

                Vector2 spawnOffset = Random.insideUnitCircle * spawnRadius;
                particle.transform.position = cellPosition + new Vector3(spawnOffset.x, spawnOffset.y, 0f);
                particle.SetActive(true);
                ApplyParticleShape(particle.transform);
                particle.transform.localScale = Vector3.one * (baseScale * Random.Range(0.85f, 1.45f));

                // Rastgele yön
                float angle = Random.Range(0f, 360f);
                float speed = Random.Range(vfxPreset.lineClearParticleSpeedMin, vfxPreset.lineClearParticleSpeedMax) * 1.1f;
                Vector3 direction = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0
                );

                var rb = particle.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * speed;
                }

                var sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = cellColor;
                    ApplyParticleSortingOrder(sr, 0, 2);
                }

                var cg = particle.GetComponent<CanvasGroup>();
                if (cg == null) cg = particle.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                _activeParticles.Add(particle);
                StartCoroutine(LineClearParticleRoutine(
                    particle,
                    cg,
                    particle.transform.position,
                    particle.transform.position + direction * burstDistance,
                    vfxPreset.lineClearParticleLifetime * Random.Range(0.92f, 1.12f)));
            }
        }

        /// <summary>
        /// Satırların highlight glow efekti
        /// </summary>
        public void AnimateLineHighlight(List<int> clearedRowIndices, Material highlightMaterial, float duration)
        {
            if (highlightMaterial == null) return;

            // TODO: Row'ları vurgulayan shader animasyonu
            // Şu an placeholder - GridView ile koordine etmeli
            Debug.Log($"[VFXEmitter] Highlighting {clearedRowIndices.Count} rows for {duration}s");
        }

        #endregion

        #region Placement Effects

        /// <summary>
        /// Yerleştirme sırasında dust efekti
        /// </summary>
        public void EmitPlacementDust(Vector3 blockPosition, float cellSize, Color blockColor)
        {
            int dustCount = vfxPreset.placementDustCount;

            for (int i = 0; i < dustCount; i++)
            {
                var dust = GetPooledParticle(dustParticlePrefab != null ? dustParticlePrefab : lineClearParticlePrefab);
                if (dust == null) continue;

                // Block sınırında rastgele nokta
                float randomX = Random.Range(-cellSize / 2, cellSize / 2);
                float randomY = Random.Range(-cellSize / 2, cellSize / 2);
                Vector3 spawnPos = blockPosition + new Vector3(randomX, randomY, 0);

                dust.transform.position = spawnPos;
                dust.SetActive(true);

                var sr = dust.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = blockColor;
                    ApplyParticleSortingOrder(sr, -2, 0);
                }

                // Yavaşça fade out
                var cg = dust.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                }

                // Üst tarafa doğru yavaşça çık
                Vector3 targetPos = spawnPos + Vector3.up * (cellSize * 3);
                AnimationController.Instance.PlayFloatingTextAnim(
                    dust,
                    spawnPos,
                    targetPos,
                    vfxPreset.placementDustLifetime,
                    () => ReturnPooledParticle(dust)
                );
            }
        }

        #endregion

        #region Floating Text

        /// <summary>
        /// Floating text oluştur (+50 gibi puan göstergesi)
        /// </summary>
        public GameObject SpawnFloatingText(string text, Vector3 worldPos, Color color, float duration = 1f)
        {
            var floatText = GetPooledFloatingText();
            if (floatText == null)
            {
                floatText = Instantiate(floatingTextPrefab);
            }

            floatText.SetActive(true);

            var textMesh = floatText.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            var tmpText = floatText.GetComponent<TextMeshPro>();
            if (tmpText != null)
            {
                tmpText.text = text;
                tmpText.color = color;
            }

            var tmpUiText = floatText.GetComponent<TextMeshProUGUI>();
            if (tmpUiText != null)
            {
                tmpUiText.text = text;
                tmpUiText.color = color;
            }

            var cg = floatText.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
            }

            floatText.transform.position = worldPos;

            return floatText;
        }

        #endregion

        #region ===== HELPER METHODS =====

        /// <summary>
        /// Get color preset for current particle
        /// </summary>
        private Color GetPresetColor(Color baseBlockColor)
        {
            Color result;
            switch (colorPreset)
            {
                case ColorPresetType.PastelGlow:
                    result = Color.Lerp(baseBlockColor, Color.white, colorBlendWhite);
                    break;

                case ColorPresetType.ElectricBlue:
                {
                    Color electric = new Color(0.2f, 0.6f, 1f);
                    result = Color.Lerp(baseBlockColor, electric, 0.7f) * 1.3f;
                    break;
                }

                case ColorPresetType.FireOrange:
                {
                    Color fire = new Color(1f, 0.6f, 0.2f);
                    result = Color.Lerp(baseBlockColor, fire, 0.6f) * 1.2f;
                    break;
                }

                case ColorPresetType.IceCrystal:
                {
                    Color ice = new Color(0.7f, 0.95f, 1f);
                    result = Color.Lerp(baseBlockColor, ice, 0.8f);
                    break;
                }

                case ColorPresetType.RainbowRandom:
                    result = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
                    break;

                default:
                    result = baseBlockColor;
                    break;
            }

            return ProjectColorGrading.Apply(result);
        }

        private void ApplyParticleShape(Transform particleTransform)
        {
            if (particleTransform == null)
                return;

            switch (particleShape)
            {
                case ParticleShapeType.Circle:
                    particleTransform.rotation = Quaternion.identity;
                    break;
                case ParticleShapeType.Square:
                    particleTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case ParticleShapeType.Diamond:
                    particleTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
            }
        }

        /// <summary>
        /// Calculate speed with ramp type
        /// </summary>
        private float ApplySpeedRamp(float baseSpeed, float progress, SpeedRampType rampType)
        {
            // progress: 0 to 1 (0 = start, 1 = end)
            switch (rampType)
            {
                case SpeedRampType.Linear:
                    return baseSpeed;

                case SpeedRampType.EaseOut:
                    // Fast start, slow end
                    return baseSpeed * (1f - (progress * progress));

                case SpeedRampType.EaseIn:
                    // Slow start, fast end
                    return baseSpeed * (progress * progress);

                case SpeedRampType.SpringBounce:
                    // Bounce effect: sin wave
                    return baseSpeed * (1f + Mathf.Sin(progress * Mathf.PI * 2f) * 0.3f);

                default:
                    return baseSpeed;
            }
        }

        private void ApplyParticleSortingOrder(SpriteRenderer renderer, int minOffset, int maxOffset)
        {
            if (renderer == null)
                return;

            int clampedMin = Mathf.Min(minOffset, maxOffset);
            int clampedMax = Mathf.Max(minOffset, maxOffset);
            renderer.sortingOrder = Random.Range(vfxSortingOrderBase + clampedMin, vfxSortingOrderBase + clampedMax + 1);
        }

        /// <summary>
        /// Calculate gravity offset for parabolic trajectory
        /// </summary>
        private Vector3 CalculateGravityOffset(Vector2 initialVelocity, float elapsed, float lifetime)
        {
            if (!enableGravityCurve) return Vector3.zero;

            // Gravity starts halfway through lifetime
            float gravityStartTime = lifetime * 0.5f;
            if (elapsed < gravityStartTime) return Vector3.zero;

            float timeSinceGravity = elapsed - gravityStartTime;
            float gravityOffset = -0.5f * gravityStrength * timeSinceGravity * timeSinceGravity;

            return new Vector3(0, gravityOffset, 0);
        }

        /// <summary>
        /// Apply combo scaling to particle parameters
        /// </summary>
        private void ApplyComboScaling(int comboLineCount, ref int particleCount, ref float speed, ref float brightness)
        {
            if (!enableComboScaling) return;

            if (comboLineCount >= comboThreshold2)
            {
                particleCount = Mathf.RoundToInt(particleCount * comboParticleMultiplier2);
                speed *= comboSpeedMultiplier;
                brightness *= comboBrightnessBoost;
            }
            else if (comboLineCount >= comboThreshold1)
            {
                particleCount = Mathf.RoundToInt(particleCount * comboParticleMultiplier1);
                speed *= comboSpeedMultiplier * 0.7f; // Less boost for lower combo
                brightness *= comboBrightnessBoost * 0.8f;
            }
        }

        /// <summary>
        /// Create shockwave ring effect (auto-creates if prefab not assigned)
        /// </summary>
        private void EmitBlockBreakShockwave(Vector3 position, Color ringColor)
        {
            if (!enableShockwaveRing) return;

            GameObject shockwave;
            if (shockwaveRingPrefab != null)
            {
                shockwave = Instantiate(shockwaveRingPrefab, position, Quaternion.identity);
            }
            else
            {
                // Auto-create shockwave if prefab not assigned
                shockwave = new GameObject("ShockwaveRing");
                shockwave.transform.position = position;
                var lineRend = shockwave.AddComponent<LineRenderer>();
                lineRend.material = new Material(Shader.Find("Sprites/Default"));
                lineRend.startWidth = shockwaveRingOutlineWidth;
                lineRend.endWidth = shockwaveRingOutlineWidth;
                lineRend.useWorldSpace = false;
            }

            shockwave.transform.SetParent(transform);

            LineRenderer lr = shockwave.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.material.color = ringColor;
                if (lr.positionCount < 8)
                {
                    ConfigureShockwaveGeometry(lr, 32);
                }
            }

            StartCoroutine(ShockwaveRingRoutine(shockwave, position, ringColor, shockwaveRingDuration));
        }

        /// <summary>
        /// Chromatic aberration flash effect
        /// </summary>
        private void TriggerChromaticFlash()
        {
            if (!enableChromaticAberration)
                return;

            if (AnimationController.Instance != null)
                AnimationController.Instance.PlayChromaticFlash(chromaticDuration, chromaticIntensity);
        }

        /// <summary>
        /// Screen shake sync with particle burst
        /// </summary>
        private void TriggerScreenShakeSync(int comboCount = 0)
        {
            if (!enableScreenShakeSync || mainCamera == null) return;

            float intensity = shakeIntensityBase;
            if (comboCount > 0)
            {
                intensity = Mathf.Lerp(shakeIntensityBase, shakeIntensityCombo, Mathf.Min(comboCount / 10f, 1f));
            }

            // Trigger camera shake via AnimationController
            if (AnimationController.Instance != null)
            {
                AnimationController.Instance.PlayCameraShakeWithCombo(
                    mainCamera,
                    comboCount,
                    vfxPreset.cameraShakeDuration,
                    intensity
                );
            }
        }

        /// <summary>
        /// Slow-mo line clear effect with automatic restore
        /// </summary>
        public void TriggerSlowMoLineClear()
        {
            if (!enableSlowMoLineClear) return;

            Time.timeScale = slowMoTimeScale;
            Debug.Log($"[VFXEmitter] SlowMo: {slowMoTimeScale}x for {slowMoDuration}s");
            
            StartCoroutine(SlowMoRestoreRoutine());
        }

        /// <summary>
        /// Restore time scale after slow-mo duration
        /// </summary>
        private System.Collections.IEnumerator SlowMoRestoreRoutine()
        {
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = 1f;
            Debug.Log("[VFXEmitter] SlowMo: Time restored to normal");
        }

        #endregion

        #region ===== COROUTINES =====

        /// <summary>
        /// Shockwave ring animation
        /// </summary>
        private System.Collections.IEnumerator ShockwaveRingRoutine(GameObject ring, Vector3 center, Color color, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.1f;
            Vector3 endScale = Vector3.one * shockwaveRingMaxScale;

            while (elapsed < duration && ring != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale up
                ring.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                // Fade alpha
                LineRenderer lr = ring.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    Color fadeColor = color;
                    fadeColor.a = Mathf.Lerp(1f, 0f, t);
                    lr.material.color = fadeColor;
                }

                yield return null;
            }

            if (ring != null)
                Destroy(ring);
        }

        private void ConfigureShockwaveGeometry(LineRenderer lineRenderer, int segments)
        {
            if (lineRenderer == null)
                return;

            int pointCount = Mathf.Max(8, segments);
            lineRenderer.loop = true;
            lineRenderer.positionCount = pointCount;

            float step = (Mathf.PI * 2f) / pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * step;
                lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
        }

        #endregion

        #region Grid Cell Glow

        /// <summary>
        /// Grid hücresi glow animasyonu (yerleştirme feedback)
        /// </summary>
        public void EmitGridCellGlow(Vector3 cellWorldPos, Color glowColor, float duration)
        {
            // TODO: Cell'e temporary glow renderer ekle ve animate et
            Debug.Log($"[VFXEmitter] Cell glow at {cellWorldPos} for {duration}s");
        }

        #endregion

        #region Object Pool

        private GameObject GetPooledParticle(GameObject preferredPrefab = null)
        {
            if (_particlePool.Count > 0)
            {
                return _particlePool.Dequeue();
            }

            // Pool boş ise yeni oluştur
            var sourcePrefab = preferredPrefab != null ? preferredPrefab : lineClearParticlePrefab;
            if (sourcePrefab != null)
            {
                var newParticle = Instantiate(sourcePrefab);
                newParticle.transform.SetParent(transform);
                return newParticle;
            }

            return null;
        }

        private void ReturnPooledParticle(GameObject particle)
        {
            if (particle != null)
            {
                particle.SetActive(false);
                _particlePool.Enqueue(particle);
            }
        }

        private GameObject GetPooledFloatingText()
        {
            if (_floatingTextPool.Count > 0)
            {
                return _floatingTextPool.Dequeue();
            }

            if (floatingTextPrefab != null)
            {
                var newText = Instantiate(floatingTextPrefab);
                newText.transform.SetParent(transform);
                return newText;
            }

            return null;
        }

        private void ReturnPooledFloatingText(GameObject floatText)
        {
            if (floatText != null)
            {
                floatText.SetActive(false);
                _floatingTextPool.Enqueue(floatText);
            }
        }

        /// <summary>
        /// Public: Floating text'i pool'a geri koy
        /// </summary>
        public void ReturnFloatingTextToPool(GameObject floatText)
        {
            Debug.Log($"[VFXEmitter] Returning float text to pool: {floatText.name}");
            ReturnPooledFloatingText(floatText);
        }

        /// <summary>
        /// Public: Particle'ı pool'a geri koy
        /// </summary>
        public void ReturnParticleToPool(GameObject particle)
        {
            Debug.Log($"[VFXEmitter] Returning particle to pool: {particle.name}");
            ReturnPooledParticle(particle);
        }

        /// <summary>
        /// Main emit public API - supports combo count
        /// </summary>
        public void EmitBlockBreakParticles(Vector3 position, Color blockColor, int comboLineCount = 0, bool emitSecondaryEffects = true)
        {
            EmitBlockBreakParticlesInternal(position, blockColor, comboLineCount, emitSecondaryEffects);
        }

        /// <summary>
        /// PROFESSIONAL BLOCK BREAK FX (10/10)
        /// - Dual-layer particles (core sparkles + dust cloud)
        /// - Color presets (pastel, electric, fire, ice, rainbow)
        /// - Physics: gravity curve + parabolic trajectory
        /// - Rotation: tumble + spin effects
        /// - Combo scaling: more particles for multi-line clears
        /// - Shockwave ring + chromatic aberration
        /// </summary>
        private void EmitBlockBreakParticlesInternal(Vector3 position, Color blockColor, int comboLineCount = 0, bool emitSecondaryEffects = true)
        {
            if (lineClearParticlePrefab == null && dustParticlePrefab == null) 
            {
                Debug.LogWarning("[VFXEmitter] No particle prefab assigned (lineClearParticlePrefab/dustParticlePrefab). Returning.");
                return;
            }

            Debug.Log($"[VFXEmitter] 🔥 EMIT START - Combo: {comboLineCount}, Dust Enabled: {enableDustLayer}, Rotation: {enableParticleRotation}");

            // === TIER 1: CORE PARTICLES ===
            int coreParticleCount = Mathf.Clamp(blockBreakParticleCount, 8, 32);
            float coreSpeed = blockBreakParticleSpeed;
            float coreBrightness = 1f;

            int countBefore = coreParticleCount;

            // Apply combo scaling
            ApplyComboScaling(comboLineCount, ref coreParticleCount, ref coreSpeed, ref coreBrightness);

            Debug.Log($"[VFXEmitter] Combo Scaling: {countBefore} → {coreParticleCount} particles, Speed: {coreSpeed}x");

            // Pool check
            int totalNeeded = coreParticleCount;
            if (enableDustLayer) totalNeeded += dustLayerCount;
            if (_activeParticles.Count + totalNeeded > maxParticlePoolSize)
            {
                coreParticleCount = Mathf.Max(8, maxParticlePoolSize - _activeParticles.Count - dustLayerCount);
                Debug.Log($"[VFXEmitter] Pool Warning: Reduced to {coreParticleCount} (active: {_activeParticles.Count}/{maxParticlePoolSize})");
            }

            // === SPAWN CORE PARTICLES ===
            for (int i = 0; i < coreParticleCount; i++)
            {
                GameObject particle = GetPooledParticle(lineClearParticlePrefab != null ? lineClearParticlePrefab : dustParticlePrefab);
                if (particle == null) break;

                particle.SetActive(true);
                particle.transform.position = position;
                particle.transform.rotation = Quaternion.identity;
                ApplyParticleShape(particle.transform);

                // Size with variation
                float minSizeMultiplier = Mathf.Clamp01(1f - blockBreakSizeVariation);
                float sizeVar = Random.Range(minSizeMultiplier, 1f);
                float finalSize = blockBreakParticleSize * sizeVar;
                particle.transform.localScale = Vector3.one * finalSize;

                // Color: apply preset + brightness
                Color finalColor = GetPresetColor(blockColor) * coreBrightness;
                finalColor *= Random.Range(0.9f, 1.1f);

                SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = finalColor;
                    ApplyParticleSortingOrder(sr, 0, 2);
                }

                // Physics: radial burst with speed variation
                Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                    float angle = (360f / coreParticleCount) * i + Random.Range(-20f, 20f);
                    float radians = angle * Mathf.Deg2Rad;
                    float speedVar = coreSpeed + Random.Range(-0.3f, 0.3f);
                    Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                    rb.linearVelocity = direction * speedVar;
                    rb.angularVelocity = 0f;
                }

                CanvasGroup cg = particle.GetComponent<CanvasGroup>();
                if (cg == null) cg = particle.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                // Track particle data for advanced effects
                ParticleData data = particle.GetComponent<ParticleData>();
                if (data == null) data = particle.AddComponent<ParticleData>();
                data.Initialize(finalColor, blockBreakParticleLifetime, enableParticleRotation, rotationSpeed);

                _activeParticles.Add(particle);
                StartCoroutine(ParticleEnergyTrailAdvanced(particle, cg, data, blockBreakParticleLifetime));
            }

            // === TIER 2: DUST LAYER ===
            if (enableDustLayer)
            {
                for (int i = 0; i < dustLayerCount; i++)
                {
                    GameObject dust = GetPooledParticle(dustParticlePrefab != null ? dustParticlePrefab : lineClearParticlePrefab);
                    if (dust == null) break;

                    dust.SetActive(true);
                    dust.transform.position = position;
                    dust.transform.rotation = Quaternion.identity;
                    ApplyParticleShape(dust.transform);

                    // Larger, slower particles
                    float dustSizeVar = Random.Range(0.5f, 0.9f);
                    float dustSize = dustLayerSize * dustSizeVar;
                    dust.transform.localScale = Vector3.one * dustSize;

                    // Softer color, more transparent
                    Color dustColor = GetPresetColor(blockColor) * 0.7f;
                    dustColor.a = dustLayerAlpha;

                    SpriteRenderer sr = dust.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = dustColor;
                        ApplyParticleSortingOrder(sr, -3, 0);
                    }

                    // Slower, wider dispersal
                    Rigidbody2D dustRb = dust.GetComponent<Rigidbody2D>();
                    if (dustRb != null)
                    {
                        dustRb.gravityScale = 0f;
                        dustRb.constraints = RigidbodyConstraints2D.FreezeRotation;

                        float dustAngle = Random.Range(0f, 360f);
                        float dustRadians = dustAngle * Mathf.Deg2Rad;
                        float dustSpeed = dustLayerSpeed + Random.Range(-0.2f, 0.2f);
                        Vector2 dustDir = new Vector2(Mathf.Cos(dustRadians), Mathf.Sin(dustRadians));
                        dustRb.linearVelocity = dustDir * dustSpeed;
                        dustRb.angularVelocity = 0f;
                    }

                    CanvasGroup dustCg = dust.GetComponent<CanvasGroup>();
                    if (dustCg == null) dustCg = dust.AddComponent<CanvasGroup>();
                    dustCg.alpha = dustLayerAlpha;

                    ParticleData dustData = dust.GetComponent<ParticleData>();
                    if (dustData == null) dustData = dust.AddComponent<ParticleData>();
                    dustData.Initialize(dustColor, dustLayerLifetime, false, 0f);

                    _activeParticles.Add(dust);
                    StartCoroutine(ParticleEnergyTrailAdvanced(dust, dustCg, dustData, dustLayerLifetime));
                }
            }

            // === VISUAL EFFECTS ===
            if (emitSecondaryEffects)
            {
                EmitBlockBreakShockwave(position, GetPresetColor(blockColor));
                TriggerChromaticFlash();
                TriggerScreenShakeSync(comboLineCount);

                if (enableCascadingEffects && comboLineCount > 1)
                    StartCoroutine(CascadingShockwaveRoutine(position, blockColor, Mathf.Min(comboLineCount - 1, 3)));
            }

            int totalEmitted = coreParticleCount + (enableDustLayer ? dustLayerCount : 0);
            Debug.Log($"[VFXEmitter] 🔥 Block Break: {totalEmitted} particles (Core:{coreParticleCount} Dust:{dustLayerCount}) Combo:{comboLineCount}");
        }

        private System.Collections.IEnumerator CascadingShockwaveRoutine(Vector3 origin, Color blockColor, int extraBursts)
        {
            for (int i = 0; i < extraBursts; i++)
            {
                yield return new WaitForSeconds(0.045f * (i + 1));
                EmitBlockBreakShockwave(origin, GetPresetColor(blockColor));
            }
        }

        private System.Collections.IEnumerator LineClearParticleRoutine(
            GameObject particle,
            CanvasGroup canvasGroup,
            Vector3 startPos,
            Vector3 endPos,
            float lifetime)
        {
            if (particle == null)
                yield break;

            Vector3 originalScale = particle.transform.localScale;
            float elapsed = 0f;

            while (elapsed < lifetime && particle != null && particle.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                float eased = 1f - ((1f - t) * (1f - t));

                particle.transform.position = Vector3.Lerp(startPos, endPos, eased);
                particle.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, eased);

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);

                yield return null;
            }

            if (particle != null && _activeParticles.Contains(particle))
            {
                ReturnPooledParticle(particle);
                _activeParticles.Remove(particle);
            }
        }

        /// <summary>
        /// Advanced particle lifecycle - supports rotation, gravity, magnetic core
        /// </summary>
        private System.Collections.IEnumerator ParticleEnergyTrailAdvanced(GameObject particle, CanvasGroup canvasGroup, ParticleData data, float lifetime)
        {
            Vector3 originalScale = particle.transform.localScale;
            Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
            float elapsed = 0f;
            Vector2 initialVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

            while (elapsed < lifetime && particle != null && particle.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // === SPEED RAMP ===
                float speedMultiplier = ApplySpeedRamp(1f, t, speedRampType);
                if (rb != null)
                {
                    rb.linearVelocity = initialVelocity * speedMultiplier;
                }

                // === PARTICLE ROTATION ===
                if (data.EnableRotation && enableParticleRotation)
                {
                    float rotation = t * Mathf.PI * 2f; // Full rotation over lifetime
                    float spinAmount = rotationSpeed * t; // Degrees
                    particle.transform.Rotate(Vector3.forward, spinAmount * Time.deltaTime);
                }

                // === GRAVITY CURVE ===
                Vector3 gravityOffset = CalculateGravityOffset(initialVelocity, elapsed, lifetime);
                if (gravityOffset != Vector3.zero && rb != null)
                {
                    particle.transform.position += gravityOffset;
                }

                // === MAGNETIC CORE (Converge to center) ===
                if (enableMagneticCore)
                {
                    float magneticStartTime = lifetime * magneticCoreStartTime;
                    if (elapsed > magneticStartTime)
                    {
                        float magneticT = (elapsed - magneticStartTime) / (lifetime - magneticStartTime);
                        Vector3 toCenter = -particle.transform.position;
                        particle.transform.position += toCenter * magneticCorePullStrength * Time.deltaTime * magneticT;
                    }
                }

                // === COLOR FADE (Separate from alpha) ===
                float colorFadeT = Mathf.Clamp01(t * 1.2f); // Fade color slightly faster
                Color faded = Color.Lerp(data.OriginalColor, new Color(data.OriginalColor.r, data.OriginalColor.g, data.OriginalColor.b, 0), colorFadeT);

                SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = faded;
                }

                // === ALPHA FADE (Eased out) ===
                float eased = 1f - ((1f - t) * (1f - t)); // Ease out quad
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
                }

                // === SCALE DOWN ===
                particle.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, eased);

                yield return null;
            }

            // Cleanup
            if (particle != null && _activeParticles.Contains(particle))
            {
                ReturnPooledParticle(particle);
                _activeParticles.Remove(particle);
            }
        }

        /// <summary>
        /// Legacy: Keep old routine for compatibility
        /// </summary>
        private System.Collections.IEnumerator ParticleEnergyTrailRoutine(GameObject particle, CanvasGroup canvasGroup, float lifetime)
        {
            Vector3 originalScale = particle.transform.localScale;
            float elapsed = 0f;

            // Smooth fade + scale: 0.4s
            while (elapsed < lifetime && particle != null && particle.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Ease out for smooth deceleration feel (1 - (1-t)²)
                float eased = 1f - ((1f - t) * (1f - t));

                // Alpha: 1 → 0 (smooth fade)
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
                }

                // Scale: 0.3 → 0 (size down into nothing)
                particle.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, eased);

                yield return null;
            }

            // Cleanup
            if (particle != null && _activeParticles.Contains(particle))
            {
                ReturnPooledParticle(particle);
                _activeParticles.Remove(particle);
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper component for storing particle-specific data
    /// </summary>
    public class ParticleData : MonoBehaviour
    {
        public Color OriginalColor { get; private set; }
        public float Lifetime { get; private set; }
        public bool EnableRotation { get; private set; }
        public float RotationSpeed { get; private set; }

        public void Initialize(Color color, float lifetime, bool enableRotation, float rotationSpeed)
        {
            OriginalColor = color;
            Lifetime = lifetime;
            EnableRotation = enableRotation;
            RotationSpeed = rotationSpeed;
        }
    }}
