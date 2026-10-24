using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
// Chuyển giữa scene MainMenu và GamePlay, mang theo Level đã chọn (hoặc trang menu cần mở khi quay về).
// Tên scene phải khớp Build Profile; chỉ giữ dữ liệu tạm cho một lần chuyển scene.
public static class SceneFlow
{
    public enum MenuPage
    {
        Home,
        Map,
        Knives
    }

    public const string MainMenuScene = "MainMenu";
    public const string GamePlayScene = "GamePlay";

    private static string _pendingLevelId = "";
    private static MenuPage _pendingPage = MenuPage.Home;

    public static void OpenGameplay(LevelConfig level)
    {
        if (!level)
        {
            return;
        }

        _pendingLevelId = level.LevelId;
        SceneManager.LoadScene(GamePlayScene);
    }

    public static void OpenMainMenu(MenuPage page)
    {
        _pendingPage = page;
        SceneManager.LoadScene(MainMenuScene);
    }

    // Trả về null khi không có Level chờ (ví dụ bấm Play trực tiếp scene GamePlay trong Editor).
    public static LevelConfig TakePendingLevel(CampaignConfig campaign)
    {
        var level = string.IsNullOrEmpty(_pendingLevelId) ? null : campaign.FindLevel(_pendingLevelId);
        _pendingLevelId = "";
        return level;
    }

    public static MenuPage TakePendingPage()
    {
        var page = _pendingPage;
        _pendingPage = MenuPage.Home;
        return page;
    }
}
