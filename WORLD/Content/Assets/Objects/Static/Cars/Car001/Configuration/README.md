# Car001 configuration

`Car001Settings.cs` and `Car001Master.tres` remain the authoring defaults for GLB car dimensions, paint/glass tints and a **closed front-left driver's door (0°)**. `DriverDoorAngleOverride = -1` inherits this setting; any 0–82° override affects only the selected scene instance. Opening the door does not write to the master.

The GLB is designed at 4.35 × 1.83 × 1.81m; the loader scales it and its simple colliders to authored length/width/roof height. The palette is applied to imported material names `PaintMain`, `PaintTop`, `PaintShade`, `WindowGlass` and `GlassGlint`. For future missing parts and independent doors/wheels see [../MODULAR_VARIANTS_PLAN.md](../MODULAR_VARIANTS_PLAN.md).
