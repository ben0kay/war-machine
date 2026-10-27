# Rocks

Procedural decorative preview integrated into TestWorld. `C/` owns the generator; `Configuration/RockMaster.tres` owns artwork and placement defaults; `Artwork/` is reserved for replacement models. Shared placement and geometry helpers live in WORLD/Content/Shared/C. F9 settings require Apply/Rebuild. No terrain writes or per-instance processing. Awaiting user visual/FPS validation.

## Stone surface

`Configuration/StoneMaterial.tres` / `Shaders/Stone.gdshader` reuse the existing CC0 ambientCG Rock023 albedo/height and normal/roughness textures from Terrain3D's demo (see demo/assets/textures/asset_licenses.txt). Demo assets are unchanged. Triplanar world-space sampling needs no mesh UV unwrap. The shader adds normal-map surface detail, roughness variation, dampness and upward-facing patchy moss. Shape remains the existing low-poly boulder; textures do not change its outline.

RockMaster owns the material and texture scale, normal strength, moss amount and wetness. F9 updates these four parameters immediately without a rebuild. Each scatter category duplicates its shared material once, protecting source resources; imported mesh materials remain untouched. Existing batching, culling and shadow defaults remain. No CPU processing callbacks or per-rock material instances are added. GPU cost increases by six texture samples per visible fragment (three albedo and three packed normal/roughness samples).

RockFace now orients normals away from the boulder's interior, fixing dark tops caused by inward normals. Other content generators are unaffected. Source/diff/reference checks completed. Godot/.NET tooling unavailable in this session, so compilation, shader compilation, visual acceptance and FPS verification are deferred to the user.

## 3D top-down / inherited FPS reuse

**The meshes/material are compatible with both views**, but **current scatter placement is wired to the parked `WORLD/testworld.tscn`** through `ExplorationController` and `WorldConfig`. Merely adding a `ContentScatter` node to active `WORLD/World.tscn` is **not enough**. The active scene uses `PlayerController` and `WorldSettings`; adapt the lifecycle/height sampling without copying the mesh generator.

For the Codex integration handoff (not yet implemented), see [TOPDOWN_INTEGRATION.md](TOPDOWN_INTEGRATION.md). The generated `MultiMeshInstance3D` rocks currently **have no gameplay collision**, so use them as scatter decoration unless the integration adds *limited separate* static colliders for larger boulders and cover.

## Shape/material variation

Rocks now generate nine **deliberately distinct** low-poly mesh profiles: small loose stone, fieldstone, angular block, long low stone, broken upright, broad slab, tall angular stone, large boulder and wide outcrop. Four irregular height rings with faceted shoulders/crowns avoid every rock being a pointed cone. Meshes are generated once per variant and reuse the existing CC0 triplanar material. Existing random yaw/scale placement remains. Triplanar sampling uses each instance's local coordinate frame plus a position-derived offset, so texture orientation follows the stone rotation instead of every rock sharing world-aligned strata. Moss remains oriented by world up. Material remains shared, with no per-rock CPU animation.
