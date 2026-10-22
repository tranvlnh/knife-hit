using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "Campaign", menuName = "Knife Hit/Campaign")]
public class CampaignConfig : ScriptableObject
{
    public const int LevelsPerWorld = 10;

    [SerializeField] private WorldConfig[] worlds = { };
    [Tooltip("Các Level theo thứ tự chơi; 10 Level mỗi World, Level cuối World là boss")]
    [SerializeField] private LevelConfig[] levels = { };
    [Tooltip("Skin đầu tiên là skin mặc định, luôn sở hữu")]
    [SerializeField] private KnifeSkin[] skins = { };

    public IReadOnlyList<WorldConfig> Worlds => worlds;
    public IReadOnlyList<LevelConfig> Levels => levels;
    public IReadOnlyList<KnifeSkin> Skins => skins;
    public KnifeSkin DefaultSkin => skins.Length > 0 ? skins[0] : null;

    public int IndexOf(LevelConfig level)
    {
        return Array.IndexOf(levels, level);
    }

    public LevelConfig FindLevel(string id)
    {
        return Array.Find(levels, level => level && level.LevelId == id);
    }

    public KnifeSkin FindSkin(string id)
    {
        return Array.Find(skins, skin => skin && skin.SkinId == id);
    }

    // Level mà lần thắng đầu tiên tặng skin này (null nếu skin chỉ mua được bằng coin).
    public LevelConfig FindUnlockLevel(KnifeSkin skin)
    {
        return Array.Find(levels, level => level && level.UnlockSkin == skin);
    }

    public bool IsValid()
    {
        return worlds.Length > 0 && levels.Length > 0 && skins.Length > 0 &&
               Array.TrueForAll(levels, level => level && level.IsValid()) &&
               Array.TrueForAll(skins, skin => skin && !string.IsNullOrEmpty(skin.SkinId));
    }

    private void OnValidate()
    {
        var ids = new HashSet<string>();
        foreach (var level in levels)
        {
            if (level && !ids.Add(level.LevelId))
            {
                Debug.LogWarning($"Duplicate level id '{level.LevelId}'.", this);
            }
        }

        ids.Clear();
        foreach (var skin in skins)
        {
            if (skin && !ids.Add(skin.SkinId))
            {
                Debug.LogWarning($"Duplicate skin id '{skin.SkinId}'.", this);
            }
        }

        if (skins.Length > 0 && skins[0] && skins[0].Price != 0)
        {
            Debug.LogWarning("The default skin (first) should cost 0.", this);
        }
    }
}
