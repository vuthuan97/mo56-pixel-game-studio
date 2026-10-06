# LEGACY AUDIT — Pixel Sprite Studio v6

> Audit performed 2026-10-03 against `legacy-reference/pixel_sprite_studio_v6/`
> (source kept read-only). Pillow version pinned during audit: **12.3.0** —
> re-rendering the default character with it reproduces
> `samples/sample_package_v6/animations/Down/idle/idle_0.png` **pixel-exactly
> (0/1472 diff)**, so all shipped sample PNGs are valid golden references.
>
> **Refresh sau Phase 0 + Phase 1** (cùng ngày): bổ sung §8 (hành vi Pillow đã
> pin khi port), §9 (cấu trúc sample package chi tiết), §10 (audit asset system
> cho Phase 2); mapping §6 có cột trạng thái migration. Toàn vẹn source được
> bảo vệ bằng `tools/legacy_checksums.txt` (984 file — zip khớp thư mục 100%
> ngoại trừ `__pycache__/*.pyc` tái sinh khi import).

## 1. Kiến trúc hiện tại

```text
main.py (Tk entry)
└── sprite_studio/
    ├── core/
    │   ├── models.py      CharacterSpec, Pose, SkillSpec, AIConfig (dataclass + JSON)
    │   └── catalog.py     palettes, body options, 13 fixed equipment slots, class templates
    ├── rendering/
    │   └── renderer.py    CharacterRenderer: procedural Pillow drawing, 32x46
    ├── animation/
    │   └── poses.py       30 hard-coded POSES, 11 ANIMATIONS, CLASS_ANIMATION_MAP
    ├── editor/
    │   ├── pose_editor.py PoseOverrideStore (per-pose field overrides, JSON)
    │   ├── anchors.py     AnchorStore (per-direction named anchors, JSON)
    │   └── art_qa.py      analyze_sprite (palette count, bbox, value range, warnings)
    ├── assets/
    │   └── library.py     AssetLibrary (PNG import per slot + JSON sidecar)
    ├── effects/
    │   └── library.py     ELEMENT_COLORS (5 elements → rgba)
    ├── exporting/
    │   └── exporter.py    export_package (frames, layers, spritesheet, package.json)
    ├── godot/
    │   └── exporter.py    sprite_frames.tres + example .gd + metadata json
    ├── ai/
    │   └── config.py      AIConfig save/load (no AI behind it)
    └── ui/
        └── app.py         512-line Tkinter monolith: tabs + preview + playback
```

Đặc điểm: toàn bộ art được **vẽ procedural bằng Pillow primitives** (rectangle /
ellipse / point / line / polygon) theo chuỗi `if/else` trên string options;
compositing bằng `alpha_composite`; outline 1px pass ở cuối; UI truy cập trực
tiếp domain objects (không MVVM, không service layer).

## 2. Feature thật sự hoạt động vs scaffold

### Hoạt động thật (đã kiểm chứng bằng code + samples)
| Feature | Bằng chứng |
|---|---|
| Render nhân vật 32x46, 4 hướng Down/Up/Left/Right | `renderer.py` + sample package |
| Body tách equipment: 17 layer có tên (`effect_back, hair_back, weapon_back, left_leg, right_leg, body, cape, pants, boots, inner, outer, chest_armor, shoulder, gloves, belt, head_equipment, off_hand, accessory, custom_*, left_arm, right_arm, head, face, hair_front, weapon_front`) | `render_layers()` + `samples/.../layers/` |
| 13 equipment slots, custom PNG overlay per slot (resize NEAREST về 32x46) | `equipment_layers()`, `set_custom_overlay()` |
| Pose = field states (`body_dx/dy, head_dx/dy, left/right_arm, left/right_leg, hair_state, weapon_state, torso_tilt`) + override store per frame | `models.Pose`, `pose_editor.py`, `samples/pose_overrides.json` |
| Animation = frame list + FPS + loop qua playback UI | `poses.ANIMATIONS`, `app._tick()` |
| Onion skin (frame trước 22% alpha) | `app.refresh()` |
| Anchor per direction (8 điểm), Anchor Debug overlay màu | `anchors.py` + `samples/anchors.json` |
| Preview modes: Normal / Native 1x / Grayscale / Silhouette / Part Debug / Anchor Debug, scale 1x/2x/4x/10x NEAREST | `render(mode)`, `app.refresh()` |
| Game preview 360x640: bg + lưới 32px + sprite 4x + NPC silhouette 2x | `app.refresh()` |
| Art QA: palette count, visible bbox, value range, 4 warning thresholds (>24 màu, <90 contrast, >30w, >44h) | `art_qa.py` + `samples/art_qa.json` |
| Export: frame PNG per direction/anim, per-layer PNG, spritesheet per direction, package.json | `exporter.py` + sample package |
| Godot export: `sprite_frames.tres` (animation name `<anim>_<direction>`, loop=false cho death), example gdscript, metadata | `godot/exporter.py` + `samples/sample_package_v6/godot/` |
| Character JSON save/load; random body/equipment; class template áp equipment + animation | `models.py`, `catalog.CLASS_TEMPLATES` |

