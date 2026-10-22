using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
// Dựng lại toàn bộ UI: sinh icon, dựng HUD + Pause + Result cho GamePlay.unity và dựng MainMenu.unity.
// Chạy lại sẽ xóa HUD cũ / ghi đè MainMenu.unity; chỉnh tay trong hai UI này sẽ mất.
public static class UiRebuilder
{
    private const string ArtPath = "Assets/_Project/Art/UI/";
    private const string CozyPath = "Assets/_Project/Art/UI/Cozy/";
    private const string GamePlayPath = "Assets/_Project/Scenes/GamePlay.unity";
    private const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string CampaignPath = "Assets/_Project/Data/Campaign.asset";

    // Green / Blue / Purple là màu nhuộm lên nút kem của Cozy UI Pack; Red và Wood chọn sprite riêng của pack.
    private static readonly Color Green = new(0.55f, 0.83f, 0.22f);
    private static readonly Color Blue = new(0.4f, 0.68f, 0.95f);
    private static readonly Color Purple = new(0.75f, 0.55f, 0.92f);
    private static readonly Color Red = new(0.88f, 0.3f, 0.3f);
    private static readonly Color Wood = new(0.36f, 0.44f, 0.58f);
    private static readonly Color Orange = new(0.95f, 0.65f, 0.2f);
    private static readonly Color Gray = new(0.6f, 0.6f, 0.6f);
    private static readonly Color Gold = new(1f, 0.8f, 0.25f);
    private static readonly Color Ink = new(0.1f, 0.13f, 0.2f);
    private static readonly Color Brown = new(0.33f, 0.15f, 0.1f);
    private static readonly Color Cream = new(1f, 1f, 0.92f);

    private static Sprite _heartFull, _heartEmpty;
    private static Sprite _btnGreen, _btnBlue, _btnPurple, _btnOrange, _btnGray, _btnWood, _btnRed, _btnRound, _pillCream;
    private static Sprite _plank, _banner, _tan, _note, _popupWood, _popupRed;
    private static Sprite _coin, _starFull, _starEmpty, _lock, _boss;
    private static Sprite _pause, _back, _home, _soundOn, _soundOff, _retry, _levels;

