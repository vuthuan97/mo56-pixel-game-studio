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
- Pose là **shared definition** (theo id) — sửa pose ảnh hưởng mọi animation dùng nó (giống legacy overrides).

## Animation — `AnimationDefinition`

- `Frames`: danh sách `{poseId, durationTicks, markers[]}`; `Fps` (1..60); `Loop`.
- `FrameMarker` types: Footstep, Sound, Hit, SpawnEffect, SpawnProjectile, Interaction, PickUp, Drop, Custom.
- Timeline UI: Add/Duplicate/Copy/Paste/Delete/Move ◀▶; onion skin (fade 22% frame trước).

## Behavior — `BehaviorDefinition`

- `AnimationId` + `HeldItemSlotId/HeldItemAssetId` + `InteractionAnchorId` + `Markers[]` (Effect/Sound/Gameplay @ frameIndex).
- Preset 66 behavior theo 5 nhóm PRODUCT_OVERVIEW — **data, không hard-code UI**; nhân bản → behavior tùy chỉnh; template export/import JSON.

## QA (Phase 10)

- `SpriteAnalyzer`: palette count, visible bounds, value range (ngưỡng từ StyleProfile).
- `CharacterQaService.CheckCharacter`: coverage view per part, compose thử → contrast/noise (tỷ lệ pixel đơn lẻ), nền đặc vs trong suốt; asset on-disk khớp khai báo.
- Preview: Native 1x/2x/4x/10x nearest-neighbor + **Game preview** 360×640 (nền + lưới + NPC silhouette).
## MO56 character build and actions

`CharacterEntity.Build` stores gender, body type and bounded native-pixel head/torso/arm/leg/foot dimensions per character. The composer applies only clamped integer part/anchor deltas, so changing one character cannot alter another character.

`ActionTemplateCatalog` evaluates required rig slots/anchors before generation. A generated action owns its `PoseDefinition` objects and `AnimationDefinition`; it is not an alias of `idle` or `walk`. `ActionIds` records the generated selections on the character.
