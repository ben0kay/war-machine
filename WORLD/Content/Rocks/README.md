# Rocks

Procedural rocks reused in both the active world and parked TestWorld. The active profile has solid collision; the legacy profile remains decorative. `C/` owns the generator; `Configuration/RockMaster.tres` owns artwork and placement defaults; `Artwork/` is reserved for replacement models. Shared placement and geometry helpers live in WORLD/Content/Shared/C. F9 settings require Apply/Rebuild. No terrain writes or per-instance processing. Awaiting user visual/FPS validation.

## Stone surface

`Configuration/StoneMaterial.tres` / `Shaders/Stone.gdshader` reuse the existing CC0 ambientCG Rock023 albedo/height and normal/roughness textures from Terrain3D's demo (see demo/assets/textures/asset_licenses.txt). Demo assets are unchanged. Triplanar world-space sampling needs no mesh UV unwrap. The shader adds normal-map surface detail, roughness variation, dampness and upward-facing patchy moss. Shape remains the existing low-poly boulder; textures do not change its outline.

RockMaster owns the material and texture scale, normal strength, moss amount and wetness. F9 updates these four parameters immediately without a rebuild. Each scatter category duplicates its shared material once, protecting source resources; imported mesh materials remain untouched. Existing batching, culling and shadow defaults remain. No CPU processing callbacks or per-rock material instances are added. GPU cost increases by six texture samples per visible fragment (three albedo and three packed normal/roughness samples).

RockFace now orients normals away from the boulder's interior, fixing dark tops caused by inward normals. Other content generators are unaffected. Source/diff/reference checks completed. Godot/.NET tooling unavailable in this session, so compilation, shader compilation, visual acceptance and FPS verification are deferred to the user.

## 3D top-down / inherited FPS reuse

**The meshes/material are compatible with both views**, but **current scatter placement is wired to the parked `WORLD/testworld.tscn`** through `ExplorationController` and `WorldConfig`. Merely adding a `ContentScatter` node to active `WORLD/World.tscn` is **not enough**. The active scene uses `PlayerController` and `WorldSettings`; adapt the lifecycle/height sampling without copying the mesh generator.

For the Codex integration handoff (not yet implemented), see [TOPDOWN_INTEGRATION.md](TOPDOWN_INTEGRATION.md). The generated `MultiMeshInstance3D` rocks currently **have no gameplay collision**, so use them as scatter decoration unless the integration adds *limited separate* static colliders for larger boulders and cover.

## Shape/material variation

Rocks now generate nine **deliberately distinct** low-poly mesh profiles: small loose stone, fieldstone, angular block, long low stone, broken upright, broad slab, tall angular stone, large boulder and wide outcrop. Four irregular height rings with faceted shoulders/crowns avoid every rock being a pointed cone. Meshes are generated once per variant and reuse the existing CC0 triplanar material. Existing random yaw/scale placement remains. Triplanar sampling uses each instance's local coordinate frame plus a position-derived offset, so texture orientation follows the stone rotation instead of every rock sharing world-aligned strata. Moss remains oriented by world up. Material remains shared, with no per-rock CPU animation.

## Active world rocks and cliff test

WorldController initializes the existing ContentScatter with the active player, reusing all nine RockGenerator silhouettes/materials. Runtime batches appear under WorldActorsRoot/PersistentPropsRoot/Rocks in both top-down and inherited FPS views. Configuration/GameplayRockMaster.tres is the separate active profile: density 0.35, spacing 12m, range 96m, size 1.2, collision enabled on terrain/solid layer 1. Existing player/zombie/rifle masks already include that layer. Parked RockMaster retains decorative defaults.

Static collision uses one cached convex hull per shared variant and one StaticBody3D per occupied chunk, with per-instance shapes matching mesh transforms. No per-rock controller or tick. Nearby chunk crossing creates/evicts batches and collision together; rebuild/OFF removes collision immediately before deferred freeing. Convex hulls approximate crevices/flat concavities conservatively. Small rock slopes can be walked over; large/steep boulder faces block movement.

Building/porch/forecourt/car and cliff clearings are supplied before placement. Candidate centers reject rectangles expanded by the largest scaled mesh radius, preventing boulder edges intruding on footprints. Initial zombie starts, player spawn and a conservative robot-route corridor are also excluded. Pine placement retains its existing scenery exclusions and receives the cliff/path clearings. These masks apply on initial placement, streaming and explicit rebuilds. No terrain edits or rebake.

The separate mesh cliff/ramp has been removed. WORLD/Generation/GameplayMountain.cs now sculpts actual native Terrain3D heightmaps at startup, south of spawn: softly domed summit, irregular steep cliffs and a broad curved climbing route. The native terrain material and collision follow the new landform. WorldMaster keeps the Cliff* tuning fields; see Generation/README.md for dimensions, startup behavior and local checks. Only the route is cleared of grass/trees/rocks; vegetation can grow naturally on the summit and shoulders. No saved terrain files are written and no rebake is required.

Checks: source/resource/setting bindings, exclusion ordering and conservative radius, collision lifecycle, Terrain3D approach/side profile and whitespace. No Godot/.NET available: build, collision/grounding, visual slope/join and streaming checks remain local. F6 WORLD/World.tscn, walk south up the ramp, explore the top, step off an edge and attempt the side walls; bump rocks, inspect building/path clearings, walk away/back. Brief FPS check uses the same shared layout. No rebake needed.
