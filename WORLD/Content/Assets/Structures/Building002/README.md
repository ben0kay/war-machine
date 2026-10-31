**Current reusable visibility:** this prefab now includes StructureVisibility using its existing master (derived from StructureSettings). The generator declares local occupied volumes, floor elevations and disjoint visual roles. No building-specific visibility case remains in WorldController. Follow [../BUILDING_AUTHORING_README.md](../BUILDING_AUTHORING_README.md) for new structures; older named wiring descriptions below are historical. Metadata does not change mesh/collision geometry. Local build/visual checks pending.

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

**Scene:** `res://WORLD/Content/Assets/Structures/Building002/Building002.tscn`

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

WorldController instances this prefab on saved terrain in both WORLD/World.tscn and WORLD/FirstPerson/FirstPersonWorld.tscn. Its offset is editable in WORLD/Configuration/WorldMaster.tres. Actual generated bounds plus approach margins exclude pines, including external porch/canopy geometry. Terrain is read-only and the parked legacy FPS world is unchanged. See WORLD/README.md for the shared layout.

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


## BL2.1 — Four FPS interior-clearance corrections (2026-10-31)

**Source implemented; awaiting local Godot C# build and owner walkthrough.** Owner screenshots identified an undersized entry-to-stair approach, a desk crowding the main entrance, furniture obstructing a side/front window sightline, and ground-floor partitions stopping well below the upstairs deck. These were the same class of problems previously repaired in PoliceStation001 and codified in the building-design guide.

- **Stair approach:** the original straight flight started at local Z=4.55, leaving only **1.95m between its first step and the +Z front wall**. The new lower entry begins at `stairFront = depth/2 - 3.45m` (Z=3.05 with the default 13m depth) and the rear is `stairFront - 5.50m` (Z=-2.45 by default). The existing 22 treads, single smooth walking-ramp collider, handrails, staircase width and floor elevations remain. At default 4m rise, the resulting slope is **~36°**, and the flat interior entry apron is **3.45m**. The matching upper-floor slab aperture, safety rails and landing move with the same two coordinates. For footprint/height combinations too small to provide this route without clipping the rear office wall, the one-shot generator now throws a descriptive authoring error instead of silently building an obstructed stair.
- **Front entrance furniture:** moved the reception desk from (-3.1,4.35) to **(-4.5,2.85)** in local X,Z at default depth; all desk papers, pencils, radio, antenna and tabletop move with it. The former free door strip is wider and the walk to the first tread is not crowded by the desk.
- **Window sightlines:** moved the dark stove/firebox out of the left-side window bay to the west-front solid wall, moved the front-right equipment locker and its visual leaf onto the right internal partition, and repositioned the rear office cabinet and its fascia away from the rear window bay. Colliders move with the solid furniture; no window geometry is altered.
- **Wall-ceiling seam:** all *downstairs interior partitions* now terminate **0.02m below the true underside of the upstairs slab**, i.e. Y=3.74m for default h=4m and upper deck thickness 0.24m. Both existing doorways keep their same clear widths/heights (headers begin at Y=2.65 and Y=2.60). One rear office wall is also trimmed flush to the receiving partition's inner face. Lower exterior facade remains continuous and upper-floor geometry above the openings remains unchanged.
- **Guard against recurrence:** `Generated/ClearanceVolumes` now reserves the front vestibule, the stair approach and relevant window bays before furnishing, and key collidable furniture passes complete-bounds `StructureClearance` checks once at generation. Reservations are not colliders, do not run per frame, and serve as future furnishing constraints.

**Test:** Build Godot C#, run the lodge in `WORLD/FirstPerson/FirstPersonWorld.tscn` (F6). Approach from outside, walk through the front door, turn to climb and descend the repositioned stairs, approach the front and side windows, inspect the stove, locker, desk and office cabinet, and look up at every partition-to-upper-slab joint. If using a nondefault footprint/height, check the authoring error and whether the room layout can support those dimensions; they are not all inherently compatible with a full-height straight staircase. Source review is not proof of Godot runtime/physics acceptance.


## BL2.2 — wall junction, roof join, porch seam and outside-collision corrections (2026-10-31)

**Implemented in source, local Godot build/FPS acceptance pending.** The owner supplied four follow-up lodge screenshots after BL2.1. This fixes their source-backed causes **without** moving the entrance, staircase or upper deck, deleting furnishings, changing Terrain3D, or modifying FPS/player/world logic.

- **Ground wall closure:** The ground X=2.7 partition previously stopped at Z=front−0.25 (and rear Z=rear+0.25), while the outer shell is 0.23m thick and its **inner face is only 0.115m from the outer plane**. That left a visible **13.5cm gap**. Partition endpoints now reach the real inside face with 5mm intentional perpendicular overlap. The left rear-office divider likewise meets the actual west inner face. Door gaps retain their dimensions and colliders.
- **Z-fighting at front porch:** The noncolliding interior plank floor ends at **Z=front−0.10**, but the former veranda extended to **Z=front−0.245** at the *same Y=0.12* finished top, overlapping 14.5cm. The veranda now starts at **Z=front+0.06**, and its board seams follow the resized deck, preserving the far porch edge. A smaller, visual-only, 4.3cm-raised threshold spans the 16cm join so the entrance does not expose bare ground; the two opaque floor surfaces no longer compete.
- **Upstairs roof closure:** Upper front/rear walls are extended toward the **actual underside of the sloped eave**, not a magic fixed height. The bedroom partition, which used to end flat below the roof, now receives one two-sided triangular infill up to the roof slope, while its existing 2.24m tall passable doorway remains intact. Exterior triangular gables are owned by the **upper wall group once**, not duplicated in roof visuals. The chimney had its bottom **0.62m below the nominal roof peak** near Z=−1.1, exposing geometry through the attic. Its visual stack is now cleanly seated on the actual ridge cap (Z=0) with only 3cm of overlap, short enough not to penetrate the roof interior. No new roof/wall collider is needed for the inaccessible visual gable portion.
- **Porch orientation and collision:** Both bench backs are moved to the lodge-facing (−Z) side so seats face **outward (+Z)**. Existing collidable legs are kept; **bench seats and backrests** now share native static collision. Both side porch rails and all six balusters likewise receive static colliders matched to their visible boxes. The default exterior approach and entrance stay open.

