# Pixel Game Studio — Gói khởi tạo cho ZCode

Pixel Game Studio là ứng dụng sản xuất tài nguyên game 2D pixel theo **dự án**.

## Định hướng
Mỗi project đại diện cho một game và định nghĩa chung: thể loại game, góc nhìn, kích thước pixel/canvas, bảng màu, viền, ánh sáng, đổ bóng, mật độ chi tiết, tỷ lệ nhân vật và quy tắc hoạt ảnh. Mọi asset trong project phải tuân cùng bộ quy chuẩn để đồng bộ hình ảnh.

## V1
Hoàn thiện **Xưởng tạo nhân vật**: Project → Phong cách → Nhân vật → Rig/Bộ phận → Trang bị → Tư thế → Hoạt ảnh → Hành vi → Kiểm tra → Xuất.

## Source cũ
`legacy-reference/pixel_sprite_studio_v6/` và `legacy-reference/pixel_sprite_studio_v6.zip` là prototype tham khảo. ZCode phải đọc để hiểu cơ chế và phong cách đã thử nghiệm, sau đó migration sang kiến trúc mới.

## Bắt đầu
1. Đọc `prompts/00_MASTER_REBUILD.md`.
2. Phase 0 đã hoàn thành — xem `docs/LEGACY_AUDIT.md` và `docs/CURRENT_STATE.md`.
3. Các lượt sau dùng `prompts/01_CONTINUE_DEVELOPMENT.md`.
4. Theo dõi `tasks/TASK_LIST.md` và `docs/CURRENT_STATE.md`.

## Build & test
- PowerShell: `./scripts/build.ps1 -Test` (thêm `-Format` để chạy `dotnet format`).
- Thủ công: `dotnet build PixelGameStudio.sln` / `dotnet test PixelGameStudio.sln`.
- App: `dotnet run --project src/PixelGameStudio.App`.
- Regenerate golden fixtures (cần Python 3.13 + Pillow): `python tools/fixturegen/generate_fixtures.py`.
