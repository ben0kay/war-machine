# WheelieBin001 artwork

`WheelieBin001.glb` is an original embedded-buffer **glTF 2.0 binary**, **1,092 triangles, 8 grouped meshes and 7 opaque PBR materials**, with no external textures or licenses. Approximate native bounds are 0.68m W × 1.02m H × 0.85m D, +Y up, +Z front, Y=0 ground.

The root `WheelieBin001` has separate body, trim, wheel and axle meshes plus `LidHinge` (pivot at `(0, 0.951, -0.365)`) with three separate lid/hinge detail mesh children. Preserve `LidHinge` node and its local transforms when re-exporting. **No actual lid-open animation** or moving collider is provided.

Material names: `Bin_DarkForestGreen`, `Bin_GreenShade`, `Bin_GreenHighlight`, `Lid_MatteBlack`, `Lid_RimBlack`, `Wheel_Rubber`, `Axle_DarkGray`. Colours are proper glTF PBR **baseColorFactor** values; they do not depend on optional vertex-colour import. No baked collision meshes: the parent .tscn uses existing SOC0 AutoBox on world instantiation. Godot import and visual acceptance pending.
