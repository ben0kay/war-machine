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

The lot preserves 0.8m perimeter collision shoulders. Seven empty bays and the broad circulation aisle remain free. Lamp count stays four (existing spot/fill lighting, shadows off by default), so adding cars does not add local lights. PL2 flicker/day-night/store-glow remains separate. Lighting ownership remains ENVIRONMENT/Lighting/Fixtures/StreetLamp; geometry remains in Placeables/Objects/Lighting/StreetLamp001.

## World fit

The authored site is beneath WorldActorsRoot/PersistentPropsRoot/Structures. The nested store alone registers shared StructureVisibility; asphalt/lamps never inherit whole-store fading. Cars retain their own existing independent camera occluders.

The expanded mesh envelope includes X +/-28.8m shoulders and Z approximately -30.2..24.8, plus reserved rear/driveway access. VillageLayout measures generated bounds, surveys the entire expanded footprint and grades/clears it via existing systems. Road start and extra access reservations now follow the actual RoadConnection/RearServiceExit markers instead of the previous fixed coordinates. The rear-exit ramp follows its moved marker. The northbound village road stays at X=35 (nearest edge X=32), outside the wider paving. Other buildings/robot remain in their existing safe village locations; player arrival remains Z=38. Two rear slow-zombie markers move to Z=-33, runner to Z=-34; actual collision support/clearance and player buffer still gate spawning. Saved terrain is not rebaked.

## Brief checks

Source/numeric checks cover 14-bay width, exact car-to-bay placement, seven separate car envelopes, new lot/road separation, arrival buffer and scene resource references. Godot/.NET unavailable: compilation and runtime appearance/collision are not verified. Pull/build/reopen and F6 WORLD/World.tscn. Inspect all seven cars inside marked bays, lamp bases, open circulation/doors, store/sidewalk alignment, slope access from every edge, complete graded support and no grass/rocks through asphalt. Walk rear service ramp and dirt road join; check store-only fades, local zombies/PER2 and FPS. World startup logs should report the larger footprint survey without layout errors.
