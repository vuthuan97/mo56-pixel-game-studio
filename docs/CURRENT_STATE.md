# Trạng thái hiện tại

> 2026-10-07 — ReferenceGrid renderer v3 and starter-art refresh are complete. Body/limb layers are separated from equipment, directions and appearance variants render distinct art, build sliders affect the composed silhouette, preview BGRA packing is corrected, and unsaved preview modes use the same renderer path. See `docs/CHARACTER_RENDERER_REPAIR.md`.

> MO56 layout upgrade (audit 2026-10-08): **55/57** mục checklist gốc đã được xác minh; **2** mục còn mở. Inspector routing đã tách khỏi MainViewModel; timeline nằm trong tab Hoạt ảnh bên phải và dùng DurationTicks/FPS; thanh nhân vật dùng chung bốn tab; Bộ khung chia nhóm Thân/Khuôn mặt/Tóc/Tay/Chân; preview có nền Ô caro/Sáng/Tối, phát/frame và Art QA thu gọn; checkbox Hành động lưu trước khi tạo; output action có ID và binding/fingerprint riêng theo nhân vật, hỗ trợ giữ bản chỉnh tay/tạo version mới/thay thế có chủ đích; footer Tạo cố định; tab Xuất chọn nhiều nhân vật/animation/hướng và xuất package riêng. Custom slot/undo hai nhân vật đã được kiểm thử, Hoạt ảnh có tạo/nhân bản và lưu behavior template vào catalog Hành động; catalog có 42 template với các nhóm chuyển động thật. Chưa nghiệm thu UI native 1280×800/1024×640. Xem `tasks/LAYOUT_ACTIONS_UPGRADE_TASK_LIST.md`; không xem Phase A–G là đã hoàn thành.

> Cập nhật 2026-10-03 — **hoàn thành TASK_LIST V1: Phase 0-13 đầy đủ** (prompts 00-05 đối chiếu trọn).

> Cập nhật bổ sung: workflow Character Studio hiện không còn gắn Role/môn phái; các type role cũ chỉ giữ lại để đọc project legacy, không được cài tự động, không xuất ra package và không xuất hiện trong UI.

## Phase 10 + 12-13 (hoàn thành — chốt V1)

- **Phase 10 Art QA**: `CharacterQaService` — coverage view per part, compose thử + contrast theo StyleProfile.MinValueRange, **pixel noise** (tỷ lệ pixel đơn lẻ, ngưỡng 0.25), alpha rule theo StyleProfile; UI **Game-scale preview** (360×640, 4 nền, lưới 32px, sprite 4x, NPC silhouette 2x — port legacy game preview); Validation workspace bao gồm character QA.
- **Phase 12 Export**: composer `ComposeLayers` (layer full-canvas per part/slot, sort z); ExportService + **layers export** (layers/{view}/{frame}/) + **package.json** (character/animation frames+markers/style/included) + **GodotExporter** (sprite_frames.tres format 3 tên `<anim>_<direction>`, loop theo animation, example .gd + README); options IncludeFrames/Spritesheet/Layers/Godot.
- **Phase 13 Docs & Release**: `docs/USER_GUIDE.md`, `docs/PROJECT_FORMAT.md` (schema v1), `docs/CHARACTER_SYSTEM.md`, `docs/EXPORT_FORMAT.md`; `tasks/ACCEPTANCE_CHECKLIST.md` cập nhật đầy đủ kèm bằng chứng test; release build `dotnet publish -c Release` → `publish/PixelGameStudio.App.exe` (verified).
- **Editor UX bổ sung**: autosave timer 30s tự động khi dirty; Behavior inspector có combo Nhóm (sửa group); VM delegate appearance/skin qua `AppearanceService` (Domain) — test được không qua UI.

## Rà soát prompts + Phase 4 (lượt này)

