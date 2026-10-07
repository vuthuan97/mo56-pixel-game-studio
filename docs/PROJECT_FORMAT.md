# PROJECT FORMAT — schema v1

Mỗi project là **một thư mục**:

```text
MyProject/
├── project.pgsproj     # JSON: toàn bộ metadata (không nhúng binary)
├── assets/             # PNG asset của project (id.png)
│   └── thumbs/         # thumbnail cache (tự sinh, có thể xóa)
├── autosave/
│   └── project.pgsproj # bản khôi phục (ghi mỗi 30s khi dirty)
└── (export/…)          # thư mục export do người dùng chọn
```

## project.pgsproj

JSON UTF-8 (camelCase, indent, giữ tiếng Việt). Cấu trúc (schemaVersion 1):

```jsonc
{
  "schemaVersion": 1,
  "projectId": "hex32",
  "name": "Tên project",
  "createdAtUtc": "…", "modifiedAtUtc": "…",
  "game":   { "genre": "Nhập vai top-down", "notes": "" },
  "view":   { "perspective": "TopDown4", "directions": ["Down","Up","Left","Right"], "defaultDirection": "Down" },
  "pixels": { "canvasWidth": 32, "canvasHeight": 46, "pixelSize": 1 },
  "palette": { "name": "Mặc định", "maxColors": 32,
               "colors": [ { "name": "Outline", "hex": "#16101EFF" }, … ] },
  "style":  { "outlineStyle": "External1px", "outlineThickness": 1, "outlineColorHex": "#16101EFF",
              "lightDirection": "TopLeft", "shadowLevels": 1, "highlightLevels": 1,
              "detailDensity": "Medium", "transparentBackground": true, "alphaThreshold": 1,
              "nearestNeighborOnly": true,
              "character": { "headHeightPx": 15, "torsoHeightPx": 15, "legHeightPx": 12 },
              "tileSizePx": 32, "minValueRange": 90 },
  "assets":   [ { "id", "displayName", "type", "file", "canvasWidth", "canvasHeight",
                  "tags": [], "views": [], "anchor": { "x", "y" }, "zIndex", "importedAtUtc", "notes" } ],
  "rigs":     [ { "id", "displayName", "parts": [ PartNode ], "anchors": [ AnchorPoint ],
                  "equipmentSlots": [ EquipmentSlotDef ] } ],
  "characters": [ { "id", "name", "rigId", "appearance": [ PartAppearance ],
                    "equipment": [ { "slotId", "assetId", "viewAssets": {} } ], "roleId", "notes" } ],
  "poses":      [ { "id", "displayName", "parts": { partId: { offsetX, offsetY, hidden, states } },
                    "slots": { slotId: { offsetX, offsetY, hidden, zIndexOverride, states } }, "stateValues": {} } ],
  "animations": [ { "id", "displayName", "fps", "loop",
                    "frames": [ { "poseId", "durationTicks", "markers": [ { "type", "value" } ] } ] } ],
  "behaviors":  [ { "id", "displayName", "group", "animationId", "heldItemSlotId",
                    "heldItemAssetId", "interactionAnchorId", "markers": [ { "type", "name", "frameIndex" } ] } ],
  "roles":      [ { "id", "displayName", "group", "description",
                    "startingEquipment": [ { "slotId", "assetFamily" } ],
                    "behaviorIds": [], "defaultAnimationId", "tags" } ]
}
```

## Quy tắc

- **Không nhúng binary vào JSON** — PNG nằm ở `assets/`, JSON chỉ giữ `AssetDefinition` (file tương đối, `/`-phân cách, cấm `..`).
- **Id asset**: chữ/số bắt đầu, cho phép `- _ .` (không `..`), tối đa 64 ký tự; duy nhất case-insensitive.
- **Asset family**: tiền tố id không kèm view suffix (vd `eq.main_hand.kiem`); per-view resolve thành `{family}.{view-slug}`.
- **View/direction**: key từ ViewProfile (TopDown4/8, Diagonal4, SideView2, Frontal, Custom); asset khai báo `views` phải ⊆ ViewProfile.
- **Forward-compat**: field lạ bị bỏ qua khi load; `schemaVersion` lớn hơn app hỗ trợ → từ chối mở.
- **Ghi atomic**: temp file + move; Save xong dọn `autosave/`.
## MO56 persisted fields

Existing project files remain readable when `CharacterEntity.build` and `actionIds` are absent; deserialization supplies safe defaults. New files persist those fields and ProjectStore keeps its atomic save/autosave behavior. Unknown fields continue to be ignored for forward compatibility.
