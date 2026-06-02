using BlockPuzzle.UnityAdapter.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSceneThemeController))]
public class GameSceneThemeControllerEditor : Editor
{
    private SerializedProperty _livePreview;
    private SerializedProperty _applyThemeOnPlay;
    private SerializedProperty _selectedTheme;
    private SerializedProperty _seedTheme1;
    private SerializedProperty _theme1Seeded;
    private SerializedProperty _gameBootstrap;
    private SerializedProperty _gridView;
    private SerializedProperty _blockTray;
    private SerializedProperty _hudView;
    private SerializedProperty _targetGoalSystem;
    private SerializedProperty _primaryGraphics;
    private SerializedProperty _secondaryGraphics;
    private SerializedProperty _theme1;
    private SerializedProperty _theme2;
    private SerializedProperty _theme3;

    private void OnEnable()
    {
        _livePreview = serializedObject.FindProperty("livePreviewInEditor");
        _applyThemeOnPlay = serializedObject.FindProperty("applyThemeOnPlay");
        _selectedTheme = serializedObject.FindProperty("selectedTheme");
        _seedTheme1 = serializedObject.FindProperty("seedTheme1FromCurrentSceneOnce");
        _theme1Seeded = serializedObject.FindProperty("theme1Seeded");
        _gameBootstrap = serializedObject.FindProperty("gameBootstrap");
        _gridView = serializedObject.FindProperty("gridView");
        _blockTray = serializedObject.FindProperty("blockTray");
        _hudView = serializedObject.FindProperty("hudView");
        _targetGoalSystem = serializedObject.FindProperty("targetGoalSystem");
        _primaryGraphics = serializedObject.FindProperty("primaryGraphics");
        _secondaryGraphics = serializedObject.FindProperty("secondaryGraphics");
        _theme1 = serializedObject.FindProperty("theme1");
        _theme2 = serializedObject.FindProperty("theme2");
        _theme3 = serializedObject.FindProperty("theme3");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var controller = (GameSceneThemeController)target;
        bool shouldApplyPreview = false;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Theme Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_livePreview);
        EditorGUILayout.PropertyField(_applyThemeOnPlay);
        EditorGUILayout.PropertyField(_seedTheme1);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(_theme1Seeded);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_gameBootstrap);
        EditorGUILayout.PropertyField(_gridView);
        EditorGUILayout.PropertyField(_blockTray);
        EditorGUILayout.PropertyField(_hudView);
        EditorGUILayout.PropertyField(_targetGoalSystem);
        EditorGUILayout.PropertyField(_primaryGraphics, true);
        EditorGUILayout.PropertyField(_secondaryGraphics, true);

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Theme Slots", EditorStyles.boldLabel);
        _selectedTheme.enumValueIndex = GUILayout.Toolbar(
            _selectedTheme.enumValueIndex,
            new[] { "Theme 1", "Theme 2", "Theme 3" });

        SerializedProperty activeTheme = GetActiveThemeProperty();
        if (activeTheme != null)
            DrawThemeProperty(activeTheme);

        if (EditorGUI.EndChangeCheck())
            shouldApplyPreview = true;

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Apply Selected Theme"))
        {
            serializedObject.ApplyModifiedProperties();
            controller.ApplySelectedTheme();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Capture Current Scene -> Selected Theme"))
        {
            serializedObject.ApplyModifiedProperties();
            switch ((GameSceneThemeController.ThemeSlot)_selectedTheme.enumValueIndex)
            {
                case GameSceneThemeController.ThemeSlot.Theme1:
                    controller.CaptureCurrentToTheme1();
                    break;
                case GameSceneThemeController.ThemeSlot.Theme2:
                    controller.CaptureCurrentToTheme2();
                    break;
                case GameSceneThemeController.ThemeSlot.Theme3:
                    controller.CaptureCurrentToTheme3();
                    break;
            }
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();

        if (shouldApplyPreview && controller.LivePreviewInEditor)
        {
            controller.ApplySelectedTheme();
            EditorUtility.SetDirty(target);
        }
    }

    private SerializedProperty GetActiveThemeProperty()
    {
        return _selectedTheme.enumValueIndex switch
        {
            1 => _theme2,
            2 => _theme3,
            _ => _theme1
        };
    }

    private static void DrawThemeProperty(SerializedProperty themeProperty)
    {
        EditorGUILayout.PropertyField(themeProperty.FindPropertyRelative("displayName"));

        DrawSection(themeProperty, "Background", new[]
        {
            "gameplayBackgroundSpriteOverride",
            "gameplayBackgroundTint",
            "gameplayBackgroundDimmerColor",
            "gameplayCameraClearColor"
        });

        DrawSection(themeProperty, "Board", new[]
        {
            "emptyCellColor",
            "emptyCellBorderColor",
            "boardBackdropColor",
            "boardBackdropBorderColor"
        });

        DrawSection(themeProperty, "Tray", new[]
        {
            "blockSpriteConfig",
            "blockPalette",
            "trayBlockBrightness",
            "dragBrightnessMultiplier",
            "trayNormalAlpha",
            "trayDragAlpha"
        });

        DrawSection(themeProperty, "Hud", new[]
        {
            "scoreTextColor",
            "bestScoreTextColor",
            "turnTextColor",
            "statusTextColor",
            "progressBarColor",
            "progressTextColor",
            "targetTextColor",
            "primaryGraphicColor",
            "secondaryGraphicColor"
        });
    }

    private static void DrawSection(SerializedProperty root, string title, string[] fieldNames)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        for (int i = 0; i < fieldNames.Length; i++)
        {
            SerializedProperty child = root.FindPropertyRelative(fieldNames[i]);
            if (child != null)
                EditorGUILayout.PropertyField(child, true);
        }
    }
}