**Local acceptance:** Pull/build C#, run the lodge in `WORLD/FirstPerson/FirstPersonWorld.tscn`. Inspect the entrance partition from either side; pause and move your camera across the threshold and inside/veranda floor for zero flicker; walk upstairs and inspect both upper eaves, the bedroom divider's top and roof/chimney from inside/outside. Sit/stand beside each outward-facing bench to verify collision, and try pushing into every fence rail/baluster (without bypassing through a side gap). For debug, enable **Debug → Visible Collision Shapes**. Check roof hiding in the alternate top-down view still removes only the roof slab, not the now-wall-owned gables. **No local Godot/.NET or in-game acceptance is claimed from source checks.**


## BL2.3 — relocated side-room entry, heavier stairs and wall-aligned furnishing (2026-10-31)

**Source implemented; await local Godot .NET build and owner FPS validation.** Five screenshots after BL2.2 exposed the equipment-room opening sitting beneath the staircase, 22 thin stair treads with flickering riser/tread junctions, a pale office noticeboard intersecting the bookshelf, a cabinet/locker floating out from the wall and a guest bed not aligned with the room. The pass is structural furniture/layout polish only; exterior shell, roof, basement-free foundation, road placement, upper-floor access and player/world scenes are not changed.

- **Doorway:** the right-hand ground-floor equipment-room opening was at X=2.7 / Z=-0.8..1.35, directly beside/under the ascending stair run Z=-2.45..3.05 at default footprint. It now occupies the **front solid partition near Z=3.65..5.85**, completely ahead of the first tread. The actual walk-through wall gap, header/physics opening and a new `EquipmentRoomDoor` authoring clearance use the *same* two coordinates, derived from `hz` so alternative supported building depths also move the doorway correctly. This is not a decorative fake opening; no collider spans its 2.20m passage below the original 2.65m header.
- **Stairs:** default **16 treads instead of 22**, each with **14cm thickness instead of 7cm**, 25cm visual rise at the default 4m upper floor, ~34cm forward tread depth on the same 5.5m run. This is a compromise between visibly chunkier steps and the impractically high 36cm risers of a literal 11-step flight. The stair's **single invisible smooth BoxShape3D ramp** and sloping physical rail colliders are unchanged in position/size; stairwell hole and landings remain aligned. Riser top surfaces are now *buried just below the thick treads*, rather than coplanar with the visible tread tops, eliminating that authored Z-fighting source. One shared configurable integer `StairTreadCount` in `Building002Settings.cs`/`Building002Master.tres` allows 12–22; 16 is the default. No individual step collision is added.
- **Rear office bookshelf:** the pale panel was **`OfficeNotice`**, not a window. It occupied the same rear wall space as the shelves/manuals. The noticeboard moves to the **solid west office wall, higher above the relocated corner cabinet**, leaving the bookshelves unobstructed. No window materials/geometry are changed.
- **Ground furniture:** the big equipment locker shifts from X=3.75, Z=3.30 to the **front-right solid-wall corner** at approximately X=7.86, Z=5.81. Its door moves with it. The office cabinet goes against the rear-office west wall at roughly X=-7.61, Z=-3.85, away from the bookshelf and glass. The stove/firebox moves into the front-left solid-wall corner; ground bunk/mattress/pillow/blankets and lantern move closer to the right-side solid wall together. Reception desk stays in its intentionally accessible reception position. All collidable bodies move with their visuals, and floor/window/stair reservation checks guard their new bounds once at generation.
- **Upstairs furniture:** guest bed is **centred in the solid rear-left wall bay** (between west corner and the first rear window), with its head near that wall. Mattress, blanket and pillow share bed-relative anchors. Wardrobe moves into the solid front-left corner; right-side study desk/books and supply chest/lid now sit against the rear-right and front-right solid corners, away from the real upstairs window openings. New `UpperFacadeWindow`, `UpperSideWindow`, `UpperStairOpening` authoring reservations and full-fixture bounds checks protect the stair and windows when the layout is regenerated.

**Source-only validation:** checked default 17×13m geometry: all five ground and four upstairs furniture volumes were non-intersecting against the registered circulation/window/stair volumes; basic stair/header clearance and code-syntax source checks passed. **Not yet a Godot build or visual/physics pass.** Locally build and F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`, verify the side-room door is clearly ahead of the stairs, descend/ascend on the same smooth collision, examine all step/riser faces for flicker, inspect office notice versus rear shelf, test bedroom centered headboard and confirm no moved furniture blocks any window or corridor. Check with non-default dimensions if customized; some custom footprints may need separately approved layout scaling.
