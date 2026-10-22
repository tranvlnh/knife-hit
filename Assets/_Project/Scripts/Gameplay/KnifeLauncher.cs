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
    [Tooltip("World distance from the knife pivot up to the blade tip, at scale 1.")]
    [SerializeField] private float knifeTipOffset = 0.791f;
    [Tooltip("World length of the solid knife from blade tip to handle butt, at scale 1.")]
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
        if (knifePrefab == null || _rotatingTarget == null)
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
            // Sample once per accepted throw; never reroll during flight.
            var depth = Mathf.Clamp(UnityEngine.Random.Range(minEmbedDepth, maxEmbedDepth), 0f, knifeLength - 0.1f);
            _landingTipY = _rotatingTarget.SurfaceY + depth;
            _isFlying = true;
        }

        if (!_isFlying)
        {
            _rotatingTarget.AdvanceRotation(Time.deltaTime);
            return;
        }

        // Short steps keep both the blade travel and the target rotation per check small.
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
            // Stop at the blocking butt, but never pull back a knife that was hit from the side.
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

            // Range where the blade tip comes to rest inside the wood.
            Gizmos.color = new Color(1f, 0.5f, 0f);
            DrawTick(x, rotatingTarget.SurfaceY + minEmbedDepth, 0.3f);
            DrawTick(x, rotatingTarget.SurfaceY + maxEmbedDepth, 0.3f);
        }

        // Solid knife from tip to butt; compare with the sprite when tuning knifeTipOffset and knifeLength.
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
