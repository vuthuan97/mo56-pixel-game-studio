# MO56 layout and actions upgrade

## Workspace mapping

| Previous top-level workspace | New location | Notes |
|---|---|---|
| Project | Project | Project profile editor remains the project-level editor. |
| Character | Character / Frame | Character identity and appearance stay character-scoped. |
| Rig | Character / Frame / advanced | Existing rig inspector is retained as migration-compatible code and will be folded into the advanced Frame section. |
| Equipment | Character / Equipment | Slot editor remains data-driven from the selected rig. |
| Animation | Character / Animation | Timeline and pose editing are character-scoped. |
| Behavior | Character / Actions | Existing behavior model is retained; the action generator is a later phase. |
| Validation | Project / QA-debug | Validation is no longer a top-level workspace; preview debug controls remain available. |
| Export | Export | Chọn nhiều nhân vật/animation/hướng; package riêng cho từng cặp. |
| Asset browser | Contextual Character browser | Visible for Equipment and Actions instead of occupying the whole application shell. |

## Migration rules

- The five top-level ids are `Project`, `Character`, `Library`, `Background`, and `Export`.
- `Library` and `Background` are explicit placeholders, not false success states.
- Character subtabs are `Frame`, `Equipment`, `Animation`, and `Actions`.
- Existing project JSON, legacy workspace state, Role fields and asset files remain readable. Role is not returned to the UI.
- Existing animation and behavior templates are not promoted to “generated actions” until they have real pose/frame output and compatibility checks.

## Preset audit (2026-10-08)

`BehaviorPresets.All` có **66** mục: Di chuyển 11, Chiến đấu 13, Sinh tồn 14, Quản lý 12, Phiêu lưu 16. **27** mục có `AnimationId=null` (chưa có animation preset); **39** mục còn lại gắn vào chỉ **6** animation chia sẻ: `attack` 13, `idle` 11, `cast` 6, `hurt` 4, `walk` 4, `death` 1. `beh.run` dùng `walk`; `beh.wave` dùng `cast`; các hành vi dùng `attack` không phải chuyển động riêng. `ActionTemplateCatalog` hiện có 14 mẫu sinh pose/animation riêng nhưng chưa đáp ứng toàn bộ các bộ động tác MO56; không được đếm 66 behavior preset là 66 action đã dựng.

## Ownership và migration

- `ActionTemplateCatalog.All` là danh mục dùng chung, bất biến. `CharacterEntity.SelectedActionIds` là checkbox lựa chọn, chưa tạo dữ liệu. `ActionIds` ghi mẫu đã tạo; `GeneratedActionBindings` lưu animation ID + fingerprint nguồn. ID output mới là `action.{templateId}.{SHA256(characterId)[0..16]}`, pose thêm `.{frameIndex}`; hai nhân vật không chia sẻ frame theo mặc định.
- Dữ liệu cũ có `action.{templateId}` vẫn được đọc và không bị ghi đè khi nhân vật đã có `ActionIds`. Export ưu tiên ID có owner, rồi fallback ID cũ. Khi nhân bản nhân vật, giữ lựa chọn nhưng không sao chép `ActionIds` vì output thuộc nhân vật nguồn.
- Pose dùng chung giữa nhiều frame vẫn được phép; sửa pose qua editor dùng clone-on-write để frame được sửa không đổi frame khác. Tạo lại mặc định giữ output cũ; người dùng có nút tạo version mới (`.v2`, `.v3`) hoặc thay thế rõ ràng. Thay thế từ chối output legacy dùng chung hay pose được animation khác tham chiếu. Fingerprint gồm template, rig, view/style và ngoại hình/trang bị/vóc dáng; đổi nguồn đánh dấu "Cần tạo lại". Composer dựng lại art lúc preview/export, không dùng cache PNG output cũ.
- Export batch nhận danh sách nhân vật/animation/hướng rõ ràng; không dùng nhân vật đang preview làm nguồn ngầm. `ExportCharacter` từ chối animation action thuộc nhân vật khác.

## Phase B implementation notes

The shell uses four physical Character Views through inspector DataTemplates. Character management is visible above all four editors; the Animation timeline is inside the right editor panel. The contextual browser collapses outside Equipment/Actions, and preview/configuration columns use a 2*:3* split with independent scrolling. Native resize acceptance at 1280×800 and 1024×640 remains unverified. Project Save/Undo/Redo remain in the main menus, honoring the earlier request to remove those buttons from the workspace.

## Phase C-F implementation notes

- Frame now owns per-character build data (`Gender`, `BodyType`, head/torso/arm/leg/foot dimensions). The composer consumes bounded integer deltas for parts and equipment anchors, preserving safe native-pixel placement.
- Equipment selectors are generated from the selected rig and asset tags, display cached thumbnails, support remove/empty, and can assign the selected contextual-browser asset after tag validation.
- Animation timeline is selectable inside the right panel, exposes current-frame duration ticks, and uses clone-on-write when a shared pose is edited. Preview playback now waits `DurationTicks / FPS` for each frame and non-loop stops on the last frame (pure playback tests).
- Actions use `ActionTemplateCatalog`: availability reasons are explicit, generation creates dedicated poses and animation frames, cancellation is atomic, progress is reportable, and generated action ids are stored on the character.
- Character package/manifest export includes build data, generated action IDs, resolved animation bindings and the persisted SHA-256 source fingerprint (legacy fallback computes one at export). Multi-character batch export writes isolated packages; tests cover different actions on two characters and wrong-owner rejection. Full catalog motion coverage and fixed-footer UI remain open.
