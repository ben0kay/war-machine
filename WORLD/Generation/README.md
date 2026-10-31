# Active gameplay generation

Run `BakeGameplayTerrain.tscn` only for a fresh active world with no saved terrain. `GameplayTerrainBake.cs` reads WORLD/Configuration/WorldMaster.tres, writes WORLD/Terrain/Gameplay, and refuses to overwrite. `GameplayTerrainStorage.cs` first reuses an existing user-local top-down bake by copying its native regions. WORLD/World.tscn loads saved terrain, then applies the authorized bounded GameplayMountain runtime sculpt before actors/foliage initialize. It does not regenerate or overwrite the saved world.

The notes below describe the **parked first-person wilderness generation**, whose config is now LegacyWorldMaster.tres.

# Explicit terrain authoring

`TerrainBake.cs` runs only through `BakeTerrain.tscn`. It creates deterministic FastNoiseLite relief plus a sinuous valley, imports a 1024m float heightmap, places seeded grass on gentle slopes, and saves native Terrain3D data/instances. No runtime dependency on this scene exists.

Do **not** run this to play: use `WORLD/testworld.tscn` instead. The first world is already baked.

For a new landscape: back up authored data, select a **new empty** `DataDirectory` in `WORLD/Configuration/LegacyWorldMaster.tres`, adjust seed/elevation/density, build, then run:

```sh
dotnet build
godot --headless --path . res://WORLD/Generation/BakeTerrain.tscn
```

Use your Godot .NET executable path in place of `godot`. Existing `.res` data causes exit 1 without changes. WE1 supports Size=1024 only; extending world size is later scope. LegacyWorldMaster's directory is authoritative at runtime; update testworld's Terrain3D `data_directory` too when changing it so the editor previews the same data. Settings affect new bakes, never automatically rewrite saved land. A new directory requires its own committed generated files.

This scene is an offline authoring utility, not another playable world. Generation does bounded one-shot work and then exits. No streaming or general generation framework is introduced.

## Pine landscape revision

LegacyWorldMaster selects a separately baked 2048m square landscape in `Terrain/PineLandscape` (four 1024m regions). Original `Terrain/Data` remains intact. ValleyWidth, MeadowBlend and MeadowRelief control the broad gently rolling floor; Elevation controls surrounding mountains. Baking still refuses any existing `.res` data: choose a new empty directory for revisions. GPU grass handles placement, so new bakes omit obsolete saved crossed-card instances.

## Native Terrain3D mountain preview

GameplayMountain.cs replaces the former separate CliffTestMesh/ramp. At startup it copies height images for regions overlapping the configured mountain, adds a continuous terrain elevation profile, updates native maps/height ranges and rebuilds Terrain3D collision once before player placement. Existing saved .res files, texture/control maps and the original small hill are retained; no save_directory or disk write is called. BakeGameplayTerrain remains a base-terrain authoring tool, so the preview is not applied twice by baking and playing. This preview is invoked once per world initialization, not as a repeated additive editor brush.

WorldMaster retains the Cliff* property names for compatibility. Defaults: center (-6,+76)m from spawn, width 44m, depth 36m, rise 12m, north approach length 38m and path width 10m. The approach bends gently and has rounded shoulders, the summit is softly domed, and steep irregular flanks blend into existing terrain. Broad FastNoiseLite surface variation and angular contour variation prevent a perfect ellipse. Native slope-aware terrain material textures the new surface. No independent rock slab, visible seam or mesh ramp remains.

Follow south from spawn: the lower approach starts about 20m south and winds toward the summit. Central route and a summit walking strip exclude grass/trees/rocks; the rest of the mountain uses normal terrain slope/height placement, allowing natural vegetation above. Buildings and their existing exclusions are preserved. Steep sides exceed player walkable slope limits so stepping off produces a real gravity fall to the lower native surface. No fall damage or navigation added. Climbing is ground walking, not a parkour system.

Clamps keep the approach broad and long relative to rise. A numerical profile check of defaults (before small noise/base relief) found maximum central-route slope about 29 degrees and a side face about 76 degrees, compared with the player's 46-degree walk limit. Shader/LOD/physics response on actual Terrain3D still requires local Godot verification.

