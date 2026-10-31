# Terrain3D building test V2 — intentionally isolated native region

BuildingTerrainTest.tscn generates this directory's native `terrain3d_*.res`
exactly once, only if no region exists. The corrected rectangular hole removes
Terrain3D from beneath the entire PoliceStation001 footprint, not only the
basement, to prevent coincident terrain with the station's Y=0 ground slab.

The old V1 region in `../` is preserved (but not loaded). The world terrain
in `WORLD/Terrain/` is never read or modified. Edit/save V2 in Godot if desired.
