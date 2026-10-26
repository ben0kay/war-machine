# Surface Geography and Erosion — Implementation Plan

> **PLAN ONLY — No SF phase is implemented or authorized by this file.**
>
> **Owner:** `WORLD/` for terrain and landform authoring; reuse `WORLD/Content/` for rocks/vegetation. Implement in the **real** `WORLD/testworld.tscn`, not throwaway test scenes.
>
> **Related:** [WORLD/README.md](README.md) · [CodePreferences](../CodePreferences.md) · [WE2/WE7 environment roadmap](ENVIRONMENT_IMPLEMENTATION_PLAN.md) · [Cave geology plan](Content/Caves/IMPLEMENTATION_PLAN.md).

**Aim:** Make the surface of War Machine's misty 2km wilderness feel *geologically formed*, not just smooth noise with scattered props. Prioritize landforms first: tiny drainage gullies, exposed stone shelves/outcrops, fallen scree, then region-aware vegetation and a memorable landmark. Work incrementally, preserving the current playable world.

## Phase overview

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **SF0** | Audit current terrain, reserved areas, masks and safe authoring approach | Regular Chat / Copilot audit; Codex if changes needed | **Planned** |
| **SF1** | Seeded downhill erosion gullies and dry streambeds | Codex / ChatGPT Work | **Planned** |
| **SF2** | Distinct rocky ridges, exposed shelves, cliff outcrops | Codex / ChatGPT Work | **Planned** |
| **SF3** | Slope-aware scree, talus and fallen-rock deposits | Codex / ChatGPT Work | **Planned** |
| **SF4** | Forest edges, wet depressions and natural ground transitions | Codex / ChatGPT Work | **Planned** |
| **SF5** | One surface landmark, integration and performance polish | Codex / ChatGPT Work; Copilot validation | **Planned** |

**Phase states:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. Work only on explicitly authorized phase(s). This plan does not advance existing WE/CV phases.

## Confirmed foundation and non-negotiable constraints

- `WORLD/testworld.tscn` already has a saved **2048 × 2048m Terrain3D** landscape (`WORLD/Terrain/PineLandscape` from `WorldMaster.tres`), grass/rock autoshader, pines, shrubs/plants, rocks/logs, Sky3D fog, weather, F6 first-person movement, a cave entrance, and an active robot patrol near spawn. This is not an empty world.
- `WORLD/Generation/` is the existing explicit terrain authoring/bake area; `WORLD/Content/Shared/C/ContentScatter.cs` provides seeded, chunked, height/slope-aware decorative scatter; `WORLD/Content/Rocks/` offers varied rock meshes and triplanar material. **Reuse** these rather than creating an unrelated scatter framework.
- The cave currently shapes a *localized runtime* heightfield near the mouth and maintains roof cover. All surface passes must preserve the cave mouth/portal, avoid unbounded height edits above buried passages, and respect cave-generated ground and collision. Cave-specific entrance geology stays in **CV3**, not SF2.
- Terrain3D is heightfield terrain: appropriate for slopes, gullies and ridges, **not** arbitrary overhangs or vertical hollow spaces. Use integrated rocky meshes where heightfields cannot represent the silhouette.
- **Saved authoring safety:** Default to non-destructive **explicit** authoring/preview layers, deterministic seed/masks and original-data protection. Do not rebake or overwrite saved Terrain3D regions implicitly on F5 or while changing F9 sliders. If runtime heightfield previews are used, apply only once after terrain loads with clear ownership/order, and provide a way to restore from saved base regions before reapplying. Do not stack cumulative edits across reload/rebuild.
- **Keep routes usable:** Exclude or feather edits around player spawn, robot patrol markers, cave entrance/apron, walkable paths, and existing landmarks. Maintain Terrain3D collision and F6 traversal. Current tree/rock placement and fog should remain visually coherent.
- **Performance:** Generate/modify terrain only on explicit action or initialization as required; no noise/erosion simulation per frame. Cache masks/heights/noise where useful; render static rocks with shared meshes, MultiMesh, culling and limited shadows. Only refresh affected placement/collision regions after a terrain change; avoid full-world per-frame scans.
- New knobs are inspector-exposed C# Resources and `.tres` under `WORLD/Configuration/` or `WORLD/Generation/` only as features need them. Keep world seed/master ownership coherent; don't create several conflicting WorldMasters.
- Respect `CodePreferences.md`: C#, short script headers, method separators, useful folder READMEs, **one GitHub commit per authorized job**, proportionate build/source checks, user visual/performance acceptance.

