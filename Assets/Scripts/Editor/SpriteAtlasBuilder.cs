using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasBuilder
{
    private const string AtlasFolder = "Assets/Resources/Atlases";
    private const string AtlasPath = AtlasFolder + "/BlockPuzzleMain.spriteatlas";

    private static readonly string[] PackableFolders =
    {
        "Assets/Bloklar",
        "Assets/Buttons",
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites"
    };

    [MenuItem("BlokDunyasi/Art/Build Main SpriteAtlas")]
    public static void BuildMainAtlas()
    {
        EnsureFolder(AtlasFolder);

        var existingAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        if (existingAtlas != null)
            AssetDatabase.DeleteAsset(AtlasPath);

        var atlas = new SpriteAtlas();
        AssetDatabase.CreateAsset(atlas, AtlasPath);

        atlas.SetIncludeInBuild(true);
        atlas.SetPackingSettings(new SpriteAtlasPackingSettings
        {
            enableRotation = false,
            enableTightPacking = false,
            padding = 2
        });
        atlas.SetTextureSettings(new SpriteAtlasTextureSettings
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear
        });

        var platformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
        platformSettings.overridden = true;
        platformSettings.maxTextureSize = 2048;
        platformSettings.textureCompression = TextureImporterCompression.Compressed;
        platformSettings.format = TextureImporterFormat.Automatic;
        atlas.SetPlatformSettings(platformSettings);

        var packables = CollectPackables();
        if (packables.Count > 0)
            SpriteAtlasExtensions.Add(atlas, packables.ToArray());

        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget, false);
        Debug.Log($"[SpriteAtlasBuilder] Built atlas at {AtlasPath} with {packables.Count} packables.");
    }

    private static List<Object> CollectPackables()
    {
        var results = new List<Object>();
        for (int i = 0; i < PackableFolders.Length; i++)
        {
            var folder = PackableFolders[i];
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            var folderObject = AssetDatabase.LoadAssetAtPath<Object>(folder);
            if (folderObject != null)
                results.Add(folderObject);
        }

        return results;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var segments = folderPath.Split('/');
        var current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            var next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }
}
