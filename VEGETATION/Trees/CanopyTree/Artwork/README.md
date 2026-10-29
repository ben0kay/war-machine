# Canopy tree replacement artwork

**DF1:** No external/imported model exists yet. The authored tree is procedurally generated in [../C/CanopyTreeMeshBuilder.cs](../C/CanopyTreeMeshBuilder.cs). This folder is reserved for future original GLB, textures and independent imported art, if the generated broadleaf shape is visually insufficient.

Keep the existing `CanopyTree.tscn`/settings contract stable. DF2 should use shared `ArrayMesh` variants via `MultiMesh`, not allocate imported scene instances per individual tree. Do not import user screenshot/reference material as a game texture.
