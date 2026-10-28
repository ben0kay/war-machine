# World content

> **Planned only:** [AM0 one-pass Assets migration](ASSETS_MIGRATION_PLAN.md) consolidates existing reusable Sites, Structures, Infrastructure and Objects/Props under `WORLD/Content/Assets/`. **Do not use the planned paths until Codex completes AM0.** Current folder paths below remain authoritative for now.

- `Vegetation/Trees`: existing procedural pines, moved with configs/shaders/artwork/validation.
- `Vegetation/Plants`: flower clumps with stems, pointed leaves and five-petal flowers.
- `Vegetation/Bushes`: low woody shrubs with folded leaves.
- `Vegetation/FallenLogs`: tapered logs with cut ends, snapped branch stubs and moss.
- `Rocks`: asymmetric low-poly boulders.
- `Objects/Cars/Car001`: standalone stationary low-poly compact car with an already-swung-open driver door and static collision; placed in both active top-down and inherited FPS views.
- `Placeables/`: **new reusable model library** (`Objects/` cannot be picked up; `Props/` eventually pickup-able). Bookcase001/Book001 GLB designs only, no physics or placement; Car001 remains at original path.
- `Structures/Building001`: existing maintenance depot with interior/collision and an active-world placement.
- `Structures/Building002`: new procedural timber ranger lodge with covered porch, walk-in door, actual window apertures and divided interior; placed in both active views.
- `Structures/Building003`: new procedural roadside fuel station with walk-in shop, closed garage shutter, rear exit, pump canopy and colliding fuel pumps; placed in both active views.
- `Shared/C`: reusable scatter settings, mesh-building helpers and Terrain3D placement.
- `Sites/ConvenienceStoreSite001`: empty composition scene reserved for a modular roadside store + lot.
- `Infrastructure/ParkingLots/ParkingLot001`: empty reusable asphalt/parking foundation, next PL0A.
- `Structures/ConvenienceStore001` and `Placeables/Objects/Lighting/StreetLamp001`: empty future store/lamp prefabs. These are NOT placed or operational.

Each new category owns its `C`, `Configuration` and `Artwork` folders. Its master resource controls density, spacing, patch noise, size, colour, variation, slope, range and shadows. `ImportedMeshes` replaces generated artwork without replacing placement. Flower/bush wind uses the shared tree foliage shader and EnvironmentMaster wind. GPU grass remains separately owned by `VEGETATION/`; it was not part of the old `WORLD/Vegetation` folder.

TestWorld initializes all categories directly. Runtime copies protect source masters. F9 exposes activation, density, size and range; Apply/Rebuild updates transient content only. No collision, harvesting or persistent gameplay state is provided by these decorative previews.

Placement wakes on chunk crossings, explicit settings application or terrain height changes. Empty/disabled categories have no meshes; no per-object nodes, frame callbacks or physics. Small 32m batches use native frustum/range culling, with conservative ranges and shadows off by default. Shapes are deliberately low-poly and currently have one detail level with distance culling, rather than multiple LODs. Terrain is read-only. Large logs align to the local slope but do not conform to every bump; art acceptance and FPS are deferred to the user.

`Caves/` now owns an independently generated, collidable rocky passage with crystal lights near spawn. Unlike decorative scatter categories, it builds once and is not affected by vegetation rebuilds. See Caves/README.md.
