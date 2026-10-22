using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private BossEncounter encounter;
    [SerializeField] private MenuScreens menu;
    [Tooltip("Chứa các thành phần chỉ hiện khi đang chơi (tên Level, số dao, cảnh báo boss)")]
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text knivesText;
    [SerializeField] private Image knifeIcon;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField, Min(0.5f)] private float hintSeconds = 4f;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDetailText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;
    [Tooltip("Nút Pause trong lúc chơi")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button pauseRetryButton;
    [SerializeField] private Button pauseMapButton;
    [Tooltip("Mỗi nút đổi cùng một cài đặt âm thanh; nhãn là TMP_Text con của nút")]
    [SerializeField] private Button[] soundButtons = { };
    [Tooltip("Có thể bỏ trống: đồng hồ Run kèm mốc sao kế tiếp")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("Chờ trước khi hiện panel kết quả để người chơi kịp thấy dao va chạm / ăn mừng")]
    [SerializeField, Min(0f)] private float failResultDelay = 0.6f;
    [SerializeField, Min(0f)] private float winResultDelay = 0.5f;

    private float _hintTimeLeft;
    private float _resultDelayLeft;
    private bool _wasFinished;
    private string _lastWarning = "";
    private int _lastTimerTenths = -1;

    private void Awake()
    {
        if (!session || !progress || !encounter || !menu || !gameplayRoot || !levelText || !knivesText || !knifeIcon ||
            !warningText || !hintPanel || !hintText || !resultPanel || !resultTitleText || !resultDetailText ||
            !retryButton || !nextButton || !mapButton || !pausePanel || !continueButton || !pauseButton || !pauseRetryButton || !pauseMapButton)
        {
            Debug.LogError("Assign the GameSession, PlayerProgress and every HUD element.", this);
            enabled = false;
            return;
        }

        retryButton.onClick.AddListener(session.StartRun);
        nextButton.onClick.AddListener(session.StartNextLevel);
        mapButton.onClick.AddListener(OpenMap);
        continueButton.onClick.AddListener(session.Resume);
        pauseButton.onClick.AddListener(session.Pause);
        pauseRetryButton.onClick.AddListener(session.StartRun);
        pauseMapButton.onClick.AddListener(OpenMap);
        foreach (var button in soundButtons)
        {
            button.onClick.AddListener(ToggleSound);
        }
    }

    private void OnEnable()
    {
        session.Changed += Refresh;
        progress.Changed += RefreshSound;
        RefreshSound();
        Refresh();
    }

    private void OnDisable()
    {
        session.Changed -= Refresh;
        progress.Changed -= RefreshSound;
    }

    private void Update()
    {
        var playing = session.CurrentState == GameSession.State.Playing;
        var warning = playing ? encounter.Warning : "";
        if (warning != _lastWarning)
        {
            _lastWarning = warning;
            warningText.text = warning;
        }

        UpdateTimer();
        HandleBackKey();

        if (_resultDelayLeft > 0f)
        {
            _resultDelayLeft -= Time.unscaledDeltaTime;
            if (_resultDelayLeft <= 0f)
            {
                _resultDelayLeft = 0f;
                resultPanel.SetActive(true);
            }
        }

        if (_hintTimeLeft > 0f)
        {
            _hintTimeLeft -= Time.unscaledDeltaTime;
            if (_hintTimeLeft <= 0f || session.CurrentState == GameSession.State.Menu)
            {
                _hintTimeLeft = 0f;
                hintPanel.SetActive(false);
            }
        }
    }

    // Hiện giây đã chơi và mốc sao kế tiếp; chỉ cập nhật chữ khi đổi phần mười giây.
    private void UpdateTimer()
    {
        var level = session.CurrentLevel;
        if (!timerText || !level)
        {
            return;
        }

        var tenths = Mathf.FloorToInt(session.RunSeconds * 10f);
        if (tenths == _lastTimerTenths)
        {
            return;
        }

        _lastTimerTenths = tenths;
        var seconds = session.RunSeconds;
        var goal = seconds <= level.ThreeStarSeconds ? level.ThreeStarSeconds
            : seconds <= level.TwoStarSeconds ? level.TwoStarSeconds
            : 0f;
        var stars = goal > 0f ? (seconds <= level.ThreeStarSeconds ? 3 : 2) : 1;
        timerText.text = goal > 0f
            ? $"{seconds:0.0}s  ({stars} STARS UNTIL {goal:0}s)"
            : $"{seconds:0.0}s  (1 STAR)";
    }

    // Nút Back của Android được Input System báo như phím Escape.
    private void HandleBackKey()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        switch (session.CurrentState)
        {
            case GameSession.State.Playing:
            case GameSession.State.Transition:
                session.Pause();
                break;
            case GameSession.State.Paused:
                session.Resume();
                break;
            case GameSession.State.GameOver:
            case GameSession.State.Victory:
                OpenMap();
                break;
        }
    }

    private void OpenMap()
    {
        menu.ShowMap();
        session.ReturnToMenu();
    }

    private void ToggleSound()
    {
        progress.SetSoundEnabled(!progress.SoundEnabled);
    }

    private void RefreshSound()
    {
        foreach (var button in soundButtons)
        {
            button.GetComponentInChildren<TMP_Text>(true).text = progress.SoundEnabled ? "SOUND: ON" : "SOUND: OFF";
        }
    }

    private void Refresh()
    {
        var state = session.CurrentState;
        var level = session.CurrentLevel;
        gameplayRoot.SetActive(state != GameSession.State.Menu);
        pausePanel.SetActive(state == GameSession.State.Paused);
        pauseButton.gameObject.SetActive(state is GameSession.State.Playing or GameSession.State.Transition);

        if (level)
        {
            levelText.text = level.IsBoss
                ? $"{level.DisplayName}  PHASE {session.StageNumber}/{session.StageCount}"
                : level.DisplayName;
        }

        knivesText.text = $"x{session.KnivesRemaining}";
        knifeIcon.sprite = progress.EquippedSkin.Sprite;
        knifeIcon.color = progress.EquippedSkin.Tint;
        ShowHintIfNeeded(state, level);

        var isFinished = state is GameSession.State.GameOver or GameSession.State.Victory;
        if (isFinished && !_wasFinished)
        {
            // Chỉ trì hoãn một lần khi vừa kết thúc; các lần Changed sau đó không đặt lại bộ đếm.
            _resultDelayLeft = state == GameSession.State.Victory ? winResultDelay : failResultDelay;
        }
        else if (!isFinished)
        {
            _resultDelayLeft = 0f;
        }

        _wasFinished = isFinished;
        resultPanel.SetActive(isFinished && _resultDelayLeft <= 0f);
        if (isFinished)
        {
            ShowResult(state == GameSession.State.Victory, level);
        }
    }

    // Gợi ý chỉ hiện một lần, ở lần đầu bắt đầu Level có hint; khi ghi nhận đã xem thì không hiện lại.
    private void ShowHintIfNeeded(GameSession.State state, LevelConfig level)
    {
        if (state != GameSession.State.Playing || !level || session.StageNumber != 1 ||
            string.IsNullOrEmpty(level.HintText) || progress.HasSeenHint(level.HintId))
        {
            return;
        }

        hintText.text = level.HintText;
        hintPanel.SetActive(true);
        _hintTimeLeft = hintSeconds;
        progress.MarkHintSeen(level.HintId);
    }

    private void ShowResult(bool won, LevelConfig level)
    {
        var next = won && level ? progress.GetNextLevel(level) : null;
        nextButton.gameObject.SetActive(next);
        if (!won)
        {
            resultTitleText.text = "FAILED";
            resultDetailText.text = "You hit a knife.";
            return;
        }

        var result = session.LastResult;
        resultTitleText.text = !next ? "CAMPAIGN COMPLETE" : level.IsBoss ? "BOSS DEFEATED" : "LEVEL CLEARED";
        var detail = $"STARS {result.Stars}/3   TIME {result.Seconds:0.0}s\n+{result.CoinsEarned} COINS";
        if (result.UnlockedSkin)
        {
            detail += $"\nNEW KNIFE: {result.UnlockedSkin.DisplayName}";
        }

        resultDetailText.text = detail;
    }
}
