# SOC1 — Object authoring diagnostics, performance controls and validation

**Status: PARTIALLY IMPLEMENTED — ShadowMode only (SOC1A shadow subset, 2026-10-29); all other phases remain proposed.** This is a future implementation handoff, not authorization for the remaining gameplay work. Current branch: `implement-td0-td1` in `ben0kay/war-machine`. Read root `README.md`, `CodePreferences.md`, `OBJECT_AUTHORING_GUIDE.md` and latest implementation source first. Implement only owner-approved phases, one commit per phase.

## Purpose and constraints

Make the GLB catalogue safe to grow from a handful of items to hundreds by exposing useful **authoring settings**, offering **on-demand F1 diagnostics**, and checking generated models **before** they get into Godot. Prioritize frame time, draw/surface count, debugging and consistency. Do **not** add a `_Process` or `_PhysicsProcess` per static object, regenerate imported mesh data, introduce a second culling system, change player/zombie/world placement, or change colour-variant semantics.

**Current foundations to reuse:**
- `Shared/PlaceableSizeSettings.cs`: per-type master now contains **SizeMultiplier** and **ShadowMode** (int Inspector dropdown; 0 Inherit / 1 Disabled / 2 Enabled, default Inherit). Only the shadow subset of SOC1A has landed.
- `Shared/PlaceableVisual.cs`: applies size and calls SOC0 static collision once at `_Ready`; `CollisionMode=0 None / 1 AutoBox / 2 CustomBoxes`. Fridge/Bed/Table derive for colour/material overrides.
- `WORLD/Rendering/WorldRenderCulling.cs` and `WorldRenderEnvelope.cs`: WS1 owns native visibility distances, fog rules, shadow safety buffer and published `world_authored_range_end` / `world_render_exempt` metadata. No custom per-object distance polling.
- `DEBUG/PlaceableSpawner/PlaceableCatalog.cs` + `PlaceableSpawnMenu.cs`: F1 discovers canonical scenes and exported enum *visual variants*, spawns temporary previews (max 24). Root exported enums for new global controls would **accidentally create fake F1 variants**; use Inspector int dropdowns or a nested Resource.
- `CAMERA/Occlusion/CameraOccluder.cs`: top-down obstruction fading uses physics layer 8 proxies. SOC0 solid collision uses layer 1; they are separate systems. Existing Godot imported mesh LOD should remain the default.

## Roadmap — prioritize what can be built independently

| ID | Status | Priority | Feature |
| --- | --- | --- | --- |
| **SOC1A** | Shadow portion in source; render cap proposed | First | `ShadowMode` **implemented**; optional per-asset `RenderDistance` cap **not implemented** |
| **SOC1B** | Proposed | First | `DebugBounds`, `DebugCollision`, pivot / mesh view in F1 |
| **SOC1C** | Proposed | First | Offline `AssetValidation` for GLB + .tscn + master + palette contracts |
| **SOC1D** | Proposed | Second | `SurfaceBudget` report and repeatable performance benchmark |
| **SOC2A** | Deferred | Later | `LODProfile` only after profiling |
| **SOC2B** | Deferred | Later | `PlacementFootprint` and descriptive `PhysicsRole` |
| **SOC2C** | Deferred | Later | `CameraFadeMode` integrated with existing camera occlusion |

### SOC1A — ShadowMode + RenderDistance

**Implementation record (2026-10-29):** Only `ShadowMode` is now implemented. `PlaceableSizeSettings.ShadowMode` is an exported `int` dropdown in each existing per-asset master; `PlaceableVisual._Ready` recursively updates imported `MeshInstance3D.CastShadow` once at runtime **only when mode 1 or 2**. Mode 0 avoids the traversal, retains original imported shadow settings, and makes existing scenes backward-compatible. No per-object update loop; SOC0, WS1, visual variants, lights and physics remain unchanged. No assets' masters were switched off by default. **Godot compile/visual/performance checks pending.** The following RenderDistance design is still a proposal; do not claim it exists or implement without permission.


**Owner:** shared asset master + `PlaceableVisual` one-time apply helper; WS1 owns distance policy. **Keep existing default appearances and visibility identical.**

