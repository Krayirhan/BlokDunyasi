using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using TMPro;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private const string AssetPath = "Assets/Resources/TMP/MalgunGothic_DynamicSDF.asset";
        private const string SourceFontPath = "Assets/Resources/TMP/malgun.ttf";
        private const string SystemFontPath = @"C:\Windows\Fonts\malgun.ttf";

        public static void EnsureKoreanFontAssetSetup()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (IsFontAssetValid(fontAsset))
            {
                VerifyFallbacks(fontAsset);
                return;
            }

            if (fontAsset != null)
            {
                Debug.LogWarning("[AutoKoreanFontSetup] Korean TMP font asset is invalid. Recreating: " + AssetPath);
                AssetDatabase.DeleteAsset(AssetPath);
            }

            CreateKoreanFont();
        }

        [MenuItem("Tools/Block Puzzle/Recreate Korean Font Asset")]
        public static void CreateKoreanFont()
        {
            if (!File.Exists(SystemFontPath))
            {
                Debug.LogError("[AutoKoreanFontSetup] System font malgun.ttf not found at Windows Fonts folder!");
                return;
            }

            string destDir = Path.GetDirectoryName(SourceFontPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            try
            {
                File.Copy(SystemFontPath, SourceFontPath, true);
                AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoKoreanFontSetup] Failed to copy/import system font malgun.ttf: {ex.Message}");
                return;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[AutoKoreanFontSetup] Failed to load source Font after importing!");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
                AssetDatabase.DeleteAsset(AssetPath);

            TMP_FontAsset fontAsset = CreateFontAsset(sourceFont);

            if (fontAsset == null)
            {
                Debug.LogError("[AutoKoreanFontSetup] Failed to create TMP_FontAsset!");
                return;
            }

            fontAsset.name = "MalgunGothic_DynamicSDF";
            AssetDatabase.CreateAsset(fontAsset, AssetPath);
            AddSubAssets(fontAsset, AssetPath);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            if (!IsFontAssetValid(fontAsset))
            {
                Debug.LogError("[AutoKoreanFontSetup] Created TMP font asset is still invalid after import.");
                return;
            }

            Debug.Log("[AutoKoreanFontSetup] Korean dynamic font asset successfully created at: " + AssetPath);
            VerifyFallbacks(fontAsset);
        }

        private static TMP_FontAsset CreateFontAsset(Font sourceFont)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                10,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            if (fontAsset != null)
                return fontAsset;

            MethodInfo[] methods = typeof(TMP_FontAsset)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "CreateFontAsset")
                .OrderByDescending(m => m.GetParameters().Length)
                .ToArray();

            foreach (MethodInfo method in methods)
            {
                if (!TryBuildArguments(method.GetParameters(), sourceFont, out object[] args))
                    continue;

                try
                {
                    fontAsset = method.Invoke(null, args) as TMP_FontAsset;
                    if (fontAsset != null)
                        return fontAsset;
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool TryBuildArguments(ParameterInfo[] parameters, Font sourceFont, out object[] args)
        {
            args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                System.Type type = parameter.ParameterType;
                string name = parameter.Name?.ToLowerInvariant() ?? string.Empty;

                if (type == typeof(Font))
                {
                    args[i] = sourceFont;
                    continue;
                }

                if (type == typeof(int))
                {
                    if (name.Contains("point") || name.Contains("sampling"))
                        args[i] = 90;
                    else if (name.Contains("padding"))
                        args[i] = 10;
                    else if (name.Contains("width") || name.Contains("height"))
                        args[i] = 1024;
                    else
                        args[i] = 0;

                    continue;
                }

                if (type == typeof(bool))
                {
                    args[i] = true;
                    continue;
                }

                if (type == typeof(string))
                {
                    args[i] = string.Empty;
                    continue;
                }

                if (type.IsEnum)
                {
                    string enumName = type.Name.ToLowerInvariant();
                    if (enumName.Contains("glyphrendermode") || name.Contains("render"))
                    {
                        args[i] = System.Enum.Parse(type, "SDFAA");
                        continue;
                    }

                    if (enumName.Contains("atlaspopulationmode") || name.Contains("population"))
                    {
                        args[i] = System.Enum.Parse(type, "Dynamic");
                        continue;
                    }

                    args[i] = System.Enum.GetValues(type).GetValue(0);
                    continue;
                }

                if (parameter.HasDefaultValue)
                {
                    args[i] = parameter.DefaultValue;
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void AddSubAssets(TMP_FontAsset fontAsset, string assetPath)
        {
            if (fontAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.material)))
            {
                fontAsset.material.name = "MalgunGothic_DynamicSDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, assetPath);
            }

            Texture[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null)
                return;

            for (int i = 0; i < atlasTextures.Length; i++)
            {
                Texture atlasTexture = atlasTextures[i];
                if (atlasTexture == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(atlasTexture)))
                    continue;

                atlasTexture.name = $"MalgunGothic_DynamicSDF Atlas {i}";
                AssetDatabase.AddObjectToAsset(atlasTexture, assetPath);
            }
        }

        private static bool IsFontAssetValid(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
                return false;

            Texture[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null || atlasTextures.Length == 0 || atlasTextures[0] == null)
                return false;

            return fontAsset.material != null;
        }

        private static void VerifyFallbacks(TMP_FontAsset fontAsset)
        {
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

    public sealed class AutoKoreanFontBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            AutoKoreanFontSetup.EnsureKoreanFontAssetSetup();
        }
    }
}
