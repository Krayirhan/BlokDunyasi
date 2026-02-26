#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlokDunyasiTools
{
    /// <summary>
    /// Applies a single source icon texture to platform icon slots.
    /// </summary>
    public static class AppIconTool
    {
        private const string DefaultIconAssetPath = "Assets/Branding/AppIconSource.png";

        [MenuItem("BlokDunyasi/Store/Apply App Icon (Default Source)", false, 320)]
        public static void ApplyDefaultSourceIcon()
        {
            if (!EnsureDefaultSourceExists())
            {
                Debug.LogError($"[AppIconTool] Source icon not found: {DefaultIconAssetPath}");
                return;
            }

            ApplyFromAssetPath(DefaultIconAssetPath);
        }

        [MenuItem("BlokDunyasi/Store/Select Icon And Apply", false, 321)]
        public static void SelectIconAndApply()
        {
            string selected = EditorUtility.OpenFilePanel(
                "Select App Icon Source",
                Application.dataPath,
                "png,jpg,jpeg");

            if (string.IsNullOrEmpty(selected))
                return;

            string relative = ToProjectRelativePath(selected);
            if (string.IsNullOrEmpty(relative))
            {
                relative = CopyExternalToDefaultSource(selected);
                AssetDatabase.ImportAsset(relative, ImportAssetOptions.ForceUpdate);
            }

            ApplyFromAssetPath(relative);
        }

        [MenuItem("BlokDunyasi/Store/Reveal App Icon Source", false, 322)]
        public static void RevealAppIconSource()
        {
            if (!EnsureDefaultSourceExists())
            {
                Debug.LogWarning($"[AppIconTool] Source icon not found: {DefaultIconAssetPath}");
                return;
            }

            EditorUtility.RevealInFinder(DefaultIconAssetPath);
        }

        private static void ApplyFromAssetPath(string iconAssetPath)
        {
            if (string.IsNullOrEmpty(iconAssetPath))
            {
                Debug.LogError("[AppIconTool] Invalid icon path.");
                return;
            }

            AssetDatabase.ImportAsset(iconAssetPath, ImportAssetOptions.ForceUpdate);
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(iconAssetPath);
            if (source == null)
            {
                Debug.LogError($"[AppIconTool] Could not load Texture2D at path: {iconAssetPath}");
                return;
            }

            EnsureDefaultIconImportSettings(iconAssetPath);
            AssetDatabase.ImportAsset(iconAssetPath, ImportAssetOptions.ForceUpdate);
            source = AssetDatabase.LoadAssetAtPath<Texture2D>(iconAssetPath);
            if (source == null)
            {
                Debug.LogError($"[AppIconTool] Could not reload Texture2D after import: {iconAssetPath}");
                return;
            }

            int appliedGroups = 0;
            if (ApplyToTargetGroup(BuildTargetGroup.Android, source))
                appliedGroups++;
            if (ApplyToTargetGroup(BuildTargetGroup.iOS, source))
                appliedGroups++;
            if (ApplyToTargetGroup(BuildTargetGroup.Standalone, source))
                appliedGroups++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AppIconTool] Applied icon '{iconAssetPath}' to {appliedGroups} target groups.");
        }

        private static bool ApplyToTargetGroup(BuildTargetGroup group, Texture2D source)
        {
            try
            {
                int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group);
                if (sizes == null || sizes.Length == 0)
                    return false;

                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++)
                    icons[i] = source;

                PlayerSettings.SetIconsForTargetGroup(group, icons);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppIconTool] Failed to apply icon for group {group}: {ex.Message}");
                return false;
            }
        }

        private static bool EnsureDefaultSourceExists()
        {
            return File.Exists(DefaultIconAssetPath);
        }

        private static void EnsureDefaultIconImportSettings(string iconAssetPath)
        {
            var importer = AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
            if (importer == null)
                return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static string CopyExternalToDefaultSource(string externalAbsolutePath)
        {
            string targetAbsolutePath = Path.GetFullPath(DefaultIconAssetPath);
            string targetFolder = Path.GetDirectoryName(targetAbsolutePath);
            if (!string.IsNullOrEmpty(targetFolder))
                Directory.CreateDirectory(targetFolder);

            File.Copy(externalAbsolutePath, targetAbsolutePath, overwrite: true);
            return DefaultIconAssetPath;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return null;

            string fullAbsolute = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string assetsAbsolute = Path.GetFullPath(Application.dataPath).Replace('\\', '/');

            if (!fullAbsolute.StartsWith(assetsAbsolute, StringComparison.OrdinalIgnoreCase))
                return null;

            return "Assets" + fullAbsolute.Substring(assetsAbsolute.Length);
        }
    }
}
#endif
