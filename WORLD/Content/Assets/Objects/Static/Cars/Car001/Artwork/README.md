# Car001 artwork — GLB

`Car001.glb` is the imported, reusable low-poly hatchback, with coloured PBR materials, named body/trim/cabin/wheel mesh groups and a separate **DriverDoor** transform pivot at (-0.93, 0, -0.83). Front is local -Z, up +Y, driver's side -X; 1 unit = 1 metre. Default car dimensions are about 4.35 × 1.83 × 1.81 metres.

The GLB contains **visuals only**. `../C/Car001Mesh.cs` supplies simplified static collision and default/per-instance driver-door angle. Do not flatten or rename `DriverDoor` when editing/re-exporting; `Car001.tscn` depends on it. Preserve named paint/glass materials if you want Car001Master recolouring to keep working.
