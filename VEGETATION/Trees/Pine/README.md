# Pine trees

Reusable pines moved from the parked FPS world's content folder. `PinePlacement.cs` owns seeded terrain placement and MultiMesh batches; `PineSettings.cs` / `Configuration/PineMaster.tres` own shared defaults. **C/PineMeshBuilder.cs** constructs the original procedural mesh. Future imported models belong in **Artwork/**. Leaf wind shader lives under Shaders/. There is no duplicate FPS version.

## PINE-V1 — layered procedural foliage polish

**Implemented in source, awaiting local Godot validation.** The procedural forest now varies branch counts (five to seven per tier), tier/branch angles and lengths, and droop for an asymmetric silhouette. Needles grow as overlapping diamond-shaped, **two-sided opaque sprays**, darkening interior/tier foliage and slightly brightening outer sprays. This replaces identical pointed cone tufts. Each foliage spray carries the original UV.y wind weight, with **one leaf material and one bark material per mesh**; no new textures, alpha blending, individual scene nodes, or idle callbacks.

The existing seeded **MultiMesh**, 32m chunk streaming, distance culling, generated **near and far LOD** variants, trunk collision, foliage camera fade, and shared wind updates are unchanged. Both LODs share exactly the same major trunk/branch skeleton; the far LOD produces fewer but broader spray clusters using the existing FarLeafStride setting. Normal defaults stay **48m LOD**, **96m ground render range**, **three shared variants** and **shadows off**. Camera height/zoom offsets still apply. The LeafBudget remains an approximate desired *cluster count*, capped at 1,200 after density scaling to prevent excessive geometry at extreme settings. At default settings each near cluster creates four or eight leaf triangles; distant clusters use eight, but far fewer clusters. Changing the mesh never requires a terrain rebake.

**New PineMaster controls:** BranchIrregularity=0.55; BranchDroop=0.20; NeedleSpraySpread=0.55; FoliageLayering=0.85. Adjust defaults in Configuration/PineMaster.tres and reload, or use an existing explicit ApplySettings route. Wind strength and speed still use the shared material; no new per-frame C# work. PineTree001.glb remains a separate F1 visual comparison, not a world replacement.

**Local check:** `dotnet build war-machine.csproj`; F6 WORLD/World.tscn and inherited FPS debug. View pine silhouettes from overhead and eye level, inspect dark/bright needle layering, camera obstruction fading behind trees, near/far transition, render cutoff, trunk collisions, forest chunk crossings and wind at nonzero/zero values. Test F9 pine OFF/ON, density/size, variant changes, and compare FPS/triangle stats with an unchanged viewpoint and tree count. Godot render/shader compilation and real frame rates are **not** yet validated.

## Overhead defaults

Three shared shape variants, **12–18m** height with seeded per-instance scale/yaw; **7 branch tiers**, **320 leaf budget**, generated far LOD; **96m** ground placement range, 32m chunks, density **0.55**, spacing **20m**, spawn clearing **12m**, shadows off. Trunks keep their corrected closed surfaces, double thickness and seeded 0.85–1.25 variation. Layered diamond sprays now replace the original pointed tufts, while keeping the budgeted near/far geometry. Gentle shared wind remains; fine flutter defaults to zero and skips its extra sine. No per-tree/leaf scripts.

WORLD/WorldController supplies its saved terrain, player anchor, region size and spawn. Crossing a chunk boundary loads/evicts deterministic nearby batches; standing still has no new C# update loop. Native frustum/range culling and generated LOD do the drawing work. Projection/distance changes adjust existing native visibility ranges to account for the elevated camera without regenerating meshes. Shared wind changes update one foliage material. Terrain height changes trigger one deferred resample. OFF/density zero frees batches; subscriptions and noise resources clean up on unload. There is no measured FPS claim.

The active tuning panel has pine enabled, density, size multiplier and wind. Density/size apply on slider release to avoid repeated mesh rebuilds during dragging; Copy settings includes them. Defaults are edited in the shared PineMaster, runtime sliders edit a copy. No terrain rebake required.

Visual forestry remains non-harvestable: **trunk collision and canopy fade exist**, but damage/chopping and navigation obstacles beyond physical trunk collision are not implemented. Tree crowns can obscure characters; the initial clearing reduces this around spawn. Finite draw range remains visible at extreme zoom/low angles. Robot patrol/zombie gameplay is unchanged. The old validation captures stay archived under WORLD/Content/Vegetation/Trees/Validation.

## PT0 — Overhead reuse

Implemented, **Awaiting Local Validation**. Scope authorized: reuse pines in the active world under VEGETATION/Trees/Pine with lighter shared settings and live tuning. Source/settings/path/scene/UID and whitespace checks only; Godot/.NET unavailable here. Local checks: build, F6 WORLD/World.tscn, inspect solid trunks/green crowns and height variation, walk across chunks, try both camera projections/zoom, adjust density/size/wind, OFF/zero/reactivation, paste Copy settings, unload without errors and assess appearance/FPS. No subsequent vegetation/gameplay phase is authorized.

## Camera obstruction fading

CameraOcclusion now queries cached canopy/chunk bounds and fades only the blocking tree through MultiMesh custom data in both LODs. Bark stays solid; foliage uses opaque ordered stipple coverage at 50% by default. Geometry rebuilds/evictions invalidate the camera. No per-tree physics collider or process node is added. See CAMERA/Occlusion/README.md; local shader/visual checks pending.

## Building placement and trunk collision

Trunks now have collision on physics layer 5 (bit 16), with one StaticBody3D per populated active chunk and one simple capsule per trunk. Radius shares the exact seeded visual thickness and per-tree scale; default collision height is 4m. Bodies/shapes are created once per chunk and removed on rebuild/OFF/eviction, with no per-tree callbacks. Player/zombie masks include trunks; the robot ignores them for now. Rifle rays also stop on trunks; trees have no damage/falling mechanic yet. World-owned exclusion rectangles keep building approaches and initial zombie positions clear. This supersedes the earlier visual-only note: canopy collision, pathfinding and robot tree destruction remain unimplemented.

## Imported-GLB art comparison (not a runtime replacement)

A separate [PineTree001 authored GLB](../../../WORLD/Content/Assets/Objects/Static/Vegetation/Trees/PineTree001/README.md) is available as a **standalone visual-only F1 preview**. It lives in the canonical authored Objects catalogue and has its own size master. It does **not** change PinePlacement, PineMeshBuilder, PineMaster, the current MultiMesh renderer, wind, LOD, foliage fading or trunk collision. This higher-detail GLB (7,679 triangles) should be visually assessed and optimized/LOD'd before trying it as an instanced forest replacement. Do not copy or duplicate the model into this folder in the meantime.
