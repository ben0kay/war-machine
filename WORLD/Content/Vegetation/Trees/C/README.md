# Tree code

- `TreeSettings.cs`: Inspector-editable TreeMaster resource, including canopy budget, shape, wind, range fade and generated-mesh LOD.
- `TreeGenerator.cs`: seeded, one-shot tapered trunks/branches, connected fine sprigs, and smaller folded four-triangle leaves. A shared budget is distributed across inner/outer crown clusters. Distant variants retain a deterministic subset of leaves with larger silhouettes, without changing tree roots.
- `TreePlacement.cs`: samples saved Terrain3D heights, groups variants into chunk-local MultiMeshes, and loads/retains chunks only on explorer chunk transitions. Native Forward+ visibility ranges crossfade near/far batches and fade at the outer distance. No tree/leaf per-frame C# loop.

Generated models use two surfaces/materials, not one node per leaf or branch. Imported Mesh overrides keep their original materials and use the outer visibility fade without automatic generation of a reduced mesh.
