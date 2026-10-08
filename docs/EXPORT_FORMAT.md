# EXPORT FORMAT

Output của **Export workspace** (ExportService). Mọi thứ nearest-neighbor, alpha chính xác, không interpolation.

Tab Xuất chọn nhiều nhân vật, animation/tư thế và hướng. `ExportBatch` tạo một package cho mỗi cặp tương thích trong `ExportDir/characters/{characterId}/animation-{animationId}/`; tư thế mặc định nằm ở `default-pose/`. Action thuộc nhân vật khác bị bỏ qua và báo số cặp đã bỏ qua, không xuất nhầm. Các đường dẫn dựa trên lựa chọn rõ ràng, không dựa vào nhân vật đang preview. Export một nhân vật trực tiếp vẫn dùng cấu trúc package dưới đây ngay tại thư mục được truyền cho service.

## Cấu trúc thư mục

```text
ExportDir/
├── frames/{direction}/{direction}_{i:00}.png   # frame compose hoàn chỉnh (tùy IncludeFrames)
├── spritesheet_{direction}.png                  # lưới cell = max frame size (tùy IncludeSpritesheet)
├── layers/{direction}/{frame_key}/{layer}.png   # từng part/slot riêng (tùy IncludeLayers)
├── manifest.json                                # frame map + sheets (luôn xuất)
├── package.json                                 # metadata character/animation/style (luôn xuất)
└── godot/
    ├── sprite_frames.tres                       # SpriteFrames format 3
    ├── character_sprite_example.gd
    └── README_GODOT.md
```

## manifest.json

```jsonc
{
  "character": { "id", "name", "rig" },
  "view": "TopDown4",
  "directions": ["Down"],
  "animation": "walk", "fps": 6, "loop": true,
  "sheets": [ {
      "file": "spritesheet_down.png", "view": "Down",
      "cellWidth": 32, "cellHeight": 46,
      "frames": [ { "key": "down_00", "column": 0, "row": 0 }, … ]
  } ]
}
```

**Frame map là phần bắt buộc** —spritesheet không kèm map là thiếu sót của legacy (LEGACY_AUDIT §9).

## package.json

`character` (id/name/rig/equipment[]) + `view` + `animation` (frames + markers) + `style` + `included` — không trộn state UI như legacy.

## Godot

- `sprite_frames.tres` (format 3): `ext_resource` per frame trỏ `../frames/{view}/{key}.png`; animation name **`<animation>_<direction>`**; `speed` = FPS; `loop` theo AnimationDefinition (vd `death` không loop).
- `character_sprite_example.gd`: `play_animation(anim, direction)`.
- Tích hợp: copy thư mục export vào project Godot, gán .tres vào `AnimatedSprite2D`, gọi `play("walk_down")`.
- Chọn Godot với animation bắt buộc bật `IncludeFrames`, vì `.tres` tham chiếu các PNG frame. `DurationTicks` được giữ trong `package.json`; Godot SpriteFrames hiện dùng FPS đồng nhất nên chưa thể hiện thời lượng khác nhau theo frame.

## Layers

Mỗi layer = **full canvas** với một part/slot (tên = part id hoặc `slot.{slotId}`), xuất theo thứ tự z (back→front), không outline — dùng để composite lại trong engine hoặc QA từng lớp.

## Options

| Option | Ý nghĩa |
|---|---|
| Views | Mặc định = mọi direction trong ViewProfile |
| Animation | Frame của animation; bỏ trống = pose mặc định |
| IncludeFrames | PNG từng frame |
| IncludeSpritesheet | Spritesheet + sheet entry trong manifest |
| IncludeLayers | Thư mục layers per frame |
| IncludeGodot | sprite_frames.tres + gdscript + readme (cần chọn animation) |
## MO56 action metadata

`package.json` ghi build, `actionIds` đã tạo và mapping action → animation; `manifest.json` ghi mapping đó và frame map. `sourceFingerprint` SHA-256 của output mới được lưu trong `CharacterEntity.GeneratedActionBindings` tại lúc tạo, giúp editor phát hiện nguồn đổi; output legacy không có binding dùng fingerprint tính lúc export để tương thích. Package của hai nhân vật nằm trong thư mục riêng và render appearance/equipment riêng. Export từ chối animation thiếu pose/FPS/duration hợp lệ hoặc animation action không thuộc nhân vật đã chọn; không âm thầm chuyển sang pose mặc định. Sprite được composer dựng lại từ appearance/equipment hiện tại khi xuất, không lấy PNG cache cũ.