No new node, timer, process callback or runtime streaming loop: bounded image edits and native collision rebuild happen at initialization, then the helper does no work. Existing terrain handles rendering/physics afterward. No terrain rebake required. Source/API/order/footprint/whitespace and numerical slope checks only; no Godot/.NET available here. Locally build/F6, inspect the south approach, summit/cliff texturing and joins, walk up, step off, and attempt climbing the steep side. Confirm buildings/robot and both views; verify normal terrain slope placement and path exclusions. Toggle CliffEnabled false to compare original saved terrain on a fresh run.

VillageTerrain.cs now performs bounded, runtime-only platform/road grading and three framing hills around VillageOriginXZ. It copies native height images, updates maps and leaves collision rebuilding to WorldController after grading; it never saves or rebakes terrain. VillageLayout performs <=2m full-footprint surveys and cut/fill checks before this pass, and support checks afterward. See WORLD/VILLAGE_IMPLEMENTATION_PLAN.md current handoff. Village mode bypasses the former isolated mountain showcase sculpt. Local native API/collision validation pending.


## VW1 — broad procedural landscape across the village and forests (revised 2026-10-31)

**Source-implemented; local .NET/Godot visual and traversal validation pending.** This **replaces**, rather than stacks on, VW1's previous seven manually positioned outskirts hills/dips. The same **single** `VillageTerrain.Apply` startup-height-image pass now combines broad seeded terrain noise (frequency `0.009`, amplitude `9m`) and gentler finer variation (frequency `0.028`, amplitude `1.5m`). Both positive and negative values are allowed: natural low ridges and basins appear wherever the saved ground was relatively flat. Actual rise/dip depends on the seeded noise and the original saved heightmap; these are noise weights, **not guaranteed minimum terrain heights**.

The procedural pass uses a **784×784m** rectangle centred on `WorldMaster.VillageOriginXZ` (with the current default, **X 160–944, Z 160–944**), covering the **eastern, western, and southeastern DF2 forests** and surrounding village. Added relief feathers to zero within 64m of the rectangle boundary; no new Terrain3D source data, new scene/controller or per-frame simulation was introduced. All existing saved `.res` terrain heightmaps remain untouched.

**Foundations and paths remain authoritative:** existing full-footprint `VillageLayout.Platforms` grading preserves the convenience-store asphalt, parking surfaces and all four buildings at `layout.Level`, with existing slope-blended road shoulders. `VillageLayout.Roads` are still graded flat and `VillageLayout.Patrol` masks the whole robot travel/foot-clearance corridor. The original **three VG2 framing hills** controlled by `VillageHillScale` remain as they were; only the seven *VW1* spot hills/basins were removed. `VillageOutskirtsEnabled` (true by default) and `VillageOutskirtsScale` (1 by default, allowed 0–2) still control **only** the new general relief. When OFF or 0, `VillageTerrain` takes the original smaller **480×500m** VG2 path and applies no additional broad noise; its old hills and grade remain.

**Ordering:** once per world startup, `VillageLayout.Prepare` measures the saved footprints and roads; `VillageTerrain.Apply` shapes the heightmap in RAM; WorldController updates native maps, rebuilds terrain collision **once**, then waits two physics ticks. Grass, DF2 forests, pines, rocks and other actors then sample the **finished Terrain3D surface**, with existing terrain-slope and structure/road exclusions. DF0 forests may reject a zone if the resulting slopes exceed `MaximumSlope` at too many samples; do not silently disable slope checks to force placement. The existing robot adapter samples every route marker Y after the final terrain/collision update, validates all path edges and foot-clearance checks and supplies a ground sampler to the real patrol.

**Cost:** the larger edit area processes about **2.56× as many height samples** at the same vertex spacing (784² / (480×500)) when enabled. This is a one-shot CPU/startup cost, not more world-frame work; vegetation batch/LOD, materials and geometry are unchanged. The old, smaller path remains when disabled. No FPS improvement or startup-time measurement is claimed.

**Local acceptance:** build C#, F6 FPS world, check the village asphalt/roads/building entrances remain flat and flush, then inspect all **three** forests for hills and shallow dips; no tree trunks float or bury themselves. Confirm DF0 approves zones (review Output), forest/grass/pines/rocks follow the updated surface, there are no cliffs or seams at X/Z 160/944, and the robot makes two complete ground-following circuits. Flip `VillageOutskirtsEnabled=false` on a **fresh** scene startup to compare old terrain, then restore true. Saved `WORLD/Terrain/Gameplay/*.res` files must remain unchanged. Compilation/Godot runtime rendering are pending.
