# PineTree001 — imported GLB source artwork

[**PineTree001.glb**](PineTree001.glb) is a standalone original **binary glTF 2.0** conifer designed as a possible visual alternative to the active procedurally-generated trees. It is not a texture, thumbnail, shader, image, or C# mesh generator.

- **Native size:** approximately 7.97m X width × **14.37m height** × 8.10m Z depth. Root at local Y≈0; +Y up. The lower exposed trunk is about 3–4m tall before the first broad whorl.
- **Shape:** tapered segmented brown trunk, five root buttress accents, 60 angled/tapered woody branches across 10 irregular tiers and 171 elongated 3D needle tufts. Branches/tufts deliberately vary to avoid a perfectly symmetrical Christmas-tree silhouette.
- **Complexity:** **7,679 triangles in 5 opaque material surfaces**, one glTF mesh. Materials in order: `BarkBase`, `BarkHighlights`, `NeedlesShadow`, `NeedlesForest`, `NeedlesSunlit`. No external textures; baked PBR base colour/roughness only.
- **No** skeleton, sway/wind animation, billboard/cross-card planes, terrain placement, GPU instancing, LOD or built-in physics. The visual preview does not yet support the active pines' camera-fade shader.

**Performance warning:** This is a higher-detail visual reference. A 7.7K-triangle static model should **not** be adopted directly for every tree in a large streaming forest without profiling and a lower-detail LOD/instanced variant. Existing `VEGETATION/Trees/Pine/` multi-mesh renderer is optimized separately and must remain unchanged.

Scale can be adjusted using `../Configuration/PineTreeMaster.tres`. The root scene imports this exact GLB under `Model/Artwork` so it can be previewed without changing the active forest.
