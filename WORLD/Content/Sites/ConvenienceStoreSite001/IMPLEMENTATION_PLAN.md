# PL — Modular convenience-store and parking-lot location

**Canonical plan for current development:** `implement-td0-td1`. **No writes to `main`.** Read root `README.md`, `CodePreferences.md`, this plan and the child-prefab READMEs before any phase. Work only on explicitly requested phases, make one commit per job, validate briefly and report what Godot still needs checking.

## Game direction and ownership

War Machine's **main game is 3D angled top-down survival**. The FPS scene is a secondary test. Compose a familiar roadside convenience store location with a believable asphalt car park, concrete walkway and curbs, 6–8 marked spaces, 2–4 parked cars, a short driveway/road stub and several streetlights (one can flicker later). The outdoor approach should be atmospheric at night, with warm lamps against cool fog. Use native reusable prefabs rather than one giant all-in-one mesh; avoid imported art/expensive shaders for the first pass.

This plan supersedes the **legacy**, default-branch `WORLD/PARKING_LOT_IMPLEMENTATION_PLAN.md`, which targets an older `TestWorld` and assumes building/visibility systems do not yet exist. Do not follow that plan's old `TestWorld` integration instructions. Treat the local files and current source as authoritative.

```text
WORLD/Content/
  Sites/ConvenienceStoreSite001/
    ConvenienceStoreSite001.tscn      composition of the children below (not yet assembled)
    Configuration/SiteMaster.tres
    IMPLEMENTATION_PLAN.md           this file
  Infrastructure/ParkingLots/ParkingLot001/
    ParkingLot001.tscn               reusable independently; no built-in store/lamps/cars
    C/ParkingLotMesh.cs
    Configuration/ParkingLotSettings.cs
    Configuration/ParkingLotMaster.tres
  Structures/ConvenienceStore001/
    ConvenienceStore001.tscn         existing shared StructureVisibility contract when implemented
  Placeables/Objects/Lighting/StreetLamp001/
    StreetLamp001.tscn               reusable lamp model; lighting behaviour separate
  Objects/Cars/Car001/              existing stationary car, never copy/relocate its source
```

The site owns **relative transforms, car/lamp selection, layout and site-wide world footprint**, not the children's geometry, building visibility, sounds, lighting behaviour or interactions. A parking lot is a fixed piece of **Infrastructure**, not a building; do **not** tag its entire surface with `StructureVisibility`. The store shell follows `Structures/BUILDING_AUTHORING_README.md`, including a single `StructureVisibility` child with `floor_level`, `structure_occupied_bounds` and disjoint visual roles. The store's top-down fading must not fade the asphalt or lamps.

## Status and phases

| Phase | Deliverable | State |
| --- | --- | --- |
| **PL0A — reusable parking foundation** | Independent asphalt/car-park scene; lane, 6–8 marked bays, sidewalk, curb/entrance gap, short driveway, static colliders, `Marker3D` bay/connection positions, typed Inspector master and docs. No cars/store/lamps/site assembly/world placement. | **Implemented; awaiting Godot validation** |
| **PL0B — reusable prefabs and site composition** | Independent store and StreetLamp001 now implemented (lamp includes opt-in steady spot/fill illumination). Parent site assembly with parking lot, cars, lamps/relative positions **still pending**. | **Partial: prefabs complete, assembly pending** |
| **PL1 — world integration** | Place assembled site on suitable saved Terrain3D near an accessible approach; validate height variation, foundations, total-site foliage/grass/rock clearings and building visibility registration. No terrain rebake. | Planned |
| **PL2 — nighttime atmosphere** | StreetLamp001 already supports static spot/fill lighting with night-friendly defaults; **site/Sky3D time-of-day control, store glow, limited active shadowed lamps and one optional flicker remain pending**. | Planned (fixture lighting ready) |
| **PL3 — optional view comparison** | Compare the **same site** in top-down and existing FPS debug camera with no new player/controller rewrite. Add fixed view markers only if requested. | Planned |
| **PL4 — polish** | Refine visual wear, sign, props, oil marks, roadside edge transitions, rain response and atmosphere based on in-game screenshots; don't build extra systems by default. | Planned |

### PL0A detailed contract — implemented (awaiting Godot validation)

