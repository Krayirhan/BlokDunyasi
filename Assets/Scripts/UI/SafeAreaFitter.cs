using UnityEngine;

/// <summary>
/// Applies Screen.safeArea as RectTransform anchors to handle notches, dynamic islands, and gesture bars.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [Header("Edge Padding (in pixels)")]
    [SerializeField] private float topPadding = 0f;
    [SerializeField] private float bottomPadding = 0f;
    [SerializeField] private float leftPadding = 0f;
    [SerializeField] private float rightPadding = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmo = false;
    [SerializeField] private Color debugGizmoColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color debugBoundsColor = new Color(1f, 1f, 0f, 0.2f);

    private Rect _lastSafeArea;
    private int _lastWidth;
    private int _lastHeight;
    private RectTransform _rectTransform;

    private void OnEnable()
    {
        _rectTransform = transform as RectTransform;
        ApplySafeArea(force: true);
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight || Screen.safeArea != _lastSafeArea)
            ApplySafeArea(force: false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        ApplySafeArea(force: false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _rectTransform = transform as RectTransform;
    }
#endif

    /// <summary>
    /// Force immediate safe area recalculation.
    /// </summary>
    public void ForceRefresh()
    {
        ApplySafeArea(force: true);
    }

    private void ApplySafeArea(bool force)
    {
        Rect safeArea = Screen.safeArea;
        if (!force &&
            Screen.width == _lastWidth &&
            Screen.height == _lastHeight &&
            safeArea == _lastSafeArea)
        {
            return;
        }

        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_rectTransform == null)
            return;

        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);

        // Apply safe area with edge padding
        float anchorMinX = (safeArea.xMin + leftPadding) / width;
        float anchorMinY = (safeArea.yMin + bottomPadding) / height;
        float anchorMaxX = (safeArea.xMax - rightPadding) / width;
        float anchorMaxY = (safeArea.yMax - topPadding) / height;

        // Clamp to valid range
        anchorMinX = Mathf.Clamp01(anchorMinX);
        anchorMinY = Mathf.Clamp01(anchorMinY);
        anchorMaxX = Mathf.Clamp01(anchorMaxX);
        anchorMaxY = Mathf.Clamp01(anchorMaxY);

        _rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        _rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localScale = Vector3.one;
        _rectTransform.localRotation = Quaternion.identity;

        _lastSafeArea = safeArea;
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmo || !isActiveAndEnabled)
            return;

        var rt = transform as RectTransform;
        if (rt == null)
            return;

        // Draw safe area bounds
        Rect safeArea = Screen.safeArea;
        Gizmos.color = debugGizmoColor;
        // Safe area rect in screen space
        Vector3 safeAreaMin = new Vector3(safeArea.xMin, safeArea.yMin, 0f);
        Vector3 safeAreaMax = new Vector3(safeArea.xMax, safeArea.yMax, 0f);
        Gizmos.DrawLine(new Vector3(safeArea.xMin, safeArea.yMin, 0), new Vector3(safeArea.xMax, safeArea.yMin, 0));
        Gizmos.DrawLine(new Vector3(safeArea.xMax, safeArea.yMin, 0), new Vector3(safeArea.xMax, safeArea.yMax, 0));
        Gizmos.DrawLine(new Vector3(safeArea.xMax, safeArea.yMax, 0), new Vector3(safeArea.xMin, safeArea.yMax, 0));
        Gizmos.DrawLine(new Vector3(safeArea.xMin, safeArea.yMax, 0), new Vector3(safeArea.xMin, safeArea.yMin, 0));
    }
#endif
}
