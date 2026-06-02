#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BlokDunyasiTools
{
    [InitializeOnLoad]
    public static class GameOverSceneHierarchySetup
    {
        private const string TargetScenePath = "Assets/Scenes/GameOver.unity";
        private const string ThemeMarkerName = "GameOverThemeAppliedV5";
        private const string ThemeFontPath = "Assets/Skyden_Games/Free_Casual_GUI/Demo/Fonts/Baloo/Baloo-Regular SDF.asset";
        private const string CardSpritePath = "Assets/Images/uisettings/card_9slice_512.png";
        private const string HeaderSpritePath = "Assets/Images/uisettings/header_bar_1024x160.png";
        private const string StatsSpritePath = "Assets/Images/uisettings/card_section_1024x300.png";
        private const string SparkleSpritePath = "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Others/icon_Star.png";
        private const string BlueButtonSpritePath = "Assets/Buttons/PNG/12Button_Long_Blue.png";
        private const string YellowButtonSpritePath = "Assets/Buttons/PNG/15Button_Long_Yellow.png";
        private const string HomeIconSpritePath = "Assets/Images/buttons/Yeni/ic_home_128_transparent.png";
        private const string RestartIconSpritePath = "Assets/Images/buttons/Yeni/ic_restart_128_transparent.png";
        private const string PlayIconSpritePath = "Assets/Images/buttons/Yeni/ic_play_128_transparent.png";
        private static bool _ensureQueued;

        static GameOverSceneHierarchySetup()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureForActiveScene;
        }

        [MenuItem("BlokDunyasi/Setup/Ensure GameOver Hierarchy", false, 340)]
        public static void EnsureCurrentGameOverScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!IsTargetScene(scene))
            {
                scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            EnsureSceneHierarchy(scene);

            if (scene.isLoaded && scene.isDirty)
                EditorSceneManager.SaveScene(scene);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            QueueEnsure(scene);
        }

        private static void EnsureForActiveScene()
        {
            QueueEnsure(SceneManager.GetActiveScene());
        }

        private static void QueueEnsure(Scene scene)
        {
            if (_ensureQueued || EditorApplication.isPlayingOrWillChangePlaymode || !IsTargetScene(scene))
                return;

            _ensureQueued = true;
            EditorApplication.delayCall += () =>
            {
                _ensureQueued = false;
                EnsureSceneHierarchy(SceneManager.GetActiveScene());
            };
        }

        private static bool IsTargetScene(Scene scene)
        {
            return scene.IsValid() &&
                   !string.IsNullOrWhiteSpace(scene.path) &&
                   scene.path.Replace('\\', '/') == TargetScenePath;
        }

        private static void EnsureSceneHierarchy(Scene scene)
        {
            if (!IsTargetScene(scene) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool changed = false;
            TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontPath);
            if (defaultFont == null)
                defaultFont = TMP_Settings.defaultFontAsset;

            Canvas canvas = EnsureCanvas(scene, ref changed);
            EnsureMainCameraData(scene, ref changed);
            EnsureEventSystem(scene, ref changed);

            RectTransform panel = EnsureGameOverPanel(canvas.transform, ref changed);
            var view = panel.GetComponent<BlockPuzzle.UnityAdapter.UI.GameOverView>();
            if (view == null)
            {
                view = panel.gameObject.AddComponent<BlockPuzzle.UnityAdapter.UI.GameOverView>();
                changed = true;
            }

            if (panel.GetComponent<CanvasGroup>() == null)
            {
                panel.gameObject.AddComponent<CanvasGroup>();
                changed = true;
            }

            RectTransform layoutRoot = EnsureChildRect(panel, panel, "LayoutRoot", ref changed);
            ConfigureStretch(layoutRoot);
            bool preserveUserLayout = FindAnyThemeMarker(layoutRoot) != null;

            DisableLegacyElement(panel, "Background", ref changed);
            RectTransform backgroundLayer = EnsureChildRect(layoutRoot, panel, "BackgroundLayer", ref changed, "Background");
            ConfigureStretchIfNew(backgroundLayer, ref changed);
            backgroundLayer.SetSiblingIndex(0);

            DisableLegacyElement(panel, "ScoreLabel", ref changed);
            DisableLegacyElement(panel, "TitlePlate", ref changed);
            DisableLegacyElement(panel, "BrandText", ref changed);
            DisableLegacyElement(panel, "StatusRibbon", ref changed);
            DisableLegacyElement(panel, "ScoreRibbon", ref changed);
            DisableLegacyElement(panel, "SessionSummaryText", ref changed);
            DisableLegacyElement(panel, "BestMoveRow", ref changed);
            DisableLegacyElement(panel, "AverageMoveRow", ref changed);
            DisableLegacyElement(panel, "BestMoveValueText", ref changed);
            DisableLegacyElement(panel, "AverageMoveValueText", ref changed);

            RectTransform cardRoot = EnsureChildRect(layoutRoot, panel, "CardRoot", ref changed);
            SetRectIfUnset(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(860f, 1420f), new Vector2(0f, -20f), ref changed);
            cardRoot.SetSiblingIndex(1);

            RectTransform cardShadow = preserveUserLayout
                ? FindDeep(cardRoot, "CardShadow") as RectTransform
                : EnsureChildRect(cardRoot, panel, "CardShadow", ref changed);
            if (cardShadow != null)
            {
                ConfigureStretch(cardShadow);
                EnsureImage(cardShadow.gameObject, false, ref changed);
            }

            RectTransform cardFrame = EnsureChildRect(cardRoot, panel, "CardFrame", ref changed);
            ConfigureStretch(cardFrame);
            EnsureImage(cardFrame.gameObject, false, ref changed);

            RectTransform headerGroup = EnsureChildRect(cardRoot, panel, "HeaderGroup", ref changed);
            SetRectIfUnset(headerGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(720f, 210f), new Vector2(0f, -122f), ref changed);
            RectTransform headerPlate = EnsureChildRect(headerGroup, panel, "HeaderPlate", ref changed);
            ConfigureStretch(headerPlate);
            EnsureImage(headerPlate.gameObject, false, ref changed);
            var gameOverText = EnsureText(EnsureChildRect(headerGroup, panel, "GameOverText", ref changed).gameObject, defaultFont, "OYUN B\u0130TT\u0130", ref changed);
            SetRectIfUnset(gameOverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 92f), Vector2.zero, ref changed);

            RectTransform scoreGroup = EnsureChildRect(cardRoot, panel, "ScoreGroup", ref changed);
            SetRectIfUnset(scoreGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640f, 420f), new Vector2(0f, -430f), ref changed);
            var finalScoreText = EnsureText(EnsureChildRect(scoreGroup, panel, "FinalScoreText", ref changed).gameObject, defaultFont, "0", ref changed);
            SetRectIfUnset(finalScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(540f, 128f), new Vector2(0f, -36f), ref changed);

            RectTransform newBestBanner = EnsureChildRect(scoreGroup, panel, "NewBestBanner", ref changed);
            SetRectIfUnset(newBestBanner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 124f), new Vector2(0f, -176f), ref changed);
            EnsureImage(newBestBanner.gameObject, false, ref changed);
            var newBestText = EnsureText(EnsureChildRect(newBestBanner, panel, "NewBestText", ref changed).gameObject, defaultFont, "YEN\u0130 REKOR!", ref changed);
            SetRectIfUnset(newBestText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);

            var bestScoreText = EnsureText(EnsureChildRect(scoreGroup, panel, "BestScoreText", ref changed).gameObject, defaultFont, "En iyi skor: 0", ref changed);
            SetRectIfUnset(bestScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 52f), new Vector2(0f, -304f), ref changed);

            RectTransform statsGroup = EnsureChildRect(cardRoot, panel, "StatsGroup", ref changed);
            SetRectIfUnset(statsGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640f, 260f), new Vector2(0f, -720f), ref changed);
            EnsureImage(statsGroup.gameObject, false, ref changed);

            var maxComboValueText = EnsureStatRow(statsGroup, panel, defaultFont, "MaxComboRow", "MaxComboValueText", "MAKS COMBO", "x1", new Vector2(0f, -64f), ref changed);
            var totalLinesValueText = EnsureStatRow(statsGroup, panel, defaultFont, "TotalLinesRow", "TotalLinesValueText", "TOPLAM \u00c7\u0130ZG\u0130", "III", new Vector2(0f, -156f), ref changed);

            RectTransform buttonsGroup = EnsureChildRect(cardRoot, panel, "ButtonsGroup", ref changed);
            SetRectIfUnset(buttonsGroup, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(680f, 150f), new Vector2(0f, 96f), ref changed);

            var mainMenuButton = EnsureButton(buttonsGroup, panel, "MainMenuButton", "ANA MEN\u00dc", defaultFont, new Vector2(-154f, 0f), new Vector2(292f, 106f), ref changed, true);
            var restartButton = EnsureButton(buttonsGroup, panel, "RestartButton", "TEKRAR OYNA", defaultFont, new Vector2(154f, 0f), new Vector2(332f, 106f), ref changed, true);

            RectTransform continueOfferPanel = EnsureChildRect(panel, panel, "ContinueOfferPanel", ref changed);
            ConfigureStretch(continueOfferPanel);
            EnsureImage(continueOfferPanel.gameObject, true, ref changed, true);
            continueOfferPanel.SetAsLastSibling();
            if (continueOfferPanel.gameObject.activeSelf)
            {
                continueOfferPanel.gameObject.SetActive(false);
                changed = true;
            }

            RectTransform continueCard = EnsureChildRect(continueOfferPanel, panel, "ContinueCard", ref changed);
            SetRectIfUnset(continueCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 360f), Vector2.zero, ref changed);
            EnsureImage(continueCard.gameObject, false, ref changed);

            var noMovesText = EnsureText(EnsureChildRect(continueCard, panel, "NoMovesText", ref changed).gameObject, defaultFont, "Hamle kalmad\u0131!", ref changed);
            SetRectIfUnset(noMovesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 56f), new Vector2(0f, -72f), ref changed);
            var continueCountdownText = EnsureText(EnsureChildRect(continueCard, panel, "ContinueCountdownText", ref changed).gameObject, defaultFont, "Devam etmek i\u00e7in: 5", ref changed);
            SetRectIfUnset(continueCountdownText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(320f, 44f), new Vector2(0f, -144f), ref changed);
            var continueButton = EnsureButton(continueCard, panel, "ContinueButton", "DEVAM ET (REKLAM)", defaultFont, new Vector2(0f, -84f), new Vector2(420f, 110f), ref changed);

            WireGameOverView(view, panel.gameObject, finalScoreText, bestScoreText, newBestText, null, null, maxComboValueText, totalLinesValueText, null, restartButton, mainMenuButton, continueOfferPanel.gameObject, noMovesText, continueCountdownText, continueButton, ref changed);
            ApplyThemeDefaultsOnce(layoutRoot, ref changed);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static Canvas EnsureCanvas(Scene scene, ref bool changed)
        {
            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "GameUI")
                {
                    canvas = root.GetComponent<Canvas>();
                    if (canvas == null)
                        canvas = root.AddComponent<Canvas>();
                    break;
                }
            }

            if (canvas == null)
            {
                var go = new GameObject("GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(go, scene);
                canvas = go.GetComponent<Canvas>();
                changed = true;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                changed = true;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                changed = true;
            }

            return canvas;
        }

        private static void EnsureMainCameraData(Scene scene, ref bool changed)
        {
            Camera mainCamera = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (!camera.CompareTag("MainCamera") &&
                        !string.Equals(camera.name, "Main Camera", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    mainCamera = camera;
                    break;
                }

                if (mainCamera != null)
                    break;
            }

            if (mainCamera == null)
                return;

            if (mainCamera.GetComponent<UniversalAdditionalCameraData>() != null)
                return;

            mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            changed = true;
        }

        private static void EnsureEventSystem(Scene scene, ref bool changed)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<EventSystem>() != null)
                    return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
            SceneManager.MoveGameObjectToScene(eventSystemGo, scene);
            changed = true;
        }

        private static RectTransform EnsureGameOverPanel(Transform canvas, ref bool changed)
        {
            RectTransform panel = canvas.Find("GameOverPanel") as RectTransform;
            if (panel == null)
            {
                var go = new GameObject("GameOverPanel", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                panel = go.GetComponent<RectTransform>();
                changed = true;
            }

            ConfigureStretch(panel);
            return panel;
        }

        private static void DisableLegacyElement(Transform root, string name, ref bool changed)
        {
            var target = FindDeep(root, name);
            if (target == null)
                return;

            if (target.name == name)
            {
                target.name = $"Legacy_{name}";
                changed = true;
            }

            if (target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(false);
                changed = true;
            }
        }

        private static void ApplyThemeDefaultsOnce(RectTransform layoutRoot, ref bool changed)
        {
            if (layoutRoot == null)
                return;

            var marker = FindAnyThemeMarker(layoutRoot);
            if (marker != null)
            {
                if (marker.name != ThemeMarkerName)
                {
                    marker.name = ThemeMarkerName;
                    changed = true;
                }

                return;
            }

            ApplyThemeDefaults(layoutRoot, ref changed);

            var markerGo = new GameObject(ThemeMarkerName, typeof(RectTransform));
            markerGo.transform.SetParent(layoutRoot, false);
            markerGo.SetActive(false);
            changed = true;
        }

        private static void ApplyThemeDefaults(RectTransform layoutRoot, ref bool changed)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontPath) ?? TMP_Settings.defaultFontAsset;

            Sprite cardSprite = LoadSprite(CardSpritePath);
            Sprite headerSprite = LoadSprite(HeaderSpritePath);
            Sprite statsSprite = LoadSprite(StatsSpritePath);
            Sprite sparkleSprite = LoadSprite(SparkleSpritePath);
            Sprite blueButtonSprite = LoadSprite(BlueButtonSpritePath);
            Sprite yellowButtonSprite = LoadSprite(YellowButtonSpritePath);
            Sprite homeIconSprite = LoadSprite(HomeIconSpritePath);
            Sprite restartIconSprite = LoadSprite(RestartIconSpritePath);
            Sprite playIconSprite = LoadSprite(PlayIconSpritePath);
            Color cardColor = cardSprite != null ? Color.white : new Color(0.2f, 0.23f, 0.74f, 1f);
            Color headerColor = headerSprite != null ? Color.white : new Color(0.12f, 0.18f, 0.56f, 1f);
            Color statsColor = statsSprite != null ? Color.white : new Color(0.12f, 0.16f, 0.46f, 0.96f);

            var cardRoot = FindDeep(layoutRoot, "CardRoot") as RectTransform;
            var cardShadow = FindDeep(layoutRoot, "CardShadow") as RectTransform;
            var cardFrame = FindDeep(layoutRoot, "CardFrame") as RectTransform;
            var headerGroup = FindDeep(layoutRoot, "HeaderGroup") as RectTransform;
            var headerPlate = FindDeep(layoutRoot, "HeaderPlate") as RectTransform;
            var gameOverText = GetTmp(FindDeep(layoutRoot, "GameOverText"));
            var scoreGroup = FindDeep(layoutRoot, "ScoreGroup") as RectTransform;
            var finalScoreText = GetTmp(FindDeep(layoutRoot, "FinalScoreText"));
            var newBestBanner = FindDeep(layoutRoot, "NewBestBanner") as RectTransform;
            var newBestText = GetTmp(FindDeep(layoutRoot, "NewBestText"));
            var bestScoreText = GetTmp(FindDeep(layoutRoot, "BestScoreText"));
            var statsGroup = FindDeep(layoutRoot, "StatsGroup") as RectTransform;
            var maxComboRow = FindDeep(layoutRoot, "MaxComboRow") as RectTransform;
            var totalLinesRow = FindDeep(layoutRoot, "TotalLinesRow") as RectTransform;
            var maxComboText = GetTmp(FindDeep(layoutRoot, "MaxComboValueText"));
            var totalLinesText = GetTmp(FindDeep(layoutRoot, "TotalLinesValueText"));
            var buttonsGroup = FindDeep(layoutRoot, "ButtonsGroup") as RectTransform;
            var mainMenuButton = FindDeep(layoutRoot, "MainMenuButton") as RectTransform;
            var restartButton = FindDeep(layoutRoot, "RestartButton") as RectTransform;
            var panelRoot = layoutRoot.parent;
            var continueOfferPanel = FindDeep(panelRoot, "ContinueOfferPanel") as RectTransform;
            var continueCard = FindDeep(panelRoot, "ContinueCard") as RectTransform;
            var noMovesText = GetTmp(FindDeep(panelRoot, "NoMovesText"));
            var continueCountdownText = GetTmp(FindDeep(panelRoot, "ContinueCountdownText"));
            var continueButton = FindDeep(panelRoot, "ContinueButton") as RectTransform;

            if (cardRoot != null)
                SetTransform(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(768f, 1428f), new Vector2(0f, -6f), ref changed);

            if (cardShadow != null)
            {
                SetTransform(cardShadow, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);
                cardShadow.localScale = new Vector3(1.04f, 1.045f, 1f);
                changed = true;
                StyleImage(cardShadow.gameObject, cardSprite, cardSprite != null ? new Color(0.05f, 0.08f, 0.2f, 0.8f) : new Color(0.05f, 0.03f, 0.18f, 0.86f), false, ref changed);
            }

            if (cardFrame != null)
            {
                SetTransform(cardFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);
                StyleImage(cardFrame.gameObject, cardSprite, cardColor, false, ref changed);
                EnsureShadow(cardFrame.gameObject, new Color(0.13f, 0.3f, 1f, 0.38f), new Vector2(0f, -18f), ref changed);
            }

            if (headerGroup != null)
                SetTransform(headerGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(664f, 152f), new Vector2(0f, -96f), ref changed);

            if (headerPlate != null)
            {
                SetTransform(headerPlate, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);
                StyleImage(headerPlate.gameObject, headerSprite, headerColor, false, ref changed);
                EnsureOutline(headerPlate.gameObject, new Color(0.41f, 0.62f, 1f, 0.28f), new Vector2(2f, -2f), ref changed);
            }

            if (gameOverText != null)
            {
                SetTransform(gameOverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(540f, 84f), new Vector2(0f, -2f), ref changed);
                StyleTmp(gameOverText, font, "OYUN B\u0130TT\u0130", 62f, new Color(1f, 0.86f, 0.24f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
                EnsureShadow(gameOverText.gameObject, new Color(0.55f, 0.14f, 0.02f, 0.9f), new Vector2(0f, -6f), ref changed);
                EnsureOutline(gameOverText.gameObject, new Color(0.78f, 0.3f, 0.02f, 0.82f), new Vector2(3f, -3f), ref changed);
                EnsureSparkle(headerGroup, "HeaderSparkleLeft", sparkleSprite, new Vector2(-278f, 0f), 34f, ref changed);
                EnsureSparkle(headerGroup, "HeaderSparkleRight", sparkleSprite, new Vector2(278f, 0f), 34f, ref changed);
            }

            if (scoreGroup != null)
                SetTransform(scoreGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(632f, 360f), new Vector2(0f, -336f), ref changed);

            if (finalScoreText != null)
            {
                SetTransform(finalScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 116f), new Vector2(0f, -20f), ref changed);
                StyleTmp(finalScoreText, font, "7,560", 108f, new Color(1f, 0.95f, 0.77f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
                EnsureShadow(finalScoreText.gameObject, new Color(0.38f, 0.15f, 0.04f, 0.85f), new Vector2(0f, -7f), ref changed);
                EnsureOutline(finalScoreText.gameObject, new Color(0.95f, 0.63f, 0.06f, 0.55f), new Vector2(2f, -2f), ref changed);
            }

            if (newBestBanner != null)
            {
                SetTransform(newBestBanner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(520f, 72f), new Vector2(0f, -154f), ref changed);
                StyleImage(newBestBanner.gameObject, null, Color.clear, false, ref changed);

                var starsDecor = FindDeep(newBestBanner, "StarsDecor");
                if (starsDecor != null && starsDecor.gameObject.activeSelf)
                {
                    starsDecor.gameObject.SetActive(false);
                    changed = true;
                }
            }

            if (newBestText != null)
            {
                SetTransform(newBestText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 54f), new Vector2(0f, 0f), ref changed);
                StyleTmp(newBestText, font, "YEN\u0130 REKOR!", 42f, new Color(1f, 0.84f, 0.28f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
                EnsureShadow(newBestText.gameObject, new Color(0.34f, 0.12f, 0.02f, 0.6f), new Vector2(0f, -3f), ref changed);
            }

            if (bestScoreText != null)
            {
                SetTransform(bestScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(460f, 46f), new Vector2(0f, -272f), ref changed);
                StyleTmp(bestScoreText, font, "En iyi skor: 0", 34f, new Color(0.98f, 0.92f, 0.82f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
                EnsureShadow(bestScoreText.gameObject, new Color(0.16f, 0.12f, 0.34f, 0.62f), new Vector2(0f, -4f), ref changed);
            }

            if (statsGroup != null)
            {
                SetTransform(statsGroup, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 244f), new Vector2(0f, -612f), ref changed);
                StyleImage(statsGroup.gameObject, statsSprite, statsColor, false, ref changed);
            }

            StyleStatRow(maxComboRow, maxComboText, font, "Maks combo", "11", "x1", new Vector2(0f, -56f), new Color(0.98f, 0.87f, 0.12f, 1f), ref changed);
            StyleStatRow(totalLinesRow, totalLinesText, font, "Toplam \u00e7izgi", "25", "III", new Vector2(0f, -138f), new Color(0.22f, 0.78f, 1f, 1f), ref changed);

            if (buttonsGroup != null)
                SetTransform(buttonsGroup, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(690f, 146f), new Vector2(0f, 110f), ref changed);

            StyleActionButton(mainMenuButton, font, blueButtonSprite, homeIconSprite, new Color(0.08f, 0.62f, 1f, 1f), "ANA MEN\u00dc", new Vector2(-156f, 0f), new Vector2(308f, 112f), new Color(0.03f, 0.18f, 0.54f, 1f), true, ref changed);
            StyleActionButton(restartButton, font, yellowButtonSprite, restartIconSprite, new Color(1f, 0.74f, 0.08f, 1f), "TEKRAR OYNA", new Vector2(156f, 0f), new Vector2(340f, 112f), new Color(0.68f, 0.28f, 0.02f, 1f), true, ref changed);

            if (continueOfferPanel != null)
                StyleImage(continueOfferPanel.gameObject, null, new Color(0.03f, 0.05f, 0.14f, 0.72f), false, ref changed);

            if (continueCard != null)
            {
                SetTransform(continueCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720f, 336f), Vector2.zero, ref changed);
                StyleImage(continueCard.gameObject, cardSprite, cardColor, false, ref changed);
                EnsureShadow(continueCard.gameObject, new Color(0.03f, 0.05f, 0.18f, 0.45f), new Vector2(0f, -16f), ref changed);
            }

            if (noMovesText != null)
            {
                StyleTmp(noMovesText, font, "Hamle kalmad\u0131!", 44f, new Color(1f, 0.95f, 0.78f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
                EnsureShadow(noMovesText.gameObject, new Color(0.22f, 0.12f, 0.34f, 0.7f), new Vector2(0f, -4f), ref changed);
            }

            if (continueCountdownText != null)
                StyleTmp(continueCountdownText, font, "Devam i\u00e7in: 5", 30f, new Color(0.88f, 0.93f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);

            StyleActionButton(continueButton, font, yellowButtonSprite, playIconSprite, new Color(1f, 0.74f, 0.08f, 1f), "DEVAM ET (REKLAM)", new Vector2(0f, -82f), new Vector2(410f, 110f), new Color(0.68f, 0.28f, 0.02f, 1f), false, ref changed);
        }

        private static void StyleStatRow(RectTransform row, TextMeshProUGUI valueText, TMP_FontAsset font, string label, string sampleValue, string badgeGlyph, Vector2 pos, Color badgeColor, ref bool changed)
        {
            if (row == null)
                return;

            SetTransform(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 74f), pos, ref changed);

            Image rowImage = row.GetComponent<Image>();
            if (rowImage == null)
            {
                rowImage = row.gameObject.AddComponent<Image>();
                changed = true;
            }

            rowImage.sprite = null;
            rowImage.color = new Color(1f, 1f, 1f, 0f);
            rowImage.raycastTarget = false;

            var badge = EnsureChildRect(row, row, "Badge", ref changed);
            SetTransform(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48f, 48f), new Vector2(26f, 0f), ref changed);
            StyleImage(badge.gameObject, null, Color.clear, false, ref changed);

            var badgeText = EnsureText(EnsureChildRect(badge, row, "BadgeText", ref changed).gameObject, font, badgeGlyph, ref changed);
            SetTransform(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);
            StyleTmp(badgeText, font, badgeGlyph, 30f, badgeColor, TextAlignmentOptions.Center, FontStyles.Bold, ref changed);

            var labelText = EnsureText(EnsureChildRect(row, row, "LabelText", ref changed).gameObject, font, label, ref changed);
            SetTransform(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(310f, 40f), new Vector2(70f, 0f), ref changed);
            StyleTmp(labelText, font, label, 31f, new Color(0.96f, 0.93f, 0.86f, 1f), TextAlignmentOptions.Left, FontStyles.Bold, ref changed);

            if (valueText != null)
            {
                SetTransform(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(120f, 42f), new Vector2(-14f, 0f), ref changed);
                StyleTmp(valueText, font, sampleValue, 35f, new Color(1f, 0.95f, 0.82f, 1f), TextAlignmentOptions.Right, FontStyles.Bold, ref changed);
            }

            EnsureDivider(row, new Vector2(0f, -34f), ref changed);
        }

        private static void StyleActionButton(RectTransform buttonRect, TMP_FontAsset font, Sprite bodySprite, Sprite iconSprite, Color bodyColor, string label, Vector2 pos, Vector2 size, Color textColor, bool addGlow, ref bool changed)
        {
            if (buttonRect == null)
                return;

            SetTransform(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, pos, ref changed);
            StyleImage(buttonRect.gameObject, bodySprite, bodyColor, false, ref changed);

            if (addGlow)
                EnsureShadow(buttonRect.gameObject, new Color(0.19f, 0.6f, 1f, 0.28f), new Vector2(0f, -10f), ref changed);

            var iconAnchor = EnsureChildRect(buttonRect, buttonRect, "IconAnchor", ref changed);
            SetTransform(iconAnchor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 70f), new Vector2(46f, 0f), ref changed);
            StyleImage(iconAnchor.gameObject, null, new Color(1f, 1f, 1f, 0.14f), false, ref changed);

            var iconImage = EnsureChildRect(iconAnchor, buttonRect, "IconImage", ref changed);
            SetTransform(iconImage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f), Vector2.zero, ref changed);
            RemoveTextComponent(iconImage.gameObject, ref changed);
            StyleImage(iconImage.gameObject, iconSprite, Color.white, true, ref changed);

            var labelText = EnsureText(EnsureChildRect(buttonRect, buttonRect, "LabelText", ref changed).gameObject, font, label, ref changed);
            SetTransform(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.x - 132f, 50f), new Vector2(24f, 0f), ref changed);
            StyleTmp(labelText, font, label, 34f, textColor, TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
            EnsureShadow(labelText.gameObject, new Color(1f, 1f, 1f, 0.28f), new Vector2(0f, 2f), ref changed);
        }

        private static bool NeedsThemeRefresh(RectTransform layoutRoot)
        {
            return IsPlaceholderGraphic(FindDeep(layoutRoot, "CardFrame")) ||
                   IsPlaceholderGraphic(FindDeep(layoutRoot, "HeaderPlate")) ||
                   IsPlaceholderGraphic(FindDeep(layoutRoot, "StatsGroup")) ||
                   IsPlaceholderGraphic(FindDeep(layoutRoot, "MainMenuButton")) ||
                   IsPlaceholderGraphic(FindDeep(layoutRoot, "RestartButton")) ||
                   IsPlaceholderGraphic(FindDeep(layoutRoot.parent, "ContinueCard")) ||
                   ButtonNeedsRefresh(FindDeep(layoutRoot, "MainMenuButton") as RectTransform) ||
                   ButtonNeedsRefresh(FindDeep(layoutRoot, "RestartButton") as RectTransform) ||
                   ButtonNeedsRefresh(FindDeep(layoutRoot.parent, "ContinueButton") as RectTransform);
        }

        private static Transform FindAnyThemeMarker(RectTransform layoutRoot)
        {
            if (layoutRoot == null)
                return null;

            for (int i = 0; i < layoutRoot.childCount; i++)
            {
                var child = layoutRoot.GetChild(i);
                if (child != null && child.name.StartsWith("GameOverThemeAppliedV", StringComparison.Ordinal))
                    return child;
            }

            return null;
        }

        private static void RepairThemeCopy(RectTransform layoutRoot, ref bool changed)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThemeFontPath) ?? TMP_Settings.defaultFontAsset;
            Sprite cardSprite = LoadSprite(CardSpritePath);
            Sprite headerSprite = LoadSprite(HeaderSpritePath);
            Sprite statsSprite = LoadSprite(StatsSpritePath);
            Sprite blueButtonSprite = LoadSprite(BlueButtonSpritePath);
            Sprite yellowButtonSprite = LoadSprite(YellowButtonSpritePath);
            Sprite homeIconSprite = LoadSprite(HomeIconSpritePath);
            Sprite restartIconSprite = LoadSprite(RestartIconSpritePath);
            Sprite playIconSprite = LoadSprite(PlayIconSpritePath);

            RepairText(GetTmp(FindDeep(layoutRoot, "GameOverText")), font, "OYUN B\u0130TT\u0130", 62f, new Color(1f, 0.86f, 0.24f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
            RepairText(GetTmp(FindDeep(layoutRoot, "NewBestText")), font, "YEN\u0130 REKOR!", 48f, new Color(0.46f, 0.17f, 0.02f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
            RepairText(GetTmp(FindDeep(layoutRoot, "BestScoreText")), font, "En iyi skor: 0", 36f, new Color(0.98f, 0.92f, 0.82f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);

            RepairImage(FindDeep(layoutRoot, "CardShadow")?.gameObject, cardSprite, cardSprite != null ? new Color(0.05f, 0.08f, 0.2f, 0.8f) : new Color(0.05f, 0.03f, 0.18f, 0.86f), ref changed);
            RepairImage(FindDeep(layoutRoot, "CardFrame")?.gameObject, cardSprite, cardSprite != null ? Color.white : new Color(0.2f, 0.23f, 0.74f, 1f), ref changed);
            RepairImage(FindDeep(layoutRoot, "HeaderPlate")?.gameObject, headerSprite, headerSprite != null ? Color.white : new Color(0.12f, 0.18f, 0.56f, 1f), ref changed);
            StyleImage(FindDeep(layoutRoot, "NewBestBanner")?.gameObject, null, Color.clear, false, ref changed);
            var legacyStarsDecor = FindDeep(layoutRoot, "StarsDecor");
            if (legacyStarsDecor != null && legacyStarsDecor.gameObject.activeSelf)
            {
                legacyStarsDecor.gameObject.SetActive(false);
                changed = true;
            }
            RepairImage(FindDeep(layoutRoot, "StatsGroup")?.gameObject, statsSprite, statsSprite != null ? Color.white : new Color(0.12f, 0.16f, 0.46f, 0.96f), ref changed);
            RepairImage(FindDeep(layoutRoot.parent, "ContinueOfferPanel")?.gameObject, null, new Color(0.03f, 0.05f, 0.14f, 0.72f), ref changed);
            RepairImage(FindDeep(layoutRoot.parent, "ContinueCard")?.gameObject, cardSprite, cardSprite != null ? Color.white : new Color(0.18f, 0.22f, 0.68f, 1f), ref changed);
            RepairText(GetTmp(FindDeep(layoutRoot.parent, "NoMovesText")), font, "Hamle kalmad\u0131!", 44f, new Color(1f, 0.95f, 0.78f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
            RepairText(GetTmp(FindDeep(layoutRoot.parent, "ContinueCountdownText")), font, "Devam etmek i\u00e7in: 5", 30f, new Color(0.88f, 0.93f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, ref changed);

            RepairButtonVisuals(FindDeep(layoutRoot, "MainMenuButton") as RectTransform, font, blueButtonSprite, homeIconSprite, new Color(0.08f, 0.62f, 1f, 1f), "ANA MEN\u00dc", new Color(0.03f, 0.18f, 0.54f, 1f), ref changed);
            RepairButtonVisuals(FindDeep(layoutRoot, "RestartButton") as RectTransform, font, yellowButtonSprite, restartIconSprite, new Color(1f, 0.74f, 0.08f, 1f), "TEKRAR OYNA", new Color(0.68f, 0.28f, 0.02f, 1f), ref changed);
            RepairButtonVisuals(FindDeep(layoutRoot.parent, "ContinueButton") as RectTransform, font, yellowButtonSprite, playIconSprite, new Color(1f, 0.74f, 0.08f, 1f), "DEVAM ET (REKLAM)", new Color(0.68f, 0.28f, 0.02f, 1f), ref changed);
        }

        private static bool IsPlaceholderGraphic(Transform transform)
        {
            var image = transform != null ? transform.GetComponent<Image>() : null;
            if (image == null)
                return false;

            Color color = image.color;
            return image.sprite == null &&
                   color.a > 0.9f &&
                   color.r > 0.9f &&
                   color.g > 0.9f &&
                   color.b > 0.9f;
        }

        private static void RepairImage(GameObject go, Sprite sprite, Color fallbackColor, ref bool changed)
        {
            if (go == null)
                return;

            var image = go.GetComponent<Image>();
            if (image == null)
                return;

            if (image.sprite == null || IsNearWhite(image.color))
                StyleImage(go, sprite, fallbackColor, false, ref changed);
        }

        private static void RepairText(TextMeshProUGUI text, TMP_FontAsset font, string value, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style, ref bool changed)
        {
            if (text == null)
                return;

            if (string.IsNullOrWhiteSpace(text.text) || IsNearWhite(text.color))
                StyleTmp(text, font, value, fontSize, color, alignment, style, ref changed);
        }

        private static void RepairButtonVisuals(RectTransform buttonRect, TMP_FontAsset font, Sprite bodySprite, Sprite iconSprite, Color bodyColor, string label, Color textColor, ref bool changed)
        {
            if (buttonRect == null)
                return;

            RepairImage(buttonRect.gameObject, bodySprite, bodyColor, ref changed);
            RepairImage(buttonRect.Find("IconAnchor")?.gameObject, null, new Color(1f, 1f, 1f, 0.18f), ref changed);
            RemoveTextComponent(buttonRect.Find("IconAnchor/IconImage")?.gameObject, ref changed);
            RepairImage(buttonRect.Find("IconAnchor/IconImage")?.gameObject, iconSprite, Color.white, ref changed);
            RepairText(GetTmp(FindDeep(buttonRect, "LabelText")), font, label, 36f, textColor, TextAlignmentOptions.Center, FontStyles.Bold, ref changed);
        }

        private static bool ButtonNeedsRefresh(RectTransform buttonRect)
        {
            if (buttonRect == null)
                return false;

            var iconImage = buttonRect.Find("IconAnchor/IconImage");
            var iconGraphic = iconImage != null ? iconImage.GetComponent<Image>() : null;
            return iconGraphic == null || iconGraphic.sprite == null || IsNearWhite(iconGraphic.color) || iconImage.GetComponent<TextMeshProUGUI>() != null;
        }

        private static bool IsNearWhite(Color color)
        {
            return color.a > 0.2f && color.r > 0.9f && color.g > 0.9f && color.b > 0.9f;
        }

        private static void EnsureSparkle(RectTransform parent, string name, Sprite sprite, Vector2 pos, float size, ref bool changed)
        {
            if (parent == null)
                return;

            var sparkle = EnsureChildRect(parent, parent, name, ref changed);
            SetTransform(sparkle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size, size), pos, ref changed);
            StyleImage(sparkle.gameObject, sprite, Color.white, false, ref changed);
        }

        private static void EnsureImageChild(RectTransform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color, ref bool changed)
        {
            if (parent == null)
                return;

            var child = EnsureChildRect(parent, parent, name, ref changed);
            SetTransform(child, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), size, pos, ref changed);
            StyleImage(child.gameObject, sprite, color, false, ref changed);
        }

        private static void EnsureDivider(RectTransform parent, Vector2 pos, ref bool changed)
        {
            if (parent == null)
                return;

            var divider = EnsureChildRect(parent, parent, "Divider", ref changed);
            SetTransform(divider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(494f, 2f), pos, ref changed);
            StyleImage(divider.gameObject, null, new Color(1f, 1f, 1f, 0.14f), false, ref changed);
        }

        private static void StyleImage(GameObject go, Sprite sprite, Color color, bool preserveAspect, ref bool changed)
        {
            if (go == null)
                return;

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                changed = true;
            }

            if (image.sprite != sprite)
            {
                image.sprite = sprite;
                changed = true;
            }

            if (image.color != color)
            {
                image.color = color;
                changed = true;
            }

            if (image.preserveAspect != preserveAspect)
            {
                image.preserveAspect = preserveAspect;
                changed = true;
            }

            if (image.raycastTarget)
            {
                image.raycastTarget = false;
                changed = true;
            }

            var desiredType = sprite != null && HasSpriteBorder(sprite) ? Image.Type.Sliced : Image.Type.Simple;
            if (image.type != desiredType)
            {
                image.type = desiredType;
                changed = true;
            }
        }

        private static void StyleTmp(TextMeshProUGUI text, TMP_FontAsset font, string value, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style, ref bool changed)
        {
            if (text == null)
                return;

            if (font != null && text.font != font)
            {
                text.font = font;
                changed = true;
            }

            if (text.text != value)
            {
                text.text = value;
                changed = true;
            }

            if (!Mathf.Approximately(text.fontSize, fontSize))
            {
                text.fontSize = fontSize;
                changed = true;
            }

            if (text.color != color)
            {
                text.color = color;
                changed = true;
            }

            if (text.alignment != alignment)
            {
                text.alignment = alignment;
                changed = true;
            }

            if (text.fontStyle != style)
            {
                text.fontStyle = style;
                changed = true;
            }

            if (text.textWrappingMode != TextWrappingModes.NoWrap)
            {
                text.textWrappingMode = TextWrappingModes.NoWrap;
                changed = true;
            }

            if (text.overflowMode != TextOverflowModes.Overflow)
            {
                text.overflowMode = TextOverflowModes.Overflow;
                changed = true;
            }

            if (text.raycastTarget)
            {
                text.raycastTarget = false;
                changed = true;
            }
        }

        private static void EnsureShadow(GameObject go, Color color, Vector2 distance, ref bool changed)
        {
            if (go == null)
                return;

            var shadow = go.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = go.AddComponent<Shadow>();
                changed = true;
            }

            if (shadow.effectColor != color)
            {
                shadow.effectColor = color;
                changed = true;
            }

            if (shadow.effectDistance != distance)
            {
                shadow.effectDistance = distance;
                changed = true;
            }

            if (!shadow.useGraphicAlpha)
            {
                shadow.useGraphicAlpha = true;
                changed = true;
            }
        }

        private static void EnsureOutline(GameObject go, Color color, Vector2 distance, ref bool changed)
        {
            if (go == null)
                return;

            var outline = go.GetComponent<Outline>();
            if (outline == null)
            {
                outline = go.AddComponent<Outline>();
                changed = true;
            }

            if (outline.effectColor != color)
            {
                outline.effectColor = color;
                changed = true;
            }

            if (outline.effectDistance != distance)
            {
                outline.effectDistance = distance;
                changed = true;
            }

            if (!outline.useGraphicAlpha)
            {
                outline.useGraphicAlpha = true;
                changed = true;
            }
        }

        private static TextMeshProUGUI GetTmp(Transform transform)
        {
            return transform != null ? transform.GetComponent<TextMeshProUGUI>() : null;
        }

        private static void SetTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, ref bool changed)
        {
            if (rect == null)
                return;

            if (rect.anchorMin != anchorMin || rect.anchorMax != anchorMax || rect.pivot != pivot ||
                rect.sizeDelta != size || rect.anchoredPosition != anchoredPosition || rect.localScale != Vector3.one)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = size;
                rect.anchoredPosition = anchoredPosition;
                rect.localScale = Vector3.one;
                changed = true;
            }
        }

        private static Sprite LoadSprite(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                return sprite;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite spriteAsset)
                    return spriteAsset;
            }

            return null;
        }

        private static bool HasSpriteBorder(Sprite sprite)
        {
            return sprite != null && sprite.border.sqrMagnitude > 0.01f;
        }

        private static RectTransform EnsureStatRow(Transform parent, Transform searchRoot, TMP_FontAsset font, string rowName, string valueName, string label, string badge, Vector2 pos, ref bool changed)
        {
            RectTransform row = EnsureChildRect(parent, searchRoot, rowName, ref changed);
            SetRectIfUnset(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(690f, 86f), pos, ref changed);
            EnsureImage(row.gameObject, false, ref changed);

            RectTransform badgeRoot = EnsureChildRect(row, searchRoot, "Badge", ref changed);
            SetRectIfUnset(badgeRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), new Vector2(40f, 0f), ref changed);
            EnsureImage(badgeRoot.gameObject, false, ref changed);
            var badgeText = EnsureText(EnsureChildRect(badgeRoot, searchRoot, "BadgeText", ref changed).gameObject, font, badge, ref changed);
            SetRectIfUnset(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);

            var labelText = EnsureText(EnsureChildRect(row, searchRoot, "LabelText", ref changed).gameObject, font, label, ref changed);
            SetRectIfUnset(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(390f, 42f), new Vector2(92f, 0f), ref changed);

            var valueText = EnsureText(EnsureChildRect(row, searchRoot, valueName, ref changed).gameObject, font, "0", ref changed);
            SetRectIfUnset(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(160f, 42f), new Vector2(-24f, 0f), ref changed);
            valueText.alignment = TextAlignmentOptions.Right;
            return row;
        }

        private static Button EnsureButton(Transform parent, Transform searchRoot, string name, string label, TMP_FontAsset font, Vector2 pos, Vector2 size, ref bool changed, bool createIconPlaceholder = false)
        {
            RectTransform rect = EnsureChildRect(parent, searchRoot, name, ref changed);
            SetRectIfUnset(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, pos, ref changed);
            EnsureImage(rect.gameObject, false, ref changed);

            var button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
                changed = true;
            }

            if (button.targetGraphic == null)
                button.targetGraphic = rect.GetComponent<Graphic>();

            var labelText = EnsureText(EnsureChildRect(rect, searchRoot, "LabelText", ref changed).gameObject, font, label, ref changed);
            SetRectIfUnset(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.x - (createIconPlaceholder ? 126f : 48f), 54f), new Vector2(createIconPlaceholder ? 26f : 0f, 0f), ref changed);

            if (createIconPlaceholder)
            {
                RectTransform iconAnchor = EnsureChildRect(rect, searchRoot, "IconAnchor", ref changed);
                SetRectIfUnset(iconAnchor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), new Vector2(46f, 0f), ref changed);

                RectTransform iconImage = EnsureChildRect(iconAnchor, searchRoot, "IconImage", ref changed);
                SetRectIfUnset(iconImage, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ref changed);
                EnsureImage(iconImage.gameObject, false, ref changed);
            }

            return button;
        }

        private static RectTransform EnsureChildRect(Transform parent, Transform searchRoot, string name, ref bool changed, string legacyName = null)
        {
            RectTransform rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = FindDeep(searchRoot, name) as RectTransform;
            }

            if (rect == null && !string.IsNullOrWhiteSpace(legacyName))
            {
                rect = FindDeep(searchRoot, legacyName) as RectTransform;
                if (rect != null && rect.name != name)
                {
                    rect.name = name;
                    changed = true;
                }
            }

            if (rect == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                rect = go.GetComponent<RectTransform>();
                changed = true;
            }
            else if (rect.parent != parent)
            {
                rect.SetParent(parent, true);
                changed = true;
            }

            if (!rect.gameObject.activeSelf)
            {
                rect.gameObject.SetActive(true);
                changed = true;
            }

            return rect;
        }

        private static Image EnsureImage(GameObject go, bool stretch, ref bool changed, bool raycastTarget = false)
        {
            if (go == null)
                return null;

            var image = go.GetComponent<Image>();
            bool created = false;
            if (image == null)
            {
                image = go.AddComponent<Image>();
                changed = true;
                created = true;
            }

            if (created || image.raycastTarget != raycastTarget)
                image.raycastTarget = raycastTarget;

            if (stretch)
                ConfigureStretch(go.GetComponent<RectTransform>());

            return image;
        }

        private static TextMeshProUGUI EnsureText(GameObject go, TMP_FontAsset font, string value, ref bool changed)
        {
            if (go == null)
                return null;

            if (!go.activeSelf)
            {
                go.SetActive(true);
                changed = true;
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            bool created = false;
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
                if (text == null)
                    return null;

                changed = true;
                created = true;
            }

            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            if (font != null && text.font == null)
                text.font = font;

            if (string.IsNullOrWhiteSpace(text.text))
                text.text = value;

            text.raycastTarget = false;

            if (created)
            {
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Center;
            }

            return text;
        }

        private static void RemoveTextComponent(GameObject go, ref bool changed)
        {
            if (go == null)
                return;

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;

            UnityEngine.Object.DestroyImmediate(text);
            changed = true;
        }

        private static void WireGameOverView(
            BlockPuzzle.UnityAdapter.UI.GameOverView view,
            GameObject panel,
            TextMeshProUGUI finalScoreText,
            TextMeshProUGUI bestScoreText,
            TextMeshProUGUI newBestText,
            TextMeshProUGUI sessionSummaryText,
            RectTransform bestMoveRow,
            RectTransform maxComboRow,
            RectTransform totalLinesRow,
            RectTransform averageMoveRow,
            Button restartButton,
            Button mainMenuButton,
            GameObject continueOfferPanel,
            TextMeshProUGUI noMovesText,
            TextMeshProUGUI continueCountdownText,
            Button continueButton,
            ref bool changed)
        {
            if (view == null)
                return;

            var so = new SerializedObject(view);
            so.UpdateIfRequiredOrScript();
            changed |= SetObjectReference(so, "gameOverPanel", panel);
            changed |= SetObjectReference(so, "finalScoreText", finalScoreText);
            changed |= SetObjectReference(so, "bestScoreText", bestScoreText);
            changed |= SetObjectReference(so, "newBestText", newBestText);
            changed |= SetObjectReference(so, "sessionSummaryText", sessionSummaryText);
            changed |= SetObjectReference(so, "bestMoveValueText", FindDeep(bestMoveRow, "BestMoveValueText")?.GetComponent<TextMeshProUGUI>());
            changed |= SetObjectReference(so, "maxComboValueText", FindDeep(maxComboRow, "MaxComboValueText")?.GetComponent<TextMeshProUGUI>());
            changed |= SetObjectReference(so, "totalLinesValueText", FindDeep(totalLinesRow, "TotalLinesValueText")?.GetComponent<TextMeshProUGUI>());
            changed |= SetObjectReference(so, "averageMoveValueText", FindDeep(averageMoveRow, "AverageMoveValueText")?.GetComponent<TextMeshProUGUI>());
            changed |= SetObjectReference(so, "restartButton", restartButton);
            changed |= SetObjectReference(so, "mainMenuButton", mainMenuButton);
            changed |= SetObjectReference(so, "continueOfferPanel", continueOfferPanel);
            changed |= SetObjectReference(so, "noMovesLabel", noMovesText);
            changed |= SetObjectReference(so, "continueCountdownText", continueCountdownText);
            changed |= SetObjectReference(so, "continueButton", continueButton);
            changed |= SetBool(so, "buildDedicatedSceneLayoutAtRuntime", false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject so, string propertyName, bool value)
        {
            var property = so.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
                return false;

            property.boolValue = value;
            return true;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindDeep(root.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static void ConfigureStretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureStretchIfNew(RectTransform rect, ref bool changed)
        {
            if (rect == null)
                return;

            bool needsStretch =
                rect.anchorMin != Vector2.zero ||
                rect.anchorMax != Vector2.one ||
                rect.offsetMin != Vector2.zero ||
                rect.offsetMax != Vector2.zero;

            if (!needsStretch)
                return;

            ConfigureStretch(rect);
            changed = true;
        }

        private static void SetRectIfUnset(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, ref bool changed)
        {
            if (rect == null)
                return;

            if (!LooksLikeFreshRect(rect))
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            changed = true;
        }

        private static bool LooksLikeFreshRect(RectTransform rect)
        {
            bool centeredAnchors =
                Approximately(rect.anchorMin, new Vector2(0.5f, 0.5f)) &&
                Approximately(rect.anchorMax, new Vector2(0.5f, 0.5f));

            bool zeroSized = Approximately(rect.sizeDelta, Vector2.zero);
            bool unityDefaultSized = Approximately(rect.sizeDelta, new Vector2(100f, 100f));
            bool centeredPosition = Approximately(rect.anchoredPosition, Vector2.zero);

            return centeredAnchors && centeredPosition && (zeroSized || unityDefaultSized);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
        }
    }
}
#endif
