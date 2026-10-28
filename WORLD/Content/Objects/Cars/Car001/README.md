# Car001 — reusable stationary hatchback

**Default driver door: CLOSED (0°).** The existing low-poly car is still the same `Car001.tscn` and is used by the active top-down world, FPS debug scene and three parking-bay instances in `ConvenienceStoreSite001.tscn`. No duplicate closed/open car models or world-placement changes were made.

The car is about **4.35m long × 1.83m wide × 1.81m tall**, local front **-Z**, driver's side **-X**, base at Y=0. It contains hood, roof, glass, four wheels/hubs, bumpers, side mirrors, cabin/seats, and a physically separate **front-left driver's door**, whose own pivot and static collision rotate together. The other front passenger door and rear body panels are currently fixed geometry. This pass does **not** add functioning four-door opening, missing-wheel variation or door-removal yet.

## Current control: per-instance driver's-door angle

- `Configuration/Car001Master.tres` and `Car001Settings.cs` now default to `DoorAngle = 0` (closed). All existing non-overridden instances inherit this, including the convenience-store parked cars.
- On an individual **Car001** root in a Godot scene, set `DriverDoorAngleOverride` in **Per-instance car state**: **-1°** inherits the master, **0°** closes, **58°** recreates the old open pose, up to **82°** is fully open. Reopen/re-instance the scene after changing the Inspector value to regenerate the static art.
- The existing `SetDoorAngle(float degrees)` runtime/editor API still rotates the same hinge and collision. It **no longer changes the shared `Car001Settings` Resource**, so opening one car won't silently affect other copies or the authoring master.
- CameraOccluder is unchanged; static collision remains in collision layer 1. No simulated wheels, driving, inventory, animation/timer, AI, damage or per-frame processing is introduced.

## Future modular damaged cars

See [MODULAR_VARIANTS_PLAN.md](MODULAR_VARIANTS_PLAN.md) for a separately proposed next step: split all doors, wheels, windows, hood and body parts into named independent pivots/groups, using **one** prefab and a lightweight `CarConditionSettings` per-instance resource. A missing part should omit/hide its own mesh **and collision**, rather than replacing the entire car with a new model. Existing front-left driver door is already the first separate hinge. Avoid creating dozens of hand-built car variants.

## Folder and placement

`C/Car001Mesh.cs` builds the procedural geometry once (`[Tool]` for editor preview); `Configuration/Car001Settings.cs` and `Car001Master.tres` own shared defaults, `Artwork/` is reserved for future imports. `Car001.tscn` includes the existing `CAMERA/Occlusion/CameraOccluder.cs`.

WorldController places one independent car near the southeast fuel station using the existing `Car001Offset` in `WORLD/Configuration/WorldMaster.tres`. The convenience-store site instances it three times in bays 02/05/08; those cars are **not yet world-placed** because the whole site is still awaiting PL1. New closed doors reduce the side clearance required compared with an open door; the scene's actual geometry/collision still determines clearance.

**Local check:** after a C# build, preview `Car001.tscn` and `ConvenienceStoreSite001.tscn`; confirm all default driver doors are shut, the front-left panel/window fit without a gap, collision matches, and the site cars remain parked in their bays. Set just one car's `DriverDoorAngleOverride=58` and verify only that car opens and the shared master remains at 0. Revert the test override if necessary. Godot runtime visuals and collision have not been verified here.
