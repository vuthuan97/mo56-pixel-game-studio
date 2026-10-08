# MO56 Pixel Game Studio — Prompt và task list nâng cấp layout V1

Repository: https://github.com/vuthuan97/mo56-pixel-game-studio

## Cách dùng

Đưa toàn bộ phần PROMPT MASTER bên dưới cho AI có quyền đọc và sửa repository. Yêu cầu làm theo từng phase, dùng task list ở cuối làm tiêu chí nghiệm thu. Không lấy trạng thái DONE của V1 cũ làm bằng chứng các yêu cầu mới đã hoàn thành.

Prompt tiếp tục sau mỗi lượt:

```text
Tiếp tục nâng cấp theo MO56_LAYOUT_ACTIONS_UPGRADE.md. Đọc .agents/AGENTS.md, docs/CURRENT_STATE.md và task list nâng cấp đã lưu trong repo. Kiểm tra code và thay đổi hiện tại; xác định phase chưa hoàn thành tiếp theo và triển khai đến khi phase đó dùng được. Giữ các thay đổi của người dùng. Chạy build/test liên quan, sửa lỗi, cập nhật tiến độ và báo cáo phần thực sự hoàn thành, phần chưa làm, cách kiểm tra trên UI. Không đánh dấu DONE cho scaffold hoặc hành động chỉ có tên nhưng chưa có chuyển động.
```

## PROMPT MASTER

Bạn là coding agent đang nâng cấp dự án C# Pixel Game Studio hiện có. Thực hiện trực tiếp trên code của repository, không tạo lại dự án từ đầu.

### 1. Đọc và audit trước khi sửa

- Đọc .agents/AGENTS.md, toàn bộ rules áp dụng, các skill dự án liên quan.
- Đọc docs/PRODUCT_OVERVIEW.md, ARCHITECTURE.md, TECH_STACK.md, CHARACTER_AND_BEHAVIOR_SPEC.md, STYLE_PROFILE_SPEC.md, CURRENT_STATE.md và tasks/TASK_LIST.md.
- Kiểm tra branch/commit hiện tại; không giả định commit từng đọc trước đây vẫn mới nhất.
- Audit MainWindow.axaml, code-behind, MainViewModel, InspectorViewModels, TimelineViewModels, Domain/Character, Domain/Animation, Domain/Behaviors, template, composer, project store và exporter.
- Đối chiếu tài liệu với code thật. CURRENT_STATE có thông tin loại bỏ Role khỏi workflow nhưng task cũ vẫn mô tả Role: không đưa Role/môn phái trở lại UI.
- Đọc legacy prototype theo quy định repository khi làm rig, pose, animation, anchor hoặc export.
- Lưu bản task list nâng cấp vào tasks/LAYOUT_ACTIONS_UPGRADE_TASK_LIST.md; giữ lịch sử task V1 cũ.

### 2. Công nghệ và nguyên tắc

Giữ C#/.NET 8/Avalonia/MVVM, renderer RGBA độc lập UI, System.Text.Json và xUnit. Không chuyển sang web, Electron hoặc framework khác. Giữ tọa độ nguyên, nearest-neighbor, không antialias/blur/bilinear và không quay sprite bằng interpolation.

Tái sử dụng service/domain hiện có; không tạo mô hình Character/Pose/Animation/Behavior song song. Không nhồi thêm logic nghiệp vụ vào MainViewModel hoặc code-behind. Tách View/ViewModel theo workspace và editor nhỏ, theo conventions hiện có. Giữ undo/redo, dirty state, autosave/recovery, project load-save và export đang hoạt động.

### 3. Layout cấp project

Thanh điều hướng chính chỉ có 5 tab, đúng thứ tự:

1. Dự án: cấu hình tên, thể loại, view/directions, canvas, palette và style chung.
2. Nhân vật: tạo và quản lý nhiều nhân vật trong project.
3. Vật phẩm / Đạo cụ: trang thông báo phiên bản tiếp theo, chưa triển khai chức năng tạo độc lập.
4. Bối cảnh: trang thông báo phiên bản tiếp theo cho background/địa hình/cây/công trình.
5. Xuất: chọn các đối tượng của project để xuất; V1 chỉ có nhân vật được hỗ trợ.

