# MASTER PROMPT — XÂY LẠI PIXEL GAME STUDIO

Bạn đang xây dựng **Pixel Game Studio**, ứng dụng desktop sản xuất tài nguyên game 2D pixel theo project.

## Bắt buộc đọc trước
- `.agents/AGENTS.md`
- toàn bộ `.agents/rules/`
- skill phù hợp trong `.agents/skills/`
- `docs/PRODUCT_OVERVIEW.md`
- `docs/ARCHITECTURE.md`
- `docs/CHARACTER_AND_BEHAVIOR_SPEC.md`
- `docs/STYLE_PROFILE_SPEC.md`
- `docs/VERSION_ROADMAP.md`
- `tasks/TASK_LIST.md`
- `docs/CURRENT_STATE.md`

Sau đó audit toàn bộ `legacy-reference/pixel_sprite_studio_v6/`.

## Vai trò của source cũ
Dùng để hiểu và tái sử dụng nhanh: style pixel, body, hair, equipment, layer, direction, pose, animation, anchor, Art QA, Godot export. Không được bê nguyên hạn chế: 32x46 hard-code, chỉ top-down, fixed class/slot, procedural if/else art.

## Kiến trúc mục tiêu
Project → Style Profile → Asset Library → Sprite Entity → Rig/Part Graph → Appearance/Equipment → Pose → Animation → Behavior → Validation → Export.

## V1 phải hoàn thiện Character Studio
1. Project System.
2. Character/Rig/Part Graph.
3. Face/Hair/Body.
4. Equipment tách body + Slot Editor.
5. Pose Editor.
6. Animation Timeline.
7. Behavior Library + Custom Behavior Editor.
8. Role data-driven.
9. Art QA + game-scale preview.
10. Export tổng quát + Godot.

## Hành vi
Người dùng có thể chọn preset hoặc tự tạo hành vi bằng chỉnh đầu/thân/tay/chân/tóc/vũ khí/vật cầm theo frame. Behavior có thể liên kết animation, held item, interaction anchor, effect marker, sound marker, gameplay marker. Không hard-code danh sách behavior trong UI.

## UI
Thiết kế kiểu editor chuyên nghiệp: Toolbar; Browser trái; Preview giữa; Inspector phải; Timeline dưới. Không dùng một form dài toàn ComboBox.

## Quy tắc thực thi
Bắt đầu từ Phase 0. Không chỉ lập kế hoạch.
1. Audit source legacy.
2. Tạo `docs/LEGACY_AUDIT.md`.
3. Chốt stack và architecture.
4. Tạo project mới.
5. Thiết lập build/test.
6. Bắt đầu Phase 1.
7. Sau mỗi checkpoint chạy build/test, sửa lỗi, cập nhật TASK_LIST và CURRENT_STATE.

Cuối mỗi lượt báo cáo: đã làm gì; file thay đổi; build/test; phần tôi có thể nghiệm thu; lỗi/hạn chế; task tiếp theo.
