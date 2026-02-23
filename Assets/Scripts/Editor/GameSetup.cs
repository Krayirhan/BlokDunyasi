#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.Blocks;
using BlockPuzzle.UnityAdapter.Input;

namespace BlokDunyasiTools
{
    /// <summary>
    /// OYUN SİSTEMİ KURULUM ARACI
    /// Menu: BlokDunyasi > Setup
    /// </summary>
    public static class GameSetup
    {
        [MenuItem("BlokDunyasi/Setup/1. Setup Game System")]
        public static void SetupFullSystem()
        {
            Debug.Log("=== OYUN SİSTEMİ KURULUMU ===");

            AddMissingComponents();

            Debug.Log("=== KURULUM TAMAMLANDI ===");
            Debug.Log("Oyunu çalıştırın ve test edin!");
        }

        [MenuItem("BlokDunyasi/Setup/2. Add Missing Components")]
        public static void AddMissingComponents()
        {
            // Root obje bul veya oluştur
            var rootObj = GameObject.Find("GameSystem");
            if (rootObj == null)
            {
                rootObj = new GameObject("GameSystem");
                Debug.Log("[Setup] GameSystem objesi oluşturuldu");
            }

            // 1. NewBlockTray
            var tray = Object.FindFirstObjectByType<NewBlockTray>();
            if (tray == null)
            {
                var trayObj = new GameObject("BlockTray");
                trayObj.transform.SetParent(rootObj.transform);
                tray = trayObj.AddComponent<NewBlockTray>();
                Debug.Log("[Setup] BlockTray eklendi");
            }

            // 2. NewPreviewSystem
            var preview = Object.FindFirstObjectByType<NewPreviewSystem>();
            if (preview == null)
            {
                var previewObj = new GameObject("PreviewSystem");
                previewObj.transform.SetParent(rootObj.transform);
                preview = previewObj.AddComponent<NewPreviewSystem>();
                Debug.Log("[Setup] PreviewSystem eklendi");
            }

            // 3. NewDragSystem
            var drag = Object.FindFirstObjectByType<NewDragSystem>();
            if (drag == null)
            {
                var dragObj = new GameObject("DragSystem");
                dragObj.transform.SetParent(rootObj.transform);
                drag = dragObj.AddComponent<NewDragSystem>();
                Debug.Log("[Setup] DragSystem eklendi");
            }

            // Sahneyi dirty işaretle
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Setup] Componentler hazır!");
        }

        [MenuItem("BlokDunyasi/Setup/3. Report System Status")]
        public static void ReportStatus()
        {
            Debug.Log("=== SİSTEM DURUMU RAPORU ===");

            // Ana sistemler
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var gridView = Object.FindFirstObjectByType<SimpleGridView>();
            var drag = Object.FindFirstObjectByType<NewDragSystem>();
            var tray = Object.FindFirstObjectByType<NewBlockTray>();
            var preview = Object.FindFirstObjectByType<NewPreviewSystem>();

            Debug.Log($"GameBootstrap: {(bootstrap != null ? "VAR" : "YOK")}");
            Debug.Log($"SimpleGridView: {(gridView != null ? "VAR" : "YOK")}");
            Debug.Log($"NewDragSystem: {(drag != null ? (drag.enabled ? "AKTIF" : "DEVRE DISI") : "YOK")}");
            Debug.Log($"NewBlockTray: {(tray != null ? (tray.enabled ? "AKTIF" : "DEVRE DISI") : "YOK")}");
            Debug.Log($"NewPreviewSystem: {(preview != null ? (preview.enabled ? "AKTIF" : "DEVRE DISI") : "YOK")}");

            Debug.Log("=== RAPOR SONU ===");
        }
    }
}
#endif
