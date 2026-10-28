# Hedge001 imported artwork

`Hedge001.glb` is an original, Godot-importable **GLB 2.0 binary**, a clipped ornamental hedge model with subtle raised leaf facets, a flat-ish neatly trimmed top, slightly bevelled top perimeter and dark thin branches at its base.

Native dimensions: **4.805m X × 2.138m Y × 1.274m Z**. Ground pivot Y=0, +Y up, long side X. **2,540 triangles, 1 primitive/surface, 1 opaque green PBR material**, no image textures or alpha blending. The material's base colour is **evergreen green** (`baseColorFactor=[0.30, 0.52, 0.29, 1]`) rather than white, so renderers that ignore glTF vertex colours still display green. `COLOR_0` remains as normalized neutral brightness values (roughly 70–99%) for darker/lighter leaf variation when supported. High roughness and the original geometry remain unchanged.

Import as a regular GLB PackedScene under `Hedge001.tscn -> Model/Artwork`. Do not move to `VEGETATION/` or regenerate in C# at runtime. To adjust default width/height/depth use `../Configuration/HedgeMaster.tres` rather than re-exporting the art. Physically solid collision is not implemented.
