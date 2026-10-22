# Danh sách kiểm tra thủ công

Chủ dự án tự kiểm tra; không có test tự động. Đánh dấu từng mục trong Editor (Play từ `GamePlay.unity`) và trên điện thoại Android (APK portrait). Để xóa dữ liệu lưu: Edit > Clear All PlayerPrefs, hoặc trên máy xóa dữ liệu ứng dụng.

Trạng thái xác minh của phiên làm việc này nằm ở báo cáo cuối của phiên; mọi mục dưới đây là việc chủ dự án cần tự chạy.

## Luồng chính (Editor)

1. Mở game lần đầu: Home hiện "PLAY", `LEVELS 0/30 STARS 0 COINS 0`, bia quay phía sau.
2. LEVELS: World 1 có Level 1 là "NEW", các Level khác "LOCKED"; tab World 2/3 xem được nhưng Level khóa không bấm được.
3. PLAY: vào Level 1, gợi ý "TAP to throw..." hiện khoảng 4 giây, chỉ lần đầu.
4. Ném đủ dao: màn kết quả hiện "LEVEL CLEARED", sao, thời gian, coin; nút NEXT LEVEL / RETRY / LEVELS.
5. Thoát Play, vào lại: Home hiện "CONTINUE", Level 2 là Level hiện tại, coin và sao còn nguyên.
6. Chơi lại Level 1 (RETRY): thắng lần hai không cộng coin gốc; chỉ cộng bonus nếu số sao cao hơn lần tốt nhất.
7. Cố ý chạm dao đã cắm: "FAILED", RETRY chơi lại, LEVELS về bản đồ; không có coin hay sao mới.

## Boss

1. Thắng Level 1–9 của World 1 để mở "BOSS: STUMP". Hiện gợi ý lần đầu; HUD ghi "PHASE 1/3".
2. Xong một pha: Transition ngắn rồi pha 2 (dao cắm sẵn đổi, bia đổi tốc độ).
3. Thắng pha 3: "BOSS DEFEATED", nhận coin, và skin STUMP trong màn KNIVES chuyển thành "EQUIP".
4. Boss Lv20: bia nhấp nháy xanh và HUD ghi "GUARD INCOMING" trước khi "GUARD UP"; chạm lúc GUARD UP không ném, không mất dao.
5. Boss Lv30: thêm "DANGER ZONE INCOMING" / "DANGER ZONE" (đỏ); khi đang đỏ, ném sát dao đã cắm dễ thua hơn.
6. Pause khi Guard đang sáng rồi Continue: Guard còn đúng thời gian đã tính (không trôi khi tạm dừng).
7. Level 30 thắng: "CAMPAIGN COMPLETE", không có nút NEXT.

## Kho dao và coin

1. KNIVES: chỉ STANDARD "EQUIPPED"; skin giá coin hiển thị giá; skin boss ghi "CLEAR BOSS: ...".
2. Đủ coin: bấm giá để mua và tự trang bị; trừ đúng coin; chưa đủ coin thì nút không bấm được.
3. Dao đang trang bị đổi hình/màu: dao ở bệ ném, dao đã cắm và biểu tượng dao trong HUD đều dùng skin đó; độ dài dao và va chạm không đổi.
4. Thoát và vào lại: skin sở hữu và đang trang bị được giữ.

## Chạm và vòng đời

1. Chạm nhanh nhiều lần khi dao đang bay: chỉ một dao bay; chạm dư bị bỏ qua.
2. Bấm PLAY / RETRY / NEXT / LEVEL: lần chạm đó không ném dao (có khóa input ngắn sau khi bắt đầu stage).
3. Ném dao cuối: không thể chạm tiếp; Transition hoặc kết quả hiện đúng một lần.
4. Home/Map/Knives mở: chạm vào bia hay vùng trống không ném dao.
5. Background/resume trên điện thoại (Home, khóa màn hình, mở lại): hiện panel Continue; lần chạm Continue không ném dao; bia, dao đang bay và thời gian sao đóng băng đến khi Continue.
6. Tắt app giữa Run: mở lại về Home, Run không được khôi phục; tiến trình (coin, sao, skin) vẫn còn.
7. Âm thanh: nút SOUND đổi ON/OFF ở Home và Pause; thoát vào lại vẫn giữ; tắt âm thì mọi tiếng im.

## Dữ liệu lưu hỏng (Editor)

1. Trong Play, mở Console và dùng `PlayerPrefs.SetString("KnifeHit.Save", "{xxx")` (hoặc chuỗi rỗng, hoặc `{"version":99}`): mở lại game, chỉ có cảnh báo trong Console, game về mặc định, không crash; bản hỏng được chép vào khóa `KnifeHit.Save.Corrupt`.
2. Chèn một id level/skin không tồn tại vào JSON hợp lệ: chỉ mục đó bị bỏ qua (có cảnh báo), phần còn lại giữ nguyên.
3. Nâng cấp từ bản demo: nếu có khóa `SoundEnabled` cũ và chưa có save mới thì cài đặt âm thanh được giữ.

## Giao diện và Android

1. Các màn Home, Level Map, Knives, Result vừa trong Game view với tỉ lệ 9:16, 9:19.5, 9:21 và 3:4; không bị tai thỏ, camera đục lỗ hay thanh điều hướng che.
2. Level Map: 10 nút Level mỗi World, chữ không tràn nút.
3. Knives: 9 thẻ, tên và nút không đè nhau.
4. Build Profiles: Android, portrait, chỉ `GamePlay.unity` trong danh sách scene; cài APK, chơi offline, giữ ổn định ~60 FPS trên máy thật và ghi lại tên máy.
5. Cân bằng: ghi lại Level nào quá khó/dễ và mốc 2★/3★ nào không hợp lý (`docs/level-table.md`; các mốc hiện là ước lượng).

## Game feel và nền tảng (bổ sung)

1. Thua: dao bật ngược rồi rơi, máy rung (Android), panel FAILED hiện sau ~0.6s; chạm trong lúc chờ không ném dao.
2. Thắng: panel kết quả hiện sau ~0.5s, chỉ một lần.
3. Mỗi lần ném, dao mới trượt nhẹ lên bệ; chạm ném ngay lúc đang trượt vẫn đúng vị trí mũi dao.
4. Dao cắm: bia giật nhẹ lên và nảy; không đổi kết quả va chạm.
5. Phím Back/Escape: đang chơi thì Pause, đang Pause thì Continue, ở màn kết quả thì về bản đồ.
6. Máy thật chạy 60 FPS và màn hình không tự tắt khi đứng chờ.
7. Nếu đã gán `timerText` trong GameHUD: đồng hồ dừng khi Pause, mốc sao đổi đúng 3 → 2 → 1.
