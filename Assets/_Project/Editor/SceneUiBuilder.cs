using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Dựng một lần UI Home / Level Map / Knives, gợi ý và panel kết quả mới trong GamePlay.unity bằng cách nhân bản
// các nút/chữ có sẵn (để giữ nguyên phong cách), rồi nối mọi tham chiếu Inspector. Chạy lại sẽ tạo thêm bản sao.
public static class SceneUiBuilder
{
    private static readonly Vector2 ButtonSize = new(520f, 150f);

    [MenuItem("Knife Hit/Build Scene UI (one-time)")]
    public static void Build()
    {
        var hud = GameObject.Find("HUD");
        if (!hud || hud.transform.Find("SafeArea/StartPanel") == null)
        {
            Debug.LogError("Scene UI was already rebuilt or HUD/SafeArea/StartPanel is missing.");
            return;
        }

        var safe = (RectTransform)hud.transform.Find("SafeArea");
        var startPanel = safe.Find("StartPanel");
        var resultPanel = (RectTransform)safe.Find("ResultPanel");
        var pausePanel = safe.Find("PausePanel");

        var buttonTemplate = resultPanel.Find("ReplayButton").GetComponent<Button>();
        var titleTemplate = startPanel.Find("StartTitleText").GetComponent<TMP_Text>();
        var smallTextTemplate = safe.Find("StageText").GetComponent<TMP_Text>();

        // Gameplay root: các thành phần chỉ hiện khi đang chơi.
        var gameplayRoot = NewStretch("GameplayRoot", safe);
        gameplayRoot.SetSiblingIndex(0);
        safe.Find("KnivesCounter").SetParent(gameplayRoot, false);
        safe.Find("StageText").SetParent(gameplayRoot, false);
        var levelText = gameplayRoot.Find("StageText").GetComponent<TMP_Text>();
        levelText.name = "LevelText";

        var warning = CloneText(smallTextTemplate, "WarningText", gameplayRoot, "", new Vector2(0f, -190f), new Vector2(900f, 90f));
        warning.color = new Color(1f, 0.45f, 0.35f);
        var hintPanel = NewStretch("HintPanel", gameplayRoot);
        hintPanel.anchorMin = new Vector2(0.5f, 1f);
        hintPanel.anchorMax = new Vector2(0.5f, 1f);
        hintPanel.pivot = new Vector2(0.5f, 1f);
        hintPanel.anchoredPosition = new Vector2(0f, -300f);
        hintPanel.sizeDelta = new Vector2(940f, 230f);
        var hintBackdrop = hintPanel.gameObject.AddComponent<Image>();
        hintBackdrop.color = new Color(0f, 0f, 0f, 0.6f);
        hintBackdrop.raycastTarget = false;
        var hintText = CloneText(smallTextTemplate, "HintText", hintPanel, "", Vector2.zero, new Vector2(900f, 210f));
        hintText.fontSize = 44f;
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintPanel.gameObject.SetActive(false);

        // Result panel.
        var resultTitle = resultPanel.Find("ResultTitleText").GetComponent<TMP_Text>();
        resultTitle.rectTransform.anchoredPosition = new Vector2(0f, 330f);
        var detail = CloneText(smallTextTemplate, "ResultDetailText", resultPanel, "", new Vector2(0f, 120f), new Vector2(960f, 240f));
        detail.fontSize = 52f;
        detail.textWrappingMode = TextWrappingModes.Normal;
        var retry = buttonTemplate;
        retry.name = "RetryButton";
        SetLabel(retry, "RETRY");
        PlaceButton(retry, new Vector2(0f, -330f));
        var next = CloneButton(buttonTemplate, "NextButton", resultPanel, "NEXT LEVEL", new Vector2(0f, -150f));
        var resultMap = CloneButton(buttonTemplate, "MapButton", resultPanel, "LEVELS", new Vector2(0f, -510f));

        // Menu root + Home.
        var menuRoot = NewStretch("MenuRoot", safe);
        var home = NewStretch("HomePanel", menuRoot);
        var title = startPanel.Find("StartTitleText").GetComponent<TMP_Text>();
        title.transform.SetParent(home, false);
        title.rectTransform.anchoredPosition = new Vector2(0f, 480f);
        title.text = "KNIFE HIT";
        var progressText = CloneText(smallTextTemplate, "ProgressText", home, "", new Vector2(0f, 300f), new Vector2(1000f, 90f));
        progressText.fontSize = 44f;
        var playButton = startPanel.Find("PlayButton").GetComponent<Button>();
        playButton.transform.SetParent(home, false);
        PlaceButton(playButton, new Vector2(0f, 100f));
        var levelsButton = CloneButton(buttonTemplate, "LevelsButton", home, "LEVELS", new Vector2(0f, -100f));
        var knivesButton = CloneButton(buttonTemplate, "KnivesButton", home, "KNIVES", new Vector2(0f, -300f));
        var homeSound = startPanel.Find("SoundButton").GetComponent<Button>();
        homeSound.transform.SetParent(home, false);
        PlaceButton(homeSound, new Vector2(0f, -500f));
        var pauseSound = pausePanel.Find("SoundButton").GetComponent<Button>();
        Object.DestroyImmediate(startPanel.gameObject);

        // Level Map.
        var map = NewStretch("MapPanel", menuRoot);
        var worldTitle = CloneText(smallTextTemplate, "WorldTitleText", map, "WORLD", new Vector2(0f, 850f), new Vector2(1000f, 110f));
        var tabs = new Button[3];
        for (var i = 0; i < 3; i++)
        {
            tabs[i] = CloneButton(buttonTemplate, $"WorldTab{i + 1}", map, $"WORLD {i + 1}", new Vector2(-330f + i * 330f, 720f));
            tabs[i].GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 110f);
            tabs[i].GetComponentInChildren<TMP_Text>().fontSize = 40f;
        }

