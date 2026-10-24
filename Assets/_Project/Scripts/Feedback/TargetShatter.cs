using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
// Chỉ là phần trình bày: khi bia bị phá (qua stage hoặc thắng Level), hình bia vỡ thành các mảnh bay ra cùng những
// con dao đang cắm. Bia logic không đổi; chỉ object hình ảnh bị ẩn rồi hiện lại khi stage mới bắt đầu.
public class TargetShatter : MonoBehaviour
{
    private class Debris
    {
        public Transform Transform;
        public Mesh Mesh;
        public Vector3 Velocity;
        public float Spin;
        public Vector3 BaseScale;
    }

    // Hai vết nứt liền nhau không cách nhau quá góc này, để mỗi múi vẫn là đa giác lồi.
    private const float MaxCrackGap = 150f;
    private const float OpenSpeedFraction = 0.15f;
    // Mảnh chiếm từ tỉ lệ diện tích bia này trở lên được coi là mảng lớn (nặng).
    private const float BigPieceShare = 0.12f;
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    [SerializeField] private GameSession session;
    [SerializeField] private RotatingTarget target;
    [Tooltip("SpriteRenderer của hình bia (object con Visual); mảnh vỡ lấy hình, màu và lớp vẽ từ đây")]
    [SerializeField] private SpriteRenderer targetVisual;
    [Tooltip("Số mảng gỗ lớn của mỗi lần vỡ được chọn ngẫu nhiên trong khoảng này (chưa kể dăm và mạt gỗ)")]
    [SerializeField, Range(3, 30)] private int minPieces = 6;
    [SerializeField, Range(3, 30)] private int maxPieces = 11;
    [Tooltip("Số dăm gỗ dài, nhọn bật ra tối đa dọc mỗi vết nứt")]
    [SerializeField, Range(0, 4)] private int maxSplintersPerCrack = 2;
    [Tooltip("Số mạt gỗ nhỏ tối đa văng ra (số thật ngẫu nhiên từ một nửa đến tối đa)")]
    [SerializeField, Range(0, 30)] private int maxChips = 9;
    [Tooltip("Thời gian (giây) vết nứt hé ra chậm trước khi các mảnh bung hết cỡ")]
    [SerializeField, Min(0f)] private float openDuration = 0.12f;
    [Tooltip("Xác suất một con dao đang cắm tạo ra vết nứt từ chỗ nó cắm vào tâm (luôn có ít nhất một vết)")]
    [SerializeField, Range(0f, 1f)] private float knifeCrackChance = 0.6f;
    [Tooltip("Khi bẻ nhỏ thêm một múi: 1 = luôn nứt dọc theo hướng vết nứt, 0 = luôn gãy ngang múi")]
    [SerializeField, Range(0f, 1f)] private float radialBias = 0.4f;
    [Tooltip("Độ răng cưa của cạnh gãy, theo tỉ lệ chiều dài cạnh; 0 = cạnh thẳng như kính")]
    [SerializeField, Range(0f, 0.4f)] private float jaggedness = 0.16f;
    [Tooltip("Tốc độ văng ra (world unit / giây); mảng lớn bay chậm và nặng, dăm và mạt gỗ bay nhanh hơn")]
    [SerializeField, Min(0f)] private float minSpeed = 2.5f;
    [SerializeField, Min(0f)] private float maxSpeed = 5.5f;
    [Tooltip("Vận tốc cộng thêm hướng lên để các mảnh bật lên trước khi rơi")]
    [SerializeField, Min(0f)] private float upwardKick = 2.5f;
    [SerializeField, Min(0f)] private float gravity = 14f;
    [Tooltip("Tốc độ xoay tối đa (độ / giây) của mảnh nhỏ; mảng lớn xoay chậm hơn nhiều")]
    [SerializeField, Min(0f)] private float maxSpin = 540f;
    [SerializeField, Min(0.1f)] private float lifetime = 1.1f;
    [Tooltip("Phần cuối đời của mảnh vỡ dùng để thu nhỏ biến mất (0.35 = 35%)")]
    [SerializeField, Range(0.05f, 1f)] private float shrinkFraction = 0.35f;

    private readonly List<Debris> _debris = new();
    private readonly List<Transform> _detachedKnives = new();
    private MaterialPropertyBlock _block;
    private GameSession.State _previousState;
    private bool _shattered;
    private float _elapsed;

    private void Awake()
    {
        if (!session || !target || !targetVisual)
        {
            Debug.LogError("Assign the GameSession, the RotatingTarget and its Visual SpriteRenderer.", this);
            enabled = false;
            return;
        }

        _block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        session.Changed += OnSessionChanged;
        _previousState = session.CurrentState;
    }

