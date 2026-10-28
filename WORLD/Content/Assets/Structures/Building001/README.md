**Current reusable visibility:** this prefab now includes StructureVisibility using its existing master (derived from StructureSettings). The generator declares local occupied volumes, floor elevations and disjoint visual roles. No building-specific visibility case remains in WorldController. Follow [../BUILDING_AUTHORING_README.md](../BUILDING_AUTHORING_README.md) for new structures; older named wiring descriptions below are historical. Metadata does not change mesh/collision geometry. Local build/visual checks pending.

# Building001 — Abandoned rural maintenance depot

**Status: first standalone procedural building prefab implemented.** Designed as a **large, stationary, walk-in visual/physics test** for the fully 3D angled top-down game. It is **not yet placed** in `WORLD/World.tscn`. No building placement, interaction, loot, doors-opening gameplay, destructibility, AI or save mechanics have been added.

## Godot prefab

**Instantiate:** `res://WORLD/Content/Assets/Structures/Building001/Building001.tscn`

**Model:** roughly **20m wide × 14m deep × 7.8m high**, plus a roof antenna. Earth-like, weathered maintenance workshop/depot with subdued industrial colours and very restrained sci-fi detail.

- Distinctive **two-slope metal roof** with raised seams, ridge cap, rain gutters, two vents and a small aerial.
- Muted green/grey **sheet-metal siding**, corner posts, concrete base, believable gable ends and siding flutes.
- **Large closed roller shutter** (6m wide) with panel ribs and narrow high windows.
- **Separate ~2m open human doorway** with recessed open door leaf, threshold, overhead canopy and frame. The opening is real, not a black painted rectangle.
- **Side window apertures** with tinted panes, frames and bars; rear window details.
- Loading apron, industrial warning stripes, garage bollards, exterior utility box and two drums.
- Single open interior room / slab footprint. No interior furnishings yet; walking ground is currently the underlying Terrain3D.

**Collision:** generated under one `StaticBody3D` on physics layer 1. Walls, garage shutter, window panes and the two roof slopes block movement. Decorative trims and small geometry are mostly non-solid. The open pedestrian door has no blocker or continuous front foundation across the entry. The concrete floor and apron are intentionally non-colliding overlays so the existing terrain remains the walkable surface; place on **reasonably level terrain**.

## Exact folder convention

- [C/Building001Mesh.cs](C/Building001Mesh.cs) is the **Godot C# 3D mesh generator**, marked `[Tool]` so Godot .NET can generate an editor preview after C# compilation. This folder does **not** own unrelated gameplay logic.
- [Configuration/Building001Settings.cs](Configuration/Building001Settings.cs) and [Building001Master.tres](Configuration/Building001Master.tres) expose dimensions/colours. Each prefab references the default master; visuals are created once from its values on scene load.
- [Artwork/](Artwork/README.md) holds **future user-imported models**. Replace generated visuals with an imported model without changing the prefab's scene path or future placement.
- `Building001.tscn` is a reusable independent scene, not generated terrain or a world-only method.

## Editor preview / run

1. Checkout branch `implement-td0-td1`, open the project in **Godot .NET** and **Build** the C# project.
2. Open `Building001.tscn`; its `[Tool]` mesh script generates the visible building **once** in the editor. Reopen/reload the scene to regenerate after authoring-resource changes (it does not poll the inspector).
3. To test inside the game, Codex can **instance this scene** under the active `WORLD/World.tscn`, then use the stored Terrain3D data's `get_height` at the chosen X/Z to position the **building root Y** on the ground. Do not place it on the robot route or directly over the player. **This placement was not done in this commit.**
4. On the prefab root, `ShowRoof=false` gives an overhead view into the interior while retaining roof physics. At runtime `SetRoofVisible(false)` can do the same. Hiding the roof from player occlusion is a future world/camera behaviour, not yet integrated.

### Practical notes

- Scene origin: centre of footprint at local **Y=0**, main human entrance and garage door face **+Z**.
- Building is intentionally static: **no `_Process`, `_PhysicsProcess`, timers, particle systems or navigation loops**.
- No terrain modifications or main-world/parked-first-person changes.
- The C# procedural model is generated on scene load. This is **not a baked `.glb` model**; if a permanent exported mesh is required later, add a separate editor baking/export workflow.
- **Visual/compile/runtime checks are still needed in Godot**; repo-only source/path inspections do not establish rendering/physics success. Check roof silhouette, the actual open door, wall collision and entry from the loading apron.

## Camera obstruction fading

Building001.tscn now includes CAMERA/Occlusion/CameraOccluder.cs. Once placed, its meshes opt into the active camera's scenery-only fade query. The component defers bounds collection until the procedural mesh is built. Solid collision remains unchanged. This does not place the prefab or implement indoor roof hiding; see CAMERA/Occlusion/README.md.

## Building placement and trunk collision

The active WORLD/World.tscn now places this prefab via WorldController.PlaceBuilding at offset (4,-32) from spawn. Ground height is sampled once, no saved terrain is changed. Pine exclusion bounds cover the building, loading apron and approach. Existing solid walls/open doorway and CameraOccluder remain in use. Default placement is safely separate from the robot patrol.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.
