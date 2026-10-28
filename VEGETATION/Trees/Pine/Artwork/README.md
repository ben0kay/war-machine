# Pine artwork source / replacement ownership

`C/PineMeshBuilder.cs` still owns the original generated pine mesh. This folder remains available for additional pine-specific imported artwork later; do not duplicate the canonical catalogue GLB here.

**Current comparison source:** `WORLD/Content/Assets/Objects/Static/Vegetation/Trees/PineTree001/Artwork/PineTree001.glb` is referenced directly by `Configuration/PineMaster.tres` and extracted once in `PinePlacement`. `UsePineGlb=true` switches all forest visuals to this one GLB mesh while preserving seeded terrain placement, MultiMesh, exclusion areas, collision, wind and camera-fade adaptation. `UsePineGlb=false` restores original generated geometry.

This is an evaluation toggle, not a separate forest spawner. The F1 catalogue scene and PineTreeMaster are unchanged; Godot/import performance testing pending.
