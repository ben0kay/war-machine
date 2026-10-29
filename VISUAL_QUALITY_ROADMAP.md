# War Machine — Visual Quality Roadmap

> **Status: High-level art direction / future roadmap, not an approved implementation task.**  
> **Primary reference:** First-person view in `WORLD/FirstPerson/FirstPersonWorld.tscn`; the shared top-down view should remain functional.  
> **Goal:** Raise environmental believability, cohesion and atmosphere while staying practical for a solo Godot 4 C# project and performance-conscious hardware.

## Vision

Make the world feel like a real, neglected place: convincing woodland, textured soil and roads, worn structures, believable materials, natural transitions and purposeful light. Aim for the *grounded atmospheric quality* associated with games such as **Road to Vostok**, not a copy of their assets, locations or art. Strong composition and material consistency matter more than raw polygon counts or stacking expensive effects.

The priority is **one polished, walkable FPS scene first**, not a global graphics overhaul. Keep an authored, eerie sci-fi survival identity: recognisable Earth, abandoned town edges, deep forest, mist and occasional unsettling silhouettes.

## Starting point (source review, 2026-10-29)

- The active project already uses **Terrain3D**, **Sky3D**, **Forward+**, a shared environment, GPU ribbon grass, instanced/chunked pines, rocks, weather, fog/haze and local lights. This is a useful foundation—not evidence that final art quality has been achieved.
- `WORLD/Terrain/TerrainMaterial.tres` currently uses a demo-derived Terrain3D autoshader and a limited ground-material presentation. Ground art direction, texturing and small natural details remain major opportunities.
- The live `VEGETATION/Trees/Pine/Configuration/PineMaster.tres` currently sets `UsePineGlb=false`: procedural pine visuals are selected, despite the pine README describing the imported `PineTree001.glb` as the default. Confirm actual in-game art selection before a visual comparison; don't assume the two versions are already being compared. Existing spacing is roughly **20 m**, with a **96 m** authored render distance.
- The dense forest work has only **DF0** (proposed test-area foundation) and **DF1** (isolated tall canopy-tree preview) in source, awaiting local validation. **DF2–DF5**—population, canopy shading, forest-floor detail and performance—are still planned. A preview tree is not a populated forest.
- Rendering, mist and lighting have multiple existing owners. Improve their authored results, rather than introducing a parallel sky, environment, tree spawner, terrain or fog stack.

These are source/configuration observations, **not a measured screenshot or runtime graphics audit**. Actual visual defects, FPS and priorities must be checked in the editor.

## Major improvement areas

### 1. A small visual-quality benchmark

Build **one approximately 80 × 80 m representative outdoor location**: a woodland road edge, dense tree transition, convincing terrain, a small abandoned structure and a readable walking route. Reuse existing world/scene systems. Capture fixed FPS views in overcast daylight, low-angle light and at night, plus a brief top-down compatibility check.

Treat this as a visual laboratory for art, lighting and optimization. Do not scatter partially finished improvements across the entire world before a strong local result exists.

### 2. Trees, canopy and undergrowth

- Prioritize convincing trunk silhouettes, realistic bark/foliage materials and multiple distinct tree shapes, sizes and ages. Use procedural art where it works; use imported GLBs and textures where they materially improve the look.
- Layer vegetation naturally: tall trees, occasional younger trees, understory, ferns/shrubs, grass, fallen limbs and leaf litter. Group by light, moisture, slope and forest edge rather than uniform random scatter.
- Create actual overhead enclosure and *selective* canopy shadowing, allowing irregular gaps and depth instead of a flat dark ceiling.
- Keep chunked MultiMesh batching, simple trunk collision, sensible near/far art, culling and restrained shadow casters. More trees is not automatically better.
- Continue through the existing [DF forest roadmap](VEGETATION/Forest/FOREST_IMPLEMENTATION_PLAN.md); this document does not authorize or replace DF phases.

### 3. Terrain and forest-floor materials

- Establish a coherent palette of believable dirt, soil, gravel, road wear, exposed rock, grass and leaf litter; pay attention to **albedo, roughness, normal detail, texture scale** and colour consistency.
- Improve macro/micro variation and natural material transitions. Blend road shoulders, building edges, clearings and forest into the ground instead of leaving obvious hard boundaries or repeating texture patterns.
- Add small but intentional surface detail: embedded stones, roots, debris, fallen branches, damp patches and subtle depressions. Reuse inexpensive batched scatter wherever possible.
- Existing terrain/geology and surface work remains with [WORLD/SURFACE_IMPLEMENTATION_PLAN.md](WORLD/SURFACE_IMPLEMENTATION_PLAN.md). Do not automatically rebake or overwrite saved Terrain3D data.

