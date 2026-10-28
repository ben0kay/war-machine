# Procedural pine mesh

PineMeshBuilder.cs only constructs seeded trunk, asymmetrical branch and layered two-sided needle-spray geometry, including a wider-spray but cheaper far LOD. General placement/settings are one folder up. Artwork/ is reserved for future imported models.

PINE-V1 tuning: `BranchIrregularity`, `BranchDroop`, `NeedleSpraySpread`, `FoliageLayering` in PineMaster. Generator adds no extra material surfaces or per-instance callbacks. Validate close/distant silhouettes in Godot before visual acceptance.
