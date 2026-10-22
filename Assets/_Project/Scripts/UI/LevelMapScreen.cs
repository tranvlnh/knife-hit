using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Lưới Level của một World; nút Level chỉ gọi GameSession.StartLevel, luật mở khóa nằm ở PlayerProgress.
public class LevelMapScreen : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private TMP_Text worldTitleText;
    [SerializeField] private Button[] worldTabs = { };
    [Tooltip("Số nút bằng số Level tối đa của một World")]
    [SerializeField] private LevelButtonView[] levelButtons = { };

    private int _worldIndex;

    private void Awake()
    {
        if (!session || !progress || !worldTitleText || levelButtons.Length == 0)
        {
            Debug.LogError("Assign the GameSession, PlayerProgress, title and level buttons.", this);
            enabled = false;
            return;
        }

        for (var i = 0; i < worldTabs.Length; i++)
        {
            var index = i;
            worldTabs[i].onClick.AddListener(() => SelectWorld(index));
        }

        foreach (var view in levelButtons)
        {
            var captured = view;
            view.Button.onClick.AddListener(() => session.StartLevel(captured.Level));
        }
    }

    public void SelectWorldOf(LevelConfig level)
    {
        var index = level ? progress.Campaign.Worlds.IndexOfWorld(level.World) : 0;
        SelectWorld(Mathf.Max(0, index));
    }

    public void Refresh()
    {
        var campaign = progress.Campaign;
        var world = _worldIndex < campaign.Worlds.Count ? campaign.Worlds[_worldIndex] : null;
        worldTitleText.text = world ? world.DisplayName : "";
        for (var i = 0; i < worldTabs.Length; i++)
        {
            worldTabs[i].gameObject.SetActive(i < campaign.Worlds.Count);
            worldTabs[i].interactable = i != _worldIndex;
        }

        var slot = 0;
        for (var i = 0; i < campaign.Levels.Count && slot < levelButtons.Length; i++)
        {
            var level = campaign.Levels[i];
            if (level.World != world)
            {
                continue;
            }

            levelButtons[slot].gameObject.SetActive(true);
            levelButtons[slot].Bind(level, i + 1, progress.GetStars(level), progress.IsLevelUnlocked(level));
            slot++;
        }

        for (; slot < levelButtons.Length; slot++)
        {
            levelButtons[slot].gameObject.SetActive(false);
        }
    }

    private void SelectWorld(int index)
    {
        _worldIndex = index;
        Refresh();
    }
}

// ReSharper disable once CheckNamespace
internal static class WorldListExtensions
{
    public static int IndexOfWorld(this System.Collections.Generic.IReadOnlyList<WorldConfig> worlds, WorldConfig world)
    {
        for (var i = 0; i < worlds.Count; i++)
        {
            if (worlds[i] == world)
            {
                return i;
            }
        }

        return -1;
    }
}
