# Kiến trúc mục tiêu

## Công nghệ triển khai

- C#
- .NET 8
- Avalonia UI
- MVVM
- Renderer bitmap độc lập UI
- System.Text.Json
- xUnit

Xem chi tiết tại `docs/TECH_STACK.md`.


Project
├── ProjectStyleProfile
├── GameProfile
├── ViewProfile
├── PixelProfile
├── PaletteProfile
├── AssetLibrary
├── SpriteEntities
├── Animations
├── Behaviors
├── Actions
└── ExportProfiles

## SpriteEntity
Khái niệm chung cho tài nguyên có thể hiển thị. V1 tập trung `CharacterEntity`. Các version sau thêm Creature, Prop, Environment, Terrain, Building, Effect.

## CharacterEntity
Tham chiếu: RigDefinition, PartGraph, Appearance, EquipmentProfile, RoleDefinition, BehaviorSet, AnimationSet, SkinSet.

## PartNode
`id, displayName, parentId, anchorId, pivot, zIndex, assetRef, transform, states, tags`.

## RigDefinition
Data-driven, không khóa vào humanoid. Humanoid chỉ là template.

## Pose / Animation / Behavior
- Pose = trạng thái rig tại một thời điểm.
- Animation = chuỗi frame/pose theo thời gian.
- Behavior = hành vi cấp cao, có thể ghép animation + held item + interaction anchor + effect + sound marker + gameplay event.

## Equipment
Slot phải data-driven và project có thể thêm/xóa slot.

## Asset-driven
Production renderer dùng PNG/asset + metadata. Procedural drawing prototype chỉ giữ cho placeholder/debug/template, không là kiến trúc art cuối.

## Nguyên tắc phụ thuộc
- Domain/Core không phụ thuộc UI.
- Renderer không chứa editor logic.
- Exporter không đổi project state.
- ProjectStyleProfile là nguồn quy chuẩn chung cho V1 và các version sau.
