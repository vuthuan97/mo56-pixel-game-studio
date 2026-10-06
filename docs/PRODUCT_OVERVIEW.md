# Tổng quan Pixel Game Studio

## Mục tiêu
Ứng dụng desktop sản xuất tài nguyên game 2D pixel đồng bộ theo project. V1 tập trung Character Studio nhưng lõi phải dùng lại được cho các version asset khác.

## V1 — Xưởng tạo nhân vật
- Tạo/mở/lưu project.
- Chọn thể loại game: quản lý/tycoon, sinh tồn, nhập vai top-down, đi cảnh ngang, nền tảng, phiêu lưu/trải nghiệm, chiến thuật, tùy chỉnh.
- Chọn góc nhìn: top-down 4/8 hướng, chéo 4 hướng, side-view 2 hướng, chính diện, tùy chỉnh.
- Chọn pixel size/canvas size, bảng màu, viền, đổ bóng, mật độ chi tiết, tỷ lệ nhân vật.
- Tạo nhân vật theo archetype/rig.
- Tạo thân thể, mặt, tóc, skin.
- Trang bị tách khỏi thân thể.
- Slot trang bị tùy chỉnh theo project.
- Pose Editor.
- Animation Timeline.
- Behavior Library + Custom Behavior Editor.
- Event Marker theo frame.
- Asset Library.
- Art QA và game-scale preview.
- Export PNG, spritesheet, layer, package, Godot.

## Hành vi dựng sẵn V1
### Di chuyển
đứng, đi, chạy, chạy nhanh, nhảy, rơi, tiếp đất, bò, leo, né, lăn, trượt.

### Chiến đấu
thủ thế, đánh nhẹ, đánh mạnh, combo, đỡ, phản đòn, bắn, ném, dùng kỹ năng, bị đánh, choáng, chết, chiến thắng.

### Sinh tồn
chặt cây, đào đá, đào đất, câu cá, hái, thu thập, chế tạo, xây dựng, sửa chữa, ăn, uống, mang đồ, đẩy, kéo.

### Quản lý / Tycoon
làm việc, đào mỏ, làm ruộng, rèn, nấu, phục vụ, mua bán, vận chuyển, ngủ, nghỉ, nói chuyện, xếp hàng.

### Phiêu lưu / Trải nghiệm
ngồi, nằm, vẫy tay, chỉ tay, suy nghĩ, cười, khóc, tức giận, bất ngờ, đọc, viết, mở cửa, nhặt đồ, đưa đồ, nhận đồ, tương tác vật thể.

## Tạo hành vi tùy chỉnh
Người dùng phải có thể tự tạo hành vi bằng cách chỉnh root/thân/đầu/tay/cẳng tay/bàn tay/chân/bàn chân/tóc/vũ khí/vật cầm theo từng frame, thêm/xóa/nhân bản/reorder frame, chỉnh thời lượng, onion skin, event marker và lưu thành mẫu dùng lại.

## Nguyên tắc dài hạn
Style thuộc **Project**, không thuộc riêng từng asset.