### Scaffold / vỏ rỗng
- **AI Config**: lưu/load JSON provider/model/key — không có AI nào chạy.
- **Asset Library**: import PNG + sidecar JSON `{id, slot, file, canvas}` — không
  tags/search/thumbnail/validation.
- **Validation**: 4 rule ad-hoc gắn class tu tiên (`validate()` trong UI).
- **SkillSpec**: chỉ metadata ghi vào package.json, không sinh ra gì.

## 3. File/logic nên tái sử dụng và cách migration

| Legacy | Giữ gìn | Migration về kiến trúc mới |
|---|---|---|
| `renderer.py` thuật toán layer + outline + debug modes | Toàn bộ logic compositing/outline/portrait các mode | Port 1:1 vào `PixelGameStudio.Rendering` (buffer RGBA thuần, không Pillow) làm **reference renderer** + nguồn golden-master; về lâu dài thay bằng asset-driven renderer, procedural chỉ còn placeholder |
| `catalog.py` palettes (skin 3 tone × base/shadow, hair 3 tone, skin_variant 4 tone, element colors) | Giá trị màu + triết lý 2-3 tone/material | Chuyển thành data palette mặc định của StyleProfile, không còn hard-code trong renderer |
| `poses.py` 30 poses + 11 animations | Thân_pose dạng state machine đơn giản | Trở thành **preset pose/animation library** dạng data (JSON), không hard-code; Pose → Animation → Behavior tách lớp |
| `pose_editor.PoseOverrideStore` | Khái niệm override field theo frame | Nucleus của Pose Editor + Animation timeline (frame = pose override) |
| `anchors.py` DEFAULT_ANCHORS | 8 anchor × 4 hướng | Anchor metadata data-driven trong RigDefinition; Anchor Debug giữ làm QA view |
| `art_qa.analyze_sprite` | 4 chỉ số + thresholds | `PixelGameStudio.Validation` — mở rộng thêm palette/bounds/missing-frame checks |
| `exporting.exporter` | Cấu trúc thư mục + spritesheet grid + metadata | `PixelGameStudio.Export`; spritesheet cần thêm frame map trong manifest |
| `godot/exporter` | Format .tres, tên animation, loop rule | `PixelGameStudio.Godot`; giữ tương thích tên `<anim>_<direction>` |
| `assets/library` | Sidecar JSON per asset | Mở rộng thành AssetDefinition (id/type/view/anchor/zIndex/tags) |
| CLASS_TEMPLATES / CLASS_ANIMATION_MAP | Ý tưởng "template áp nhanh" | Role data-driven (Phase 9), không hard-code class |
| ART_STYLE_RULES.md | Style spec v6 | Input cho StyleProfile Spec doc |

## 4. Hard-code / technical debt KHÔNG giữ

1. **Canvas 32x46 là global const** (`W,H=32,46` trong `renderer.py`, sidecar
   `canvas:[32,46]`, anchor ranges 0..31/0..45, UI spinbox bounds). → mới:
   `PixelProfile`/StyleProfile quyết định canvas; renderer nhận size.
2. **Chỉ top-down 4 hướng**; direction chỉ là string + `dx ∈ {-1,0,1}`. → mới:
   ViewProfile với mapping view→asset, hỗ trợ 4/8 hướng, side-view, chính diện.
3. **Fixed class tu tiên** (5 class) trộn lẫn: equipment style + animation riêng
   + validation rule. → mới: RoleDefinition data-driven.
4. **13 fixed equipment slots** (`EQUIPMENT_LIBRARY` + `SLOT_LABELS`). → mới:
   slot do project định nghĩa; equipment là asset có slot tag.
5. **Procedural if/else art** — mỗi item là một nhánh if vẽ rect. Không mở rộng
   được, không import asset production. → mới: asset-driven, procedural chỉ
   placeholder.
