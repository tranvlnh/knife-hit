using System;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable once CheckNamespace
public class KnifeLauncher : MonoBehaviour
{
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float throwSpeed = 14f;
    [SerializeField, Min(0f)] private float minEmbedDepth = 0.15f;
    [SerializeField, Min(0f)] private float maxEmbedDepth = 0.6f;
    [Tooltip("Khoảng cách (world unit) từ pivot dao đến mũi dao, ở scale 1.")]
    [SerializeField] private float knifeTipOffset = 0.791f;
    [Tooltip("Chiều dài (world unit) phần dao đặc từ mũi đến đuôi cán, ở scale 1.")]
    [SerializeField, Min(0.2f)] private float knifeLength = 1.527f;

    private Transform _currentKnife;
    private RotatingTarget _rotatingTarget;
    private bool _isFlying;
    private bool _inputEnabled = true;
    private float _landingTipY;

    public event Action<Transform> KnifeLanded;
    public event Action<Transform> KnifeHitKnife;

    public bool HasFailed { get; private set; }

    private float TipY => _currentKnife.position.y + knifeTipOffset;

    private void Start()
    {
        _rotatingTarget = target ? target.GetComponent<RotatingTarget>() : null;
        if (!knifePrefab || !_rotatingTarget)
        {
            Debug.LogError("Assign the knife prefab and a Target with RotatingTarget.", this);
            enabled = false;
            return;
        }

        if (!Mathf.Approximately(transform.position.x, target.position.x) ||
            transform.position.y + knifeTipOffset >= _rotatingTarget.SurfaceY)
        {
            Debug.LogError("Place KnifeLauncher below the target and align their X positions.", this);
            enabled = false;
            return;
        }

        SpawnReadyKnife();
    }

    private void Update()
    {
        if (!_inputEnabled || HasFailed)
        {
            return;
        }

        if (!_isFlying && WasPressedThisFrame())
        {
            // Chỉ lấy ngẫu nhiên một lần cho mỗi lần ném được chấp nhận, không lấy lại khi dao đang bay.
            var depth = Mathf.Clamp(UnityEngine.Random.Range(minEmbedDepth, maxEmbedDepth), 0f, knifeLength - 0.1f);
            _landingTipY = _rotatingTarget.SurfaceY + depth;
            _isFlying = true;
        }

        if (!_isFlying)
        {
            _rotatingTarget.AdvanceRotation(Time.deltaTime);
            return;
        }

        // Chia bước ngắn để quãng đường dao đi và góc bia quay giữa hai lần kiểm tra đều nhỏ.
        var remainingTime = Time.deltaTime;
        while (remainingTime > 0f && _isFlying)
        {
            var stepTime = Mathf.Min(remainingTime, _rotatingTarget.MaximumStepTime, 0.08f / throwSpeed);
            _rotatingTarget.AdvanceRotation(stepTime);
            MoveFlyingKnife(throwSpeed * stepTime);
            remainingTime -= stepTime;
        }

        if (remainingTime > 0f && !HasFailed)
        {
            _rotatingTarget.AdvanceRotation(remainingTime);
        }
    }

    public void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
    }

    private void SpawnReadyKnife()
    {
        _currentKnife = Instantiate(knifePrefab, transform.position, Quaternion.identity).transform;
        _isFlying = false;
    }

    private static bool WasPressedThisFrame()
    {
        var touchPressed = Touchscreen.current != null &&
                           Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        var mousePressed = Mouse.current != null &&
                           Mouse.current.leftButton.wasPressedThisFrame;
        return touchPressed || mousePressed;
    }

    private void MoveFlyingKnife(float distance)
    {
        var previousTipY = TipY;
        var tipY = Mathf.Min(previousTipY + distance, _landingTipY);
        if (_rotatingTarget.TryGetBlockingKnife(tipY, out var buttY))
        {
            // Dừng tại đuôi dao chặn, nhưng không kéo lùi dao bị chạm từ bên hông.
            SetTipY(Mathf.Max(buttY, previousTipY));
            HasFailed = true;
            _inputEnabled = false;
            _isFlying = false;
            KnifeHitKnife?.Invoke(_currentKnife);
            return;
        }

        SetTipY(tipY);
        if (tipY < _landingTipY)
        {
            return;
        }

        _rotatingTarget.EmbedKnife(_currentKnife, tipY - knifeLength);
        var landedKnife = _currentKnife;
        SpawnReadyKnife();
        KnifeLanded?.Invoke(landedKnife);
    }

    private void SetTipY(float tipY)
    {
        var position = _currentKnife.position;
        position.y = tipY - knifeTipOffset;
        _currentKnife.position = position;
    }

    private void OnDestroy()
    {
        if (_currentKnife != null)
        {
            Destroy(_currentKnife.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        var x = transform.position.x;
        var tipY = (_currentKnife ? _currentKnife.position.y : transform.position.y) + knifeTipOffset;
        var rotatingTarget = target ? target.GetComponent<RotatingTarget>() : null;
        if (rotatingTarget)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(new Vector3(x, tipY), new Vector3(x, rotatingTarget.SurfaceY));

            // Khoảng mũi dao có thể dừng lại trong gỗ.
            Gizmos.color = new Color(1f, 0.5f, 0f);
            DrawTick(x, rotatingTarget.SurfaceY + minEmbedDepth, 0.3f);
            DrawTick(x, rotatingTarget.SurfaceY + maxEmbedDepth, 0.3f);
        }

        // Phần dao đặc từ mũi đến đuôi; đối chiếu với sprite khi chỉnh knifeTipOffset và knifeLength.
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(x, tipY), new Vector3(x, tipY - knifeLength));
        DrawTick(x, tipY, 0.15f);
        DrawTick(x, tipY - knifeLength, 0.15f);
    }

    private static void DrawTick(float x, float y, float halfWidth)
    {
        Gizmos.DrawLine(new Vector3(x - halfWidth, y), new Vector3(x + halfWidth, y));
    }

    private void OnValidate()
    {
        maxEmbedDepth = Mathf.Max(minEmbedDepth, maxEmbedDepth);
    }
}
