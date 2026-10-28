# C — GLB scene adapter

`Car001Mesh.cs` now **loads** `../Artwork/Car001.glb`, rather than constructing the rendered car from C# BoxMesh/CylinderMesh. It preserves the existing `Car001` node/API, imported `DriverDoor` pivot, instance-specific `DriverDoorAngleOverride` and `SetDoorAngle(float)`, body/hinge collision, per-instance palette and camera-occlusion integration.

The script builds once and then sleeps. It keeps only coarse static collision shapes; no physics simulation or per-frame animation. Artwork is shared across instances. See [../README.md](../README.md).
