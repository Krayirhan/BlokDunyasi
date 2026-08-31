using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BlokDunyasiTools
{
    public static class GameFontInstaller
    {
        private const string SourceFontPath = "Assets/Font/GameFont.ttf";
        private const string TargetAssetPath = "Assets/Resources/TMP/GameFont SDF.asset";
        private const string AliasAssetPath = "Assets/Resources/TMP/Fredoka SDF.asset";
        private const string KoreanFontAssetPath = "Assets/Resources/TMP/MalgunGothic_DynamicSDF.asset";

        private static bool _queued;

        [InitializeOnLoadMethod]
        private static void QueueEnsureInstalled()
        {
            if (_queued)
                return;

            _queued = true;
            EditorApplication.delayCall += () =>
            {
                _queued = false;
                EnsureInstalled();
            };
        }

        [MenuItem("Tools/BlokDunyasi/Fonts/Install Casual Game Font")]
        public static void InstallFromMenu()
        {
            EnsureInstalled(forceRecreate: true);
        }

        public static TMP_FontAsset EnsureInstalled(bool forceRecreate = false)
        {
            try
            {
                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetAssetPath);
                if (existing != null && !forceRecreate)
                {
                    LinkKoreanFallback(existing);
                    return existing;
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (sourceFont == null)
                {
                    sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/TMP/GameFont.ttf");
                }

                if (sourceFont == null)
                {
                    Debug.LogWarning("[GameFontInstaller] Source font not found at: " + SourceFontPath);
                    return null;
                }

                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(TargetAssetPath);
                }

                var fontAsset = CreateFontAsset(sourceFont);
                if (fontAsset == null)
                {
                    Debug.LogError("[GameFontInstaller] Failed to create TMP font asset for GameFont.ttf");
                    return null;
                }

                fontAsset.name = "GameFont SDF";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                AssetDatabase.CreateAsset(fontAsset, TargetAssetPath);

                if (fontAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.material)))
                {
                    fontAsset.material.name = "GameFont SDF Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, TargetAssetPath);
                }

                var atlasTextures = fontAsset.atlasTextures;
                if (atlasTextures != null)
                {
                    for (int i = 0; i < atlasTextures.Length; i++)
                    {
                        var atlasTexture = atlasTextures[i];
                        if (atlasTexture == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(atlasTexture)))
                            continue;

                        atlasTexture.name = $"GameFont SDF Atlas {i}";
                        AssetDatabase.AddObjectToAsset(atlasTexture, TargetAssetPath);
                    }
                }

                LinkKoreanFallback(fontAsset);

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(TargetAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                // Also overwrite the alias if exists so any references pointing to Fredoka SDF get the bold font immediately
                try
                {
                    if (File.Exists(AliasAssetPath))
                    {
                        AssetDatabase.DeleteAsset(AliasAssetPath);
                    }
                    AssetDatabase.CopyAsset(TargetAssetPath, AliasAssetPath);
                    AssetDatabase.ImportAsset(AliasAssetPath, ImportAssetOptions.ForceSynchronousImport);
                }
                catch {}

                Debug.Log("[GameFontInstaller] Successfully created and installed GameFont SDF at " + TargetAssetPath);
                return fontAsset;
            }
            catch (Exception ex)
            {
                Debug.LogError("[GameFontInstaller] Exception during font installation: " + ex);
                return null;
            }
        }

        private static void LinkKoreanFallback(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            var koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            if (koreanFont != null && koreanFont != fontAsset)
            {
                if (fontAsset.fallbackFontAssetTable == null)
                    fontAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

                if (!fontAsset.fallbackFontAssetTable.Contains(koreanFont))
                {
                    fontAsset.fallbackFontAssetTable.Add(koreanFont);
                    EditorUtility.SetDirty(fontAsset);
                }
            }
        }

        private static TMP_FontAsset CreateFontAsset(Font sourceFont)
        {
            try
            {
                var standardAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (standardAsset != null)
                    return standardAsset;
            }
            catch
            {
            }

            var methods = typeof(TMP_FontAsset)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => string.Equals(m.Name, "CreateFontAsset", StringComparison.Ordinal))
                .OrderByDescending(m => m.GetParameters().Length)
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                bool valid = true;

                for (int p = 0; p < parameters.Length; p++)
                {
                    var paramType = parameters[p].ParameterType;
                    var paramName = parameters[p].Name?.ToLowerInvariant() ?? "";

                    if (paramType == typeof(Font))
                        args[p] = sourceFont;
                    else if (paramType == typeof(int))
                        args[p] = paramName.Contains("padding") ? 8 : (paramName.Contains("sampling") || paramName.Contains("point") ? 90 : 1024);
                    else if (paramType == typeof(bool))
                        args[p] = true;
                    else if (paramType == typeof(string))
                        args[p] = string.Empty;
                    else if (paramType.IsEnum)
                    {
                        var names = Enum.GetNames(paramType);
                        string match = names.FirstOrDefault(n => n.Equals("SDFAA", StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault(n => n.Contains("SDF", StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault(n => n.Equals("Dynamic", StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault();
                        args[p] = match != null ? Enum.Parse(paramType, match) : null;
                    }
                    else if (parameters[p].HasDefaultValue)
                        args[p] = parameters[p].DefaultValue;
                    else
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid) continue;

                try
                {
                    var result = method.Invoke(null, args) as TMP_FontAsset;
                    if (result != null)
                        return result;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
