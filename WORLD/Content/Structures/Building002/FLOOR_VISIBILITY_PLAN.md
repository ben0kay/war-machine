# Building002 — floor-aware camera visibility handoff for Codex

> **Status: FL0–FL3 source implementation complete; local Godot validation pending.** Geometry and metadata are already authored in `Building002Mesh.cs`: ground floor, collidable upper floor, open stairwell, visual stairs, separate walkable ramp collision and floor tags. Automatic level changes/fade logic now live in Shared/BuildingFloorVisibility.cs. Keep existing roof, stair and player physics intact.

## Goal

In angled top-down play, make the *current occupied storey* readable. When the player is on **Level 0**, upper storey meshes/roof may fade partly so the ground level is easy to see. When the player walks upstairs to **Level 1**, restore the upper storey interior and keep the occupied floor readable by fading relevant roof or camera-facing wall pieces. Leave the ground floor structurally solid and preserve all physics. Outside the building, restore normal visibility. FPS should not be forced to hide the floor under the player.

### Important terminology

- `Level_0` is the **ground floor** (local elevation ~0).
- `Level_1` is the **upper/second storey** (local elevation `Building002Settings.WallHeight`, currently 4.0m).
- `Stairs_0_to_1` is a shared connector with a smooth walkable **collision ramp**, not owned as a barrier by either level. `Level_1/Visuals/FloorDeck` consists of four disconnected rectangles so a genuine stairwell hole remains.
- This uses **local building-space elevations**. Don't compare world Y against 4.0 globally—building placement follows Terrain3D and may be rotated or raised.

## Existing generated tree and discovery

```text
Building002 (Building002Mesh, existing CameraOccluder sibling)
  Generated
    Level_0 [meta floor_level=0, floor_elevation=0] (group building_floor_0)
      Visuals/{Exterior,TimberAndProps,Rooms,ShowcaseExterior,ShowcaseInterior}
      Collision
    Level_1 [meta floor_level=1, floor_elevation=4.0] (group building_floor_1)
      Visuals/{RoofVisuals,FloorDeck,ExteriorWalls,Furnishings,Partitions}
      Collision
    Stairs_0_to_1 [meta connects_floor_from=0, connects_floor_to=1; lower_elevation=0; upper_elevation=4.0]
      Visuals
      Collision
```

Floor metadata is set at generator runtime. `Building002Mesh.SetFloorOpacity(floor,opacity)` is an **available visual-only API** for manual/future testing, not yet bound to player motion. It never changes collision or levels. `Building002Mesh.SetRoofVisible()` remains available.

## Proposed implementation (separate approved phases)

| Phase | Work |
| --- | --- |
| **FL0** | Inspect generated floor hierarchy/solid collision, verify stair ramp alignment and stairwell clearance in Godot. Stabilize metadata/path contract, no fade gameplay. |
| **FL1** | Optional Building002-local or shared floor occupancy component: detect player enter/exit footprint and current floor using the inverse building transform and sampled foot position. Use hysteresis near stair landings (0.2–0.4m) and a clear transitional **OnStairs** state to prevent bouncing between floors. No per-frame full-world scans. Consider a couple of Area3D trigger volumes or player position-change signal; whichever is simpler. |
| **FL2** | Top-down floor-aware fade: outside => upper+ground normal; Level 0 => ground opaque / upper partial fade (e.g. 0.15–0.35 opacity) including ceiling/floor visual only; Level 1 => upper interior opaque, fade/hide roof or camera-facing upper walls as needed. Keep ground-floor visuals intact. Stair state blends predictably. Add configurable opacity and transition duration and restore on exit/despawn. |
| **FL3** | Coordinate with existing `CAMERA/Occlusion/CameraOccluder.cs` and `CAMERA/Occlusion/CameraOcclusion.cs`, which currently snapshot all prefab meshes and overwrite per-mesh transparency to fade the WHOLE building. Avoid competing transparency writers. Preferred: one composed visual opacity driver (camera obstruction factor × level visibility factor), or per-level mesh fade proxies managed by the same owner, *without* changing physics or creating extra whole-building collider proxies. |
| **FL4** | Optional DEBUG/F9 floor state readout and short checks for climbing/descending, indoor/outdoor, shadows, crouch LOS and cover. Perception's geometric visibility is unchanged by the camera's cosmetic fades. |

## Technical and gameplay safeguards

