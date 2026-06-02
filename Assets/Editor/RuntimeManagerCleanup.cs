using System.Collections.Generic;
using BlockPuzzle.UnityAdapter.UI.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RuntimeManagerCleanup
{
    [MenuItem("Tools/Cleanup Runtime Managers In Scene")]
    private static void CleanupRuntimeManagersInScene()
    {
        int removed = 0;
        removed += RemoveDuplicateComponents<LanguageManager>();
        removed += RemoveDuplicateObjectsByComponentTypeName("DeferredPlayerPrefsSaverBehaviour");

        if (removed > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[RuntimeManagerCleanup] Removed {removed} duplicate runtime manager objects.");
    }

    private static int RemoveDuplicateComponents<T>() where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (found.Length <= 1)
            return 0;

        var kept = new HashSet<GameObject>();
        int removed = 0;

        for (int i = 0; i < found.Length; i++)
        {
            T component = found[i];
            if (component == null)
                continue;

            GameObject go = component.gameObject;
            if (kept.Count == 0)
            {
                kept.Add(go);
                continue;
            }

            if (kept.Contains(go))
                continue;

            Undo.DestroyObjectImmediate(go);
            removed++;
        }

        return removed;
    }

    private static int RemoveDuplicateObjectsByComponentTypeName(string typeName)
    {
        MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var matches = new List<GameObject>();

        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour component = all[i];
            if (component == null)
                continue;

            if (component.GetType().Name == typeName)
                matches.Add(component.gameObject);
        }

        if (matches.Count <= 1)
            return 0;

        int removed = 0;
        for (int i = 1; i < matches.Count; i++)
        {
            if (matches[i] == null)
                continue;

            Undo.DestroyObjectImmediate(matches[i]);
            removed++;
        }

        return removed;
    }
}
