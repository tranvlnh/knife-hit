using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameSession : MonoBehaviour
{
    public enum State
    {
        Playing,
        GameOver,
        Victory
    }

    [SerializeField] private KnifeLauncher launcher;
    [SerializeField] private RotatingTarget target;
    [SerializeField, Min(1)] private int requiredKnives = 7;
    [Tooltip("Các dao cắm sẵn")]
    [SerializeField] private float[] initialKnifeAngles = { 90f, 210f };

    // Chỉ dành cho HUD và feedback; kết quả gameplay luôn đi qua các lời gọi trực tiếp bên dưới.
    public event Action Changed;

    public State CurrentState { get; private set; }
    public int RunScore { get; private set; }
    public int KnivesRemaining { get; private set; }

    private void Start()
    {
        if (!launcher || !launcher.isActiveAndEnabled || !target)
        {
            Debug.LogError("Assign an enabled KnifeLauncher and the RotatingTarget.", this);
            enabled = false;
            return;
        }

        StartRun();
    }

    [ContextMenu("Replay")]
    public void StartRun()
    {
        RunScore = 0;
        StartStage();
    }

    // KnifeLauncher gọi khi nhận một lần chạm; trả về false nghĩa là bỏ qua lần chạm đó.
    public bool TryAcceptThrow()
    {
        if (CurrentState != State.Playing || KnivesRemaining == 0)
        {
            return false;
        }

        KnivesRemaining--;
        NotifyChanged();
        return true;
    }

    public void HandleSuccessfulThrow()
    {
        RunScore++;
        if (KnivesRemaining > 0)
        {
            launcher.SpawnReadyKnife();
        }
        else
        {
            CurrentState = State.Victory;
        }

        NotifyChanged();
    }

    public void HandleKnifeHitKnife()
    {
        CurrentState = State.GameOver;
        NotifyChanged();
    }

    private void StartStage()
    {
        target.ClearEmbeddedKnives();
        foreach (var angle in initialKnifeAngles)
        {
            launcher.SpawnEmbeddedKnife(angle);
        }

        KnivesRemaining = requiredKnives;
        CurrentState = State.Playing;
        launcher.ResetForStage();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        // Tạm thời thay cho HUD khi duyệt thủ công.
        Debug.Log($"{CurrentState}: score {RunScore}, knives {KnivesRemaining}", this);
        Changed?.Invoke();
    }
}
