# ConvenienceStoreSite001 — future roadside site

**Status: folder/empty scene scaffold only. PL0A not implemented.**

Scene: `ConvenienceStoreSite001.tscn` — currently a valid *empty* Node3D root, with no instantiated lot, building, cars, lamps, collisions, lighting or world placement.

**Future site composition (PL0B or later):**
```text
ConvenienceStoreSite001
  ParkingLot         instance Infrastructure/ParkingLots/ParkingLot001.tscn
  Store              instance Structures/ConvenienceStore001.tscn
  ParkedCars         instances of existing Objects/Cars/Car001/Car001.tscn
  StreetLighting     instances of Placeables/Objects/Lighting/StreetLamp001.tscn
  SiteMarkers        entrance, road connection, test player location
```

The parent owns layout/positions, **not** child art, light animation, building fades or car logic. Each child remains usable outside this site. The site should eventually be instantiated once by the appropriate world placement system; no runtime scene composition is done by this placeholder.

`Configuration/SiteMaster.tres` is a valid but empty generic Godot Resource. `Configuration/ConvenienceStoreSiteSettings.cs` is intentionally empty; neither is attached or used by the scene yet. Replace with typed configurable dimensions/relative prefab positions when the site assembly is authorized, and duplicate authoring resources before runtime tuning.

**PL0A next:** implement *only* the independent parking-lot prefab under `Infrastructure/ParkingLots/ParkingLot001/`, not the store, streetlamps, site assembly or world integration. The older original plan is `WORLD/PARKING_LOT_IMPLEMENTATION_PLAN.md` on `main`; review/adapt it to `implement-td0-td1` before implementing any phase.
