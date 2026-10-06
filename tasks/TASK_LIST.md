# TASK LIST — Pixel Game Studio V1

Quy ước: `[ ]` chưa làm, `[~]` đang làm, `[x]` hoàn thành và đã kiểm tra.

## Phase 0 — Audit prototype và nền tảng
- [x] Audit toàn bộ prototype v6.
- [x] Tạo `docs/LEGACY_AUDIT.md`.
- [x] Chọn công nghệ ứng dụng desktop (C#/.NET 8/Avalonia/MVVM theo `docs/TECH_STACK.md`).
- [x] Chốt kiến trúc module (12 thư viện + 4 test project theo cấu trúc TECH_STACK).
- [x] Tạo `PixelGameStudio.sln`.
- [x] Tạo Avalonia App (`src/PixelGameStudio.App`, MVVM + CommunityToolkit, đã kiểm chứng khởi động).
- [x] Tạo các class library Domain/Core/Rendering/Assets/Animation/Behaviors/ProjectSystem/Validation/Export/Godot/Infrastructure (Domain→Validation đã có nội dung Phase 0; các thư viện còn lại là scaffold cho phase sau).
- [x] Tạo xUnit test projects (4 project; Rendering.Tests chứa toàn bộ golden-master).
- [x] Tạo renderer pixel bitmap độc lập UI (`PixelBuffer`, `PixelDraw`, `PixelOps`, `PngCodec`, `Procedural.LegacySpriteRenderer` — không reference Avalonia).
- [x] Tạo golden-master/pixel-diff test so với prototype cho các primitive cốt lõi (478 fixtures: 324 ellipse, 14 line, 7 rect, 6 polygon, 7 outline, grayscale, 7 nearest-resize, 112 sprite full-render, 48 composite; generator `tools/fixturegen/generate_fixtures.py`).
- [x] Thiết lập build/test/lint (`scripts/build.ps1`, `.editorconfig`, `dotnet format` clean).

## Phase 1 — Project System
- [x] New/Open/Save Project (`ProjectStore` — thư mục project với `project.pgsproj`, ghi atomic, menu File trong App).
- [x] Game Profile (`GameProfile` — genre data-driven, 8 preset + tùy chỉnh).
- [x] View Profile (`ViewProfile` — TopDown4/TopDown8/Diagonal4/SideView2/Frontal/Custom, danh sách direction + default data-driven).
- [x] Pixel/Canvas Profile (`PixelProfile` — canvas WxH + pixel size là data, không hard-code).
- [x] Style Profile (`ProjectStyleProfile` — outline/light/shadow-highlight/detail/transparency/nearest-neighbor/proportions/tile/QA thresholds theo STYLE_PROFILE_SPEC).
- [x] Palette Profile (`PaletteProfile` — hex entries + MaxColors, không nhúng binary vào JSON).
- [x] Autosave/recovery (`ProjectStore.Autosave`/`TryRecover` — khôi phục khi main file mất hoặc autosave mới hơn; 35 test Domain+ProjectSystem chứng minh).

## Phase 2 — Asset Core
- [x] AssetDefinition/AssetReference (`Domain/Assets` — id/displayName/type/file/tags/views/anchor/zIndex; AssetRules chặn path traversal, id sai, view lạ; asset lớn hơn canvas project bị từ chối).
- [x] Asset Library (`AssetLibraryService` — import/delete/load/search; PNG nằm ở `<root>/assets/`, JSON chỉ giữ definition).
- [x] Import PNG (decode + normalize RGBA; sai canvas project → **từ chối**, chỉ resize khi `AllowResize` và luôn NEAREST — audit §10; re-encode chuẩn qua PngCodec).
- [x] Thumbnail cache (`assets/thumbs/<id>_<box>_<hash>.png`, invalidation theo content hash, integer nearest up-scale, nearest down-scale khi vượt box).
- [x] Tags/search/filter (search substring id/displayName, tags AND semantics, filter type; case-insensitive).
- [x] Asset validation (`AssetRules` + tích hợp `ProfileRules.Validate` + `ValidateOnDisk` kiểm file tồn tại/khớp canvas; duplicate id chặn ở project level).
- [x] UI: Browser trái với thumbnail + import + search/tag filter + xóa (viên gạch đầu layout editor).

## Phase 3 — Character + Rig
- [x] CharacterEntity (Id/Name/RigId/Appearance/Equipment/RoleId).
- [x] Character archetype → archetype = template data (RigTemplates); role data-driven ở Phase 9.
- [x] PartGraph/PartNode (id/parentId/anchorId/pivot/zIndex/assetRef/transform integer/states/tags; validate single-root/cycle).
- [x] RigDefinition (+AnchorPoint +EquipmentSlotDef; FindPart/ChildrenOf/PathToRoot).
- [x] Anchor/Pivot/Z-order metadata (16 anchor humanoid; anchor đính part + offset).
- [x] View mapping (PartAppearance.ViewAssets theo ViewProfile + ViewMirrors; equipment ViewAssets).
- [x] Humanoid template (RigTemplates.HumanoidTopDown4 — 10 part, 13 slot, z-order khớp legacy).
- [x] RigSpriteComposer asset-driven + golden test: compose == legacy render pixel-perfect 4 hướng.
- [x] Rig editor cơ bản (Rig workspace: danh sách part theo z + sửa DisplayName/ZIndex/Offset dX/dY qua Inspector — prompt 04).

## Phase 4 — Body / Face / Hair
- [x] Base body assets (StarterContentFactory: body/head/face/hair per view từ legacy layers — placeholder/template).
- [x] Body variants (3 dáng Tiêu chuẩn/Mảnh/Đậm × 4 view = state assets `body` trên torso; test pixel edge khác silhouette).
- [x] Face parts (part face + variants mắt 5 kiểu × miệng 3 kiểu làm state assets; view Up không mặt — fallback đúng).
- [x] Hair parts (hair_front/hair_back riêng + 10 kiểu tóc × 4 view × 2 part = state assets `hair`).
- [x] Direction/view variants (per-view assets 4 hướng; state variant thiếu view → fallback về view asset gốc).
- [x] Palette overrides (PartAppearance.PaletteOverride → composer recolor từng part).
- [x] Skin variants (AppearanceService.ApplySkinOverride — remap LegacyCatalog.SkinTones trên 7 part mang da, Clear để reset; UI combo Da).
- [x] UI: Character inspector — combos Body/Mắt/Miệng/Tóc/Da (options đọc từ state assets). **Test**: AppearanceVariantTests (5 case) + parity giữ xanh.

## Phase 5 — Equipment
- [x] EquipmentProfile → EquipmentSlotDef trên RigDefinition (slot là data của rig, không hard-code).
- [x] Slot Editor (UI: các ComboBox sinh từ rig.EquipmentSlots + assets tag `slot:<id>`; đổi slot render lại ngay).
- [x] EquipmentDefinition → EquippedItem (SlotId + AssetId + ViewAssets per direction).
- [x] Equip/unequip (UpdateEquipment; unequip = chọn "Không"; test equip bộ đồ khác == legacy render).
- [x] Custom slot (thêm EquipmentSlotDef mới vào rig là có ngay trong UI + validation).
- [x] Anchor binding (slot gắn AnchorId; composer đặt asset tại vị trí anchor).
- [x] Z-order (slot ZIndex riêng biệt 6..17 khớp legacy; stable sort).
- [x] Direction assets (ViewAssets per direction; test hero 4 hướng pixel-perfect).

## Phase 6 — Pose Editor
- [x] PoseDefinition (per-part PartPose: integer offsets/hidden/states; per-slot SlotPose: offsets/hidden/state/z-override; StateValues).
- [x] Chỉnh root/body/head (offsets integer theo part; composer áp trước khi composite).
- [x] Chỉnh tay (PartPose.States hand: down/up/forward/back → state variant assets).
- [x] Chỉnh chân (leg: neutral/forward/back → state variant assets).
- [x] Hair/held-item state (weapon raise/slash qua SlotPose.States + ZIndexOverride front; hair sway = data StateValues).
- [x] Save/duplicate pose (poses lưu trong project JSON; Clone deep-copy; presets 16 pose legacy).
- [x] Pose Library (project.Poses; PosePresets build từ data — test parity 22 case).

## Phase 7 — Animation Editor
- [x] AnimationDefinition (frames = pose refs, FPS, loop).
- [x] Timeline (UI strip frame dưới cửa sổ + frame hiện tại highlight).
- [x] Add/delete/duplicate/reorder/copy/paste frame (UI buttons trên timeline + commands có Checkpoint undo).
- [x] Frame duration/FPS/loop (DurationTicks per frame + Fps + Loop; DurationSeconds).
- [x] Play/pause (polling playback clock theo FPS; non-loop dừng ở frame cuối).
- [x] Onion skin (PixelOps.Fade mask 56 = legacy blend 22%; composite prev dưới frame hiện tại).
- [x] Direction variants (compose theo ViewProfile; parity 4 hướng đã có từ Phase 5).
- [x] Event markers (FrameMarker: Footstep/Sound/Hit/SpawnEffect/SpawnProjectile/Interaction/PickUp/Drop/Custom).

## Phase 8 — Behavior System
- [x] BehaviorDefinition (animation + held item + interaction anchor + markers + group).
- [x] Behavior Library (UI panel trái với danh sách + binding summary; BehaviorLibraryService).
- [x] Preset groups (66 preset data từ PRODUCT_OVERVIEW: 5 nhóm; preset = data, không hard-code UI).
- [x] Custom Behavior Editor (CreateCustom nhân bản → sửa; Delete bảo vệ preset).
- [x] Held-item binding (HeldItemSlotId/HeldItemAssetId + validate slot/asset tồn tại).
- [x] Interaction anchor (InteractionAnchorId + validate anchor trên rig).
- [x] Sound/effect/gameplay markers (BehaviorMarker Type/Name/FrameIndex + validate phạm vi).
- [x] Template import/export (BehaviorLibraryService.Export/ImportTemplate JSON; test round-trip).

## Phase 9 — Role System
- [x] RoleDefinition data-driven (Id/DisplayName/Group/StartingEquipment theo asset family/BehaviorIds/DefaultAnimationId/Tags; roles = project data, không có danh sách cứng trong code).
- [x] Custom role (RoleService.CreateCustom nhân bản + sửa; Delete).
- [x] Role behavior/equipment/animation templates (ApplyToCharacter: resolve per-view assets từ family, thay toàn bộ loadout "starting equipment", set RoleId; validate slot/family/behavior/animation).
- [x] Import cultivation templates as demo only (CultivationRoleTemplates — 5 role port từ legacy CLASS_TEMPLATES, tag "demo", không gì trong core phụ thuộc).
- [x] UI: Character inspector — Role combo + Áp dụng role + Cài role mẫu (demo).

## Phase 10 — Art QA
- [x] Native 1x / 2x / 4x / 10x (Scale combo, nearest-neighbor; native 1x là nghiệm thu trong QA).
- [x] Grayscale/Silhouette/Layer/Anchor debug (Mode combo; Layer qua ComposeLayers/Export).
- [x] Palette/contrast/bounds/alpha checks (SpriteAnalyzer + StyleProfile.MinValueRange + alpha rule).
- [x] Missing direction/frame checks (CharacterQaService.CheckCharacter — coverage view per part; animation frame→pose đã validate ở AnimationRules).
- [x] Pixel noise warning (MeasurePixelNoise — tỷ lệ pixel đơn lẻ không kề cùng màu; ngưỡng 0.25).
- [x] Game-scale preview (viewport 360×640, 4 nền, lưới 32px, sprite 4x + NPC silhouette 2x).

## Phase 11 — Editor UX (prompt 04)
- [x] Undo/Redo (`UndoRedoService` — memento snapshot qua ProjectStore JSON, cap 50, Checkpoint trước mutation; Ctrl+Z/Ctrl+Y; test round-trip + cap).
- [x] Dirty state (`IsDirty` + tiêu đề cửa sổ "●" + status indicator + đóng cửa sổ vẫn autosave giữ bản khôi phục).
- [x] Autosave/recovery (đã có từ Phase 1; VM `AutosaveNow` sẵn cho timer).
- [x] Asset/Entity Browser (trái: asset browser thumbnail + search/tag + Behavior Library; resizable qua GridSplitter).
- [x] Inspector (phải, 320px, context-sensitive theo workspace: Project/Character/Rig/Animation/Behavior/Validation/Export — DataTemplates per VM).
- [x] Timeline panel (dưới, workspace Animation: frame strip + markers + play/stop/step + onion skin).
- [x] Shortcut system (Ctrl+N/Z/Y, F5 play, ◀▶ step; menu File/Edit/Animation đầy đủ InputGesture).

## Phase 12 — Export
- [x] Frame/Pose/Animation/Direction (ExportService: compose theo view × animation frames).
- [x] Spritesheet (PixelPrimitives.SpritesheetPack + frame map trong manifest.json — sửa thiếu sót legacy audit §9).
- [x] Layers (composer ComposeLayers — layer full-canvas per part/slot, sort z; thư mục layers/{view}/{frame}/).
- [x] Character package + metadata (package.json: character/view/animation frames+markers/style/included — không trộn state UI như legacy).
- [x] Godot SpriteFrames + manifest + example integration (GodotExporter: .tres format 3, tên `<anim>_<direction>`, loop theo animation, gd + readme).
- [x] Exporter interface cho Unity tương lai (ExportService tách khỏi UI/domain, options object).

## Phase 13 — Documentation & Release
- [x] User docs (docs/USER_GUIDE.md — quy trình nhanh, bố cục, phím tắt, lưu/khôi phục, sự cố).
- [x] Project format docs (docs/PROJECT_FORMAT.md — schema v1 đầy đủ + quy tắc).
- [x] Character/Behavior docs (docs/CHARACTER_SYSTEM.md — rig→pose→animation→behavior→role + QA).
- [x] Export docs (docs/EXPORT_FORMAT.md — thư mục, manifest, package, Godot, layers).
- [x] Manual acceptance checklist (tasks/ACCEPTANCE_CHECKLIST.md — cập nhật theo hiện trạng + hạn chế đã biết).
- [x] Release build (dotnet publish -c Release → publish/PixelGameStudio.App.exe).