        var levelButtons = new LevelButtonView[CampaignConfig.LevelsPerWorld];
        for (var i = 0; i < levelButtons.Length; i++)
        {
            var column = i % 2;
            var row = i / 2;
            var position = new Vector2(-190f + column * 380f, 530f - row * 190f);
            levelButtons[i] = BuildLevelButton(buttonTemplate, smallTextTemplate, map, $"Level{i + 1}", position);
        }

        var mapBack = CloneButton(buttonTemplate, "MapBackButton", map, "BACK", new Vector2(0f, -800f));

        // Knives.
        var knives = NewStretch("KnivesPanel", menuRoot);
        var coinsText = CloneText(smallTextTemplate, "CoinsText", knives, "COINS 0", new Vector2(0f, 850f), new Vector2(1000f, 110f));
        var cards = new SkinCardView[9];
        for (var i = 0; i < cards.Length; i++)
        {
            var position = new Vector2(-340f + (i % 3) * 340f, 470f - (i / 3) * 430f);
            cards[i] = BuildSkinCard(buttonTemplate, smallTextTemplate, knives, $"SkinCard{i + 1}", position);
        }

        var knivesBack = CloneButton(buttonTemplate, "KnivesBackButton", knives, "BACK", new Vector2(0f, -800f));
        map.gameObject.SetActive(false);
        knives.gameObject.SetActive(false);

        // Menu controller (luôn bật, giữ các script màn hình).
        var menuObject = new GameObject("Menu", typeof(RectTransform));
        menuObject.transform.SetParent(hud.transform, false);
        var campaign = AssetDatabase.LoadAssetAtPath<CampaignConfig>("Assets/_Project/Data/Campaign.asset");
        var session = Object.FindFirstObjectByType<GameSession>();
        var launcher = Object.FindFirstObjectByType<KnifeLauncher>();
        var target = Object.FindFirstObjectByType<RotatingTarget>();
        var gameHud = Object.FindFirstObjectByType<GameHUD>();
        var feedback = Object.FindFirstObjectByType<GameFeedback>();

        var progressObject = new GameObject("PlayerProgress");
        var progress = progressObject.AddComponent<PlayerProgress>();
        Set(progress, "campaign", campaign);

        var encounterObject = new GameObject("BossEncounter");
        var encounter = encounterObject.AddComponent<BossEncounter>();
        Set(encounter, "target", target);
        Set(encounter, "targetVisual", target.transform.Find("Visual").GetComponent<SpriteRenderer>());

        var menu = menuObject.AddComponent<MenuScreens>();
        var mapScreen = menuObject.AddComponent<LevelMapScreen>();
        var inventory = menuObject.AddComponent<KnifeInventoryScreen>();

        Set(session, "progress", progress);
        Set(session, "encounter", encounter);
        Set(session, "background", GameObject.Find("background_forest").GetComponent<SpriteRenderer>());
        Set(launcher, "progress", progress);

        Set(menu, "session", session);
        Set(menu, "progress", progress);
        Set(menu, "menuRoot", menuRoot.gameObject);
        Set(menu, "homePanel", home.gameObject);
        Set(menu, "mapPanel", map.gameObject);
        Set(menu, "knivesPanel", knives.gameObject);
        Set(menu, "playLabel", playButton.GetComponentInChildren<TMP_Text>());
        Set(menu, "progressText", progressText);
        Set(menu, "playButton", playButton);
        Set(menu, "levelsButton", levelsButton);
        Set(menu, "knivesButton", knivesButton);
        SetArray(menu, "backButtons", mapBack, knivesBack);
        Set(menu, "mapScreen", mapScreen);
        Set(menu, "inventoryScreen", inventory);

        Set(mapScreen, "session", session);
        Set(mapScreen, "progress", progress);
        Set(mapScreen, "worldTitleText", worldTitle);
        SetArray(mapScreen, "worldTabs", tabs);
        SetArray(mapScreen, "levelButtons", levelButtons);

        Set(inventory, "progress", progress);
        Set(inventory, "coinsText", coinsText);
        SetArray(inventory, "cards", cards);

