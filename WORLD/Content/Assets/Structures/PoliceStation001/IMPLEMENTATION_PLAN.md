# PS — PoliceStation001 staged implementation

**Target:** reusable large two-storey structural police-station prefab for staged visual/physics/performance testing. Branch `implement-td0-td1`.

| Phase | Deliverable | Suggested assistant | State |
| --- | --- | --- | --- |
| **PS0** | Standalone [Tool] scene, 48×64m shell, both floors, real apertures, stairs/landing/collision, semantic structure visibility, clearance, editor preview | Regular Chat / Codex integration; owner visual check | **Implemented in source; Awaiting Validation** |
| **PS1** | Owner-reviewed room/entry/stair/window layout adjustments from Godot screenshots and walkthrough | Regular Chat for bounded edits, Codex for geometry/physics redesign | Planned |
| **PS2** | Selectively furnish accepted rooms with reusable models under safe clearance / floor ownership | Codex for integration, Regular Chat for simple placement | Planned |
| **PS3** | Separate explicit world/site placement, culling/streaming integration and repeatable performance stress baseline | Codex + owner local profiler | Planned |

Each phase requires explicit separate authorization; PS0 is the only authorized implementation here.

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
