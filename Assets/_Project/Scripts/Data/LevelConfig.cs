using UnityEngine;

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "Level", menuName = "Knife Hit/Level")]
public class LevelConfig : ScriptableObject
{
    [Tooltip("Khóa ổn định dùng trong file lưu; đừng đổi sau khi phát hành")]
    [SerializeField] private string levelId = "";
    [SerializeField] private string displayName = "Level";
    [SerializeField] private WorldConfig world;
    [Tooltip("Level thường: một Stage. Bỏ trống nếu là Level boss")]
    [SerializeField] private StageConfig stage;
    [Tooltip("Level boss: các Phase thay cho Stage")]
    [SerializeField] private BossConfig boss;
    [SerializeField, Min(0)] private int coinReward = 20;
    [Tooltip("Coin thêm cho mỗi sao tăng thêm so với lần tốt nhất trước đó")]
    [SerializeField, Min(0)] private int starBonusCoins = 5;
    [Tooltip("Thắng trong số giây này (chỉ tính lúc đang chơi) để được 2 sao")]
    [SerializeField, Min(1f)] private float twoStarSeconds = 30f;
    [SerializeField, Min(1f)] private float threeStarSeconds = 20f;
    [Tooltip("Skin được tặng ở lần thắng đầu tiên")]
    [SerializeField] private KnifeSkin unlockSkin;
    [SerializeField] private string hintId = "";
    [SerializeField, TextArea(1, 3)] private string hintText = "";

    public string LevelId => levelId;
    public string DisplayName => displayName;
    public WorldConfig World => world;
    public BossConfig Boss => boss;
    public bool IsBoss => boss;
    public int StageCount => boss ? boss.PhaseCount : 1;
    public int CoinReward => coinReward;
    public int StarBonusCoins => starBonusCoins;
    public float TwoStarSeconds => twoStarSeconds;
    public float ThreeStarSeconds => threeStarSeconds;
    public KnifeSkin UnlockSkin => unlockSkin;
    public string HintId => hintId;
    public string HintText => hintText;

    public StageConfig GetStage(int index)
    {
        return boss ? boss.GetPhase(index).stage : stage;
    }

    // Luật riêng của pha boss; null với Level thường.
    public BossPhase GetPhase(int index)
    {
        return boss ? boss.GetPhase(index) : null;
    }

    public int CalculateStars(float seconds)
    {
        if (seconds <= threeStarSeconds)
        {
            return 3;
        }

        return seconds <= twoStarSeconds ? 2 : 1;
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(levelId) || !world || (stage ? 1 : 0) + (boss ? 1 : 0) != 1 || StageCount == 0)
        {
            return false;
        }

        for (var i = 0; i < StageCount; i++)
        {
            if (!GetStage(i))
            {
                return false;
            }
        }

        return true;
    }

    private void OnValidate()
    {
        threeStarSeconds = Mathf.Min(threeStarSeconds, twoStarSeconds);
    }
}
