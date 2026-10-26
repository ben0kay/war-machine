# World content

- `Vegetation/Trees`: existing procedural pines, moved with configs/shaders/artwork/validation.
- `Vegetation/Plants`: flower clumps with stems, pointed leaves and five-petal flowers.
- `Vegetation/Bushes`: low woody shrubs with folded leaves.
- `Vegetation/FallenLogs`: tapered logs with cut ends, snapped branch stubs and moss.
- `Rocks`: asymmetric low-poly boulders.
- `Shared/C`: reusable scatter settings, mesh-building helpers and Terrain3D placement.

Each new category owns its `C`, `Configuration` and `Artwork` folders. Its master resource controls density, spacing, patch noise, size, colour, variation, slope, range and shadows. `ImportedMeshes` replaces generated artwork without replacing placement. Flower/bush wind uses the shared tree foliage shader and EnvironmentMaster wind. GPU grass remains separately owned by `VEGETATION/`; it was not part of the old `WORLD/Vegetation` folder.

TestWorld initializes all categories directly. Runtime copies protect source masters. F9 exposes activation, density, size and range; Apply/Rebuild updates transient content only. No collision, harvesting or persistent gameplay state is provided by these decorative previews.

Placement wakes on chunk crossings, explicit settings application or terrain height changes. Empty/disabled categories have no meshes; no per-object nodes, frame callbacks or physics. Small 32m batches use native frustum/range culling, with conservative ranges and shadows off by default. Shapes are deliberately low-poly and currently have one detail level with distance culling, rather than multiple LODs. Terrain is read-only. Large logs align to the local slope but do not conform to every bump; art acceptance and FPS are deferred to the user.
