# C — procedural car visual builder

`Car001Mesh.cs` generates the low-poly stationary hatchback once. Its **front-left door** is a separate `DriverDoor` pivot with child static collider, closed at the shared `DoorAngle=0` default. The `DriverDoorAngleOverride` export on the car root (`-1` inherit, `0..82` explicit degrees) allows different scene instances to keep different fixed poses. `SetDoorAngle(float)` changes only this instance's hinge/collision—**never the shared master**.

All other existing body geometry and procedural wheel meshes remain unchanged. No update loops or animation, no missing-wheel/door implementation yet; see [../MODULAR_VARIANTS_PLAN.md](../MODULAR_VARIANTS_PLAN.md).
