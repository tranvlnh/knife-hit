using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class RotatingTarget : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField, Min(0.01f)] private float radius = 1.5f;
    [Tooltip("Độ lệch chặn dao")]
    [SerializeField, Range(0.1f, 30f)] private float blockAngle = 2.75f;

    // Góc tính theo hệ tọa độ của bia (0 = chĩa thẳng xuống khi bia chưa quay); buttDistance là khoảng cách từ tâm bia đến đuôi dao.
    private readonly List<(Transform knife, float angle, float buttDistance)> _embeddedKnives = new();
    private float _angle;

    public float Radius => radius;
    public float SurfaceY => transform.position.y - radius;
    public float MaximumStepTime => Mathf.Min(1f / 120f, blockAngle / Mathf.Max(1f, Mathf.Abs(speed)));

    private void Awake()
    {
        _angle = transform.eulerAngles.z;
    }

    public void AdvanceRotation(float deltaTime)
    {
        _angle = Mathf.Repeat(_angle + speed * deltaTime, 360f);
        transform.rotation = Quaternion.Euler(0f, 0f, _angle);
    }

    // Tìm đuôi dao thấp nhất trong các dao đã cắm nằm trên đường ném mà mũi dao ở độ cao tipY đã chạm tới.
    public bool TryGetBlockingKnife(float tipY, out float buttY)
    {
        buttY = float.PositiveInfinity;
        var centerY = transform.position.y;
        foreach (var (_, angle, buttDistance) in _embeddedKnives)
        {
            var knifeButtY = centerY - buttDistance;
            if (tipY >= knifeButtY && knifeButtY < buttY && IsBlocking(angle))
            {
                buttY = knifeButtY;
            }
        }

        return buttY < float.PositiveInfinity;
    }

    // Dao phải đang ở ngay dưới tâm bia, chĩa thẳng xuống.
    public void EmbedKnife(Transform knife, float buttY)
    {
        knife.SetParent(transform, true);
        _embeddedKnives.Add((knife, -_angle, transform.position.y - buttY));
    }

    // Cắm dao ở góc cục bộ bất kỳ, mũi dao hướng vào tâm; dùng cho dao cắm sẵn đầu stage.
    public void AddEmbeddedKnife(Transform knife, float localAngle, float pivotDistance, float buttDistance)
    {
        knife.SetParent(transform, false);
        knife.localPosition = Quaternion.Euler(0f, 0f, localAngle) * Vector3.down * pivotDistance;
        knife.localRotation = Quaternion.Euler(0f, 0f, localAngle);
        _embeddedKnives.Add((knife, localAngle, buttDistance));
    }

    public void ClearEmbeddedKnives()
    {
        foreach (var (knife, _, _) in _embeddedKnives)
        {
            if (knife)
            {
                Destroy(knife.gameObject);
            }
        }

        _embeddedKnives.Clear();
    }

    private bool IsBlocking(float localAngle)
    {
        return Mathf.Abs(Mathf.DeltaAngle(0f, localAngle + _angle)) < blockAngle;
    }

    private void OnDrawGizmos()
    {
        var center = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, radius);

        // Vùng chặn đứng yên quanh đường ném khi bia quay.
        var zoneLength = radius * 2f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + Quaternion.Euler(0f, 0f, blockAngle) * Vector3.down * zoneLength);
        Gizmos.DrawLine(center, center + Quaternion.Euler(0f, 0f, -blockAngle) * Vector3.down * zoneLength);

        // Phần lộ ra ngoài gỗ của mỗi dao đã cắm; màu đỏ khi đang chặn đường ném.
        foreach (var (_, angle, buttDistance) in _embeddedKnives)
        {
            var direction = Quaternion.Euler(0f, 0f, angle + _angle) * Vector3.down;
            Gizmos.color = IsBlocking(angle) ? Color.red : Color.green;
            Gizmos.DrawLine(center + direction * radius, center + direction * buttDistance);
        }
    }
}
