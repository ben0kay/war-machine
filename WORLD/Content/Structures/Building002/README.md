# Building002 — Forest ranger lodge

**Status:** Reusable procedural scene, now placed by WorldController in both the active top-down world and inherited FPS test. Source complete; local build/render/physics validation pending.

## Two-storey lodge and stairs (new)

Building002 now has **two usable storeys in the same 17m × 13m default footprint**. The pre-existing wall height (4.0m) becomes the ground-to-upper floor elevation. Upper wall height defaults to 2.8m, lifting the gable roof base to 6.8m and roof ridge to ~9.4m. Roof/chimney move upward; no new Terrain3D bake or additional building placement.

The ground-storey reception, office, equipment room and porch are retained. Inside, a **straight 22-tread timber stair** rises from the reception/common area to an upstairs landing. There is a **genuine missing floor section above the stairwell**, bounded by upstairs safety railings. Decorated upstairs rooms include a furnished sleeping room and a small desk/supply area; upper facades have actual window apertures and matching glazing.

**Stair collision:** the visible individual treads/risers are cosmetic. A single aligned, invisible **inclined BoxShape3D** provides a walkable slope beneath them, avoiding the CharacterBody3D problem of colliding with each tiny step riser. Both deck segments and windows/walls have their own static collision. Confirm the final walking feel in Godot, particularly cresting the top step and reversing downstairs.

**Generated hierarchy and tagging** (ordinary runtime/generated nodes, not separately authored scene roots):

```text
Building002
├── FloorVisibility                          (top-down runtime section fade owner)
└── Generated
    ├── Level_0                              floor_level=0, floor_elevation=0
    │   ├── Visuals
    │   │   ├── Exterior
    │   │   ├── TimberAndProps
    │   │   ├── Rooms
    │   │   ├── ShowcaseExterior
    │   │   └── ShowcaseInterior
    │   └── Collision                         ground walls, furniture, porch supports
    ├── Level_1                              floor_level=1, floor_elevation=WallHeight
    │   ├── Visuals
    │   │   ├── RoofVisuals
    │   │   ├── FloorDeck                     four spans around open stairwell
    │   │   ├── ExteriorWalls
    │   │   ├── Furnishings
    │   │   └── Partitions
    │   └── Collision                         upper deck/walls/windows/fixtures/roof
    └── Stairs_0_to_1                         connects_floor_from=0, connects_floor_to=1
        ├── Visuals                          treads, risers, side handrails
        └── Collision                        smooth ramp + handrail shapes
```

Both level roots have Godot groups `building_floor_0` and `building_floor_1` for discovery, plus metadata `floor_level` and `floor_elevation` (local metres above building origin). Stair root uses metadata `lower_elevation`, `upper_elevation`, `connects_floor_from` and `connects_floor_to`, plus `building_stairs` group. These tags describe each floor's **geometry ownership**, not an automatic player floor-state detector.

`SetFloorOpacity(int floor, float opacity)` is a **manual visual-only hook** (0=transparent, 1=opaque) for future floor-aware camera logic. It changes only meshes under that floor's `Visuals`; collision under either `Collision` remains enabled. `ShowRoof`/`SetRoofVisible` remain independent of floor visual opacity. Automatic top-down floor detection/fading is now implemented by Shared/BuildingFloorVisibility.cs. Do not call the manual SetFloorOpacity hook while that controller owns presentation.

Read [FLOOR_VISIBILITY_PLAN.md](FLOOR_VISIBILITY_PLAN.md) before building the camera integration. Top-down composition replaces whole-building fading with disjoint section fade owners, composing floor opacity with camera obstruction. Glass physics and line-of-sight classification are a separate consideration from floor cosmetic fade.

**Local validation required:** pull, compile C#, F6 `WORLD/World.tscn`, approach Building002 northwest of spawn, walk up/down stairs and around upstairs furniture in top-down and inherited FPS test. In the remote tree, inspect `Generated/Level_0`, `Level_1` and `Stairs_0_to_1`. Test camera occlusion separately; automatic floor visibility now enabled in top-down.

## Scene and silhouette

**Scene:** `res://WORLD/Content/Structures/Building002/Building002.tscn`

Approximate footprint **17m × 13m**, ground wall height **4m**, upper wall height **2.8m**, gable roof peak **~9.4m** (plus chimney), with a broad covered timber veranda, posts, handrails, weathered wood siding, framed windows, stove chimney and ranger signage.

**Ground-floor interior:** the 2m-wide front door is a real opening with no wall collider across the gap. There are two solid internal partition walls with 1.9–2.15m door gaps, forming a reception/common area, rear office and bunk/equipment room. Low-poly fittings: reception desk, wood stove, bunk and pillow, lockers and cabinet. Side/front window apertures have separate solid glass panes; the rear window glazing is decorative.

**Placement:** centre origin, base at `Y=0`, front entrance faces **+Z**. Place onto reasonably level Terrain3D, leaving the veranda approach clear. Decorative slab and veranda planks use existing terrain for floor collision; walls, furniture, window panes, porch support columns and main sloped roof have static colliders on physics layer 1.

## Source ownership

- `C/Building002Mesh.cs`: **C# mesh-construction script**, `[Tool]` editor-time preview after Godot .NET compilation; no idle processors.
- `Configuration/Building002Settings.cs`: settings resource class with footprint, roof rise and colours.
- `Configuration/Building002Master.tres`: inspector-editable defaults.
- `Artwork/`: future imported `.glb` or texture assets; none supplied.
- Shared primitive routines live in `../Shared/C/StructureGeometry.cs`.
- `Building002.tscn` uses runtime section CameraOccluder owners installed by the top-down composition; no whole-building scene proxy.

