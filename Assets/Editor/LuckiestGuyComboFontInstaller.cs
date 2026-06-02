using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class LuckiestGuyComboFontInstaller
{
    private const string SourceFontPath = "Assets/Font/LuckiestGuy-Regular.ttf";
    private const string TargetFolderPath = "Assets/Resources/TMP";
    private const string TargetAssetPath = "Assets/Resources/TMP/LuckiestGuy-Regular Combo SDF.asset";

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

    [MenuItem("Tools/BlokDunyasi/Fonts/Install Luckiest Guy Combo Font")]
    public static void InstallFromMenu()
    {
        EnsureInstalled(forceRecreate: true);
    }

    private static void EnsureInstalled(bool forceRecreate = false)
    {
        try
        {
            Directory.CreateDirectory(Path.GetFullPath(TargetFolderPath));

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetAssetPath);
            if (existing != null && !forceRecreate)
                return;

            if (existing != null)
                AssetDatabase.DeleteAsset(TargetAssetPath);

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogWarning("[LuckiestGuyComboFontInstaller] Source font not found: " + SourceFontPath);
                return;
            }

            var fontAsset = CreateFontAssetViaReflection(sourceFont);
            if (fontAsset == null)
            {
                Debug.LogError("[LuckiestGuyComboFontInstaller] Failed to create TMP font asset for LuckiestGuy-Regular.ttf");
                return;
            }

            fontAsset.name = "LuckiestGuy-Regular Combo SDF";
            AssetDatabase.CreateAsset(fontAsset, TargetAssetPath);

            if (fontAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.material)))
            {
                fontAsset.material.name = "LuckiestGuy-Regular Combo SDF Material";
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

                    atlasTexture.name = $"LuckiestGuy-Regular Combo SDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(atlasTexture, TargetAssetPath);
                }
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TargetAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Debug.Log("[LuckiestGuyComboFontInstaller] Installed combo TMP font: " + TargetAssetPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[LuckiestGuyComboFontInstaller] " + ex);
        }
    }

    private static TMP_FontAsset CreateFontAssetViaReflection(Font sourceFont)
    {
        var methods = typeof(TMP_FontAsset)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => string.Equals(m.Name, "CreateFontAsset", StringComparison.Ordinal))
            .OrderByDescending(m => m.GetParameters().Length)
            .ToArray();

        for (int i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (!TryBuildArguments(method.GetParameters(), sourceFont, out var args))
                continue;

            try
            {
                var result = method.Invoke(null, args) as TMP_FontAsset;
                if (result != null)
                    return result;
            }
            catch
            {
                // Try next overload.
            }
        }

        return null;
    }

    private static bool TryBuildArguments(ParameterInfo[] parameters, Font sourceFont, out object[] args)
    {
        args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var type = parameter.ParameterType;
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
                    args[i] = 8;
                else if (name.Contains("width"))
                    args[i] = 1024;
                else if (name.Contains("height"))
                    args[i] = 1024;
                else if (name.Contains("face") || name.Contains("index"))
                    args[i] = 0;
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
                if (!TryResolveEnumValue(type, name, out var enumValue))
                    return false;

                args[i] = enumValue;
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

    private static bool TryResolveEnumValue(Type enumType, string parameterName, out object value)
    {
        string[] names = Enum.GetNames(enumType);

        if (enumType.Name.Contains("GlyphRenderMode", StringComparison.OrdinalIgnoreCase) || parameterName.Contains("render"))
        {
            string preferred = names.FirstOrDefault(n => n.Equals("SDFAA", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(n => n.Contains("SDFAA", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(n => n.Contains("SDF", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault();

            if (preferred != null)
            {
                value = Enum.Parse(enumType, preferred);
                return true;
            }
        }

        if (enumType.Name.Contains("AtlasPopulationMode", StringComparison.OrdinalIgnoreCase) || parameterName.Contains("population"))
        {
            string preferred = names.FirstOrDefault(n => n.Equals("Dynamic", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(n => n.Contains("Dynamic", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault();

            if (preferred != null)
            {
                value = Enum.Parse(enumType, preferred);
                return true;
            }
        }

        if (names.Length > 0)
        {
            value = Enum.Parse(enumType, names[0]);
            return true;
        }

        value = null;
        return false;
    }
}
