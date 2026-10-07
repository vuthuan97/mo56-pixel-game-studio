# USER GUIDE — Pixel Game Studio

Hướng dẫn sử dụng V1 (Xưởng tạo nhân vật). Ứng dụng: `src/PixelGameStudio.App` — chạy `dotnet run --project src/PixelGameStudio.App`.

## 1. Quy trình nhanh (5 phút có nhân vật hoàn chỉnh)

1. **File → Save Project As…** — chọn thư mục trống (project cần vị trí lưu trước khi import/tạo nội dung).
2. Tab **Equipment** → **Tạo nội dung mẫu** — sinh ~310 asset humanoid (part/equipment/variants × 4 hướng) + rig + nhân vật "Nhân vật mẫu".
3. Menu **Animation → Cài preset** — 16 pose + 6 animation + 66 behavior.
4. Tab **Animation** → chọn `walk` → **Play/Stop** (F5), bật **Onion skin**; nút **Thêm/Copy/Paste/Xóa/đổi chỗ** để chỉnh frame.
5. Tab **Character** → đổi Tóc/Da/Body/Eye/Mouth (combos Appearance). Bộ tóc gồm tóc dài, tóc ngắn, tóc nam và **Không tóc**.
6. Tab **Validation** → **Chạy validation**; tab **Export** → chọn animation → **Export…**.

## 2. Bố cục cửa sổ

| Vùng | Nội dung |
|---|---|
| Toolbar trên | Menu File/Edit/Animation; workspace bar 8 tab; New/Open/Save/Save As/Undo/Redo; project summary |
| Browser trái | Asset/Entity Browser (thumbnail, tìm kiếm, tag filter, import/xóa) + Behavior Library |
| Preview giữa | Direction/Pose/Mode (Normal/Grayscale/Silhouette/Part Debug/Anchor Debug)/Scale; **Game preview** (viewport 360×640, nền, lưới 32px, NPC silhouette) |
| Inspector phải | Nội dung đổi theo workspace tab |
| Timeline dưới | Frame strip + markers; frame ops; play/step; onion skin (workspace Animation) |
| Status bar | Trạng thái + chỉ báo "● có thay đổi chưa lưu" |

Panel.resize được bằng **GridSplitter**. Tab workspace: Project / Character / Rig / Equipment / Animation / Behavior / Validation / Export.

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
- **Preview nhảy cảnh báo compose**: mở tab Validation để xem chi tiết (asset thiếu, view thiếu…).
- **"No precompiled XAML"** khi tự build sau khi sửa XAML: chạy `dotnet build -t:Rebuild` một lần (lỗi AVLN bị incremental build nuốt).
## MO56 Character workspace

- `Frame` holds character identity, appearance variants and build parameters. Gender/body type use selectors; head, torso, arm, leg and foot values use one-pixel sliders.
- `Equipment` reads slots from the selected rig. Filter/select an asset in the contextual browser, then use `Use selected` on a slot; incompatible tags are rejected.
- `Animation` owns the selectable timeline, duration ticks and pose editor. Shared poses clone on first edit.
- `Actions` shows availability reasons and generates dedicated poses/animations. Generation can be cancelled atomically.
