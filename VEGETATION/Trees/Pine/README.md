# Pine trees

Reusable pines moved from the parked FPS world's content folder. `PinePlacement.cs` owns seeded terrain placement and MultiMesh batches; `PineSettings.cs` / `Configuration/PineMaster.tres` own shared defaults. **C/PineMeshBuilder.cs** constructs the original procedural mesh. Future imported models belong in **Artwork/**. Leaf wind shader lives under Shaders/. There is no duplicate FPS version.

## Overhead defaults

Three shared shape variants, **12–18m** height with seeded per-instance scale/yaw; **7 branch tiers**, **320 leaf budget**, generated far LOD; **96m** ground placement range, 32m chunks, density **0.55**, spacing **20m**, spawn clearing **12m**, shadows off. Trunks keep their corrected closed surfaces, double thickness and seeded 0.85–1.25 variation. Needle tufts/branches remain volumetric, with lighter geometry than the previous 12 tiers/900 budget. Gentle shared wind remains; fine flutter defaults to zero and skips its extra sine. No per-tree/leaf scripts.

WORLD/WorldController supplies its saved terrain, player anchor, region size and spawn. Only crossing a chunk boundary loads/evicts deterministic nearby batches; standing still has no C# update loop. Native frustum/range culling and generated LOD do the drawing work. Projection/distance changes adjust existing native visibility ranges to account for the elevated camera without regenerating meshes. Shared wind changes update one foliage material. Terrain height changes trigger one deferred resample. OFF/density zero frees batches; subscriptions and noise resources clean up on unload. There is no measured FPS claim.

The active tuning panel has pine enabled, density, size multiplier and wind. Density/size apply on slider release to avoid repeated mesh rebuilds during dragging; Copy settings includes them. Defaults are edited in the shared PineMaster, runtime sliders edit a copy. No terrain rebake required.

Visual trees only: **no trunk collision, damage/chopping, canopy fade or navigation obstacles** in this test. Tree crowns can obscure characters; the initial clearing reduces this around spawn. Finite draw range remains visible at extreme zoom/low angles. Robot patrol/zombie gameplay is unchanged. The old validation captures stay archived under WORLD/Content/Vegetation/Trees/Validation.

## PT0 — Overhead reuse

Implemented, **Awaiting Local Validation**. Scope authorized: reuse pines in the active world under VEGETATION/Trees/Pine with lighter shared settings and live tuning. Source/settings/path/scene/UID and whitespace checks only; Godot/.NET unavailable here. Local checks: build, F6 WORLD/World.tscn, inspect solid trunks/green crowns and height variation, walk across chunks, try both camera projections/zoom, adjust density/size/wind, OFF/zero/reactivation, paste Copy settings, unload without errors and assess appearance/FPS. No subsequent vegetation/gameplay phase is authorized.

## Camera obstruction fading

CameraOcclusion now queries cached canopy/chunk bounds and fades only the blocking tree through MultiMesh custom data in both LODs. Bark stays solid; foliage uses opaque ordered stipple coverage at 50% by default. Geometry rebuilds/evictions invalidate the camera. No per-tree physics collider or process node is added. See CAMERA/Occlusion/README.md; local shader/visual checks pending.