Không giữ Rig, Equipment, Animation, Behavior, Validation thành tab cấp project. Di chuyển các chức năng đó vào Nhân vật hoặc nhóm kiểm tra phù hợp, giữ đầy đủ chức năng đã có. Asset browser chuyển thành bộ chọn/thư viện theo ngữ cảnh trong từng editor, không chiếm một cột cố định toàn ứng dụng.

Thanh chung có tên project, Lưu, Undo, Redo và trạng thái dirty/autosave. Hai tab tương lai phải có nhãn rõ ràng; không có nút giả báo thành công.

### 4. Workspace Nhân vật

Một thanh ngắn phía trên: chọn nhân vật, Tạo mới, Nhân bản, Đổi tên, Xóa. Project chứa nhiều nhân vật; lưu lại nhân vật đang chọn là editor state nếu phù hợp, không trộn tùy tiện vào dữ liệu export.

Bên dưới chia 2 cột mặc định 2*:3*: trái preview 40%, phải cấu hình 60%, GridSplitter cho phép điều chỉnh và có min-width hợp lý. Kiểm tra tại 1280x800 và 1024x640; không tràn ngang hay che nút thao tác. Panel cấu hình cuộn độc lập.

Trái: canvas nhân vật, direction, scale 1x/2x/4x/10x, nền sáng/tối/checkerboard, play/pause và frame hiện tại. Nhóm Art QA/debug thu gọn giữ grayscale/silhouette/part/anchor, game-scale preview và validation. Preview giữ nguyên ngữ cảnh khi chuyển tab con; đổi nhân vật cập nhật tất cả editor, không tác động nhầm nhân vật khác.

Phải: 4 tab Bộ khung, Trang bị, Hoạt ảnh, Hành động. Không lồng thêm nhiều cấp tab; dùng section thu gọn cho cấu hình chi tiết.

### 5. Bộ khung

Tổ chức nhóm Tổng thể, Thân, Khuôn mặt, Tóc, Tay, Chân, Cấu trúc nâng cao. Chứa ngoại hình và cấu trúc mặc định: body, mắt, miệng, tóc, màu da, biến thể tay/chân; nâng cao chứa part graph, parent, anchor, pivot, offset và z-order.

Giới tính/core nhân vật và độ dài tay/chân là yêu cầu mới nếu code chưa hỗ trợ: triển khai thành dữ liệu/template và cơ chế render thật; không chỉ thêm control. Preset vóc dáng hoặc giới tính không được tự gắn môn phái/trang bị. Với thay đổi kích thước bộ phận phải cập nhật vị trí liên kết/anchor cần thiết, giữ pixel nguyên và không làm đứt rig.

Chỉnh ngoại hình mặc định ở Bộ khung; chỉnh tư thế của một frame ở Hoạt ảnh. Không để việc giơ tay/đá chân một frame làm thay đổi nhân vật gốc hoặc frame khác ngoài ý muốn.

### 6. Trang bị

Mỗi slot có tên, thumbnail, item đang gắn, chọn thay, tháo. Slot mẫu: vũ khí, tay phụ, đầu, áo trong, áo ngoài, giáp ngực, giáp vai, bao tay, quần, giày, đai, áo choàng, phụ kiện.

Lấy slot từ rig, không hard-code danh sách trong UI. Nhóm vũ khí/quần áo/giáp/phụ kiện. Lọc asset tương thích, import/chọn asset theo ngữ cảnh, cập nhật preview ngay. Giữ per-direction assets, anchor và z-order. Slot tùy chỉnh nằm ở mục nâng cao.

Trang bị nhân vật vẫn làm V1; tab Vật phẩm/Đạo cụ tương lai là nơi tạo đối tượng độc lập.

### 7. Hoạt ảnh

Trong panel phải: danh sách/chọn hoạt ảnh; tạo/nhân bản; FPS/loop; timeline thumbnail; add/copy/paste/delete/reorder frame; duration; chỉnh pose frame; onion skin; marker editor nâng cao.

Timeline chỉ hiện trong tab Hoạt ảnh và nằm trong phần phải, không lấy toàn bộ chiều ngang ứng dụng. Cho phép tăng chiều cao. Chọn frame cập nhật preview trái; play/step giữ đúng FPS và duration; non-loop dừng đúng frame cuối.

Chỉnh pose phải có semantics rõ: nếu pose đang dùng chung, chỉnh riêng frame mặc định cần clone-on-write hoặc cơ chế tương đương để không sửa hàng loạt frame. Cho phép lưu pose/hành động thành mẫu và dùng lại.

