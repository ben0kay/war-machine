# Building003 — Abandoned roadside fuel/service station

**Status:** Reusable procedural scene, now placed by WorldController in both the active top-down world and inherited FPS test. Source complete; local build/render/physics validation pending.

## Scene and silhouette

**Scene:** `res://WORLD/Content/Structures/Building003/Building003.tscn`

Main shop/service hall: about **22m × 12m**, **4.4m** walls, flat industrial roof, parapet and air conditioner. A tall **fuel island canopy** reaches forward of the main footprint, supported by four columns. Two detailed gas pumps, hoses/nozzles, service lane, price sign and weathered shop sign give a recognizable Earth-like roadside silhouette.

**Actual interior:** accessible front customer door **2.1m wide** and a separate open **rear service exit**. A large service shutter is deliberately closed/collidable, but an internal passage through the dividing wall connects the store and garage. The shop contains two shelving aisles, a counter/cash register and refrigerated cabinet; the garage has a workbench/tools. Shopfront and side window holes are physically open in the wall, with separate colliding panes. Main roof and pump canopy can both be hidden visually from the angled camera.

**Collision:** true static colliders on layer 1 for outside walls, glass, shutter, partition, fixed shelves/counter, workbench, canopy posts, pump bodies and roadside sign. Thin decorative floor and forecourt overlays remain non-colliding, with Terrain3D handling walkable ground. **Place on level ground with clearance forward of the +Z entrance**: the pump canopy extends about 19m from the building centre; do not intersect trees or robot waypoints.

## Source ownership

- `C/Building003Mesh.cs`: one-shot `[Tool]` C# generator, no per-frame logic.
- `Configuration/Building003Settings.cs`, `Configuration/Building003Master.tres`: adjustable size and muted colours.
- `Artwork/`: optional future imported models.
- `../Shared/C/StructureGeometry.cs`: shared Godot primitive mesh/collision construction.
- `Building003.tscn`: scene root with `ShowRoof` and existing `CameraOccluder` child.

`SetRoofVisible(false)` hides the flat roof and pump canopy for an interior view, but leaves collision in place. This prefab is a **visual/physics test**, not a fuel/refueling system. No dynamic lights, NPCs, loot, doors opening, interaction, stock simulation, or save logic.

## Active placement

WorldController instances this prefab on saved terrain in both WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Its offset is editable in WORLD/Configuration/WorldMaster.tres. Actual generated bounds plus approach margins exclude pines, including external porch/canopy geometry. Terrain is read-only and the parked legacy FPS world is unchanged. See WORLD/README.md for the shared layout.

Godot/.NET are unavailable here; compile, grounding, interior access, collider and camera checks remain for local validation.

## Roof-only camera fading

The former whole-building proxy is replaced with two independent roof camera-only section proxies: Generated/RoofAndCanopy/StationRoof and /PumpCanopy. Standing below the pump canopy fades its roof/fascia, leaving pumps, hoses, supports, signs and forecourt solid. Shop roof fading leaves shelves, counter and tools solid. StationShell walls use individual camera-only bounds proxies: only wall pieces obstructing the camera-to-player corridor fade, while other walls remain solid. Both roofs restore through existing camera obstruction timing/opacity settings when clear. No new ticking script or changes to collision, zombie LOS, interior simulation or FPS. SetRoofVisible still controls the parent RoofAndCanopy group. Source checks passed; visual/load validation pending locally.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.
