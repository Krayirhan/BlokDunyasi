using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ButtonTextStyleApplier
{
    private const string TargetFontPath = "Assets/Skyden_Games/Free_Casual_GUI/Demo/Fonts/Baloo/Baloo-Regular SDF.asset";

    [MenuItem("Tools/BlokDunyasi/Apply Button Text Style")]
    public static void ApplyButtonTextStyleMenu()
    {
        ApplyButtonTextStyleInternal();
    }

    public static void ApplyButtonTextStyleBatch()
    {
        ApplyButtonTextStyleInternal();
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }

    private static void ApplyButtonTextStyleInternal()
    {
        var targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        if (targetFont == null)
        {
            throw new Exception("TMP font bulunamadı: " + TargetFontPath);
        }

        var changedTmpCount = 0;
        var changedLegacyCount = 0;

        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (var guid in sceneGuids)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                var buttons = root.GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    var tmpTexts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmpTexts)
                    {
                        if (ApplyTmpStyle(tmp, targetFont))
                        {
                            changed = true;
                            changedTmpCount++;
                        }
                    }

                    var legacyTexts = button.GetComponentsInChildren<Text>(true);
                    foreach (var text in legacyTexts)
                    {
                        if (text.font == null)
                        {
                            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                            EditorUtility.SetDirty(text);
                            changed = true;
                            changedLegacyCount++;
                        }
                    }
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (var guid in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var changed = false;

            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                var tmpTexts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpTexts)
                {
                    if (ApplyTmpStyle(tmp, targetFont))
                    {
                        changed = true;
                        changedTmpCount++;
                    }
                }

                var legacyTexts = button.GetComponentsInChildren<Text>(true);
                foreach (var text in legacyTexts)
                {
                    if (text.font == null)
                    {
                        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        EditorUtility.SetDirty(text);
                        changed = true;
                        changedLegacyCount++;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ButtonTextStyleApplier] TMP değişen: {changedTmpCount}, Legacy Text düzeltilen: {changedLegacyCount}");
    }

    private static bool ApplyTmpStyle(TextMeshProUGUI tmp, TMP_FontAsset targetFont)
    {
        var changed = false;

        if (tmp.font != targetFont)
        {
            tmp.font = targetFont;
            changed = true;
        }

        if (tmp.fontStyle != FontStyles.Normal)
        {
            tmp.fontStyle = FontStyles.Normal;
            changed = true;
        }

        if (tmp.enableAutoSizing)
        {
            tmp.enableAutoSizing = false;
            changed = true;
        }

        if (tmp.overflowMode != TextOverflowModes.Overflow)
        {
            tmp.overflowMode = TextOverflowModes.Overflow;
            changed = true;
        }

        if (tmp.horizontalMapping != TextureMappingOptions.Character)
        {
            tmp.horizontalMapping = TextureMappingOptions.Character;
            changed = true;
        }

        if (tmp.verticalMapping != TextureMappingOptions.Character)
        {
            tmp.verticalMapping = TextureMappingOptions.Character;
            changed = true;
        }

        if (tmp.alignment != TextAlignmentOptions.Center)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(tmp);
        }

        return changed;
    }
}