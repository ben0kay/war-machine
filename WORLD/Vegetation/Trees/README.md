# Procedural trees — first visual pass

- `C/TreeGenerator.cs`: deterministic, low-poly oak trunks, tapered branches and **hundreds of individual pointed leaves** in two mesh surfaces (wood + shader-driven leaf polygons).
- `C/TreeSettings.cs`: editable resource settings for size, crown, density, slope, patch noise, clearing, distance and seed. Can accept imported Mesh resources later.
- `C/TreePlacement.cs`: samples the existing Terrain3D **height map**; groups trees into chunked MultiMesh instances; streams on explorer chunk transitions.
- `Configuration/TreeMaster.tres`: all current tree tuning in Godot's Inspector.
- `Shaders/LeafWind.gdshader`: shared vertex shader for independent leaf-tip flutter and coherent breeze using EnvironmentMaster wind direction. No leaf scripts/physics.
- `Artwork/`: future authored `.glb` tree models; no model art is bundled with this pass.

## Try in testworld

Open `WORLD/testworld.tscn` (F5 runs it as the main world). The `Trees` node gets the terrain and explorer after they initialize, and a seeded forest appears immediately on suitable slopes. Use `WORLD/Vegetation/Trees/Configuration/TreeMaster.tres` to tune leaves per cluster, length/width, wind strength, speed, flutter and overall forest density, then restart. Wind direction and shared environment wind strength come from `ENVIRONMENT/Configuration/EnvironmentMaster.tres`. To hot-reapply via code call `GetNode<TreePlacement>("Trees").ApplySettings()`.

To swap in authored meshes later, add imported **Mesh** resources to `TreeMaster.tres -> ImportedMeshes`. Those meshes replace the C# generators without changing placement, chunk culling or settings. A `.glb` containing a complete scene may need its constituent meshes exposed or extracted first; scene-based interactive trees require a separate near-player system.

## Runtime and limits

Tree placement is deterministic from seed and chunk position. Each chunk samples terrain once when loaded; nearby chunks are tracked while moving and discarded when distant. Instances are batched by mesh variant using `MultiMeshInstance3D`. There is **no per-tree Node, collision, interaction or save mutation**. Leaf movement is entirely GPU vertex shading, using UV.y attachment weights; no per-leaf Nodes, particles or per-frame C# updates. Those can come later, in their own authorized phase. Do not use this visual-only tree renderer for harvesting yet.

Activity: world initialization, crossing a chunk boundary, explicit settings changes, or a terrain height-map change. Sleep: standing still or disabled. Remaining cost: normal native rendering, shadows and culling for visible instances; no idle C# tick. Each generated oak now contains hundreds of separate **opaque leaf diamonds** rather than solid green polyhedra. The canopy is still an intentionally stylized no-texture preview, not photorealistic leaf art. GPU wind continues while the trees are visible; idle **CPU** cost is zero. Imported Mesh overrides keep their own materials and do not automatically receive the generated-tree wind shader. High densities, shadows and leaf counts can be GPU expensive; measure on the target PC.

## Local validation (awaiting)

Run `dotnet build`, `godot --headless --editor --path . --import`, and `godot --headless --path . --quit-after 180` with a Godot .NET editor. The project declares 4.7 features while its .csproj pins the 4.5.1 SDK. Check natural canopy openness, sunlight through leaves, wind sway, zero-strength static leaves, seed-stable appearance, tree bases at terrain height, dense/empty forest patches, clear spawn space, uphill placement, distance streaming, reproducible tree positions after restart, and no movement work while stationary. Turn `Enabled` off or set `Density` to 0 and confirm all tree instances disappear; restore and confirm they return. Validate imported-mesh override separately.

**Status: Individual-leaf wind refinement implemented, awaiting local compilation/visual/performance validation.** No phase status in `WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md` advances because this is a separate authorized tree preview ahead of WE4.
