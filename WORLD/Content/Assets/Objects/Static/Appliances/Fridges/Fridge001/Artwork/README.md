# Fridge001 imported artwork contract

`Fridge001.glb` is a **genuine original glTF 2.0 binary** with embedded buffer, **796 triangles**, **11 grouped mesh nodes**, **7 opaque named PBR materials**, and no textures. Up-axis +Y, front +Z; source pivot is floor Y=0; approximate native dimensions **0.83 W × 1.87 H × 0.91 D metres**, including protruding handles. The housing itself is approximately 0.74 m deep.

**Stable paint mesh node contract (one surface each):** `FridgePaint_Shell`, `FridgePaint_UpperDoor`, `FridgePaint_LowerDoor`. `FridgeVisual.cs` finds these by name after GLB import and sets per-instance single-surface overrides from four cached material palettes. If re-exporting, **preserve exact node names and separate paint surfaces**, or update FridgeVisual deliberately. Other visual groups use the default PBR materials: `Enamel_Default`, `Enamel_Door`, `Recess_Shadow`, `Rubber_Gasket`, `Handle_BrushedSteel`, `Vent_DarkGraphite`, `Bright_MetalTrim`. Base colour is in PBR `baseColorFactor`, not only glTF vertex colours.

**Future-ready, nonfunctional door pivots:** `UpperFreezerHinge` and `LowerFridgeHinge` are glTF transform parents, each containing its paint panel, gasket and handle mesh. Both doors are closed and part of a single fixed SOC0 AutoBox; no current animations, hinges, sound, food storage or item interaction. Preserve hinges in art for future work.

**Binary validation:** source-built GLB has a correct glTF 2.0 magic/version, header and two chunk lengths, all buffer views/accessors in range, all finite vertex normals/positions, and no vertex index out of range. Full Base64 encode/decode check was used due to a past export bug in DumpsterBin001. **Godot import still requires local visual/engine validation.**
