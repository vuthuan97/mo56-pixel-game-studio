# EXPORT FORMAT

Output của **Export workspace** (ExportService). Mọi thứ nearest-neighbor, alpha chính xác, không interpolation.

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

`package.json` and `manifest.json` include the selected character build, selected action ids, generated animation bindings and a SHA-256 `sourceFingerprint` for each action. Exporting two characters uses separate output directories and resolves each character's appearance/equipment independently.
