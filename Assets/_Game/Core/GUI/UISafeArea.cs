using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class UISafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea;
    private ScreenOrientation _lastOrientation;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea || _lastOrientation != Screen.orientation)
            Apply();
    }

    private void Apply()
    {
        Rect safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;
        _lastOrientation = Screen.orientation;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
