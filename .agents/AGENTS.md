# AGENTS — Pixel Game Studio

Mọi agent phải đọc file này trước khi code.

## Nguồn sự thật
1. docs/PRODUCT_OVERVIEW.md
2. docs/ARCHITECTURE.md
3. docs/CHARACTER_AND_BEHAVIOR_SPEC.md
4. docs/STYLE_PROFILE_SPEC.md
5. tasks/TASK_LIST.md
6. source hiện tại
7. prototype v6

## Prototype
Đọc `legacy-reference/pixel_sprite_studio_v6/` khi làm body, hair, equipment, pose, animation, anchor, Art QA hoặc Godot export. Tái sử dụng logic phù hợp nhưng migration về kiến trúc mới.

## Quy tắc DONE
Chỉ `[x]` khi đã code, build/typecheck, test liên quan và chức năng dùng được hoặc có test chứng minh. Scaffold/interface chưa DONE.

## Mỗi lượt
Đọc CURRENT_STATE + task → audit code → triển khai → test/build → fix → cập nhật TASK_LIST/CURRENT_STATE → báo cáo.

## Không được
- hard-code chỉ cho game tu tiên;
- hard-code top-down hoặc 32x46 toàn hệ thống;
- hard-code role/equipment slot;
- tạo kiến trúc song song;
- antialias/bilinear cho pixel preview;
- xóa feature cũ mà không migration tương đương.