`ShowRoof=false` (or `SetRoofVisible(false)` at runtime) hides the roof and veranda canopy for testing the interior, while leaving their solid physics unchanged. The existing camera-fading component applies to the building as one object; no automatic player-indoor detection is included.

## Active placement

WorldController instances this prefab on saved terrain in both WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Its offset is editable in WORLD/Configuration/WorldMaster.tres. Actual generated bounds plus approach margins exclude pines, including external porch/canopy geometry. Terrain is read-only and the parked legacy FPS world is unchanged. See WORLD/README.md for the shared layout.

Godot/.NET are unavailable here; compile, grounding, interior access, collider and camera checks remain for local validation.

## Enhanced detail showcase — additive pass

Building002 now generates extra low-poly C# detail **within its original footprint**. The existing shell, veranda, rooms, openings and base collision are retained.

- **Exterior:** horizontal timber weatherboard accents, subtle green weathering/moss geometry, porch trail noticeboard with paper lines, utility meter/conduit, two benches, small crate, doormat and **small potted plants** (no trees).
- **Interior:** ranger wall map with markings, desk paperwork and pencils, tabletop radio with antenna, office wall shelves and manuals, cupboard fascia, bunk blankets, bedside lantern and a wall clock.
- **Approach:** these are modeled geometry details rather than screen-space decals or textures; no new shaders, physics simulations, lights, AI or per-frame processing. Major furniture/porch post collision remains; lightweight dressing is mostly cosmetic, to avoid excessive collision shapes.
- **Layering:** `Generated/ShowcaseExterior` and `Generated/ShowcaseInterior` are grouped separately so a later pass can refine or replace the dressing without reconstructing the building shell. The entire enhancement lives in `C/Building002Mesh.cs` and can be used in both the top-down world and the inherited FPS preview.

This is a practical example of developing a prop in layers: **shell → usable rooms → furnishings → material/architectural dressing → small storytelling props**. It does not create an imported model or a complete loot/interaction system. Build and view locally to judge densities, placement and collision; authoring here did not include a Godot render test.

### Doorway visibility correction

The interior ranger-map prop is now mounted wholly on the solid front-wall section between the left window and the entrance; it no longer overlaps the open door. The added clapboard/weatherboard strips are split around the pedestrian doorway **at all heights below its header**, and around window openings, so no decorative horizontal beam cuts through an opening. These are purely visual changes—building footprint, solid walls, door collision and room layout are unchanged. Verify after a Godot scene reload/F6.

## Automatic top-down floor visibility (FL0–FL3 source implementation)

World installs `Shared/BuildingFloorVisibility.cs` only through its top-down occlusion hook. It uses explicit player PositionChanged events, inverse building transform and tagged upper-floor elevation. Footprint entry/exit and landing hysteresis avoid boundary flicker; CurrentFloor exposes Outside/Ground/OnStairs/Upper for remote inspection. Occupancy does not depend on camera or raycasts. Local Godot stair/physics verification remains outstanding.

Ground floor: upper deck, furnishings, exterior walls and partitions fade to HiddenUpperOpacity (0.12); roof to IndoorRoofOpacity (0.1). Near the upper landing, upper content restores smoothly over the final 1.8m of ascent; upstairs deck/furnishings are fully opaque. Roof stays faded and upper obstructing wall sections retain normal camera fading. Ground walls restore upstairs. Descending reverses the transition, leaving restores presentation. Stairs and every collider remain intact throughout. Roof includes the porch roof; furniture on the occupied floor never receives camera obstruction fades.

CameraOccluder owns disjoint mesh sets; final opacity = authored opacity × floor presentation × camera obstruction. This avoids competing transparency writes. Lower partition art moved into Level_0/Visuals/Partitions, separate from Rooms furniture. Exterior details keep their existing appearance; walls use individual mesh bounds, so only wall pieces intersecting the existing camera-to-player corridor fade. This is the first visual pass, not per-room visibility or navigation.

Tune Building002Master: HiddenUpperOpacity, IndoorRoofOpacity, FloorFadeSeconds (0.25), LandingHysteresis (0.3m). Settings are copied on initialization. Wake on player movement; processing runs only during a fade and sleeps when settled. No building polling, physics query or whole-world scan. Exit releases player subscription/restores presentation; scene owns proxies. FPS skips the controller and keeps opaque geometry; manual roof toggle remains.

Checks here: source/path/group ownership review, numeric boundary/transition checks and git diff --check. No Godot/.NET executable: build, actual stair alignment, visuals/shadows and inherited FPS load remain pending. Locally build and F6 World; enter lodge, ascend/descend slowly, stop midway/at landing, leave, crouch, test camera angles, and check FPS remains normal. No rebake required.

Wall fading follows camera obstruction, not indoor proximity: individual opaque wall pieces/partitions have camera-only bounds proxies. Other walls stay solid. Upstairs restores ground walls. No additional wall raycasts/timers; these targets join the existing capped camera capsule query. Static per-piece proxies add scene/physics bounds overhead, not script callbacks.

Outside-house obstruction fallback: BuildingFloorVisibility now registers as an explicit camera occlusion provider/target. If the camera-to-player corridor intersects its cached whole-house bounds while the player is outside, the entire lodge (including deck, stairs, furnishings and trim) fades using CameraMaster's obstruction opacity/duration. Leaving the obstruction restores it. Indoors that factor is ignored, preserving occupied-floor furniture and stair readability. Each disjoint mesh owner multiplies the outside factor with existing presentation/camera factors; there is no second transparency writer. The check joins the existing event-driven camera cadence (no extra ray, physics proxy, timer or frame loop); bounds are collected once and endpoints transformed to current building space. This fallback applies to the lodge only: petrol station retains roof/wall-only fading, solid pumps and contents. Source checks only; local Godot build/visual validation pending.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.
