# ConvenienceStoreSite001 — future roadside site

**Status: folder/empty scene scaffold only. See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for PL0A–PL4 (next: PL0A).**

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

**PL0A next:** implement *only* the independent parking-lot prefab under `Infrastructure/ParkingLots/ParkingLot001/`, not the store, streetlamps, site assembly or world integration. The current-branch, adapted [implementation plan](IMPLEMENTATION_PLAN.md) is now the canonical PL roadmap; the original main-branch version is historical only.