6. **Pose state enums string cố định** (arm: down/up/forward/back; weapon:
   6 state; hair: still/left/right). `torso_tilt` **có field nhưng không được
   render dùng** (dead field). → mới: PartGraph transform thật.
7. **UI monolith 512 dòng**: domain tạo trực tiếp trong UI, sync bằng tay
   vars ↔ spec, không tách ViewModel/service. → mới: MVVM, UI chỉ gọi service.
8. **Bug đã phát hiện (port trung thực, không sửa trong golden)**:
   - Cape được composite **sau body** dù comment "cape behind" → áo choàng che
     torso thay vì nằm sau;
   - `weapon_state` `ready/recover/back` render giống `idle` (chỉ slash/raise
     khác);
   - **hair_back bob không đồng nhất**: khối tóc chính (ellipse) và hai búi bob
     theo `body_dy`, nhưng dây tóc hai bên (Tóc dài/xõa/mái dài/Búi cao) vẽ ở
     y cố định 12 — model pose mới (part-granular) dịch cả part nên pixel dây
     tóc có thể lệch trên pose bob; đây là **thay đổi có chủ ý** (parity test
     mask vùng dây tóc, xem ComposerPoseParityTests);
   - Spritesheet exporter không ghi frame map (frame nào ở ô nào);
   - `outline()` hard-code 32x46 nên không tái dùng cho canvas khác;
   - Custom overlay luôn nằm trên mọi layer (kể cả weapon_front).
   Các bug này được **ghi nhận**, golden-master port theo hành vi thật để giữ
   pixel parity; phiên bản production sẽ quyết định riêng từng cái.
9. **Không có test nào** trong legacy. → mới: xUnit + golden-master pixel diff.

## 5. Phân tích style pixel

- **Tỷ lệ**: canvas 32x46 portrait; nhân vật chiếm ~27x44 visible (theo
  `samples/art_qa.json`); đầu ~16x15 px ≈ 1/3 chiều cao — tỷ lệ chibi 2.5-head.
- **Palette**: giới hạn chặt, 2-3 tone/material:
  - da: 3 màu da × (base, shadow);
  - tóc: 6 màu × (base, dark, light);
  - skin_variant/theme: 4 tone áo (Mặc định/Lạnh/Hỏa/U tối/Mộc);
  - equipment: màu hard-code per item, accent gold `(246,200,84)`;
  - outline duy nhất 1 màu `(22,16,30)` — tím than đậm, không đen tuyền.
- **Outline**: external 1px — chỉ pixel trong suốt kề pixel opacity > 0; không
  outline nội bộ giữa các layer.
- **Hair**: silhouette-first — dark tone phủ, light tone 1-2px highlight;
  sway ±1px chỉ ở hair_back theo `hair_state`.
- **Equipment**: đọc qua khối màu lớn + accent (đai ngọc, phù giấy), không
  micro-detail; custom overlay PNG cho art ngoài hệ thống.
- **Readability**: QA yêu cầu value range ≥ 90 và silhouette không chạm biên
  (>30w/>44h là warning); native 1x là tiêu chí nghiệm thu; preview chỉ
  NEAREST.

## 6. Mapping source cũ → module mới

Trạng thái: **✅ đã migration có test** / **⏳ kế hoạch** (phase trong ngoặc).

