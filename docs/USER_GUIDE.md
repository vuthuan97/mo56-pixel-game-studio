# USER GUIDE — Pixel Game Studio

Hướng dẫn sử dụng V1 (Xưởng tạo nhân vật). Ứng dụng: `src/PixelGameStudio.App` — chạy `dotnet run --project src/PixelGameStudio.App`.

## 1. Quy trình nhanh (5 phút có nhân vật hoàn chỉnh)

1. Menu **Tệp → Lưu project vào thư mục khác…** — chọn thư mục cho project trước khi import PNG/tạo asset.
2. Tab **Nhân vật → Trang bị** → **Create starter content** để tạo rig/asset mẫu.
3. Menu **Hoạt ảnh → Cài preset** — 16 pose, 6 animation và 66 behavior tham khảo (không phải 66 chuyển động riêng).
4. Tab **Nhân vật → Hoạt ảnh** → chọn `walk`, phát/dừng (F5), bật onion skin hoặc sửa frame ngay trong panel phải. **Tạo hoạt ảnh** bắt đầu từ pose đang xem; **Nhân bản** tách riêng pose khỏi bản gốc; **Lưu mẫu behavior** đưa animation vào thư viện behavior và catalog Hành động để tạo bản riêng cho từng nhân vật.
5. Tab **Nhân vật → Bộ khung** → đổi Tóc/Da/Body/Eye/Mouth; thanh trên cùng cho phép chọn/tạo/nhân bản/đổi tên/xóa nhân vật.
6. Tab **Nhân vật → Hành động** → chọn checkbox, xem thử, bấm **Tạo**. Tab **Xuất** → chọn nhiều nhân vật/hoạt ảnh/hướng và định dạng, rồi chọn thư mục xuất.

## 2. Bố cục cửa sổ

| Vùng | Nội dung |
|---|---|
| Thanh trên | Menu Tệp/Chỉnh sửa/Hoạt ảnh; 5 tab Dự án, Nhân vật, Vật phẩm/Đạo cụ, Bối cảnh, Xuất; thanh nhân vật chỉ trong tab Nhân vật |
| Browser trái | Asset/Behavior Browser theo ngữ cảnh, chỉ mở trong Trang bị/Hành động |
| Preview giữa | Direction/Pose/Mode (Normal/Grayscale/Silhouette/Part Debug/Anchor Debug)/Scale; **Game preview** (viewport 360×640, nền, lưới 32px, NPC silhouette) |
| Inspector phải | Nội dung đổi theo workspace tab |
| Timeline | Trong panel phải của Nhân vật → Hoạt ảnh; frame ops, play/step, onion skin |
| Status bar | Trạng thái + chỉ báo "● có thay đổi chưa lưu" |

Panel.resize được bằng **GridSplitter**. Nhân vật có 4 tab con: Bộ khung / Trang bị / Hoạt ảnh / Hành động. Vật phẩm và Bối cảnh là trang chờ tính năng.

## 3. Phím tắt

| Phím | Hành động |
|---|---|
| Ctrl+N | Project mới |
| Ctrl+O | Mở project |
| Ctrl+S | Lưu project |
| Ctrl+Shift+S | Lưu project vào thư mục mới |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| F5 | Play/Stop animation |
| ◀ / ▶ | Frame trước/sau |

## 4. Lưu & khôi phục

- **Lưu**: File → Save (Ctrl+S) ghi `project.pgsproj` + dọn autosave; Save As… chọn thư mục mới.
- **Dirty state**: mọi chỉnh sửa đánh dấu "●" ở tiêu đề và status bar.
- **Autosave**: mỗi 30s khi có thay đổi, bản khôi phục ghi vào `autosave/`. Đóng app khi dirty vẫn có bản khôi phục.
- **Khôi phục**: File → Recover Autosave… → chọn thư mục project (dùng khi main file mất hoặc autosave mới hơn).

## 5. Xử lý sự cố

- **"Canvas của PNG khác canvas project"** khi import: PNG không đúng kích thước native của project — resize ngoài (nearest) hoặc tạo project mới với canvas phù hợp.
- **Preview nhảy cảnh báo compose**: mở tab Dự án để chạy kiểm tra chi tiết (asset thiếu, view thiếu…).
- **"No precompiled XAML"** khi tự build sau khi sửa XAML: chạy `dotnet build -t:Rebuild` một lần (lỗi AVLN bị incremental build nuốt).
## MO56 Character workspace

- `Frame` holds character identity, appearance variants and build parameters. Gender/body type use selectors; head, torso, arm, leg and foot values use one-pixel sliders.
- `Equipment` reads slots from the selected rig. Filter/select an asset in the contextual browser, then use `Use selected` on a slot; incompatible tags are rejected.
- `Animation` owns the selectable timeline, duration ticks and pose editor. Playback honors each frame's duration; shared poses clone on first edit.
- `Actions` có tìm kiếm/lọc, checkbox lựa chọn lưu theo nhân vật, thumbnail, xem thử và Tạo. Tick checkbox không tạo animation. Một số hành động yêu cầu slot/anchor; lý do không tương thích hiện trên thẻ. Có thể hủy giữa chừng mà không để action hiện tại dang dở. Khi nguồn đổi, thẻ báo cần tạo lại; nút **Tạo** giữ bản chỉnh tay, **Tạo bản mới** thêm version, **Thay thế bản đã tạo** ghi đè có chủ đích (bị chặn nếu pose dùng chung/legacy).
- `Export` chọn nhiều nhân vật/animation/hướng; mỗi package nằm trong `characters/{characterId}/animation-{animationId}/` hoặc `default-pose/`. Godot chỉ sinh cho package có animation.
