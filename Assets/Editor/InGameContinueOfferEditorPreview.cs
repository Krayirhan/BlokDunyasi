using BlockPuzzle.UnityAdapter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InGameContinueOfferEditorPreview
{
    static InGameContinueOfferEditorPreview()
    {
        EditorApplication.delayCall += TryApplyPreviewToOpenScene;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += TryApplyPreviewToOpenScene;
    }

    [MenuItem("Tools/Apply In-Game Continue Offer Preview")]
    private static void ApplyPreviewManually()
    {
        ApplyPreview();
    }

    private static void TryApplyPreviewToOpenScene()
    {
        if (Application.isPlaying)
            return;

        ApplyPreview();
    }

    private static void ApplyPreview()
    {
        var views = Object.FindObjectsByType<GameOverView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool changed = false;

        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null)
                continue;

            view.RebuildInGameContinueOfferEditorPreview();
            EditorUtility.SetDirty(view);
            changed = true;
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
