using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Áp dụng bộ asset đã tạo (Art/Knives, Targets, Overlays, Icons, UI, nền World 2/3, SFX, nhạc) vào game:
// cấu hình import, gán vào dữ liệu Campaign (chỉ đổi các trường hình/nhạc, không đụng thông số màn) và nối scene.
// Chạy lại được: các object đã tạo được tìm theo tên và không bị nhân bản.
public static class AssetWiring
{
    private const string Art = "Assets/_Project/Art";
    private const string Audio = "Assets/_Project/Audio";
    private const string Data = "Assets/_Project/Data";

    private static readonly string[] SkinIds =
        { "standard", "bronze", "ocean", "ember", "jade", "royal", "stump", "dune", "frost" };

    private static readonly string[] WorldKeys = { "forest", "desert", "frost" };
    private static readonly string[] BossKeys = { "stump", "sand_guardian", "frost_king" };

    [MenuItem("Knife Hit/Apply Generated Assets")]
    public static void Apply()
    {
        ConfigureTextures();
        ConfigureAudio();
        AssetDatabase.Refresh();
        PatchData();
        WireScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Generated assets applied.");
    }

    // ---------------------------------------------------------------- import
    private static void ConfigureTextures()
    {
        Sprite("Backgrounds/background_desert.png", 167.2f);
        Sprite("Backgrounds/background_frost.png", 167.2f);
        foreach (var id in SkinIds)
        {
            if (id != "standard")
            {
                Sprite($"Knives/knife_{id}.png", 900f);
            }
        }

        foreach (var key in BossKeys)
        {
            Sprite($"Targets/target_boss_{key}.png", 400f);
        }

        Sprite("Overlays/guard_shield.png", 400f);
        Sprite("Overlays/danger_ring.png", 400f);
        foreach (var icon in new[] { "lock", "coin", "star_full", "star_empty", "boss" })
        {
            Sprite($"Icons/icon_{icon}.png", 256f);
        }

        Sprite("UI/ui_button.png", 100f, new Vector4(44f, 44f, 44f, 44f));
        Sprite("UI/ui_panel.png", 100f, new Vector4(60f, 60f, 60f, 60f));
        Sprite("UI/ui_card.png", 100f, new Vector4(48f, 48f, 48f, 48f));
    }

