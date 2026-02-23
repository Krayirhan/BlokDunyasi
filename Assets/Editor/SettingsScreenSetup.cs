// Editor script: creates a production-ready Settings scene with named UI elements
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlokDunyasiTools
{
    public static class SettingsScreenSetup
    {
        [MenuItem("BlokDunyasi/Setup/Create Settings Screen")]
        public static void CreateSettingsScreen()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Root Canvas
            var canvasGO = new GameObject("SettingsCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fixed Header
            var header = CreateUIObject("Header", canvasGO.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80));
            var headerImg = header.AddComponent<Image>(); headerImg.color = new Color(0,0,0,0.4f);
            var headerTitle = CreateText("Title", header.transform, "Ayarlar", 32, TextAnchor.MiddleCenter);
            headerTitle.rectTransform.anchoredPosition = new Vector2(0, -20);

            // Back button
            var backBtn = CreateButton("BackButton", header.transform, "←");
            backBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -20);

            // ScrollView
            var scrollView = new GameObject("ScrollView"); scrollView.transform.SetParent(canvasGO.transform, false);
            var svRect = scrollView.AddComponent<RectTransform>(); svRect.anchorMin = new Vector2(0, 0); svRect.anchorMax = new Vector2(1, 1); svRect.offsetMin = new Vector2(0, 0); svRect.offsetMax = new Vector2(0, -100);
            var scroll = scrollView.AddComponent<ScrollRect>(); scroll.vertical = true;
            var svImage = scrollView.AddComponent<Image>(); svImage.color = new Color(0,0,0,0);

            var viewport = CreateUIObject("Viewport", scrollView.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero);
            var mask = viewport.AddComponent<Mask>(); mask.showMaskGraphic = false; var vpImg = viewport.AddComponent<Image>(); vpImg.color = new Color(0,0,0,0);

            var content = CreateUIObject("Content", viewport.transform, new Vector2(0.5f,1), new Vector2(0.5f,1), Vector2.zero);
            var contentRect = content.GetComponent<RectTransform>(); contentRect.pivot = new Vector2(0.5f,1);
            var layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 16; layout.padding = new RectOffset(24,24,24,24);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect; scroll.viewport = viewport.GetComponent<RectTransform>();

            // Create Cards
            CreateAudioCard(content.transform);
            CreateGameplayCard(content.transform);
            CreateVisualCard(content.transform);
            CreateLanguageCard(content.transform);
            CreateNotificationsCard(content.transform);
            CreateDataCard(content.transform);
            CreateAboutCard(content.transform);

            // Add runtime SettingsManager component to canvas
            var settingsManager = canvasGO.AddComponent< global::SettingsManager >();

            // Save scene
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Settings.unity");
            Debug.Log("Settings scene created: Assets/Scenes/Settings.unity");
        }

        static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.anchoredPosition = anchoredPos; rt.sizeDelta = new Vector2(0, 100);
            return go;
        }

        static Text CreateText(string name, Transform parent, string text, int size = 18, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text; t.fontSize = size; t.alignment = anchor; t.color = Color.white;
            var rt = t.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0, 40);
            return t;
        }

        static Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.2f,0.6f,1f);
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var txt = CreateText("Text", go.transform, label, 20, TextAnchor.MiddleCenter);
            txt.rectTransform.sizeDelta = new Vector2(160, 48);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(160, 48);
            return btn;
        }

        static Toggle CreateToggle(string name, Transform parent, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            var bg = go.AddComponent<Image>(); bg.color = new Color(0.15f,0.15f,0.15f);
            toggle.targetGraphic = bg;
            var labelText = CreateText("Label", go.transform, label, 18, TextAnchor.MiddleLeft);
            labelText.rectTransform.anchoredPosition = new Vector2(24, 0);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0, 48);
            return toggle;
        }

        static Slider CreateSlider(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            var bg = go.AddComponent<Image>(); bg.color = new Color(0.2f,0.2f,0.2f);
            slider.targetGraphic = bg;
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(0, 48);
            return slider;
        }

        static void CreateCardHeader(Transform card, string title)
        {
            CreateText("CardTitle", card, title, 20, TextAnchor.UpperLeft).rectTransform.anchoredPosition = new Vector2(8, -8);
        }

        static void CreateAudioCard(Transform parent)
        {
            var card = new GameObject("AudioCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,180);
            CreateCardHeader(card.transform, "Ses");

            // Controls
            var musicToggle = CreateToggle("MusicToggle", card.transform, "Müzik");
            var musicSlider = CreateSlider("MusicSlider", card.transform);
            var sfxToggle = CreateToggle("SFXToggle", card.transform, "Ses Efektleri");
            var sfxSlider = CreateSlider("SFXSlider", card.transform);
            var vibrationToggle = CreateToggle("VibrationToggle", card.transform, "Titreşim");
        }

        static void CreateGameplayCard(Transform parent)
        {
            var card = new GameObject("GameplayCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,220);
            CreateCardHeader(card.transform, "Oyun");

            CreateToggle("PlacementPreviewToggle", card.transform, "Yerleştirme önizlemesi");
            CreateToggle("GridHighlightToggle", card.transform, "Izgara vurgusu");
            CreateToggle("ComboVisualToggle", card.transform, "Kombolarda görsel efektler");
            CreateToggle("AnimationsToggle", card.transform, "Animasyonlar");
            CreateToggle("AutoSaveToggle", card.transform, "Otomatik Kaydetme");
        }

        static void CreateVisualCard(Transform parent)
        {
            var card = new GameObject("VisualCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,180);
            CreateCardHeader(card.transform, "Görünüm");

            var themeRow = new GameObject("ThemeRow"); themeRow.transform.SetParent(card.transform, false); var hl = themeRow.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8;
            CreateButton("ThemeLightButton", themeRow.transform, "Light");
            CreateButton("ThemeDarkButton", themeRow.transform, "Dark");
            CreateButton("ThemeColorfulButton", themeRow.transform, "Colorful");

            CreateToggle("ReduceMotionToggle", card.transform, "Hareketi Azalt");
            CreateToggle("HighContrastToggle", card.transform, "Yüksek Kontrast");
        }

        static void CreateLanguageCard(Transform parent)
        {
            var card = new GameObject("LanguageCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,120);
            CreateCardHeader(card.transform, "Dil");

            var ddGO = new GameObject("LanguageDropdown"); ddGO.transform.SetParent(card.transform, false); var dd = ddGO.AddComponent<Dropdown>(); dd.options.Add(new Dropdown.OptionData("Türkçe")); dd.options.Add(new Dropdown.OptionData("English")); dd.value = 0;
        }

        static void CreateNotificationsCard(Transform parent)
        {
            var card = new GameObject("NotificationsCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,140);
            CreateCardHeader(card.transform, "Bildirimler");

            CreateToggle("DailyReminderToggle", card.transform, "Günlük Hatırlatma");
            CreateToggle("NewFeaturesToggle", card.transform, "Yeni Özellikler");
            CreateToggle("TipsToggle", card.transform, "İpuçları");
        }

        static void CreateDataCard(Transform parent)
        {
            var card = new GameObject("DataCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,140);
            CreateCardHeader(card.transform, "Veri");

            CreateButton("ResetProgressButton", card.transform, "Reset Progress");
            CreateButton("ClearCacheButton", card.transform, "Clear Cache");
            CreateButton("RestoreDefaultsButton", card.transform, "Restore Defaults");
        }

        static void CreateAboutCard(Transform parent)
        {
            var card = new GameObject("AboutCard"); card.transform.SetParent(parent, false);
            var img = card.AddComponent<Image>(); img.color = new Color(0.06f,0.07f,0.09f);
            var v = card.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.padding = new RectOffset(12,12,12,12);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0,120);
            CreateCardHeader(card.transform, "Hakkında");

            var version = CreateText("VersionText", card.transform, "v1.0.0", 16, TextAnchor.MiddleLeft);
            var dev = CreateText("DevText", card.transform, "Developer: YourName", 14, TextAnchor.MiddleLeft);
            CreateButton("PrivacyButton", card.transform, "Privacy Policy");
            CreateButton("TermsButton", card.transform, "Terms of Service");
            CreateButton("CreditsButton", card.transform, "Credits");
        }
    }
}
#endif