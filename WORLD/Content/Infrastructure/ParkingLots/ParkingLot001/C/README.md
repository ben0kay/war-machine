# ParkingLot001 C# geometry — PL0A

`ParkingLotMesh.cs` is now a Godot `[Tool]` `Node3D` builder for a reusable outdoor asphalt foundation; it is attached only to `ParkingLot001.tscn`. It reuses `WORLD/Content/Structures/Shared/C/StructureGeometry.cs` to create one static apron and driveway collision body, raised sidewalk spans and curbs, and noncolliding parking/road paint. All parking bays and connection points use passive named `Marker3D` nodes under Generated/Markers.

All construction happens on `_Ready()`; `Generated` guards against duplication, `SetProcess(false)` and `SetPhysicsProcess(false)` guarantee no per-frame work. No world, car, lamp, store, Terrain3D or lighting references. Scene must be re-instanced after changing master settings; no runtime rebuild polling. Build/render/physics checks pending locally.
