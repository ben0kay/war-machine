# ForestGrass001 — UV-fixed drop-in replacement

This pack is a **drop-in replacement** for `ASSETS/Vegetation/Grass/ForestGrass001/`.
It preserves the existing War Machine pipeline shape: three A/B/C variants, two authored LODs each,
one shared opaque material surface per model, and the same material/shader file names already used by
`StylizedGrassPlacement.cs`.

## What was changed

The visible issue in the original asset set was an **underlying root-to-tip UV/atlas mismatch**: blade roots sampled
lighter atlas regions while many tips sampled darker regions. In this replacement pack, the grass models are authored so
**roots sample lower V = dark atlas values** and **tips sample higher V = lighter atlas values**. No height-darkening fix was added in the shader.

## Included models

- Variant A: LOD1 75 tris, LOD2 20 tris, height about 0.816 m
- Variant B: LOD1 80 tris, LOD2 25 tris, height about 0.917 m
- Variant C: LOD1 75 tris, LOD2 25 tris, height about 0.998 m


## Install

1. Back up your current `ASSETS/Vegetation/Grass/ForestGrass001/` folder.
2. Copy the included `ASSETS/Vegetation/Grass/ForestGrass001/` folder into your project, replacing the old one.
3. Let Godot re-import the assets.

## Notes

- The active grass shader/material remain compatible with the existing code path.
- This pack was authored from scratch to match the existing low-poly geometric blade style and fix the underlying root/tip colour logic.
- No C# or repository files were modified here; this is an asset drop-in only.