- **Prompt 03 (đóng gap UI)**: timeline giờ có **frame ops** (Thêm/Copy/Paste/Xóa/đổi chỗ ◀▶ — có Checkpoint undo); **Pose Editor** trong Animation inspector (chọn part → dX/dY/Ẩn/States `key=value` → áp vào pose của frame hiện tại, semantics chia sẻ pose như legacy overrides); **marker editor** (thêm/xóa marker theo type+value); **Nhân bản behavior tùy chỉnh** button. 10/10 yêu cầu prompt 03 đủ.
- **Prompt 05 (đóng gap)**: primitive **`PixelOps.ApplyMask`** (nhân alpha target×mask, DIV255) + 2 test — đủ 10/10 mục checklist.
- **Prompt 04**: đối chiếu 7 mục — đã đủ từ lượt trước, không gap.
- **Phase 4 (hoàn thiện đúng nghĩa)**: variants thật sự — **Body 3 dáng × 4 view**, **Mắt 5 × Miệng 3 × 3 view**, **Tóc 10 kiểu × 4 view × 2 part** sinh làm state assets từ legacy renderer; **Skin tone override** qua `AppearanceService.ApplySkinOverride` (remap skin palette 7 part, Clear để reset); Character inspector có 5 combo (options đọc từ state assets). **Sửa lỗi composer quan trọng**: state variant thiếu art cho view → fallback về view asset gốc (trước đây vẽ nhầm art view khác lên view Up).

## Phase 9 — Role System (hoàn thành)

- **Domain/Role**: `RoleDefinition` (StartingEquipment = `RoleEquipmentBinding(SlotId, AssetFamily)` — family id không kèm view suffix; BehaviorIds; DefaultAnimationId; Tags), `RoleService` (InstallTemplates idempotent, CreateCustom clone, Delete, `ApplyToCharacter` — **thay toàn bộ loadout** theo semantics "starting equipment", resolve per-view assets `{family}.{view-slug}` từ project.Assets, skip + warning khi family trống), `RoleRules` (validate slot/family/behavior/animation, hook vào ProfileRules).
- **CultivationRoleTemplates**: 5 role (Kiếm/Đan/Phù/Thể/Du hiệp) port từ legacy CLASS_TEMPLATES — **demo data only**, tag "demo", gắn starter asset families + behaviors preset + signature animation; không gì trong core phụ thuộc.
- **UI**: Character inspector — Role combo (theo project.Roles) + "Áp dụng role" + "Cài role mẫu (demo)"; áp xong render lại qua composer.
- **Test mới (6)**: install idempotent, apply per-view + RoleId, render khác nhau giữa 2 role, family thiếu → skip + warning, custom clone + validation bắt refs hỏng, round-trip giữ roles.

## Phase 11 — Editor UX + Export core (prompt 04, hoàn thành)

- **Layout studio**: Menu (File/Edit/Animation) + Toolbar (workspace bar 8 tab + project ops + Undo/Redo) + Browser trái (assets + behaviors, resizable GridSplitter) + Preview giữa (direction/pose/mode/scale) + **Inspector phải context-sensitive** (ContentControl + DataTemplates theo 7 inspector VM) + Timeline dưới (workspace Animation) + Status bar (dirty indicator).
- **Workspace tabs**: Project (Game/View/Pixel/Palette profile editing) / Character (tên/notes) / Rig (part list + Z/offset editing) / Equipment (Slot Editor data-driven) / Animation (FPS/loop/duration) / Behavior (bindings: animation/held slot/interaction anchor) / Validation (chạy project.Validate + ValidateOnDisk) / Export.
- **Undo/Redo**: `UndoRedoService` memento JSON snapshot (cap 50); Checkpoint trước mọi mutation (equipment/import/delete/preset/part edit/behavior edit); Ctrl+Z/Y + nút toolbar.
- **Dirty state**: `IsDirty` → tiêu đề "●" + status; Save/Open/New reset; autosave giữ bản khôi phục khi đóng.
- **Export core (một phần Phase 12)**: `PixelGameStudio.Export.ExportService` — compose từng frame theo animation × direction, xuất PNG frames + spritesheet (SpritesheetPack) + **manifest.json có frame map** (sửa thiếu sót legacy audit §9). Export workspace tab chạy với folder picker.
- **Bài học XAML**: 3 lỗi AVLN2000 bị incremental build nuốt — StringFormat bắt đầu `{0}` phải escape `{}{0}`, ListBox dùng `Styles` không phải `ItemContainerStyle`, ItemsControl không có Spacing. `-t:Rebuild -v:n` mới lộ ra.

