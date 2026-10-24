using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
// Âm thanh của scene MainMenu: nhạc nền, tiếng bấm nút, mua và trang bị dao. Không ảnh hưởng gameplay.
public class MenuFeedback : MonoBehaviour
{
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.3f;
    [SerializeField] private AudioClip buyClip;
    [SerializeField, Range(0f, 1f)] private float buyVolume = 0.55f;
    [SerializeField] private AudioClip equipClip;
    [SerializeField, Range(0f, 1f)] private float equipVolume = 0.4f;
    [Tooltip("Mọi Button dưới object này (kể cả đang ẩn) đều phát tiếng bấm")]
    [SerializeField] private Transform buttonRoot;

    private int _previousCoins;
    private KnifeSkin _previousSkin;

    private void Awake()
    {
        if (!progress || !source)
        {
            Debug.LogError("Assign PlayerProgress and an AudioSource.", this);
            enabled = false;
        }
    }

    // Đăng ký ở Start để các nút do script khác sinh ra trong Awake đã tồn tại.
    private void Start()
    {
        if (buttonRoot)
        {
            foreach (var button in buttonRoot.GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(() => Play(clickClip, clickVolume));
            }
        }

        if (musicSource && menuMusic)
        {
            musicSource.clip = menuMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void OnEnable()
    {
        progress.Changed += OnProgressChanged;
        _previousCoins = progress.Coins;
        _previousSkin = progress.EquippedSkin;
    }

    private void OnDisable()
    {
        progress.Changed -= OnProgressChanged;
    }

    private void OnProgressChanged()
    {
        if (progress.Coins < _previousCoins)
        {
            Play(buyClip, buyVolume);
        }
        else if (progress.EquippedSkin != _previousSkin)
        {
            Play(equipClip, equipVolume);
        }

        _previousCoins = progress.Coins;
        _previousSkin = progress.EquippedSkin;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip)
        {
            source.PlayOneShot(clip, volume);
        }
    }
}
