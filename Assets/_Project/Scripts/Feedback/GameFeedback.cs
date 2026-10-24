using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
public class GameFeedback : MonoBehaviour
{
    [Serializable]
    private class Cue
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
    }

    [SerializeField] private GameSession session;
    [SerializeField] private AudioSource source;
    [Tooltip("Có thể bỏ trống: dùng để lặp nhạc nền của World")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
    [Tooltip("Nhạc nền khi ở menu hoặc khi World không có nhạc")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("Có thể bỏ trống: để phát tiếng báo Guard / Danger Zone")]
    [SerializeField] private BossEncounter encounter;
    [Tooltip("Có thể bỏ trống: để phát tiếng mua / trang bị dao")]
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private Cue throwCue = new() { volume = 0.25f };
    [Tooltip("Chọn ngẫu nhiên một clip cho mỗi lần dao cắm thành công")]
    [SerializeField] private AudioClip[] woodHitClips = { };
    [SerializeField, Range(0f, 1f)] private float woodHitVolume = 0.65f;
    [SerializeField] private Cue clashCue = new() { volume = 0.65f };
    [SerializeField] private Cue stageClearCue = new() { volume = 0.45f };
    [SerializeField] private Cue runFailedCue = new() { volume = 0.35f };
    [SerializeField] private Cue victoryCue = new() { volume = 0.45f };
    [SerializeField] private Cue clickCue = new() { volume = 0.3f };
    [SerializeField] private Cue toggleCue = new() { volume = 0.3f };
    [SerializeField] private Cue guardWarningCue = new() { volume = 0.5f };
    [SerializeField] private Cue dangerWarningCue = new() { volume = 0.5f };
    [SerializeField] private Cue coinCue = new() { volume = 0.5f };
    [SerializeField] private Cue buyCue = new() { volume = 0.55f };
    [SerializeField] private Cue equipCue = new() { volume = 0.4f };
    [Tooltip("Tiếng báo thua phát sau tiếng va chạm")]
    [SerializeField, Min(0f)] private float runFailedDelay = 0.3f;
    [SerializeField] private Button[] clickButtons = { };
    [SerializeField] private Button[] toggleButtons = { };
    [Tooltip("Object con chỉ chứa hình bia; nảy ở đây để không đổi góc hay hình học va chạm của bia")]
    [SerializeField] private Transform targetVisual;
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Mức phóng to thêm của bia khi dao cắm (0.06 = 6%)")]
    [SerializeField, Min(0f)] private float hitPulse = 0.06f;
    [SerializeField, Min(0f)] private float stageClearPulse = 0.12f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.14f;
    [Tooltip("Bia giật lên (world unit) theo hướng ném khi dao cắm; chỉ dịch object hình ảnh")]
    [SerializeField, Min(0f)] private float hitRecoil = 0.06f;
    [Tooltip("Rung điện thoại khi thua (Android)")]
    [SerializeField] private bool vibrateOnFail = true;
    [Tooltip("Biên độ rung camera (world unit) khi thua")]
    [SerializeField, Min(0f)] private float failShake = 0.2f;
    [SerializeField, Min(0.01f)] private float failShakeDuration = 0.35f;

    private GameSession.State _previousState;
    private int _previousKnives;
    private int _previousStage;
    private float _pulseAmount;
    private float _pulseElapsed = float.PositiveInfinity;
    private float _shakeElapsed = float.PositiveInfinity;
    private Vector3 _cameraBasePosition;
    private Vector3 _visualBasePosition;
    private string _previousWarning = "";
    private int _previousCoins;
    private KnifeSkin _previousSkin;

    private void Awake()
    {
        if (!session || !source)
        {
            Debug.LogError("Assign the GameSession and an AudioSource.", this);
            enabled = false;
        }
    }

    // Đăng ký nút ở Start để GameHUD đã áp dụng cài đặt âm thanh trước khi tiếng bật/tắt phát.
    private void Start()
    {
        if (cameraTransform)
        {
            _cameraBasePosition = cameraTransform.localPosition;
        }

        if (targetVisual)
        {
            _visualBasePosition = targetVisual.localPosition;
        }

        foreach (var button in clickButtons)
        {
            button.onClick.AddListener(() => Play(clickCue));
        }

        foreach (var button in toggleButtons)
        {
            button.onClick.AddListener(() => Play(toggleCue));
        }
    }

    private void OnEnable()
    {
        session.Changed += OnSessionChanged;
        if (progress)
        {
            progress.Changed += OnProgressChanged;
            _previousCoins = progress.Coins;
            _previousSkin = progress.EquippedSkin;
        }

        Remember();
        UpdateMusic();
    }

    private void OnDisable()
    {
        session.Changed -= OnSessionChanged;
        if (progress)
        {
            progress.Changed -= OnProgressChanged;
        }

        _pulseElapsed = float.PositiveInfinity;
        _shakeElapsed = float.PositiveInfinity;
        if (targetVisual)
        {
            targetVisual.localScale = Vector3.one;
            targetVisual.localPosition = _visualBasePosition;
        }

        if (cameraTransform)
        {
            cameraTransform.localPosition = _cameraBasePosition;
        }
    }

    private void LateUpdate()
    {
        UpdateWarningCue();
        if (targetVisual && _pulseElapsed < pulseDuration)
        {
            _pulseElapsed += Time.deltaTime;
            var wave = _pulseElapsed < pulseDuration ? Mathf.Sin(_pulseElapsed / pulseDuration * Mathf.PI) : 0f;
            targetVisual.localScale = Vector3.one * (1f + _pulseAmount * wave);
            // Bia xoay quanh tâm, nên dịch ở hệ thế giới: đổi hướng "lên" sang tọa độ cục bộ của object cha.
            var parent = targetVisual.parent;
            var up = parent ? parent.InverseTransformDirection(Vector3.up) : Vector3.up;
            targetVisual.localPosition = _visualBasePosition + up * (hitRecoil * wave * (_pulseAmount / Mathf.Max(hitPulse, 0.0001f)));
        }

        if (cameraTransform && _shakeElapsed < failShakeDuration)
        {
            _shakeElapsed += Time.deltaTime;
            var offset = Vector3.zero;
            if (_shakeElapsed < failShakeDuration)
            {
                offset = UnityEngine.Random.insideUnitCircle * (failShake * (1f - _shakeElapsed / failShakeDuration));
            }

            cameraTransform.localPosition = _cameraBasePosition + offset;
        }
    }

    // Suy ra sự kiện từ thay đổi của session; chỉ phát âm thanh, không quyết định gameplay.
    private void OnSessionChanged()
    {
        if (_previousState == GameSession.State.Playing)
        {
            var state = session.CurrentState;
            var sameStage = session.StageNumber == _previousStage;
            switch (state)
            {
                case GameSession.State.Playing when sameStage:
                {
                    if (session.KnivesRemaining < _previousKnives)
                    {
                        Play(throwCue);
                    }
                    else if (session.KnivesRemaining == _previousKnives)
                    {
                        PlayWoodHit();
                    }

                    break;
                }
                case GameSession.State.Transition:
                    PlayWoodHit();
                    Pulse(stageClearPulse);
                    Play(stageClearCue);
                    break;
                case GameSession.State.Victory:
                    PlayWoodHit();
                    Pulse(stageClearPulse);
                    Play(victoryCue);
                    StartCoroutine(PlayCoinLater());
                    break;
                case GameSession.State.GameOver:
                    _shakeElapsed = 0f;
                    Play(clashCue);
                    Vibrate();
                    StartCoroutine(PlayRunFailedLater());
                    break;
            }
        }

        Remember();
        UpdateMusic();
    }

    private void OnProgressChanged()
    {
        if (progress.Coins < _previousCoins)
        {
            Play(buyCue);
        }
        else if (progress.EquippedSkin != _previousSkin)
        {
            Play(equipCue);
        }

        _previousCoins = progress.Coins;
        _previousSkin = progress.EquippedSkin;
    }

    // Phát tiếng báo đúng một lần khi cảnh báo mới xuất hiện (rỗng -> INCOMING, hoặc đổi loại).
    private void UpdateWarningCue()
    {
        if (!encounter)
        {
            return;
        }

        var warning = session.CurrentState == GameSession.State.Playing ? encounter.Warning : "";
        if (warning != _previousWarning)
        {
            if (warning == "GUARD INCOMING")
            {
                Play(guardWarningCue);
            }
            else if (warning == "DANGER ZONE INCOMING")
            {
                Play(dangerWarningCue);
            }

            _previousWarning = warning;
        }
    }

    private void UpdateMusic()
    {
        if (!musicSource)
        {
            return;
        }

        var level = session.CurrentLevel;
        var clip = level && level.World && level.World.Music
            ? level.World.Music
            : menuMusic;
        if (musicSource.clip == clip)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        if (clip)
        {
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }

    private IEnumerator PlayCoinLater()
    {
        yield return new WaitForSeconds(0.6f);
        if (session.CurrentState == GameSession.State.Victory && session.LastResult.CoinsEarned > 0)
        {
            Play(coinCue);
        }
    }

    private void Remember()
    {
        _previousState = session.CurrentState;
        _previousKnives = session.KnivesRemaining;
        _previousStage = session.StageNumber;
    }

    private IEnumerator PlayRunFailedLater()
    {
        yield return new WaitForSeconds(runFailedDelay);
        if (session.CurrentState == GameSession.State.GameOver)
        {
            Play(runFailedCue);
        }
    }

    private void Vibrate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrateOnFail)
        {
            Handheld.Vibrate();
        }
#endif
    }

    private void Pulse(float amount)
    {
        _pulseAmount = amount;
        _pulseElapsed = 0f;
    }

    private void PlayWoodHit()
    {
        Pulse(hitPulse);
        if (woodHitClips.Length > 0)
        {
            source.PlayOneShot(woodHitClips[UnityEngine.Random.Range(0, woodHitClips.Length)], woodHitVolume);
        }
    }

    private void Play(Cue cue)
    {
        if (cue.clip)
        {
            source.PlayOneShot(cue.clip, cue.volume);
        }
    }
}
