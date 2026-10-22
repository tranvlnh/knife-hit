using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "Stage", menuName = "Knife Hit/Stage Config")]
public class StageConfig : ScriptableObject
{
    [Serializable]
    public struct RotationSegment
    {
        [Tooltip("Tốc độ quay (độ/giây); âm là quay ngược chiều")]
        public float speed;
        [Min(0.01f)] public float duration;
        [Tooltip("Thời gian chuyển mượt từ tốc độ của đoạn trước sang tốc độ của đoạn này, tính từ đầu đoạn")]
        [Min(0f)] public float blendTime;
    }

    [SerializeField, Min(1)] private int requiredKnives = 7;
    [Tooltip("Góc cục bộ của các dao cắm sẵn")]
    [SerializeField] private float[] initialKnifeAngles = { };
    [Tooltip("Các đoạn quay lặp lại theo chu kỳ; một đoạn duy nhất là quay đều")]
    [SerializeField] private RotationSegment[] rotationSegments = { new() { speed = 90f, duration = 1f } };

    public int RequiredKnives => requiredKnives;
    public int InitialKnifeCount => initialKnifeAngles.Length;
    public float CycleDuration { get; private set; }
    public float MaximumAbsoluteSpeed { get; private set; }

    public float GetInitialKnifeAngle(int index)
    {
        return initialKnifeAngles[index];
    }

    // Tốc độ tại thời điểm stageTime kể từ đầu stage; hàm thuần nên không làm thay đổi asset.
    public float GetSpeed(float stageTime)
    {
        if (rotationSegments.Length == 0)
        {
            return 0f;
        }

        var time = Mathf.Repeat(stageTime, CycleDuration);
        for (var i = 0; i < rotationSegments.Length; i++)
        {
            var segment = rotationSegments[i];
            if (time < segment.duration || i == rotationSegments.Length - 1)
            {
                if (time >= segment.blendTime)
                {
                    return segment.speed;
                }

                // Đoạn đầu chuyển từ đoạn cuối để chu kỳ lặp lại liền mạch.
                var previous = rotationSegments[(i - 1 + rotationSegments.Length) % rotationSegments.Length];
                return Mathf.Lerp(previous.speed, segment.speed, Mathf.SmoothStep(0f, 1f, time / segment.blendTime));
            }

            time -= segment.duration;
        }

        return 0f;
    }

    private void OnEnable()
    {
        CacheCycle();
    }

    private void OnValidate()
    {
        for (var i = 0; i < rotationSegments.Length; i++)
        {
            rotationSegments[i].duration = Mathf.Max(0.01f, rotationSegments[i].duration);
            rotationSegments[i].blendTime = Mathf.Clamp(rotationSegments[i].blendTime, 0f, rotationSegments[i].duration);
        }

        CacheCycle();
    }

    private void CacheCycle()
    {
        CycleDuration = 0f;
        MaximumAbsoluteSpeed = 0f;
        foreach (var segment in rotationSegments)
        {
            CycleDuration += segment.duration;
            MaximumAbsoluteSpeed = Mathf.Max(MaximumAbsoluteSpeed, Mathf.Abs(segment.speed));
        }
    }
}
