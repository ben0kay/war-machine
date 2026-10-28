# ParkingLot001 C# geometry — PL0A

`ParkingLotMesh.cs` is now a Godot `[Tool]` `Node3D` builder for a reusable outdoor asphalt foundation; it is attached only to `ParkingLot001.tscn`. It reuses `WORLD/Content/Assets/Structures/Shared/C/StructureGeometry.cs` to create one static apron and driveway collision body, raised sidewalk spans and curbs, and noncolliding parking/road paint. All parking bays and connection points use passive named `Marker3D` nodes under Generated/Markers.

All construction happens on `_Ready()`; `Generated` guards against duplication, `SetProcess(false)` and `SetPhysicsProcess(false)` guarantee no per-frame work. No world, car, lamp, store, Terrain3D or lighting references. Scene must be re-instanced after changing master settings; no runtime rebuild polling. Build/render/physics checks pending locally.

## Three-row optional generation (2026-10-28)

`ParkingLotMesh.Build()` retains the original 14-bay storefront row. For `ParkingRows=2` or `3`, it generates additional left/right banked painted rows and sequential `Bay_15..` passive markers using the same `BayWidth`/`BayDepth` master. The centre gap and row aisles are authorable resource values. The third row's marker yaw turns toward +Z to face the shared second aisle; no car is spawned. Depth checks skip rows that cannot fit within the apron. This happens once during scene initialization with no extra physics or idle processing. The convenient-store site overrides its master to enable 3 rows; other lots keep 1. Godot compile/visual checks pending.
