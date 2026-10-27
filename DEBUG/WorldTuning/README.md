# World tuning

`WorldTuningPanel.cs` owns the active world's scrollable projection/FOV/tilt/zoom, day/night and weather controls plus clipboard export. It receives explicit camera/config/sky/weather references from WorldController. Runtime resources only; no saved master edits and no per-frame UI polling. The legacy FPS EnvironmentTuning panel remains separate and parked with that world.

Grass enabled/density/height/wind controls now target the existing TerrainGrass. Density/height commit on drag release. Copy settings includes their values. Shared wind controls update both precipitation and grass.

## Pine integration

Pine enabled/density/size/wind controls target PinePlacement; density/size commit on drag release. Copy settings includes pine values. Shared wind heading/strength update grass, weather and pines together.

The overhead panel now accepts the optional PlayerDistanceFog owner and exposes enabled, clear/full radius, opacity, edge variation and a color picker. Copy settings includes the distance-fog fields; edits invoke ApplySettings explicitly, with no UI polling. FPS continues to omit this panel/effect.