## Phase 6-8 — Pose/Animation/Behavior (hoàn thành, prompt 03)

- **Pose**: `PoseDefinition` đầy đủ — `PartPose` (offset nguyên, hidden, States) cho 9 part + `SlotPose` (offset/hidden/state/`ZIndexOverride`) cho trang bị. `PosePresets` port 16 pose legacy (idle/walk/attack/hurt/cast/death) dạng data thuần.
- **Composer nâng cấp**: state variant assets (`PartAppearance.StateAssets` + `EquippedItem.StateAssets` dạng `StateAssetVariant`), pose states merge (pose thắng mặc định), weapon raise/slash → art front + z 23 (đúng legacy), equipment offset theo slot pose (weapon bỏ qua body_dy — đúng legacy).
- **Starter content mở rộng**: thêm state variants cho tay (4 trạng thái × 4 hướng), chân (3 × 4), weapon front raise/slash (4 hướng) — render từ legacy renderer với pose tương ứng.
- **Animation**: `AnimationDefinition` (frames = pose refs + DurationTicks + FrameMarker; FPS; Loop). `AnimationPresets` 6 preset (idle/walk/attack/hurt/cast/death — death không loop, footstep/hit/sound markers).
- **PixelOps.Fade**: onion skin primitive (mask 56 = legacy blend 22%, DIV255 rounding).
- **Behavior**: `BehaviorDefinition` + `BehaviorMarker` + `BehaviorGroups`; `BehaviorPresets` 66 preset theo 5 nhóm PRODUCT_OVERVIEW; `PixelGameStudio.Behaviors.BehaviorLibraryService` (InstallPresets idempotent, CreateCustom nhân bản, Delete bảo vệ preset, Export/ImportTemplate JSON).
- **Presets trả deep-copy** mỗi lần truy cập — editor mutate không đụng template data (bug shared-state đã sửa kèm test ổn định 3 lần chạy).
- **UI**: Timeline strip dưới cửa sổ (chọn animation, play/pause/step, onion skin toggle, frame cells với markers, nút cài preset); Behavior Library panel trái (lọc nhóm, binding summary, Export/Import template); playback clock polling theo FPS của animation.
- **Golden parity mới (22 test)**: compose(pose preset) == legacy render cho 16 pose × Down + 4 case Left/Right/Up — bao gồm bob, head offset, arm/leg states, weapon raise/slash front. **Deviation có chủ ý đã ghi nhận**: legacy giữ dây tóc hair_back cố định khi bob (vẽ per-element), model mới dịch cả part → mask vùng dây tóc trong test + ghi audit §4.

## Phase 3-5 — Character + Rig + Equipment + Body/Face/Hair (hoàn thành)

