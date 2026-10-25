# Vegetation configuration

Select `VegetationMaster.tres` in Godot's Inspector for grouped quality, coverage and appearance controls. This Resource is defined by `../VegetationConfig.cs`. Settings apply on scene launch or an explicit `TerrainGrass.ApplySettings()` call. Density/Enabled=0 completely removes GPU emitters; reapplying nonzero values restores the pool. Wind is owned by EnvironmentMaster. See the parent README for units, quality multipliers and budget behaviour.
