# Shared procedural structure geometry

Reusable **Godot C# mesh-construction helpers** for Building002 and Building003. They do not alter Building001 or take over world placement/AI. See `C/StructureGeometry.cs`.

Each prefab remains individually configurable and has its own roof visibility and StaticBody3D. Shared helpers create simple boxes, cylinders, wall spans and low-poly pitched roofs; no processing callbacks.

BuildingFloorVisibility.cs (outside C/) owns top-down lodge occupancy and presentation. It receives explicit prefab/player references, uses local feet position and authored elevation, and creates disjoint CameraOccluder section owners. It does not construct asset art or change collision/sensing. Player movement activates updates; transition-only processing sleeps at rest. See Building002 README for settings and local checks.
