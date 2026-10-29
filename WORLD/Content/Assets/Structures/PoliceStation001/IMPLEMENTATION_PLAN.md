# PS — PoliceStation001 staged implementation

**Target:** reusable large two-storey structural police-station prefab for staged visual/physics/performance testing. Branch `implement-td0-td1`.

| Phase | Deliverable | Suggested assistant | State |
| --- | --- | --- | --- |
| **PS0** | Standalone [Tool] scene, 48×64m shell, both floors, real apertures, stairs/landing/collision, semantic structure visibility, clearance, editor preview | Regular Chat / Codex integration; owner visual check | **Implemented in source; Awaiting Validation** |
| **PS1** | Front-right lobby U-stair, connected upper landing, varied asymmetrical ground rooms | Regular Chat / Codex; owner visual validation | **Implemented in source; Awaiting Validation** |
| **PS2** | Selectively furnish accepted rooms with reusable models under safe clearance / floor ownership | Codex for integration, Regular Chat for simple placement | Planned |
| **PS3** | Separate explicit world/site placement, culling/streaming integration and repeatable performance stress baseline | Codex + owner local profiler | Planned |

Each phase requires separate authorization. PS0 and explicitly requested PS1 are implemented in source; PS2 and PS3 remain planned.

## PS0 — precise scope

- **Inputs:** active CodePreferences, building authoring README, convenience-store clearance/visibility pattern, Building002's upper-deck void and sloped stair collider.
- **Geometry:** fixed 48×64m footprint; 4.2m ground and 3.8m upper wall defaults; front lobby, central connecting corridors, empty enclosed side rooms, upstairs offices/open-plan space, empty bathroom and west-rear stairs. Art is structural low-poly palette only.
- **Exclusions:** no furnishings, operative door physics, lights, props, player/AI/nav changes, world placement, terrain rebake, FPS adapter edits or new global managers.
- **Owners:** `PoliceStation001.tscn`, `C/PoliceStationMesh.cs`, `Configuration/PoliceStationSettings.cs`, `Configuration/PoliceStationMaster.tres`, `Artwork/`, own docs and the Structures catalogue. Shared systems are reused, not modified.
- **Compatibility:** retain level-0 and level-1 markers, separate wall/roof/deck/stair visual roles, occupied footprint metadata, independent static collisions and live top-down fade/aim semantics. Editor preview hides presentation nodes but never modifies colliders or runtime visibility behavior.
- **Performance:** generated once when scene enters the tree. All geometry and clearance volumes are static; dormant per-instance process callbacks. No horde spawn until a later independently scoped phase.
- **Validation:** static source/scene/path checks before commit; `dotnet build`, Godot C# `[Tool]` preview modes and world runtime physics, camera fade and performance are **deferred to local validation**. No acceptance or measured gain claimed.

## Next owner handoff

Open `PoliceStation001.tscn` directly after C# build. Inspect GroundFloor, UpperFloor and Exterior via `EditorPreview`. Capture observations or screenshots to guide PS1. Only place the prefab into `WORLD/World.tscn` after an explicit placement request and terrain/streaming plan.

## PS0 editor preview repair — 2026-10-29

**Source implemented; awaiting local acceptance.** Screenshot showed Inspector settings but no visible model. The generator now rebuilds its transient children whenever the tool scene enters the tree, and offers a dedicated `[ExportToolButton]` **Build / Refresh Preview** action so a post-compile scene can recover without attaching to WORLD. Only the lightweight `Generated` anchor receives editor scene ownership for tree visibility; large procedural descendants remain transient and are not packed into `.tscn` on Save. The scene's root, world integration, procedural geometry, collision, fade and all three preview states are unchanged. Local owner should build, reopen, click Refresh if needed and inspect the Output dock for `[PoliceStation001]` or the first error.

## PS1 — front U-stair and nonrepetitive floor plan (2026-10-28)

**Authorized after owner reviewed the editor scene; source implemented / awaiting local validation.** Retains the 48×64m facade, window/door openings, editor preview/refresh, shared wall/deck/roof/stair roles and collision linking. The straight rear-left stair is replaced with two opposed 12-step flights in the front-right lobby, a walkable mid landing, connected top landing and a matching true deck void. A lobby-adjacent waiting room, large offices/admin and varied smaller interview/records rooms replace repeated ground-floor room sizes. Protected clearance markers track routes and openings. No furnishing, interactivity, placement, streaming, or new processing systems are included.

