# Rock procedural geometry

`RockGenerator.cs` builds **nine shared low-polygon meshes** (one per silhouette) during `ContentScatter.ApplySettings()`, not once per placed rock. The existing separate `ContentScatter` RNG determines seeded world placement, so shape-generation random changes do not move rocks around. Meshes are batched with MultiMesh and receive one shared stone shader; convex hulls are cached once per variant for active-world static collisions.

## R1 — More natural crowns and chipped facets (2026-10-28)

The generator keeps its nine rock profiles and the **same number of triangles** (8 triangles per polygon side, generally 7–11 sides per mesh). The source's old high central rock crown made many rocks resemble tidy domes/cones. New crown and shoulder proportions reduce the peak, particularly on low slab/outcrop variants; angular/upright variants receive one chipped top-edge corner and slightly uneven shoulder facets.

Editable in `../Configuration/GameplayRockMaster.tres` (active World/FPS) or `../Configuration/RockMaster.tres` (parked TestWorld):

- `RockCrownFlatness = 0.65` (0 = original peaked centre; 1 = broader, flatter and gently uneven crown).
- `RockAngularVariation = 0.55` (0 = minimal extra chip/shoulder distortion; 1 = visibly jagged/broken edge). 

These are **one-time mesh generation parameters**; changing the `.tres` requires world restart or explicitly calling `ContentScatter.ApplySettings()`. No per-instance CPU callbacks, procedural texture allocations, added material surfaces, scene nodes, extra physics shapes or new rock spawn logic. Collider hulls automatically derive from the newly generated variant meshes. Moss, surface normals, roughness and normal-map triplanar shader remain unchanged.

The triangle count and material sampling cost remain unchanged per rock; only vertex locations and baked normals change. **Local Godot .NET build, visual assessment and collision checks are pending.** Test near the village/rocks in both camera views, check natural flat/sloped tops, ensure rock hulls still block correctly, and verify safe approach to buildings and doorways.
