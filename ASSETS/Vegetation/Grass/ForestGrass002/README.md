# ForestGrass002 — tighter, shorter stylized grass

This pack is a companion grass family for War Machine, intended to match the visual language of ForestGrass001 while producing **shorter, more tightly clumped** tufts. It is suited for denser grass-bed authoring where many instances are combined without the roots feeling too spread out when seen from above.

## Contents

- 3 variants (`a`, `b`, `c`)
- 2 authored LODs per variant
- shared dark and green texture atlases
- shared wind shader/materials
- Godot preview scene
- source generator script

## Visual intent

ForestGrass002 keeps the same low-poly geometric-blade look as ForestGrass001, but:

- roots are packed much closer together
- overall heights are lower
- sway/spread is reduced
- the top-down footprint reads as a dense tuft rather than a loose starburst

## Stats

- Variant A: LOD1 90 tris, LOD2 35 tris, height about 0.457 m, footprint about 0.137 × 0.163 m\n- Variant B: LOD1 110 tris, LOD2 40 tris, height about 0.556 m, footprint about 0.238 × 0.276 m\n- Variant C: LOD1 120 tris, LOD2 45 tris, height about 0.673 m, footprint about 0.298 × 0.422 m\n

## Install

Copy the `ASSETS/Vegetation/Grass/ForestGrass002/` folder into your Godot project and let Godot import the glTF and textures.

## Next step ideas

- use it as a manually placed grass-bed asset
- add a dedicated `ForestGrass002Master.tres` later if you want it integrated into the existing grass scatter
- mix ForestGrass001 and ForestGrass002 to get both loose taller clumps and tighter bed-like filler
