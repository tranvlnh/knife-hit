using UnityEngine;

// ReSharper disable once CheckNamespace
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _appliedArea;
    private Vector2Int _appliedScreenSize;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        Apply();
    }

    // Tai thỏ và thanh điều hướng có thể đổi khi xoay hoặc khi app quay lại từ nền.
    private void Update()
    {
        if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreenSize.x ||
            Screen.height != _appliedScreenSize.y)
        {
            Apply();
        }
    }

    private void Apply()
    {
        _appliedArea = Screen.safeArea;
        _appliedScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        // Trong Editor, safe area có thể lấy từ Device Simulator trong khi Screen lấy từ Game view; khi đó dùng toàn màn hình.
        var screenSize = new Vector2(Screen.width, Screen.height);
        var area = _appliedArea;
        if (area.xMax > screenSize.x + 1f || area.yMax > screenSize.y + 1f)
        {
            area = new Rect(Vector2.zero, screenSize);
        }

        _rectTransform.anchorMin = area.position / screenSize;
        _rectTransform.anchorMax = (area.position + area.size) / screenSize;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
