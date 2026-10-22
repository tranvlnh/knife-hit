using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameSession : MonoBehaviour
{
    public enum State
    {
        Playing,
        Transition,
        GameOver,
        Victory
    }

    [SerializeField] private KnifeLauncher launcher;
    [SerializeField] private RotatingTarget target;
    [Tooltip("Các stage theo thứ tự chơi trong một Run")]
    [SerializeField] private StageConfig[] stages = { };
    [Tooltip("Thời gian (giây) dừng giữa hai stage")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.8f;

    private int _stageIndex;
    private float _transitionTimeLeft;

    // Chỉ dành cho HUD và feedback; kết quả gameplay luôn đi qua các lời gọi trực tiếp bên dưới.
    public event Action Changed;

    public State CurrentState { get; private set; }
    public int KnivesRemaining { get; private set; }
    public int StageNumber => _stageIndex + 1;
    public int StageCount => stages.Length;

    private void Start()
    {
        if (!launcher || !launcher.isActiveAndEnabled || !target || stages.Length == 0 ||
            Array.Exists(stages, stage => !stage))
        {
            Debug.LogError("Assign an enabled KnifeLauncher, the RotatingTarget and every StageConfig.", this);
            enabled = false;
            return;
        }

        StartRun();
    }

    private void Update()
    {
        if (CurrentState != State.Transition)
        {
            return;
        }

        _transitionTimeLeft -= Time.deltaTime;
        if (_transitionTimeLeft <= 0f)
        {
            _stageIndex++;
            StartStage();
        }
    }

    [ContextMenu("Replay")]
    public void StartRun()
    {
        _stageIndex = 0;
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
        if (KnivesRemaining > 0)
        {
            launcher.SpawnReadyKnife();
        }
        else if (_stageIndex < stages.Length - 1)
        {
            // Dao cuối vẫn cắm trên bia cho tới khi stage sau xóa nó.
            CurrentState = State.Transition;
            _transitionTimeLeft = transitionDuration;
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
        var stage = stages[_stageIndex];
        target.BeginStage(stage);
        for (var i = 0; i < stage.InitialKnifeCount; i++)
        {
            launcher.SpawnEmbeddedKnife(stage.GetInitialKnifeAngle(i));
        }

        KnivesRemaining = stage.RequiredKnives;
        CurrentState = State.Playing;
        launcher.ResetForStage();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        // Tạm thời thay cho HUD khi duyệt thủ công.
        Debug.Log($"{CurrentState}: stage {StageNumber}/{StageCount}, knives {KnivesRemaining}", this);
        Changed?.Invoke();
    }
}
