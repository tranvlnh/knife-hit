using UnityEngine;

// ReSharper disable once CheckNamespace
// Chỉ là phần trình bày của luật boss: tô màu bia theo World/boss và báo hiệu Guard / Wide Zone.
// Luật thật nằm ở RotatingTarget (theo StageTime), nên component này không ảnh hưởng gameplay.
public class BossEncounter : MonoBehaviour
{
    [SerializeField] private RotatingTarget target;
    [Tooltip("SpriteRenderer của hình bia (object con Visual)")]
    [SerializeField] private SpriteRenderer targetVisual;
    [SerializeField] private Color guardColor = new(0.35f, 0.8f, 1f);
    [SerializeField] private Color wideZoneColor = new(1f, 0.3f, 0.25f);
    [SerializeField, Min(1f)] private float telegraphFlashHz = 4f;
    [Tooltip("Khiên phủ lên bia khi Guard báo hiệu/hoạt động; có thể bỏ trống")]
    [SerializeField] private SpriteRenderer guardOverlay;
    [Tooltip("Vành đỏ quanh bia khi Danger Zone báo hiệu/hoạt động; có thể bỏ trống")]
    [SerializeField] private SpriteRenderer dangerOverlay;

    private Color _baseColor = Color.white;
    private Sprite _defaultSprite;

    // Báo hiệu đang hiển thị cho HUD; rỗng khi không có cửa sổ nào sắp tới hoặc đang hoạt động.
    public string Warning { get; private set; } = "";

    private void Awake()
    {
        if (!target || !targetVisual)
        {
            Debug.LogError("Assign the RotatingTarget and its Visual SpriteRenderer.", this);
            enabled = false;
            return;
        }

        _defaultSprite = targetVisual.sprite;
        SetOverlay(guardOverlay, 0f);
        SetOverlay(dangerOverlay, 0f);
    }

    // Hình bia của boss; null trả về hình mặc định của bia.
    public void SetTargetSprite(Sprite sprite)
    {
        targetVisual.sprite = sprite ? sprite : _defaultSprite;
    }

    private static void SetOverlay(SpriteRenderer overlay, float alpha)
    {
        if (!overlay)
        {
            return;
        }

        overlay.enabled = alpha > 0.01f;
        var color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }

    public void SetBaseColor(Color color)
    {
        _baseColor = color;
        targetVisual.color = color;
    }

    private void LateUpdate()
    {
        var rules = target.Rules;
        if (rules == null)
        {
            Warning = "";
            targetVisual.color = _baseColor;
            SetOverlay(guardOverlay, 0f);
            SetOverlay(dangerOverlay, 0f);
            return;
        }

        var time = target.StageTime;
        var guard = rules.GetGuardState(time);
        var wide = rules.GetWideZoneState(time);
        var flash = Mathf.PingPong(Time.time * telegraphFlashHz, 1f);
        SetOverlay(guardOverlay, guard switch
        {
            WindowState.Active => 1f,
            WindowState.Telegraph => flash,
            _ => 0f
        });
        SetOverlay(dangerOverlay, wide switch
        {
            WindowState.Active => 1f,
            WindowState.Telegraph => flash,
            _ => 0f
        });

        // Guard quan trọng hơn vì chặn cả lần ném; Wide Zone chỉ làm vùng chặn rộng hơn.
        if (guard != WindowState.Idle)
        {
            Warning = guard == WindowState.Active ? "GUARD UP" : "GUARD INCOMING";
            targetVisual.color = Color.Lerp(_baseColor, guardColor, guard == WindowState.Active ? 0.8f : flash * 0.7f);
        }
        else if (wide != WindowState.Idle)
        {
            Warning = wide == WindowState.Active ? "DANGER ZONE" : "DANGER ZONE INCOMING";
            targetVisual.color = Color.Lerp(_baseColor, wideZoneColor, wide == WindowState.Active ? 0.8f : flash * 0.7f);
        }
        else
        {
            Warning = "";
            targetVisual.color = _baseColor;
        }
    }
}
