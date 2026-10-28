# Hedge001 imported artwork

`Hedge001.glb` is an original, Godot-importable **GLB 2.0 binary**, a clipped ornamental hedge model with subtle raised leaf facets, a flat-ish neatly trimmed top, slightly bevelled top perimeter and dark thin branches at its base.

Native dimensions: **4.805m X × 2.138m Y × 1.274m Z**. Ground pivot Y=0, +Y up, long side X. **2,540 triangles, one mesh with three opaque PBR material surfaces**, no image textures or alpha blending. The screenshot-matched colour pass replaces the overly bright green material with **deep olive shadow**, **muted moss green** and **slightly lighter olive leaf tips**. Each material has an independently usable non-white `baseColorFactor`, so the three-tone appearance survives importers that do not display glTF `COLOR_0`. Neutral per-vertex brightness variations remain for subtle dappled shading where supported. All triangles, normals, dimensions, winding and ground pivot remain unchanged; roughness stays 0.96.

Import as a regular GLB PackedScene under `Hedge001.tscn -> Model/Artwork`. Do not move to `VEGETATION/` or regenerate in C# at runtime. To adjust default width/height/depth use `../Configuration/HedgeMaster.tres` rather than re-exporting the art. Physically solid collision is not implemented.

### Current three-surface glTF palette (linear PBR values)

| Primitive index | Material | `baseColorFactor` RGB | Purpose |
| --- | --- | --- | --- |
| 0 | Hedge_Deep_Leaf_Shadows | `[0.052, 0.080, 0.020]` | Recessed/darker foliage |
| 1 | Hedge_Muted_Moss_Olive | `[0.100, 0.155, 0.036]` | Dominant hedge colour |
| 2 | Hedge_Olive_Sunlit_LeafTips | `[0.154, 0.225, 0.048]` | Restrained sunlit leaf facets |

This is intentionally much darker and more yellow-olive (less blue-green) than the prior bright `[0.30, 0.52, 0.29]` material. 522, 1,386, 632 triangles respectively. Three surfaces cost two additional draw calls per visible hedge instance versus the one-surface pass, but preserve *exactly the same triangle budget*. The supplied clipped-hedge photo is a colour/lighting reference, not copied as a texture. Real-world foliage texture/density and lighting still require Godot visual assessment.

**Import validation:** restart/reimport `Hedge001.glb` and verify three mesh surfaces named above. If it still appears as one bright green or white surface, inspect `Model/Artwork` surface/material overrides and cached import data. Do not put a global colour override on the instance, which would flatten the three hues. The convenience-store site keeps its existing taller hedge setting.