Build `dotnet build war-machine.csproj` locally, open `PoliceStation001.tscn`, refresh and inspect all three editor modes, verify two ramps/turn and upper-floor opening, and later validate walkable routes in a separately authorized world placement. No local Godot compilation/runtime test was performed for this change.

## PS1 shell clearance correction (2026-10-28)

**Owner-reported visual defects:** stair door and guardrail overlapped the lower flight, no safe level apron before the first riser, and partitions terminated into facade windows. **Repair implemented in source, pending local acceptance.** Move the front-right U-stair's bottom flight to X=11/Z=23 and move the side doorway to X=7/Z=20.8. Structural validation now rejects less than 2m of front or lateral level approach; the lower entrance has 2.2m front and 2.24m side separation from the lower rail, and a named flat-zone reservation. Upper guards live in the upper-floor contents role. Move waiting-room junction to solid front pier and the west/east partitions to side piers; enforce a 0.5m window edge margin for all side-exterior-joining dividers on both floors. The root building authoring guide now mandates structural (not just furniture) collision checks for stairs, doors and windows. No furnishing/AI/world placement.

**Owner test:** Godot .NET compile, rebuild scene preview, inspect entrance from lobby eye height and overhead, inspect side/front facade-window junctions on both levels, then test walking through the U-turn when this prefab is authorized for in-world use. Do not call those tests passed until observed.

## PS1.2 — multi-floor seams, wall-junction audit and basic ceiling lights

**Owner authorized in follow-up screenshots; implemented in source; local validation pending.** Fix structural clipping rather than conceal it: lower wall tops stop at the upper deck underside minus 0.02m, upper walls at the roof underside minus 0.02m. Trim perimeter and main-hall room cross-walls to their receiving solid wall faces and repair interview/records/bathroom/waiting-room wall run endpoints; move an upstairs office divider away from its dangling endpoint. Keep openings, stair safety, collision semantics, views and editor refresh working. Add reusable small indoor ceiling fixtures with shared Light3D settings, one cheap unshadowed spotlight each, 2.1m corridor strips at ~8m intervals and optional room-centre small panels on each floor; avoid lighting every room. Exclude furniture, other gameplay systems and world placement. Check multi-floor slab seam, all wall intersections, windows, doorways and stairwell in both editor modes; local compile/render/physics and performance acceptance still needed.

## PS1.3 — exterior facade continuity (2026-10-28)

**Owner-reported regression:** PS1.2 applied its roof/floor clearance to every wall, including the exterior facade, producing a visible 0.26m high horizontal band between the two storeys. **Source fix implemented, Godot preview acceptance pending.** Keep lower **interior** partition tops at H−T−0.02m and upper interior tops below the roof; keep exterior wall top at H (ground) / actual roof underside (upper). Meet upper slab perimeter at the facade's inside face (inset 0.115m), leaving the U-stair opening unchanged. Maintain matching collisions, window/door spans, editor modes and lighting. Record separate exterior/interior requirements in the canonical authoring guide. Door001 reuse was examined and described in the prefab README; police-station doors remain static, noncolliding placeholders until separately authorized physical-door integration after local hinge validation.

**Local test:** build C#, refresh the scene, review each facade side in Exterior mode at the floor boundary and roof, verify no gap between upper/lower wall surfaces, and check UpperFloor mode for interior wall tops. No local Godot build or visual/physical acceptance claimed.

## PS1.4 — fill upper landing void, guard exposed shaft, reconnect walls (2026-10-29)

**Explicit owner correction from four editor screenshots; implemented in source, awaiting local validation.** Move the upper floor cutout's south edge to the actual last stair tread at Z=23. Construct colliding floor across the previous uncovered Z=18..23 landing rather than retain the duplicate 4m-wide landing slab. Guard exposed south edge on either side of the upper stair exit with a 3.52m clear opening, while keeping side/north guardrails and physical walkable stair slopes. Eliminate the 0.41m free-ended upper corridor nubs: tie both hall walls into a front vestibule boundary at Z=18.5, with a 3.6m real doorway at the stair exit's X position connecting to upper offices. Reinforce upstairs stair cutout/exit validation and building authoring policy. No furniture, hinged door migration, AI, scene placement or looping work. Tests pending in Godot: top step → continuous apron, guarded shaft edges, both interior approaches, no crossing rails/walls, proper upper facade/windows and preview ownership.
