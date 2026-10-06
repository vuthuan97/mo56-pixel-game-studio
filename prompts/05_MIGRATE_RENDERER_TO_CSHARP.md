# PROMPT — CHUYỂN RENDERER PIXEL TỪ PYTHON SANG C#

Đọc:
- `docs/TECH_STACK.md`
- `docs/ARCHITECTURE.md`
- `.agents/rules/02_pixel_art.md`
- source trong `legacy-reference/pixel_sprite_studio_v6/`

Mục tiêu:
port các primitive/render behavior hữu ích của prototype sang module:

`PixelGameStudio.Rendering`

Yêu cầu:

1. Không port nguyên kiến trúc if/else của prototype.
2. Tách primitive pixel:
   - set pixel;
   - fill rectangle;
   - mask;
   - alpha composite;
   - outline;
   - palette swap;
   - grayscale;
   - silhouette;
   - nearest-neighbor scale;
   - spritesheet pack.
3. Tất cả tọa độ production là integer.
4. Dùng RGBA 8-bit.
5. Không anti-alias.
6. Không interpolation ngoài nearest-neighbor.
7. Export PNG giữ alpha chính xác.
8. Renderer độc lập Avalonia.
9. Avalonia chỉ hiển thị bitmap output của renderer.
10. Tạo test cho từng primitive.

Nếu có thể:
- render cùng một fixture bằng Python prototype;
- render fixture tương đương bằng C#;
- so sánh pixel-diff;
- chỉ chấp nhận sai khác khi được ghi rõ là thay đổi có chủ ý.

Sau khi hoàn tất:
- chạy dotnet test;
- chạy dotnet build;
- cập nhật CURRENT_STATE và TASK_LIST.