1. **Never disable collision to make a floor transparent.** Standing on an upper deck and peeking through a faded roof must still work. The collision-only `StaticBody3D` nodes are already separate by floor, plus a dedicated stair ramp/rails body.
2. Floor visibility is **presentation, not enemy perception**. Zombies and robots must still treat opaque walls/ceilings as solid even if the top-down camera renders them translucent. Link to `ENTITIES/Shared/Sensing/PERCEPTION_IMPLEMENTATION_PLAN.md` for shared physical LOS conventions.
3. Don't make transparency globally shared `Material` state; use instance-level mesh visibility/transparency or well-defined proxy composition so one building's fade does not affect every lodge instance.
4. The upper floor may have collidable glazing. For sight raycasts, glass must be classified separately from opaque blockers; this is **not** solved by visually fading the upstairs mesh.
5. The upper stairwell is an actual gap. If hiding Level 1 entirely, the staircase still needs visible geometry for the player to understand ascent. Don't fade the stair meshes by default.
6. Watch for the `CameraOccluder` whole-building fade compatibility. Its current implementation caches `MeshInstance3D.Transparency` once; naive `SetFloorOpacity` writes may be overwritten on the next occlusion update. Solve compositing centrally during FL3 before declaring auto fades production-ready.
7. Preserve the single prefab shared by `WORLD/World.tscn` and inherited `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`. Prefer auto fade only for the angled top-down camera; FPS can use existing manual roof toggle if needed.
8. React only on entering a building footprint, position/stance/vertical movement, camera-change events and transition animations. Idle scene should settle; no per-structure unbounded `_Process`.
9. Don't change the player controller, zombie intelligence, shared sensor APIs or building placement offsets as a side effect. Existing forest lodge front (+Z), solid entrance, partitions and foot corridor stay usable.

## Quick acceptance tests

- From ground level, hide/fade upper visuals enough to see player and first-floor layout.
- Start ascending; don't pop levels at stair midpoint. Stepping over the top correctly changes to Level 1.
- Upstairs decking remains solid throughout opacity changes; descend without falling through the opening.
- From upstairs, show furnishings and windows while fading roof/high walls as necessary.
- Outside or after unload, restore all visual opacities to normal.
- Verify camera occlusion transitions don't unintentionally restore the whole building when floors should remain faded.
- FPS view isn't incorrectly forced to hide upper walls or floors.
- Performance: idle does no repeated ray/scene scans; only event/tween transition frames wake.

**Codex handoff:** Read root `README.md`, `CodePreferences.md`, `WORLD/Content/Structures/Building002/README.md`, `Building002Mesh.cs`, `CAMERA/Occlusion/CameraOccluder.cs`, `CameraOcclusion.cs` and this plan. Begin with **FL0–FL1** only when authorized. Commit/push one focused change to `implement-td0-td1`; keep validation brief per project preferences.

## Implemented first pass

FL0 metadata inspected in source; Godot stair/collision verification pending. FL1 local-space player movement occupancy with Outside/Ground/OnStairs/Upper and footprint/landing hysteresis. FL2 configurable upper-content/roof presentation with landing blend and settled-idle sleep. FL3 disjoint section CameraOccluder owners compose authored × presentation × obstruction, excluding occupied furniture/decks from obstruction; lower walls restore upstairs. Static geometry/collision/LOS unchanged. FPS skips integration. FL4 debug UI remains optional/unimplemented; CurrentFloor can be inspected remotely. Runtime visual acceptance pending; see Building002 README. No interior simulation/culling policy introduced.

Owner refinement: occupied-floor walls fade only where individual wall-mesh proxies intersect the existing camera-to-player corridor. Entering a floor does not force all its walls transparent. Roof/upper-storey presentation remains occupancy-based.

Outside-house obstruction fallback: BuildingFloorVisibility now registers as an explicit camera occlusion provider/target. If the camera-to-player corridor intersects its cached whole-house bounds while the player is outside, the entire lodge (including deck, stairs, furnishings and trim) fades using CameraMaster's obstruction opacity/duration. Leaving the obstruction restores it. Indoors that factor is ignored, preserving occupied-floor furniture and stair readability. Each disjoint mesh owner multiplies the outside factor with existing presentation/camera factors; there is no second transparency writer. The check joins the existing event-driven camera cadence (no extra ray, physics proxy, timer or frame loop); bounds are collected once and endpoints transformed to current building space. This fallback applies to the lodge only: petrol station retains roof/wall-only fading, solid pumps and contents. Source checks only; local Godot build/visual validation pending.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.