| Legacy module | Module mới | Trạng thái | Ghi chú |
|---|---|---|---|
| `rendering/renderer.py` | `PixelGameStudio.Rendering` (PixelBuffer, PixelDraw, PixelOps, PngCodec, `Procedural.LegacySpriteRenderer`) | ✅ Phase 0 | Port bit-exact, golden-master 478 fixtures (634 test) |
| `core/models.py` (CharacterSpec) | `PixelGameStudio.Domain` (CharacterEntity/Appearance) | ⏳ Phase 3 | Data-driven, bỏ fixed class |
| `core/models.py` (Pose, SkillSpec) | `PixelGameStudio.Animation` (Pose/AnimationDefinition), `PixelGameStudio.Behaviors` | ⏳ Phase 6-8 | Pose=state, Animation=chuỗi pose, Behavior=animation+item+markers |
| `core/catalog.py` (palettes) | StyleProfile defaults (Domain) | ✅ Phase 1 (một phần) | `PaletteProfile`/`ProjectStyleProfile` data-driven; palette v6 giữ trong `LegacyCatalog` làm reference |
| `core/catalog.py` (equipment slots/options) | `PixelGameStudio.Assets` + EquipmentProfile | ⏳ Phase 5 | Slot do project định nghĩa |
| `animation/poses.py` | `PixelGameStudio.Animation` (preset library dạng data) | ⏳ Phase 6/7 | Preset 30 poses giữ trong `LegacyPosePresets` |
| `editor/pose_editor.py` | `PixelGameStudio.Animation` (pose library/overrides) | ⏳ Phase 6 | |
| `editor/anchors.py` | `PixelGameStudio.Domain` (anchor metadata) | ⏳ Phase 3 | Port trong `LegacyAnchorStore` |
| `editor/art_qa.py` | `PixelGameStudio.Validation` (SpriteAnalyzer) | ✅ Phase 0 | Port kèm 112 test QA |
| `assets/library.py` | `PixelGameStudio.Assets` | ⏳ Phase 2 | Xem §10 cho bài học migration |
| `effects/library.py` | `LegacyCatalog.ElementColors` | ✅ Phase 0 (data) | Behavior effect markers ở Phase 8 |
| `exporting/exporter.py` | `PixelGameStudio.Export` | ⏳ Phase 12 | Xem §9 cho chi tiết format |
| `godot/exporter.py` | `PixelGameStudio.Godot` | ⏳ Phase 12 | Giữ tên animation `<anim>_<direction>` |
| `ai/config.py` | Không port ở V1 | ✅ quyết định | Không có giá trị production |
| `ui/app.py` | `PixelGameStudio.App` (Avalonia MVVM) | ⏳ Phase 11 | Phase 0-1 đã có preview + menu File |

## 7. Kết luận Phase 0

- Prototype v6 là **máy vẽ procedural + pipeline export tốt**, giá trị nhất ở:
  layer model, palettes, pose data, anchor data, QA metrics, Godot flow.
- Golden-master strategy đã chọn: giữ Pillow prototype làm nguồn fixture
  (Python 3.13 + Pillow 12.3.0 khớp 100% sample package) → C# renderer port
  theo đúng thuật toán Pillow C (`Draw.c`, `AlphaComposite.c`) → pixel-diff
  0 sai lệch. Chi tiết fixtures: `tests/PixelGameStudio.Rendering.Tests/Fixtures/`,
  generator: `tools/fixturegen/generate_fixtures.py`.

## 8. Hành vi Pillow đã pin trong quá trình port (sự thật pixel, không phỏng đoán)

Những phát hiện dưới đây là kết quả probe thực nghiệm + đọc source C Pillow
12.3.0 (`Draw.c`, `AlphaComposite.c`, `ImagingUtils.h`, `_imaging.c`) khi port
Phase 0, và được khóa vĩnh viễn bởi golden-master tests:

1. **Primitive draw = overwrite toàn phần.** `point/hline/rectangle/ellipse/
   line/polygon` với bất kỳ ink nào ghi đè cả 4 kênh RGBA của pixel đích
   (probe: vẽ ink a=128 lên pixel a=200 → ra a=128). Không blend, không trộn.
2. **Rectangle/ellipse dùng bbox inclusive** cả hai đầu (`rr(d,x,y,w,h)` của
   prototype vẽ tới `x+w-1`).
3. **Line width=1**: Bresenham `line32rgba` KHÔNG vẽ điểm cuối; wrapper
   `_draw_lines` vẽ thêm điểm cuối tường minh → tổng pixel = Bresenham + endpoint.
4. **Ellipse fill**: thuật toán midpoint 2 lần quarter (doubled grid) — đã port
   nguyên state machine `quarter_state`/`ellipse_state`; bbox 1x1 (a=b=0, fill
   width=0) **không vẽ pixel nào**.
5. **Polygon fill** (`polygon_generic`, nhánh RGBA): edge table + even-odd
   scanline, `ROUND_UP/ROUND_DOWN` quanh 0, corner-fix logic, `draw_horizontal_lines`
   cho horizontal edges; float 32-bit, `roundf` = round half **away from zero**
   (khác MathF.Round mặc định).
6. **alpha_composite** (AlphaComposite.c, integer 7-bit precision):
   `src.a==0 → copy dst`; ngược lại `blend = dst.a*(255-src.a)`,
   `outa255 = src.a*255 + blend`, `coef1 = src.a*255*255*128/outa255` (chia nguyên),
   `out.rgb = SHIFTFORDIV255(src.rgb*coef1 + dst.rgb*coef2 + 0x4000) >> 7`,
   `out.a = SHIFTFORDIV255(outa255 + 0x80)`. Lưu ý bẫy đã gặp: rounding offset
   RGB là `0x80 << 7 = 0x4000` (không phải 0x8000).
