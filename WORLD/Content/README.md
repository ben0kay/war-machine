# World content

Reusable authored assets now live under [Assets](Assets/README.md). **AM0 is implemented in source, awaiting local Godot import/build and visual validation.** [Migration record](ASSETS_MIGRATION_PLAN.md).

- [Assets/Sites](Assets/Sites/README.md): composed locations; ConvenienceStoreSite001 is placed in the active village with its store, parking lot, seven cars and four lamps.
- [Assets/Structures](Assets/Structures/README.md): Building001–004, ConvenienceStore001 and shared building visibility/authoring contracts.
- [Assets/Infrastructure](Assets/Infrastructure/README.md): reusable parking and ground assemblies.
- [Assets/Objects](Assets/Objects/README.md): fixed `Static/` assets (cars, furniture, lamps), future liftable `Props/`, shared visual helpers and the GLB catalogue.
- `Vegetation/`: existing procedural trees, flowers, bushes and fallen logs; independent of authored Assets and the separate `VEGETATION/` GPU grass owner.
- `Rocks/`: procedural low-poly boulders.
- `Caves/`: independently generated collidable passage and crystal lights; see [caves README](Caves/README.md).
- `Shared/C/`: existing scatter settings, mesh helpers and Terrain3D placement utilities.

AM0 moves only the reusable asset library and its references. Caves, rocks, vegetation, scatter utilities and terrain data remain in place. Asset folder classification does not rename scene-tree nodes or add interaction/physics.

Decorative scatter still uses runtime master copies, explicit rebuilds and chunk-crossing/terrain-change wake-ups. Empty categories have no meshes; small batches use native culling. These previews do not provide harvesting or persistent gameplay state. See each category README for current behavior and local validation.
