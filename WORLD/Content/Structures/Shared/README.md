# Reusable structure visibility and procedural geometry

**Current contract:** [../BUILDING_AUTHORING_README.md](../BUILDING_AUTHORING_README.md). Future AI-designed/imported/procedural buildings should follow that short guide. Older per-building integration notes below are historical; named WorldController visibility cases have been removed.

- StructureVisibility: prefab opt-in, deep-copied settings, deferred attach/refresh/rebuild/unload.
- StructureSettings: common inherited exports on each building master.
- StructureLayout: startup metadata reader/helpers, any increasing floor count and disjoint visual roles.
- BuildingFloorVisibility: generic feet-height/volume occupancy, upper-deck/contents restoration, roof/wall rules and transition-only processing.
- BuildingObstructionVisibility: cached outside bounds and coherent whole-building target.
- C/StructureGeometry: mesh construction only.

World only registers the structures subtree with CameraOcclusion. Existing prefabs are discovered once; later Ready/Exit events attach/remove components without scans. Context is scoped to that subtree and skipped by FPS. Invalid/missing occupancy/master logs a warning and leaves visibility inactive. Explicit rebuild releases subscriptions/owners before replacing art. No new collision/AI/navigation/editor-generator feature. Source checks only; local .NET/Godot build, inherited export compatibility and runtime visual/streaming checks pending.

# Shared procedural structure geometry

Reusable **Godot C# mesh-construction helpers** for Building002 and Building003. They do not alter Building001 or take over world placement/AI. See `C/StructureGeometry.cs`.

Each prefab remains individually configurable and has its own roof visibility and StaticBody3D. Shared helpers create simple boxes, cylinders, wall spans and low-poly pitched roofs; no processing callbacks.

BuildingFloorVisibility.cs (outside C/) owns top-down lodge occupancy and presentation. It receives explicit prefab/player references, uses local feet position and authored elevation, and creates disjoint CameraOccluder section owners. It does not construct asset art or change collision/sensing. Player movement activates updates; transition-only processing sleeps at rest. See Building002 README for settings and local checks.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.

## Outside false-positive fade correction

The merged whole-building box is now only a preliminary rejection test. A possible hit must also intersect a cached mesh-local box of a currently visible mesh before the entire building becomes an obstruction target. Mesh-local transforms keep rotated parts tighter than the overall building AABB. This avoids fading solely because the corridor passes through empty space between building parts. The 0.45m configured corridor radius, coherent outside opacity, occupancy/floor rules, cadence and FPS opt-out remain unchanged.

Caches are built once during visibility initialization; StructureVisibility.Rebuild/Refresh regenerates them after geometry/layout changes. No runtime mesh enumeration, allocations, added rays, timer or frame callback. Only buildings passing the broad test visit their cached parts, exiting on the first hit. Mesh boxes remain conservative for holes/concave meshes and the safety radius can still catch nearby edges; this is not triangle-accurate visibility. Moving individual parts requires the existing explicit visibility rebuild/refresh contract.

Focused source/loop checks and numeric empty-gap/part-hit slab checks completed; exact screenshot intersection was not reproduced. Godot/.NET unavailable: compilation and visual acceptance remain pending. Build, F6 World, walk beside the screenshot building, then behind each building; check clearing restores opacity and actual obstruction still fades. Check entry/stairs and FPS briefly.
