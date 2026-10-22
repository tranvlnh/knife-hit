using UnityEngine;

// ReSharper disable once CheckNamespace
[CreateAssetMenu(fileName = "World", menuName = "Knife Hit/World")]
public class WorldConfig : ScriptableObject
{
    [SerializeField] private string displayName = "World";
    [SerializeField] private Sprite background;
    [SerializeField] private Color backgroundTint = Color.white;
    [SerializeField] private Color targetTint = Color.white;
    [Tooltip("Nhạc nền lặp của World; có thể bỏ trống")]
    [SerializeField] private AudioClip music;

    public string DisplayName => displayName;
    public Sprite Background => background;
    public Color BackgroundTint => backgroundTint;
    public Color TargetTint => targetTint;
    public AudioClip Music => music;
}
