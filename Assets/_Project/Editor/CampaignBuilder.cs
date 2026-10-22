using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
// Tạo (hoặc ghi đè) toàn bộ dữ liệu Campaign từ bảng thông số bên dưới. Chỉ dùng để sinh dữ liệu ban đầu:
// chạy lại sẽ ghi đè mọi chỉnh sửa tay trên các asset này. Bảng kết quả được xuất ra docs/level-table.md.
public static class CampaignBuilder
{
    private const string Root = "Assets/_Project/Data";
    private const string KnifeSpritePath = "Assets/_Project/Art/Sprites/knife_standard.png";
    private const string BackgroundPath = "Assets/_Project/Art/Backgrounds/background_forest.png";
    private const string ArtRoot = "Assets/_Project/Art";
    private const string AudioRoot = "Assets/_Project/Audio";

    private struct Segment
    {
        public float Speed, Duration, Blend;
    }

    private static readonly string[] WorldNames = { "FOREST", "DESERT", "FROST" };
    private static readonly string[] BossNames = { "STUMP", "SAND GUARDIAN", "FROST KING" };
    private static readonly string[] WorldKeys = { "forest", "desert", "frost" };
    private static readonly string[] BossSpriteKeys = { "stump", "sand_guardian", "frost_king" };

    private static readonly int[][] NormalKnives =
    {
        new[] { 6, 6, 7, 7, 8, 8, 8, 9, 9 },
        new[] { 8, 8, 9, 9, 10, 10, 11, 11, 12 },
        new[] { 10, 10, 11, 11, 12, 12, 13, 13, 14 }
    };

    private static readonly int[][] NormalInitial =
    {
        new[] { 0, 0, 1, 1, 2, 2, 2, 3, 3 },
        new[] { 2, 2, 3, 3, 3, 4, 4, 4, 4 },
        new[] { 3, 3, 4, 4, 5, 5, 5, 6, 6 }
    };

    // Mẫu quay: 0 đều, 1 đổi tốc độ, 2 đảo chiều, 3 đảo chiều nhanh.
    private static readonly int[][] NormalPattern =
    {
        new[] { 0, 0, 0, 1, 1, 1, 1, 1, 1 },
        new[] { 0, 0, 1, 1, 1, 2, 2, 2, 2 },
        new[] { 1, 1, 2, 2, 2, 3, 3, 3, 3 }
    };

    private static readonly float[] WorldBaseSpeed = { 90f, 110f, 130f };
    private static readonly float[] WorldSpeedStep = { 8f, 10f, 12f };
    private static readonly int[] WorldStarBonus = { 5, 7, 10 };
    private static readonly int[] BossCoins = { 120, 150, 200 };
    private static readonly int[] BossStarBonus = { 10, 15, 20 };

    [MenuItem("Knife Hit/Build Campaign Data (overwrites)")]
    public static void Build()
    {
        foreach (var folder in new[] { "Levels", "Stages", "Worlds", "Bosses", "Skins" })
        {
            if (!AssetDatabase.IsValidFolder($"{Root}/{folder}"))
            {
                AssetDatabase.CreateFolder(Root, folder);
            }
        }

        var knifeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KnifeSpritePath);
        var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (!knifeSprite || !background)
        {
            Debug.LogError("Missing knife or background sprite.");
            return;
        }

        var worlds = BuildWorlds(background);
        var skins = BuildSkins(knifeSprite);
        var bossSkins = new[] { skins[6], skins[7], skins[8] };
        var levels = new List<LevelConfig>();
        var table = new StringBuilder();
        table.AppendLine("# Bảng thông số 30 level");
        table.AppendLine();
        table.AppendLine("Sinh bởi `Knife Hit > Build Campaign Data`. Mốc 2★/3★ là ước lượng, cần playtest. Tốc độ tính bằng độ/giây.");
        table.AppendLine();
        table.AppendLine("| # | Level | Dao | Dao cắm sẵn | Quay | Luật | Coin | Bonus/sao | 3★ (s) | 2★ (s) |");
        table.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        for (var w = 0; w < 3; w++)
        {
            for (var k = 0; k < CampaignConfig.LevelsPerWorld; k++)
            {
                var number = w * CampaignConfig.LevelsPerWorld + k + 1;
                var id = k == 9 ? $"w{w + 1}_boss" : $"w{w + 1}_l{k + 1:00}";
                if (k < 9)
                {
                    levels.Add(BuildNormalLevel(id, number, w, k, worlds[w], k == 0 && w == 0, table));
                }
                else
                {
                    levels.Add(BuildBossLevel(id, number, w, worlds[w], bossSkins[w], table));
                }
            }
        }

