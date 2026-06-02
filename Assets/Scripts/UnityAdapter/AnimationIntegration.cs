using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Animation;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Configuration;

namespace BlockPuzzle.UnityAdapter
{
    /// <summary>
    /// Oyun eventlerini animasyon sistemine bağlar.
    /// GameBootstrap → AnimationController / VFXEmitter
    /// </summary>
    public class AnimationIntegration : MonoBehaviour
    {
        private const string AnimationsPreferenceKey = SettingsKeys.Animations;
        private const string VibrationPreferenceKey = SettingsKeys.Vibration;
        private const string ComboPopupFontResourcePath = "TMP/LuckiestGuy-Regular Combo SDF";

        [Header("=== ANIMATION SETTINGS ===")]
        [SerializeField] private BlockAnimationPreset blockAnimPreset = BlockAnimationPreset.GetDefault();
        [SerializeField] private UIAnimationPreset uiAnimPreset = UIAnimationPreset.GetDefault();
        [SerializeField] private VFXAnimationPreset vfxPreset = VFXAnimationPreset.GetDefault();

        [Header("=== COMBO TEXT APPEARANCE ===")]
        [SerializeField] private TMP_FontAsset comboPopupFont;
        [SerializeField] private int comboFontSize = 140;
        [SerializeField] private Color comboTextColor = new Color(0.4f, 0.85f, 1f, 1f); // Parlak cyan mavi
        [SerializeField] private float comboOutlineWidth = 0.25f;
        [SerializeField] private Color comboOutlineColor = new Color(0f, 0.3f, 0.8f, 1f); // Koyu mavi outline
        [SerializeField] private bool useComboValueInText = true; // Combo value'yu metne dahil et (x2, x3, etc.)
        [SerializeField] private string comboTextPrefix = "COMBO"; // "COMBO" veya başka şey
        [SerializeField] private bool forceReferenceComboLook = true;
        [SerializeField] private bool keepComboVariantLabels = true;
        [SerializeField] private bool enableReferenceDepthLayers = true;
        [SerializeField] [Range(1, 6)] private int comboDepthLayerCount = 3;
        [SerializeField] private Vector2 comboDepthStep = new Vector2(0f, -4f);
        [SerializeField] private Color comboDepthColor = new Color(0.02f, 0.16f, 0.55f, 0.95f);

        [Header("=== COMBO RESPONSIVE LAYOUT ===")]
        [SerializeField] private bool enableResponsiveComboLayout = true;
        [SerializeField] [Range(0.45f, 0.98f)] private float comboPopupWidthRatio = 0.9f;
        [SerializeField] [Range(0.08f, 0.25f)] private float comboPopupHeightRatio = 0.15f;
        [SerializeField] [Min(240f)] private float comboPopupMinWidth = 720f;
        [SerializeField] [Min(100f)] private float comboPopupMinHeight = 180f;
        [SerializeField] [Min(0f)] private float comboPopupMaxWidth = 1600f;
        [SerializeField] [Min(0f)] private float comboPopupMaxHeight = 260f;
        [SerializeField] [Range(-0.2f, 0.2f)] private float comboPopupVerticalOffsetRatio = 0.025f;
        [SerializeField] private float comboPopupVerticalOffsetPixels = 0f;
        [SerializeField] [Min(0f)] private float comboPopupHorizontalPadding = 36f;
        [SerializeField] [Min(0f)] private float comboPopupVerticalPadding = 12f;
        [SerializeField] [Min(24f)] private float comboPopupMinFontSize = 60f;

        [Header("=== COMBO TEXT EFFECTS ===")]
        [SerializeField] private bool enableGradientFill = true;
        [SerializeField] private Color comboGradientColorA = new Color(0.6f, 0.95f, 1f, 1f); // Çok açık mavi
        [SerializeField] private Color comboGradientColorB = new Color(0.2f, 0.7f, 1f, 1f); // Orta mavi
        
        [SerializeField] private bool enableThickOutline = true;
        [SerializeField] private float comboThickOutlineWidth = 0.35f;
        [SerializeField] private Color comboThickOutlineColor = new Color(0f, 0.25f, 0.7f, 1f); // Koyu mavi
        
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private float comboGlowOffset = 0.3f;
        [SerializeField] private Color comboGlowColor = new Color(0.4f, 0.85f, 1f, 0.4f); // Parlak mavi glow
        
        [SerializeField] private bool enableShadow = true;
        [SerializeField] private Color comboShadowColor = new Color(0f, 0.2f, 0.5f, 0.6f); // Koyu mavi shadow

        [Header("=== COMBO ANIMATIONS ===")]
        [SerializeField] private bool enableScalePop = true;
        [SerializeField] private float comboPopScale = 1.4f;
        [SerializeField] private float comboPopDuration = 0.25f;
        
        [SerializeField] private bool enableFadeIn = true;
        [SerializeField] private float comboFadeDuration = 0.25f;
        
        [SerializeField] private bool enableSlightShake = true;
        [SerializeField] private float comboShakeIntensity = 0.4f;
        [SerializeField] private float comboShakeDuration = 0.2f;
        
        [SerializeField] private bool enableGlowPulse = true;
        [SerializeField] private float comboPulseSpeed = 2.5f;
        [SerializeField] private float comboPulseAmount = 0.2f;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SimpleGridView gridView;
        [SerializeField] private Boot.GameBootstrap gameBootstrap;
        [SerializeField] private Blocks.NewBlockTray blockTray;
        [SerializeField] private Canvas hudCanvas;

        [Header("=== SCORE CELEBRATION THRESHOLDS ===")]
        [SerializeField] [Min(1)] private int goodScoreThreshold = 20;
        [SerializeField] [Min(1)] private int greatScoreThreshold = 50;
        [SerializeField] [Min(1)] private int epicScoreThreshold = 100;

