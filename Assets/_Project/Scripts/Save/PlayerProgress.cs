using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
// Nơi duy nhất đọc/ghi tiến trình: level đã thắng + sao, coin, dao sở hữu/đang trang bị, âm thanh, gợi ý đã xem.
// Lưu JSON có version trong PlayerPrefs; dữ liệu thiếu hoặc hỏng luôn quay về mặc định an toàn.
public class PlayerProgress : MonoBehaviour
{
    public struct WinResult
    {
        public int Stars;
        public int PreviousBestStars;
        public int CoinsEarned;
        public float Seconds;
        public KnifeSkin UnlockedSkin;
        public bool IsFirstWin => PreviousBestStars == 0;
    }

    [Serializable]
    private class LevelRecord
    {
        public string id;
        public int stars;
    }

    [Serializable]
    private class SaveData
    {
        public int version = SaveVersion;
        public bool soundEnabled = true;
        public int coins;
        public List<LevelRecord> levels = new();
        public List<string> ownedSkins = new();
        public string equippedSkin = "";
        public List<string> seenHints = new();
    }

    private const int SaveVersion = 1;
    private const string SaveKey = "KnifeHit.Save";
    private const string CorruptSaveKey = "KnifeHit.Save.Corrupt";
    private const string LegacySoundKey = "SoundEnabled";

    [SerializeField] private CampaignConfig campaign;

    private SaveData _data;

    // Nạp lười: các component UI có thể OnEnable trước Awake của component này.
    private SaveData Data
    {
        get
        {
            if (_data == null)
            {
                Initialize();
            }

            return _data;
        }
    }

    // Chỉ để UI cập nhật hiển thị; mọi quyết định đi qua lời gọi trực tiếp.
    public event Action Changed;

    public CampaignConfig Campaign => campaign;
    public int Coins => Data.coins;
    public bool SoundEnabled => Data.soundEnabled;
    public KnifeSkin EquippedSkin => campaign.FindSkin(Data.equippedSkin) ?? campaign.DefaultSkin;

    public int CompletedLevelCount
    {
        get
        {
            var count = 0;
            foreach (var record in Data.levels)
            {
                count += record.stars > 0 ? 1 : 0;
            }

            return count;
        }
    }

    public int TotalStars
    {
        get
        {
            var total = 0;
            foreach (var record in Data.levels)
            {
                total += record.stars;
            }

            return total;
        }
    }

    // Level đầu tiên chưa thắng; thắng hết thì là Level cuối.
    public LevelConfig CurrentLevel
    {
        get
        {
            foreach (var level in campaign.Levels)
            {
                if (GetStars(level) == 0)
                {
                    return level;
                }
            }

            return campaign.Levels[^1];
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (!campaign || !campaign.IsValid())
        {
            Debug.LogError("Assign a valid CampaignConfig (levels, skins and worlds fully set).", this);
            enabled = false;
            _data = new SaveData();
            return;
        }

        _data = Load();
        ApplySound();
    }

    public int GetStars(LevelConfig level)
    {
        var record = FindRecord(level.LevelId);
        return record?.stars ?? 0;
    }

    // Level đầu luôn mở; các Level sau mở khi đã thắng Level liền trước.
    public bool IsLevelUnlocked(LevelConfig level)
    {
        var index = campaign.IndexOf(level);
        return index == 0 || (index > 0 && GetStars(campaign.Levels[index - 1]) > 0);
    }

    public LevelConfig GetNextLevel(LevelConfig level)
    {
        var index = campaign.IndexOf(level);
        return index >= 0 && index + 1 < campaign.Levels.Count ? campaign.Levels[index + 1] : null;
    }

    // Chỉ gọi một lần cho mỗi lần thắng. Thưởng lần đầu + phần sao tăng thêm; sao thấp hơn không ghi đè sao cao hơn.
    public WinResult RecordWin(LevelConfig level, int stars, float seconds)
    {
        stars = Mathf.Clamp(stars, 1, 3);
        var record = FindRecord(level.LevelId);
        var previous = record?.stars ?? 0;
        var result = new WinResult { Stars = stars, PreviousBestStars = previous, Seconds = seconds };
        result.CoinsEarned = (previous == 0 ? level.CoinReward : 0) +
                             level.StarBonusCoins * Mathf.Max(0, stars - previous);

        if (record == null)
        {
            Data.levels.Add(new LevelRecord { id = level.LevelId, stars = stars });
        }
        else
        {
            record.stars = Mathf.Max(record.stars, stars);
        }

        Data.coins += result.CoinsEarned;
        if (previous == 0 && level.UnlockSkin && !IsOwned(level.UnlockSkin))
        {
            Data.ownedSkins.Add(level.UnlockSkin.SkinId);
            result.UnlockedSkin = level.UnlockSkin;
        }

        Commit();
        return result;
    }

    public bool IsOwned(KnifeSkin skin)
    {
        return skin == campaign.DefaultSkin || Data.ownedSkins.Contains(skin.SkinId);
    }

    public bool CanBuy(KnifeSkin skin)
    {
        return !IsOwned(skin) && skin.Price > 0 && Data.coins >= skin.Price;
    }

    public bool TryBuy(KnifeSkin skin)
    {
        if (!CanBuy(skin))
        {
            return false;
        }

        Data.coins -= skin.Price;
        Data.ownedSkins.Add(skin.SkinId);
        Commit();
        return true;
    }

    public bool Equip(KnifeSkin skin)
    {
        if (!skin || !IsOwned(skin))
        {
            return false;
        }

        Data.equippedSkin = skin.SkinId;
        Commit();
        return true;
    }

    public bool HasSeenHint(string hintId)
    {
        return string.IsNullOrEmpty(hintId) || Data.seenHints.Contains(hintId);
    }

    public void MarkHintSeen(string hintId)
    {
        if (!HasSeenHint(hintId))
        {
            Data.seenHints.Add(hintId);
            Commit();
        }
    }

    public void SetSoundEnabled(bool enabledValue)
    {
        Data.soundEnabled = enabledValue;
        ApplySound();
        Commit();
    }

    private LevelRecord FindRecord(string id)
    {
        return Data.levels.Find(record => record.id == id);
    }

    private void ApplySound()
    {
        AudioListener.volume = Data.soundEnabled ? 1f : 0f;
    }

    private void Commit()
    {
        Save();
        Changed?.Invoke();
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
        PlayerPrefs.Save();
    }

    private SaveData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            // Lần chạy đầu sau khi nâng cấp: giữ lại cài đặt âm thanh của bản demo.
            var fresh = CreateDefault();
            fresh.soundEnabled = PlayerPrefs.GetInt(LegacySoundKey, 1) == 1;
            return fresh;
        }

