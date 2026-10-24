using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Điều hướng Home / Level Map / Knives của scene MainMenu. Chỉ đọc tiến trình và gọi hành động.
public class MenuScreens : MonoBehaviour
{
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject knivesPanel;
    [SerializeField] private TMP_Text playLabel;
    [SerializeField] private TMP_Text levelsStatText;
    [SerializeField] private TMP_Text starsStatText;
    [SerializeField] private TMP_Text coinsStatText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button knivesButton;
    [SerializeField] private Button[] backButtons = { };
    [Tooltip("Mỗi nút đổi cùng một cài đặt âm thanh; nhãn là TMP_Text con của nút")]
    [SerializeField] private Button[] soundButtons = { };
    [SerializeField] private Image[] soundIcons = { };
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private LevelMapScreen mapScreen;
    [SerializeField] private KnifeInventoryScreen inventoryScreen;

    private SceneFlow.MenuPage _page;

    private void Awake()
    {
        if (!progress || !homePanel || !mapPanel || !knivesPanel || !playLabel || !levelsStatText ||
            !starsStatText || !coinsStatText || !playButton || !levelsButton || !knivesButton || !mapScreen ||
            !inventoryScreen)
        {
            Debug.LogError("Assign PlayerProgress and every menu element.", this);
            enabled = false;
            return;
        }

        playButton.onClick.AddListener(() => SceneFlow.OpenGameplay(progress.CurrentLevel));
        levelsButton.onClick.AddListener(ShowMap);
        knivesButton.onClick.AddListener(ShowKnives);
        foreach (var button in backButtons)
        {
            button.onClick.AddListener(ShowHome);
        }

        foreach (var button in soundButtons)
        {
            button.onClick.AddListener(() => progress.SetSoundEnabled(!progress.SoundEnabled));
        }

        _page = SceneFlow.TakePendingPage();
    }

    private void OnEnable()
    {
        progress.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        progress.Changed -= Refresh;
    }

    private void Update()
    {
        // Nút Back của Android được Input System báo như phím Escape.
        if (_page != SceneFlow.MenuPage.Home && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowHome();
        }
    }

    private void ShowHome()
    {
        _page = SceneFlow.MenuPage.Home;
        Refresh();
    }

    private void ShowMap()
    {
        _page = SceneFlow.MenuPage.Map;
        mapScreen.SelectWorldOf(progress.CurrentLevel);
        Refresh();
    }

    private void ShowKnives()
    {
        _page = SceneFlow.MenuPage.Knives;
        Refresh();
    }

    private void Start()
    {
        if (_page == SceneFlow.MenuPage.Map)
        {
            mapScreen.SelectWorldOf(progress.CurrentLevel);
        }

        Refresh();
    }

    private void Refresh()
    {
        homePanel.SetActive(_page == SceneFlow.MenuPage.Home);
        mapPanel.SetActive(_page == SceneFlow.MenuPage.Map);
        knivesPanel.SetActive(_page == SceneFlow.MenuPage.Knives);

        playLabel.text = progress.CompletedLevelCount == 0 ? "PLAY" : "CONTINUE";
        levelsStatText.text = $"{progress.CompletedLevelCount}/{progress.Campaign.Levels.Count}";
        starsStatText.text = progress.TotalStars.ToString();
        coinsStatText.text = progress.Coins.ToString();

        var sprite = progress.SoundEnabled ? soundOnSprite : soundOffSprite;
        foreach (var icon in soundIcons)
        {
            icon.sprite = sprite;
        }

        switch (_page)
        {
            case SceneFlow.MenuPage.Map:
                mapScreen.Refresh();
                break;
            case SceneFlow.MenuPage.Knives:
                inventoryScreen.Refresh();
                break;
            case SceneFlow.MenuPage.Home:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
