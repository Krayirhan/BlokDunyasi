#pragma warning disable 0414
using UnityEngine.InputSystem;
using System;
using System.Reflection;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Configuration;
using BlockPuzzle.UnityAdapter.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlockPuzzle.UnityAdapter.UI
{
    [ExecuteAlways]
    public class GameSceneThemeController : MonoBehaviour
    {
        [Header("Universal Flat Background")]
        [SerializeField] private bool useUniversalSolidBackground = true;
        [SerializeField] private Color universalBackgroundColor = new Color(0.055f, 0.082f, 0.141f, 1f); // #0E1524 Clean Dark Slate

        private const int Theme2RunScoreMilestone = 900;
        private const int Theme3RunScoreMilestone = 2200;
        private const int Theme2ComboMilestone = 3;
        private const int Theme3ComboMilestone = 5;
        private static bool _firstGameplayOpeningConsumed;

        public enum ThemeSlot
        {
            Theme1 = 0,
            Theme2 = 1,
            Theme3 = 2,
            Theme4 = 3
        }

        [Serializable]
        public class SceneThemeData
        {
            public string displayName = "Theme";

            [Header("Background")]
            public Sprite gameplayBackgroundSpriteOverride;
            public Color gameplayBackgroundTint = Color.white;
            public Color gameplayBackgroundDimmerColor = new Color(0f, 0.30980393f, 0.36862746f, 0.34f);
            public Color gameplayCameraClearColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);

            [Header("Board")]
            public Color emptyCellColor = new Color(0.12f, 0.15f, 0.25f, 1f);
            public Color emptyCellBorderColor = new Color(0.3f, 0.38f, 0.55f, 0.65f);
            public Color boardBackdropColor = new Color(0.06f, 0.1f, 0.2f, 0.55f);
            public Color boardBackdropBorderColor = new Color(0.75f, 0.9f, 1f, 0.12f);

            [Header("Tray")]
            public BlockSpriteConfig blockSpriteConfig;
            public Color[] blockPalette = new Color[8];
            public float trayBlockBrightness = 1f;
            public float dragBrightnessMultiplier = 1.2f;
            public float trayNormalAlpha = 1f;
            public float trayDragAlpha = 0.9f;

            [Header("HUD")]
            public Color scoreTextColor = Color.white;
            public Color bestScoreTextColor = Color.white;
            public Color turnTextColor = Color.white;
            public Color statusTextColor = Color.white;
            public Color progressBarColor = Color.white;
            public Color progressTextColor = Color.white;
            public Color targetTextColor = Color.white;
            public Color primaryGraphicColor = Color.white;
            public Color secondaryGraphicColor = Color.white;
        }

        [Header("Mode")]
        [SerializeField] private bool livePreviewInEditor = true;
        [SerializeField] private bool applyThemeOnPlay = true;
        [SerializeField] private bool useAutomaticThemeProgression = true;
        [SerializeField] private ThemeSlot selectedTheme = ThemeSlot.Theme1;
        [SerializeField] private bool seedTheme1FromCurrentSceneOnce = true;
        [SerializeField] private bool theme1Seeded;
        [SerializeField] private bool verboseThemeLogs = true;
        

        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private SimpleGridView gridView;
        [SerializeField] private NewBlockTray blockTray;
        [SerializeField] private HudView hudView;
        [SerializeField] private TargetGoalSystem targetGoalSystem;
        [SerializeField] private Graphic[] primaryGraphics;
        [SerializeField] private Graphic[] secondaryGraphics;

        [Header("Themes")]
        [SerializeField] private SceneThemeData theme1 = new SceneThemeData { displayName = "Klasik" };
        [SerializeField] private SceneThemeData theme2 = new SceneThemeData { displayName = "Meyve" };
        [SerializeField] private SceneThemeData theme3 = new SceneThemeData { displayName = "Kot Pantolon" };
        [SerializeField] private SceneThemeData theme4 = new SceneThemeData { displayName = "Ahşap (Wood)" };

        private ThemeSlot _lastAppliedTheme = (ThemeSlot)(-1);
        private bool _theme2TriggeredThisRun;
        private bool _theme3TriggeredThisRun;
        private bool _runThemeInitialized;
        private int _forceTheme1FramesRemaining;
        private bool _manualThemeSelected;
        private int _lastComboStreak;

        public bool LivePreviewInEditor
        {
            get => livePreviewInEditor;
            set => livePreviewInEditor = value;
        }

        public ThemeSlot SelectedTheme
        {
            get => selectedTheme;
            set => selectedTheme = value;
        }

        public SceneThemeData Theme1 => theme1;
        public SceneThemeData Theme2 => theme2;
        public SceneThemeData Theme3 => theme3;
        public SceneThemeData Theme4 => theme4;

        public static GameSceneThemeController GetOrCreateRuntimeController()
        {
            GameSceneThemeController controller = FindFirstObjectByType<GameSceneThemeController>(FindObjectsInactive.Include);
            if (controller != null || !Application.isPlaying)
                return controller;

            var host = new GameObject("[RuntimeGameSceneThemeController]");
            controller = host.AddComponent<GameSceneThemeController>();
            controller.BuildRuntimeThemeSetFromScene();
            return controller;
        }

                private void Start()
        {
            if (Application.isPlaying && applyThemeOnPlay)
            {
                ApplyThemeById(UISettingsProfile.GetThemeId());
            }
        }

        private void Awake()
        {
            ResolveReferences();
            TrySeedTheme1();

            if (Application.isPlaying && applyThemeOnPlay)
            {
                if (useAutomaticThemeProgression)
                    InitializeThemeForRun();

                ApplyThemeById(UISettingsProfile.GetThemeId());
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            TrySeedTheme1();

            if (!Application.isPlaying && livePreviewInEditor)
                ApplySelectedTheme();

            if (Application.isPlaying)
            {
                GameBootstrap.OnGameStarted += HandleRuntimeGameStarted;
                GameBootstrap.OnGameContinued += HandleRuntimeGameContinued;
                GameBootstrap.OnScoreBreakdown += HandleRuntimeScoreBreakdown;
                GameBootstrap.OnGameOver += HandleRuntimeGameOver;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            GameBootstrap.OnGameStarted -= HandleRuntimeGameStarted;
            GameBootstrap.OnGameContinued -= HandleRuntimeGameContinued;
            GameBootstrap.OnScoreBreakdown -= HandleRuntimeScoreBreakdown;
            GameBootstrap.OnGameOver -= HandleRuntimeGameOver;
        }

        private void Update()
        {
            if (useUniversalSolidBackground)
            {
                var mainCam = Camera.main;
                if (mainCam != null && mainCam.backgroundColor != universalBackgroundColor)
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = universalBackgroundColor;
                }
            }
            // Listen for T key to cycle themes
            bool tPressed = false;
            try
            {
                if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                    tPressed = true;
            }
            catch {}
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.T))
                    tPressed = true;
            }
            catch {}

            if (tPressed)
            {
                CycleThemeForTesting();
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && livePreviewInEditor)
            {
                if (_lastAppliedTheme != selectedTheme)
                    ApplyTheme(selectedTheme);
                return;
            }