- **Only change** the independent `Infrastructure/ParkingLots/ParkingLot001/` scene, mesh/settings/master/docs, plus documentation links/status. Leave `ConvenienceStoreSite001.tscn`, store and lamp placeholders untouched.
- Base grid: local Y up; **-Z is towards the future store**, **+Z faces the road/approach**. Center origin is the paving's centre. Suggested 40m × 28m main apron, 8 bays approximately 3.1 × 5.6m along the north storefront edge, central ~9m circulation aisle and ~8m-wide southward driveway stub.
- Sidewalk: north/back edge, slightly raised (roughly 0.14–0.18m) from the asphalt, with a small unobstructed central curb-access gap for future store entrance. Painted stall markings must be distinguishable from unmarked driving aisle; leave car placement to separate `Marker3D` children; never spawn a car from the lot generator.
- **Surface:** modestly elevated, thin, low-poly dark asphalt and a simple concrete sidewalk/curb; use shared static meshes and basic rough StandardMaterial3D with a few simple painted markings, no textures/normal maps/expensive decals. Collision should match solids only: walkable asphalt if intended, sidewalk and solid curb; paint should never block feet. Avoid Z-fighting with Terrain3D.
- **Settings:** typed `ParkingLotSettings : Resource` and `ParkingLotMaster.tres`; expose width/depth, stall count/width/length, sidewalk/curb parameters, driveway width/length, marking thickness and simple material colours. Bound and validate the footprint (don't allow bays/sidewalk to overrun paving); no mutable settings or rebuilding on every frame.
- **Markers:** `ParkingSpots/Bay_01..Bay_08` and named `Entrance`, `RoadConnection`, `Storefront` nodes use stable transforms and no gameplay. Keep them inside the reusable lot and independent of any car or lamp art.
- **Performance/lifecycle:** procedural generation **once at Ready**, no `_Process` or `_PhysicsProcess` while idle. Reuse basic box mesh/material resources. No terrain/vegetation searches or scene-tree queries on repeated frames. The scene is authored with no per-instance random variations to keep identical placement predictable.
- **Terrain limitation:** a flat single-root lot can't conform automatically to uneven Terrain3D. Later PL1 must sample multiple terrain heights/slope across the *entire site*, choose a reasonably flat spot, place surfaces just above ground, and reserve scatter/grass clearings. Do not add terrain editing/voxel cutting, automatic snap-to-slope per bay or world integration to PL0A.
- **Accept:** the *standalone* `ParkingLot001.tscn` loads, surfaces have no obvious visual holes, bays/curbs/markers fit at human scale, collider walkability works when tested with a player, settings are Inspector editable, and existing world remains unchanged.

### PL0B — assembly with real independent children

Use `ConvenienceStore001` (~12–16m width) as a small store with an actual front-door opening, physical walls and window panes (not painted-on rectangles), with inherited `StructureVisibility` / `StructureSettings`, like Building002/004. Use `StreetLamp001` as a reusable pole and luminaire, **lighting behaviour off until PL2**. Compose `ConvenienceStoreSite001` with one lot, one building, 2–4 instances of existing Car001, a few lamps and markers. Position children relative to the lot's painted-bay markers; do not duplicate meshes, instantiate cars into the lot class, or change existing Car001 paths.

### PL1 — placement and world ownership

`WORLD/WorldController.cs` and `WORLD/WorldSettings.cs` currently place four buildings and Car001 via a terrain-height-once `PlaceObject` helper. Adapt that existing composition logic, not historical `WORLD/TestWorld.cs`. Check slope at multiple corners: if the selected terrain is unsuitable, choose a different site or propose bounded terrain modification separately. Reserve foliage/rock/grass clearings from the *whole assembled site* (including driveway and streetlamps) to avoid vegetation through pavement. Store under a stable Structures subtree when required for its visibility component, or explicitly register its nested store if ownership requires a Sites parent. No camera/player/terrain bake changes.

### PL2 — nighttime light

Reuse `ENVIRONMENT/Lighting/` authored system and its existing muzzle-flash foundation, but campfire-style persistent life-cycle rather than muzzle-shot duration. Light only relevant nearby lamps; support one irregularly flickering fixture with no permanent per-lamp C# loop while off. Do not add another `WorldEnvironment` or change the Sky3D clock. Put physical lamp art under Placeables; its light logic under shared lighting. Test with low ground fog, balance shadows and check FPS.

## Handoff and regression list

Read current branch, inspect modified sources, perform a brief C# build if tools exist, otherwise say compile is **unverified**. For PL0A open `ParkingLot001.tscn` in Godot; check asphalt from above/ground level, 8 slots, clearance from markings, raised footpath and central curb gap, face direction and named marker transforms. Check source master values are actually read by the scene, `SetProcess(false)`, collision layers, and no accidental world placement. Other prefabs stay empty. Check changes against root CodePreferences. Record exact commit, files and deferred in-game/visual testing; user acceptance is separate from source review.

### PL0A delivery note

PL0A is now implemented in `Infrastructure/ParkingLots/ParkingLot001`, with typed `ParkingLotSettings.cs`, an authored master, a standalone `[Tool]` scene, one-time generated collidable asphalt/sidewalk/curb, non-colliding painted bays/road dashes, and stable slot/site markers. It deliberately does **not** instantiate anything in `ConvenienceStoreSite001`, modify WorldController, alter Terrain3D, or add store/lamp/car models. **PL0B is next** once the user approves the standalone scale. Local C# compile, Godot preview and physical walk testing remain pending.

### PL0B store-only checkpoint

`WORLD/Content/Structures/ConvenienceStore001/` now has typed configuration, its own flat-roof retail structure, large open side/front window gaps, an accessible till, four low display rows, and a separated stockroom with an inner walk-through and exterior service exit. It reuses the `StructureVisibility` contract. **Not assembled, not placed, no lamps, no car instances or glass.** Continue PL0B by building a separate lamp and composing the parent scene when explicitly requested. C# compile/render and physical walk validation pending.

### PL0B lamp-only checkpoint

`WORLD/Content/Placeables/Objects/Lighting/StreetLamp001/` is now a reusable standalone 5.8m lamp post with a warm directional downward `SpotLight3D`, weaker omnidirectional fill, small emissive LED panel and basic static collision. Typed `StreetLampSettings`/`StreetLampMaster.tres` expose the geometry, spot cone/range/energy, fill and fog. Native shadows are off by default. `SetLampEnabled(bool)` is an event-driven future day/night integration hook; there is no per-frame C# light logic and no flicker yet. The complete site scene is **still empty**; cars, store and lamps are not assembled or placed in the world. PL0B assembly, PL1 placement and PL2 site-level day/night/flicker remain future separately requested work. Godot compile/nighttime appearance/FPS checks pending.
