# MO56 Layout / Actions Upgrade - completed task list

Source prompt: `prompts/NANGCAP_DIEUCHINH/MO56_LAYOUT_ACTIONS_UPGRADE.md`.
[x] means implemented and verified by build/tests/docs.

## Phase A - audit and migration

- [x] Repository rules, architecture/style/current-state docs and MO56 prompt reviewed.
- [x] Baseline and post-change test/build evidence recorded.
- [x] Workspace mapping, preset policy and migration policy documented.
- [x] Existing Role data remains readable but is not exposed in the UI.

## Phase B - shell and Character workspace

- [x] Top-level workspaces are Project, Character, Library, Background and Export.
- [x] Library and Background are honest future-feature placeholders.
- [x] Character sections are Frame, Equipment, Animation and Actions.
- [x] Timeline is scoped to Character / Animation.
- [x] Contextual asset/behavior browser is visible for Equipment and Actions.
- [x] Character create, rename, select, duplicate and delete flows are available.
- [x] Character sections use physical Avalonia Views.
- [x] 40/60 layout, minimum sizes and 1280x800 / 1024x640 acceptance contract are documented.

## Phase C - Frame and Equipment

- [x] Frame owns identity, appearance, rig advanced controls and build parameters.
- [x] Gender, body type, head/torso/arm/leg/foot dimensions persist per character.
- [x] Integer-step sliders drive numeric build parameters and display current values.
- [x] Renderer applies bounded build deltas to parts and equipment anchors.
- [x] Equipment uses data-driven rig slots, thumbnails, tags, remove/empty and contextual assignment.
- [x] Two-character appearance/build/equipment/export isolation is tested.

## Phase D - Animation

- [x] Animation section owns timeline editing and direct frame selection.
- [x] Current-frame duration ticks are editable.
- [x] Shared poses use clone-on-write when edited.
- [x] Non-loop playback stops on the last frame.
- [x] Undo checkpoints cover animation mutations.

## Phase E - Actions

- [x] ActionTemplateCatalog provides data-driven templates and availability reasons.
- [x] Generator creates dedicated poses and animations; unsupported actions are never aliased to idle/walk.
- [x] Rig slot/anchor compatibility is validated.
- [x] Generation supports progress, cancellation with atomic commit and undo.
- [x] UI exposes a progress bar and Cancel action.

## Phase F - persistence and export

- [x] Selected action ids, generated animation bindings and source fingerprints persist per character/export package.
- [x] Legacy characters without new build/action fields migrate to safe defaults.
- [x] Autosave/recovery behavior remains covered by ProjectStore tests.
- [x] Multi-character export writes isolated packages.

## Phase G - verification and documentation

- [x] Full regression suite: 776 tests passed.
- [x] App build: 0 warnings, 0 errors.
- [x] UI acceptance contract is documented in `docs/MO56_UI_ACCEPTANCE.md`; native screenshot capture was unavailable because the Computer Use pipe is unavailable.
- [x] Current-state, MO56, user, character, project and export documentation updated.

