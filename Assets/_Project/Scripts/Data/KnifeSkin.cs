using UnityEngine;

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "Skin", menuName = "Knife Hit/Knife Skin")]
public class KnifeSkin : ScriptableObject
{
    [Tooltip("Khóa ổn định dùng trong file lưu; đừng đổi sau khi phát hành")]
    [SerializeField] private string skinId = "";
    [SerializeField] private string displayName = "Knife";
    [Tooltip("Chỉ đổi hình; kích thước và hình học va chạm vẫn do KnifeLauncher quyết định")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color tint = Color.white;
    [Tooltip("Giá bằng coin; 0 với skin không phải mặc định nghĩa là chỉ mở khóa bằng cách thắng một Level")]
    [SerializeField, Min(0)] private int price;

    public string SkinId => skinId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public Color Tint => tint;
    public int Price => price;

    // Gán hình và màu của skin cho prefab dao vừa tạo.
    public void ApplyTo(GameObject knife)
    {
        var knifeRenderer = knife.GetComponent<SpriteRenderer>();
        if (!knifeRenderer)
        {
            return;
        }

        if (sprite)
        {
            knifeRenderer.sprite = sprite;
        }

        knifeRenderer.color = tint;
    }
}
