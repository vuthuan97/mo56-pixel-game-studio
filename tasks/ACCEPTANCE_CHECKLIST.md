# Checklist nghiệm thu V1

Cách nghiệm: chạy `dotnet run --project src/PixelGameStudio.App`, làm theo từng mục; dấu kiểm ở cột **Cách xác nhận** mô tả bằng chứng tối thiểu (một số mục có test tự động — xem cột Test).

- [x] Tạo/mở/lưu project — New/Open/Save/Save As + autosave 30s + Recover. **Test**: ProjectStoreTests, AutosaveTests.
- [x] Chọn game/view/pixel/style profile — Inspector tab Project. **Test**: ProfileRulesTests.
- [x] Tạo character — Tạo nội dung mẫu / mở project; Character inspector (tên/notes/role). **Test**: RoleSystemTests.
- [x] Body không phụ thuộc clothes — body là part riêng; combo Body variant đổi silhouette khi không che. **Test**: AppearanceVariantTests.BodyVariant, composer golden.
- [x] Hair/face/equipment hoạt động — variants tóc 10 kiểu, mắt/miệng, 13 slot data-driven. **Test**: AppearanceVariantTests, AssetLibraryTests.
- [x] Tạo custom equipment slot — thêm `EquipmentSlotDef` vào rig (JSON) → slot xuất hiện trong Equipment inspector.
- [x] Chỉnh pose đầu/thân/tay/chân — Pose Editor (Animation inspector): part + dX/dY/Ẩn + states. **Test**: ComposerPoseParityTests (22 case).
- [x] Tạo animation bằng timeline — Add/Copy/Paste/Delete/Move + FPS/Loop + onion skin + play F5. **Test**: AnimationBehaviorTests.
- [x] Chọn hành vi preset — Behavior Library (66 preset / 5 nhóm), binding summary.
- [x] Tạo hành vi tùy chỉnh — Nhân bản behavior → sửa bindings; Delete bảo vệ preset. **Test**: AnimationBehaviorTests.
- [x] Đặt event marker — Marker editor (9 type) + markers theo frame. **Test**: AnimationRules validation.
- [x] Lưu/mở lại không mất dữ liệu — round-trip toàn bộ model (assets/rigs/characters/poses/animations/behaviors/roles). **Test**: ProjectRoundTrip* (nhiều file).
- [x] Native/silhouette/grayscale/debug preview — Mode combo + Scale 1x/2x/4x/10x nearest + Game preview 360×640.
- [x] Export PNG/spritesheet/layer/package/Godot — Export workspace. **Test**: ExportServiceTests, QaAndExportTests.
- [x] Role system — 5 role demo + custom; áp role thay loadout. **Test**: RoleSystemTests.
- [x] Undo/Redo + dirty + autosave — Ctrl+Z/Y, tiêu đề "●", timer 30s. **Test**: UndoRedoServiceTests.
- [x] Art QA — SpriteAnalyzer (palette/bounds/contrast) + noise + coverage view. **Test**: QaAndExportTests.CheckCharacter.

## Hạn chế đã biết (không chặn nghiệm thu)

- Body variant chỉ thấy khi không mặc áo ngoài che torso (đúng layer logic).
- Eye/mouth variants độc lập (chưa tổ hợp); face không có art view Up (đúng legacy).
- Weapon state assets (raise/slash) gắn theo item Kiếm của starter content.
- Godot .tres trỏ `../frames/` — người dùng giữ cấu trúc thư mục export khi copy vào engine.
