**Current ground contract:** flat terrain-backed site, no added wedges or duplicate pavement/floor collision. See Flush ground-alignment correction below; older ramp descriptions are historical.

# ConvenienceStoreSite001 — village-centre roadside location

**Current:** integrated in WORLD/World.tscn, enlarged to **56 x 36m asphalt, 14 bays and 7 parked cars**. Local build/visual acceptance pending. See IMPLEMENTATION_PLAN.md and ../../../VILLAGE_IMPLEMENTATION_PLAN.md for phase history; older 40x28m / 8-bay / 3-car descriptions are superseded.

The site is a native PackedScene composition: ParkingLot001, one ConvenienceStore001 beneath Structures/Store, seven existing Car001 instances under ParkedCars, and four StreetLamp001 instances under StreetLighting. All 13 child prefab instances stay independently reusable; no copied car/store meshes or site processing loop. World owns placement/terrain/actors; the site owns relative child transforms.

## Current authored positions (site-local metres)

| Element | Position/details |
| --- | --- |
| Parking lot | Origin; main asphalt 56 x 36, +Z driveway ends at Z=24 |
| Store | (0,0,-23.75); front at Z=-17.75 aligns with the new sidewalk/central gap |
| Parked cars | Bays 01/03/05/08/10/12/14; X=-20.15/-13.95/-7.75/1.55/7.75/13.95/20.15, Y=0.12, Z=-12.03 |
| Car orientation | Bays 03 and 12 backed in; others face -Z. Doors closed by default |
| Store lamps | X=-25/+25, Y=0.12, Z=-12.2; heads face inward |
| Approach lamps | X=-24/+24, Y=0.12, Z=12.5; heads face inward |
| FrontEntrance marker | (0,0.12,-17.4) |
| RearServiceExit marker | (5.7,0.12,-30.2) |
| RoadConnection marker | (0,0.12,24) |
| PlayerTestSpawn marker | (0,0.16,4); passive only, not the world's arrival spawn |

The site now uses flush native terrain support with no perimeter wedges (current correction below). Seven empty bays and the broad circulation aisle remain free. Lamp count stays four (existing spot/fill lighting, shadows off by default), so adding cars does not add local lights. PL2 flicker/day-night/store-glow remains separate. Lighting ownership remains ENVIRONMENT/Lighting/Fixtures/StreetLamp; geometry remains in Assets/Objects/Static/Lighting/StreetLamp001.

## World fit

The authored site is beneath WorldActorsRoot/PersistentPropsRoot/Structures. The nested store alone registers shared StructureVisibility; asphalt/lamps never inherit whole-store fading. Cars retain their own existing independent camera occluders.

The expanded mesh envelope includes X +/-28.8m shoulders and Z approximately -30.2..24.8, plus reserved rear/driveway access. VillageLayout measures generated bounds, surveys the entire expanded footprint and grades/clears it via existing systems. Road start and extra access reservations now follow the actual RoadConnection/RearServiceExit markers instead of the previous fixed coordinates. Rear access follows its moved marker on the same graded walking plane. The northbound village road stays at X=35 (nearest edge X=32), outside the wider paving. Other buildings/robot remain in their existing safe village locations; player arrival remains Z=38. Two rear slow-zombie markers move to Z=-33, runner to Z=-34; actual collision support/clearance and player buffer still gate spawning. Saved terrain is not rebaked.

## Brief checks

Source/numeric checks cover 14-bay width, exact car-to-bay placement, seven separate car envelopes, new lot/road separation, arrival buffer and scene resource references. Godot/.NET unavailable: compilation and runtime appearance/collision are not verified. Pull/build/reopen and F6 WORLD/World.tscn. Inspect all seven cars inside marked bays, lamp bases, open circulation/doors, store/sidewalk alignment, slope access from every edge, complete graded support and no grass/rocks through asphalt. Walk rear service ramp and dirt road join; check store-only fades, local zombies/PER2 and FPS. World startup logs should report the larger footprint survey without layout errors.

## Flush ground-alignment correction (current; supersedes ramp fixes)

Owner requested the simpler flat-ground solution. Site groups ParkingLot/Structures/ParkedCars/StreetLighting/SiteMarkers now sit at local Y=-0.12m, embedding the 12cm foundations so pavement, store floor, car/lamp bases and entry markers share the site's root walking elevation. World still grounds the root at VillageLayout.Level; all protected platform terrain is surveyed/graded to that same elevation, with post-check tolerance tightened from 3.5cm to 2mm. Roads still connect via markers.

The site explicitly enables UseTerrainSupport on ParkingLot and Store: native Terrain3D provides the actual walking floor throughout the lot/driveway/interior. Duplicate pavement/store-floor collision boxes are omitted for those instances; standalone prefab defaults retain optional floor support. Building walls/roof, cars, poles and intentionally raised sidewalk curbs remain collidable. The central entrance strip is visual only. Asphalt/store-floor art renders 1cm above the plane to avoid terrain z-fighting; road art is 6mm above it. This visual clearance adds no physical step. No new player step/teleport code or collision-mask workaround.

All asphalt shoulder meshes/wedge colliders, rear-door access wedge, EdgeRampWidth and the world BuildSiteAccess call are removed. This supersedes all previous ramp/wedge instructions above. The same 56x36m, 14-bay, seven-car layout, spawn safety checks, full-footprint vegetation exclusions, store-only visibility and PER2 behavior are preserved. Environment interior exclusion remains a separate plan; no haze/rain changes here.

No new per-frame work: art generation, bounds, terrain survey/grading and support verification remain startup-only. No saved region rebake/write. Numeric/source checks verify common walking heights, the two site terrain-support overrides, retained seven cars, valid resource paths and absence of wedge/trimesh floor code; diff/loop review passed. Godot/.NET unavailable: compilation, Jolt contacts and traversal remain unverified. Locally pull/build/reload, F6 World/FPS, walk directly onto every open pavement edge and into front/rear openings, then check cars/curbs/walls still block normally. No terrain rebake needed. For any remaining obstruction, hold movement and F10 to report the actual collider instead of changing ground height blindly.
