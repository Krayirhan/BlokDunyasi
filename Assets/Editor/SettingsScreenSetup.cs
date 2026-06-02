#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BlokDunyasiTools
{
    public static class SettingsScreenSetup
    {
        private const string ScenePath = "Assets/Scenes/Settings.unity";
        private const string TitleFontPath = "Assets/Resources/TMP/LuckiestGuy-Regular Combo SDF.asset";
        private const string BodyFontPath = "Assets/Skyden_Games/Free_Casual_GUI/Demo/Fonts/Baloo/Baloo-Regular SDF.asset";
        private const string BackgroundPath = "Assets/Images/arkaplanb.png";
        private const string BlockSpritePath = "Assets/Images/FilledCellSprite_512.png";
        private const string LongBlueButtonPath = "Assets/Buttons/PNG/12Button_Long_Blue.png";
        private const string LongYellowButtonPath = "Assets/Buttons/PNG/15Button_Long_Yellow.png";
        private const string LongRedButtonPath = "Assets/Buttons/PNG/14Button_Long_Red.png";
        private const string MidBlueButtonPath = "Assets/Buttons/PNG/11Button_Midl_Blue.png";
        private const string MidYellowButtonPath = "Assets/Buttons/PNG/10Button_Midl_Yellow.png";
        private const string MidGreenButtonPath = "Assets/Buttons/PNG/22Button_Midl_Green.png";

        private sealed class ThemeAssets
        {
            public TMP_FontAsset TitleFont;
            public TMP_FontAsset BodyFont;
            public Sprite BackgroundSprite;
            public Sprite PanelSprite;
            public Sprite WhiteSprite;
            public Sprite BlockSprite;
            public Sprite LongBlueButton;
            public Sprite LongYellowButton;
            public Sprite LongRedButton;
            public Sprite MidBlueButton;
            public Sprite MidYellowButton;
            public Sprite MidGreenButton;
        }

        [MenuItem("BlokDunyasi/Setup/Create Settings Screen")]
        public static void CreateSettingsScreen()
        {
            ThemeAssets assets = LoadAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            EnsureEventSystem();
            EnsureMainCameraData();

            var canvasGo = new GameObject("SettingsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(global::SettingsManager));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.matchWidthOrHeight = 0.65f;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            Stretch(canvasRect);

            CreateFullscreenImage("BackgroundArt", canvasRect, assets.BackgroundSprite, Color.white, false);
            CreateFullscreenImage("BackgroundDim", canvasRect, assets.WhiteSprite, new Color(0.03f, 0.06f, 0.14f, 0.58f), false);
            CreateGlow("WarmGlow", canvasRect, assets.WhiteSprite, new Color(1f, 0.72f, 0.18f, 0.18f), new Vector2(-280f, 760f), new Vector2(720f, 720f));
            CreateGlow("CoolGlow", canvasRect, assets.WhiteSprite, new Color(0.18f, 0.74f, 1f, 0.18f), new Vector2(280f, -120f), new Vector2(920f, 920f));

            RectTransform safeRoot = CreateRect("__SettingsSafeAreaRoot", canvasRect);
            Stretch(safeRoot);
            safeRoot.gameObject.AddComponent<global::SafeAreaFitter>();

            CreateDecorBlock("DecorBlockLeft", safeRoot, assets, new Vector2(-392f, 692f), 168f, -12f, new Color(1f, 0.77f, 0.19f, 0.88f));
            CreateDecorBlock("DecorBlockRight", safeRoot, assets, new Vector2(386f, 628f), 132f, 14f, new Color(0.2f, 0.74f, 1f, 0.88f));
            CreateDecorBlock("DecorBlockLowerLeft", safeRoot, assets, new Vector2(-410f, -860f), 116f, 18f, new Color(0.25f, 0.92f, 0.58f, 0.82f));
            CreateDecorBlock("DecorBlockLowerRight", safeRoot, assets, new Vector2(398f, -892f), 160f, -10f, new Color(1f, 0.44f, 0.22f, 0.82f));

            RectTransform header = CreateHeader(safeRoot, assets);
            CreateWindowShell(safeRoot, assets, header, canvasRect);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SettingsScreenSetup] Settings scene rebuilt at " + ScenePath);
        }

        private static ThemeAssets LoadAssets()
        {
            ThemeAssets assets = new ThemeAssets();
            assets.TitleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
            assets.BodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath) ?? TMP_Settings.defaultFontAsset;
            assets.BackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            assets.BlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlockSpritePath);
            assets.LongBlueButton = AssetDatabase.LoadAssetAtPath<Sprite>(LongBlueButtonPath);
            assets.LongYellowButton = AssetDatabase.LoadAssetAtPath<Sprite>(LongYellowButtonPath);
            assets.LongRedButton = AssetDatabase.LoadAssetAtPath<Sprite>(LongRedButtonPath);
            assets.MidBlueButton = AssetDatabase.LoadAssetAtPath<Sprite>(MidBlueButtonPath);
            assets.MidYellowButton = AssetDatabase.LoadAssetAtPath<Sprite>(MidYellowButtonPath);
            assets.MidGreenButton = AssetDatabase.LoadAssetAtPath<Sprite>(MidGreenButtonPath);
            assets.PanelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            assets.WhiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return assets;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
                Object.DestroyImmediate(standaloneInputModule);
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void EnsureMainCameraData()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cameras.Length > 0)
                    mainCamera = cameras[0];
            }

            if (mainCamera == null || mainCamera.GetComponent<UniversalAdditionalCameraData>() != null)
                return;

            mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        private static RectTransform CreateHeader(RectTransform parent, ThemeAssets assets)
        {
            RectTransform header = CreateRect("Header", parent);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 240f);
            header.anchoredPosition = Vector2.zero;

            Image headerBacking = CreateImage("HeaderBacking", header, assets.PanelSprite, new Color(0.04f, 0.08f, 0.2f, 0.62f), false, true);
            Stretch(headerBacking.rectTransform, 14f, 14f, 16f, 0f);
            AddShadow(headerBacking.gameObject, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -6f));

            Button backButton = CreateActionButton("BackButton", header, assets.MidBlueButton, assets.BodyFont, new Vector2(236f, 88f), false, true);
            SetAnchor(backButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -34f), new Vector2(0f, 1f));
            SetButtonText(backButton.transform, "GERI", "Ana menuye don", assets.BodyFont, 34f, 18f);

            Image titlePlate = CreateImage("TitlePlate", header, assets.LongYellowButton, Color.white, false, false);
            RectTransform titlePlateRect = titlePlate.rectTransform;
            SetAnchor(titlePlateRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0.5f, 1f));
            titlePlateRect.sizeDelta = new Vector2(620f, 128f);
            AddShadow(titlePlate.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -8f));

            TextMeshProUGUI screenTitle = CreateText("ScreenTitleText", titlePlateRect, assets.TitleFont, "AYARLAR", 78f, new Color(0.14f, 0.1f, 0.02f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(screenTitle.rectTransform, 50f, 50f, 14f, 18f);

            TextMeshProUGUI screenSubtitle = CreateText("ScreenSubtitleText", header, assets.BodyFont, "Oyunu kendine gore ayarla", 28f, new Color(0.94f, 0.97f, 1f, 0.9f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchor(screenSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(0.5f, 1f));
            screenSubtitle.rectTransform.sizeDelta = new Vector2(640f, 44f);

            return header;
        }

        private static void CreateWindowShell(RectTransform safeRoot, ThemeAssets assets, RectTransform header, RectTransform canvasRect)
        {
            Image windowShell = CreateImage("WindowShell", safeRoot, assets.PanelSprite, new Color(0.05f, 0.11f, 0.26f, 0.86f), false, true);
            RectTransform windowShellRect = windowShell.rectTransform;
            windowShellRect.anchorMin = new Vector2(0f, 0f);
            windowShellRect.anchorMax = new Vector2(1f, 1f);
            windowShellRect.offsetMin = new Vector2(24f, 24f);
            windowShellRect.offsetMax = new Vector2(-24f, -216f);
            AddShadow(windowShell.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -12f));

            Image shellAccentBar = CreateImage("ShellAccentBar", windowShellRect, assets.WhiteSprite, new Color(0.14f, 0.68f, 1f, 0.94f), false, false);
            RectTransform shellAccentRect = shellAccentBar.rectTransform;
            shellAccentRect.anchorMin = new Vector2(0f, 1f);
            shellAccentRect.anchorMax = new Vector2(1f, 1f);
            shellAccentRect.pivot = new Vector2(0.5f, 1f);
            shellAccentRect.sizeDelta = new Vector2(0f, 10f);
            shellAccentRect.anchoredPosition = new Vector2(0f, -10f);

            CreateScrollView(windowShellRect, out RectTransform contentRect, assets);

            RectTransform audioCard = CreateCard(contentRect, "AudioCard", assets);
            RectTransform gameplayCard = CreateCard(contentRect, "GameplayCard", assets);
            RectTransform visualCard = CreateCard(contentRect, "VisualCard", assets);
            RectTransform languageCard = CreateCard(contentRect, "LanguageCard", assets);
            RectTransform notificationsCard = CreateCard(contentRect, "NotificationsCard", assets);
            RectTransform dataCard = CreateCard(contentRect, "DataCard", assets);
            RectTransform aboutCard = CreateCard(contentRect, "AboutCard", assets);

            CreateToggleRow(audioCard, assets, "MusicToggle");
            CreateSliderRow(audioCard, assets, "MusicSliderRow", "MusicSlider");
            CreateToggleRow(audioCard, assets, "SfxToggle");
            CreateSliderRow(audioCard, assets, "SfxSliderRow", "SfxSlider");
            CreateToggleRow(audioCard, assets, "VibrationToggle");

            CreateToggleRow(gameplayCard, assets, "PlacementPreviewToggle");
            CreateToggleRow(gameplayCard, assets, "GridHighlightToggle");
            CreateToggleRow(gameplayCard, assets, "ComboVisualToggle");
            CreateToggleRow(gameplayCard, assets, "AnimationsToggle");
            CreateToggleRow(gameplayCard, assets, "AutoSaveToggle");

            RectTransform themeRow = CreateButtonRow("ThemeButtonsRow", visualCard, 18f, 116f);
            CreateChoiceButton("ThemeClassicButton", themeRow, assets.MidYellowButton, assets);
            CreateChoiceButton("ThemeNightButton", themeRow, assets.MidBlueButton, assets);
            CreateChoiceButton("ThemeVividButton", themeRow, assets.MidGreenButton, assets);
            CreateToggleRow(visualCard, assets, "ReduceMotionToggle");
            CreateToggleRow(visualCard, assets, "HighContrastToggle");

            RectTransform languageRow = CreateButtonRow("LanguageChoiceRow", languageCard, 18f, 110f);
            CreateChoiceButton("LanguageTrButton", languageRow, assets.MidYellowButton, assets);
            CreateChoiceButton("LanguageEnButton", languageRow, assets.MidBlueButton, assets);

            CreateToggleRow(notificationsCard, assets, "DailyReminderToggle");
            CreateToggleRow(notificationsCard, assets, "NewFeaturesToggle");
            CreateToggleRow(notificationsCard, assets, "TipsToggle");

            CreateActionButton("ResetProgressButton", dataCard, assets.LongRedButton, assets.BodyFont, new Vector2(0f, 104f), true, false);
            CreateActionButton("ClearCacheButton", dataCard, assets.LongBlueButton, assets.BodyFont, new Vector2(0f, 104f), true, false);
            CreateActionButton("RestoreDefaultsButton", dataCard, assets.LongYellowButton, assets.BodyFont, new Vector2(0f, 104f), true, false);

            TextMeshProUGUI aboutSummary = CreateText("AboutSummaryText", aboutCard, assets.BodyFont, string.Empty, 26f, new Color(0.93f, 0.96f, 1f, 0.88f), FontStyles.Normal, TextAlignmentOptions.Left);
            aboutSummary.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement aboutSummaryLayout = aboutSummary.gameObject.AddComponent<LayoutElement>();
            aboutSummaryLayout.minHeight = 120f;
            aboutSummaryLayout.preferredHeight = 132f;

            TextMeshProUGUI versionText = CreateText("VersionText", aboutCard, assets.BodyFont, string.Empty, 24f, new Color(1f, 0.97f, 0.9f, 0.92f), FontStyles.Bold, TextAlignmentOptions.Left);
            versionText.rectTransform.sizeDelta = new Vector2(0f, 40f);

            CreateActionButton("PrivacyButton", aboutCard, assets.LongBlueButton, assets.BodyFont, new Vector2(0f, 94f), true, false);
            CreateActionButton("TermsButton", aboutCard, assets.LongBlueButton, assets.BodyFont, new Vector2(0f, 94f), true, false);
            CreateActionButton("CreditsButton", aboutCard, assets.LongBlueButton, assets.BodyFont, new Vector2(0f, 94f), true, false);

            CreateConfirmDialog(canvasRect, assets);
        }

        private static ScrollRect CreateScrollView(RectTransform parent, out RectTransform contentRect, ThemeAssets assets)
        {
            RectTransform scrollRectTransform = CreateRect("ScrollView", parent);
            Stretch(scrollRectTransform, 24f, 24f, 34f, 26f);
            Image scrollImage = scrollRectTransform.gameObject.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = false;

            ScrollRect scrollRect = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;

            RectTransform viewport = CreateRect("Viewport", scrollRectTransform);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            contentRect = CreateRect("Content", viewport);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 18f;
            contentLayout.padding = new RectOffset(8, 8, 8, 28);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = contentRect;
            return scrollRect;
        }

        private static RectTransform CreateCard(Transform parent, string name, ThemeAssets assets)
        {
            RectTransform card = CreateRect(name, parent);
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = assets.PanelSprite;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.06f, 0.13f, 0.3f, 0.88f);
            AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -8f));

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(24, 24, 0, 24);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layoutElement = card.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120f;

            Image accentBar = CreateImage("AccentBar", card, assets.WhiteSprite, new Color(0.14f, 0.68f, 1f, 0.94f), false, false);
            LayoutElement accentLayout = accentBar.gameObject.AddComponent<LayoutElement>();
            accentLayout.minHeight = 10f;
            accentLayout.preferredHeight = 10f;

            TextMeshProUGUI title = CreateText("CardTitleText", card, assets.BodyFont, name, 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 42f;
            titleLayout.preferredHeight = 42f;

            TextMeshProUGUI subtitle = CreateText("CardSubtitleText", card, assets.BodyFont, string.Empty, 22f, new Color(0.93f, 0.96f, 1f, 0.78f), FontStyles.Normal, TextAlignmentOptions.Left);
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement subtitleLayout = subtitle.gameObject.AddComponent<LayoutElement>();
            subtitleLayout.minHeight = 44f;
            subtitleLayout.preferredHeight = 58f;

            return card;
        }

        private static Toggle CreateToggleRow(Transform parent, ThemeAssets assets, string name)
        {
            RectTransform row = CreateRect(name, parent);
            Image rowImage = row.gameObject.AddComponent<Image>();
            rowImage.sprite = assets.PanelSprite;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(1f, 1f, 1f, 0.1f);

            LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 100f;
            layoutElement.preferredHeight = 100f;

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Toggle toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = rowImage;
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.graphic = null;
            toggle.isOn = true;

            RectTransform textColumn = CreateRect("TextColumn", row);
            LayoutElement textColumnLayout = textColumn.gameObject.AddComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;
            textColumnLayout.minHeight = 64f;
            VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            CreateText("TitleText", textColumn, assets.BodyFont, string.Empty, 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            TextMeshProUGUI subtitle = CreateText("SubtitleText", textColumn, assets.BodyFont, string.Empty, 20f, new Color(0.93f, 0.96f, 1f, 0.74f), FontStyles.Normal, TextAlignmentOptions.Left);
            subtitle.textWrappingMode = TextWrappingModes.Normal;

            RectTransform switchRoot = CreateRect("Switch", row);
            LayoutElement switchLayout = switchRoot.gameObject.AddComponent<LayoutElement>();
            switchLayout.preferredWidth = 208f;
            switchLayout.minWidth = 208f;
            switchLayout.preferredHeight = 56f;
            switchLayout.minHeight = 56f;

            Image track = CreateImage("Track", switchRoot, assets.PanelSprite, new Color(0.18f, 0.28f, 0.46f, 1f), false, true);
            Stretch(track.rectTransform);

            TextMeshProUGUI valueText = CreateText("ValueText", switchRoot, assets.BodyFont, "ACIK", 22f, new Color(1f, 0.98f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(valueText.rectTransform, 38f, 38f, 0f, 0f);

            Image knob = CreateImage("Knob", switchRoot, assets.BlockSprite != null ? assets.BlockSprite : assets.WhiteSprite, Color.white, false, false);
            RectTransform knobRect = knob.rectTransform;
            SetAnchor(knobRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 0f), new Vector2(0.5f, 0.5f));
            knobRect.sizeDelta = new Vector2(46f, 46f);

            return toggle;
        }

        private static Slider CreateSliderRow(Transform parent, ThemeAssets assets, string rowName, string sliderName)
        {
            RectTransform row = CreateRect(rowName, parent);
            Image rowImage = row.gameObject.AddComponent<Image>();
            rowImage.sprite = assets.PanelSprite;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(1f, 1f, 1f, 0.1f);

            LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 142f;
            layoutElement.preferredHeight = 142f;

            VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(20, 20, 16, 18);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform headerRow = CreateRect("HeaderRow", row);
            LayoutElement headerLayout = headerRow.gameObject.AddComponent<LayoutElement>();
            headerLayout.minHeight = 52f;
            HorizontalLayoutGroup headerGroup = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerGroup.spacing = 12f;
            headerGroup.childAlignment = TextAnchor.MiddleLeft;
            headerGroup.childControlWidth = true;
            headerGroup.childControlHeight = true;
            headerGroup.childForceExpandWidth = false;
            headerGroup.childForceExpandHeight = false;

            RectTransform titleColumn = CreateRect("TitleColumn", headerRow);
            LayoutElement titleColumnLayout = titleColumn.gameObject.AddComponent<LayoutElement>();
            titleColumnLayout.flexibleWidth = 1f;
            VerticalLayoutGroup titleLayout = titleColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 2f;
            titleLayout.childAlignment = TextAnchor.UpperLeft;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            CreateText("TitleText", titleColumn, assets.BodyFont, string.Empty, 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            CreateText("SubtitleText", titleColumn, assets.BodyFont, string.Empty, 20f, new Color(0.93f, 0.96f, 1f, 0.74f), FontStyles.Normal, TextAlignmentOptions.Left);
            CreateText("ValueText", headerRow, assets.BodyFont, "100%", 26f, new Color(1f, 0.98f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Right);

            RectTransform sliderRect = CreateRect(sliderName, row);
            LayoutElement sliderLayout = sliderRect.gameObject.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 34f;
            sliderLayout.preferredHeight = 34f;

            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            Image sliderBackground = CreateImage("Background", sliderRect, assets.PanelSprite, new Color(0.18f, 0.28f, 0.46f, 0.95f), false, true);
            Stretch(sliderBackground.rectTransform);

            RectTransform fillArea = CreateRect("Fill Area", sliderRect);
            Stretch(fillArea, 16f, 16f, 6f, 6f);
            fillArea.offsetMax = new Vector2(-32f, -6f);

            Image fill = CreateImage("Fill", fillArea, assets.WhiteSprite, new Color(0.15f, 0.78f, 1f, 1f), false, false);
            Stretch(fill.rectTransform);

            RectTransform handleSlideArea = CreateRect("Handle Slide Area", sliderRect);
            Stretch(handleSlideArea, 12f, 12f, 0f, 0f);

            Image handle = CreateImage("Handle", handleSlideArea, assets.BlockSprite != null ? assets.BlockSprite : assets.WhiteSprite, Color.white, false, false);
            RectTransform handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(48f, 48f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            return slider;
        }

        private static RectTransform CreateButtonRow(string name, Transform parent, float spacing, float minHeight)
        {
            RectTransform row = CreateRect(name, parent);
            LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = minHeight;

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static Button CreateChoiceButton(string name, Transform parent, Sprite sprite, ThemeAssets assets)
        {
            Button button = CreateActionButton(name, parent, sprite, assets.BodyFont, new Vector2(0f, 114f), false, true);
            LayoutElement layout = button.gameObject.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.minHeight = 114f;
            layout.preferredHeight = 114f;
            return button;
        }

        private static Button CreateActionButton(string name, Transform parent, Sprite sprite, TMP_FontAsset font, Vector2 size, bool useLayoutHeight, bool addSelectionGlow)
        {
            RectTransform root = CreateRect(name, parent);
            if (size.x > 0f)
                root.sizeDelta = size;

            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            AddShadow(root.gameObject, new Color(0f, 0f, 0f, 0.2f), new Vector2(0f, -6f));

            if (useLayoutHeight)
            {
                LayoutElement layoutElement = root.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = size.y;
                layoutElement.preferredHeight = size.y;
            }

            if (addSelectionGlow)
            {
                Image glow = CreateImage("SelectionGlow", root, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), new Color(0f, 0f, 0f, 0f), false, false);
                Stretch(glow.rectTransform, -8f, -8f, -8f, -8f);
                glow.transform.SetAsFirstSibling();
            }

            RectTransform content = CreateRect("Content", root);
            Stretch(content, 22f, 22f, 12f, 14f);
            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 0f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            CreateText("LabelText", content, font, string.Empty, 30f, new Color(0.08f, 0.11f, 0.24f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            CreateText("SubtitleText", content, font, string.Empty, 18f, new Color(0.08f, 0.11f, 0.24f, 0.78f), FontStyles.Normal, TextAlignmentOptions.Center);
            return button;
        }

        private static void SetButtonText(Transform button, string title, string subtitle, TMP_FontAsset font, float titleSize, float subtitleSize)
        {
            TextMeshProUGUI label = FindText(button, "LabelText");
            TextMeshProUGUI sub = FindText(button, "SubtitleText");
            if (label != null)
            {
                label.text = title;
                label.font = font;
                label.fontSize = titleSize;
            }
            if (sub != null)
            {
                sub.text = subtitle;
                sub.font = font;
                sub.fontSize = subtitleSize;
            }
        }

        private static void CreateConfirmDialog(RectTransform parent, ThemeAssets assets)
        {
            RectTransform dialogRoot = CreateRect("ConfirmDialog", parent);
            Stretch(dialogRoot);

            CreateFullscreenImage("DialogDim", dialogRoot, assets.WhiteSprite, new Color(0f, 0f, 0f, 0.76f), true);

            Image panel = CreateImage("DialogPanel", dialogRoot, assets.PanelSprite, new Color(0.05f, 0.11f, 0.26f, 0.98f), true, true);
            RectTransform panelRect = panel.rectTransform;
            SetAnchor(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.5f, 0.5f));
            panelRect.sizeDelta = new Vector2(760f, 560f);
            AddShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -12f));

            TextMeshProUGUI title = CreateText("DialogTitleText", panelRect, assets.TitleFont, string.Empty, 56f, new Color(1f, 0.95f, 0.32f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.Normal;
            SetAnchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0.5f, 1f));
            title.rectTransform.sizeDelta = new Vector2(620f, 90f);

            TextMeshProUGUI message = CreateText("DialogMessageText", panelRect, assets.BodyFont, string.Empty, 28f, new Color(0.95f, 0.98f, 1f, 0.9f), FontStyles.Normal, TextAlignmentOptions.Center);
            message.textWrappingMode = TextWrappingModes.Normal;
            SetAnchor(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(0.5f, 0.5f));
            message.rectTransform.sizeDelta = new Vector2(620f, 180f);

            RectTransform buttonsRow = CreateButtonRow("DialogButtonsRow", panelRect, 18f, 108f);
            SetAnchor(buttonsRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(0.5f, 0f));
            buttonsRow.sizeDelta = new Vector2(640f, 108f);

            Button yesButton = CreateActionButton("ConfirmYesButton", buttonsRow, assets.LongYellowButton, assets.BodyFont, new Vector2(0f, 102f), false, false);
            Button noButton = CreateActionButton("ConfirmNoButton", buttonsRow, assets.LongBlueButton, assets.BodyFont, new Vector2(0f, 102f), false, false);
            LayoutElement yesLayout = yesButton.gameObject.AddComponent<LayoutElement>();
            yesLayout.minHeight = 102f;
            yesLayout.preferredHeight = 102f;
            yesLayout.flexibleWidth = 1f;
            LayoutElement noLayout = noButton.gameObject.AddComponent<LayoutElement>();
            noLayout.minHeight = 102f;
            noLayout.preferredHeight = 102f;
            noLayout.flexibleWidth = 1f;

            SetButtonText(yesButton.transform, "TAMAM", string.Empty, assets.BodyFont, 34f, 16f);
            SetButtonText(noButton.transform, "VAZGEC", string.Empty, assets.BodyFont, 34f, 16f);

            dialogRoot.gameObject.SetActive(false);
        }

        private static void CreateDecorBlock(string name, Transform parent, ThemeAssets assets, Vector2 position, float size, float rotation, Color color)
        {
            Image block = CreateImage(name, parent, assets.BlockSprite != null ? assets.BlockSprite : assets.WhiteSprite, color, false, false);
            RectTransform rect = block.rectTransform;
            SetAnchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void CreateGlow(string name, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            Image glow = CreateImage(name, parent, sprite, color, false, false);
            RectTransform rect = glow.rectTransform;
            SetAnchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(0.5f, 0.5f));
            rect.sizeDelta = size;
        }

        private static Image CreateFullscreenImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            Image image = CreateImage(name, parent, sprite, color, raycast, false);
            Stretch(image.rectTransform);
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget, bool sliced)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string text, float fontSize, Color color, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static TextMeshProUGUI FindText(Transform root, string name)
        {
            Transform child = FindTransform(root, name);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindTransform(root.GetChild(i), name);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
#endif