        [Header("=== HAPTICS ===")]
        [SerializeField] private bool enableComboHaptics = true;
        [SerializeField] [Min(2)] private int comboHapticThreshold = 2;
        private Coroutine comboPopupRoutine;
        private Canvas comboPopupCanvas;
        private GameObject comboPopupRoot;
        private RectTransform comboPopupRect;
        private CanvasGroup comboPopupCanvasGroup;
        private TextMeshProUGUI comboPopupMainText;
        private RectTransform comboPopupMainTextRect;
        private readonly List<TextMeshProUGUI> comboPopupDepthTexts = new List<TextMeshProUGUI>(6);
        private Material comboPopupReferenceMaterial;
        private Material comboPopupDefaultMaterial;
        private Material comboPopupReferenceSourceMaterial;
        private Material comboPopupDefaultSourceMaterial;
        private readonly List<SpriteRenderer> breakVisualPool = new List<SpriteRenderer>(24);
        private bool _loggedDependencyWarning;
        private bool _loggedResourcesFallbackWarning;

        private void Start()
        {
            ResolveDependencies();

            // 🎬 Subscribe to game events
            Boot.GameBootstrap.OnBoardChanged += HandleBoardChanged;
            Boot.GameBootstrap.OnScoreBreakdown += HandleScoreBreakdown;
            Boot.GameBootstrap.OnGameOver += HandleGameOver;

            GameLogger.Log("[AnimationIntegration] Initialized and subscribed to game events");
        }

        private void OnDestroy()
        {
            StopComboPopupRoutine();
            DestroyComboPopupResources();
            DestroyBreakVisualPool();
            Boot.GameBootstrap.OnBoardChanged -= HandleBoardChanged;
            Boot.GameBootstrap.OnScoreBreakdown -= HandleScoreBreakdown;
            Boot.GameBootstrap.OnGameOver -= HandleGameOver;
        }

        #region Event Handlers

        /// <summary>
        /// Blok yerleştirildi veya satır silindi
        /// </summary>
        private void HandleBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            if (linesCleared > 0)
            {
                PlayLineClearSequence(boardState, linesCleared, clearedPositions);
            }
            else
            {
                // Normal placement feedback - shake
                PlayPlacementFeedback();
            }
        }

        /// <summary>
        /// Skor artış event'ı
        /// </summary>
        private void HandleScoreBreakdown(Boot.ScoreBreakdownInfo breakdown)
        {
            if (breakdown.ScoreDelta <= 0)
                return;

            // 🎯 Floating text göster
            SpawnScorePopup(breakdown);

            // 🎬 Combo effect'i kontrol et
            if (breakdown.LinesCleared > 0 && breakdown.ComboStreak >= 2)
            {
                TriggerComboHaptic(breakdown.ComboStreak);
                PlayComboEffect(breakdown.ComboStreak);
            }
        }

        /// <summary>
        /// Oyun sonu
        /// </summary>
        private void HandleGameOver(int finalScore)
        {
            GameLogger.Log($"[AnimationIntegration] Game over with final score: {finalScore}");
            PlayBoardGameOverBreakSequence();
            PlayTrayGameOverBreakSequence();
            // Oyun sonu animasyonları eklenebilir
        }

        private void PlayTrayGameOverBreakSequence()
        {
            ResolveDependencies();
            var tray = blockTray;
            if (tray == null)
                return;

            for (int i = 0; i < 3; i++)
            {
                var block = tray.GetBlock(i);
                if (block == null || block.IsUsed || !block.gameObject.activeInHierarchy)
                    continue;

                PlayBreakEffectsForTrayBlock(block);
                block.SetAlpha(0f);
            }
        }

