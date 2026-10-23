# Bộ asset Knife Hit

Bộ hình 2D tông gỗ vàng, thép bạc và xanh teal cho demo Android dọc. Hình được tạo bằng công cụ image_gen tích hợp; prompt gốc nằm trong `asset-generation-prompts.json`. Âm thanh được chọn từ Kenney, giữ nguyên dữ liệu OGG và đổi tên theo hành động. Đây là bộ asset để ráp gameplay; dự án hiện chưa có gameplay để xuất bản APK chơi được.

## Hình ảnh

| File trong `Assets/_Project/Art/` | Công dụng |
| --- | --- |
| `Sprites/target_wood.png` | Bia gỗ, nhìn chính diện, nền trong suốt, tâm sprite ở giữa |
| `Sprites/knife_standard.png` | Dao thép cán teal, mũi hướng lên +Y, nền trong suốt |
| `Backgrounds/background_forest.png` | Nền rừng tối dọc, vùng giữa trống cho gameplay |

Một bia dùng chung cho cả năm màn, độ khó do dữ liệu màn quyết định. Hình tạo bằng AI; không gán giấy phép CC0 của Kenney cho các hình này.

## Âm thanh và nguồn

| File trong `Assets/_Project/Audio/` | Khi phát | File gốc / gói |
| --- | --- | --- |
| `SFX/knife_throw.ogg` | Nhận lệnh ném hợp lệ | `knifeSlice2.ogg` (cắt 0.42s, đỉnh âm sớm để không trễ so với cú ném) / RPG Audio |
| `SFX/knife_hit_wood_01.ogg` | Dao cắm gỗ thành công | Trộn `impactWood_light_000` + `impactWood_medium_000` + `impactMetal_medium_002` (thân gỗ sắc, thép rung nhẹ ở cán) / Impact Sounds |
| `SFX/knife_hit_wood_02.ogg` | Biến thể tiếng cắm gỗ | Trộn `impactWood_light_002` + `impactWood_medium_001` + `impactMetal_medium_004`, cao hơn 6% / Impact Sounds |
| `SFX/knife_hit_wood_03.ogg` | Biến thể tiếng cắm gỗ | Trộn `impactWood_light_004` + `impactWood_medium_003` + `impactMetal_medium_001`, thấp hơn 5% / Impact Sounds |
| `SFX/knife_clash.ogg` | Dao va vào dao đã cắm | Trộn `impactMetal_light_003` (leng keng) + `impactMetal_heavy_001` (độ nặng) / Impact Sounds |
| `SFX/stage_clear.ogg` | Qua màn 1–4 | `confirmation_002.ogg` / Interface Sounds |
| `SFX/run_failed.ogg` | Báo thua sau tiếng va chạm, tùy chọn | `error_006.ogg` / Interface Sounds |
| `SFX/ui_click.ogg` | Start, Replay, Continue | `click_003.ogg` / Interface Sounds |
| `SFX/ui_toggle.ogg` | Bật âm thanh | `switch_002.ogg` / Interface Sounds |
| `Jingles/run_victory.ogg` | Hoàn thành màn 5, ứng viên cần nghe duyệt | `jingles_STEEL00.ogg` / Music Jingles |

