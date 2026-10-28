# ConvenienceStoreSite001 — assembled reusable roadside location

**Status:** PL0B assembled, Godot build/visual validation pending. **Not placed in the active world**; PL1 is the next phase. See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

This is a **native Godot PackedScene composition**: all existing models stay independent, with editable local child transforms and no new site C# controller or per-frame logic.

```text
ConvenienceStoreSite001
├── ParkingLot                  ParkingLot001.tscn
├── Structures
│   └── Store                   ConvenienceStore001.tscn
├── ParkedCars
│   ├── Car_Bay02               Car001.tscn
│   ├── Car_Bay05               Car001.tscn
│   └── Car_Bay08               Car001.tscn
├── StreetLighting
│   ├── Lamp_WestStore          StreetLamp001.tscn
│   ├── Lamp_EastStore          StreetLamp001.tscn
│   ├── Lamp_WestApproach       StreetLamp001.tscn
│   └── Lamp_EastApproach       StreetLamp001.tscn
└── SiteMarkers                 FrontEntrance, RearServiceExit,
                                RoadConnection, PlayerTestSpawn
```

## Authored layout (metres; root space)

- Main ParkingLot001 at origin, 40 × 28m main asphalt with 8 bays, +Z road and -Z store. Top of asphalt Y=0.12m.
- Store root at **(0,0,-19.75)**, with front (+Z) at Z=-13.75: its central entrance aligns with the parking lot's lowered sidewalk access. Rear service exit faces further -Z. Store uses the existing StructureVisibility directly.
- Existing **Car001** hatchbacks placed in bay **02** (-7.75,0.12,-8.03), **05** (1.55,0.12,-8.03), **08** (10.85,0.12,-8.03). Cars face -Z toward the store; their **driver doors now default CLOSED**, with the original static collision and camera occluder. Each car can independently set `DriverDoorAngleOverride=58` to restore the old open-door pose without a duplicate prefab.
- Four **StreetLamp001** at X=-14/+14, Z=-8.2 and X=-14.5/+14.5, Z=8.5; all Y=0.12. West lamps yaw +90°, east lamps yaw -90° so the light-head arms point inwards over the pavement. Real light behaviour and masters stay under `ENVIRONMENT/Lighting/Fixtures/StreetLamp/`.
- Passive site markers: front entry at (0,0.12,-13.4), rear service exit (5.7,0.12,-26.2), road connection (0,0.12,20), test spawn (0,0.16,4). **No character is spawned.**

Children are genuine **scene instances**, not copied geometry. Their local transforms can be edited manually in Godot. If the parking lot's dimensions or bay count change, realign the cars/store/lamps manually: the site does not poll markers or automatically rearrange itself. `SiteMaster.tres` and the still-blank site-settings C# file remain future PL1 world-placement placeholders, not active duplicate transform settings.

**Structure visibility:** when PL1 places the *entire site* under `WorldActorsRoot/PersistentPropsRoot/Structures`, nested store visibility can register through the existing recursive discovery, while the lot/lamps/cars remain independently owned. Don't tag the entire site with building fade.

**Lighting:** four lamps default to steady ON for preview (up to eight actual local lights, shadows off); automatic day/night switching, one flickering lamp, range relevance and Sky3D coordination remain PL2. No new site lighting components were introduced.

**PL1 terrain:** full footprint approximately X ±20, Z -26..20, plus rear-door access and clearing margin. Find sufficiently level Terrain3D and reserve trees/grass/rocks across store, lot, cars, lamps and approach; **no rebake or world placement happened in PL0B**.

**Local checks:** after pulling and building C#, open `ConvenienceStoreSite001.tscn` in the 3D editor. Confirm all **9 child scene instances** resolve and show mesh art; cars sit in painted bays with open doors, store front meets sidewalk, lamp bases are not floating, interior/rear exit stay navigable. The scene doesn't include a camera or sky; test nighttime illumination in a temporary dark test environment. **Godot runtime, visual and collision validation remain outstanding.**
