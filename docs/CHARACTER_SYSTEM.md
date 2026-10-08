# CHARACTER SYSTEM — Rig, Pose, Animation, Behavior

## Mô hình phân tầng (rule 04_animation_behavior)

```text
RigDefinition (cây PartNode + anchors + slots)
   └── CharacterEntity (appearance + equipment)      ← "mặc đồ" lên rig
         └── PoseDefinition (trạng thái rig 1 thời điểm)
               └── AnimationDefinition (chuỗi frame = pose + duration + markers)
                     └── BehaviorDefinition (animation + held item + anchor + markers)
```

## Rig — `RigDefinition`

- **PartNode**: `id, displayName, parentId, anchorId, pivot, zIndex, assetRef, transform{offsetX/Y integer, mirrorX}, states, tags`. Cây 1 root; validate chặn cycle/parent thiếu.
- **AnchorPoint**: điểm tên gắn vào part (offset nguyên) — dùng cho equipment, interaction, effect.
- **EquipmentSlotDef**: `id, displayName, anchorId, zIndex, allowedTags` — slot là **data của rig**; mỗi slot 1 item.
- Template `HumanoidTopDown4` (10 part, 16 anchor, 13 slot) chỉ là template — chỉnh/xóa/thêm tự do. Z-order 1..22 khớp thứ tự composite legacy.

## Appearance — `PartAppearance`

- `AssetRef` (mặc định), `ViewAssets` (per direction), `ViewMirrors` (mirror ngang), `States` (giá trị mặc định), `StateAssets` (state → variant → per-view), `PaletteOverride` (remap màu part — skin/hair tone).
- Composer resolve: **pose state → state variant → view asset → default**, rồi palette swap → mirror → composite theo z.

## Pose — `PoseDefinition`

- `Parts`: partId → `PartPose{offsetX, offsetY, hidden, states}` (toàn số nguyên).
- `Slots`: slotId → `SlotPose{offsetX, offsetY, hidden, zIndexOverride, states}` (vd weapon raise/slash → art front + z 23).
- Pose là **shared definition** theo id. Khi sửa qua editor trên một frame đang dùng pose chung, editor tạo bản sao trước khi thay đổi (clone-on-write); các frame khác giữ pose cũ. Chỉnh trực tiếp `Project.Poses` ngoài editor vẫn là sửa dữ liệu dùng chung.

## Animation — `AnimationDefinition`

- `Frames`: danh sách `{poseId, durationTicks, markers[]}`; `Fps` (1..60); `Loop`.
- `FrameMarker` types: Footstep, Sound, Hit, SpawnEffect, SpawnProjectile, Interaction, PickUp, Drop, Custom.
- Timeline UI ở panel phải tab Hoạt ảnh: Add/Copy/Paste/Delete/Move ◀▶; onion skin (fade 22% frame trước). Playback dùng `DurationTicks / FPS` cho từng frame; non-loop dừng ở frame cuối.

## Behavior — `BehaviorDefinition`

- `AnimationId` + `HeldItemSlotId/HeldItemAssetId` + `InteractionAnchorId` + `Markers[]` (Effect/Sound/Gameplay @ frameIndex).
- Preset 66 behavior theo 5 nhóm PRODUCT_OVERVIEW — **data, không hard-code UI**; nhân bản → behavior tùy chỉnh; template export/import JSON.

## QA (Phase 10)

- `SpriteAnalyzer`: palette count, visible bounds, value range (ngưỡng từ StyleProfile).
- `CharacterQaService.CheckCharacter`: coverage view per part, compose thử → contrast/noise (tỷ lệ pixel đơn lẻ), nền đặc vs trong suốt; asset on-disk khớp khai báo.
- Preview: Native 1x/2x/4x/10x nearest-neighbor + **Game preview** 360×640 (nền + lưới + NPC silhouette).
## MO56 character build and actions

`CharacterEntity.Build` stores gender, body type and bounded native-pixel head/torso/arm/leg/foot dimensions per character. The composer applies only clamped integer part/anchor deltas, so changing one character cannot alter another character.

`ActionTemplateCatalog` đánh giá rig slot/anchor trước khi tạo. `SelectedActionIds` là lựa chọn checkbox chưa tạo; `ActionIds` là các mẫu đã tạo. `GeneratedActionBindings` lưu animation ID và fingerprint nguồn của từng mẫu. Output mới dùng ID ổn định riêng theo `character.Id` và template, nên hai nhân vật chọn cùng một mẫu vẫn có pose/animation tách biệt. Khi nguồn đổi, thẻ báo cần tạo lại; bấm Tạo mặc định giữ bản đã chỉnh tay. Có nút tạo version mới và nút thay thế chủ đích; thay thế bị từ chối nếu pose đang dùng chung hoặc output là legacy toàn project. Danh mục hiện mới có 14 mẫu cơ bản, chưa đủ toàn bộ bộ hành động MO56.
