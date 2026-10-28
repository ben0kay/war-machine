# ParkingLot001 — reusable asphalt/parking foundation

**Status: scaffold only; PL0A has NOT started.**

`ParkingLot001.tscn` is an intentionally empty but parseable Node3D scene. `C/ParkingLotMesh.cs` and `Configuration/ParkingLotSettings.cs` are zero-byte C# placeholders. `Configuration/ParkingLotMaster.tres` is an inert generic Resource (no settings, no script attached).

**PL0A requirements (future authorization):** create a roughly 35–40m wide asphalt car park with an approach lane, 6–8 painted bays, curb/sidewalk and minimal collision, with adjustable footprint, colour and dimensions. Include named `Marker3D` parking positions, but **do not instance vehicles**: those belong to the site. A future convenience store is positioned alongside the lot, not permanently embedded into the lot model. Use a locally flat Y=0 base at human scale and document whether ground is supported by Terrain3D versus a physical apron; do not silently rebake terrain.

**Performance:** one-shot mesh construction, shared meshes/materials and static collision, no permanent `_Process`/physics callback. Separate visual asphalt/paint from curbs' collision so the player can walk across the apron. Plan for foliage clearing based on the complete lot footprint.

**Not included now:** asphalt geometry, collision, lines, curbs, parking markers, vehicle positions, materials, lighting, vegetation clearing, site instancing, terrain edits or world placement. Open the scene and you will see an empty root, by design.

**Roadmap:** see [site-owned PL implementation plan](../../../Sites/ConvenienceStoreSite001/IMPLEMENTATION_PLAN.md) for PL0A and remaining site phases.
