# ParkingLot001 — reusable parking foundation

Current default: 56x36m main apron, 14 bays, 8x6m driveway and 0.12m foundation thickness. The convenience-store site embeds this foundation 0.12m into its graded terrain, so the site root is the walking plane. Seven cars are placed by the independent site scene.

C/ParkingLotMesh.cs builds art, optional pavement collision, raised curbs and markers once. Configuration/ParkingLotMaster.tres is the [Tool]/[GlobalClass] data master. No per-frame processing, terrain queries, rebuild polling, cars or lighting logic lives in this prefab.

## Flat-ground site mode (current)

The exported UseTerrainSupport flag defaults false for independent placements. ConvenienceStoreSite001 explicitly sets it true: Terrain3D is the sole floor under the asphalt/driveway, and no duplicate pavement boxes are generated. The central entrance strip is visual only. Raised side sidewalks/curbs retain physical collision; use the central opening for level access.

The site's ParkingLot/Structures/ParkedCars/StreetLighting/SiteMarkers groups are offset -0.12m. Pavement surfaces render 0.01m above their nominal walking top to avoid z-fighting with the supporting terrain. This visual offset is not a physical step. Paint/entrance access remains visible above that art. All previously added shoulder ramps/wedges and EdgeRampWidth setting have been removed. For standalone use with pavement collision enabled, place the root 0.12m below desired walking elevation over suitable flat support.

Fourteen Generated/Markers/ParkingSpots/Bay_01..Bay_14 markers remain; BayCount supports up to sixteen within the available width. Storefront/Entrance/RoadConnection markers remain. Reload/reinstance after changing dimensions, and realign site cars/lamps/markers as needed.

World/VillageLayout measures the whole generated site, reserves access and supplies existing grass/tree/rock exclusions. VillageTerrain grades all platform samples to the common elevation before actors activate; post-validation now requires <=2mm error. Terrain-support mode requires that prepared ground; this art prefab does not create terrain or provide a floating floor.

Checks: source/resource/height-contract and whitespace review passed. No Godot/.NET here: compilation and actual walking remain unverified. Pull/build/reload and F6 WORLD/World.tscn, enter from both driveway sides, lot edges, central storefront and rear exit in both views. F10 while moving against any remaining blocker prints the actual MoveAndSlide contact path/normal. Saved terrain need not be rebaked.

## Optional second and third bay rows (2026-10-28)

**Implemented in reusable `ParkingLotMesh.cs`, local Godot build/visual validation pending.** New `ParkingLotSettings` fields are `ParkingRows` (1–3; **standalone default 1**, unchanged), `CentralDriveGap` (default 9.3m) and `InteriorAisleWidth` (default 6.0m). Additional rows are built **only** if selected by an instance-specific settings resource, and only if there is room inside its asphalt dimensions. For added rows the markings/parking markers are split into left and right 7-bay banks on a 56m-wide site, leaving a central corridor extending from the +Z entry to the original front parking aisle. The second row faces north, third faces south, and their shared approach aisle remains 6m deep. Painted dividers/heads and passive `Generated/Markers/ParkingSpots/Bay_NN` nodes are created once in `_Ready`; no parked cars or lamps are generated in this prefab.

**Current ConvenienceStoreSite001:** Overrides the standalone `ParkingLotMaster.tres` with `Sites/ConvenienceStoreSite001/Configuration/SiteParkingLotMaster.tres`, requesting `ParkingRows=3`, yielding **42 bays total**: original 14 + two groups of 14. Middle/outer row bank corners are X±4.65..26.35, while entry corridor X=-4.65..4.65 remains free of stalls. From north to south: row1 Z=-14.83..-9.23, aisle Z=-9.23..-3.23, row2 Z=-3.23..2.37, aisle Z=2.37..8.37, row3 Z=8.37..13.97; rear service/asphalt margin to Z=18 remains clear. Full site stays 56×36m, with the existing 8×6m +Z driveway stub, curb/storefront/hedges, terrain-support mode, and original Bay_01–Bay_14 marker IDs. New markers run Bay_15..Bay_42. Existing seven cars remain in their exact prior positions. Extra rows are rejected with a warning when dimensions cannot fit. Other ParkingLot001 uses with their standard master remain single-row.

No added collision for parking paint/markers, no new floor layer, no Terrain3D rebuild and no continuous runtime work. F6 World and FPS check bay markings and driving corridors; test driving/walking through the continuous centre, ~6m horizontal circulation aisles, store-front hedges/entry and new lamp placement. Local Godot/C# tests still required.
