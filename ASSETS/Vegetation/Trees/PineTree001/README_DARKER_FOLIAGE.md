# PineTree001 — foliage colour tuning (2026-10-31)

This archive keeps ForestGrass001 and the PineTree001 shape/LOD structure unchanged.
The foliage `COLOR_0` vertex RGB has been multiplied by (0.88, 0.82, 0.74) in
LOD1, LOD2 and LOD3 to darken the bright blue-green leaf canopy slightly
and shift it toward a more olive woodland green.

Only the foliage primitive colours were edited. Bark vertices/materials, mesh positions,
UVs, normals, triangle counts, pivot, foliage alpha, original shader resources,
and all ForestGrass001 files are byte-identical to the source ZIP.

The PineTree001 foliage shader already uses COLOR.rgb and the existing shared F1
forest palette. This tint remains effective even if you inspect the GLTF directly
in Blender with vertex colours. It should remain compatible with your palette controls,
but the final look will depend on in-game lighting and shader assignment.

Install: back up the original PineTree001 folder, merge `Trees/PineTree001` into
`ASSETS/Vegetation/Trees/PineTree001` in the project. The Grass folder
is included unchanged for convenience, but need not be reimported. Let Godot
reimport the three changed GLTFs, then compare in the same lighting/view.

If it is too dark, restore the original from the source ZIP or use a lighter multiplier
(e.g. 0.95, 0.91, 0.87). No tests were run inside Godot.