        var json = PlayerPrefs.GetString(SaveKey, "");
        SaveData data = null;
        try
        {
            data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Save data is not valid JSON, using defaults: {exception.Message}", this);
        }

        if (data == null || data.version != SaveVersion)
        {
            Debug.LogWarning("Save data is empty, corrupt or from another version; using defaults.", this);
            if (!string.IsNullOrWhiteSpace(json))
            {
                PlayerPrefs.SetString(CorruptSaveKey, json);
            }

            return CreateDefault();
        }

        return Sanitize(data);
    }

    private SaveData CreateDefault()
    {
        var data = new SaveData();
        data.ownedSkins.Add(campaign.DefaultSkin.SkinId);
        data.equippedSkin = campaign.DefaultSkin.SkinId;
        return data;
    }

    // Bỏ từng mục không còn hợp lệ nhưng giữ nguyên phần còn lại.
    private SaveData Sanitize(SaveData data)
    {
        var clean = CreateDefault();
        clean.soundEnabled = data.soundEnabled;
        clean.coins = Mathf.Max(0, data.coins);

        foreach (var record in data.levels ?? new List<LevelRecord>())
        {
            if (record == null || !campaign.FindLevel(record.id))
            {
                Debug.LogWarning($"Ignoring save entry for unknown level '{record?.id}'.", this);
                continue;
            }

            var existing = clean.levels.Find(other => other.id == record.id);
            var stars = Mathf.Clamp(record.stars, 0, 3);
            if (existing == null)
            {
                clean.levels.Add(new LevelRecord { id = record.id, stars = stars });
            }
            else
            {
                existing.stars = Mathf.Max(existing.stars, stars);
            }
        }

        foreach (var id in data.ownedSkins ?? new List<string>())
        {
            if (!campaign.FindSkin(id))
            {
                Debug.LogWarning($"Ignoring save entry for unknown skin '{id}'.", this);
            }
            else if (!clean.ownedSkins.Contains(id))
            {
                clean.ownedSkins.Add(id);
            }
        }

        if (clean.ownedSkins.Contains(data.equippedSkin ?? ""))
        {
            clean.equippedSkin = data.equippedSkin;
        }

        foreach (var id in data.seenHints ?? new List<string>())
        {
            if (!string.IsNullOrEmpty(id) && !clean.seenHints.Contains(id))
            {
                clean.seenHints.Add(id);
            }
        }

        return clean;
    }
}
