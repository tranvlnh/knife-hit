using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text knivesText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private UnityEngine.UI.Button replayButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [Tooltip("Mỗi nút đổi cùng một cài đặt âm thanh; nhãn là TMP_Text con của nút")]
    [SerializeField] private UnityEngine.UI.Button[] soundButtons = { };

    private const string SoundEnabledKey = "SoundEnabled";

    private bool _soundEnabled;

    private void Awake()
    {
        if (!session || !stageText || !knivesText || !resultPanel || !resultTitleText || !replayButton ||
            !pausePanel || !continueButton || !startPanel || !playButton)
        {
            Debug.LogError("Assign the GameSession and every HUD element.", this);
            enabled = false;
            return;
        }

        replayButton.onClick.AddListener(session.StartRun);
        continueButton.onClick.AddListener(session.Resume);
        playButton.onClick.AddListener(session.Play);

        _soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        ApplySound();
        foreach (var button in soundButtons)
        {
            button.onClick.AddListener(ToggleSound);
        }
    }

    private void OnEnable()
    {
        session.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        session.Changed -= Refresh;
    }

    private void ToggleSound()
    {
        _soundEnabled = !_soundEnabled;
        PlayerPrefs.SetInt(SoundEnabledKey, _soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySound();
    }

    private void ApplySound()
    {
        AudioListener.volume = _soundEnabled ? 1f : 0f;
        foreach (var button in soundButtons)
        {
            button.GetComponentInChildren<TMP_Text>(true).text = _soundEnabled ? "SOUND: ON" : "SOUND: OFF";
        }
    }

    private void Refresh()
    {
        stageText.text = $"STAGE {session.StageNumber}/{session.StageCount}";
        knivesText.text = $"x{session.KnivesRemaining}";

        startPanel.SetActive(session.CurrentState == GameSession.State.Menu);
        pausePanel.SetActive(session.CurrentState == GameSession.State.Paused);

        var isFinished = session.CurrentState is GameSession.State.GameOver or GameSession.State.Victory;
        resultPanel.SetActive(isFinished);
        if (isFinished)
        {
            resultTitleText.text = session.CurrentState == GameSession.State.Victory ? "VICTORY" : "GAME OVER";
        }
    }
}
