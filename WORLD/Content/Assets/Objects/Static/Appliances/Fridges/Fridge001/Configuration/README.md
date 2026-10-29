# Fridge001 configuration

`FridgeMaster.tres` reuses shared `PlaceableSizeSettings` with `SizeMultiplier = Vector3(1,1,1)`; X is relative width, Y height, Z depth (not metres). `PlaceableVisual` applies this once, and SOC0 auto-generates one matching static box at runtime. `FinishVariant` lives on the `Fridge001.tscn` / `FridgeVisual.cs` root, not here, and is independently selected per instance through F1. No per-object tick or geometry generator.
