# Zombie mesh performance — ZM-PERF2B

| Phase | Goal | State |
| --- | --- | --- |
| ZM-PERF2A | Shared same-pivot/material batches and species templates | Source implemented; local profiling pending |
| ZM-PERF2B | Native renderer-selected lower-detail mesh geometry | Source implemented; awaiting local build/visual/profiling acceptance |

## Scope and ownership

C/ZombieMeshLod.cs adds one native ArrayMesh LOD index buffer to eligible one-surface meshes. C/ZombieHumanGeometry.cs supplies six-sided versions of the existing ten-sided body contours, retaining every vertical profile ring, cap, winding and UV range. Rounded incidental pieces use a shared lower-resolution sphere; boxes remain unchanged. C/ZombieVisualBatcher.cs bakes full and far geometry with the same part transforms/material grouping before attaching LOD buffers. No editor-only importer or runtime simplification dependency.

Full near geometry remains unchanged. Contour far versions contain 60% of their original triangle count; this is a geometry-count calculation, not a measured whole-zombie or FPS improvement. All pivots, palette names, independent limb visibility, wounds, gait/attacks, virtual hit bounds and ragdoll/corpse ownership stay intact. Both species reuse the same implementation and their existing cached templates. Corpses inherit the native LOD meshes without new callbacks.

This phase reduces rendered triangle cost, not mesh-instance or draw-submission count. It does not add crowd impostors, whole-actor batching, actor culling, animation throttling, shadow policy changes or AI relevance.

## Configuration and performance

Both zombie masters author MeshLodEnabled=true and MeshLodBias=1.0 under Visual mesh LOD (ZM-PERF2B). InitializeTarget applies them once; restart/reinitialize actors after editing. Disabled uses the shared original full mesh for an A/B comparison. Bias is finite/clamped to 0.1–4; higher keeps detail longer, lower reduces detail sooner.

Godot selects mesh LOD based on projected size, viewport LOD threshold, scale and bias; the index-buffer error key is 0.025 object-space units. There is no fixed metre switch. FPS distance/FOV and top-down zoom therefore both affect selection. Very close geometry stays detailed. Transition visibility needs local review.

Geometry and both index buffers are generated once per cached species template/source mesh, never per actor or distance transition. Native LOD uses no added node, timer, physics query, managed distance check or frame callback. Configuration adds one spawn-only tree walk; thereafter nothing in this helper processes. Geometry caches are intentionally process-lifetime and shared. Extra near/far vertex/index storage and retained full-mesh references trade modest shared mesh memory/startup generation for cheaper distant triangles; per-actor node count is unchanged.

## Brief validation / handoff

Source/diff/API/loop checks only. No dotnet or Godot executable is installed here: compilation, rendering and measured performance are unverified. No new scene or broad test suite was added.

1. Run `dotnet build war-machine.csproj` with the compatible Godot .NET SDK.
2. F6 WORLD/FirstPerson/FirstPersonWorld.tscn. Compare the same crowd/camera at near/mid/far ranges with MeshLodEnabled true/false (restart between comparisons). Check rendered primitives, frame time, draw calls, seams, UVs and switching. Do not expect fewer mesh draw submissions solely from this phase.
3. Check both first/template-copy shamblers and runners, clothing variation, missing limbs, swipe/locomotion, death and settled articulated poses. Existing render-hide/shadow diagnostics should still work. Check secondary World.tscn at multiple zooms.
4. Try bias 0.5 and 2.0 to assess tuning; restore 1.0 afterward. No idle managed update or per-actor geometry allocation should appear in profiler traces.

Single job commit on implement-td0-td1; see Git history. Local acceptance remains pending; later performance phases are not authorized by this implementation.