## SF0 — Safe surface authoring preflight

**Objective:** Find the simplest nondestructive way to layer surface changes over the saved landscape.

- **Scope:** Inspect Terrain3D region APIs, existing generation/bake vs runtime cave modification, content scatter update signals, cave mouth and RobotEncounter route locations; define **protected masks** and edit-order rules.
- **Out of scope:** New terrain features or global procedural world rewrite.
- **Owning files:** `WORLD/Generation/`, `WORLD/Terrain/`, `WORLD/Configuration/`, `WORLD/README.md`; optional small documented helper only when used.
- **Config:** Reuse `WorldMaster.tres` seed and saved data path; prefer small dedicated authoring settings Resource if extra tuning is required.
- **Performance:** Run audit and any authoring preview explicitly, not continuously.
- **Pass:** A documented non-destructive editing strategy, protected-area coordinates/footprints and no accidental rebake or overwritten stored terrain data.
- **Lead:** Regular Chat can audit/docs; Codex for necessary API integration; Copilot for local check.

## SF1 — Natural erosion gullies and dry streambeds

**Objective:** Break up uniformly rolling slopes with winding downhill drainage details.

- **Scope:** Seed a **small localized area** first (not the whole 2km map). Generate plausible downhill polylines following sampled slope with low-frequency lateral meander, safe grade and variable channel depth/width; blend into existing terrain over feathered banks. Add a dry gravel/rock ground mask or locally exposed stone where existing Terrain3D assets support it.
- **Out of scope:** Physical running water, watershed simulation, infinite drainage network, full terrain rebake.
- **Owning files:** `WORLD/Generation/` for geometry/mask generation, `WORLD/Terrain/` for existing material/height integration, small world config if needed.
- **Proposed settings:** Gullies enabled, seed offset, count, min/max length, width (~2–8m), depth (~0.3–1.5m), meander, bank feather, slope threshold and protected-radius mask. Clamp extremes and keep all defaults tweakable.
- **Performance:** Deterministic one-shot height/mask work in bounded local regions; update Terrain3D collision once per changed batch, not every vertex. No runtime `_Process`.
- **Pass:** Walk alongside and across a few visibly winding shallow channels, with natural soil/stone blending and **no** sudden trenches, terrain seams, cave leaks or robot patrol interference.
- **Lead:** Codex for Terrain3D edits; Regular Chat for visual direction; Copilot/user for F6 walk check.

## SF2 — Exposed rock faces, ledges and outcrops

**Objective:** Create identifiable rock formations that interrupt otherwise rounded hills.

- **Scope:** Add a few seeded erosion-resistant ridge shapes/shelves with height and slope masks, and selectively place **large irregular rock meshes** on steeper faces for silhouettes/overhangs. Reuse current RockGenerator, stone materials and shared mesh helpers; replace with GLB artwork later without changing placement.
- **Out of scope:** Global cliff replacement, arbitrary voxel geology, digging or mandatory climb mechanics, rewriting cave entrance mesh.
- **Owning files:** `WORLD/Generation/` for limited ridge authoring, `WORLD/Content/Rocks/` for mesh variants, `WORLD/Content/Shared/C/` for any reusable terrain-driven placement.
- **Proposed settings:** Outcrop seed/count, maximum height adjustment, orientation, rock mass scale/variation, target slope, material thresholds and culling/shadow quality.
- **Performance:** Build meshes once per variant; instance/reuse, range cull, minimize large shadows and static collider count. Add collision for only traversable large rocks; small decorative ones stay nonphysical.
- **Pass:** A small number of recognizable cliff/outcrop silhouettes, convincing join into the ground, no uniform repeated boulders, appropriate scale and no visible cube-ended cliffs.
- **Lead:** Codex; optional Regular Chat feedback on silhouette and scale.

## SF3 — Scree and talus below steep slopes

**Objective:** Make small rocks accumulate **where they naturally fall** instead of randomly across meadows.