### 8. Hành động: chọn và Tạo


Giữ mô hình Behavior hiện có trong domain, hiển thị nhãn Hành động trên UI. Phân biệt rõ danh mục template dùng chung, hành động đã chọn cho từng nhân vật và animation/pose đã tạo.

UI: tìm kiếm, lọc nhóm, thẻ thumbnail, checkbox chọn nhiều, click xem thử riêng; footer cố định Đã chọn N hành động + nút Tạo. Chọn toàn bộ chỉ áp dụng các mục tương thích đang lọc; không tự tạo khi tick checkbox.

Nhóm và bộ mẫu tối thiểu:
- Cơ bản: đứng im, đi bộ, chạy, nhảy tại chỗ, xoay vòng.
- Tư thế: ngồi, nằm, chống nạnh, khoanh tay, quỳ.
- Tay/chân: đấm trái, đấm phải, đá trái, đá phải, giơ tay trái, giơ tay phải, vẫy tay, chỉ tay.
- Chiến đấu: chém, đâm, đỡ, né, bị đánh, chết.
- Sinh hoạt: ăn, uống, ngủ, đọc, viết, nói chuyện.
- Làm việc/tương tác: nhặt đồ, mang đồ, sửa chữa, đào, chặt, mở cửa.
- Tùy chỉnh: lưu mẫu do người dùng dựng từ pose/animation.

Danh mục và nhóm là data-driven, mở rộng bằng template, không viết switch theo tên tiếng Việt. Không tuyên bố hỗ trợ vô hạn hành động chỉ bằng danh sách tên.

Audit 66 preset hiện có: nhiều mục AnimationId=null; chạy hiện dùng walk; nhiều hành động dùng chung attack/cast/idle. Không xem chúng là chuyển động đã hoàn thiện. Template phải có dữ liệu chuyển động thực: pose sequence, duration/FPS/loop, states tay/chân, offset, view mapping, yêu cầu rig/slot và markers thích hợp.

Nút Tạo dùng nhân vật đang chọn + ngoại hình + trang bị + danh sách hành động + hướng của project để tạo pose/animation và bindings. Dùng composer hiện có để preview/render; không gọi AI bên ngoài hoặc dựng renderer thứ hai. Không tạo PNG mới ghi đè asset gốc của nhân vật. PNG/package được xuất ở tab Xuất.

Các động tác không dựng được bằng asset/state hiện có cần bổ sung state assets/template tương ứng; không dùng một pose idle hoặc đổi tên walk để giả lập chuyển động mới. Xoay vòng là chuỗi view theo profile khi phù hợp, không rotate bitmap bằng interpolation; với view profile không đủ hướng phải nêu không tương thích.

Có progress/cancel, kết quả theo từng hành động và lý do lỗi. Hành động chưa có template/không tương thích được thể hiện rõ, không báo thành công. Xác thực trước khi sửa; tạo theo đơn vị hành động atomic, hủy không để dữ liệu dang dở. Undo được kết quả tạo; redo phục hồi, dirty state/autosave hoạt động.

Tạo lại mặc định giữ animation đã chỉnh tay; cho chọn tạo bản mới hoặc thay thế có chủ đích. Không ghi đè im lặng. ID và quan hệ sở hữu phải tránh xung đột giữa nhiều nhân vật; không làm thay đổi template dùng chung.

Trạng thái thẻ: Có mẫu để tạo, Đã tạo, Cần tạo lại (nguồn đã đổi), Chưa có mẫu, Không tương thích. Đổi ngoại hình/trang bị cập nhật preview hoặc invalidation theo kiến trúc thực; lưu fingerprint/version nguồn nếu có dữ liệu cache để tránh dùng kết quả cũ.

### 9. Project và export

Lưu lựa chọn hành động, animation bindings và dữ liệu mới theo từng nhân vật. Audit schema hiện có, chỉ bump schema khi cần; có migration và tests đọc project cũ. Các field cũ/Role legacy vẫn đọc được theo chính sách repository.

Tab Xuất có danh sách nhân vật chọn nhiều, chọn hoạt ảnh/hướng và các tùy chọn frames/spritesheet/layers/Godot. Chỉ xuất hành động có animation hợp lệ. Xuất nhiều nhân vật tách folder/metadata không đè nhau; giữ frame map, duration/FPS/loop, marker và alpha. Không dùng active character như nguồn ngầm cho mọi mục được chọn.

