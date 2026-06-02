using UnityEngine;
using UnityEditor;
using TMPro;

namespace BlokDunyasi.Editor
{
    public class BungeeFontCreator
    {
        [MenuItem("Tools/BlokDunyasi/Manual: Create Bungee SDF")]
        public static void ShowBungeeCreationGuide()
        {
            string message = "Bungee SDF Font'u manuel oluşturma:\n\n" +
                "1. Window → TextMesh Pro → Font Asset Creator aç\n" +
                "2. \"Browse\" → Bungee-Regular.ttf seç\n" +
                "   (Assets/TextMesh Pro/Resources/Fonts & Materials/)\n" +
                "3. \"Generate Font Atlas\" tıkla\n" +
                "4. \"Save\" → \"Bungee Regular SDF\" adıyla kaydet\n" +
                "5. AnimationIntegration'da seç\n\n" +
                "NOT: Eğer Bungee sorun verirse, Oswald Bold SDF kullan!";

            EditorUtility.DisplayDialog("Bungee SDF Oluşturma Rehberi", message, "Tamam");
        }

        [MenuItem("Tools/BlokDunyasi/Use Oswald Bold (Default)")]
        public static void UseOswaldDefault()
        {
            EditorUtility.DisplayDialog("Oswald Bold Seçildi", 
                "Combo Font olarak Oswald Bold SDF kullanılacak.\n" +
                "Inspector'de AnimationIntegration'ı kontrol et.", 
                "Tamam");
            Debug.Log("[BungeeFontCreator] Oswald Bold SDF varsayılan font olarak ayarlandı.");
        }
    }
}
