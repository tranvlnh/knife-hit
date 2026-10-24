using UnityEngine;

// ReSharper disable once CheckNamespace
// Xoay chậm một phần trang trí UI (bia ở màn Home); dùng thời gian không co giãn nên không phụ thuộc timeScale.
public class SlowSpin : MonoBehaviour
{
    [Tooltip("Độ mỗi giây; số âm xoay ngược chiều")]
    [SerializeField] private float degreesPerSecond = -24f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
    }
}