7. **Grayscale** (convert "L" từ RGB): `L = (R*19595 + G*38470 + B*7471 + 0x8000) >> 16`
   (macro L24), alpha giữ nguyên.
8. **NEAREST resize**: dst(x,y) sample src tại `floor((x+0.5)*srcSize/dstSize)`.
9. **Outline của prototype** chỉ là pass thuần integer 4-neighborhood (không phụ
   thuộc Pillow) — port trực tiếp.

Bất kỳ thay đổi nào của Render về các hành vi này phải regenerate fixtures +
cập nhật mục này.

## 9. Sample package — cấu trúc export chi tiết (input cho Phase 12)

Đo đạc thực tế trên `samples/sample_package_v6/`:

- **animations/{Direction}/{anim}/{pose}.png**: 4 hướng × 4 animation (idle 2,
  walk 4, attack 4, class_sword_dash 3); tên file = tên pose, không phải index.
- **spritesheet_{down|left|right|up}.png**: 128×184 = lưới 4 cột × 4 hàng ô
  32×46. `cols = max(frame count)`, `rows = số animation` — **không có frame map**
  trong package.json; ô trống (idle chỉ 2 frame) là trong suốt, consumer phải tự
  suy từ `animations` metadata. Bài học: exporter mới cần spritesheet manifest
  (cell → frame).
- **layers/{Direction}/{anim}/{frame}/{17 layer}.png**: danh sách layer thay đổi
  theo loadout — 12 layer luôn có (effect_back, hair_back, weapon_back,
  left_leg, right_leg, body, left_arm, right_arm, head, face, hair_front,
  weapon_front) + layer equipment chỉ khi được mặc (mặc định: outer, pants,
  boots, belt, accessory → 17). Layer rỗng (weapon_back khi raise/slash) vẫn
  được xuất.
- **package.json**: `character` (toàn bộ CharacterSpec), `animations` (map
  anim → frame list), `directions`, `skill` (SkillSpec của UI), `ai_config`
  (scaffold UI). Skill/ai_config là state UI, không phải dữ liệu character —
  exporter mới tách rõ.
- **godot/sprite_frames.tres**: format 3, `load_steps = số frame + 1`
  (52 ext_resource cho 4×13 frame); id = `{n}_{Direction}_{anim}_{frame}`;
  path tương đối `../animations/...`; animation name = `<anim>_<direction
  lowercase>`; `duration 1.0` mỗi frame; `speed = fps (float)`; `loop` false
  chỉ cho `death`. Kèm `character_sprite_example.gd` (2 hàm play tiện ích),
  `godot_export.json`, `README_GODOT.md`.

## 10. Audit asset system của legacy (đầu vào trực tiếp Phase 2)

`assets/library.py` (10 dòng) + cơ chế custom overlay của renderer:

1. **Cấu trúc hiện tại**: thư mục `asset_library/<slot>/<id>.png` + sidecar JSON
   `{id, slot, file, canvas:[32,46]}`; `list_assets(slot)` bằng glob. Import chỉ
   copy file, **không validate nội dung PNG**.
2. **Custom overlay**: `set_custom_overlay(slot, path)` → renderer vẽ layer
   `custom_<slot>` **luôn trên cùng** (sau weapon_front); nếu kích thước ≠ 32×46
   thì **tự resize NEAREST** âm thầm. Hai quyết định nguy hiểm: z-order cứng và
   rescale foreign art không hỏi user.
3. **Slot cố định 13 slot** gắn `EQUIPMENT_LIBRARY` (danh sách item string) —
   asset không có identity riêng ngoài tên file.
4. **Không có tags/search/thumbnail/validation/reference** — library chỉ là
   file browser thô.

**Bài học bắt buộc cho Phase 2 (AssetDefinition/AssetReference/AssetLibrary):**
- sidecar JSON → nâng cấp thành `AssetDefinition` đầy đủ: `id, displayName, type,
  file (tương đối), tags[], view/directions[], anchor, zIndex` (đúng skill
  `.agents/skills/asset-system`); không hard-code slot.
- Import phải **validate canvas theo PixelProfile của project** và báo lỗi/cảnh
  báo thay vì tự resize; resize (nếu user chọn) chỉ NEAREST.
- zIndex thành metadata tường minh của asset/part — không còn "overlay luôn trên cùng".
- Asset lưu trong thư mục `assets/` của project (legacy gắn cứng với thư mục
  source app: `parents[2]/asset_library`).
- Mọi thao tác library phải test được không cần UI (legacy không có test nào).