#endif

            if (!Application.isPlaying || !applyThemeOnPlay)
                return;

            if (_forceTheme1FramesRemaining > 0)
            {
                _forceTheme1FramesRemaining--;
                UISettingsProfile.SetThemeId(UISettingsProfile.ThemeClassic);
                ApplyTheme(ThemeSlot.Theme1);
                return;
            }

            ThemeSlot runtimeSlot = ResolveEnabledTheme(UISettingsProfile.GetThemeId());
            if (_lastAppliedTheme != runtimeSlot)
                ApplyTheme(runtimeSlot);
        }

        private void OnValidate()
        {
            ResolveReferences();
            TrySeedTheme1();

            if (!Application.isPlaying && livePreviewInEditor)
                ApplySelectedTheme();
        }

        public void ApplySelectedTheme()
        {
            ApplyTheme(selectedTheme);
        }

        public void ApplyThemeById(int themeId)
        {
            ApplyTheme(ResolveEnabledTheme(themeId));
        }

        /// <summary>
        /// Applies a player-selected theme and prevents automatic progression
        /// from overwriting it during the current session.
        /// </summary>
        public void ApplyManualThemeById(int themeId)
        {
            _manualThemeSelected = true;
            int clampedThemeId = (int)ResolveEnabledTheme(themeId);
            UISettingsProfile.SetThemeId(clampedThemeId);
            UISettingsProfile.SetLastAutomaticThemeId(clampedThemeId);
            ApplyTheme((ThemeSlot)clampedThemeId);
        }

        public void CycleThemeForTesting()
        {
            _forceTheme1FramesRemaining = 0;
            int currentThemeId = UISettingsProfile.GetThemeId();
            // T-key preview is intentionally limited to the four approved themes.
            int nextThemeId = currentThemeId >= UISettingsProfile.ThemeClassic &&
                              currentThemeId <= UISettingsProfile.ThemeWood
                ? (currentThemeId + 1) % 4
                : UISettingsProfile.ThemeClassic;
            selectedTheme = ClampThemeId(nextThemeId);
            ApplyManualThemeById(nextThemeId);
            UnityEngine.Debug.Log($"[GameSceneThemeController] CycleThemeForTesting -> Theme {nextThemeId} ({selectedTheme}) applied successfully!");
        }

        public void SelectAutomaticThemeForCurrentProgress()
        {
            InitializeThemeForRun();
        }

        private void InitializeThemeForRun()
        {
            _runThemeInitialized = true;
            _theme2TriggeredThisRun = false;
            _theme3TriggeredThisRun = false;

            if (!_firstGameplayOpeningConsumed)
            {
                _firstGameplayOpeningConsumed = true;
                _forceTheme1FramesRemaining = 0;
                ApplyAutomaticTheme(UISettingsProfile.ThemeClassic, "first gameplay opening");
                return;
            }

            ApplyAutomaticTheme(
                PickWeightedTheme(
                    UISettingsProfile.GetLastAutomaticThemeId(),
                    (UISettingsProfile.ThemeClassic, 1f),
                    (UISettingsProfile.ThemeNight, 1f),
                    (UISettingsProfile.ThemeVivid, 1f),
                    (UISettingsProfile.ThemeWood, 1f)),
                "run-start random");
        }

        private void HandleRuntimeGameStarted()
        {
            if (!useAutomaticThemeProgression || !applyThemeOnPlay)
                return;

            if (_runThemeInitialized)
            {
                _runThemeInitialized = false;
                if (_forceTheme1FramesRemaining > 0)
                {
                    UISettingsProfile.SetThemeId(UISettingsProfile.ThemeClassic);
                    ApplyTheme(ThemeSlot.Theme1);
                    return;
                }

                ApplyThemeById(UISettingsProfile.GetThemeId());
                return;
            }

            InitializeThemeForRun();
            ApplyThemeById(UISettingsProfile.GetThemeId());
        }

        private void HandleRuntimeGameContinued()
        {
            if (!useAutomaticThemeProgression || !applyThemeOnPlay)
                return;

            _theme2TriggeredThisRun = UISettingsProfile.GetThemeId() >= UISettingsProfile.ThemeNight;
            _theme3TriggeredThisRun = UISettingsProfile.GetThemeId() >= UISettingsProfile.ThemeVivid;
            EvaluateThemeMilestones(gameBootstrap != null ? gameBootstrap.CurrentScore : 0, comboStreak: 0, allowThemePromotion: true);
        }

        private void HandleRuntimeGameOver(int _)
        {
            _runThemeInitialized = false;
            _theme2TriggeredThisRun = false;
            _theme3TriggeredThisRun = false;
            _lastComboStreak = 0;
        }

        private void HandleRuntimeScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            if (!useAutomaticThemeProgression || !applyThemeOnPlay)
                return;

            EvaluateComboThemeProgression(breakdown);
        }

        private void EvaluateThemeMilestones(int totalScore, int comboStreak, bool allowThemePromotion)
        {
            if (!allowThemePromotion || _manualThemeSelected)
                return;

            int currentThemeId = UISettingsProfile.GetThemeId();
            if (!_theme2TriggeredThisRun &&
                (comboStreak >= Theme2ComboMilestone || totalScore >= Theme2RunScoreMilestone))
            {
                _theme2TriggeredThisRun = true;
                ApplyAutomaticTheme((currentThemeId + 1) % 4, $"milestone-1 totalScore={totalScore}, combo={comboStreak}");
                currentThemeId = UISettingsProfile.GetThemeId();
            }

            if (!_theme3TriggeredThisRun &&
                (comboStreak >= Theme3ComboMilestone || totalScore >= Theme3RunScoreMilestone))
            {
                _theme3TriggeredThisRun = true;
                ApplyAutomaticTheme((currentThemeId + 1) % 4, $"milestone-2 totalScore={totalScore}, combo={comboStreak}");
            }
        }

        private void EvaluateComboThemeProgression(ScoreBreakdownInfo breakdown)
        {
            bool shouldAdvanceTheme = false;

            // Trigger on combo streak increment (Combo 1, Combo 2, Combo 3...)
            if (breakdown.ComboStreak > 0 && breakdown.ComboStreak != _lastComboStreak)
            {
                _lastComboStreak = breakdown.ComboStreak;
                shouldAdvanceTheme = true;
            }
            // Also trigger on exciting multi-line clears (2 or more lines cleared at once)
            else if (breakdown.LinesCleared >= 2)
            {
                shouldAdvanceTheme = true;
            }

            // Reset streak tracker if combo drops
            if (breakdown.ComboStreak == 0)
            {
                _lastComboStreak = 0;
            }

            if (shouldAdvanceTheme)
            {
                int currentThemeId = UISettingsProfile.GetThemeId();
                int nextThemeId = (currentThemeId + 1) % 4;
                ApplyAutomaticTheme(nextThemeId, $"Combo: {breakdown.ComboStreak}x, Lines: {breakdown.LinesCleared}");
            }
        }

        private void ApplyAutomaticTheme(int themeId, string reason)
        {
            int nextThemeId = (int)ResolveEnabledTheme(themeId);
            UISettingsProfile.SetThemeId(nextThemeId);
            UISettingsProfile.SetLastAutomaticThemeId(nextThemeId);
            selectedTheme = (ThemeSlot)nextThemeId;
            ApplyTheme((ThemeSlot)nextThemeId);

            UnityEngine.Debug.Log($"[GameSceneThemeController] Combo Theme Advance -> Theme {nextThemeId} ({selectedTheme}) applied! ({reason})");
        }

        private static int PickWeightedTheme(int excludedThemeId, params (int themeId, float weight)[] entries)
        {
            float totalWeight = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].themeId == excludedThemeId && entries.Length > 1)
                    continue;

                totalWeight += Mathf.Max(0f, entries[i].weight);
            }

            if (totalWeight <= 0f)
                return entries.Length > 0 ? entries[0].themeId : UISettingsProfile.ThemeClassic;

            float pick = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].themeId == excludedThemeId && entries.Length > 1)
                    continue;

                cumulative += Mathf.Max(0f, entries[i].weight);
                if (pick <= cumulative)
                    return entries[i].themeId;
            }

            return entries[entries.Length - 1].themeId;
        }

        public void ApplyTheme(ThemeSlot slot)
        {
            ResolveReferences();

            SceneThemeData data = GetThemeData(slot);
            if (data == null)
                return;

            ApplyToBootstrap(data);
            ApplyToGrid(data);
            ApplyToTray(data);
            ApplyToHud(data);
            ApplyToTargetGoal(data);
            ApplyGraphics(primaryGraphics, data.primaryGraphicColor);
            ApplyGraphics(secondaryGraphics, data.secondaryGraphicColor);

            _lastAppliedTheme = slot;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (gameBootstrap != null) EditorUtility.SetDirty(gameBootstrap);
                if (gridView != null) EditorUtility.SetDirty(gridView);
                if (blockTray != null) EditorUtility.SetDirty(blockTray);
                if (hudView != null) EditorUtility.SetDirty(hudView);
                if (targetGoalSystem != null) EditorUtility.SetDirty(targetGoalSystem);
                if (gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        [ContextMenu("Capture Current To Theme 1")]
        public void CaptureCurrentToTheme1()
        {
            ResolveReferences();
            CaptureCurrentInto(theme1);
            theme1.displayName = "Klasik";
            theme1Seeded = true;
        }

        [ContextMenu("Capture Current To Theme 2")]
        public void CaptureCurrentToTheme2()
        {
            ResolveReferences();
            CaptureCurrentInto(theme2);
            theme2.displayName = "Meyve";
        }

        [ContextMenu("Capture Current To Theme 3")]
        public void CaptureCurrentToTheme3()
        {
            ResolveReferences();
            CaptureCurrentInto(theme3);
            theme3.displayName = "Kot Pantolon";
        }

        private void TrySeedTheme1()
        {
            if (!seedTheme1FromCurrentSceneOnce || theme1Seeded)
                return;

            if (!HasEnoughReferences())
                return;

            CaptureCurrentInto(theme1);
            theme1.displayName = "Theme 1";
            theme1Seeded = true;
        }

        private void BuildRuntimeThemeSetFromScene()
        {
            ResolveReferences();
            if (!HasEnoughReferences())
                return;

            CaptureCurrentInto(theme1);
            theme1.displayName = "Theme 1";
            theme1Seeded = true;

            CopyTheme(theme1, theme2);
            CopyTheme(theme1, theme3);
            theme2.displayName = "Theme 2";
            theme3.displayName = "Theme 3";

            ApplyThemeVariant(theme2, 0.88f, 0.92f, 1.08f, 0.82f, 1.08f);
            ApplyThemeVariant(theme3, 1.16f, 1.12f, 0.94f, 1.18f, 0.94f);
        }

        private bool HasEnoughReferences()
        {
            return gameBootstrap != null && gridView != null && blockTray != null;
        }

        private static void CopyTheme(SceneThemeData source, SceneThemeData target)
        {
            if (source == null || target == null)
                return;

            target.gameplayBackgroundSpriteOverride = source.gameplayBackgroundSpriteOverride;
            target.gameplayBackgroundTint = source.gameplayBackgroundTint;
            target.gameplayBackgroundDimmerColor = source.gameplayBackgroundDimmerColor;
            target.gameplayCameraClearColor = source.gameplayCameraClearColor;
            target.emptyCellColor = source.emptyCellColor;
            target.emptyCellBorderColor = source.emptyCellBorderColor;
            target.boardBackdropColor = source.boardBackdropColor;
            target.boardBackdropBorderColor = source.boardBackdropBorderColor;
            target.blockSpriteConfig = source.blockSpriteConfig;
            target.blockPalette = ClonePalette(source.blockPalette);
            target.trayBlockBrightness = source.trayBlockBrightness;
            target.dragBrightnessMultiplier = source.dragBrightnessMultiplier;
            target.trayNormalAlpha = source.trayNormalAlpha;
            target.trayDragAlpha = source.trayDragAlpha;
            target.scoreTextColor = source.scoreTextColor;
            target.bestScoreTextColor = source.bestScoreTextColor;
            target.turnTextColor = source.turnTextColor;
            target.statusTextColor = source.statusTextColor;
            target.progressBarColor = source.progressBarColor;
            target.progressTextColor = source.progressTextColor;
            target.targetTextColor = source.targetTextColor;
            target.primaryGraphicColor = source.primaryGraphicColor;
            target.secondaryGraphicColor = source.secondaryGraphicColor;
        }

        private static void ApplyThemeVariant(SceneThemeData data, float saturation, float value, float hueShiftDegrees, float boardBrightness, float hudBrightness)
        {
            if (data == null)
                return;

            data.gameplayBackgroundTint = ShiftColor(data.gameplayBackgroundTint, saturation, value, hueShiftDegrees, 1f);
            data.gameplayBackgroundDimmerColor = ShiftColor(data.gameplayBackgroundDimmerColor, saturation, value, hueShiftDegrees, 1f);
            data.gameplayCameraClearColor = ShiftColor(data.gameplayCameraClearColor, saturation, value, hueShiftDegrees, 1f);
            data.emptyCellColor = ShiftColor(data.emptyCellColor, saturation, value, hueShiftDegrees, boardBrightness);
            data.emptyCellBorderColor = ShiftColor(data.emptyCellBorderColor, saturation, value, hueShiftDegrees, boardBrightness);
            data.boardBackdropColor = ShiftColor(data.boardBackdropColor, saturation, value, hueShiftDegrees, boardBrightness);
            data.boardBackdropBorderColor = ShiftColor(data.boardBackdropBorderColor, saturation, value, hueShiftDegrees, boardBrightness);
            data.scoreTextColor = ShiftColor(data.scoreTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.bestScoreTextColor = ShiftColor(data.bestScoreTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.turnTextColor = ShiftColor(data.turnTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.statusTextColor = ShiftColor(data.statusTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.progressBarColor = ShiftColor(data.progressBarColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.progressTextColor = ShiftColor(data.progressTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.targetTextColor = ShiftColor(data.targetTextColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.primaryGraphicColor = ShiftColor(data.primaryGraphicColor, saturation, value, hueShiftDegrees, hudBrightness);
            data.secondaryGraphicColor = ShiftColor(data.secondaryGraphicColor, saturation, value, hueShiftDegrees, hudBrightness);

            if (data.blockPalette == null)
                return;

            for (int i = 0; i < data.blockPalette.Length; i++)
                data.blockPalette[i] = ShiftColor(data.blockPalette[i], saturation, value, hueShiftDegrees, 1.04f);
        }

        private static Color ShiftColor(Color source, float saturationMultiplier, float valueMultiplier, float hueShiftDegrees, float brightnessMultiplier)
        {
            Color.RGBToHSV(source, out float h, out float s, out float v);
            h = Mathf.Repeat(h + (hueShiftDegrees / 360f), 1f);
            s = Mathf.Clamp01(s * saturationMultiplier);
            v = Mathf.Clamp01(v * valueMultiplier * brightnessMultiplier);
            Color shifted = Color.HSVToRGB(h, s, v);
            shifted.a = source.a;
            return shifted;
        }

        private void ResolveReferences()
        {
            if (gameBootstrap == null)
                gameBootstrap = FindFirstObjectByType<GameBootstrap>();
            if (gridView == null)
                gridView = FindFirstObjectByType<SimpleGridView>();
            if (blockTray == null)
                blockTray = FindFirstObjectByType<NewBlockTray>();
            if (hudView == null)
                hudView = FindFirstObjectByType<HudView>();
            if (targetGoalSystem == null)
                targetGoalSystem = FindFirstObjectByType<TargetGoalSystem>();
        }

        private SceneThemeData GetThemeData(ThemeSlot slot)
        {
            return slot switch
            {
                ThemeSlot.Theme1 => theme1,
                ThemeSlot.Theme2 => theme2,
                ThemeSlot.Theme3 => theme3,
                ThemeSlot.Theme4 => theme4,
                _ => theme1
            };
        }

        private static ThemeSlot ClampThemeId(int themeId)
        {
            return themeId switch
            {
                0 => ThemeSlot.Theme1,
                1 => ThemeSlot.Theme2,
                2 => ThemeSlot.Theme3,
                3 => ThemeSlot.Theme4,
                _ => ThemeSlot.Theme1
            };
        }

        private ThemeSlot ResolveEnabledTheme(int themeId)
        {
            return ClampThemeId(themeId);
        }

        private void CaptureCurrentInto(SceneThemeData data)
        {
            if (data == null)
                return;

            data.gameplayBackgroundSpriteOverride = GetField<Sprite>(gameBootstrap, "gameplayBackgroundSpriteOverride");
            data.gameplayBackgroundTint = GetField<Color>(gameBootstrap, "gameplayBackgroundTint");
            data.gameplayBackgroundDimmerColor = GetField<Color>(gameBootstrap, "gameplayBackgroundDimmerColor");
            data.gameplayCameraClearColor = GetField<Color>(gameBootstrap, "gameplayCameraClearColor");

            data.emptyCellColor = GetField<Color>(gridView, "emptyCellColor");
            data.emptyCellBorderColor = GetField<Color>(gridView, "emptyCellBorderColor");
            data.boardBackdropColor = GetField<Color>(gridView, "boardBackdropColor");
            data.boardBackdropBorderColor = GetField<Color>(gridView, "boardBackdropBorderColor");

            data.blockSpriteConfig = GetField<BlockSpriteConfig>(blockTray, "spriteConfig");
            if (data.blockSpriteConfig == null)
                data.blockSpriteConfig = GetField<BlockSpriteConfig>(gridView, "spriteConfig");
            data.blockPalette = ClonePalette(GetField<Color[]>(blockTray, "blockColors"));
            data.trayBlockBrightness = GetField<float>(blockTray, "trayBlockBrightness");
            data.dragBrightnessMultiplier = GetField<float>(blockTray, "dragBrightnessMultiplier");
            data.trayNormalAlpha = GetField<float>(blockTray, "normalAlpha");
            data.trayDragAlpha = GetField<float>(blockTray, "dragAlpha");

            data.scoreTextColor = GetHudTextColor("currentScoreText");
            data.bestScoreTextColor = GetHudTextColor("bestScoreText");
            data.turnTextColor = GetHudTextColor("turnCountText");
            data.statusTextColor = GetHudTextColor("gameStatusText");
            data.progressBarColor = GetField<Color>(targetGoalSystem, "progressBarColor");
            data.progressTextColor = GetTargetTextColor("progressText");
            data.targetTextColor = GetTargetTextColor("targetText");
            data.primaryGraphicColor = GetFirstGraphicColor(primaryGraphics, Color.white);
            data.secondaryGraphicColor = GetFirstGraphicColor(secondaryGraphics, Color.white);
        }

                private void ApplyToBootstrap(SceneThemeData data)
        {
            SetField(gameBootstrap, "gameplayBackgroundSpriteOverride", data.gameplayBackgroundSpriteOverride);
            SetField(gameBootstrap, "gameplayBackgroundTint", data.gameplayBackgroundTint);
            SetField(gameBootstrap, "gameplayBackgroundDimmerColor", data.gameplayBackgroundDimmerColor);
            SetField(gameBootstrap, "gameplayCameraClearColor", data.gameplayCameraClearColor);

            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = data.gameplayCameraClearColor;
            }

            // 1. Update SceneBackground SpriteRenderer
            SpriteRenderer bgSr = null;
            if (gameBootstrap != null)
            {
                var bgChild = gameBootstrap.transform.Find("SceneBackground");
                if (bgChild != null) bgSr = bgChild.GetComponent<SpriteRenderer>();
            }
            if (bgSr == null)
            {
                var allSrs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var r in allSrs)
                {
                    if (r != null && r.gameObject.name == "SceneBackground")
                    {
                        bgSr = r;
                        break;
                    }
                }
            }

            if (bgSr != null)
            {
                if (data.gameplayBackgroundSpriteOverride != null)
                {
                    bgSr.gameObject.SetActive(true);
                    bgSr.enabled = true;
                    bgSr.sprite = data.gameplayBackgroundSpriteOverride;
                    bgSr.color = data.gameplayBackgroundTint;

                    if (cam != null && bgSr.sprite != null)
                    {
                        bgSr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
                        Vector2 spriteSize = bgSr.sprite.bounds.size;
                        if (spriteSize.x > 0f && spriteSize.y > 0f)
                        {
                            float worldHeight = cam.orthographicSize * 2f;
                            float worldWidth = worldHeight * cam.aspect;
                            float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
                            bgSr.transform.localScale = new Vector3(scale, scale, 1f);
                        }
                    }
                }
                else
                {
                    bgSr.enabled = false;
                    bgSr.gameObject.SetActive(false);
                }
            }

            // 2. Update SceneBackgroundDimmer SpriteRenderer
            SpriteRenderer dimmerSr = null;
            if (gameBootstrap != null)
            {
                var dimmerChild = gameBootstrap.transform.Find("SceneBackgroundDimmer");
                if (dimmerChild != null) dimmerSr = dimmerChild.GetComponent<SpriteRenderer>();
            }
            if (dimmerSr == null)
            {
                var allSrs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var r in allSrs)
                {
                    if (r != null && r.gameObject.name == "SceneBackgroundDimmer")
                    {
                        dimmerSr = r;
                        break;
                    }
                }
            }

            if (dimmerSr != null)
            {
                if (data.gameplayBackgroundSpriteOverride != null && data.gameplayBackgroundDimmerColor.a > 0.01f)
                {
                    dimmerSr.gameObject.SetActive(true);
                    dimmerSr.enabled = true;
                    dimmerSr.color = data.gameplayBackgroundDimmerColor;

                    if (cam != null)
                    {
                        dimmerSr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 9.5f);
                        if (dimmerSr.sprite != null)
                        {
                            Vector2 spriteSize = dimmerSr.sprite.bounds.size;
                            if (spriteSize.x > 0f && spriteSize.y > 0f)
                            {
                                float worldHeight = cam.orthographicSize * 2f;
                                float worldWidth = worldHeight * cam.aspect;
                                float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
                                dimmerSr.transform.localScale = new Vector3(scale, scale, 1f);
                            }
                        }
                    }
                }
                else
                {
                    dimmerSr.enabled = false;
                    dimmerSr.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyToGrid(SceneThemeData data)
        {
            SetField(gridView, "emptyCellColor", data.emptyCellColor);
            SetField(gridView, "emptyCellBorderColor", data.emptyCellBorderColor);
            SetField(gridView, "boardBackdropColor", data.boardBackdropColor);
            SetField(gridView, "boardBackdropBorderColor", data.boardBackdropBorderColor);
            if (data.blockSpriteConfig != null)
                gridView?.ApplyThemeSpriteConfig(data.blockSpriteConfig);

            InvokePrivate(gridView, "RefreshInspectorDrivenVisuals");
            InvokePrivate(gridView, "EnsureBoardBackdropVisible");
            object lastBoardState = GetFieldObject(gridView, "_lastBoardState");
            if (Application.isPlaying && lastBoardState != null)
                InvokePrivate(gridView, "OnBoardChanged", lastBoardState, null, 0);
        }

        private void ApplyToTray(SceneThemeData data)
        {
            if (data.blockSpriteConfig != null)
                blockTray?.ApplyThemeSpriteConfig(data.blockSpriteConfig);
            SetField(blockTray, "blockColors", ClonePalette(data.blockPalette));
            SetField(blockTray, "trayBlockBrightness", data.trayBlockBrightness);
            SetField(blockTray, "dragBrightnessMultiplier", data.dragBrightnessMultiplier);
            SetField(blockTray, "normalAlpha", data.trayNormalAlpha);
            SetField(blockTray, "dragAlpha", data.trayDragAlpha);

            InvokePrivate(blockTray, "RefreshBlockVisuals");
        }

                private void ApplyToHud(SceneThemeData data)
        {
            SetHudTextColor("currentScoreText", data.scoreTextColor);
            SetHudTextColor("bestScoreText", data.bestScoreTextColor);
            SetHudTextColor("turnCountText", data.turnTextColor);
            SetHudTextColor("gameStatusText", data.statusTextColor);

            // Harmonize Home Button (MainMenuButton ingame)
            var allImgs = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var img in allImgs)
            {
                if (img == null) continue;
                if (img.gameObject.name == "MainMenuButton ingame" || img.gameObject.name == "MainMenuButton")
                {
                    img.color = data.primaryGraphicColor;
                }
                else if (img.gameObject.name == "BestPanelarkasi")
                {
                    Color pCol = data.boardBackdropColor;
                    pCol.a = 0.85f;
                    img.color = pCol;
                }
            }
        }

                private void ApplyToTargetGoal(SceneThemeData data)
        {
            if (targetGoalSystem == null)
                targetGoalSystem = UnityEngine.Object.FindFirstObjectByType<TargetGoalSystem>(FindObjectsInactive.Include);

            if (targetGoalSystem != null)
            {
                SetField(targetGoalSystem, "progressBarColor", data.progressBarColor);
                SetTargetTextColor("progressText", data.progressTextColor);
                SetTargetTextColor("targetText", data.targetTextColor);

                // Update EmptyStatePanel (background capsule of progress bar)
                Transform emptyPanel = targetGoalSystem.transform.Find("EmptyStatePanel");
                if (emptyPanel == null)
                {
                    foreach (Transform c in targetGoalSystem.transform)
                    {
                        if (c.name.Contains("EmptyState") || c.name.Contains("Panel"))
                        {
                            emptyPanel = c;
                            break;
                        }
                    }
                }

                if (emptyPanel != null)
                {
                    var img = emptyPanel.GetComponent<Image>();
                    if (img != null)
                    {
                        Color capsuleColor = data.boardBackdropColor;
                        capsuleColor.a = 0.95f;
                        img.color = capsuleColor;
                    }
                }

                InvokePrivate(targetGoalSystem, "UpdateDisplay");
            }
        }

        private static void ApplyGraphics(Graphic[] graphics, Color color)
        {
            if (graphics == null)
                return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].color = color;
            }
        }

        private Color GetHudTextColor(string fieldName)
        {
            if (hudView == null)
                return Color.white;

            var text = GetField<TextMeshProUGUI>(hudView, fieldName);
            return text != null ? text.color : Color.white;
        }

        private void SetHudTextColor(string fieldName, Color color)
        {
            if (hudView == null)
                return;

            var text = GetField<TextMeshProUGUI>(hudView, fieldName);
            if (text != null)
                text.color = color;
        }

        private Color GetTargetTextColor(string fieldName)
        {
            if (targetGoalSystem == null)
                return Color.white;

            var text = GetField<TextMeshProUGUI>(targetGoalSystem, fieldName);
            return text != null ? text.color : Color.white;
        }

        private void SetTargetTextColor(string fieldName, Color color)
        {
            if (targetGoalSystem == null)
                return;

            var text = GetField<TextMeshProUGUI>(targetGoalSystem, fieldName);
            if (text != null)
                text.color = color;
        }

        private static Color[] ClonePalette(Color[] source)
        {
            if (source == null)
                return Array.Empty<Color>();

            var result = new Color[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static Color GetFirstGraphicColor(Graphic[] graphics, Color fallback)
        {
            if (graphics == null)
                return fallback;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    return graphics[i].color;
            }

            return fallback;
        }

        private static T GetField<T>(object target, string fieldName)
        {
            object value = GetFieldObject(target, fieldName);
            return value is T typed ? typed : default;
        }

        private static object GetFieldObject(object target, string fieldName)
        {
            if (target == null)
                return null;

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field != null ? field.GetValue(target) : null;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null)
                return;

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            if (target == null)
                return;

            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            method?.Invoke(target, args);
        }
    }
}