Nguồn tải chính thức: [Impact Sounds](https://kenney.nl/assets/impact-sounds), [Interface Sounds](https://kenney.nl/assets/interface-sounds), [RPG Audio](https://kenney.nl/assets/rpg-audio), [Music Jingles](https://kenney.nl/assets/music-jingles).

Cả bốn gói âm thanh dùng Creative Commons Zero (CC0), cho phép dùng trong game thương mại; không bắt buộc ghi công. Bản giấy phép đi kèm được giữ tại `Assets/_Project/Audio/Licenses/`. Có thể ghi credit: “Audio by Kenney — kenney.nl (CC0)”.

## Cách dùng trong Unity

Hình được import thành Sprite (2D and UI), Single, Bilinear, không mipmap, không nén, pivot giữa. PPU: bia 400, dao 900, nền 167.2. Dùng material sprite unlit khi chưa có đèn 2D. Nền cao 10 world units, gần tỉ lệ 9:16; dùng camera orthographic size 5 và crop nhẹ theo màn hình. Bia rộng khoảng 3.1 world units. Dùng sorting order nền -100, bia 0, dao 10.

Dao giữ nguyên canvas trong suốt; tọa độ Transform là giữa canvas. Khi cắm dao, đặt mũi dao theo mép bia dựa trên hình thực tế, không dùng toàn bộ khung ảnh làm kích thước va chạm. Bia có mép vỏ hơi gồ ghề; gameplay vẫn dùng bán kính tròn logic và cần chỉnh để cảm giác chạm hợp lý.

Âm ngắn dùng Decompress On Load, preload, mono, PCM; jingle dùng Vorbis. Đặt AudioSource ở 2D (Spatial Blend 0), loop tắt. Mức volume bắt đầu để chỉnh bằng tai: ném 0.25, cắm gỗ 0.65, va dao 0.65, qua màn 0.45, báo thua 0.35, UI 0.3, thắng 0.45. Đây là đề xuất, chưa nghe duyệt trên loa điện thoại. Chọn ngẫu nhiên một trong ba tiếng gỗ cho mỗi lần thành công. Tắt âm phải dừng cả tiếng đang phát.

## Kết quả import

Đã import bằng Unity Editor 6000.6.5f1, tạo đủ file `.meta`: 3 sprite và 10 AudioClip mono. Kích thước hình: bia 1254×1254, dao 1024×1536, nền 941×1672. Clip cắm gỗ dài khoảng 0.33 giây, va dao 0.35 giây, ném 0.60 giây, nhạc thắng 0.93 giây. Nguồn PNG gốc được giữ nguyên độ phân giải. Dao vẫn có viền sáng mềm quanh hình, cần xem trực tiếp trên nền khi duyệt mỹ thuật.

## Duyệt thủ công

1. Chọn từng PNG trong Project, xem preview; kéo bia và dao vào scene tạm để xem ở kích thước chơi thật. Xoay bia đủ một vòng và xem mép alpha trên nền rừng.
2. Chọn từng OGG, bấm Play trong preview của Inspector để duyệt tiếng và nhạc thắng. Chưa thực hiện nghe duyệt bằng tai trong phiên tạo asset này.
3. Khi gameplay đã được ráp, nghe trên điện thoại khi ném nhanh, cắm dao cuối, va dao và chuyển màn; giảm volume nếu tiếng dồn gây chói. Kiểm tra mute và tiếp tục sau khi app trở lại foreground.

Không chạy automated tests. Scene, package và project settings không cần thay đổi cho bộ asset này.

## Asset Campaign (đã tạo và nối vào game)

Tạo bằng script Python (Pillow): tái tạo từ art có sẵn hoặc vẽ thủ tục, KHÔNG phải hình AI; nên thay bằng art tay nếu cần chất lượng cao hơn. Nối vào game bằng `Knife Hit > Apply Generated Assets` (`Editor/AssetWiring.cs`, chạy lại được; chỉ đổi trường hình/nhạc, không đụng thông số màn). Không chạy khi đang ở Play mode.

| Hạng mục | File | Nối vào |
| --- | --- | --- |
| 3 bia boss | `Art/Targets/target_boss_*.png` | `BossConfig.targetSprite` (`BossEncounter.SetTargetSprite`) |
| Khiên Guard, vành đỏ Danger Zone | `Art/Overlays/guard_shield.png`, `danger_ring.png` | `BossEncounter.guardOverlay/dangerOverlay` (con của `Target/Visual`); thay "nêm" bằng vành đỏ quanh bia |
| Nền World 2, 3 | `Art/Backgrounds/background_desert.png`, `background_frost.png` | `WorldConfig.background` |
| 8 skin dao (tô lại từ dao chuẩn, cùng canvas/hình học) | `Art/Knives/knife_*.png` | `KnifeSkin.sprite`, tint = trắng |
| Icon khóa, coin, sao đủ/thiếu, boss | `Art/Icons/` | `LevelButtonView` (khóa, boss, 3 sao), icon coin ở màn Knives |
| Khung nút / panel / thẻ skin (9-slice) | `Art/UI/ui_*.png` | Image của mọi Button, thẻ skin, khung gợi ý |
| SFX báo Guard / Danger Zone, nhận coin, mua dao, trang bị | `Audio/SFX/boss_guard_warning.wav`, `boss_danger_warning.wav`, `coin_collect.wav`, `knife_buy.wav`, `knife_equip.wav` | `GameFeedback` (cue mới). Nguồn Kenney (CC0), trộn lại: Guard = `impactPlate_heavy_000` + `impactMetal_heavy_000`; Danger = `impactBell_heavy_000` + `impactWood_heavy_000`; coin = `handleCoins`; mua = `handleCoins` + `confirmation_003`; trang bị = `drawKnife3` + `metalLatch` |
| Nhạc nền 3 World (lặp, tổng hợp) | `Audio/Music/music_*.wav` | `WorldConfig.music`; `GameFeedback.musicSource` (menu dùng nhạc forest) |

`CampaignBuilder` đã cập nhật để dùng các asset này nếu chạy lại. Nhạc là âm tổng hợp đơn giản; SFX đã thay bằng clip Kenney trộn lại (chưa nghe duyệt trên loa điện thoại); volume nhạc mặc định 0.3.

Font mặc định không có glyph tiếng Việt và glyph sao (★), nên mọi chuỗi UI là tiếng Anh; sao hiển thị bằng icon.
