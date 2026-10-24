using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
public class LevelButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Có thể bỏ trống: hiện khi Level còn khóa")]
    [SerializeField] private Image lockIcon;
    [Tooltip("Có thể bỏ trống: hiện ở Level boss đã mở khóa")]
    [SerializeField] private Image bossIcon;
    [Tooltip("Ba sao theo thứ tự; có thể bỏ trống")]
    [SerializeField] private Image[] starIcons = { };
    [SerializeField] private Sprite starFull;
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private Color lockedColor = new(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color newColor = new(0.95f, 0.75f, 0.25f);
    [SerializeField] private Color clearedColor = new(0.35f, 0.7f, 0.4f);
    [SerializeField] private Color bossColor = new(0.8f, 0.3f, 0.25f);
    [Tooltip("Có thể bỏ trống: sprite nền theo trạng thái, thay cho việc chỉ đổi màu")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite newSprite;
    [SerializeField] private Sprite clearedSprite;
    [SerializeField] private Sprite bossSprite;

    public Button Button => button;
    public LevelConfig Level { get; private set; }

    public void Bind(LevelConfig level, int number, int stars, bool unlocked)
    {
        Level = level;
        button.interactable = unlocked;
        numberText.text = level.IsBoss ? "BOSS" : number.ToString();
        statusText.text = !unlocked ? "LOCKED" : stars > 0 ? $"{stars}/3" : "NEW";
        var showStars = unlocked && starIcons.Length > 0 && starFull && starEmpty;
        if (lockIcon)
        {
            lockIcon.gameObject.SetActive(!unlocked);
        }

        if (bossIcon)
        {
            bossIcon.gameObject.SetActive(unlocked && level.IsBoss);
        }

        for (var i = 0; i < starIcons.Length; i++)
        {
            starIcons[i].gameObject.SetActive(showStars);
            starIcons[i].sprite = i < stars ? starFull : starEmpty;
        }

        if (showStars)
        {
            statusText.text = "";
        }

        background.color = !unlocked ? lockedColor : level.IsBoss ? bossColor : stars > 0 ? clearedColor : newColor;
        var sprite = !unlocked ? lockedSprite : level.IsBoss ? bossSprite : stars > 0 ? clearedSprite : newSprite;
        if (sprite)
        {
            background.sprite = sprite;
        }
    }
}