    [MenuItem("Knife Hit/Rebuild UI and Scenes")]
    public static void Build()
    {
        ImportCozy();
        GenerateSprites();
        LoadSprites();
        var audio = RebuildGamePlay();
        BuildMainMenu(audio);
        var gamePlay = AssetDatabase.LoadAssetAtPath<SceneAsset>(GamePlayPath);
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true), new EditorBuildSettingsScene(GamePlayPath, true)
        };
        Debug.Log($"UI rebuilt. Scenes in build: {menu.name}, {gamePlay.name}. Update the Android build profile scene list too.");
    }

    // ---------------------------------------------------------------- sprites

    private static void GenerateSprites()
    {
        Directory.CreateDirectory(ArtPath);
        Save("icon_pause", 128, Fill(p => RoundBox(p, 0.355f, 0.5f, 0.085f, 0.3f, 0.04f) || RoundBox(p, 0.645f, 0.5f, 0.085f, 0.3f, 0.04f)), 0);
        Save("icon_back", 128, Fill(p => Capsule(p, new Vector2(0.62f, 0.78f), new Vector2(0.36f, 0.5f), 0.075f) ||
                                         Capsule(p, new Vector2(0.36f, 0.5f), new Vector2(0.62f, 0.22f), 0.075f)), 0);
        Save("icon_home", 128, Fill(Home), 0);
        Save("icon_sound_on", 128, Fill(p => Speaker(p) || Wave(p, 0.2f) || Wave(p, 0.32f)), 0);
        Save("icon_sound_off", 128, Fill(p => Speaker(p) || Capsule(p, new Vector2(0.64f, 0.36f), new Vector2(0.86f, 0.64f), 0.045f) ||
                                              Capsule(p, new Vector2(0.64f, 0.64f), new Vector2(0.86f, 0.36f), 0.045f)), 0);
        Save("icon_retry", 128, Fill(Retry), 0);
        Save("icon_levels", 128, Fill(p => RoundBox(p, 0.34f, 0.34f, 0.15f, 0.15f, 0.04f) || RoundBox(p, 0.66f, 0.34f, 0.15f, 0.15f, 0.04f) ||
                                           RoundBox(p, 0.34f, 0.66f, 0.15f, 0.15f, 0.04f) || RoundBox(p, 0.66f, 0.66f, 0.15f, 0.15f, 0.04f)), 0);
        AssetDatabase.Refresh();
    }

    private static Func<Vector2, bool> Fill(Func<Vector2, bool> inside) => inside;

    private static bool RoundBox(Vector2 p, float cx, float cy, float hx, float hy, float radius)
    {
        var q = new Vector2(Mathf.Abs(p.x - cx) - (hx - radius), Mathf.Abs(p.y - cy) - (hy - radius));
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) < radius;
    }

    private static bool Capsule(Vector2 p, Vector2 a, Vector2 b, float radius)
    {
        var ab = b - a;
        var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude < radius;
    }

    private static bool Polygon(Vector2 p, params Vector2[] v)
    {
        var inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            if (v[i].y > p.y != v[j].y > p.y &&
                p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool Home(Vector2 p)
    {
        var roof = Polygon(p, new Vector2(0.12f, 0.5f), new Vector2(0.5f, 0.85f), new Vector2(0.88f, 0.5f));
        var body = RoundBox(p, 0.5f, 0.38f, 0.26f, 0.18f, 0.02f) && !RoundBox(p, 0.5f, 0.32f, 0.07f, 0.12f, 0.02f);
        return roof || body;
    }

    private static bool Speaker(Vector2 p)
    {
        return RoundBox(p, 0.23f, 0.5f, 0.09f, 0.11f, 0.02f) ||
               Polygon(p, new Vector2(0.3f, 0.39f), new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.76f), new Vector2(0.3f, 0.61f));
    }

    private static bool Wave(Vector2 p, float radius)
    {
        var d = p - new Vector2(0.5f, 0.5f);
        var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        return Mathf.Abs(d.magnitude - (0.14f + radius)) < 0.03f && Mathf.Abs(angle) < 45f && d.x > 0f;
    }

    private static bool Retry(Vector2 p)
    {
        var center = new Vector2(0.5f, 0.5f);
        var d = p - center;
        var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        var ring = Mathf.Abs(d.magnitude - 0.27f) < 0.055f && (angle < 10f || angle > 75f);
        var radians = 10f * Mathf.Deg2Rad;
        var radial = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        var tangent = new Vector2(-radial.y, radial.x);
        var c = center + radial * 0.27f;
        var arrow = Polygon(p, c + tangent * 0.17f, c - tangent * 0.03f + radial * 0.15f, c - tangent * 0.03f - radial * 0.15f);
        return ring || arrow;
    }

    private static void Save(string name, int size, Func<Vector2, bool> inside, int border)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        const int samples = 3;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var hits = 0;
                for (var sy = 0; sy < samples; sy++)
                {
                    for (var sx = 0; sx < samples; sx++)
                    {
                        var point = new Vector2((x + (sx + 0.5f) / samples) / size, (y + (sy + 0.5f) / samples) / size);
                        if (inside(point))
                        {
                            hits++;
                        }
                    }
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, hits / (float)(samples * samples)));
            }
        }

        var path = ArtPath + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.SaveAndReimport();
    }

    private static Sprite LoadCozy(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(CozyPath + name + ".png");

    // Cắt 9 mảnh cho sprite của Cozy UI Pack (Left, Bottom, Right, Top theo pixel của ảnh gốc).
    private static void ImportCozy()
    {
        var borders = new (string name, Vector4 border)[]
        {
            ("RoundButton2_white", new Vector4(75f, 75f, 75f, 75f)),
            ("RoundButton2_wood", new Vector4(75f, 75f, 75f, 75f)),
            ("RoundButton1_wood", Vector4.zero),
            ("Icon_heart", Vector4.zero),
            ("Icon_heart_empty", Vector4.zero),
            ("Btn_green", new Vector4(90f, 70f, 90f, 70f)),
            ("Btn_blue", new Vector4(90f, 70f, 90f, 70f)),
            ("Btn_purple", new Vector4(90f, 70f, 90f, 70f)),
            ("Btn_orange", new Vector4(90f, 70f, 90f, 70f)),
            ("Btn_gray", new Vector4(90f, 70f, 90f, 70f)),
            ("Btn_red", new Vector4(90f, 70f, 90f, 70f)),
            ("InventoryHotbar", new Vector4(40f, 40f, 40f, 40f)),
            ("WoodenContainer1", new Vector4(60f, 60f, 60f, 60f)),
            ("Container1", new Vector4(70f, 70f, 70f, 70f)),
            ("NoteContainerSimple", new Vector4(100f, 40f, 40f, 40f)),
            ("CardRegular1_wood", new Vector4(45f, 50f, 45f, 80f)),
            ("CardRegular1_red", new Vector4(45f, 50f, 45f, 80f))
        };
        foreach (var (name, border) in borders)
        {
            var path = CozyPath + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer)
            {
                Debug.LogError($"Missing {path}. Copy the Cozy UI Pack sprites into {CozyPath}.");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name + ".png");

    private static void LoadSprites()
    {
        _heartFull = LoadCozy("Icon_heart");
        _heartEmpty = LoadCozy("Icon_heart_empty");
        _btnGreen = LoadCozy("Btn_green");
        _btnBlue = LoadCozy("Btn_blue");
        _btnPurple = LoadCozy("Btn_purple");
        _btnOrange = LoadCozy("Btn_orange");
        _btnGray = LoadCozy("Btn_gray");
        _btnRed = LoadCozy("Btn_red");
        _btnWood = LoadCozy("RoundButton2_wood");
        _pillCream = LoadCozy("RoundButton2_white");
        _btnRound = LoadCozy("RoundButton1_wood");
        _plank = LoadCozy("InventoryHotbar");
        _banner = LoadCozy("WoodenContainer1");
        _tan = LoadCozy("Container1");
        _note = LoadCozy("NoteContainerSimple");
        _popupWood = LoadCozy("CardRegular1_wood");
        _popupRed = LoadCozy("CardRegular1_red");
        _coin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Icons/icon_coin.png");
        _starFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Icons/icon_star_full.png");
        _starEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Icons/icon_star_empty.png");
        _lock = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Icons/icon_lock.png");
        _boss = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Icons/icon_boss.png");
        _pause = Load("icon_pause");
        _back = Load("icon_back");
        _home = Load("icon_home");
        _soundOn = Load("icon_sound_on");
        _soundOff = Load("icon_sound_off");
        _retry = Load("icon_retry");
        _levels = Load("icon_levels");
    }

    // ---------------------------------------------------------------- GamePlay

    private class AudioSetup
    {
        public string Click, Buy, Equip, Music;
    }

    private static AudioSetup RebuildGamePlay()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(GamePlayPath, OpenSceneMode.Single);
        LoadSprites();

        var feedback = Object.FindFirstObjectByType<GameFeedback>();
        var feedbackObject = new SerializedObject(feedback);
        var audio = new AudioSetup
        {
            Click = AssetDatabase.GetAssetPath(feedbackObject.FindProperty("clickCue.clip").objectReferenceValue),
            Buy = AssetDatabase.GetAssetPath(feedbackObject.FindProperty("buyCue.clip").objectReferenceValue),
            Equip = AssetDatabase.GetAssetPath(feedbackObject.FindProperty("equipCue.clip").objectReferenceValue),
            Music = AssetDatabase.GetAssetPath(feedbackObject.FindProperty("menuMusic").objectReferenceValue)
        };

        var old = GameObject.Find("HUD");
        if (old)
        {
            Object.DestroyImmediate(old);
        }

        var session = Object.FindFirstObjectByType<GameSession>();
        var progress = Object.FindFirstObjectByType<PlayerProgress>();
        var encounter = Object.FindFirstObjectByType<BossEncounter>();
        EnsureEventSystem();

        var canvas = NewCanvas("HUD");
        var hud = canvas.gameObject.AddComponent<GameHUD>();
        var safe = Stretch("SafeArea", canvas.transform);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        // Thanh trên: Pause | tên Level; dưới là thanh trái tim.
        var pause = RoundButton("PauseButton", safe, _pause, Wood, 120f);
        Anchor(pause.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(120f, 120f));
        var levelPill = Img("LevelPill", safe, _pillCream, Color.white, Image.Type.Sliced);
        Anchor(levelPill.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(570f, 100f));
        var levelText = Text("LevelText", levelPill.transform, "LEVEL 1", 46f, Brown);
        Fill(levelText.rectTransform, 38f);
        levelText.enableAutoSizing = true;
        levelText.fontSizeMin = 18f;
        levelText.fontSizeMax = 46f;

        // Thanh máu của mục tiêu: mỗi trái tim là một lần ném còn phải trúng.
        var heartBar = new GameObject("HeartBar", typeof(RectTransform), typeof(HorizontalLayoutGroup)) { layer = 5 };
        var heartBarRect = (RectTransform)heartBar.transform;
        heartBarRect.SetParent(safe, false);
        Anchor(heartBarRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 84f));
        var heartLayout = heartBar.GetComponent<HorizontalLayoutGroup>();
        heartLayout.childAlignment = TextAnchor.MiddleCenter;
        heartLayout.spacing = 6f;
        heartLayout.childControlWidth = false;
        heartLayout.childControlHeight = false;
        heartLayout.childForceExpandWidth = false;
        heartLayout.childForceExpandHeight = false;
        var hearts = new Image[16];
        for (var i = 0; i < hearts.Length; i++)
        {
            hearts[i] = Img($"Heart{i + 1}", heartBarRect, _heartFull, Color.white, Image.Type.Simple);
            hearts[i].preserveAspect = true;
            hearts[i].rectTransform.sizeDelta = new Vector2(64f, 64f);
        }

        var timer = Text("TimerText", safe, "0.0s", 40f, Color.white);
        timer.fontSharedMaterial = OutlineMaterial();
        Anchor(timer.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(900f, 60f));

        var warning = Img("WarningBanner", safe, _btnRed, Color.white, Image.Type.Sliced);
        Anchor(warning.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(820f, 96f));
        var warningText = Text("WarningText", warning.transform, "", 44f, Color.white);
        warningText.fontSharedMaterial = OutlineMaterial();
        Fill(warningText.rectTransform, 12f);
        warning.gameObject.SetActive(false);

        var hint = Img("HintPanel", safe, _note, Color.white, Image.Type.Sliced);
        Anchor(hint.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(900f, 230f));
        var hintText = Text("HintText", hint.transform, "", 42f, Brown);
        Fill(hintText.rectTransform, 40f);
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hint.gameObject.SetActive(false);

        // Pause.
        var pausePanel = Dim("PausePanel", canvas.transform);
        var pauseCard = Card(pausePanel.transform, new Vector2(820f, 1060f), _popupWood);
        Title(pauseCard, "PAUSED", 450f, Brown);
        var resume = Button("ContinueButton", pauseCard, "RESUME", Green, new Vector2(620f, 150f), 62f);
        Anchor(resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(620f, 150f));
        var restart = Button("RestartButton", pauseCard, "RESTART", Blue, new Vector2(620f, 130f), 50f);
        Anchor(restart.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(620f, 130f));
        var levels = Button("LevelsButton", pauseCard, "LEVELS", Purple, new Vector2(620f, 130f), 50f);
        Anchor(levels.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(620f, 130f));
        var home = Button("HomeButton", pauseCard, "HOME", Red, new Vector2(620f, 130f), 50f);
        Anchor(home.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -270f), new Vector2(620f, 130f));
        var sound = RoundButton("SoundButton", pauseCard, _soundOn, Wood, 110f);
        Anchor(sound.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -425f), new Vector2(110f, 110f));
        pausePanel.gameObject.SetActive(false);

        // Result.
        var resultPanel = Dim("ResultPanel", canvas.transform);
        var resultCard = Card(resultPanel.transform, new Vector2(860f, 1160f), _popupRed);
        var resultTitle = Title(resultCard, "LEVEL CLEARED", 470f, Color.white);
        resultTitle.fontSharedMaterial = OutlineMaterial();
        var stars = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            stars[i] = Img($"Star{i + 1}", resultCard, _starFull, Color.white, Image.Type.Simple);
            stars[i].preserveAspect = true;
            var size = i == 1 ? 210f : 170f;
            Anchor(stars[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2((i - 1) * 210f, i == 1 ? 300f : 270f), new Vector2(size, size));
        }

        var detail = Text("ResultDetailText", resultCard, "", 54f, Brown);
        Anchor(detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(740f, 230f));
        detail.textWrappingMode = TextWrappingModes.Normal;
        var next = Button("NextButton", resultCard, "NEXT LEVEL", Green, new Vector2(620f, 140f), 54f);
        Anchor(next.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(620f, 140f));
        var retry = Button("RetryButton", resultCard, "RETRY", Blue, new Vector2(620f, 125f), 48f);
        Anchor(retry.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -305f), new Vector2(620f, 125f));
        var resultLevels = Button("LevelsButton", resultCard, "LEVELS", Purple, new Vector2(620f, 125f), 48f);
        Anchor(resultLevels.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -450f), new Vector2(620f, 125f));
        resultPanel.gameObject.SetActive(false);

        // Nối tham chiếu.
        Set(hud, "session", session);
        Set(hud, "progress", progress);
        Set(hud, "encounter", encounter);
        Set(hud, "levelText", levelText);
        SetArray(hud, "hearts", hearts);
        Set(hud, "heartFull", _heartFull);
        Set(hud, "heartEmpty", _heartEmpty);
        Set(hud, "warningBanner", warning.gameObject);
        Set(hud, "warningText", warningText);
        Set(hud, "hintPanel", hint.gameObject);
        Set(hud, "hintText", hintText);
        Set(hud, "timerText", timer);
        Set(hud, "resultPanel", resultPanel.gameObject);
        Set(hud, "resultTitleText", resultTitle);
        Set(hud, "resultDetailText", detail);
        SetArray(hud, "resultStars", stars);
        Set(hud, "starFull", _starFull);
        Set(hud, "starEmpty", _starEmpty);
        Set(hud, "retryButton", retry);
        Set(hud, "nextButton", next);
        Set(hud, "mapButton", resultLevels);
        Set(hud, "pausePanel", pausePanel.gameObject);
        Set(hud, "continueButton", resume);
        Set(hud, "pauseButton", pause);
        Set(hud, "pauseRetryButton", restart);
        Set(hud, "pauseMapButton", levels);
        Set(hud, "pauseHomeButton", home);
        SetArray(hud, "soundButtons", sound);
        SetArray(hud, "soundIcons", sound.transform.Find("Icon").GetComponent<Image>());
        Set(hud, "soundOnSprite", _soundOn);
        Set(hud, "soundOffSprite", _soundOff);

        SetArray(feedback, "clickButtons", pause, resume, restart, levels, home, next, retry, resultLevels);
        SetArray(feedback, "toggleButtons", sound);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return audio;
    }

    // ---------------------------------------------------------------- MainMenu

    private static void BuildMainMenu(AudioSetup audio)
    {
        var gamePlayCamera = Camera.main;
        var gamePlayBackground = GameObject.Find("background_forest");
        var cameraSize = gamePlayCamera ? gamePlayCamera.orthographicSize : 5f;
        var cameraColor = gamePlayCamera ? gamePlayCamera.backgroundColor : Color.black;
        // NewScene giải phóng asset đang không được scene nào dùng, nên chỉ nhớ đường dẫn rồi nạp lại sau đó.
        var backgroundSpritePath = gamePlayBackground ? AssetDatabase.GetAssetPath(gamePlayBackground.GetComponent<SpriteRenderer>().sprite) : "";
        var backgroundPosition = gamePlayBackground ? gamePlayBackground.transform.position : Vector3.zero;
        var backgroundMaterialPath = gamePlayBackground ? AssetDatabase.GetAssetPath(gamePlayBackground.GetComponent<SpriteRenderer>().sharedMaterial) : "";
        var backgroundScale = gamePlayBackground ? gamePlayBackground.transform.localScale : Vector3.one;
        var targetSpritePath = AssetDatabase.GetAssetPath(Object.FindFirstObjectByType<RotatingTarget>().transform.Find("Visual").GetComponent<SpriteRenderer>().sprite);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LoadSprites();
        var campaign = AssetDatabase.LoadAssetAtPath<CampaignConfig>(CampaignPath);
        var targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetSpritePath);
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundSpritePath);
        var backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(backgroundMaterialPath);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = cameraSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = cameraColor;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var background = new GameObject("Background", typeof(SpriteRenderer));
        background.transform.position = backgroundPosition;
        background.transform.localScale = backgroundScale;
        var backgroundRenderer = background.GetComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite;
        if (backgroundMaterial)
        {
            backgroundRenderer.sharedMaterial = backgroundMaterial;
        }

        EnsureEventSystem();

        var progressObject = new GameObject("PlayerProgress");
        var progress = progressObject.AddComponent<PlayerProgress>();
        Set(progress, "campaign", campaign);

        var canvas = NewCanvas("MenuCanvas");
        var safe = Stretch("SafeArea", canvas.transform);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        var menu = canvas.gameObject.AddComponent<MenuScreens>();
        var mapScreen = canvas.gameObject.AddComponent<LevelMapScreen>();
        var inventory = canvas.gameObject.AddComponent<KnifeInventoryScreen>();

        // ---- Home
        var home = Stretch("HomePanel", safe);
        var banner = Img("TitleBanner", home, _banner, Color.white, Image.Type.Sliced);
        Anchor(banner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(880f, 240f));
        var title = Text("TitleText", banner.transform, "KNIFE HIT", 150f, Brown);
        Fill(title.rectTransform, 20f);

        var spin = new GameObject("TargetSpin", typeof(RectTransform), typeof(SlowSpin));
        var spinRect = (RectTransform)spin.transform;
        spinRect.SetParent(home, false);
        Anchor(spinRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 560f));
        var targetImage = Img("Target", spinRect, targetSprite, Color.white, Image.Type.Simple);
        targetImage.preserveAspect = true;
        Fill(targetImage.rectTransform, 0f);
        var knifeSprite = campaign.DefaultSkin.Sprite;
        const int decorKnives = 6;
        for (var i = 0; i < decorKnives; i++)
        {
            var angle = i * 360f / decorKnives + 15f;
            var radians = angle * Mathf.Deg2Rad;
            var knife = Img($"DecorKnife{i + 1}", spinRect, knifeSprite, campaign.DefaultSkin.Tint, Image.Type.Simple);
            knife.preserveAspect = true;
            knife.transform.SetAsFirstSibling();
            Anchor(knife.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians)) * 290f, new Vector2(130f, 300f));
            knife.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f + angle);
        }

        var play = Button("PlayButton", home, "PLAY", Green, new Vector2(680f, 180f), 80f);
        Anchor(play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 340f), new Vector2(680f, 180f));
        var levelsButton = Button("LevelsButton", home, "LEVELS", Blue, new Vector2(330f, 130f), 46f);
        Anchor(levelsButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-175f, 170f), new Vector2(330f, 130f));
        var knivesButton = Button("KnivesButton", home, "KNIVES", Purple, new Vector2(330f, 130f), 46f);
        Anchor(knivesButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(175f, 170f), new Vector2(330f, 130f));

        // Thanh thông số + âm thanh nằm trên mọi trang để luôn thấy tiến trình.
        var levelsStat = StatPill("LevelsStat", safe, _levels, 0);
        var starsStat = StatPill("StarsStat", safe, _starFull, 1);
        var coinsStat = StatPill("CoinsStat", safe, _coin, 2);
        var sound = RoundButton("SoundButton", safe, _soundOn, Wood, 100f);
        Anchor(sound.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(100f, 100f));

        // ---- Map
        var map = Stretch("MapPanel", safe);
        var mapBack = RoundButton("BackButton", map, _back, Wood, 110f);
        Anchor(mapBack.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -170f), new Vector2(110f, 110f));
        var worldBanner = Img("WorldBanner", map, _banner, Color.white, Image.Type.Sliced);
        Anchor(worldBanner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -160f), new Vector2(760f, 130f));
        var worldTitle = Text("WorldTitleText", worldBanner.transform, "WORLD", 64f, Brown);
        Fill(worldTitle.rectTransform, 12f);

        var tabs = new UnityEngine.UI.Button[3];
        for (var i = 0; i < 3; i++)
        {
            tabs[i] = Button($"WorldTab{i + 1}", map, $"WORLD {i + 1}", Wood, new Vector2(300f, 100f), 40f);
            Anchor(tabs[i].transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 320f, -320f), new Vector2(300f, 100f));
        }

        var grid = Img("LevelsCard", map, _tan, Color.white, Image.Type.Sliced);
        Anchor(grid.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(980f, 1240f));
        var levelButtons = new LevelButtonView[CampaignConfig.LevelsPerWorld];
        for (var i = 0; i < levelButtons.Length; i++)
        {
            var position = new Vector2(-235f + (i % 2) * 470f, 470f - (i / 2) * 230f);
            levelButtons[i] = LevelButton($"Level{i + 1}", grid.transform, position);
        }

        // ---- Knives
        var knives = Stretch("KnivesPanel", safe);
        var knivesBack = RoundButton("BackButton", knives, _back, Wood, 110f);
        Anchor(knivesBack.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -170f), new Vector2(110f, 110f));
        var knivesBanner = Img("KnivesBanner", knives, _banner, Color.white, Image.Type.Sliced);
        Anchor(knivesBanner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -160f), new Vector2(760f, 130f));
        var knivesTitle = Text("KnivesTitleText", knivesBanner.transform, "KNIVES", 64f, Brown);
        Fill(knivesTitle.rectTransform, 12f);
        var coinsText = Text("CoinsText", knives, "COINS 0", 46f, Color.white);
        Anchor(coinsText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(700f, 70f));
        var cards = new SkinCardView[9];
        for (var i = 0; i < cards.Length; i++)
        {
            var position = new Vector2(-330f + (i % 3) * 330f, 180f - (i / 3) * 410f);
            cards[i] = SkinCard($"SkinCard{i + 1}", knives, position);
        }

        map.gameObject.SetActive(false);
        knives.gameObject.SetActive(false);

        // Âm thanh.
        var audioObject = new GameObject("MenuFeedback");
        var sfx = audioObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        var music = audioObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        var feedback = audioObject.AddComponent<MenuFeedback>();
        Set(feedback, "progress", progress);
        Set(feedback, "source", sfx);
        Set(feedback, "musicSource", music);
        Set(feedback, "menuMusic", Reload(audio.Music));
        Set(feedback, "clickClip", Reload(audio.Click));
        Set(feedback, "buyClip", Reload(audio.Buy));
        Set(feedback, "equipClip", Reload(audio.Equip));
        Set(feedback, "buttonRoot", canvas.transform);

        // Nối tham chiếu.
        Set(menu, "progress", progress);
        Set(menu, "homePanel", home.gameObject);
        Set(menu, "mapPanel", map.gameObject);
        Set(menu, "knivesPanel", knives.gameObject);
        Set(menu, "playLabel", play.GetComponentInChildren<TMP_Text>());
        Set(menu, "levelsStatText", levelsStat.value);
        Set(menu, "starsStatText", starsStat.value);
        Set(menu, "coinsStatText", coinsStat.value);
        Set(menu, "playButton", play);
        Set(menu, "levelsButton", levelsButton);
        Set(menu, "knivesButton", knivesButton);
        SetArray(menu, "backButtons", mapBack, knivesBack);
        SetArray(menu, "soundButtons", sound);
        SetArray(menu, "soundIcons", sound.transform.Find("Icon").GetComponent<Image>());
        Set(menu, "soundOnSprite", _soundOn);
        Set(menu, "soundOffSprite", _soundOff);
        Set(menu, "mapScreen", mapScreen);
        Set(menu, "inventoryScreen", inventory);

        Set(mapScreen, "progress", progress);
        Set(mapScreen, "worldTitleText", worldTitle);
        SetArray(mapScreen, "worldTabs", tabs);
        SetArray(mapScreen, "levelButtons", levelButtons);

        Set(inventory, "progress", progress);
        Set(inventory, "coinsText", coinsText);
        SetArray(inventory, "cards", cards);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainMenuPath);
    }

    // Chữ trắng viền nâu cho nút màu; tạo từ preset Outline của TMP để dùng chung atlas LiberationSans.
    private static Material OutlineMaterial()
    {
        const string path = "Assets/_Project/Art/UI/Fonts/OutlineBrown.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing)
        {
            return existing;
        }

        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
        Directory.CreateDirectory("Assets/_Project/Art/UI/Fonts");
        var material = new Material(source) { name = "OutlineBrown" };
        material.SetColor("_OutlineColor", new Color(0.3f, 0.14f, 0.08f, 1f));
        material.SetFloat("_OutlineWidth", 0.22f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static AudioClip Reload(string path)
    {
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static (RectTransform pill, TMP_Text value) StatPill(string name, Transform parent, Sprite icon, int index)
    {
        var pill = Img(name, parent, _pillCream, Color.white, Image.Type.Sliced);
        Anchor(pill.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f + index * 276f, -45f), new Vector2(260f, 84f));
        var iconImage = Img("Icon", pill.transform, icon, icon == _levels ? Brown : Color.white, Image.Type.Simple);
        iconImage.preserveAspect = true;
        Anchor(iconImage.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(56f, 56f));
        var value = Text("Value", pill.transform, "0", 46f, Brown, TextAlignmentOptions.Right);
        Anchor(value.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(160f, 70f));
        return (pill.rectTransform, value);
    }

    private static LevelButtonView LevelButton(string name, Transform parent, Vector2 position)
    {
        var button = Button(name, parent, "", Orange, new Vector2(440f, 205f), 40f);
        Anchor(button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(440f, 205f));
        var number = button.GetComponentInChildren<TMP_Text>(true);
        number.name = "NumberText";
        number.enableAutoSizing = false;
        number.fontSize = 76f;
        Anchor(number.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f), new Vector2(380f, 100f));
        var status = Text("StatusText", button.transform, "", 38f, Color.white);
        status.fontSharedMaterial = OutlineMaterial();
        Anchor(status.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(380f, 60f));

        var stars = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            stars[i] = Img($"Star{i + 1}", button.transform, _starEmpty, Color.white, Image.Type.Simple);
            stars[i].preserveAspect = true;
            Anchor(stars[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 64f, -54f), new Vector2(60f, 60f));
        }

        var lockIcon = Img("LockIcon", button.transform, _lock, Color.white, Image.Type.Simple);
        lockIcon.preserveAspect = true;
        Anchor(lockIcon.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(56f, 56f));
        var bossIcon = Img("BossIcon", button.transform, _boss, Color.white, Image.Type.Simple);
        bossIcon.preserveAspect = true;
        Anchor(bossIcon.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(56f, 56f));

        var view = button.gameObject.AddComponent<LevelButtonView>();
        Set(view, "button", button);
        Set(view, "background", button.GetComponent<Image>());
        Set(view, "numberText", number);
        Set(view, "statusText", status);
        Set(view, "lockIcon", lockIcon);
        Set(view, "bossIcon", bossIcon);
        SetArray(view, "starIcons", stars);
        Set(view, "starFull", _starFull);
        Set(view, "starEmpty", _starEmpty);
        SetColor(view, "lockedColor", Color.white);
        SetColor(view, "newColor", Color.white);
        SetColor(view, "clearedColor", Color.white);
        SetColor(view, "bossColor", Color.white);
        Set(view, "lockedSprite", _btnGray);
        Set(view, "newSprite", _btnOrange);
        Set(view, "clearedSprite", _btnGreen);
        Set(view, "bossSprite", _btnRed);
        return view;
    }

    private static SkinCardView SkinCard(string name, Transform parent, Vector2 position)
    {
        var card = Img(name, parent, _popupWood, Color.white, Image.Type.Sliced);
        Anchor(card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(310f, 380f));
        var icon = Img("Icon", card.transform, null, Color.white, Image.Type.Simple);
        icon.preserveAspect = true;
        Anchor(icon.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(110f, 190f));
        var nameText = Text("NameText", card.transform, "", 38f, Brown);
        Anchor(nameText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(280f, 56f));
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 24f;
        nameText.fontSizeMax = 38f;
        var action = Button("ActionButton", card.transform, "", Green, new Vector2(270f, 90f), 30f);
        Anchor(action.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(270f, 90f));
        var label = action.GetComponentInChildren<TMP_Text>(true);
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 32f;

        var view = card.gameObject.AddComponent<SkinCardView>();
        Set(view, "icon", icon);
        Set(view, "nameText", nameText);
        Set(view, "button", action);
        Set(view, "buttonLabel", label);
        return view;
    }

    // ---------------------------------------------------------------- widgets

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>())
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas NewCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        return canvas;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Fill(rect, 0f);
        return rect;
    }

    private static void Fill(RectTransform rect, float margin)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }

    private static void Anchor(Transform transform, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color, Image.Type type)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)) { layer = 5 };
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)) { layer = 5 };
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string label, Color tint, Vector2 size, float fontSize,
        Sprite sprite = null, Color? textColor = null)
    {
        // Mỗi màu của nút là một sprite riêng của Cozy UI Pack (đã xóa chữ in sẵn), không nhuộm màu.
        var isWood = tint == Wood;
        sprite ??= tint == Red ? _btnRed
            : isWood ? _btnWood
            : tint == Blue ? _btnBlue
            : tint == Purple ? _btnPurple
            : tint == Orange ? _btnOrange
            : tint == Gray ? _btnGray
            : _btnGreen;
        var image = Img(name, parent, sprite, Color.white, Image.Type.Sliced);
        image.raycastTarget = true;
        image.rectTransform.sizeDelta = size;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        StyleButton(button, image);
        var text = Text("Label", image.transform, label, fontSize, textColor ?? (isWood ? Brown : Color.white));
        Fill(text.rectTransform, 10f);
        text.rectTransform.offsetMin = new Vector2(10f, 22f);
        if (!isWood && textColor == null)
        {
            text.fontSharedMaterial = OutlineMaterial();
        }

        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Max(18f, fontSize * 0.5f);
        return button;
    }

    private static UnityEngine.UI.Button RoundButton(string name, Transform parent, Sprite icon, Color tint, float size)
    {
        var image = Img(name, parent, _btnRound, Color.white, Image.Type.Simple);
        image.raycastTarget = true;
        image.rectTransform.sizeDelta = new Vector2(size, size * 1.13f);
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        StyleButton(button, image);
        var iconImage = Img("Icon", image.transform, icon, Brown, Image.Type.Simple);
        iconImage.preserveAspect = true;
        Anchor(iconImage.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.07f),
            new Vector2(size * 0.58f, size * 0.58f));
        return button;
    }

    private static void StyleButton(UnityEngine.UI.Button button, Image target)
    {
        button.targetGraphic = target;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
    }

    private static RectTransform Dim(string name, Transform parent)
    {
        var dim = Img(name, parent, null, new Color(0f, 0f, 0f, 0.7f), Image.Type.Simple);
        dim.raycastTarget = true;
        Fill(dim.rectTransform, 0f);
        return dim.rectTransform;
    }

    private static RectTransform Card(Transform parent, Vector2 size, Sprite sprite)
    {
        var card = Img("Card", parent, sprite, Color.white, Image.Type.Sliced);
        // Nhân đôi cỡ viền để phần đầu thẻ đủ cao cho tiêu đề.
        card.pixelsPerUnitMultiplier = 0.5f;
        Anchor(card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return card.rectTransform;
    }

    private static TMP_Text Title(Transform parent, string text, float y, Color color)
    {
        var title = Text("TitleText", parent, text, 84f, color);
        Anchor(title.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 120f));
        title.enableAutoSizing = true;
        title.fontSizeMin = 40f;
        title.fontSizeMax = 84f;
        return title;
    }

    private static void SetColor(Object target, string property, Color value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).colorValue = value;
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

    private static void SetArray(Object target, string property, params Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(property);
        if (p == null)
        {
            Debug.LogError($"{target.GetType().Name} has no serialized field '{property}'.");
            return;
        }

        p.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
        {
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
