using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.TextCore.LowLevel;

namespace BlockPuzzle.UnityAdapter.Editor
{
    [InitializeOnLoad]
    public static class AutoKoreanFontSetup
    {
        static AutoKoreanFontSetup()
        {
            EditorApplication.delayCall += EnsureKoreanFontAssetSetup;
        }

        public static void EnsureKoreanFontAssetSetup()
        {
            string assetPath = "Assets/Resources/TMP/MalgunGothic_DynamicSDF.asset";
            
            // Check if it already exists and fallbacks are registered
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset != null)
            {
                // Verify fallback is added to main fonts
                VerifyFallback("Assets/Resources/TMP/LuckiestGuy-Regular Combo SDF.asset", fontAsset);
                VerifyFallback("Assets/Skyden_Games/Free_Casual_GUI/Demo/Fonts/Baloo/Baloo-Regular SDF.asset", fontAsset);
                VerifyFallback("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", fontAsset);
                return;
            }

            // Create it!
            CreateKoreanFont();
        }

        [MenuItem("Tools/Block Puzzle/Recreate Korean Font Asset")]
        public static void CreateKoreanFont()
        {
            string systemFontPath = @"C:\Windows\Fonts\malgun.ttf";
            if (!File.Exists(systemFontPath))
            {
                Debug.LogError("[AutoKoreanFontSetup] System font malgun.ttf not found at Windows Fonts folder!");
                return;
            }

            string destFontPath = "Assets/Resources/TMP/malgun.ttf";
            string destDir = Path.GetDirectoryName(destFontPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            try
            {
                File.Copy(systemFontPath, destFontPath, true);
                AssetDatabase.ImportAsset(destFontPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoKoreanFontSetup] Failed to copy/import system font malgun.ttf: {ex.Message}");
                return;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(destFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[AutoKoreanFontSetup] Failed to load source Font after importing!");
                return;
            }

            // Create the dynamic font asset
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 
                90,                 // samplingPointSize
                10,                 // atlasPadding
                GlyphRenderMode.SDFAA, 
                1024,               // atlasWidth
                1024,               // atlasHeight
                AtlasPopulationMode.Dynamic
            );

            if (fontAsset == null)
            {
                Debug.LogError("[AutoKoreanFontSetup] Failed to create TMP_FontAsset!");
                return;
            }

            string assetPath = "Assets/Resources/TMP/MalgunGothic_DynamicSDF.asset";
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AutoKoreanFontSetup] Korean dynamic font asset successfully created at: " + assetPath);

            VerifyFallback("Assets/Resources/TMP/LuckiestGuy-Regular Combo SDF.asset", fontAsset);
            VerifyFallback("Assets/Skyden_Games/Free_Casual_GUI/Demo/Fonts/Baloo/Baloo-Regular SDF.asset", fontAsset);
            VerifyFallback("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", fontAsset);
        }

        private static void VerifyFallback(string mainFontPath, TMP_FontAsset fallback)
        {
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainFontPath);
            if (mainFont == null)
                return;

            if (mainFont.fallbackFontAssetTable == null)
                mainFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

            if (!mainFont.fallbackFontAssetTable.Contains(fallback))
            {
                mainFont.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(mainFont);
                AssetDatabase.SaveAssets();
                Debug.Log("[AutoKoreanFontSetup] Added Korean fallback to main font: " + mainFontPath);
            }
        }
    }
}
