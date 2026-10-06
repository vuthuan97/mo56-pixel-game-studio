# Đặc tả Nhân vật, Tư thế, Hoạt ảnh và Hành vi

## Khối nhân vật
Rig là cây PartNode. Humanoid mặc định có Root, Torso, Head, vai, tay, cẳng tay, bàn tay, hông, đùi, cẳng chân, bàn chân, hair/face attachments. Người dùng nâng cao có thể thêm part tùy chỉnh.

## Pose Editor
Cho phép chỉnh transform/state của root, torso, head, tay, chân, hair, equipment, held item. Pose có thể lưu vào thư viện.

## Animation Editor
- timeline;
- add/delete/duplicate/reorder/copy/paste frame;
- duration per frame;
- FPS;
- loop;
- play/pause;
- onion skin;
- direction variants;
- event markers.

## Behavior Editor
Behavior là thứ người dùng chọn theo nghĩa gameplay. Ví dụ `Chặt cây` = animation chop + axe + interaction anchor + HitResource marker + sound/effect metadata.

Behavior phải hỗ trợ:
- preset;
- tạo mới;
- nhân bản;
- sửa/xóa behavior tùy chỉnh;
- lưu/nhập/xuất template;
- không giới hạn vào danh sách preset.

## Event marker
Footstep, Sound, Hit, SpawnEffect, SpawnProjectile, Interaction, PickUp, Drop, Custom.
