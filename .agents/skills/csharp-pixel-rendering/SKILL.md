# Skill — Render Pixel bằng C#

## Khi dùng

Mọi task liên quan render, preview, compositing, export PNG/spritesheet.

## Quy trình

1. Đọc `docs/TECH_STACK.md`.
2. Xác định thao tác ở native pixel resolution.
3. Chỉ dùng integer coordinate.
4. Render vào RGBA bitmap/pixel buffer.
5. Không anti-alias.
6. Preview scale bằng nearest-neighbor.
7. Viết unit test.
8. Khi port từ prototype, dùng pixel-diff/golden-master khi phù hợp.
9. UI chỉ nhận bitmap kết quả; không nhét thuật toán pixel vào code-behind/View.
