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
| Export | Export | Current V1 export supports characters; multi-character selection is a later phase. |
| Asset browser | Contextual Character browser | Visible for Equipment and Actions instead of occupying the whole application shell. |

## Migration rules

- The five top-level ids are `Project`, `Character`, `Library`, `Background`, and `Export`.
- `Library` and `Background` are explicit placeholders, not false success states.
- Character subtabs are `Frame`, `Equipment`, `Animation`, and `Actions`.
- Existing project JSON, legacy workspace state, Role fields and asset files remain readable. Role is not returned to the UI.
- Existing animation and behavior templates are not promoted to “generated actions” until they have real pose/frame output and compatibility checks.

## Phase B implementation notes

The current shell keeps the established inspector DataTemplates while the physical View split is deferred. The left asset/behavior browser collapses outside the two character contexts that need it, and the preview/configuration columns use a 2*:3* split with minimum widths suitable for the requested window sizes.

## Phase C-F implementation notes

- Frame now owns per-character build data (`Gender`, `BodyType`, head/torso/arm/leg/foot dimensions). The composer consumes bounded integer deltas for parts and equipment anchors, preserving safe native-pixel placement.
- Equipment selectors are generated from the selected rig and asset tags, display cached thumbnails, support remove/empty, and can assign the selected contextual-browser asset after tag validation.
- Animation timeline is selectable, exposes current-frame duration ticks, and uses clone-on-write when a shared pose is edited. Non-loop playback stops on the last frame.
- Actions use `ActionTemplateCatalog`: availability reasons are explicit, generation creates dedicated poses and animation frames, cancellation is atomic, progress is reportable, and generated action ids are stored on the character.
- Character package/manifest export includes build data, selected actions, generated animation bindings and SHA-256 source fingerprints. Two-character isolation is covered by automated tests.
