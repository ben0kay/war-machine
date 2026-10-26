# Procedural trees — layered canopy preview

The trees in `WORLD/testworld.tscn` use deterministic C# meshes and saved Terrain3D heights. This is replaceable preview artwork, with no collision, harvesting, gameplay simulation or terrain/save mutation.

- `C/`: generator, settings and chunk placement.
- `Configuration/TreeMaster.tres`: editable shape, density, leaf budget, GPU wind, quality and distances.
- `Shaders/LeafWind.gdshader`: shared, compilation-checked leaf-tip flutter. Zero wind strength/speed bypasses the wind branch without an invalid processor return.
- `Artwork/`: future authored models.
- `Validation/`: checks, rendered evidence and remaining target-PC profiling.

## Tree fixes

Smaller **folded** leaves are attached along connected fine sprigs, supported by secondary twigs and lower main branches. Inner and outer crown clusters form fuller layers without opaque canopy balls. TreeMaster controls leaf size, twigs, inner canopy fullness and a hard per-variant leaf budget. Four seeded variants share two material surfaces each. Wind remains on the GPU with fixed attachment bases and moving tips; no per-leaf nodes, scripts or physics.

A cheaper generated far mesh retains a stable subset of leaves and coarser leaf silhouettes. Native Forward+ geometry ranges blend near/far levels around 110m and fade the outer range around 240m, with configurable margins. Distant shadows default off. MultiMesh roots are local to a chunk anchor so native range culling measures the correct world position; near/far levels share identical root transforms. Wind culling margin avoids clipping leaf tips. Chunk cache retention reduces boundary reload thrashing and covers the camera's movement inside a chunk.

## Configuration / model replacement

Edit TreeMaster and relaunch, or call `GetNode<TreePlacement>("Trees").ApplySettings()`. Shared wind direction/strength comes from EnvironmentMaster at settings application. Density=0 or Enabled=false removes both mesh levels. Height-map changes coalesce into an explicit rebuild; tree placement never regenerates saved terrain.

Add Mesh resources to ImportedMeshes to replace the generator. Imported materials remain untouched; they do not automatically receive procedural wind or generated LOD. Imported meshes still use native outer range fading. A full .glb scene may need its Mesh resources extracted; interactive scene trees require a separate future system.

## Performance / status

Activation: world initialization, explorer chunk crossing, explicit settings application and terrain-height changes. Sleep: no tree `_Process` or `_PhysicsProcess`; stationary and sub-chunk movement do not rebuild batches. Off removes all instances. Active GPU wind, native culling/fading and enabled shadows remain rendering costs. The visible FPS counter is a separate, throttled diagnostic.

Native range fades temporarily overlap mesh levels. More detailed near foliage costs more geometry than the old sparse trees; no FPS improvement is claimed. Leaf budget, far sampling, render range and shadows are tunable. Trees remain stylized, untextured preview art rather than final oak models. Imported mesh support is preserved.

**Implemented and validated under Godot 4.5.1 .NET, including software Vulkan renders. Target-PC visual/performance acceptance remains pending.** See [validation](Validation/README.md). This separate requested refinement does not advance WE4 or any other environment-plan phase.
