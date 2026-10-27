# Robot_001 shader source — MAT1

- `WeatheredMetal.gdshader`: physically based paint, worn edges, directional scuffs and dirt using inexpensive deterministic object-space functions
- `IndustrialSurface.gdshader`: physically based brushed metal/joint/rubber grain and roughness variation

They do not create lights, bloom, emission, animated effects, CPU processing, image textures or mesh geometry. Both derive coordinates from local mesh vertices and inherited geometry scale so the finish moves along with the existing animated joints. Used by the `../Materials/` presets and `../../C/RobotMaterialFactory.cs`. Check Godot shader import warnings and visual results locally.