    private static void Sprite(string relativePath, float ppu, Vector4? border = null)
    {
        var path = $"{Art}/{relativePath}";
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            Debug.LogWarning($"Missing texture {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        if (border.HasValue)
        {
            importer.spriteBorder = border.Value;
        }

        importer.SaveAndReimport();
    }

    private static void ConfigureAudio()
    {
        foreach (var name in new[]
                 {
                     "boss_guard_warning", "boss_danger_warning", "coin_collect", "knife_buy", "knife_equip"
                 })
        {
            Clip($"{Audio}/SFX/{name}.wav", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, true);
        }

        foreach (var key in WorldKeys)
        {
            Clip($"{Audio}/Music/music_{key}.wav", AudioClipLoadType.CompressedInMemory,
                AudioCompressionFormat.Vorbis, false);
        }
    }

    private static void Clip(string path, AudioClipLoadType load, AudioCompressionFormat format, bool preload)
    {
        if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
        {
            Debug.LogWarning($"Missing audio {path}");
            return;
        }

        importer.forceToMono = true;
        importer.loadInBackground = !preload;
        importer.defaultSampleSettings = new AudioImporterSampleSettings
        {
            loadType = load,
            compressionFormat = format,
            quality = 0.6f,
            preloadAudioData = preload,
            sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
        };
        importer.SaveAndReimport();
    }

    // ---------------------------------------------------------------- data
    private static void PatchData()
    {
        var knifeStandard = Load<Sprite>($"{Art}/Sprites/knife_standard.png");
        foreach (var id in SkinIds)
        {
            var skin = Load<KnifeSkin>($"{Data}/Skins/Skin_{id}.asset");
            if (!skin)
            {
                continue;
            }

            var so = new SerializedObject(skin);
            so.FindProperty("sprite").objectReferenceValue =
                id == "standard" ? knifeStandard : Load<Sprite>($"{Art}/Knives/knife_{id}.png");
            so.FindProperty("tint").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
        }

        var worldFiles = new[] { "World1_FOREST", "World2_DESERT", "World3_FROST" };
        for (var i = 0; i < 3; i++)
        {
            var world = Load<WorldConfig>($"{Data}/Worlds/{worldFiles[i]}.asset");
            if (!world)
            {
                continue;
            }

            var so = new SerializedObject(world);
            if (i > 0)
            {
                so.FindProperty("background").objectReferenceValue =
                    Load<Sprite>($"{Art}/Backgrounds/background_{WorldKeys[i]}.png");
                so.FindProperty("backgroundTint").colorValue = Color.white;
            }

            so.FindProperty("music").objectReferenceValue = Load<AudioClip>($"{Audio}/Music/music_{WorldKeys[i]}.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(world);
        }

        var bossFiles = new[] { "Boss1_STUMP", "Boss2_SAND_GUARDIAN", "Boss3_FROST_KING" };
        for (var i = 0; i < 3; i++)
        {
            var boss = Load<BossConfig>($"{Data}/Bosses/{bossFiles[i]}.asset");
            if (!boss)
            {
                continue;
            }

            var so = new SerializedObject(boss);
            so.FindProperty("targetSprite").objectReferenceValue =
                Load<Sprite>($"{Art}/Targets/target_boss_{BossKeys[i]}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }
    }

    // ---------------------------------------------------------------- scene
    private static void WireScene()
    {
        var target = Object.FindFirstObjectByType<RotatingTarget>();
        var encounter = Object.FindFirstObjectByType<BossEncounter>();
        var feedback = Object.FindFirstObjectByType<GameFeedback>();
        if (!target || !encounter || !feedback)
        {
            Debug.LogError("GamePlay scene is missing the target, BossEncounter or GameFeedback.");
            return;
        }

        var visual = target.transform.Find("Visual").GetComponent<SpriteRenderer>();
        var guard = EnsureOverlay(visual, "GuardShield", $"{Art}/Overlays/guard_shield.png");
        var danger = EnsureOverlay(visual, "DangerRing", $"{Art}/Overlays/danger_ring.png");
        Set(encounter, "guardOverlay", guard);
        Set(encounter, "dangerOverlay", danger);

        var music = feedback.transform.Find("MusicSource");
        if (!music)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(feedback.transform, false);
            music = go.transform;
        }

        var musicSource = music.GetComponent<AudioSource>();
        if (!musicSource)
        {
            musicSource = music.gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        Set(feedback, "musicSource", musicSource);
        Set(feedback, "menuMusic", Load<AudioClip>($"{Audio}/Music/music_forest.wav"));
        Set(feedback, "encounter", encounter);
        Set(feedback, "progress", Object.FindFirstObjectByType<PlayerProgress>());
        SetClip(feedback, "guardWarningCue", "boss_guard_warning");
        SetClip(feedback, "dangerWarningCue", "boss_danger_warning");
        SetClip(feedback, "coinCue", "coin_collect");
        SetClip(feedback, "buyCue", "knife_buy");
        SetClip(feedback, "equipCue", "knife_equip");

        WireUi();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static SpriteRenderer EnsureOverlay(SpriteRenderer visual, string name, string spritePath)
    {
        var child = visual.transform.Find(name);
        if (!child)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(visual.transform, false);
            child = go.transform;
        }

        var overlay = child.GetComponent<SpriteRenderer>();
        overlay.sprite = Load<Sprite>(spritePath);
        overlay.sortingLayerID = visual.sortingLayerID;
        overlay.sortingOrder = visual.sortingOrder + 1;
        overlay.sharedMaterial = visual.sharedMaterial;
        overlay.enabled = false;
        return overlay;
    }

    private static void WireUi()
    {
        var button = Load<Sprite>($"{Art}/UI/ui_button.png");
        var panel = Load<Sprite>($"{Art}/UI/ui_panel.png");
        var card = Load<Sprite>($"{Art}/UI/ui_card.png");
        var lockSprite = Load<Sprite>($"{Art}/Icons/icon_lock.png");
        var coin = Load<Sprite>($"{Art}/Icons/icon_coin.png");
        var starFull = Load<Sprite>($"{Art}/Icons/icon_star_full.png");
        var starEmpty = Load<Sprite>($"{Art}/Icons/icon_star_empty.png");
        var boss = Load<Sprite>($"{Art}/Icons/icon_boss.png");

        var hud = GameObject.Find("HUD");
        if (!hud)
        {
            Debug.LogWarning("HUD not found; UI frames skipped.");
            return;
        }

        // Khung nút: giữ nguyên Image.color để màu trạng thái vẫn đổi theo mã.
        foreach (var b in hud.GetComponentsInChildren<Button>(true))
        {
            var image = b.GetComponent<Image>();
            if (image)
            {
                image.sprite = button;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
        }

        foreach (var skinCard in hud.GetComponentsInChildren<SkinCardView>(true))
        {
            var image = skinCard.GetComponent<Image>();
            image.sprite = card;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        var hint = hud.transform.Find("SafeArea/GameplayRoot/HintPanel");
        if (hint)
        {
            var image = hint.GetComponent<Image>();
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.92f);
        }

        foreach (var view in hud.GetComponentsInChildren<LevelButtonView>(true))
        {
            var rect = (RectTransform)view.transform;
            var lockImage = EnsureIcon(rect, "LockIcon", lockSprite, new Vector2(0.88f, 0.72f), 64f);
            var bossImage = EnsureIcon(rect, "BossIcon", boss, new Vector2(0.12f, 0.72f), 72f);
            var stars = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                stars[i] = EnsureIcon(rect, $"Star{i + 1}", starEmpty, new Vector2(0.5f + (i - 1) * 0.18f, 0.2f), 52f);
            }

            Set(view, "lockIcon", lockImage);
            Set(view, "bossIcon", bossImage);
            SetArray(view, "starIcons", stars);
            Set(view, "starFull", starFull);
            Set(view, "starEmpty", starEmpty);
        }

        var coinsText = hud.transform.Find("SafeArea/MenuRoot/KnivesPanel/CoinsText");
        if (coinsText)
        {
            EnsureIcon((RectTransform)coinsText.parent, "CoinIcon", coin, new Vector2(0.5f, 0.5f), 90f)
                .rectTransform.anchoredPosition = new Vector2(-330f, ((RectTransform)coinsText).anchoredPosition.y);
        }
    }

    private static Image EnsureIcon(RectTransform parent, string name, Sprite sprite, Vector2 anchor, float size)
    {
        var child = parent.Find(name);
        if (!child)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        var rect = (RectTransform)child;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        var image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // ---------------------------------------------------------------- helpers
    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset)
        {
            Debug.LogWarning($"Missing asset {path}");
        }

        return asset;
    }

    private static void SetClip(Object target, string cue, string clipName)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty($"{cue}.clip");
        if (property == null)
        {
            Debug.LogError($"No cue '{cue}'.");
            return;
        }

        property.objectReferenceValue = Load<AudioClip>($"{Audio}/SFX/{clipName}.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(property);
        if (p == null)
        {
            Debug.LogError($"{target.GetType().Name} has no serialized field '{property}'.");
            return;
        }

        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string property, IReadOnlyList<Object> values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(property);
        p.arraySize = values.Count;
        for (var i = 0; i < values.Count; i++)
        {
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
