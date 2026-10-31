# Dense canopy forest — DF0/DF1/DF2

**Status:** DF0–DF2 implemented in source; awaiting local Godot/.NET and visual validation. DF3–DF5 remain planned.

The shared World and FPS scenes now support a **conditional dense, high-canopy broadleaf patch** east of the village. The forest is now **enabled in `Configuration/ForestMaster.tres`** for DF2 preview testing. DF0 surveys the 96 × 96m footprint around X/Z (667,567), including terrain, entry approach and existing village/robot/actor reservations. If the survey fails, **no replacement trees are created and the old pines stay**.

## DF2 gameplay integration

- `ForestPlacement.cs` builds an irregular, seeded layout from DF1's three tall canopy variants and smaller, simplified filler trees; mature/filler candidate spacings default to 7m/8m, with configurable densities and minimum trunk separations.
- Trees are grouped into **32m chunks**, with shared near/far Mesh templates and `MultiMeshInstance3D` batches. Each loaded chunk owns one static body with capsule colliders only at trunks (same physics layer 5 as pines).
- A winding **7m walking corridor** and two small clearings keep the zone traversable. Terrain3D height and slope are sampled once for candidate tree roots; no runtime terrain writing.
- New canopy foliage uses one shared opaque shader, which permits **top-down-only per-tree camera fading**. FPS displays full leaves. Near trees cast shadows by default; far trees do not. DF3 will refine canopy light blocking.
- The patch excludes old random pines **and random rocks only inside the successfully approved footprint**. Grass remains, ready for DF4 undergrowth integration.

## Configuration and test

`Configuration/ForestMaster.tres` now has `Enabled=true`. Optionally enable `ShowDebugOutline`, and restart F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Check Output for the DF0 approved/rejected message and, if approved, `DF2 forest: ... seeded trees`. Visit the east village outskirts; try walking along the corridor and shooting between collidable trunks. Repeat F6 `WORLD/World.tscn` to inspect top-down canopy fading. OFF + scene restart restores the original deterministic pine/rock distribution.

Source and settings are present; **compilation, visible density, tree collision and performance remain untested locally**. There are no per-tree update loops or standalone spawned tree scenes. [Detailed DF2 handoff](FOREST_IMPLEMENTATION_PLAN.md#df2--implementation-handoff-2026-10-29).

**STG1 shared tint only (2026-10-30):** `Shaders/CanopyFoliage.gdshader` additionally reads the project-global `wm_foliage_tint`/`wm_foliage_tint_strength`, preserving the existing DF2 near/far batching and per-tree top-down fade. No changes to canopy placement, zone activation or existing saved terrain. This keeps all leafy tree families visually adjustable alongside the new imported grass.

**DF2 white foliage screenshot correction (2026-10-30):** the DF2 MultiMesh now explicitly carries instance colour (white multiplier) alongside the existing top-down opacity stream. Both bark and canopy shaders preserve authored DF1 vertex colours, with bark/green fallback palette values taken from `CanopyTreeMaster.tres` whenever the renderer supplies uncoloured white vertices. This is an isolated visual fix; no forest density, tree mesh silhouette, collision, ground cover or LOD-distance changes. **Godot runtime acceptance pending:** build, F6 FPS, look up among the DF2 crowns and verify green leaves plus muted brown/grey bark, then check near/far and top-down fade.
