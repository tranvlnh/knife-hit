using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text knivesText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private UnityEngine.UI.Button replayButton;

    private void Awake()
    {
        if (!session || !scoreText || !knivesText || !resultPanel || !resultTitleText || !resultScoreText ||
            !replayButton)
        {
            Debug.LogError("Assign the GameSession and every HUD element.", this);
            enabled = false;
            return;
        }

        replayButton.onClick.AddListener(session.StartRun);
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

    private void Refresh()
    {
        scoreText.text = session.RunScore.ToString();
        knivesText.text = $"x{session.KnivesRemaining}";

        var isFinished = session.CurrentState != GameSession.State.Playing;
        resultPanel.SetActive(isFinished);
        if (isFinished)
        {
            resultTitleText.text = session.CurrentState == GameSession.State.Victory ? "VICTORY" : "GAME OVER";
            resultScoreText.text = $"Score {session.RunScore}";
        }
    }
}