### 10. Thực hiện và báo cáo

Triển khai lần lượt phase A-G trong task list. Sau mỗi phase chạy build và test liên quan, sửa lỗi, cập nhật task/state. Nếu môi trường không chạy được test phải báo chính xác, không đánh dấu verified.

Không xóa feature cũ khi chưa có vị trí tương đương. Không sửa golden fixtures để che regression. Với thay đổi hình ảnh chủ đích, ghi lý do và chỉ cập nhật fixture bằng quy trình dự án.

Cuối lượt báo cáo: code đã thay đổi, phase hoàn thành, build/test thực chạy, hướng dẫn thao tác nghiệm thu, hạn chế còn lại. Không tự merge/publish ngoài phạm vi yêu cầu.

## TASK LIST NÂNG CẤP

Quy ước: [ ] chưa hoàn thành hoặc chưa đủ bằng chứng; [~] đang làm; [x] đã code và kiểm chứng. Đối chiếu ngày 2026-10-08 tại `main` / `027e1c5`; bằng chứng và khoảng trống ghi ở `tasks/LAYOUT_ACTIONS_UPGRADE_TASK_LIST.md`. Build/test xanh không thay cho nghiệm thu UI hoặc chứng minh chuyển động thật.

### Phase A — Audit và thiết kế migration
- [x] Đọc rules/spec/current state; audit UI, services, schema và branch hiện tại.
- [x] Lập mapping workspace cũ → tab chính/tab con mới, gồm asset browser và Art QA.
- [x] Audit preset: animation thật, alias, thiếu template; ghi bảng kết quả.
- [x] Chốt quan hệ template/character selection/generated animation và chính sách shared pose.
- [x] Ghi baseline build/test, tạo task list riêng và đặc tả layout trong docs.

### Phase B — Shell và workspace Nhân vật
- [x] Tạo 5 tab chính đúng thứ tự; placeholder trung thực cho hai tab tương lai.
- [x] Tách View/ViewModel theo workspace, tái sử dụng service hiện có.
- [x] Thanh project Lưu/Undo/Redo/dirty và trạng thái autosave.
- [x] Thanh quản lý nhiều nhân vật: chọn/tạo/nhân bản/đổi tên/xóa.
- [x] Layout 40/60 với splitter, min-width và cuộn riêng.
- [x] Preview chung: hướng/zoom/nền/play/frame; Art QA/debug thu gọn.
- [x] Bốn tab con; giữ lựa chọn và ngữ cảnh khi chuyển tab.
- [ ] Kiểm tra 1280x800 và 1024x640; không mất chức năng cũ.

### Phase C — Bộ khung và Trang bị
- [x] Di chuyển appearance/rig controls vào đúng nhóm.
- [x] Core/template giới tính và vóc dáng hoạt động thật, lưu/đọc được.
- [x] Tay/chân: cấu hình độ dài và liên kết anchor/render pixel nguyên.
- [x] Phân biệt ngoại hình mặc định với pose frame.
- [x] Trang bị theo slot data-driven, thumbnail/chọn/tháo/filter/import.
- [x] Preview đúng bốn hướng được hỗ trợ; slot tùy chỉnh hoạt động.
- [x] Undo/redo và isolation giữa hai nhân vật.

### Phase D — Hoạt ảnh
- [x] Timeline nằm trong panel phải của tab Hoạt ảnh.
- [x] Giữ add/copy/paste/delete/reorder/duration/FPS/loop/markers.
- [x] Play/pause/step và onion skin liên kết preview trái.
- [x] Chỉnh pose riêng frame không sửa shared pose ngoài ý muốn.
- [x] Tạo/nhân bản hoạt ảnh, lưu pose và mẫu hành động tùy chỉnh.
- [x] Non-loop và thời lượng frame được kiểm chứng.

