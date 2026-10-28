# PineTree001 — imported GLB source artwork

[**PineTree001.glb**](PineTree001.glb) is a standalone original **binary glTF 2.0** conifer designed as a possible visual alternative to the active procedurally-generated trees. It is not a texture, thumbnail, shader, image, or C# mesh generator.

- **Native size:** approximately 7.97m X width × **14.37m height** × 8.10m Z depth. Root at local Y≈0; +Y up. The lower exposed trunk is about 3–4m tall before the first broad whorl.
- **Shape:** tapered segmented brown trunk, five root buttress accents, 60 angled/tapered woody branches across 10 irregular tiers and 171 elongated 3D needle tufts. Branches/tufts deliberately vary to avoid a perfectly symmetrical Christmas-tree silhouette.
- **Complexity:** **5,969 triangles in 5 opaque material surfaces**, one glTF mesh. Materials in order: `BarkBase`, `BarkHighlights`, `NeedlesShadow`, `NeedlesForest`, `NeedlesSunlit`. No external textures; baked PBR base colour/roughness only.
- **No** skeleton, sway/wind animation, billboard/cross-card planes, terrain placement, GPU instancing, LOD or built-in physics. The visual preview does not yet support the active pines' camera-fade shader.

**Performance warning:** This is a higher-detail visual reference. A ~6K-triangle static model should **not** be adopted directly for every tree in a large streaming forest without profiling and a lower-detail LOD/instanced variant. Existing `VEGETATION/Trees/Pine/` multi-mesh renderer is optimized separately and must remain unchanged.

Scale can be adjusted using `../Configuration/PineTreeMaster.tres`. The root scene imports this exact GLB under `Model/Artwork` so it can be previewed without changing the active forest.
 
## PINE-OPT1 — Less geometry per needle tuft (2026-10-28)

The source pine originally contained **7,679 triangles**: **839 woody triangles** (trunk, branches and roots) and **6,840 foliage triangles** distributed across **171 elongated needle tufts**.

The current `PineTree001.glb` preserves **all 171 tufts**, their orientation, widest cross-section, original pointed tip, all woody geometry and the same **five opaque PBR material surfaces**. It simplifies each tuft from **five cross-section rings to four**, eliminating the second-to-last intermediate ring and reconstructing the final needle section with correct winding and flat face normals.

| Geometry | Original | Optimized |
| --- | ---: | ---: |
| Trunk/branches/roots | 839 tris | 839 tris |
| Needle tufts | 6,840 tris | **5,130 tris** |
| **Whole tree** | **7,679 tris** | **5,969 tris (-22.3%)** |
| Binary GLB size | 603,168 bytes | 470,472 bytes |
| Material surfaces | 5 | 5 |

This is a conservative reduction of **actual GPU-drawn triangle work** when the whole tree is instanced. It doesn't add GPU leaf-level instancing, extra nodes/material surfaces, textures or new physics. The earlier full-detail GLB remains recoverable from Git history.

**Why not make each tuft a separately instanced model?** Whole-tree `MultiMesh` already shares its mesh efficiently between trees. Splitting each leaf into individually drawn meshes adds more batches/material management unless a truly shared leaf mesh and an efficient instancing strategy are engineered. Using reused leaf motifs is a useful *modelling technique*, but putting repeated geometry into one baked GLB does not reduce drawn triangles by itself.

The GLB binary, accessors, material order, face winding, triangle counts and source topology were checked while editing; exact Godot import, shading, visual silhouette and FPS await local evaluation. Imported world-pine shaders and forest spawner remain untouched.
