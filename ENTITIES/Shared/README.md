# Shared mobile actor foundation — RM0

`Entity.tscn` is a reusable CharacterBody3D with one inexpensive capsule and an empty `VisualRoot`. It is not placed in TestWorld. Inherit the scene for a mobile species; resize/reposition the local capsule there. Stationary props need not use this body.

`C/EntitySettings.cs` defines only shared type identity and the initial active flag. `../Configuration/EntityMaster.tres` is the authoring master. Each actor deep-copies it once into `RuntimeSettings` on Ready, so runtime edits do not change the master or other instances. Set `Settings` before adding an actor to the tree. `TypeLabel` describes a type, not a persistent save ID. StartActive is a startup default; call Activate/Deactivate for runtime transitions.

## Lifecycle and component contract

- `Activate()` and `Deactivate()` are idempotent. Calls before Ready do nothing; set StartActive before spawning instead. Child Ready runs before activation, allowing optional components to subscribe once to `ActiveChanged` using an injected/exported actor reference.
- Concrete C# actors can derive from EntityCore and override `OnActivated`, `OnDeactivated`, and `OnEntityExitTree`. Call base if overriding Godot lifecycle callbacks. Choose hooks or the signal for each capability so it is not started twice.
- Components cache explicit owner/route/socket references during initialization. No per-frame scene searches. Connect in Ready and disconnect in ExitTree; external player/terrain subscriptions must also be released on exit, even if the actor never activated. A reused component must arrange its own Ready/reconnection on re-entry.
- On deactivation, stop capability timers, hide scan VFX, settle/stop gait and cancel sensory work synchronously. Do not rely on disabling process alone to hide visuals or stop timers. `ActiveChanged(false)` is emitted after IsActive becomes false. Timers do not exist in RM0.
- All children must inherit process mode. Do not use Always/WhenPaused to bypass actor dormancy. Root ProcessMode becomes Disabled on deactivation; reactivation restores Inherit, respecting scene pause. Deactivation clears velocity and both root processing flags. Collision and general visuals remain present while dormant; future species must explicitly hide effects or alter collision if needed.
- The base owns no external subscriptions. On exit it deactivates, invokes unconditional cleanup and requests Ready for possible reattachment. Reattachment uses StartActive and preserves that instance's runtime settings; it does not resume movement state in RM0.

## Art and later phases

Physics movement changes the actor root. Art/animation changes `VisualRoot`. RM1 defines `VisualRoot/BodyRoot`, `HeadPivot`, `SensorSocket` (local forward -Z), and named leg pivots in Robot_001's rig; they are not mandatory empty nodes in every entity. Inject typed socket references into later components, preserving them when replacing generated art with a GLB.

RM2 adds optional [Movement](Movement/README.md), RM3 adds optional [Sensing](Sensing/README.md), and robot-only gait/beam stay under TYPES/Robots/Shared. RM4 world relevance subscribes to explorer ViewPositionChanged with an initial check and distinct wake/sleep radii. None of those capabilities is mandatory on this base; no global registry/manager is introduced.

## Cost and validation handoff

Wake: Ready with StartActive, or an explicit Activate call. Sleep: Deactivate or scene exit. The base has no Process/PhysicsProcess overrides, timers, input handlers or queries, even while active. Only optional future capabilities request active callbacks. This is a source-level design claim, not measured profiling.

Validation performed: source review, scene/resource path checks, `git diff --check`. This environment has no dotnet or Godot executable, so compilation and scene instantiation remain **Awaiting Validation**. Locally run `dotnet build war-machine.csproj`, then briefly instantiate two Entity scenes: verify independent RuntimeSettings, repeated Activate/Deactivate emits once per transition, zero velocity/processing when disabled, never-active cleanup, remove/re-add and QueueFree without errors. Verify TestWorld still opens; RM0 itself made no world edits (RM4 now adds a world-owned encounter). No extended visual/performance testing is needed for this nonvisual phase.

PatrolController and ScanSensor now attach each timer Timeout callback only when creating that timer. Reinitialization reuses the connection, and exit disconnects it. This removes the initial attempt to disconnect a nonexistent connection shown alongside the grass shader failure; no timer cadence or behavior changes. Local Godot validation pending.
