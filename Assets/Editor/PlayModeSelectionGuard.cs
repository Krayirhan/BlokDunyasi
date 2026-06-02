#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlokDunyasiTools
{
    [InitializeOnLoad]
    internal static class PlayModeSelectionGuard
    {
        private static bool _refreshQueued;

        static PlayModeSelectionGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearTransientSelectionBeforeReload;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            Selection.selectionChanged += HandleSelectionChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
                ClearTransientSelection();
        }

        private static void HandleHierarchyChanged()
        {
            QueueInspectorRefreshIfSelectionInvalid();
        }

        private static void HandleSelectionChanged()
        {
            QueueInspectorRefreshIfSelectionInvalid();
        }

        private static void ClearTransientSelectionBeforeReload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                ClearTransientSelection();
        }

        private static void ClearTransientSelection()
        {
            Object selected = Selection.activeObject;
            if (selected == null)
                return;

            if (EditorUtility.IsPersistent(selected))
                return;

            Selection.activeObject = null;
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static void QueueInspectorRefreshIfSelectionInvalid()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || _refreshQueued)
                return;

            if (!IsSelectionInvalid())
                return;

            _refreshQueued = true;
            EditorApplication.delayCall += RefreshInspectorSelection;
        }

        private static void RefreshInspectorSelection()
        {
            _refreshQueued = false;

            if (!IsSelectionInvalid())
                return;

            Selection.objects = new Object[0];
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static bool IsSelectionInvalid()
        {
            Object selected = Selection.activeObject;
            if (selected == null)
                return true;

            int instanceId = Selection.activeInstanceID;
            if (instanceId == 0)
                return false;

            return EditorUtility.InstanceIDToObject(instanceId) == null;
        }
    }
}
#endif