### Phase E — Thư viện và bộ tạo hành động
- [x] Danh mục template data-driven và nhóm, trạng thái khả dụng.
- [x] Tìm kiếm/lọc/thẻ/checkbox/xem thử; footer số lượng + Tạo.
- [x] Lưu danh sách hành động đã chọn theo từng nhân vật.
- [x] Template chuyển động và kiểm tra rig/view/slot tương thích.
- [x] Bộ cơ bản: đứng, đi, chạy, nhảy tại chỗ, xoay vòng có output thực.
- [x] Bộ tư thế: ngồi, nằm, chống nạnh, khoanh tay, quỳ có output thực.
- [x] Bộ tay/chân: đấm/đá/giơ tay trái-phải, vẫy, chỉ có output thực.
- [x] Bộ chiến đấu: chém, đâm, đỡ, né, bị đánh, chết có output thực.
- [x] Bộ sinh hoạt: ăn, uống, ngủ, đọc, viết, nói chuyện có output thực.
- [x] Bộ tương tác: nhặt, mang, sửa chữa, đào, chặt, mở cửa có output thực.
- [x] Bổ sung state assets cần thiết, không giả lập bằng idle/alias.
- [x] Service tạo pose/animation/bindings; ID không xung đột nhiều nhân vật.
- [x] Progress/cancel; atomic từng hành động; thông báo lỗi cụ thể.
- [x] Tạo lại giữ bản chỉnh tay, hỗ trợ bản mới/thay thế chủ đích.
- [x] Undo/redo thao tác tạo; template dùng chung không bị mutate.
- [x] Đổi nguồn đánh dấu cần tạo lại khi thích hợp; không dùng cache lỗi thời.

### Phase F — Lưu project và Xuất
- [x] Serialization dữ liệu hành động/bindings/nguồn theo nhân vật.
- [x] Migration và mở project cũ; không mất appearance/equipment/animation.
- [x] Autosave/recovery phục hồi dữ liệu mới.
- [x] Chọn nhiều nhân vật/hoạt ảnh/hướng trong tab Xuất.
- [x] Export frames/spritesheet/layers/Godot, metadata và folder riêng.
- [x] Chặn animation thiếu/hỏng, không xuất nhầm nhân vật đang active.

### Phase G — Kiểm chứng và tài liệu
- [x] Build solution và test liên quan; báo rõ môi trường/lệnh/kết quả.
- [x] Regression render/alpha/nearest và golden tests phù hợp.
- [x] Test save/load hai nhân vật có hành động khác nhau.
- [x] Test generation compatibility, deterministic output, shared-template isolation.
- [x] Test tạo lại sau chỉnh tay, cancel và undo/redo.
- [x] Test export nhiều nhân vật, frame map/duration/loop/marker đúng.
- [ ] Nghiệm thu UI với project mới và project cũ.
- [x] Cập nhật CURRENT_STATE, USER_GUIDE, CHARACTER_SYSTEM, PROJECT_FORMAT, EXPORT_FORMAT và checklist.
- [x] Chỉ đánh dấu từng mục DONE khi có bằng chứng, ghi phần chưa làm.

## Kịch bản nghiệm thu thủ công

1. Mở project cũ: dữ liệu và preview còn đúng; chỉ thấy 5 tab chính.
2. Tạo nhân vật A và B; đổi ngoại hình/trang bị A không ảnh hưởng B.
3. Chuyển bốn tab con: preview và nhân vật đang chọn được giữ.
4. Chọn đi, chạy, nhảy, đấm trái, đá phải cho A; click xem thử không đổi checkbox.
5. Bấm Tạo: từng hành động có animation/frame thật; chạy khác chuyển động đi, nhảy có rời đất và tiếp đất, động tác trái/phải đúng theo nhân vật.
6. Chọn hành động không tương thích: có lý do, không báo đã tạo.
7. Mở hoạt ảnh vừa tạo, sửa một frame; frame khác/nhân vật B/template không thay đổi.
8. Tạo lại: bản chỉnh tay được giữ mặc định; tạo bản mới không trùng ID.
9. Undo/redo việc tạo; thử hủy giữa chừng, không có animation dang dở.
10. Lưu, đóng, mở lại và phục hồi autosave: lựa chọn và dữ liệu vẫn đúng.
11. Xuất cả A và B: folder riêng, đúng hoạt ảnh/hướng, PNG giữ alpha và Godot đọc được.
12. Resize về 1024x640: timeline, footer Tạo và nút chính vẫn dùng được.

## Mẫu báo cáo mỗi lượt

```text
Phase thực hiện:
Thay đổi và file chính:
Task đã hoàn thành và bằng chứng:
Build/test đã chạy và kết quả:
Thao tác nghiệm thu trên UI:
Hạn chế/task còn lại:
Phase tiếp theo:
```