        var campaign = LoadOrCreate<CampaignConfig>($"{Root}/Campaign.asset");
        var so = new SerializedObject(campaign);
        SetObjects(so.FindProperty("worlds"), worlds);
        SetObjects(so.FindProperty("levels"), levels.ToArray());
        SetObjects(so.FindProperty("skins"), skins);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(campaign);

        File.WriteAllText("docs/level-table.md", table.ToString(), new UTF8Encoding(false));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Campaign data built: 30 levels, 3 worlds, 3 bosses, 9 skins.");
    }

    private static Sprite LoadOr(string path, Sprite fallback)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite ? sprite : fallback;
    }

    private static WorldConfig[] BuildWorlds(Sprite background)
    {
        var tints = new[]
        {
            (new Color(1f, 1f, 1f), new Color(1f, 1f, 1f)),
            (new Color(1f, 1f, 1f), new Color(1f, 0.9f, 0.75f)),
            (new Color(1f, 1f, 1f), new Color(0.8f, 0.92f, 1f))
        };
        var worlds = new WorldConfig[3];
        for (var i = 0; i < 3; i++)
        {
            worlds[i] = LoadOrCreate<WorldConfig>($"{Root}/Worlds/World{i + 1}_{WorldNames[i]}.asset");
            var so = new SerializedObject(worlds[i]);
            so.FindProperty("displayName").stringValue = $"WORLD {i + 1}: {WorldNames[i]}";
            so.FindProperty("background").objectReferenceValue = LoadOr(
                $"{ArtRoot}/Backgrounds/background_{WorldKeys[i]}.png", background);
            so.FindProperty("music").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/Music/music_{WorldKeys[i]}.wav");
            so.FindProperty("backgroundTint").colorValue = tints[i].Item1;
            so.FindProperty("targetTint").colorValue = tints[i].Item2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return worlds;
    }

    private static KnifeSkin[] BuildSkins(Sprite sprite)
    {
        var defs = new (string id, string name, Color tint, int price)[]
        {
            ("standard", "STANDARD", Color.white, 0),
            ("bronze", "BRONZE", new Color(1f, 0.72f, 0.45f), 80),
            ("ocean", "OCEAN", new Color(0.45f, 0.72f, 1f), 150),
            ("ember", "EMBER", new Color(1f, 0.5f, 0.35f), 250),
            ("jade", "JADE", new Color(0.5f, 1f, 0.6f), 350),
            ("royal", "ROYAL", new Color(0.8f, 0.6f, 1f), 500),
            ("stump", "STUMP", new Color(0.85f, 0.65f, 0.3f), 0),
            ("dune", "DUNE", new Color(1f, 0.88f, 0.4f), 0),
            ("frost", "FROST", new Color(0.85f, 1f, 1f), 0)
        };
        var skins = new KnifeSkin[defs.Length];
        for (var i = 0; i < defs.Length; i++)
        {
            skins[i] = LoadOrCreate<KnifeSkin>($"{Root}/Skins/Skin_{defs[i].id}.asset");
            var so = new SerializedObject(skins[i]);
            so.FindProperty("skinId").stringValue = defs[i].id;
            so.FindProperty("displayName").stringValue = defs[i].name;
            so.FindProperty("sprite").objectReferenceValue = defs[i].id == "standard"
                ? sprite
                : LoadOr($"{ArtRoot}/Knives/knife_{defs[i].id}.png", sprite);
            // Mỗi skin đã có hình tô màu riêng, nên không nhân thêm tint.
            so.FindProperty("tint").colorValue = Color.white;
            so.FindProperty("price").intValue = defs[i].price;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return skins;
    }

    private static LevelConfig BuildNormalLevel(string id, int number, int w, int k, WorldConfig world, bool tutorial,
        StringBuilder table)
    {
        var knives = NormalKnives[w][k];
        var initial = NormalInitial[w][k];
        var pattern = NormalPattern[w][k];
        var speed = WorldBaseSpeed[w] + WorldSpeedStep[w] * k;
        var stage = BuildStage($"{Root}/Stages/{id}.asset", knives, initial, Segments(pattern, speed), number);
        var coins = 14 + number;
        var three = knives * 1f + 3f;
        var two = knives * 1.9f + 6f;

        var level = LoadOrCreate<LevelConfig>($"{Root}/Levels/{id}.asset");
        var so = new SerializedObject(level);
        so.FindProperty("levelId").stringValue = id;
        so.FindProperty("displayName").stringValue = $"LEVEL {number}";
        so.FindProperty("world").objectReferenceValue = world;
        so.FindProperty("stage").objectReferenceValue = stage;
        so.FindProperty("boss").objectReferenceValue = null;
        so.FindProperty("coinReward").intValue = coins;
        so.FindProperty("starBonusCoins").intValue = WorldStarBonus[w];
        so.FindProperty("twoStarSeconds").floatValue = two;
        so.FindProperty("threeStarSeconds").floatValue = three;
        so.FindProperty("unlockSkin").objectReferenceValue = null;
        so.FindProperty("hintId").stringValue = tutorial ? "hint_basic" : "";
        so.FindProperty("hintText").stringValue = tutorial ? "TAP to throw a knife.\nDon't hit another knife!" : "";
        so.ApplyModifiedPropertiesWithoutUndo();

        table.AppendLine($"| {number} | {level.DisplayName} | {knives} | {initial} | {PatternName(pattern)} {speed:0} | - | {coins} | {WorldStarBonus[w]} | {three:0} | {two:0} |");
        return level;
    }

    private static LevelConfig BuildBossLevel(string id, int number, int w, WorldConfig world, KnifeSkin reward,
        StringBuilder table)
    {
        var knives = new[] { new[] { 8, 10, 12 }, new[] { 9, 11, 13 }, new[] { 10, 12, 15 } }[w];
        var initial = new[] { new[] { 2, 3, 4 }, new[] { 3, 4, 5 }, new[] { 4, 5, 6 } }[w];
        var patterns = new[] { new[] { 0, 1, 2 }, new[] { 1, 2, 3 }, new[] { 2, 3, 3 } }[w];
        var speed = WorldBaseSpeed[w] + WorldSpeedStep[w] * 8f;

        var boss = LoadOrCreate<BossConfig>($"{Root}/Bosses/Boss{w + 1}_{BossNames[w].Replace(' ', '_')}.asset");
        var bossSo = new SerializedObject(boss);
        bossSo.FindProperty("displayName").stringValue = BossNames[w];
        bossSo.FindProperty("targetSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Targets/target_boss_{BossSpriteKeys[w]}.png");
        bossSo.FindProperty("targetTint").colorValue = new[]
        {
            new Color(0.85f, 0.7f, 0.55f), new Color(1f, 0.8f, 0.5f), new Color(0.7f, 0.85f, 1f)
        }[w];
        var phases = bossSo.FindProperty("phases");
        phases.arraySize = 3;
        var seconds = 10f;
        var rules = new StringBuilder();
        for (var p = 0; p < 3; p++)
        {
            var stage = BuildStage($"{Root}/Stages/{id}_p{p + 1}.asset", knives[p], initial[p],
                Segments(patterns[p], speed + p * 10f), number * 10 + p);
            var phase = phases.GetArrayElementAtIndex(p);
            phase.FindPropertyRelative("stage").objectReferenceValue = stage;
            phase.FindPropertyRelative("telegraphSeconds").floatValue = 1.2f;
            var guard = GuardWindows(w, p, out var cycle);
            var wide = WideWindows(w, p);
            phase.FindPropertyRelative("windowCycle").floatValue = cycle;
            SetWindows(phase.FindPropertyRelative("guardWindows"), guard);
            SetWindows(phase.FindPropertyRelative("wideZoneWindows"), wide);
            phase.FindPropertyRelative("bannerText").stringValue = $"PHASE {p + 1}";
            seconds += knives[p] * 1f + 3f;
            rules.Append($"P{p + 1}: {knives[p]} dao/{initial[p]} cắm sẵn/{PatternName(patterns[p])}{(guard.Length > 0 ? $"/Guard×{guard.Length}" : "")}{(wide.Length > 0 ? $"/Wide×{wide.Length}" : "")}; ");
        }

        bossSo.ApplyModifiedPropertiesWithoutUndo();

        var three = seconds;
        var two = seconds * 1.7f;
        var level = LoadOrCreate<LevelConfig>($"{Root}/Levels/{id}.asset");
        var so = new SerializedObject(level);
        so.FindProperty("levelId").stringValue = id;
        so.FindProperty("displayName").stringValue = $"BOSS: {BossNames[w]}";
        so.FindProperty("world").objectReferenceValue = world;
        so.FindProperty("stage").objectReferenceValue = null;
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("coinReward").intValue = BossCoins[w];
        so.FindProperty("starBonusCoins").intValue = BossStarBonus[w];
        so.FindProperty("twoStarSeconds").floatValue = two;
        so.FindProperty("threeStarSeconds").floatValue = three;
        so.FindProperty("unlockSkin").objectReferenceValue = reward;
        var hint = new[]
        {
            ("hint_boss", "BOSS! Clear every phase to win."),
            ("hint_guard", "GUARD glows blue: your taps are ignored.\nWait for it to drop."),
            ("hint_wide", "DANGER ZONE glows red: embedded knives\nblock a wider area. Be careful!")
        }[w];
        so.FindProperty("hintId").stringValue = hint.Item1;
        so.FindProperty("hintText").stringValue = hint.Item2;
        so.ApplyModifiedPropertiesWithoutUndo();

        table.AppendLine($"| {number} | {level.DisplayName} | {knives[0]}+{knives[1]}+{knives[2]} | {initial[0]}/{initial[1]}/{initial[2]} | 3 pha | {rules} | {BossCoins[w]} | {BossStarBonus[w]} | {three:0} | {two:0} |");
        return level;
    }

    private static (float start, float duration, float scale)[] GuardWindows(int world, int phase, out float cycle)
    {
        cycle = 8f;
        switch (world)
        {
            case 1:
                cycle = 7f;
                return phase == 2
                    ? new[] { (2.5f, 1.5f, 1f), (5f, 1.5f, 1f) }
                    : new[] { (3f, phase == 0 ? 1.5f : 2f, 1f) };
            case 2:
                cycle = phase == 2 ? 9f : 8f;
                return phase == 2
                    ? new[] { (2.5f, 1.5f, 1f), (8f, 1f, 1f) }
                    : new[] { (2.5f, 1.5f, 1f) };
            default:
                return System.Array.Empty<(float, float, float)>();
        }
    }

    private static (float start, float duration, float scale)[] WideWindows(int world, int phase)
    {
        if (world != 2)
        {
            return System.Array.Empty<(float, float, float)>();
        }

        return phase switch
        {
            0 => new[] { (6f, 1.5f, 2.2f) },
            1 => new[] { (5.5f, 2f, 2.5f) },
            _ => new[] { (5f, 1.8f, 2.8f) }
        };
    }

    private static void SetWindows(SerializedProperty array, (float start, float duration, float scale)[] windows)
    {
        array.arraySize = windows.Length;
        for (var i = 0; i < windows.Length; i++)
        {
            var element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("start").floatValue = windows[i].start;
            element.FindPropertyRelative("duration").floatValue = windows[i].duration;
            element.FindPropertyRelative("blockAngleScale").floatValue = windows[i].scale;
        }
    }

    private static Segment[] Segments(int pattern, float s)
    {
        switch (pattern)
        {
            case 1:
                return new[]
                {
                    new Segment { Speed = s, Duration = 2.5f, Blend = 0.5f },
                    new Segment { Speed = s * 1.6f, Duration = 2f, Blend = 0.6f }
                };
            case 2:
                return new[]
                {
                    new Segment { Speed = s, Duration = 2.5f, Blend = 0.5f },
                    new Segment { Speed = -s, Duration = 2.5f, Blend = 0.8f }
                };
            case 3:
                return new[]
                {
                    new Segment { Speed = s * 1.3f, Duration = 1.8f, Blend = 0.4f },
                    new Segment { Speed = -s * 0.8f, Duration = 1.8f, Blend = 0.5f },
                    new Segment { Speed = s * 1.6f, Duration = 1.2f, Blend = 0.4f }
                };
            default:
                return new[] { new Segment { Speed = s, Duration = 1f, Blend = 0f } };
        }
    }

    private static string PatternName(int pattern)
    {
        return new[] { "đều", "đổi tốc độ", "đảo chiều", "đảo chiều nhanh" }[pattern];
    }

    private static StageConfig BuildStage(string path, int knives, int initial, Segment[] segments, int seed)
    {
        var stage = LoadOrCreate<StageConfig>(path);
        var so = new SerializedObject(stage);
        so.FindProperty("requiredKnives").intValue = knives;

        // Góc cắm sẵn chia đều quanh bia, lệch theo seed để mỗi Level nhìn khác nhau.
        var angles = so.FindProperty("initialKnifeAngles");
        angles.arraySize = initial;
        var offset = (seed * 37) % 360;
        for (var i = 0; i < initial; i++)
        {
            angles.GetArrayElementAtIndex(i).floatValue = Mathf.Repeat(offset + i * 360f / Mathf.Max(1, initial), 360f);
        }

        var rotation = so.FindProperty("rotationSegments");
        rotation.arraySize = segments.Length;
        for (var i = 0; i < segments.Length; i++)
        {
            var element = rotation.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("speed").floatValue = segments[i].Speed;
            element.FindPropertyRelative("duration").floatValue = segments[i].Duration;
            element.FindPropertyRelative("blendTime").floatValue = segments[i].Blend;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return stage;
    }

    private static void SetObjects(SerializedProperty array, Object[] objects)
    {
        array.arraySize = objects.Length;
        for (var i = 0; i < objects.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