        Set(gameHud, "session", session);
        Set(gameHud, "progress", progress);
        Set(gameHud, "encounter", encounter);
        Set(gameHud, "menu", menu);
        Set(gameHud, "gameplayRoot", gameplayRoot.gameObject);
        Set(gameHud, "levelText", levelText);
        Set(gameHud, "knivesText", gameplayRoot.Find("KnivesCounter/KnivesText").GetComponent<TMP_Text>());
        Set(gameHud, "knifeIcon", gameplayRoot.Find("KnivesCounter/KnifeIcon").GetComponent<Image>());
        Set(gameHud, "warningText", warning);
        Set(gameHud, "hintPanel", hintPanel.gameObject);
        Set(gameHud, "hintText", hintText);
        Set(gameHud, "resultPanel", resultPanel.gameObject);
        Set(gameHud, "resultTitleText", resultTitle);
        Set(gameHud, "resultDetailText", detail);
        Set(gameHud, "retryButton", retry);
        Set(gameHud, "nextButton", next);
        Set(gameHud, "mapButton", resultMap);
        Set(gameHud, "pausePanel", pausePanel.gameObject);
        Set(gameHud, "continueButton", pausePanel.Find("ContinueButton").GetComponent<Button>());
        SetArray(gameHud, "soundButtons", homeSound, pauseSound);

        var clicks = new System.Collections.Generic.List<Button>
        {
            retry, next, resultMap, playButton, levelsButton, knivesButton, mapBack, knivesBack,
            pausePanel.Find("ContinueButton").GetComponent<Button>()
        };
        clicks.AddRange(tabs);
        foreach (var view in levelButtons)
        {
            clicks.Add(view.Button);
        }

        foreach (var card in cards)
        {
            clicks.Add(card.Button);
        }

        SetArray(feedback, "clickButtons", clicks.ToArray());
        SetArray(feedback, "toggleButtons", homeSound, pauseSound);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Scene UI built and saved.");
    }

    private static RectTransform NewStretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void SetLabel(Button button, string text)
    {
        button.GetComponentInChildren<TMP_Text>(true).text = text;
    }

    private static void PlaceButton(Button button, Vector2 position)
    {
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = ButtonSize;
    }

    private static Button CloneButton(Button template, string name, Transform parent, string label, Vector2 position)
    {
        var clone = Object.Instantiate(template.gameObject, parent, false);
        clone.name = name;
        clone.SetActive(true);
        var button = clone.GetComponent<Button>();
        PlaceButton(button, position);
        SetLabel(button, label);
        return button;
    }

    private static TMP_Text CloneText(TMP_Text template, string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        var clone = Object.Instantiate(template.gameObject, parent, false);
        clone.name = name;
        var rect = (RectTransform)clone.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var tmp = clone.GetComponent<TMP_Text>();
        tmp.text = text;
        return tmp;
    }

    private static LevelButtonView BuildLevelButton(Button buttonTemplate, TMP_Text textTemplate, Transform parent, string name, Vector2 position)
    {
        var button = CloneButton(buttonTemplate, name, parent, "", position);
        ((RectTransform)button.transform).sizeDelta = new Vector2(350f, 170f);
        var number = button.GetComponentInChildren<TMP_Text>(true);
        number.name = "NumberText";
        number.fontSize = 60f;
        ((RectTransform)number.transform).anchorMin = new Vector2(0f, 0.3f);
        var status = Object.Instantiate(number.gameObject, button.transform, false);
        status.name = "StatusText";
        var statusRect = (RectTransform)status.transform;
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = new Vector2(1f, 0.38f);
        statusRect.offsetMin = statusRect.offsetMax = Vector2.zero;
        ((RectTransform)number.transform).anchorMax = Vector2.one;
        var statusText = status.GetComponent<TMP_Text>();
        statusText.fontSize = 36f;

        var view = button.gameObject.AddComponent<LevelButtonView>();
        Set(view, "button", button);
        Set(view, "background", button.GetComponent<Image>());
        Set(view, "numberText", number);
        Set(view, "statusText", statusText);
        return view;
    }

    private static SkinCardView BuildSkinCard(Button buttonTemplate, TMP_Text textTemplate, Transform parent, string name, Vector2 position)
    {
        var card = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)card.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 400f);
        var backdrop = card.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.45f);
        backdrop.raycastTarget = false;

        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 90f);
        iconRect.sizeDelta = new Vector2(110f, 190f);
        var icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var nameText = CloneText(textTemplate, "NameText", rect, "", new Vector2(0f, -50f), new Vector2(300f, 60f));
        nameText.fontSize = 38f;
        var button = CloneButton(buttonTemplate, "ActionButton", rect, "", new Vector2(0f, -135f));
        ((RectTransform)button.transform).sizeDelta = new Vector2(290f, 100f);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        label.fontSize = 28f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 30f;

        var view = card.AddComponent<SkinCardView>();
        Set(view, "icon", icon);
        Set(view, "nameText", nameText);
        Set(view, "button", button);
        Set(view, "buttonLabel", label);
        return view;
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
