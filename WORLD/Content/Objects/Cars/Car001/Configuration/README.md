# Car001 configuration

`Car001Settings.cs` and `Car001Master.tres` contain shared body dimensions, body/glass colours, and now a **closed default front-left driver's door angle (0°)**. `DoorAngle` is the default for every car unless the *individual* `Car001Mesh` has `DriverDoorAngleOverride` set to 0..82 instead of -1. The override lives on the car scene instance and does not mutate this resource.

For future missing parts and independent doors/wheels, see [../MODULAR_VARIANTS_PLAN.md](../MODULAR_VARIANTS_PLAN.md); do not embed a combinatorial list of fixed prefab variants into this shared master.
