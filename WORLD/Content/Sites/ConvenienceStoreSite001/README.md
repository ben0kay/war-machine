# ConvenienceStoreSite001 — future roadside site

**Status: site assembly still empty. PL0A reusable [ParkingLot001](../../Infrastructure/ParkingLots/ParkingLot001/README.md) is implemented separately, awaiting Godot validation; next planned phase PL0B. See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).**

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

**PL0A delivered:** the independent parking-lot prefab now has asphalt, bays, curbs and markers. It is intentionally **not** instanced into this site. PL0B will compose the store, lamps, cars and the lot into one reusable scene; PL1 will place that scene in the world. The current-branch, adapted [implementation plan](IMPLEMENTATION_PLAN.md) is now the canonical PL roadmap; the original main-branch version is historical only.

**PL0B progress:** [ConvenienceStore001](../../Structures/ConvenienceStore001/README.md) is now authored independently, with big empty windows, retail shelves, checkout and stockroom/rear exit. Parent site and lamp prefab remain untouched. No world placement has occurred.
