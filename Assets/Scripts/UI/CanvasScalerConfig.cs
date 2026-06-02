using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Standardizes Canvas Scaler settings across all scenes.
/// Standard values: Reference Resolution (1080, 1920), Match Width Or Height = 0.5
/// </summary>
public static class CanvasScalerConfig
{
    public const float DEFAULT_REFERENCE_WIDTH = 1080f;
    public const float DEFAULT_REFERENCE_HEIGHT = 1920f;
    public const float DEFAULT_MATCH_WIDTH_HEIGHT = 0.5f;

    /// <summary>
    /// Applies standard Canvas Scaler settings to all Canvas components in the current scene.
    /// </summary>
    public static void ApplyToCurrentScene()
    {
        var canvases = Object.FindObjectsOfType<Canvas>();
        int count = 0;

        foreach (var canvas in canvases)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            ApplyStandardSettings(scaler);
            count++;
        }

        Debug.Log($"[CanvasScalerConfig] Applied standard settings to {count} Canvas component(s).");
    }

    /// <summary>
    /// Applies standard Canvas Scaler settings to a specific Canvas Scaler component.
    /// </summary>
    public static void ApplyToCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
            return;

        ApplyStandardSettings(scaler);
    }

    private static void ApplyStandardSettings(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(DEFAULT_REFERENCE_WIDTH, DEFAULT_REFERENCE_HEIGHT);
        scaler.matchWidthOrHeight = DEFAULT_MATCH_WIDTH_HEIGHT;

        string sceneName = scaler.gameObject.scene.name ?? "Unknown";
        Debug.Log($"[CanvasScalerConfig] Applied to Canvas '{scaler.name}' in scene '{sceneName}'");
    }

#if UNITY_EDITOR
    [MenuItem("UI/Canvas Scaler/Apply Standard Settings to Scene")]
    private static void MenuApplyToScene()
    {
        ApplyToCurrentScene();
    }

    [MenuItem("UI/Canvas Scaler/Log Canvas Scaler Info")]
    private static void MenuLogCanvasInfo()
    {
        var canvases = Object.FindObjectsOfType<Canvas>();
        Debug.Log($"[CanvasScalerConfig] Found {canvases.Length} Canvas(es):");

        foreach (var canvas in canvases)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                string refResStr = scaler.referenceResolution.ToString();
                string matchStr = scaler.matchWidthOrHeight.ToString("F2");
                string scaleStr = scaler.uiScaleMode.ToString();

                Debug.Log($"  - {canvas.name}: RefRes={refResStr}, Match={matchStr}, ScaleMode={scaleStr}");
            }
            else
            {
                Debug.Log($"  - {canvas.name}: No CanvasScaler found");
            }
        }
    }
#endif
}
