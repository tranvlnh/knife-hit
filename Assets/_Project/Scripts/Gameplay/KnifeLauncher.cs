using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ReSharper disable once CheckNamespace
public class KnifeLauncher : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private GameObject knifePrefab;
    [Tooltip("Skin đang trang bị quyết định hình dao; không ảnh hưởng hình học va chạm")]
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float throwSpeed = 14f;
    [SerializeField, Min(0f)] private float minEmbedDepth = 0.15f;
    [SerializeField, Min(0f)] private float maxEmbedDepth = 0.6f;
    [Tooltip("Khoảng cách (world unit) từ pivot dao đến mũi dao, ở scale 1.")]
    [SerializeField] private float knifeTipOffset = 0.791f;
    [Tooltip("Chiều dài (world unit) phần dao đặc từ mũi đến đuôi cán, ở scale 1.")]
    [SerializeField, Min(0.2f)] private float knifeLength = 1.527f;
    [Tooltip("Thời gian dao mới trượt lên bệ ném; chỉ là hiệu ứng, có thể ném ngay")]
    [SerializeField, Min(0f)] private float readySlideSeconds = 0.08f;
    [SerializeField, Min(0f)] private float readySlideDistance = 0.6f;

    private Transform _currentKnife;
    private RotatingTarget _rotatingTarget;
    private bool _isFlying;
    private bool _isStopped;
    private float _landingTipY;

    private float TipY => _currentKnife.position.y + knifeTipOffset;

    private void Awake()
    {
        _rotatingTarget = target ? target.GetComponent<RotatingTarget>() : null;
        if (!session || !knifePrefab || !progress || !_rotatingTarget)
        {
            Debug.LogError("Assign the GameSession, the knife prefab, PlayerProgress and a Target with RotatingTarget.", this);
            enabled = false;
            return;
        }

        if (!Mathf.Approximately(transform.position.x, target.position.x) ||
            transform.position.y + knifeTipOffset >= _rotatingTarget.SurfaceY)
        {
            Debug.LogError("Place KnifeLauncher below the target and align their X positions.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Khi tạm dừng, dao và bia đứng yên; lần chạm Continue cũng không được tính là một lần ném.
        if (_isStopped || session.CurrentState == GameSession.State.Paused)
        {
            return;
        }

        if (!_isFlying && _currentKnife && WasPressedThisFrame() && session.TryAcceptThrow())
        {
            // Chỉ lấy ngẫu nhiên một lần cho mỗi lần ném được chấp nhận, không lấy lại khi dao đang bay.
            var depth = Mathf.Clamp(Random.Range(minEmbedDepth, maxEmbedDepth), 0f, knifeLength - 0.1f);
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

        if (remainingTime > 0f && !_isStopped)
        {
            _rotatingTarget.AdvanceRotation(remainingTime);
        }
    }

    // Xóa dao đang bay hoặc dao vừa thua còn sót lại, rồi chuẩn bị dao đầu tiên của stage.
    public void ResetForStage()
    {
        if (_currentKnife)
        {
            Destroy(_currentKnife.gameObject);
        }

        _isStopped = false;
        SpawnReadyKnife();
    }

    public void SpawnReadyKnife()
    {
        var knife = Instantiate(knifePrefab, transform.position, Quaternion.identity);
        progress.EquippedSkin.ApplyTo(knife);
        _currentKnife = knife.transform;
        _isFlying = false;
        if (readySlideSeconds > 0f)
        {
            StartCoroutine(SlideIn(knife.transform));
        }
    }

    // Hiệu ứng thuần hình ảnh: ngừng ngay khi dao được ném, vì tọa độ va chạm chỉ dựa vào mũi dao.
    private IEnumerator SlideIn(Transform knife)
    {
        var home = transform.position;
        for (var elapsed = 0f; elapsed < readySlideSeconds; elapsed += Time.deltaTime)
        {
            if (!knife || knife != _currentKnife || _isFlying)
            {
                break;
            }

            var t = elapsed / readySlideSeconds;
            knife.position = home + Vector3.down * (readySlideDistance * (1f - t));
            knife.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t);
            yield return null;
        }

        if (knife)
        {
            knife.localScale = Vector3.one;
            if (knife == _currentKnife && !_isFlying)
            {
                knife.position = home;
            }
        }
    }

    // Dao thua bật ngược xuống và rơi khỏi màn hình; chỉ trình bày, trạng thái thua đã được báo.
    private static IEnumerator FallAway(Transform knife)
    {
        var velocity = new Vector3(Random.Range(-1.5f, 1.5f), -3f, 0f);
        var spin = Random.Range(360f, 720f) * (Random.value < 0.5f ? -1f : 1f);
        for (var elapsed = 0f; knife && elapsed < 1.2f; elapsed += Time.deltaTime)
        {
            velocity.y -= 18f * Time.deltaTime;
            knife.position += velocity * Time.deltaTime;
            knife.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }
    }

    // Dao cắm sẵn dùng độ sâu giữa Min/Max để mọi stage trông giống nhau mỗi lần chơi.
    public void SpawnEmbeddedKnife(float localAngle)
    {
        var tipDistance = _rotatingTarget.Radius - (minEmbedDepth + maxEmbedDepth) * 0.5f;
        var knifeObject = Instantiate(knifePrefab);
        progress.EquippedSkin.ApplyTo(knifeObject);
        var knife = knifeObject.transform;
        _rotatingTarget.AddEmbeddedKnife(knife, localAngle, tipDistance + knifeTipOffset, tipDistance + knifeLength);
    }

    // Lần chạm đặt lên nút UI (ví dụ Pause) không bao giờ là lần ném.
    private static bool WasPressedThisFrame()
    {
        var touchPressed = Touchscreen.current != null &&
                           Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        if (touchPressed)
        {
            return !IsOverUi(Touchscreen.current.primaryTouch.touchId.ReadValue());
        }

        var mousePressed = Mouse.current != null &&
                           Mouse.current.leftButton.wasPressedThisFrame;
        return mousePressed && !IsOverUi(-1);
    }

    private static bool IsOverUi(int pointerId)
    {
        var eventSystem = EventSystem.current;
        return eventSystem && (pointerId < 0
            ? eventSystem.IsPointerOverGameObject()
            : eventSystem.IsPointerOverGameObject(pointerId));
    }

    private void MoveFlyingKnife(float distance)
    {
        var previousTipY = TipY;
        var tipY = Mathf.Min(previousTipY + distance, _landingTipY);
        if (_rotatingTarget.TryGetBlockingKnife(tipY, out var buttY))
        {
            // Dừng tại đuôi dao chặn, nhưng không kéo lùi dao bị chạm từ bên hông.
            SetTipY(Mathf.Max(buttY, previousTipY));
            _isFlying = false;
            _isStopped = true;
            StartCoroutine(FallAway(_currentKnife));
            session.HandleKnifeHitKnife();
            return;
        }

        SetTipY(tipY);
        if (tipY < _landingTipY)
        {
            return;
        }

        _rotatingTarget.EmbedKnife(_currentKnife, tipY - knifeLength);
        _currentKnife = null;
        _isFlying = false;
        session.HandleSuccessfulThrow();
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
