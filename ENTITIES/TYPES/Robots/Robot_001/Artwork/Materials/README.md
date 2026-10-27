# Robot_001 material presets — MAT1

These four Godot `ShaderMaterial` resources are authored presets, not textures from outside War Machine. The files have **no imported PNG** dependencies.

- `WeatheredArmour.tres`: `../Shaders/WeatheredMetal.gdshader` — weathered paint/edge chips and mixed roughness
- `BrushedSteel.tres`: `../Shaders/IndustrialSurface.gdshader` — lightly brushed exposed metal
- `JointMetal.tres`: `../Shaders/IndustrialSurface.gdshader` — darker working joints
- `IndustrialRubber.tres`: `../Shaders/IndustrialSurface.gdshader` — rubberized cables and dark recessed gaskets

`../../C/RobotMaterialFactory.cs` loads and **duplicates** each applicable preset once per finish/robot, then adjusts robot-specific settings from `../../Configuration/RobotMaster.tres`. The source `.tres` files stay reusable and do not gain global side effects.

To disable MAT1 and restore the older flat finishes, set `RobotMaster.ProceduralMaterialsEnabled=false` and respawn.
