using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameSession : MonoBehaviour
{
    public enum State
    {
        Playing,
        Transition,
        Paused,
        GameOver,
        Victory
    }

    [SerializeField] private KnifeLauncher launcher;
    [SerializeField] private RotatingTarget target;
    [SerializeField] private PlayerProgress progress;
    [Tooltip("Hiển thị màu bia theo World/boss; có thể bỏ trống")]
    [SerializeField] private BossEncounter encounter;
    [Tooltip("Nền theo World; có thể bỏ trống")]
    [SerializeField] private SpriteRenderer background;
    [Tooltip("Thời gian (giây) dừng giữa hai stage")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.8f;
    [Tooltip("Sau khi bắt đầu Run hoặc stage mới, bỏ qua chạm trong khoảng này để một lần chạm không vừa bấm nút vừa ném")]
    [SerializeField, Min(0f)] private float inputLockSeconds = 0.15f;

    private State _stateBeforePause;
    private LevelConfig _level;
    private int _stageIndex;
    private float _transitionTimeLeft;
    private float _inputUnlockTime;

    // Chỉ dành cho HUD và feedback; kết quả gameplay luôn đi qua các lời gọi trực tiếp bên dưới.
    public event Action Changed;

    public State CurrentState { get; private set; }
    public int KnivesRemaining { get; private set; }
    public LevelConfig CurrentLevel => _level;
    public int StageNumber => _stageIndex + 1;
    public int StageCount => _level ? _level.StageCount : 0;
    public float RunSeconds { get; private set; }
    public PlayerProgress.WinResult LastResult { get; private set; }

    private void Awake()
    {
        // Android mặc định 30 FPS và tự tắt màn hình khi không chạm; cả hai đều làm hỏng cảm giác ném.
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Start()
    {
        if (!launcher || !launcher.isActiveAndEnabled || !target || !progress || !progress.isActiveAndEnabled)
        {
            Debug.LogError("Assign an enabled KnifeLauncher, PlayerProgress and the RotatingTarget.", this);
            enabled = false;
            return;
        }

        // Level do menu chọn; bấm Play trực tiếp scene này trong Editor thì chơi Level hiện tại.
        _level = SceneFlow.TakePendingLevel(progress.Campaign);
        if (!_level || !progress.IsLevelUnlocked(_level))
        {
            _level = progress.CurrentLevel;
        }

        StartRun();
    }

    private void Update()
    {
        if (CurrentState == State.Playing)
        {
            RunSeconds += Time.deltaTime;
        }
        else if (CurrentState == State.Transition)
        {
            _transitionTimeLeft -= Time.deltaTime;
            if (_transitionTimeLeft <= 0f)
            {
                _stageIndex++;
                StartStage(State.Playing);
            }
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Pause();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Trong Editor, bấm ra ngoài Game view cũng mất focus nên chỉ áp dụng trên thiết bị.
        if (!hasFocus && !Application.isEditor)
        {
            Pause();
        }
    }

    // Có thể gọi nhiều lần liên tiếp; chỉ lần đầu trong Playing hoặc Transition có tác dụng.
    [ContextMenu("Pause")]
    public void Pause()
    {
        if (CurrentState is not (State.Playing or State.Transition))
        {
            return;
        }

        _stateBeforePause = CurrentState;
        CurrentState = State.Paused;
        NotifyChanged();
    }

    [ContextMenu("Resume")]
    public void Resume()
    {
        if (CurrentState != State.Paused)
        {
            return;
        }

        CurrentState = _stateBeforePause;
        NotifyChanged();
    }

    // Rời scene GamePlay; trang menu mở ra phụ thuộc nơi người chơi đi tiếp.
    public void ExitToMenu(SceneFlow.MenuPage page)
    {
        SceneFlow.OpenMainMenu(page);
    }

    // Bắt đầu một Run mới ở Level đã mở; Level khóa bị bỏ qua.
    public void StartLevel(LevelConfig level)
    {
        if (!level || !progress.IsLevelUnlocked(level))
        {
            return;
        }

        _level = level;
        StartRun();
    }

    public void StartNextLevel()
    {
        var next = _level ? progress.GetNextLevel(_level) : null;
        if (next)
        {
            StartLevel(next);
        }
    }

    // Chơi lại Level hiện tại từ stage đầu (Retry/Replay).
    [ContextMenu("Replay")]
    public void StartRun()
    {
        if (!_level)
        {
            return;
        }

        _stageIndex = 0;
        RunSeconds = 0f;
        LastResult = default;
        StartStage(State.Playing);
    }

    // KnifeLauncher gọi khi nhận một lần chạm; trả về false nghĩa là bỏ qua lần chạm đó.
    public bool TryAcceptThrow()
    {
        if (CurrentState != State.Playing || KnivesRemaining == 0 || Time.unscaledTime < _inputUnlockTime ||
            target.IsThrowGated)
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
        else if (_stageIndex < _level.StageCount - 1)
        {
            // Dao cuối vẫn cắm trên bia cho tới khi stage sau xóa nó.
            CurrentState = State.Transition;
            _transitionTimeLeft = transitionDuration;
        }
        else
        {
            // Mỗi Run chỉ ghi thưởng đúng một lần, vào lúc chuyển sang Victory.
            LastResult = progress.RecordWin(_level, _level.CalculateStars(RunSeconds), RunSeconds);
            CurrentState = State.Victory;
        }

        NotifyChanged();
    }

    public void HandleKnifeHitKnife()
    {
        CurrentState = State.GameOver;
        NotifyChanged();
    }

    private void StartStage(State initialState)
    {
        var stage = _level.GetStage(_stageIndex);
        var phase = _level.GetPhase(_stageIndex);
        target.BeginStage(stage, phase);
        ApplyTheme();
        for (var i = 0; i < stage.InitialKnifeCount; i++)
        {
            launcher.SpawnEmbeddedKnife(stage.GetInitialKnifeAngle(i));
        }

        KnivesRemaining = stage.RequiredKnives;
        CurrentState = initialState;
        _inputUnlockTime = Time.unscaledTime + inputLockSeconds;
        launcher.ResetForStage();
        NotifyChanged();
    }

    private void ApplyTheme()
    {
        var world = _level.World;
        if (background && world)
        {
            if (world.Background)
            {
                background.sprite = world.Background;
            }

            background.color = world.BackgroundTint;
        }

        if (encounter)
        {
            var tint = world ? world.TargetTint : Color.white;
            var boss = _level.IsBoss ? _level.Boss : null;
            var bossSprite = boss ? boss.TargetSprite : null;
            encounter.SetTargetSprite(bossSprite);
            // Hình boss riêng đã có màu sẵn, nên không nhân thêm tint của World/boss.
            encounter.SetBaseColor(bossSprite ? Color.white : boss ? tint * boss.TargetTint : tint);
        }
    }

    private void NotifyChanged()
    {
        LogState();
        Changed?.Invoke();
    }

    // Chỉ chạy trong Editor để bản build không cấp phát chuỗi ở mỗi lần ném.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogState()
    {
        Debug.Log($"{CurrentState}: {(_level ? _level.DisplayName : "-")} stage {StageNumber}/{StageCount}, knives {KnivesRemaining}", this);
    }
}
