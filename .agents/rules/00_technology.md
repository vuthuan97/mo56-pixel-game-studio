# Rule — Công nghệ bắt buộc

Pixel Game Studio production sử dụng:

- C#
- .NET 8
- Avalonia UI
- MVVM
- System.Text.Json
- xUnit

Không tự đổi framework.

Pixel renderer:
- bitmap/RGBA;
- integer coordinates;
- nearest-neighbor;
- no anti-alias;
- no blur;
- no bilinear;
- no fractional transform cho sprite production.

Renderer phải test được mà không cần chạy Avalonia.
