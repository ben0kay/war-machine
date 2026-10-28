# ParkingLot001 — reusable parking foundation

Current default: 56x36m main apron, 14 bays, 8x6m driveway and 0.12m foundation thickness. The convenience-store site embeds this foundation 0.12m into its graded terrain, so the site root is the walking plane. Seven cars are placed by the independent site scene.

C/ParkingLotMesh.cs builds art, optional pavement collision, raised curbs and markers once. Configuration/ParkingLotMaster.tres is the [Tool]/[GlobalClass] data master. No per-frame processing, terrain queries, rebuild polling, cars or lighting logic lives in this prefab.

## Flat-ground site mode (current)

The exported UseTerrainSupport flag defaults false for independent placements. ConvenienceStoreSite001 explicitly sets it true: Terrain3D is the sole floor under the asphalt/driveway, and no duplicate pavement boxes are generated. The central entrance strip is visual only. Raised side sidewalks/curbs retain physical collision; use the central opening for level access.

The site's ParkingLot/Structures/ParkedCars/StreetLighting/SiteMarkers groups are offset -0.12m. Pavement surfaces render 0.01m above their nominal walking top to avoid z-fighting with the supporting terrain. This visual offset is not a physical step. Paint/entrance access remains visible above that art. All previously added shoulder ramps/wedges and EdgeRampWidth setting have been removed. For standalone use with pavement collision enabled, place the root 0.12m below desired walking elevation over suitable flat support.

Fourteen Generated/Markers/ParkingSpots/Bay_01..Bay_14 markers remain; BayCount supports up to sixteen within the available width. Storefront/Entrance/RoadConnection markers remain. Reload/reinstance after changing dimensions, and realign site cars/lamps/markers as needed.

World/VillageLayout measures the whole generated site, reserves access and supplies existing grass/tree/rock exclusions. VillageTerrain grades all platform samples to the common elevation before actors activate; post-validation now requires <=2mm error. Terrain-support mode requires that prepared ground; this art prefab does not create terrain or provide a floating floor.

Checks: source/resource/height-contract and whitespace review passed. No Godot/.NET here: compilation and actual walking remain unverified. Pull/build/reload and F6 WORLD/World.tscn, enter from both driveway sides, lot edges, central storefront and rear exit in both views. F10 while moving against any remaining blocker prints the actual MoveAndSlide contact path/normal. Saved terrain need not be rebaked.
