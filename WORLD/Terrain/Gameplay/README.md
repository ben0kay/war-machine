# Active gameplay terrain

Native Terrain3D regions for WORLD/World.tscn. The runtime uses WorldSettings.TerrainDirectory. An existing user-local WORLD/TopDownWorldTest/Terrain/Data/*.res bake is copied here on first launch if this directory has no regions; originals are retained. No regeneration is required for this cleanup.

Fresh setup only: run WORLD/Generation/BakeGameplayTerrain.tscn once. Generation refuses to overwrite existing data. This directory never targets saved legacy wilderness terrain.
