# Công nghệ bắt buộc — Pixel Game Studio

## Ngôn ngữ và nền tảng

Pixel Game Studio bản production phải sử dụng:

- **C#**
- **.NET 8**
- **Avalonia UI**
- **MVVM**

Không tự đổi sang Python/Tkinter, Electron, WinForms, WPF hoặc framework khác nếu chưa có quyết định mới từ chủ dự án.

## Thư viện và nền tảng khuyến nghị

### Giao diện
- Avalonia UI
- CommunityToolkit.Mvvm hoặc MVVM thuần nếu kiến trúc dự án đã chốt tương đương

### Render pixel
Ưu tiên lớp render bitmap độc lập với UI.

Có thể dùng:
- `WriteableBitmap` / `Bitmap` của Avalonia cho hiển thị
- SkiaSharp cho thao tác bitmap nếu thực sự cần
- hoặc buffer RGBA tự quản lý cho các thao tác pixel quan trọng

Không phụ thuộc vào API vector để tạo pixel-art production.

### Dữ liệu
- System.Text.Json

### Dependency Injection
- Microsoft.Extensions.DependencyInjection

### Logging
- Microsoft.Extensions.Logging

### Testing
- xUnit

## Yêu cầu bắt buộc về độ ổn định Pixel Art

Việc chuyển từ Python/Pillow sang C# **không được làm thay đổi chất lượng pixel**.

Renderer phải đáp ứng:

1. Tất cả tọa độ pixel là số nguyên.
2. Mọi sprite production được lưu dưới dạng RGBA.
3. Không anti-aliasing.
4. Không blur.
5. Không interpolation tuyến tính.
6. Phóng to/thu nhỏ preview bằng nearest-neighbor.
7. Không arbitrary rotation có interpolation.
8. Không transform phân số cho sprite production.
9. Mọi phép dịch chuyển sprite phải theo số pixel nguyên ở native resolution.
10. Outline, palette swap, mask và alpha phải thao tác trực tiếp trên bitmap/pixel buffer.
11. PNG export phải giữ alpha chính xác.
12. Có test so sánh output pixel cho các thao tác cốt lõi.

## Chiến lược migration từ Pillow

Prototype Python/Pillow chỉ dùng làm nguồn tham khảo.

Các cơ chế cần tái tạo tương đương bằng C#:

- compositing các layer RGBA;
- nearest-neighbor scaling;
- alpha mask;
- outline 1px;
- palette swap;
- layer order;
- crop/bounds;
- spritesheet packing;
- export PNG;
- grayscale/silhouette/debug view.

Nếu một thuật toán từ Pillow đang cho output tốt:
- port thuật toán theo logic tương đương;
- tạo golden-master test hoặc pixel-diff test để đảm bảo kết quả không sai lệch ngoài chủ ý.

## Kiến trúc solution đề xuất

```text
PixelGameStudio.sln

src/
├── PixelGameStudio.App
├── PixelGameStudio.Domain
├── PixelGameStudio.Core
├── PixelGameStudio.Rendering
├── PixelGameStudio.Assets
├── PixelGameStudio.Animation
├── PixelGameStudio.Behaviors
├── PixelGameStudio.ProjectSystem
├── PixelGameStudio.Validation
├── PixelGameStudio.Export
├── PixelGameStudio.Godot
└── PixelGameStudio.Infrastructure

tests/
├── PixelGameStudio.Domain.Tests
├── PixelGameStudio.Rendering.Tests
├── PixelGameStudio.ProjectSystem.Tests
└── PixelGameStudio.Export.Tests
```

## Nguyên tắc

- Domain không phụ thuộc Avalonia.
- Renderer không phụ thuộc ViewModel.
- UI chỉ gọi service/domain.
- Exporter không sửa project state.
- Mọi logic có thể test ngoài UI.