- **Scope:** Place seeded rock deposits at bases of exposed cliffs and gullies, graded from larger blocks near the rock face to smaller stones farther downhill. Use slope/distance/altitude or rock-face masks to cluster deposits; avoid perfectly regular grids.
- **Out of scope:** Falling-rock rigidbody simulation, erosion physics or large changes to terrain geometry.
- **Owning files:** `WORLD/Content/Rocks/`, `WORLD/Content/Shared/C/` and an SF-owned terrain mask, if introduced.
- **Proposed settings:** Scree enabled, density, spread radius, slope cutoff, rock-size distribution, patchiness, variation seed and render distance.
- **Performance:** Reuse existing static/chunked MultiMesh scatter; dormant placement off, explicit rebuild on authoring change, no per-rock `_Process` or thousands of collision bodies.
- **Pass:** Visible scree aprons below steep features, sparse/no coverage on flat meadows or robot routes, and good nearby vs distant culling.
- **Lead:** Codex for targeted scatter extension; Regular Chat can assist with settings/docs.

## SF4 — Forest-edge ecology and damp pockets

**Objective:** Connect geology to vegetation/weather so natural regions emerge.

- **Scope:** Adjust *placement inputs*, not tree art: fewer pines on sheer rock, shrubs and plants along sheltered slopes, sparse grass on exposed stone, thicker plants near gullies; optional small wet depressions or marshy-looking dark ground patches with subtle existing ground haze.
- **Out of scope:** Full biome scheduler, hydrology, changing Sky3D owner, seasons, water simulation, global vegetation rewrite.
- **Owning files:** `WORLD/Content/Vegetation/`, `VEGETATION/`, `WORLD/Terrain/`, scoped mask utilities and existing environment settings only as needed.
- **Proposed settings:** Edge sharpness, rock exclusion, dampness/noise mask, sparse meadow/forest probabilities, wet patch intensity and ground material blend.
- **Performance:** Reuse current chunk-resident, movement-triggered scatter and GPU grass; update only affected regions and rebuild only via explicit settings action. Zero unnecessary per-frame weather/ground updates.
- **Pass:** Tree lines and undergrowth transition by landform, gullies attract appropriate plants, exposed outcrops remain readable, current F9 tools and performance still work.
- **Lead:** Codex; user visual acceptance.

## SF5 — One natural surface landmark and acceptance

**Objective:** Make one memorable exploration location using SF1–SF4 pieces rather than many scattered filler props.

- **Scope:** Choose **one** destination (e.g. a gully leading to a jagged viewpoint or fractured cliff and talus slope), with a readable walking route and a fog-silhouette reveal. Review camera/terrain collision, range culling and the authored seed/override flow.
- **Out of scope:** Whole-map POI generation, new game objectives/enemies, networking, infinite streaming.
- **Owning files:** Integrated `WORLD/testworld.tscn`, small `WORLD/Content/` authoring references, config and READMEs.
- **Config:** Landmark seed/anchor/extent, protected route radius and quality controls if needed; preserve any hand-placed authoring.
- **Performance:** Measure visible rock/vegetation batches and terrain edit load in the main world when possible. New decoration remains static and content loading follows existing chunk events.
- **Pass:** One coherent walking route with recognizable geology, no accidental overlap with cave mouth/RobotEncounter, no cracks/terrain collision failures and acceptable user-observed frame time.
- **Lead:** Codex implementation; Copilot brief local diagnosis; user visual/FPS acceptance.

## Relationship to the other roadmaps

- **WE2:** Broad geographic composition / saved terrain authoring; these SF tasks are smaller opt-in work packages and **do not automatically mark WE2 implemented**.
- **WE7:** Natural landmarks and routes; SF5 is one specific experiment, not the entire WE7 phase.
- **CV3 cave entrance:** Owns *only* the cliff / apron / portal geometry and localized entrance surroundings. SF2/SF3 can reuse compatible rock meshes/materials nearby, but must not duplicate or undo the cave terrain height adjustments.
- **Robot and first-person:** Patrol waypoints/spawn and walking access are protected. Re-evaluate collision and placement heights after any explicitly authorized terrain edit.
- **Priority:** SF0 → SF1 (gullies) → SF2 (outcrops) → SF3 (scree). Start SF4/SF5 only after seeing the real environment; no need for more addons.

**Suggested Codex handoff:** “Read `README.md`, `CodePreferences.md`, `WORLD/README.md`, `WORLD/SURFACE_IMPLEMENTATION_PLAN.md` and the actual current Terrain3D/Cave/Robot source. Perform SF0 briefly and then implement **SF1 only**: a few seeded, shallow winding erosion gullies in a protected local area of the playable TestWorld. Preserve saved terrain, cave entrance and patrol paths. Keep validation brief, update the docs, commit/push, then stop.”