        private void PlayBoardGameOverBreakSequence()
        {
            ResolveDependencies();
            var board = gameBootstrap?.CurrentState?.Board;
            if (board == null || gridView == null)
                return;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (!board.IsOccupied(x, y))
                        continue;

                    PlayBlockBreakEffectAtCell(x, y, 4);
                    gridView.ForceCellEmptyVisual(x, y);
                }
            }
        }

        private void PlayBreakEffectsForTrayBlock(Blocks.NewSimpleBlock block)
        {
            if (block == null)
                return;

            const int spectacleCombo = 4;
            Color blockColor = block.BlockColor;

            for (int i = 0; i < block.transform.childCount; i++)
            {
                Transform cellTransform = block.transform.GetChild(i);
                if (cellTransform == null || !cellTransform.gameObject.activeInHierarchy)
                    continue;

                Transform breakVisual = CreateTemporaryBreakVisual(cellTransform, blockColor);
                if (breakVisual != null)
                {
                    AnimationController.Instance.PlayBlockBreakEffect(
                        breakVisual,
                        blockColor,
                        () =>
                        {
                            if (breakVisual != null)
                                ReleaseBreakVisual(breakVisual);
                        }
                    );
                }

                if (VFXEmitter.Instance != null)
                {
                    Vector3 worldPosition = cellTransform.position;
                    VFXEmitter.Instance.EmitBlockBreakParticles(worldPosition, blockColor, spectacleCombo, emitSecondaryEffects: false);
                    VFXEmitter.Instance.EmitLineClearEffect(worldPosition, blockColor, gridView != null ? gridView.CellSize : 0.6f);
                }
            }
        }

        private string ResolveSafeComboCelebrationLabel(int comboValue)
        {
            if (comboValue >= 8)
                return "İNANILMAZ KOMBO";
            if (comboValue >= 5)
                return "EFSANE KOMBO";
            if (comboValue >= 3)
                return "HARİKA KOMBO";

            return comboTextPrefix;
        }

        private string BuildComboPopupText(int comboValue)
        {
            if (!useComboValueInText)
                return comboTextPrefix;

            if (!keepComboVariantLabels)
                return forceReferenceComboLook ? "Combo" : $"Combo {comboValue}";

            if (comboValue >= 3)
            {
                string celebrationLabel = ResolveSafeComboCelebrationLabel(comboValue);
                return $"{celebrationLabel} x{comboValue}";
            }

            return $"Combo x{comboValue}";
        }

        private void ApplyComboPopupTextStyle(TextMeshProUGUI textComponent)
        {
            if (textComponent == null)
                return;

            if (comboPopupFont == null)
            {
                comboPopupFont = Resources.Load<TMP_FontAsset>(ComboPopupFontResourcePath);
                LogResourcesFallbackOnce();
            }

            textComponent.fontSize = comboFontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Normal;
            textComponent.color = comboTextColor;
            textComponent.outlineWidth = enableThickOutline ? comboThickOutlineWidth : comboOutlineWidth;
            textComponent.outlineColor = enableThickOutline ? comboThickOutlineColor : comboOutlineColor;
            textComponent.characterSpacing = 0f;
            textComponent.wordSpacing = 0f;
            textComponent.lineSpacing = 0f;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.enableAutoSizing = enableResponsiveComboLayout;
            textComponent.fontSizeMin = Mathf.Min(comboFontSize, comboPopupMinFontSize);
            textComponent.fontSizeMax = comboFontSize;
            textComponent.richText = false;
            textComponent.font = comboPopupFont != null ? comboPopupFont : TMP_Settings.defaultFontAsset;
            textComponent.fontSharedMaterial = textComponent.font != null ? textComponent.font.material : null;

            if (forceReferenceComboLook)
            {
                textComponent.fontStyle = FontStyles.Bold;
                textComponent.color = Color.white;
                textComponent.enableVertexGradient = true;
                textComponent.colorGradient = new VertexGradient(
                    new Color(0.73f, 0.95f, 1f, 1f),
                    new Color(0.73f, 0.95f, 1f, 1f),
                    new Color(0.16f, 0.48f, 0.98f, 1f),
                    new Color(0.16f, 0.48f, 0.98f, 1f)
                );
                textComponent.outlineWidth = 0.34f;
                textComponent.outlineColor = new Color(0.02f, 0.27f, 0.72f, 1f);

                if (textComponent.fontSharedMaterial != null)
                    textComponent.fontSharedMaterial = GetOrCreateComboPopupMaterial(textComponent.fontSharedMaterial, true);

                return;
            }

            // ===== EFFECTS =====
            
            // Gradient Fill
            if (enableGradientFill)
            {
                textComponent.enableVertexGradient = true;
                VertexGradient gradient = new VertexGradient(comboGradientColorA, comboGradientColorA, comboGradientColorB, comboGradientColorB);
                textComponent.colorGradient = gradient;
            }
            else
            {
                textComponent.enableVertexGradient = false;
                textComponent.color = comboTextColor;
            }
            
            // Outline + Glow - enhance outline for depth
            float totalOutlineWidth = comboThickOutlineWidth;
            if (enableGlow)
            {
                totalOutlineWidth += comboGlowOffset * 0.05f;
            }
            textComponent.outlineWidth = totalOutlineWidth;
            
            // Create material instance and force face/outline/underlay colors
            if (textComponent.fontSharedMaterial != null)
                textComponent.fontSharedMaterial = GetOrCreateComboPopupMaterial(textComponent.fontSharedMaterial, false);
        }

        private Material GetOrCreateComboPopupMaterial(Material sourceMaterial, bool referenceLook)
        {
            if (sourceMaterial == null)
                return null;

            Material cachedMaterial = referenceLook ? comboPopupReferenceMaterial : comboPopupDefaultMaterial;
            Material cachedSourceMaterial = referenceLook ? comboPopupReferenceSourceMaterial : comboPopupDefaultSourceMaterial;

            if (cachedMaterial == null || cachedSourceMaterial != sourceMaterial)
            {
                if (cachedMaterial != null)
                    Destroy(cachedMaterial);

                cachedMaterial = new Material(sourceMaterial);
                cachedSourceMaterial = sourceMaterial;

                if (referenceLook)
                {
                    comboPopupReferenceMaterial = cachedMaterial;
                    comboPopupReferenceSourceMaterial = cachedSourceMaterial;
                }
                else
                {
                    comboPopupDefaultMaterial = cachedMaterial;
                    comboPopupDefaultSourceMaterial = cachedSourceMaterial;
                }
            }

            if (cachedMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
                cachedMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

            if (referenceLook)
            {
                if (cachedMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
                    cachedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.02f, 0.27f, 0.72f, 1f));

                if (cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    cachedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.01f, 0.12f, 0.36f, 0.85f));

                if (cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, -0.08f);

                if (cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.12f);

                if (cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
            }
            else
            {
                if (cachedMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
                    cachedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, comboThickOutlineColor);

                if (enableShadow && cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    cachedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, comboShadowColor);

                if (enableShadow && cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, -0.08f);

                if (enableShadow && cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.12f);

                if (enableShadow && cachedMaterial.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    cachedMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.22f);
            }

            return cachedMaterial;
        }

        private void EnsureComboPopup(Canvas canvas)
        {
            if (canvas == null)
                return;

            if (comboPopupRoot != null)
            {
                if (comboPopupCanvas != canvas)
                {
                    Destroy(comboPopupRoot);
                    comboPopupRoot = null;
                    comboPopupRect = null;
                    comboPopupCanvasGroup = null;
                    comboPopupMainText = null;
                    comboPopupMainTextRect = null;
                    comboPopupDepthTexts.Clear();
                }
                else
                {
                    return;
                }
            }

            DestroyLegacyComboPopups(canvas);

            comboPopupCanvas = canvas;
            comboPopupRoot = new GameObject("ComboPopup");
            comboPopupRoot.transform.SetParent(canvas.transform, false);

            comboPopupRect = comboPopupRoot.AddComponent<RectTransform>();
            comboPopupRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboPopupRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboPopupRect.pivot = new Vector2(0.5f, 0.5f);
            comboPopupRect.anchoredPosition = new Vector2(0f, 50f);
            comboPopupRect.sizeDelta = new Vector2(1600f, 240f);

            comboPopupCanvasGroup = comboPopupRoot.AddComponent<CanvasGroup>();
            comboPopupCanvasGroup.alpha = enableFadeIn ? 0f : 1f;

            GameObject comboMainText = new GameObject("ComboMainText");
            comboMainText.transform.SetParent(comboPopupRoot.transform, false);
            comboPopupMainText = comboMainText.AddComponent<TextMeshProUGUI>();

            comboPopupMainTextRect = comboMainText.GetComponent<RectTransform>();
            comboPopupMainTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboPopupMainTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboPopupMainTextRect.pivot = new Vector2(0.5f, 0.5f);
            comboPopupMainTextRect.anchoredPosition = Vector2.zero;
            comboPopupMainTextRect.sizeDelta = new Vector2(1600f, 240f);

            EnsureReferenceDepthLayers();
            comboPopupMainText.transform.SetAsLastSibling();
            comboPopupRoot.SetActive(false);
        }

        private static void DestroyLegacyComboPopups(Canvas canvas)
        {
            if (canvas == null)
                return;

            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (child != null && child.name.Equals("ComboPopup", System.StringComparison.Ordinal))
                    Object.Destroy(child.gameObject);
            }
        }

        private void EnsureReferenceDepthLayers()
        {
            if (comboPopupRoot == null || comboPopupMainText == null)
                return;

            for (int i = comboPopupDepthTexts.Count; i < comboDepthLayerCount; i++)
            {
                GameObject depthObj = new GameObject($"ComboDepth_{i + 1}");
                depthObj.transform.SetParent(comboPopupRoot.transform, false);
                depthObj.transform.SetSiblingIndex(0);
                comboPopupDepthTexts.Add(depthObj.AddComponent<TextMeshProUGUI>());
            }

            for (int i = 0; i < comboPopupDepthTexts.Count; i++)
            {
                var depthText = comboPopupDepthTexts[i];
                if (depthText == null)
                    continue;

                bool shouldBeActive = forceReferenceComboLook && enableReferenceDepthLayers && i < comboDepthLayerCount;
                depthText.gameObject.SetActive(shouldBeActive);
                if (!shouldBeActive)
                    continue;

                int layerIndex = comboDepthLayerCount - i;
                depthText.transform.SetSiblingIndex(0);
                depthText.text = comboPopupMainText.text;
                depthText.font = comboPopupMainText.font;
                depthText.fontSharedMaterial = comboPopupMainText.fontSharedMaterial;
                depthText.fontSize = comboPopupMainText.fontSize;
                depthText.alignment = comboPopupMainText.alignment;
                depthText.fontStyle = FontStyles.Bold;
                depthText.enableAutoSizing = comboPopupMainText.enableAutoSizing;
                depthText.fontSizeMin = comboPopupMainText.fontSizeMin;
                depthText.fontSizeMax = comboPopupMainText.fontSizeMax;
                depthText.richText = false;
                depthText.enableVertexGradient = false;
                depthText.color = comboDepthColor;
                depthText.outlineWidth = 0f;
                depthText.textWrappingMode = comboPopupMainText.textWrappingMode;
                depthText.overflowMode = comboPopupMainText.overflowMode;

                RectTransform depthRect = depthText.rectTransform;
                depthRect.anchorMin = new Vector2(0.5f, 0.5f);
                depthRect.anchorMax = new Vector2(0.5f, 0.5f);
                depthRect.pivot = new Vector2(0.5f, 0.5f);
                depthRect.sizeDelta = comboPopupMainText.rectTransform.sizeDelta;
                depthRect.anchoredPosition = comboDepthStep * layerIndex;
            }
        }

        private void ResetComboPopupVisualState()
        {
            if (comboPopupRoot != null)
                comboPopupRoot.transform.localScale = Vector3.one;

            if (comboPopupRect != null)
                comboPopupRect.anchoredPosition = new Vector2(0f, 50f);

            if (comboPopupCanvasGroup != null)
                comboPopupCanvasGroup.alpha = enableFadeIn ? 0f : 1f;
        }

        private void HideComboPopup()
        {
            if (comboPopupRoot == null)
                return;

            ResetComboPopupVisualState();
            comboPopupRoot.SetActive(false);
        }

        private void DestroyComboPopupResources()
        {
            if (comboPopupReferenceMaterial != null)
                Destroy(comboPopupReferenceMaterial);

            if (comboPopupDefaultMaterial != null)
                Destroy(comboPopupDefaultMaterial);

            comboPopupReferenceMaterial = null;
            comboPopupDefaultMaterial = null;
            comboPopupReferenceSourceMaterial = null;
            comboPopupDefaultSourceMaterial = null;

            if (comboPopupRoot != null)
                Destroy(comboPopupRoot);

            comboPopupRoot = null;
            comboPopupRect = null;
            comboPopupCanvasGroup = null;
            comboPopupMainText = null;
            comboPopupMainTextRect = null;
            comboPopupCanvas = null;
            comboPopupDepthTexts.Clear();
        }

        private SpriteRenderer GetOrCreateBreakVisualRenderer()
        {
            for (int i = 0; i < breakVisualPool.Count; i++)
            {
                var renderer = breakVisualPool[i];
                if (renderer != null && !renderer.gameObject.activeSelf)
                    return renderer;
            }

            var breakObj = new GameObject("BreakVisual");
            var spriteRenderer = breakObj.AddComponent<SpriteRenderer>();
            breakObj.SetActive(false);
            breakVisualPool.Add(spriteRenderer);
            return spriteRenderer;
        }

        private Transform CreateTemporaryBreakVisual(Transform sourceCellTransform, Color blockColor)
        {
            if (sourceCellTransform == null)
                return null;

            var sourceRenderer = sourceCellTransform.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null || sourceRenderer.sprite == null)
                return null;

            var pooledRenderer = GetOrCreateBreakVisualRenderer();
            if (pooledRenderer == null)
                return null;

            Transform breakTransform = pooledRenderer.transform;
            breakTransform.position = sourceCellTransform.position;
            breakTransform.rotation = sourceCellTransform.rotation;
            breakTransform.localScale = sourceCellTransform.lossyScale;

            pooledRenderer.sprite = sourceRenderer.sprite;
            pooledRenderer.color = blockColor;
            pooledRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            pooledRenderer.sortingOrder = sourceRenderer.sortingOrder + 10;
            pooledRenderer.gameObject.SetActive(true);

            return breakTransform;
        }

        private void ReleaseBreakVisual(Transform breakVisual)
        {
            if (breakVisual == null)
                return;

            breakVisual.localScale = Vector3.one;

            var renderer = breakVisual.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = null;
                renderer.color = Color.white;
            }

            breakVisual.gameObject.SetActive(false);
        }

        private void DestroyBreakVisualPool()
        {
            for (int i = 0; i < breakVisualPool.Count; i++)
            {
                var renderer = breakVisualPool[i];
                if (renderer == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(renderer.gameObject);
                else
                    DestroyImmediate(renderer.gameObject);
            }

            breakVisualPool.Clear();
        }

        private void ConfigureResponsiveComboPopup(Canvas canvas, RectTransform containerRect, RectTransform textRect, TextMeshProUGUI textComponent)
        {
            if (containerRect == null || textRect == null)
                return;

            Vector2 containerSize = new Vector2(1600f, 240f);
            float verticalOffset = 50f;

            if (enableResponsiveComboLayout)
            {
                RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
                float canvasWidth = canvasRect != null && canvasRect.rect.width > 0f ? canvasRect.rect.width : Screen.width;
                float canvasHeight = canvasRect != null && canvasRect.rect.height > 0f ? canvasRect.rect.height : Screen.height;

                float maxWidth = comboPopupMaxWidth > 0f ? comboPopupMaxWidth : canvasWidth;
                float maxHeight = comboPopupMaxHeight > 0f ? comboPopupMaxHeight : canvasHeight;

                containerSize.x = Mathf.Clamp(canvasWidth * comboPopupWidthRatio, comboPopupMinWidth, Mathf.Max(comboPopupMinWidth, maxWidth));
                containerSize.y = Mathf.Clamp(canvasHeight * comboPopupHeightRatio, comboPopupMinHeight, Mathf.Max(comboPopupMinHeight, maxHeight));
                verticalOffset = (canvasHeight * comboPopupVerticalOffsetRatio) + comboPopupVerticalOffsetPixels;
            }

            containerRect.anchoredPosition = new Vector2(0f, verticalOffset);
            containerRect.sizeDelta = containerSize;

            float paddedWidth = Mathf.Max(120f, containerSize.x - (comboPopupHorizontalPadding * 2f));
            float paddedHeight = Mathf.Max(80f, containerSize.y - (comboPopupVerticalPadding * 2f));
            textRect.sizeDelta = new Vector2(paddedWidth, paddedHeight);

            if (textComponent != null)
            {
                textComponent.enableAutoSizing = enableResponsiveComboLayout;
                textComponent.fontSize = comboFontSize;
                textComponent.fontSizeMax = comboFontSize;
                textComponent.fontSizeMin = Mathf.Min(comboFontSize, comboPopupMinFontSize);
                textComponent.margin = new Vector4(0f, 0f, 0f, 0f);
            }
        }

        private static string ResolveSafeScorePopupPrefix(ScorePopupTier tier)
        {
            return tier switch
            {
                ScorePopupTier.Epic => "EFSANE ",
                ScorePopupTier.Great => "HARIKA ",
                ScorePopupTier.Good => "IYI ",
                _ => string.Empty
            };
        }

        #endregion

        #region Animation Sequences

        private void PlayPlacementFeedback()
        {
            if (!AreAnimationsEnabled())
                return;

            if (mainCamera != null)
            {
                AnimationController.Instance.PlayCameraShake(
                    mainCamera,
                    vfxPreset.cameraShakeDuration,
                    vfxPreset.cameraShakeIntensity
                );
            }
        }

        private void PlayLineClearSequence(BoardState boardState, int linesCleared, Int2[] clearedPositions)
        {
            bool animationsEnabled = AreAnimationsEnabled();

            if (gridView == null)
            {
                GameLogger.LogWarning("[AnimationIntegration] GridView not found!");
                return;
            }

            // 1. Satır highlight animasyonu
            GameLogger.Log($"[AnimationIntegration] Playing line clear effect for {linesCleared} lines");

            // 2. Silinen her blok için break effect (combo-aware)
            ResolveDependencies();
            int comboCount = (gameBootstrap?.CurrentState?.Combo ?? 0);
            if (clearedPositions != null && clearedPositions.Length > 0)
            {
                foreach (var pos in clearedPositions)
                {
                    PlayBlockBreakEffectAtCell(pos.X, pos.Y, comboCount);
                }
            }

            // 3. Cleared cells'de parçacık efekti
            EmitLineClearParticles(boardState, clearedPositions, linesCleared);

            // 4. Camera shake
            if (animationsEnabled && mainCamera != null)
            {
                AnimationController.Instance.PlayCameraShake(
                    mainCamera,
                    vfxPreset.cameraShakeDuration * 1.5f,
                    vfxPreset.cameraShakeIntensity * 2f
                );
            }

            if (animationsEnabled && VFXEmitter.Instance != null)
            {
                VFXEmitter.Instance.TriggerSlowMoLineClear();
            }

            // 5. Line clear animation sequence
            var clearedLines = new List<int>();
            if (clearedPositions != null && clearedPositions.Length > 0)
            {
                clearedLines = clearedPositions
                    .Select(p => p.Y)
                    .Distinct()
                    .ToList();
            }
            
            if (animationsEnabled)
            {
                AnimationController.Instance.PlayLineClearEffect(
                    clearedLines,
                    gridView.CellSize,
                    () =>
                    {
                        GameLogger.Log("[AnimationIntegration] Line clear animation completed");
                    }
                );
            }
        }

        private static bool AreAnimationsEnabled()
        {
            return PlayerPrefs.GetInt(AnimationsPreferenceKey, 1) == 1;
        }

        private void TriggerComboHaptic(int comboValue)
        {
            if (!enableComboHaptics || comboValue < comboHapticThreshold)
                return;

            if (PlayerPrefs.GetInt(VibrationPreferenceKey, 1) != 1)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Gridin belirtilen cell'inde blok kırılma efektini oynat (combo-aware)
        /// </summary>
        private void PlayBlockBreakEffectAtCell(int x, int y, int comboCount = 0)
        {
            Transform cellTransform = gridView.GetCellTransform(x, y);
            if (cellTransform == null) return;

            Color blockColor = GetClearedCellEffectColor(x, y);

            Transform breakVisual = CreateTemporaryBreakVisual(cellTransform, blockColor);
            if (breakVisual != null)
            {
                // Block break animation
                AnimationController.Instance.PlayBlockBreakEffect(
                    breakVisual,
                    blockColor,
                    () =>
                    {
                        if (breakVisual != null)
                            ReleaseBreakVisual(breakVisual);
                    }
                );
            }

            Vector3 worldPosition = cellTransform.position;
            int spectacleCombo = Mathf.Max(comboCount, 4);

            if (VFXEmitter.Instance != null)
            {
                // Every cleared block should visibly spit particles out.
                VFXEmitter.Instance.EmitBlockBreakParticles(worldPosition, blockColor, spectacleCombo, emitSecondaryEffects: false);
                VFXEmitter.Instance.EmitLineClearEffect(worldPosition, blockColor, gridView.CellSize);
            }
        }

        private void EmitLineClearParticles(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            if (gridView == null)
            {
                GameLogger.LogWarning("[AnimationIntegration] GridView not found!");
                return;
            }

            if (clearedPositions == null || clearedPositions.Length == 0)
            {
                return;
            }

            for (int i = 0; i < clearedPositions.Length; i++)
            {
                Int2 pos = clearedPositions[i];
                Vector3 worldPos = gridView.GetWorldPosition(pos.X, pos.Y);
                Color cellColor = GetClearedCellEffectColor(pos.X, pos.Y);

                VFXEmitter.Instance.EmitLineClearEffect(
                    worldPos,
                    cellColor,
                    gridView.CellSize
                );
            }
        }

        private void SpawnScorePopup(Boot.ScoreBreakdownInfo breakdown)
        {
            if (!AreAnimationsEnabled())
                return;

            int scoreAmount = breakdown.ScoreDelta;
            // 🎯 HUD'ın üzerinde floating text göster
            ResolveDependencies();
            Canvas activeHudCanvas = hudCanvas;
            if (activeHudCanvas == null) return;

            var tier = ResolveScoreTier(scoreAmount);
            float durationMultiplier = tier switch
            {
                ScorePopupTier.Epic => 1.4f,
                ScorePopupTier.Great => 1.2f,
                ScorePopupTier.Good => 1.1f,
                _ => 1f
            };

            string prefix = tier switch
            {
                ScorePopupTier.Epic => "EFSANE ",
                ScorePopupTier.Great => "HARİKA ",
                ScorePopupTier.Good => "İYİ ",
                _ => string.Empty
            };

	            prefix = ResolveSafeScorePopupPrefix(tier);

	            Color popupColor = tier switch
            {
                ScorePopupTier.Epic => new Color(1f, 0.85f, 0.2f, 1f),
                ScorePopupTier.Great => new Color(1f, 0.95f, 0.45f, 1f),
                ScorePopupTier.Good => new Color(0.85f, 1f, 0.55f, 1f),
                _ => new Color(1f, 0.9f, 0.3f, 1f)
            };

            Vector3 hudPos;
            if (activeHudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                hudPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.82f, 0f);
            }
            else
            {
                hudPos = activeHudCanvas.transform.position + Vector3.up * 50f;
            }

            GameObject floatText = VFXEmitter.Instance.SpawnFloatingText(
                $"{prefix}+{scoreAmount}",
                hudPos,
                popupColor,
                uiAnimPreset.floatingTextTiming.duration * durationMultiplier
            );

            if (floatText != null)
            {
                Vector3 endPos = hudPos + Vector3.up * (uiAnimPreset.floatingTextRiseDist * durationMultiplier);
                AnimationController.Instance.PlayFloatingTextAnim(
                    floatText,
                    hudPos,
                    endPos,
                    uiAnimPreset.floatingTextTiming.duration * durationMultiplier
                );
            }

            if (mainCamera != null && tier >= ScorePopupTier.Great)
            {
                float shakeIntensity = tier == ScorePopupTier.Epic ? 2.1f : 1.6f;
                AnimationController.Instance.PlayCameraShake(mainCamera, 0.12f, shakeIntensity);
            }
        }

        /// <summary>
        /// Combo visual effect - Canvas'te ortada combo yazı (x2, x3, x4 vb.)
        /// </summary>
        private void PlayComboEffect(int comboValue)
        {
            GameLogger.Log($"[AnimationIntegration] COMBO EFFECT x{comboValue}!");

            if (!AreAnimationsEnabled())
                return;

            ResolveDependencies();
            Canvas canvas = hudCanvas;
            if (canvas == null) return;

            StopComboPopupRoutine();
            EnsureComboPopup(canvas);
            if (comboPopupRoot == null || comboPopupRect == null || comboPopupCanvasGroup == null || comboPopupMainText == null || comboPopupMainTextRect == null)
                return;

            comboPopupRoot.SetActive(true);
            ResetComboPopupVisualState();
            var textComponent = comboPopupMainText;
            
            // Dinamik combo yazı oluştur
            textComponent.text = BuildComboPopupText(comboValue);
            
            // Font seç - Türkçe karakterleri destekleyen font kullan
            TMP_FontAsset fontToUse = null;
            
            // 1. Inspector'de ayarlanmış font var mı diye bak
            if (comboPopupFont != null)
            {
                fontToUse = comboPopupFont;
            }
            // 2. Resources'tan yükle (LuckiestGuy Combo SDF)
            else
            {
                fontToUse = Resources.Load<TMP_FontAsset>(ComboPopupFontResourcePath);
                LogResourcesFallbackOnce();
            }
            
            // 3. Fallback: Arial vardır diye bak (standard TMPro font)
            if (fontToUse == null)
            {
                fontToUse = TMP_Settings.defaultFontAsset;
            }
            
            if (fontToUse != null)
            {
                textComponent.font = fontToUse;
                textComponent.fontSharedMaterial = fontToUse.material;
                GameLogger.Log($"[AnimationIntegration] Font loaded: {fontToUse.name}");
            }
            else
            {
                GameLogger.LogError("[AnimationIntegration] No font found!");
            }
            
            ApplyComboPopupTextStyle(textComponent);
            ConfigureResponsiveComboPopup(canvas, comboPopupRect, comboPopupMainTextRect, textComponent);
            EnsureReferenceDepthLayers();
            comboPopupMainText.transform.SetAsLastSibling();

            // Camera shake
            if (mainCamera != null)
            {
                AnimationController.Instance.PlayCameraShake(mainCamera, 0.15f, 1.5f);
            }

            // Combo text animasyonu: pop in → stay → pop out
            comboPopupRoutine = StartCoroutine(ComboUIAnimation(comboPopupRoot, comboPopupCanvasGroup, comboPopupRect, 1.5f));
        }

        private System.Collections.IEnumerator ComboUIAnimation(GameObject comboUI, CanvasGroup canvasGroup, RectTransform rectTransform, float totalDuration)
        {
            if (comboUI == null || canvasGroup == null || rectTransform == null)
            {
                comboPopupRoutine = null;
                yield break;
            }

            var textComponent = comboUI == comboPopupRoot && comboPopupMainText != null
                ? comboPopupMainText
                : comboUI.GetComponentInChildren<TextMeshProUGUI>();
            Vector2 originalPos = rectTransform.anchoredPosition;
            
            comboUI.transform.localScale = Vector3.zero;
            if (enableFadeIn)
                canvasGroup.alpha = 0f;

            // Phase 1: Pop in + Fade in (0-popDuration) - Scale & Fade
            float popDuration = enableScalePop ? comboPopDuration : comboFadeDuration;
            float shakeDuration = enableSlightShake ? comboShakeDuration : popDuration;
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                if (comboUI == null || rectTransform == null || canvasGroup == null)
                {
                    comboPopupRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                
                // Scale pop
                float scale = enableScalePop ? EaseOutElasticUI(t) * comboPopScale : 1f;
                comboUI.transform.localScale = new Vector3(scale, scale, 1f);
                
                // Fade in
                if (enableFadeIn)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, EaseOutQuadUI(t));
                
                // Slight shake
                if (enableSlightShake && elapsed < shakeDuration)
                {
                    float shakeT = elapsed / shakeDuration;
                    float shakeX = Mathf.Sin(shakeT * Mathf.PI * 8f) * comboShakeIntensity;
                    float shakeY = Mathf.Cos(shakeT * Mathf.PI * 6f) * comboShakeIntensity;
                    rectTransform.anchoredPosition = originalPos + new Vector2(shakeX, shakeY);
                }
                
                yield return null;
            }
            
            if (comboUI == null || rectTransform == null || canvasGroup == null)
            {
                comboPopupRoutine = null;
                yield break;
            }

            rectTransform.anchoredPosition = originalPos;

            // Phase 2: Hold with glow pulse (popDuration to 0.8s)
            float holdStart = popDuration;
            float holdEnd = 0.8f;
            float holdDuration = holdEnd - holdStart;
            elapsed = 0f;
            
            while (elapsed < holdDuration)
            {
                if (comboUI == null || rectTransform == null || canvasGroup == null)
                {
                    comboPopupRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                
                // Glow pulse effect
                if (enableGlowPulse && textComponent != null)
                {
                    float pulse = Mathf.Sin(elapsed * comboPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
                    float outlineWidth = comboThickOutlineWidth + (comboPulseAmount * pulse);
                    textComponent.outlineWidth = outlineWidth;
                }
                
                yield return null;
            }

            // Phase 3: Pop out (0.8-totalDuration) - Fade + scale down
            elapsed = 0f;
            float popOutDuration = totalDuration - holdEnd;
            while (elapsed < popOutDuration)
            {
                if (comboUI == null || rectTransform == null || canvasGroup == null)
                {
                    comboPopupRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / popOutDuration;
                
                comboUI.transform.localScale = Vector3.Lerp(
                    new Vector3(comboPopScale, comboPopScale, 1f),
                    Vector3.zero,
                    EaseInQuadUI(t)
                );
                
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            HideComboPopup();

            comboPopupRoutine = null;
        }

        private void StopComboPopupRoutine()
        {
            if (comboPopupRoutine == null)
                return;

            StopCoroutine(comboPopupRoutine);
            comboPopupRoutine = null;
            HideComboPopup();
        }

        private float EaseOutElasticUI(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private float EaseInQuadUI(float t) => t * t;
        
        private float EaseOutQuadUI(float t) => 1f - (1f - t) * (1f - t);

        private ScorePopupTier ResolveScoreTier(int scoreDelta)
        {
            if (scoreDelta >= Mathf.Max(greatScoreThreshold + 1, epicScoreThreshold))
                return ScorePopupTier.Epic;

            if (scoreDelta >= Mathf.Max(goodScoreThreshold + 1, greatScoreThreshold))
                return ScorePopupTier.Great;

            if (scoreDelta >= goodScoreThreshold)
                return ScorePopupTier.Good;

            return ScorePopupTier.Normal;
        }

        #endregion

        #region Utility

        private Color GetClearedCellEffectColor(int x, int y)
        {
            if (gridView != null)
            {
                ResolveDependencies();
                var board = gameBootstrap?.CurrentState?.Board;
                if (board != null)
                {
                    var cellState = board.GetCell(x, y);
                    if (cellState.BlockId != 0)
                    {
                        return gridView.GetBlockColor(cellState.ColorId);
                    }
                }
            }

            int pseudoColorId = ((x + y) % 8) + 1;
            return GetCellColor(pseudoColorId);
        }

        private Color GetCellColor(int colorId)
        {
            // Default color palette (NewSimpleBlock ile match et)
            Color[] palette = new Color[]
            {
                new Color(0.9f, 0.3f, 0.8f),  // Magenta
                new Color(0.5f, 0.3f, 0.9f),  // Purple
                new Color(0.2f, 0.7f, 1f),    // Cyan
                new Color(1f, 0.5f, 0.2f),    // Orange
                new Color(0.3f, 0.8f, 0.4f),  // Green
                new Color(1f, 0.2f, 0.2f),    // Red
                new Color(0.9f, 0.8f, 0.1f),  // Yellow
                new Color(0.7f, 0.4f, 1f)     // Violet
            };

            if (colorId <= 0 || colorId > palette.Length)
                return ProjectColorGrading.Apply(Color.white);

            return ProjectColorGrading.Apply(palette[colorId - 1]);
        }

        private void ResolveDependencies()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera != null)
                    GameLogger.LogWarning("[AnimationIntegration] mainCamera is not wired in the inspector. Falling back to Camera.main.");
            }

            if (gridView == null)
            {
                gridView = FindFirstObjectByType<SimpleGridView>();
                if (gridView != null)
                    GameLogger.LogWarning("[AnimationIntegration] gridView was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (gameBootstrap == null)
            {
                gameBootstrap = FindFirstObjectByType<Boot.GameBootstrap>();
                if (gameBootstrap != null)
                    GameLogger.LogWarning("[AnimationIntegration] gameBootstrap was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (blockTray == null)
            {
                blockTray = FindFirstObjectByType<Blocks.NewBlockTray>();
                if (blockTray != null)
                    GameLogger.LogWarning("[AnimationIntegration] blockTray was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (hudCanvas == null)
            {
                hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                    GameLogger.LogWarning("[AnimationIntegration] hudCanvas was resolved via runtime lookup. Inspector wiring is the preferred production path.");
            }

            if (!_loggedDependencyWarning && (gridView == null || gameBootstrap == null))
            {
                _loggedDependencyWarning = true;
                GameLogger.LogWarning(
                    $"[AnimationIntegration] Scene wiring missing. Required references should be assigned in the inspector. " +
                    $"gridView={(gridView != null)}, gameBootstrap={(gameBootstrap != null)}");
            }
        }

        private void LogResourcesFallbackOnce()
        {
            if (_loggedResourcesFallbackWarning)
                return;

            _loggedResourcesFallbackWarning = true;
            GameLogger.LogWarning(
                "[AnimationIntegration] comboPopupFont was not assigned. Falling back to Resources.Load. " +
                "Inspector font wiring is preferred for production.");
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (mainCamera == null)
                mainCamera = TryAutoAssignSingleton<Camera>();

            if (gridView == null)
                gridView = TryAutoAssignSingleton<SimpleGridView>();

            if (gameBootstrap == null)
                gameBootstrap = TryAutoAssignSingleton<Boot.GameBootstrap>();

            if (blockTray == null)
                blockTray = TryAutoAssignSingleton<Blocks.NewBlockTray>();

            if (hudCanvas == null)
                hudCanvas = TryAutoAssignSingleton<Canvas>();
#endif
        }

#if UNITY_EDITOR
        private static T TryAutoAssignSingleton<T>() where T : UnityEngine.Object
        {
            T[] instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return instances.Length == 1 ? instances[0] : null;
        }
#endif

        #endregion

        private enum ScorePopupTier
        {
            Normal = 0,
            Good = 1,
            Great = 2,
            Epic = 3
        }
    }
}
