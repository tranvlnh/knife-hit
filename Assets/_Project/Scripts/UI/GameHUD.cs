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

    private void Awake()
    {
        if (!session || !stageText || !knivesText || !resultPanel || !resultTitleText || !replayButton)
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
        stageText.text = $"STAGE {session.StageNumber}/{session.StageCount}";
        knivesText.text = $"x{session.KnivesRemaining}";

        var isFinished = session.CurrentState is GameSession.State.GameOver or GameSession.State.Victory;
        resultPanel.SetActive(isFinished);
        if (isFinished)
        {
            resultTitleText.text = session.CurrentState == GameSession.State.Victory ? "VICTORY" : "GAME OVER";
        }
    }
}
