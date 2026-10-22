using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
public enum WindowState
{
    Idle,
    Telegraph,
    Active
}

// ReSharper disable once CheckNamespace
[Serializable]
public struct BossWindow
{
    [Tooltip("Giây (trong chu kỳ cửa sổ) bắt đầu cửa sổ")]
    [Min(0f)] public float start;
    [Min(0.01f)] public float duration;
    [Tooltip("Chỉ dùng cho Wide Zone: hệ số nhân blockAngle khi cửa sổ đang hoạt động")]
    [Min(1f)] public float blockAngleScale;
}

// ReSharper disable once CheckNamespace
// Một pha của boss: một StageConfig cùng các cửa sổ luật theo StageTime của bia (xác định, đóng băng khi tạm dừng).
[Serializable]
public class BossPhase
{
    public StageConfig stage;
    [Tooltip("Độ dài chu kỳ (giây) lặp lại các cửa sổ")]
    [Min(1f)] public float windowCycle = 6f;
    [Tooltip("Thời gian báo hiệu trước mỗi cửa sổ; nên lớn hơn thời gian dao bay (~0.4 giây)")]
    [Min(0.5f)] public float telegraphSeconds = 1.2f;
    [Tooltip("Guard: trong cửa sổ này mọi lần chạm ném bị bỏ qua")]
    public BossWindow[] guardWindows = { };
    [Tooltip("Wide Zone: trong cửa sổ này vùng chặn của dao đã cắm rộng hơn")]
    public BossWindow[] wideZoneWindows = { };
    public string bannerText = "";

    public WindowState GetGuardState(float stageTime)
    {
        return Evaluate(guardWindows, stageTime, out _);
    }

    public WindowState GetWideZoneState(float stageTime)
    {
        return Evaluate(wideZoneWindows, stageTime, out _);
    }

    // Hệ số nhân blockAngle tại thời điểm stageTime; 1 khi không có cửa sổ Wide Zone nào đang hoạt động.
    public float GetBlockAngleScale(float stageTime)
    {
        return Evaluate(wideZoneWindows, stageTime, out var scale) == WindowState.Active
            ? Mathf.Max(1f, scale)
            : 1f;
    }

    private WindowState Evaluate(BossWindow[] windows, float stageTime, out float scale)
    {
        scale = 1f;
        var state = WindowState.Idle;
        var time = Mathf.Repeat(stageTime, windowCycle);
        foreach (var window in windows)
        {
            if (Mathf.Repeat(time - window.start, windowCycle) < window.duration)
            {
                scale = window.blockAngleScale;
                return WindowState.Active;
            }

            var timeUntilStart = Mathf.Repeat(window.start - time, windowCycle);
            if (timeUntilStart <= telegraphSeconds)
            {
                state = WindowState.Telegraph;
            }
        }

        return state;
    }
}

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "Boss", menuName = "Knife Hit/Boss")]
public class BossConfig : ScriptableObject
{
    [SerializeField] private string displayName = "Boss";
    [Tooltip("Hình bia riêng của boss; bỏ trống thì dùng hình bia mặc định (tô màu theo targetTint)")]
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private Color targetTint = Color.white;
    [SerializeField] private BossPhase[] phases = { };

    public string DisplayName => displayName;
    public Sprite TargetSprite => targetSprite;
    public Color TargetTint => targetTint;
    public int PhaseCount => phases.Length;

    public BossPhase GetPhase(int index)
    {
        return phases[index];
    }
}
