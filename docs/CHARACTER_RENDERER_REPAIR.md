# Character renderer repair (ReferenceGrid v3)

Updated 2026-10-07.

## What changed

`ReferenceGridSpriteRenderer` now draws an integer-coordinate character from a
canvas-fitted 32x46 design envelope. The body regions are explicit: head/neck,
torso/hips, shoulders/arms, legs and feet. The torso and limbs use the skin
palette; clothes, pants, shoes, armour and weapons remain equipment assets.

| Region | Before | Now |
| --- | --- | --- |
| Head/body | Face and clothing grids shared `OriginY = 8` and overlapped | Head ends before the shoulder/torso region |
| Arms | Fixed block positions with limited state changes | Shoulder-connected down/up/forward/back silhouettes per side |
| Legs | Each layer stamped both pants/legs and restored the old pixels | `left_leg` and `right_leg` contain one leg only; movement clears the old position |
| Direction | Most reference-grid art was front-facing | Down/Up/Left/Right have separate head, face, body, limb and hair silhouettes; Up has no face details |
| Canvas | Reference grid was always 32x46 | Geometry is fitted to `LegacySpriteSpec.CanvasWidth/CanvasHeight` |
| Preview | BGRA bitmap received RGBA-packed bytes | Premultiplied BGRA8888 is packed in the correct little-endian order |

`RigSpriteComposer` applies the per-character build sliders to the opaque
region of each body part with nearest-neighbor scaling, so Head/Torso/Arm/Leg
and Foot values affect the rendered silhouette. Rig Z/dX/dY fields also have
integer-step sliders in the Frame and Rig inspectors.

## Starter asset refresh

Generated PNG definitions carry `source:starter-template` and a provenance
note. Refresh replaces only assets with that provenance; imported or manually
edited definitions are preserved. Regenerated files invalidate their thumbnail
cache. `Project.Style.CharacterRendererVersion` is set to 3 only after a full
generation succeeds; `generateFiles: false` never marks the library current.

Projects with the new provenance metadata refresh automatically when opened and
their renderer version is below 3. Older projects remain readable and are not
silently overwritten because their old PNGs have no trustworthy generated
provenance; use the starter-content workflow after reviewing those assets, or
tag only the known generated definitions with `source:starter-template` before
refreshing. This protects imported/manual art by design.

## Verification

- `ReferenceGridRendererRegressionTests`: independent leg movement/no ghost,
  body and direction variants, Up face suppression, non-classic canvas sizing,
  build-profile silhouette changes, provenance/version rules, manual asset
  preservation and red/green/blue/half-alpha BGRA packing.
- ProjectSystem tests: 106 passed.
- Rendering tests and existing Legacy golden fixtures remain separate; the
  Legacy renderer was not replaced or modified.
- App build and the ProjectSystem test project build cleanly with zero warnings.

QA artifacts generated from the same renderer are in
`docs/artifacts/renderer-v3/`: bare native 1x, bare/equipped nearest 4x,
legacy-before comparison, walk/arm-up, body variants and preview modes.

The native Computer Use screenshot pipe was unavailable in this environment, so
visual acceptance is represented by the native-pixel regression checks rather
than a captured application screenshot.
