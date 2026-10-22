using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class RotatingTarget : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField, Min(0.01f)] private float radius = 1.5f;
    [Tooltip("An embedded knife closer than this angle to the throw line blocks the flying knife.")]
    [SerializeField, Range(0.1f, 30f)] private float blockAngle = 2.75f;

    // Angle is target-local (0 = straight down at zero rotation); butt distance is measured from the center.
    private readonly List<(float angle, float buttDistance)> _embeddedKnives = new();
    private float _angle;

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

    // Finds the lowest butt among embedded knives that point at the throw line and that a tip at tipY has reached.
    public bool TryGetBlockingKnife(float tipY, out float buttY)
    {
        buttY = float.PositiveInfinity;
        var centerY = transform.position.y;
        foreach (var (angle, buttDistance) in _embeddedKnives)
        {
            var knifeButtY = centerY - buttDistance;
            if (tipY >= knifeButtY && knifeButtY < buttY && IsBlocking(angle))
            {
                buttY = knifeButtY;
            }
        }

        return buttY < float.PositiveInfinity;
    }

    // The knife must hang straight down under the center.
    public void EmbedKnife(Transform knife, float buttY)
    {
        knife.SetParent(transform, true);
        _embeddedKnives.Add((-_angle, transform.position.y - buttY));
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

        // The block zone stays fixed around the throw line while the target rotates.
        var zoneLength = radius * 2f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + Quaternion.Euler(0f, 0f, blockAngle) * Vector3.down * zoneLength);
        Gizmos.DrawLine(center, center + Quaternion.Euler(0f, 0f, -blockAngle) * Vector3.down * zoneLength);

        // Exposed part of each embedded knife; red while it blocks the throw line.
        foreach (var (angle, buttDistance) in _embeddedKnives)
        {
            var direction = Quaternion.Euler(0f, 0f, angle + _angle) * Vector3.down;
            Gizmos.color = IsBlocking(angle) ? Color.red : Color.green;
            Gizmos.DrawLine(center + direction * radius, center + direction * buttDistance);
        }
    }
}
