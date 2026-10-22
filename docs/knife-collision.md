# Va dao và độ sâu cắm

Chỉ phần dao nằm ngoài gỗ mới chặn dao đang bay. Độ sâu cắm là ngẫu nhiên, không phụ thuộc tốc độ ném. Mô hình này thay cho "Landing Model" và `ThrowRules` trong `knife-hit-plan.md`.

Va chạm được tính bằng hình học, không dùng Physics2D. Dao luôn bay thẳng đứng dọc trục tâm bia, nên một dao đã cắm chỉ chạm được dao đang bay khi nó chĩa gần đúng xuống trục bay. Điểm gần trục nhất của phần lộ ra nằm ở mặt gỗ, vì vậy thắng/thua chỉ phụ thuộc vào góc; độ sâu chỉ quyết định vị trí dao dừng lại khi thua.

## Cấu hình

- `RotatingTarget`:
  - `Radius = 1.5`: bán kính gỗ logic, xem bằng Gizmo khi chọn Target.
  - `Block Angle = 2.75°`: dao đã cắm lệch khỏi trục bay ít hơn góc này sẽ chặn dao đang bay. Giá trị mặc định xấp xỉ `asin(bề rộng dao / Radius)`, tức cảm giác của mô hình collider cũ. Tăng lên để luật dễ thua hơn và dễ thấy hơn.
- `KnifeLauncher`:
  - `Min/Max Embed Depth`, tính bằng world unit. Mỗi lần ném được chấp nhận chỉ lấy một giá trị ngẫu nhiên. Đặt hai giá trị bằng nhau để cố định độ sâu khi duyệt thủ công.
  - `Knife Tip Offset = 0.791` (từ pivot đến mũi) và `Knife Length = 1.527` (từ mũi đến đuôi cán), đo trên sprite dao ở scale 1.
- Prefab dao chỉ cần SpriteRenderer với sorting order `-1`, thấp hơn bia (`0`), để gỗ che phần dao cắm vào trong.
- Giữ target/dao scale 1 và launcher thẳng phía dưới tâm bia. Bia chỉ quay khi launcher gọi `AdvanceRotation`; không thêm Update khác xoay bia.

## Luồng xử lý

1. Dao bay lên theo các bước ngắn; mỗi bước bia quay tối đa `Block Angle`, nên dao đã cắm không thể "nhảy" qua trục bay giữa hai lần kiểm tra ở bất kỳ FPS nào.
2. Sau mỗi bước, nếu mũi dao đang bay đã vượt qua đuôi của một dao đã cắm đang nằm trong `Block Angle` thì thua: dao dừng tại đuôi dao bị chạm (hoặc giữ nguyên nếu bị chạm từ bên hông) và khóa lượt chơi.
3. Nếu không, khi mũi dao đạt mặt gỗ cộng độ sâu đã chọn, dao trở thành con của bia. Bia lưu góc cục bộ và khoảng cách từ tâm đến đuôi dao đó.

Xấp xỉ: đuôi dao đã cắm được xem như nằm ngay dưới tâm (bỏ qua `cos` của góc nhỏ hơn vài độ).

## Duyệt thủ công

1. Vào Play, ném nhiều dao cách xa nhau: dao cắm với độ sâu khác nhau, gỗ che đúng phần bị cắm.
2. Đợi một dao đã cắm quay về đúng phía dưới rồi ném: dao đang bay dừng ở đuôi dao đó, bia dừng, không có dao tiếp theo.
3. Ném sát cạnh một dao đã cắm (lệch hơn `Block Angle`): cắm thành công.
4. Đặt Min/Max cùng bằng 0.15, rồi cùng bằng 0.6: kết quả thắng/thua theo góc không đổi, chỉ vị trí dừng khi thua khác.
5. Chạm nhanh khi dao còn bay: không có dao thứ hai bay cùng lúc.
6. Giới hạn FPS ở 30 và 60 (`Application.targetFrameRate`): cùng một kiểu ném phải cho cùng kết quả.
7. Stop rồi Play để chơi lại. Chưa có giao diện Replay ở bước này.

Không chạy automated tests. Chủ dự án duyệt gameplay thủ công.

## Tài liệu API đã đối chiếu

- [Mathf.DeltaAngle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mathf.DeltaAngle.html): khoảng cách góc ngắn nhất, xử lý vòng 0/360°.