Suggested optional exports within the existing size-master Resource (choose one canonical settings owner; don't clone competing masters):

```text
[Model] SizeMultiplier = (1,1,1)        # existing
[Render] ShadowMode = 0                  # IMPLEMENTED, int dropdown: 0 Inherit, 1 Disabled, 2 Enabled
[Render] MaxRenderDistance = 0.0         # NOT IMPLEMENTED; future float metres (0 = inherit)
```

1. `ShadowMode=Inherit`: preserve imported `GeometryInstance3D.CastShadow`. `Disabled`: set Off on the imported visual mesh nodes; `Enabled`: set ordinary On for appropriate opaque meshes. No changes to collider, lights, camera proxies or materials. Do not force transparent glass panels to cast unsuitable opaque shadows without asset-specific testing.
2. `MaxRenderDistance=0`: change nothing. A positive, finite cap means an authored **maximum**; preserve any *shorter* imported range and never extend existing visibility. Use native `GeometryInstance3D.VisibilityRangeEnd`; record the authored effective cap with existing WS1 `world_authored_range_end` metadata so WS1's fog ON/OFF and refresh restore behaviour remains correct.
3. Apply both **once** after the model exists, during `PlaceableVisual._Ready`, before WS1's deferred registration. When intentionally editing a live asset, explicitly notify WS1 via existing `Refresh` or `RefreshSubtree`; no per-object timer or camera distance check.
4. Keep world-wide `WorldRenderSettings` (HiddenBuffer, ShadowBuffer, opacity/sky policy) authoritative for fog culling. An object cap should not override WS1 into revealing sky through hidden terrain; test FPS where the current clear-sky mode disables fog geometry cutoffs. A short authored cap may still pop, so defaults should remain inherit.
5. Scope to canonical GLB `PlaceableVisual` assets, not Car001, doors, streetlamp, terrain, player, zombies, weather or pine instancing systems. Existing material palette overrides remain unchanged.

**Acceptance:** Fridge001/Bed001 defaults look identical. On a copy, Shadow Disabled removes visual shadow but not contact/collision; other copies unchanged. Test 0 / 20m / pre-existing-shorter range, WS1 ON/OFF, fog opaque/transparent, FPS/top-down, daytime/nighttime. Report frame times, not promised FPS gains; no new per-object updates.

### SOC1B — F1 Asset Inspector, DebugBounds and DebugCollision

**Owner:** F1 placeable spawner; optional small shared inspector helper. **Do not export debug toggles as new root C# enum variants.**

1. Add a compact collapsible, debug-only Inspector for the **last spawned or selected F1 preview** (not a general live-world inspector yet). Cache an asset snapshot **on spawn/selection/clear**, not every frame.
2. Display asset ID/variant, native vs scaled AABB (metres), root pivot/orientation (+Y up, +Z authored front), mesh nodes, surfaces, material resources, triangle/index totals (when readable), SOC0 mode, number/types of actual generated physics shapes, optional shadow policy and authored range. Say **potential visible surfaces**, not actual render draw calls.
3. `DebugBounds`: on-demand wire bounding box in world space; `DebugCollision`: outline actual SOC0 primitive boxes (not duplicate solid physics bodies); `DebugPivot`: show orientation arrows; optional `DebugMeshBounds`: show separate imported mesh AABBs to diagnose over-segmentation.
4. Draw under a dedicated **noninteractive debug root**, ideally outside WS1's managed prop roots. No shadows, no collision, no saved geometry. Toggle off/clear/reselect/scene exit removes all overlay visuals. Zero idle per-object work; use Godot debug APIs where practical.
5. Preserve F1 controls, 24-instance cap, spawn/clear, camera focus and imported material variants. Make DebugCollision useful for the large full-envelope AutoBox on DumpsterBin001 and open CustomBoxes later.

**Acceptance:** spawn Fridge001, WheelieBin001 and DumpsterBin001; read correct sizes, numbers and collision. Spawn scaled/rotated copy and confirm overlays align in FPS and top-down. Clear all 24 without leaked lines/nodes. Disabled debug view introduces no runtime work.

### SOC1C — Offline AssetValidation (GLB import mistakes)

**Owner:** one offline authoring script, e.g. `tools/validate_placeable_assets.py` (standard library preferred), plus optional Godot C# headless import verification. No runtime property or game-start scan.

Validate **before committing each generated GLB**:
- glTF 2.0 magic/version and **exact binary lengths**; JSON and BIN chunks, buffer boundaries/alignment, data types/strides/accessors, node hierarchy/cycles, index/material references, nonfinite positions/normals, unit-normal warnings, and every triangle index less than its vertex count.
- Verify declared POSITION min/max against actual floats, no missing external buffers/textures, valid PBR baseColorFactor fallbacks. Support genuine glTF embedded textures if intentionally authored, but flag unknown external dependencies.
- Canonical `<AssetID>.tscn` loads expected `Artwork/<AssetID>.glb`, script/master exists, resource paths match, `SizeMultiplier` sensible, SOC0 mode explicitly selected, CustomBoxes valid, scale/pivot/orientation plausible, and scene-required paint meshes/pivots exist for Fridge/Bed/Table-style palette adapters.
- Emit warnings (not arbitrary fatal limits) for huge triangle/surface/collider counts and far-from-origin pivots; record asset/scene/material names so an AI can correct errors.
- Optional **Godot headless .NET import + project build** when installed; separate status `structurally valid` from `Godot imported` and `visually approved`. Automated binary checks alone are NOT import certification.

**Regression test:** deliberately truncated BIN, corrupted Base64 geometry, out-of-range indices, nonfinite vertices and missing named paint node must fail with useful diagnostics. Valid source files must pass. **Never automatically edit/re-encode GLBs** as part of validation. This phase directly addresses the earlier dumpster importer failure.

### SOC1D — SurfaceBudget report + measurable profiling

**Owner:** offline report plus optional F1 inspector columns; never a new per-object FPS counter.

- Per asset: GLB triangles, mesh instances, surface count, unique material refs, estimated texture footprint if possible, SOC0 shape count, shadow mode, maximum render range, current world/F1 instance count. For aggregate scenes, group by asset type so the biggest contributors stand out.
- **Don't equate mesh count × instances with exact draw calls.** Actual submissions and shadow passes depend on renderer, materials, batching, visibility, LOD and lights. Keep Godot's measured renderer stats separate from source estimates.
- Define a repeatable scene/experiment with 1 / 10 / 24 copies; optionally stress 100+ in a dedicated benchmark, not the permanent world. Record CPU/GPU frame times (where available), actual draw submissions, instance count, memory and 1%-low frame-time proxies where feasible. Hold viewpoint, fog, lighting and visibility constant; compare FPS and top-down at day/night with WS1 ON/OFF and shadows Inherit/Disabled.
- Add configurable **warning** budgets (not hard limits), based on measurements. MultiMesh is a later candidate for many identical noninteractive static objects—not a blanket replacement for palette/hinge furniture.

**Acceptance:** asset counts for Fridge001 match imported resource data; report doesn't count render submissions as a hard fact; A/B captures are repeatable and inspector does no repeated mesh traversal every frame.

## SOC2 — deferred options (not included in SOC1)

**SOC2A: LODProfile.** Default `ImportedAuto` uses Godot's existing import-generated mesh LODs and WS1 cutoffs. Only after profiling large repeated groups consider `NoLOD` / `HighDensity` authoring options or extra manually authored LOD models. No custom `_Process` distance switching; GLB source and variants remain authoritative.

**SOC2B: PlacementFootprint.** Optional `AutoXZBounds / CustomRectangle / None` plus margin/orientation to reserve placement area around a bin, table, shelter or container. Future world generator may use it for overlap clearance; **not** a physics collision shape or F1 placement logic. No current world-scatter approval. **PhysicsRole** is only descriptive `Decorative / FixedSolid / InteractiveFixed / PlannedMovable` metadata, not a switch that creates RigidBody3D or inventory. Moving/throwing stays with IX, independently of SOC0 collision.

**SOC2C: CameraFadeMode.** Future `Default / Never / OptInOccluder`; opt in only on large props that actually obscure player in top-down. Reuse `CAMERA/Occlusion` and separate layer-8 fade proxies, not SOC0 layer-1 colliders. Preserve player shots, enemy vision, physical cover, building fade groups and FPS view. Do not spawn an occlusion Area3D for every can/book/fridge by default.

**Related gameplay ideas NOT owned by SOC1:** destructibility, climbable furniture, navmesh blockers, loot containers, mass, audio attenuation, doors opening and WS2+ streaming. Only their existing owner may implement these after separate approval.

## Implementation guardrails and handoff

- Each SOC1 subphase should land independently with a proportional test/README update, one commit per approved task, always on `implement-td0-td1`. **No code implementation authorized by writing this plan.**
- Preserve every current `.tscn` and `.tres` default unless explicitly changed. Use shared helpers and `[ExportGroup]`; comments above methods with `// =========================================================`. No per-object polling or duplicate material resources.
- A source test is not a Godot visual/physics test. Mark separately: `planned`, `source implemented`, `build passed`, `Godot import passed`, `runtime checked`, `performance measured`.
- Update this plan's status, `OBJECT_AUTHORING_GUIDE.md`, `PLACEABLE_CATALOG.md`, and owning READMEs when each phase is **actually implemented**.
- Validate FPS/top-down, day/night, WS1 on/off, imported LOD/shadows, collision unaffected, reused colour variants, scene instancing and deletion of previews.

**Future AI instructions:** To begin later, ask `Implement SOC1A only from WORLD/Content/Assets/Objects/SOC1_OBJECT_DIAGNOSTICS_PLAN.md`. Likewise SOC1B, SOC1C, or SOC1D. Don't silently implement deferred SOC2 features.

**Phase checklist:** SOC1A ShadowMode [x] (source, runtime validation pending) · SOC1A RenderDistance [ ] · SOC1B [ ] · SOC1C [ ] · SOC1D [ ] · SOC2A [ ] · SOC2B [ ] · SOC2C [ ].
