#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Sahne geçişi için gerekli
using UnityEngine.Rendering.Universal; // UniversalAdditionalCameraData için gerekli

namespace BlokDunyasiTools
{
    public static class MainMenuSceneSetup
    {
        [MenuItem("BlokDunyasi/Setup/Create Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Main Camera setup
            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            // Canvas setup
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Background setup
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(canvasGO.transform, false);
            var bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f); // Gradient or pattern can be added later
            bgImage.rectTransform.sizeDelta = new Vector2(1920, 1080);

            // Header setup
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(canvasGO.transform, false);
            var headerText = headerGO.AddComponent<Text>();
            headerText.text = "Blok Dünyası";
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.fontSize = 48;
            headerText.color = Color.white;
            headerText.rectTransform.anchoredPosition = new Vector2(0, 400);

            // Menu Buttons setup
            var menuButtonsGO = new GameObject("MenuButtons");
            menuButtonsGO.transform.SetParent(canvasGO.transform, false);
            CreateButton(menuButtonsGO.transform, "ContinueButton", "Devam Et", new Vector2(0, 150), ContinueGame);
            CreateButton(menuButtonsGO.transform, "PlayButton", "Başlat", new Vector2(0, 50), StartGame);
            CreateButton(menuButtonsGO.transform, "HowToPlayButton", "Nasıl Oynanır", new Vector2(0, -50), OpenHowToPlay);
            CreateButton(menuButtonsGO.transform, "SettingsButton", "Ayarlar", new Vector2(0, -150), OpenSettings);

            // Stats Panel setup
            var statsPanelGO = new GameObject("StatsPanel");
            statsPanelGO.transform.SetParent(canvasGO.transform, false);
            var bestScoreText = statsPanelGO.AddComponent<Text>();
            bestScoreText.text = "En Yüksek Skor: 0";
            bestScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bestScoreText.alignment = TextAnchor.MiddleCenter;
            bestScoreText.fontSize = 32;
            bestScoreText.color = Color.white;
            bestScoreText.rectTransform.anchoredPosition = new Vector2(0, -300);

            // Footer setup
            var footerGO = new GameObject("Footer");
            footerGO.transform.SetParent(canvasGO.transform, false);
            CreateButton(footerGO.transform, "SoundToggle", "🔊", new Vector2(-100, -400), ToggleSound);
            CreateButton(footerGO.transform, "ThemeButton", "🎨", new Vector2(100, -400), ChangeTheme);

            var versionTextGO = new GameObject("VersionText");
            versionTextGO.transform.SetParent(footerGO.transform, false);
            var versionText = versionTextGO.AddComponent<Text>();
            versionText.text = "v1.0.0";
            versionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            versionText.alignment = TextAnchor.MiddleCenter;
            versionText.fontSize = 24;
            versionText.color = Color.gray;
            versionText.rectTransform.anchoredPosition = new Vector2(0, -450);

            // Save the scene
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("Main Menu Scene created and saved at: " + scenePath);
        }

        private static void CreateButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClickAction)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            var button = buttonGO.AddComponent<Button>();
            buttonGO.AddComponent<Image>().color = Color.white;
            button.onClick.AddListener(onClickAction);

            var rectTransform = buttonGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            rectTransform.anchoredPosition = position;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // LegacyRuntime.ttf kullanımı
            textComponent.color = Color.black;
            textComponent.rectTransform.sizeDelta = rectTransform.sizeDelta;

            // Ensure the button is linked to the correct function
            if (name == "StartButton")
            {
                button.onClick.AddListener(StartGame);
            }
        }

        private static void StartGame()
        {
            Debug.Log("Start Game clicked! Loading OyunEkranı scene...");
            SceneManager.LoadScene("OyunEkranı"); // OyunEkranı sahnesine geçiş
        }

        private static void OpenSettings()
        {
            Debug.Log("Settings clicked!");
            // Add logic to open settings menu
        }

        private static void QuitGame()
        {
            Debug.Log("Quit clicked!");
            // Add logic to quit the application
        }

        private static void ContinueGame()
        {
            Debug.Log("Continue clicked!");
            // Add logic to continue the game
        }

        private static void OpenHowToPlay()
        {
            Debug.Log("How to Play clicked!");
            // Add logic to open the tutorial
        }

        private static void ToggleSound()
        {
            Debug.Log("Sound toggle clicked!");
            // Add logic to toggle sound
        }

        private static void ChangeTheme()
        {
            Debug.Log("Theme change clicked!");
            // Add logic to change theme
        }
    }
}
#endif