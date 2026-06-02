using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to set up SafeAreaRoot hierarchy in scenes.
/// Usage: Attach to Canvas or call SetupSafeAreaRootInScene() from editor script.
/// </summary>
public class SafeAreaRootSetup : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private string safeAreaRootName = "SafeAreaRoot";
    [SerializeField] private bool setupScreenLayoutManager = true;

    private void OnValidate()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();
    }

    /// <summary>
    /// Sets up or updates the SafeAreaRoot hierarchy under the target Canvas.
    /// Call this from Editor script or manually via inspector.
    /// </summary>
    public void SetupSafeAreaRoot()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[SafeAreaRootSetup] Target Canvas is null. Assign it in inspector.");
            return;
        }

        Transform canvasTransform = targetCanvas.transform;

        // Find or create SafeAreaRoot
        Transform safeAreaRoot = canvasTransform.Find(safeAreaRootName);
        if (safeAreaRoot == null)
        {
            GameObject safeAreaGO = new GameObject(safeAreaRootName);
            safeAreaRoot = safeAreaGO.transform;
            safeAreaRoot.SetParent(canvasTransform, false);
            Debug.Log($"[SafeAreaRootSetup] Created '{safeAreaRootName}' under '{canvasTransform.name}'");
        }

        // Configure RectTransform
        var safeAreaRectTransform = safeAreaRoot.GetComponent<RectTransform>();
        if (safeAreaRectTransform == null)
            safeAreaRectTransform = safeAreaRoot.gameObject.AddComponent<RectTransform>();

        // Set full stretch anchors
        safeAreaRectTransform.anchorMin = Vector2.zero;
        safeAreaRectTransform.anchorMax = Vector2.one;
        safeAreaRectTransform.offsetMin = Vector2.zero;
        safeAreaRectTransform.offsetMax = Vector2.zero;
        safeAreaRectTransform.localScale = Vector3.one;

        // Add SafeAreaFitter if not present
        var safeAreaFitter = safeAreaRoot.GetComponent<SafeAreaFitter>();
        if (safeAreaFitter == null)
        {
            safeAreaFitter = safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();
            Debug.Log($"[SafeAreaRootSetup] Added SafeAreaFitter to '{safeAreaRootName}'");
        }

        Debug.Log($"[SafeAreaRootSetup] SafeAreaRoot setup complete for '{canvasTransform.name}'");
    }

    /// <summary>
    /// Editor menu helper to set up SafeAreaRoot for a scene.
    /// </summary>
    public static void SetupAllCanvasesInScene()
    {
        var canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"[SafeAreaRootSetup] Found {canvases.Length} Canvas(es). Processing...");

        foreach (var canvas in canvases)
        {
            SetupSafeAreaRootForCanvas(canvas);
        }

        Debug.Log("[SafeAreaRootSetup] All Canvas(es) processed.");
    }

    /// <summary>
    /// Static helper to set up SafeAreaRoot for a specific Canvas.
    /// </summary>
    public static void SetupSafeAreaRootForCanvas(Canvas canvas, string safeAreaRootName = "SafeAreaRoot")
    {
        if (canvas == null)
            return;

        Transform canvasTransform = canvas.transform;
        Transform safeAreaRoot = canvasTransform.Find(safeAreaRootName);

        // If SafeAreaRoot doesn't exist, create it
        if (safeAreaRoot == null)
        {
            GameObject safeAreaGO = new GameObject(safeAreaRootName);
            safeAreaRoot = safeAreaGO.transform;
            safeAreaRoot.SetParent(canvasTransform, false);
            safeAreaRoot.SetSiblingIndex(0); // Move to top
        }

        // Configure RectTransform for full coverage
        var rectTr = safeAreaRoot.GetComponent<RectTransform>();
        if (rectTr == null)
            rectTr = safeAreaRoot.gameObject.AddComponent<RectTransform>();

        rectTr.anchorMin = Vector2.zero;
        rectTr.anchorMax = Vector2.one;
        rectTr.offsetMin = Vector2.zero;
        rectTr.offsetMax = Vector2.zero;

        // Add SafeAreaFitter component
        if (safeAreaRoot.GetComponent<SafeAreaFitter>() == null)
            safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();

        Debug.Log($"[SafeAreaRootSetup] Set up SafeAreaRoot for Canvas '{canvas.name}'");
    }
}