### 4. Lighting, colour and atmospheric depth

- Develop a believable **overcast daylight** reference first: balanced sky/sun contribution, subdued contrast, restrained saturation and readable surface materials.
- Then tune directional light, soft shadows, ambient contribution, exposure/tonemapping and carefully graded distance mist. Trees and buildings should have convincing depth without crushing interior detail.
- Preserve the established Sky3D/day-night/weather ownership. Existing fog, ground haze and volumetric rays should complement art, **not disguise low-detail assets or abrupt render cutoffs**.
- Evaluate any SSAO, SSIL, SDFGI or volumetric adjustments on target hardware before treating them as defaults; use only effects with visible benefit worth their cost.

### 5. Buildings, roads and reusable prop quality

- Make a small set of hero locations convincingly authored: consistent proportions, weathering, believable glass/metal/wood/concrete, dirt around foundations, purposeful clutter and exterior-to-interior lighting transitions.
- Standardize material quality and texel density for new GLBs while keeping geometry efficient and variants reusable. Reuse the existing object authoring/collision conventions rather than replacing gameplay assets wholesale.
- Prioritize believable asset silhouettes, materials and composition over thousands of unique props.

### 6. Efficient art pipeline and measurable quality

- Keep procedural C# geometry and future imported art interchangeable under existing stable prefabs. Use a repeatable materials/asset review: scale, normals, UVs, roughness, shadows, distance appearance, LOD and collision policy.
- Continue shared meshes/materials, MultiMeshes, per-chunk ownership, distance culling, GPU grass and limited shadow costs. Avoid per-object idle processing or huge always-resident forests.
- Use **1080p on the GTX 1080-class target** as an initial benchmark context. Aim for a smooth **~60 FPS visual test area**, but treat that as an aspiration—not a claim about current performance or a guarantee for zombie-heavy gameplay. Record frame times, draw calls and GPU/CPU cost with identical camera/weather/content before and after.
- Preserve the project's first-person priority and basic top-down readability. Every improvement should have a clear revert/quality toggle where practical.

## Suggested sequence (direction, not implementation phases)

1. **Baseline:** capture the current FPS forest/road scene under fixed lighting, note visibly weak materials, art density, shadows and frame times; verify whether procedural or GLB pines are actually active.
2. **Establish art targets:** collect a compact reference set and choose representative ground, trunk, leaf, building and road materials. Decide what stays generated versus what needs textured/imported art.
3. **Make one excellent patch:** improve the benchmark terrain, two or three convincing tree variants, coherent undergrowth and one small roadside location. Use existing DF/SF ownership.
4. **Light and grade it:** tune one overcast preset and one low-sun/night view, keeping distant fog and canopy shadows believable and interiors readable.
5. **Profile and simplify:** compare the fixed captures, LODs, canopy shadows, vegetation density, shader cost and frame times. Only then decide which treatments scale into the larger world.
6. **Expand deliberately:** turn the accepted assets/presets into reusable scene/content guidance for later village, forest and wilderness art passes.

## What “good enough to expand” means

- At eye level the scene reads as a coherent place, not repeated placeholder trees on a flat-textured ground.
- Nearby, medium and distant views look credible; no distracting terrain repeats, pop-in, shadow artifacts, obvious tiling or foliage cutoffs under intended fog.
- An overcast screenshot and a low-light screenshot both retain readable movement/combat silhouettes and plausible outdoor/indoor transitions.
- Changes have been compared at **fixed viewpoints and settings**; costs are measured rather than inferred.
- The patch can be reused without rewriting the shared world, vegetation, physics or gameplay systems.

## Boundaries and related documents

This is an **aspirational visual roadmap only**. It does **not** authorize changes to assets, shaders, saved terrain, project settings, rendering defaults, gameplay or global scene composition. Work should later be split into small, separately approved tasks in the relevant owning folders.

- [Dense high-canopy forest phases](VEGETATION/Forest/FOREST_IMPLEMENTATION_PLAN.md)
- [Vegetation foundation](VEGETATION/README.md) and [pine art/placement](VEGETATION/Trees/Pine/README.md)
- [Terrain and geology roadmap](WORLD/SURFACE_IMPLEMENTATION_PLAN.md) and [terrain resources](WORLD/Terrain/README.md)
- [Shared environment/lighting](ENVIRONMENT/SHARED_ENVIRONMENT_README.md)
- [Object authoring guide](WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md)
- [Rendering/culling policy](WORLD/Rendering/README.md)

**Guiding principle:** *Make a small area look outstanding, prove it runs well, and only then scale the art direction to the world.*
