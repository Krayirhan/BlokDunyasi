using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Debug = BlockPuzzle.Core.Common.GameLogger;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlockPuzzle.UnityAdapter.UI.Localization
{
    /// <summary>
    /// Editor yardımcı - Dil sistemi setup'ı kolay yapmak için
    /// Otomatik olarak LanguageManager'ı scene'e ekler
    /// </summary>
    public class LanguageSystemSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Setup Language System")]
        private static void SetupLanguageSystem()
        {
            // LanguageManager'ı kontrol et, yoksa oluştur
            LanguageManager existingManager = Object.FindFirstObjectByType<LanguageManager>();
            
            if (existingManager == null)
            {
                GameObject managerGO = new GameObject("[LanguageManager]");
                LanguageManager manager = managerGO.AddComponent<LanguageManager>();
                Undo.RegisterCreatedObjectUndo(managerGO, "Create LanguageManager");
                Debug.Log("[LanguageSystemSetup] LanguageManager created successfully!");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            else
            {
                Debug.Log("[LanguageSystemSetup] LanguageManager already exists in scene!");
            }

            EditorUtility.DisplayDialog("Language System", "Language System setup complete!\n\n" +
                "Next steps:\n" +
                "1. Add 'LocalizedText' component to text elements\n" +
                "2. Add 'LocalizedImage' component to buttons/images\n" +
                "3. Add 'LanguageButton' component to language selection buttons\n" +
                "4. Check Inspector to configure Turkish/English content",
                "OK");
        }

        [MenuItem("Tools/Language System/Create Turkish Text Button")]
        private static void CreateTurkishButton()
        {
            CreateLanguageButton(LanguageManager.Language.Turkish, "Türkçe");
        }

        [MenuItem("Tools/Language System/Create English Text Button")]
        private static void CreateEnglishButton()
        {
            CreateLanguageButton(LanguageManager.Language.English, "English");
        }

        [MenuItem("Tools/Language System/Create Korean Text Button")]
        private static void CreateKoreanButton()
        {
            CreateLanguageButton(LanguageManager.Language.Korean, "한국어");
        }

        private static void CreateLanguageButton(LanguageManager.Language language, string buttonName)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in scene!", "OK");
                return;
            }

            // Button GameObject oluştur
            GameObject buttonGO = new GameObject(buttonName);
            buttonGO.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(buttonGO, $"Create {buttonName} button");

            // Image (Button background) component ekle
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 0.8f);

            // Button component ekle
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            Undo.RecordObject(button, $"Configure {buttonName} button");

            // LanguageButton component ekle
            LanguageButton langButton = buttonGO.AddComponent<LanguageButton>();
            SerializedObject so = new SerializedObject(langButton);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("targetLanguage").enumValueIndex = (int)language;
            so.ApplyModifiedProperties();
            Undo.RecordObject(langButton, $"Configure LanguageButton for {buttonName}");

            // Text child GameObject oluştur
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            Undo.RegisterCreatedObjectUndo(textGO, $"Create text for {buttonName} button");

            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = buttonName;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 36;

            // RectTransform'ı ayarla
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // LocalizedText component ekle
            LocalizedText localizedText = textGO.AddComponent<LocalizedText>();
            SerializedObject textSo = new SerializedObject(localizedText);
            textSo.FindProperty("textComponent").objectReferenceValue = textComponent;
            if (language == LanguageManager.Language.Turkish)
            {
                textSo.FindProperty("turkishText").stringValue = "Türkçe";
                textSo.FindProperty("englishText").stringValue = "Turkish";
                textSo.FindProperty("koreanText").stringValue = "터키어";
            }
            else if (language == LanguageManager.Language.Korean)
            {
                textSo.FindProperty("turkishText").stringValue = "Korece";
                textSo.FindProperty("englishText").stringValue = "Korean";
                textSo.FindProperty("koreanText").stringValue = "한국어";
            }
            else
            {
                textSo.FindProperty("turkishText").stringValue = "İngilizce";
                textSo.FindProperty("englishText").stringValue = "English";
                textSo.FindProperty("koreanText").stringValue = "영어";
            }
            textSo.ApplyModifiedProperties();

            // RectTransform'ı ayarla
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 60);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[LanguageSystemSetup] {buttonName} button created!");
        }
#endif
    }
}
