# World tuning

`WorldTuningPanel.cs` owns the active world's scrollable projection/FOV/tilt/zoom, day/night and weather controls plus clipboard export. It receives explicit camera/config/sky/weather references from WorldController. Runtime resources only; no saved master edits and no per-frame UI polling. The legacy FPS EnvironmentTuning panel remains separate and parked with that world.