- **Domain/Character**: `PartNode` (id/parentId/anchorId/pivot/zIndex/assetRef/PartTransform integer/states/tags), `RigDefinition` (parts/anchors/equipment slots + PathToRoot + validate single-root/anti-cycle/parent-exists), `AnchorPoint` (record, đính part + offset), `CharacterEntity` (Id/Name/RigId/Appearance/Equipment/RoleId), `PartAppearance` (AssetRef + ViewAssets per direction + ViewMirrors + PaletteOverride + States), `EquippedItem` (SlotId/AssetId/ViewAssets), `CharacterRules` (validate cross-references: rig tồn tại, part tồn tại, asset tồn tại, view ∈ ViewProfile, slot 1 item, AllowedTags).
- **Domain/Animation**: `PoseDefinition` (nucleus Phase 6 — PartPose offset/hidden per part + StateValues).
- **Domain/Templates**: `RigTemplates.HumanoidTopDown4` — 10 part (torso/head/face/hair_front/hair_back/2 tay/2 chân/weapon), 16 anchor, 13 slot với z-order 1..22 khớp chính xác thứ tự composite của legacy.
- **Assets/Composition**: `RigSpriteComposer` — production composer asset-driven: resolve asset per (part, view), palette swap, MirrorX, z-order stable sort, đặt equipment tại anchor, outline theo StyleProfile. `StarterContentFactory` — tạo ~190 asset humanoid (12 part-layer + full equipment catalog × 4 view) từ legacy procedural renderer (placeholder/template đúng rule), dựng rig + hero mặc định.
- **Rendering primitives (hoàn tất checklist prompt 05)**: `PixelPrimitives.PaletteSwap` (exact RGBA match), `PixelPrimitives.SpritesheetPack` (grid + frame map — sửa thiếu sót legacy audit §9), `PixelOps.MirrorX`. `PixelOps.Composite` giờ clip phần tràn canvas (Pillow paste semantics).
- **UI**: Character workspace — chọn character, nút "Tạo nội dung mẫu", Slot Editor data-driven (ComboBox sinh từ rig slots × assets tag `slot:<id>`), preview render qua composer theo direction + scale.
- **Test mới (40)**: golden parity compose == legacy render **pixel-perfect từng view Down/Up/Left/Right** + unequipped + mixed equipment; palette override chỉ recolor đúng part; mirror Left == MirrorX(Right); pose offset nguyên; hidden parts giữ equipment; broken ref → CompositionException; round-trip rigs/characters; PaletteSwap/SpritesheetPack.

## Phase 2 — Asset Core (hoàn thành)

