using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Điều hướng Home / Level Map / Knives trong trạng thái Menu của GameSession. Chỉ đọc state và gọi hành động.
public class MenuScreens : MonoBehaviour
{
    private enum Page
    {
        Home,
        Map,
        Knives
    }

    [SerializeField] private GameSession session;
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject knivesPanel;
    [SerializeField] private TMP_Text playLabel;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button knivesButton;
    [SerializeField] private Button[] backButtons = { };
    [SerializeField] private LevelMapScreen mapScreen;
    [SerializeField] private KnifeInventoryScreen inventoryScreen;

    private Page _page;

    private void Awake()
    {
        if (!session || !progress || !menuRoot || !homePanel || !mapPanel || !knivesPanel || !playLabel ||
            !progressText || !playButton || !levelsButton || !knivesButton || !mapScreen || !inventoryScreen)
        {
            Debug.LogError("Assign the GameSession, PlayerProgress and every menu element.", this);
            enabled = false;
            return;
        }

        playButton.onClick.AddListener(() => session.StartLevel(progress.CurrentLevel));
        levelsButton.onClick.AddListener(ShowMap);
        knivesButton.onClick.AddListener(ShowKnives);
        foreach (var button in backButtons)
        {
            button.onClick.AddListener(ShowHome);
        }
    }

    private void OnEnable()
    {
        session.Changed += Refresh;
        progress.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        session.Changed -= Refresh;
        progress.Changed -= Refresh;
    }

    public void ShowHome()
    {
        _page = Page.Home;
        Refresh();
    }

    public void ShowMap()
    {
        _page = Page.Map;
        mapScreen.SelectWorldOf(progress.CurrentLevel);
        Refresh();
    }

    public void ShowKnives()
    {
        _page = Page.Knives;
        Refresh();
    }

    private void Refresh()
    {
        var inMenu = session.CurrentState == GameSession.State.Menu;
        menuRoot.SetActive(inMenu);
        homePanel.SetActive(_page == Page.Home);
        mapPanel.SetActive(_page == Page.Map);
        knivesPanel.SetActive(_page == Page.Knives);
        if (!inMenu)
        {
            return;
        }

        playLabel.text = progress.CompletedLevelCount == 0 ? "PLAY" : "CONTINUE";
        progressText.text = $"LEVELS {progress.CompletedLevelCount}/{progress.Campaign.Levels.Count}   " +
                            $"STARS {progress.TotalStars}   COINS {progress.Coins}";
        switch (_page)
        {
            case Page.Map:
                mapScreen.Refresh();
                break;
            case Page.Knives:
                inventoryScreen.Refresh();
                break;
        }
    }
}