    private void OnDisable()
    {
        session.Changed -= OnSessionChanged;
        ClearDebris();
        // Khi scene bị dỡ bỏ, object hình ảnh có thể đã bị hủy trước component này.
        if (_shattered && targetVisual)
        {
            targetVisual.gameObject.SetActive(true);
            _shattered = false;
        }
    }

    private void Update()
    {
        if (_debris.Count == 0)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed >= lifetime)
        {
            ClearDebris();
            return;
        }

        var shrinkStart = lifetime * (1f - shrinkFraction);
        var shrink = _elapsed > shrinkStart ? Mathf.InverseLerp(lifetime, shrinkStart, _elapsed) : 1f;
        // Lúc đầu vết nứt chỉ hé ra chậm, sau đó các mảnh mới bung ra hết cỡ.
        var burst = openDuration > 0f ? Mathf.SmoothStep(OpenSpeedFraction, 1f, _elapsed / openDuration) : 1f;
        foreach (var piece in _debris)
        {
            if (!piece.Transform)
            {
                continue;
            }

            piece.Velocity.y -= gravity * burst * Time.deltaTime;
            piece.Transform.position += piece.Velocity * (burst * Time.deltaTime);
            piece.Transform.Rotate(0f, 0f, piece.Spin * burst * Time.deltaTime);
            piece.Transform.localScale = piece.BaseScale * shrink;
        }
    }

    // Suy ra thời điểm bia bị phá từ thay đổi của session; không quyết định gameplay.
    private void OnSessionChanged()
    {
        var state = session.CurrentState;
        if (!_shattered)
        {
            if (_previousState == GameSession.State.Playing &&
                state is GameSession.State.Transition or GameSession.State.Victory)
            {
                Shatter();
            }
        }
        else if (state == GameSession.State.Playing)
        {
            // Sang stage mới thì mảnh cũ cứ rơi tiếp; chơi lại từ Victory thì dọn ngay.
            if (_previousState != GameSession.State.Transition)
            {
                ClearDebris();
            }

            targetVisual.gameObject.SetActive(true);
            _shattered = false;
        }

        _previousState = state;
    }

    private void Shatter()
    {
        var sprite = targetVisual.sprite;
        if (!sprite)
        {
            return;
        }

        ClearDebris();
        _block.Clear();
        _block.SetTexture(MainTexId, sprite.texture);
        // Tháo dao trước: vị trí dao quyết định các vết nứt, và dao bay đi cùng mảnh vỡ.
        _detachedKnives.Clear();
        target.DetachKnives(_detachedKnives);
        CreatePieces(sprite);
        CreateKnifeDebris();
        targetVisual.gameObject.SetActive(false);
        _shattered = true;
    }

    // Bia bị bổ dọc theo các vết nứt từ chỗ một số dao đã cắm vào tâm thành vài mảng gỗ lớn, bẻ thêm cho đến số mảng ngẫu
    // nhiên mong muốn; cạnh gãy răng cưa như gỗ toác ra. Dọc vết nứt còn bật ra dăm gỗ dài nhọn và các mạt gỗ nhỏ.
    private void CreatePieces(Sprite sprite)
    {
        var extents = sprite.bounds.extents;
        var radius = Mathf.Max(extents.x, extents.y);
        var wanted = Random.Range(minPieces, Mathf.Max(minPieces, maxPieces) + 1);

        var cracks = GetCrackAngles();
        var pieces = BuildSectors(radius, cracks);
        for (var attempt = 0; pieces.Count < wanted && attempt < wanted * 6; attempt++)
        {
            var index = PickByArea(pieces);
            if (TrySplit(pieces[index], radius, out var first, out var second))
            {
                pieces[index] = first;
                pieces.Add(second);
            }
        }

        foreach (var piece in pieces)
        {
            CreatePiece(sprite, Roughen(piece, radius), radius);
        }

        foreach (var crack in cracks)
        {
            var splinters = Random.Range(0, maxSplintersPerCrack + 1);
            for (var i = 0; i < splinters; i++)
            {
                CreatePiece(sprite, MakeSplinter(crack, radius), radius);
            }
        }

        var chips = Random.Range(maxChips / 2, maxChips + 1);
        for (var i = 0; i < chips; i++)
        {
            CreatePiece(sprite, MakeChip(radius), radius);
        }
    }

    // Dăm gỗ: mảnh dài, hẹp, nhọn đầu, nằm dọc vết nứt ở một khoảng ngẫu nhiên từ tâm ra.
    private static List<Vector2> MakeSplinter(float crackAngle, float radius)
    {
        var direction = Polar(1f, crackAngle + Random.Range(-4f, 4f));
        var side = new Vector2(-direction.y, direction.x);
        var start = direction * (radius * Random.Range(0.3f, 0.8f)) + side * (radius * Random.Range(-0.04f, 0.04f));
        var length = radius * Random.Range(0.25f, 0.6f);
        var width = radius * Random.Range(0.03f, 0.075f);
        return new List<Vector2>
        {
            start - side * (width * 0.5f),
            start + direction * (length * 0.5f) - side * (width * Random.Range(0.4f, 0.8f)),
            start + direction * length + side * (width * Random.Range(-0.4f, 0.4f)),
            start + direction * (length * 0.5f) + side * (width * Random.Range(0.4f, 0.8f)),
            start + side * (width * 0.5f)
        };
    }

    // Mạt gỗ: tam giác nhỏ ở vị trí ngẫu nhiên trên bia.
    private static List<Vector2> MakeChip(float radius)
    {
        var center = Random.insideUnitCircle * (radius * 0.9f);
        var size = radius * Random.Range(0.04f, 0.09f);
        var angle = Random.Range(0f, 360f);
        return new List<Vector2>
        {
            center + Polar(size, angle),
            center + Polar(size * Random.Range(0.6f, 1.2f), angle + Random.Range(100f, 140f)),
            center + Polar(size * Random.Range(0.6f, 1.2f), angle + Random.Range(230f, 260f))
        };
    }

    // Góc (độ, hệ toạ độ sprite) của các vết nứt, tăng dần: mỗi dao có xác suất knifeCrackChance tạo một vết nứt tại
    // vị trí nó cắm (luôn có ít nhất một nếu bia có dao). Khoảng trống quá rộng được thêm vết nứt ngẫu nhiên để mọi múi lồi.
    private List<float> GetCrackAngles()
    {
        var knifeAngles = new List<float>();
        var angles = new List<float>();
        foreach (var knife in _detachedKnives)
        {
            var local = (Vector2)targetVisual.transform.InverseTransformPoint(knife.position);
            if (local.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            var angle = Mathf.Repeat(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg + Random.Range(-2f, 2f), 360f);
            knifeAngles.Add(angle);
            if (Random.value < knifeCrackChance)
            {
                angles.Add(angle);
            }
        }

        if (angles.Count == 0)
        {
            angles.Add(knifeAngles.Count > 0 ? knifeAngles[Random.Range(0, knifeAngles.Count)] : Random.Range(0f, 360f));
        }

        angles.Sort();
        for (var i = 0; i < angles.Count;)
        {
            var next = i + 1 < angles.Count ? angles[i + 1] : angles[0] + 360f;
            var gap = next - angles[i];
            if (gap > MaxCrackGap)
            {
                angles.Insert(i + 1, angles[i] + gap * Random.Range(0.35f, 0.65f));
            }
            else
            {
                i++;
            }
        }

        return angles;
    }

    // Mỗi múi nằm giữa hai vết nứt liên tiếp; mọi vết nứt gặp nhau ở một điểm gần tâm bia.
    private static List<List<Vector2>> BuildSectors(float radius, List<float> angles)
    {
        var hub = Random.insideUnitCircle * (radius * 0.1f);
        var sectors = new List<List<Vector2>>();
        for (var i = 0; i < angles.Count; i++)
        {
            var angle = angles[i];
            var nextAngle = i + 1 < angles.Count ? angles[i + 1] : angles[0] + 360f;
            var sector = new List<Vector2> { hub, Polar(radius, angle) };
            var arcSteps = Mathf.FloorToInt((nextAngle - angle) / 25f);
            for (var k = 1; k <= arcSteps; k++)
            {
                sector.Add(Polar(radius, Mathf.Lerp(angle, nextAngle, k / (arcSteps + 1f))));
            }

            sector.Add(Polar(radius, nextAngle));
            sectors.Add(sector);
        }

        return sectors;
    }

    // Chọn ngẫu nhiên một mảnh với xác suất tỉ lệ diện tích.
    private static int PickByArea(List<List<Vector2>> pieces)
    {
        var total = 0f;
        foreach (var piece in pieces)
        {
            total += Area(piece);
        }

        var roll = Random.value * total;
        for (var i = 0; i < pieces.Count; i++)
        {
            roll -= Area(pieces[i]);
            if (roll <= 0f)
            {
                return i;
            }
        }

        return pieces.Count - 1;
    }

    // Cắt đa giác lồi bằng một đường thẳng, vị trí lệch ngẫu nhiên khỏi tâm mảnh. Mảnh xa tâm bia bị bẻ dọc (nứt tiếp theo
    // hướng vết nứt) hoặc ngang (gãy rời đầu múi), còn mảnh sát tâm bị cắt hướng bất kỳ.
    private bool TrySplit(List<Vector2> polygon, float radius, out List<Vector2> first, out List<Vector2> second)
    {
        first = new List<Vector2>();
        second = new List<Vector2>();
        var centroid = Centroid(polygon);
        var angle = Random.Range(0f, 180f);
        if (centroid.magnitude > radius * 0.15f)
        {
            var radial = Mathf.Atan2(centroid.y, centroid.x) * Mathf.Rad2Deg;
            angle = radial + (Random.value < radialBias ? 0f : 90f) + Random.Range(-25f, 25f);
        }

        var direction = Polar(1f, angle);
        var normal = new Vector2(-direction.y, direction.x);

        var low = float.MaxValue;
        var high = float.MinValue;
        foreach (var point in polygon)
        {
            var distance = Vector2.Dot(point - centroid, normal);
            low = Mathf.Min(low, distance);
            high = Mathf.Max(high, distance);
        }

        var origin = centroid + normal * Mathf.Lerp(low * 0.6f, high * 0.6f, Random.value);
        var count = polygon.Count;
        for (var i = 0; i < count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % count];
            var sideA = Vector2.Dot(a - origin, normal);
            var sideB = Vector2.Dot(b - origin, normal);
            (sideA >= 0f ? first : second).Add(a);
            if (sideA >= 0f != sideB >= 0f)
            {
                var cross = Vector2.Lerp(a, b, sideA / (sideA - sideB));
                first.Add(cross);
                second.Add(cross);
            }
        }

        var minArea = Area(polygon) * 0.04f;
        return first.Count >= 3 && second.Count >= 3 && Area(first) > minArea && Area(second) > minArea;
    }

    // Mỗi cạnh gãy (không phải mép tròn của bia) thành đường răng cưa: 3-5 điểm lệch ngang luân phiên hai phía, thỉnh
    // thoảng có một mũi nhọn dài, giống thớ gỗ toác ra thay vì đường cắt thẳng như kính.
    private List<Vector2> Roughen(List<Vector2> polygon, float radius)
    {
        var result = new List<Vector2>();
        var count = polygon.Count;
        for (var i = 0; i < count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % count];
            result.Add(a);
            var edge = b - a;
            var length = edge.magnitude;
            var isRim = ((a + b) * 0.5f).magnitude > radius * 0.985f;
            if (isRim || length < radius * 0.12f || jaggedness <= 0f)
            {
                continue;
            }

            var normal = new Vector2(-edge.y, edge.x) / length;
            var splits = Random.Range(3, 6);
            var sign = Random.value < 0.5f ? -1f : 1f;
            for (var k = 1; k <= splits; k++)
            {
                var t = (k + Random.Range(-0.3f, 0.3f)) / (splits + 1);
                var amplitude = Random.Range(0.4f, 1f) * jaggedness * length;
                if (Random.value < 0.2f)
                {
                    amplitude *= 2.2f;
                }

                result.Add(a + edge * t + normal * (sign * amplitude));
                sign = -sign;
            }
        }

        return result;
    }

    // points là đa giác theo chiều ngược kim đồng hồ, tính theo toạ độ sprite (gốc là pivot).
    private void CreatePiece(Sprite sprite, List<Vector2> points, float radius)
    {
        var centroid = Centroid(points);
        var count = points.Count;
        var vertices = new Vector3[count];
        var uvs = new Vector2[count];
        var colors = new Color[count];
        for (var i = 0; i < count; i++)
        {
            vertices[i] = points[i] - centroid;
            uvs[i] = ToUv(sprite, points[i]);
            colors[i] = targetVisual.color;
        }

        var triangles = Triangulate(points);

        var mesh = new Mesh { name = "ShatterPiece", vertices = vertices, uv = uvs, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();

        var visualTransform = targetVisual.transform;
        var pieceObject = new GameObject("ShatterPiece", typeof(MeshFilter), typeof(MeshRenderer));
        var pieceTransform = pieceObject.transform;
        pieceTransform.SetPositionAndRotation(visualTransform.TransformPoint(centroid), visualTransform.rotation);
        pieceTransform.localScale = visualTransform.lossyScale;
        pieceObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        var meshRenderer = pieceObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = targetVisual.sharedMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingLayerID = targetVisual.sortingLayerID;
        meshRenderer.sortingOrder = targetVisual.sortingOrder + 1;
        meshRenderer.SetPropertyBlock(_block);

        var outward = (Vector2)(visualTransform.rotation * centroid);
        var direction = outward.sqrMagnitude > 0.0001f ? outward.normalized : Random.insideUnitCircle.normalized;
        var outerness = Mathf.Clamp01(centroid.magnitude / Mathf.Max(radius, 0.0001f));
        // Mảng lớn nặng nên bay chậm và xoay chậm; dăm và mạt gỗ nhẹ nên bay nhanh, xoay nhanh.
        var share = Area(points) / (Mathf.PI * radius * radius);
        var lightness = 1f - Mathf.Clamp01(share / BigPieceShare);
        var speed = Mathf.Lerp(minSpeed, maxSpeed, outerness) * Mathf.Lerp(0.6f, 1.4f, lightness) * Random.Range(0.8f, 1.2f);
        _debris.Add(new Debris
        {
            Transform = pieceTransform,
            Mesh = mesh,
            Velocity = (Vector3)(direction * speed) + Vector3.up * upwardKick,
            Spin = Random.Range(-maxSpin, maxSpin) * Mathf.Lerp(0.2f, 1f, lightness),
            BaseScale = pieceTransform.localScale
        });
    }

    // Dao đang cắm bị hất khỏi bia theo hướng từ tâm ra.
    private void CreateKnifeDebris()
    {
        var center = target.transform.position;
        foreach (var knife in _detachedKnives)
        {
            var outward = (Vector2)(knife.position - center);
            var direction = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector2.up;
            _debris.Add(new Debris
            {
                Transform = knife,
                Velocity = (Vector3)(direction * Random.Range(minSpeed, maxSpeed)) + Vector3.up * upwardKick,
                Spin = Random.Range(-maxSpin, maxSpin),
                BaseScale = knife.localScale
            });
        }

        _detachedKnives.Clear();
        _elapsed = 0f;
    }

    private void ClearDebris()
    {
        foreach (var piece in _debris)
        {
            if (piece.Transform)
            {
                Destroy(piece.Transform.gameObject);
            }

            if (piece.Mesh)
            {
                Destroy(piece.Mesh);
            }
        }

        _debris.Clear();
    }

    private static Vector2 Centroid(List<Vector2> points)
    {
        var sum = Vector2.zero;
        foreach (var point in points)
        {
            sum += point;
        }

        return sum / points.Count;
    }

    private static float Area(List<Vector2> points)
    {
        var area = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            area += a.x * b.y - b.x * a.y;
        }

        return Mathf.Abs(area) * 0.5f;
    }

    // Cắt tai đa giác đơn (ngược kim đồng hồ), vì mảnh gồ ghề không còn lồi nên không chia quạt từ một điểm được.
    private static int[] Triangulate(List<Vector2> points)
    {
        var remaining = new List<int>();
        for (var i = 0; i < points.Count; i++)
        {
            remaining.Add(i);
        }

        var triangles = new List<int>();
        while (remaining.Count > 3)
        {
            var clipped = false;
            for (var i = 0; i < remaining.Count && !clipped; i++)
            {
                var previous = remaining[(i + remaining.Count - 1) % remaining.Count];
                var current = remaining[i];
                var next = remaining[(i + 1) % remaining.Count];
                if (!IsEar(points, remaining, previous, current, next))
                {
                    continue;
                }

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                remaining.RemoveAt(i);
                clipped = true;
            }

            if (!clipped)
            {
                // Đa giác suy biến: bỏ phần còn lại thay vì lặp vô hạn.
                return triangles.ToArray();
            }
        }

        triangles.AddRange(remaining);
        return triangles.ToArray();
    }

    private static bool IsEar(List<Vector2> points, List<int> remaining, int previous, int current, int next)
    {
        var a = points[previous];
        var b = points[current];
        var c = points[next];
        if (Cross(b - a, c - b) <= 1e-6f)
        {
            return false;
        }

        foreach (var other in remaining)
        {
            if (other == previous || other == current || other == next)
            {
                continue;
            }

            var p = points[other];
            if (Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f)
            {
                return false;
            }
        }

        return true;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static Vector2 Polar(float radius, float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
    }

    // Toạ độ sprite (đơn vị, gốc là pivot) -> UV trên texture, đúng với vùng của sprite.
    private static Vector2 ToUv(Sprite sprite, Vector2 position)
    {
        var pixel = position * sprite.pixelsPerUnit + sprite.pivot;
        var rect = sprite.rect;
        var texture = sprite.texture;
        return new Vector2((rect.x + pixel.x) / texture.width, (rect.y + pixel.y) / texture.height);
    }
}
