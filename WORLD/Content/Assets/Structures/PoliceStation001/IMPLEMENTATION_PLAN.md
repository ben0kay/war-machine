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
