# Terrain-following GPU grass

- `TerrainGrass.cs`: event-driven tile pool, terrain signal subscriptions, one-shot configuration/binding, shutdown.
- `GrassPlacement.gdshader` / `PlacementMaterial.tres`: adaptation of Terrain3D's particle placement/wind-noise shader and cellular noise setup. Guards missing regions, safely rejects holes/steep ground/painted rock. No invalid texture-array layer fetches or NaN rejection geometry.
- `GrassRibbon.gdshader` / `RibbonMaterial.tres`: adaptation of the demo's organic ribbon shape, colour variation and wind bending, with continuous camera-relative grow/shrink distance fade and width compensation.

Both shaders retain Cory Petkovsek, Roope Palmroos and Contributors' copyright. Source and MIT license remain at `addons/terrain_3d/extras/particle_example/` and `addons/terrain_3d/LICENSE.txt`. Original files are unchanged. One shared material pair and ribbon mesh serve each controller's entire fixed tile pool; no per-blade C# objects or processing.