- **Domain/Assets**: `AssetDefinition` (id, displayName, type — open string với 4 hằng, file tương đối, canvas, tags, views, `AssetAnchor`, `ZIndex`, ImportedAtUtc, notes), `AssetReference` (record, dùng từ Phase 3), `AssetTypes`. `AssetRules.Validate` chặn: id sai định dạng (>64 ký tự/ký tự lạ), path traversal (`..`, `\`, absolute), non-PNG, anchor ngoài canvas, asset lớn hơn canvas project, tag/view trùng, view không có trong ViewProfile. `ProfileRules.Validate` giờ bao gồm assets + chặn duplicate id.
- **PixelGameStudio.Assets**: `AssetLibraryService` — Import (decode PNG → validate canvas theo PixelProfile; sai canvas **từ chối**, chỉ resize khi `AllowResize` và luôn NEAREST; re-encode RGBA chuẩn; id slug từ tên file; unique id case-insensitive), Delete (definition + PNG + thumbnails), LoadPixels, Search (substring + tags AND + type), `ValidateOnDisk` (file tồn tại + khớp canvas). `ThumbnailCache` — `assets/thumbs/<id>_<box>_<contenthash>.png`, tự invalidate theo content hash + prune bản cũ; integer nearest up-scale, nearest down-scale khi vượt box.
- **App**: Browser trái (270px) — nút Import PNG (file picker), ô search, ô tag filter (phẩy), ListBox thumbnail 48px + tên + meta (type • canvas • z), nút xóa; giữa là Preview. Import yêu cầu project đã lưu (root known); import xong tự save project.
- **Sửa hệ thống**: `PngCodec.Decode(Stream)` giờ chiếm quyền dispose stream; mọi call site encode dùng `using` — không còn khóa file.

## Phase 1 — Project System (hoàn thành)

- **Domain**: `Project` (schemaVersion 1, ProjectId, timestamps) với 5 profile data-driven:
  - `GameProfile` — genre (8 preset + tùy chỉnh, chỉ là metadata, không có logic rẽ nhánh theo genre).
  - `ViewProfile` — perspective + danh sách direction + default direction; factory TopDown4/TopDown8/Diagonal4/SideView2/Frontal/Custom.
  - `PixelProfile` — CanvasWidth/Height/PixelSize là data (32x46 chỉ còn là dữ liệu template `ClassicTopDown46`, không phải hằng số hệ thống).
  - `PaletteProfile` — PaletteEntry (Name + Hex "#RRGGBBAA") + MaxColors.
  - `ProjectStyleProfile` — outline (style/thickness/màu), light direction, shadow/highlight levels, detail density, transparency rules, nearest-neighbor-only (bắt buộc bật, validate error nếu tắt), proportions nhân vật, TileSizePx, MinValueRange cho Art QA.
  - `ProfileRules.Validate` trả danh sách cảnh báo tiếng Việt; 3 template: `ClassicTopDown46`, `SideScroller48x32`, `Sandbox8Direction64`.
- **ProjectSystem**: `ProjectStore` — New/Open/Save (thư mục: `project.pgsproj` JSON camelCase indented + UTF-8 không escape tiếng Việt; `assets/`; `autosave/`), ghi atomic (temp+move), từ chối schema mới hơn, bỏ qua field lạ (forward-compat); `Autosave`/`TryRecover` — khôi phục khi main file mất hoặc autosave mới hơn; Save xong sẽ dọn autosave cũ.
- **App**: menu File (New / Open… / Save As… / Recover Autosave…) + ô tên project (two-way) + summary profile (genre • perspective • canvas • palette). Chưa dựng full editor layout (thuộc Phase 11).
- Core thêm `Rgba32.TryParseHex/ToHex`.

## Nền tảng đã triển khai (Phase 0 — hoàn thành)

Solution `PixelGameStudio.sln` — C# / .NET 8 / Avalonia 11.3 / MVVM / xUnit, đúng theo `docs/TECH_STACK.md`:

```text
src/
├── PixelGameStudio.App             Avalonia app MVVM (menu File + project + preview)
├── PixelGameStudio.Core            Rgba32 (+ hex parse/format)
├── PixelGameStudio.Rendering       PixelBuffer, PixelDraw, PixelOps (+MirrorX), PixelPrimitives (PaletteSwap, SpritesheetPack), PngCodec, Procedural/Legacy*
├── PixelGameStudio.Validation      SpriteAnalyzer (Art QA port)
├── PixelGameStudio.Domain          Project + profiles + Assets + Character (rig/parts/appearance) + PoseDefinition + templates + rules
├── PixelGameStudio.ProjectSystem   ProjectPaths, ProjectStore (open/save/autosave/recover)
├── (Domain/Templates)              PosePresets 16 + AnimationPresets 6 + BehaviorPresets 66 (deep-copy)
├── PixelGameStudio.Assets          AssetLibraryService + ThumbnailCache + Composition (RigSpriteComposer, StarterContentFactory)
├── PixelGameStudio.Animation       PoseDefinition/PartPose/SlotPose, AnimationDefinition/FrameMarker, AnimationRules
├── PixelGameStudio.Behaviors       BehaviorLibraryService (presets/custom/template import-export)
├── PixelGameStudio.Export          ExportService (frames + spritesheet + manifest)
├── PixelGameStudio.Godot           (scaffold — Phase 12)
└── PixelGameStudio.Infrastructure  (scaffold — theo nhu cầu)
tests/
├── PixelGameStudio.Rendering.Tests 638 test (golden-master + primitives)
├── PixelGameStudio.Domain.Tests    37 test (profiles/rules/templates/hex/asset rules)
├── PixelGameStudio.ProjectSystem.Tests 75 test (store/autosave/assets/composer/pose parity/animation/behavior/undo-redo/export)
└── PixelGameStudio.Export.Tests    smoke scaffold (logic test nằm ở ProjectSystem.Tests)
```

Phụ thuộc: Rendering → Core; Validation → Core+Rendering; App → Core+Rendering+Validation. Renderer và Validation test được không cần Avalonia.

## Kết quả kỹ thuật then chốt

1. **Golden-master pixel parity đạt 100%**: renderer C# tái tạo bit-exact prototype Pillow v6 trên 478 fixtures — 324 ellipse (grid w/h 1..18), 14 line, 7 rect, 6 polygon (kể cả even-odd concave), 7 outline mask, grayscale L24, 7 nearest-resize, **112 sprite full-render** (spec/pose/direction/mode variants) và 48 case alpha_composite. Generator: `tools/fixturegen/generate_fixtures.py` (chạy lại bằng Python 3.13 + Pillow 12.3.0; đã xác nhận Pillow 12 render khớp 100% sample package v6).
2. **`PixelGameStudio.Rendering.Procedural.LegacySpriteRenderer`** là port trung thực (kể cả bug đã ghi nhận trong `docs/LEGACY_AUDIT.md` §4) — dùng làm reference renderer + placeholder; các thuật toán Pillow đã port bit-exact: midpoint ellipse (Draw.c), Bresenham + last point (draw_lines), polygon scanline nhánh RGBA (polygon_generic + draw_horizontal_lines), AlphaComposite.c integer precision, ITU-R 601-2 grayscale (L24), NEAREST resize `floor((dst+0.5)*src/dst)`.
3. **`PngCodec`** encode/decode PNG không phụ thuộc UI (color types 0/2/3/4/6, bit depths 1/2/4/8/16, 5 filter, không hỗ trợ Adam7 interlace).
4. **Avalonia App** hiển thị sprite render (Direction/Pose/Mode/Scale qua nearest-neighbor); adapter `PixelBuffer → WriteableBitmap` nằm trong App, renderer không biết gì về UI.
5. Build/test/lint: `scripts/build.ps1 [-Test] [-Format]`, `.editorconfig`; `dotnet format` sạch; **768/768 test pass** (640 rendering + 37 domain + 90 project-system + 1 smoke). — Release publish verified.

## Định dạng project file (schema v1)

Thư mục project: `project.pgsproj` (JSON, camelCase, indent, UTF-8 giữ tiếng Việt) + `assets/` (Phase 2) + `autosave/`. JSON không chứa binary ảnh. Ví dụ field chính: `schemaVersion, projectId, name, createdAtUtc, modifiedAtUtc, game{genre,notes}, view{perspective,directions,defaultDirection}, pixels{canvasWidth,canvasHeight,pixelSize}, palette{name,maxColors,colors[{name,hex}]}, style{...}`.

## Source tham khảo
- Audit đầy đủ: `docs/LEGACY_AUDIT.md` (kiến trúc, feature thật vs scaffold, debt, style analysis, mapping module có trạng thái migration). **Đã refresh sau Phase 0+1** với: §8 hành vi Pillow đã pin khi port (9 mục — là sự thật pixel khóa bởi golden tests), §9 cấu trúc sample package chi tiết (input Phase 12), §10 audit asset system + bài học bắt buộc cho Phase 2.
- Prototype v6 giữ nguyên ở `legacy-reference/pixel_sprite_studio_v6/` (read-only); toàn vẹn bảo vệ bởi `tools/legacy_checksums.txt` (984 file, tái sinh bằng `python tools/fixturegen/make_legacy_manifest.py`; zip khớp thư mục trừ `__pycache__`).
- Fixtures: `tests/PixelGameStudio.Rendering.Tests/Fixtures/` (không sửa tay; regenerate bằng script).

## Đã giữ lại từ prototype (dạng port/golden)
- layer composition 17 layer, outline 1px external, preview modes (Normal/Grayscale/Silhouette/Part Debug/Anchor Debug), anchor per-direction, Art QA metrics, pose/animation presets, palette data — tất cả đã có test chứng minh.

## Không được bê vào production architecture (đã cô lập trong Procedural)
- hard-code 32x46, chỉ top-down 4 hướng, fixed class/slot, procedural if/else art, UI gắn domain.

## Việc tiếp theo (sau V1 — không chặn release)
1. V2+ theo VERSION_ROADMAP: Xưởng vật phẩm/đạo cụ (V2), môi trường (V3)…
2. Cải tiến đã biết: tổ hợp eye×mouth variants; weapon state assets cho mọi item (hiện chỉ starter Kiếm); role appearance presets; undo cho mọi micro-operation.
3. Unity exporter khi có yêu cầu (interface đã sẵn sàng).
