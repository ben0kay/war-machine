# Cave Expansion — Underground Geology and Surface Integration

> **PLAN ONLY — No CV phases implemented or authorized by this document.**
>
> **Owner:** `WORLD/Content/Caves/`. This roadmap refines the existing cave inside the actual `WORLD/testworld.tscn`, not a detached test scene. It covers the next underground ravine/chamber pass **and** the nearby above-ground cliff/entrance composition. The broader mountains/biomes belong to the separate `WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md`.
>
> **Guidance:** [root README](../../../README.md) · [CodePreferences](../../../CodePreferences.md) · [cave README](README.md) · [world environment plan](../../ENVIRONMENT_IMPLEMENTATION_PLAN.md).
>
> **Rule:** Implement only explicitly requested phase(s); preserve authored Terrain3D data and all current caves/world systems. Run brief checks by default and defer extended visual/performance testing to the owner.

## Phase overview

| Phase | Goal | Suggested lead | Status |
| --- | --- | --- | --- |
| **CV0** | Briefly validate current cave rings, routing, ground cover and collision before extending it | Regular Chat/Copilot audit; Codex only for necessary fixes | **Planned** |
| **CV1** | One **true underground ravine/fissure** in a large existing chamber, with walkable ledge | Codex / ChatGPT Work | **Planned** |
| **CV2** | Asymmetric chambers, alcoves, shelves, vertical variation and better joins | Codex / ChatGPT Work | **Planned** |
| **CV3** | Exterior cave-mouth hillside: naturally eroded rock/cliff, talus, drainage and blending | Codex / ChatGPT Work | **Planned** |
| **CV4** | Interior geology, reusable placement and restrained light/wetness polish | Codex / ChatGPT Work; Regular Chat art-direction review | **Planned** |
| **CV5** | Optimization, enclosure, traversal and authoring validation | Codex / Copilot targeted checks | **Planned** |

**States:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. A push is not proof of rendered or traversal correctness.

## What already exists (confirmed at plan creation)

- `C/WildernessCave.cs`, `C/WildernessCave.Fork.cs`, `C/CaveRouteBuilder.cs`, `C/CaveShapeNoise.cs` and `C/CaveSettings.cs` are current implementation files; `Configuration/CaveMaster.tres` is the authoritative tunable resource.
- One default **190m winding descending main passage**, a **Y junction**, and two **125m winding branches** with seeded paths, wide chamber pulses (**2 on the main route and 1 per branch**), detailed wall noise and subdivided mostly walkable floors.
- The generator caches passage centre/ring arrays and makes inward-facing 3D mesh and static concave collision **once**. The fork owns fixed shared connection ports; the mouth owns a distinct terrain-matching exterior material and a connecting apron.
- Shallow surface roof cover is adjusted in **loaded runtime Terrain3D height maps only**, and the small entrance is opened in its control map. Source height regions are **not saved or rebaked**. Terrain3D is a **heightfield**: it cannot form arbitrary volumetric overhangs, undercut walls, underground ravines or multiple stacked surfaces by itself.
- Existing crystals/lights, weather shelter, first-person F6 cave traversal, fog, pines, robot patrol, terrain and entrance must continue working. There is **no requirement to replace terrain with voxels**.
- Latest winding/chamber revision is source-reviewed but awaits local Godot/.NET build, rendered entrance check and full walking validation; do not call it accepted without user feedback.

### Art direction

**Desired exploration rhythm:** close, winding tunnel → unpredictable large chamber → interesting height differences/ravine → constricted passage → next room, with occasional exterior clues of underground geology. Keep the floor comfortably traversable most of the time. Avoid a perfect symmetric pipe, regular repeating rooms, perfectly flat ground everywhere, obvious flat circular cuts and uniformly scattered rock decorations.

**Terminology:** Here *surface* means both the physical rock surfaces on cave walls **and** the outside terrain/hillside. The main above-ground improvements are CV3; the finer internal wall/detail improvements are CV2/CV4.

## Design contracts applying to every phase

1. **Macro → meso → fine noise:** Use seeded route/chamber/fissure placement for *large* forms, secondary deformation for wall/ceiling/floor asymmetry, and smaller detail noise for rock relief. Do **not** try to create a chamber or ravine merely by increasing fine `RockFrequency` or `WallRelief`.
2. **Single canonical geometry:** Collision must follow the **same generated surface** that is drawn. When cutting a fissure, **remove/replace affected floor polygons**, then add the ledges, vertical walls and bottom; do **not** place a ravine over an intact hidden collision floor or leave open edges. Join to the original passage rings/ports with shared vertices or an explicitly validated sealed transition.
3. **Safe route and ledge width:** Preserve a continuous collision-supported F6 route, with configurable minimum walkable ledge width (suggest **2.5–3.5m**) through the first ravine. No unavoidable leaps or player-sized collision gaps; ensure head clearance, sensible grade and step height.
4. **Preserve the Y junction:** Do not deform the three exact shared connection loops or seal their openings. Expand chambers away from the port transition zones. Treat added junctions/loops as a **later, separately scoped** architecture change if needed.
5. **Roof safety first:** Sample actual Terrain3D surface above all expanded outlines (centre, roof extremes, sides) and minimum clearance. If a chamber/ravine approaches the surface, move/deepen/clamp the local feature or conservatively support the entrance. **Never** accidentally raise a massive artificial ridge over a deep room or cause surface daylight gaps.
6. **Bounded procedural complexity:** All seeded construction, tessellation, collision and terrain correction happen once at cave initialization or via an explicit reauthor action, never per frame. Cache sampled noise/height and ring dimensions. Avoid allocating fresh meshes/materials per vertex/decoration. Use shared static meshes/instancing for repeated rocks where useful.
7. **Preserve old data:** Keep config defaults stable until a requested phase changes them. New exported settings must be clamped, documented and placed in CaveMaster. No writing checked-in Terrain3D regions, regenerating entire landscapes, changing project main scene, or adding voxel/AI addons.
8. **Scope and versioning:** A standalone plan/doc commit does not implement CV0–CV5. Each implementation job updates the status of only its authorized phase and records known compromises. Brief source review/compile/load check by default; user owns deep visual/FPS walkthrough.

## CV0 — Preflight and existing passage safety

- **Objective:** Confirm the newly winding paths and 2+2 chamber pulses can be safely extended before adding voids, ledges and more deformation.
- **Dependencies:** Existing `WildernessCave` C# files, CaveMaster, TestWorld, Terrain3D/first-person collision.
- **Scope:** Quick audit of ring topology, face normals/winding, entrance aperture, fork port continuity, floor collision, cave roof cover from above, shelter coverage and the current route's minimum grade/clearance. Especially check bends/chambers for self-intersection or bad collider triangles.
- **Out of scope:** New cave art, ravines, new geography, exhaustive runtime testing.
- **Files:** Existing cave C# and README; only add small diagnostics if genuinely needed.
- **Config:** No new gameplay defaults required.
- **Performance:** Audit once/offline; do not add a permanent validation tick or full-world scan.
- **Pass:** A brief C# compile/scene load when available, and clear list of any unresolved manual checks. If a significant blocking problem is found, report before extending geometry.
- **Lead:** Regular Chat for review; Copilot for local compile/walkthrough; Codex for targeted repairs only.

## CV1 — First proper underground ravine

- **Objective:** Carve a convincing **3D fissure** through **one** already-expanded main chamber as a manageable proof.
- **Dependencies:** CV0 safe chamber topology, existing profile/ring caches and StaticBody3D collision.
- **Scope:** One seeded chasm with a meandering, irregular opening, depth variation, rock walls and a closed rocky bottom. Preserve an **uninterrupted walkable ledge** along one side. Optionally one **natural rock bridge**, but only if simple and collision-safe.
- **Out of scope:** A global random ravine network, destructible caves, real water simulation, climbing/jumping puzzle, open-air world-heightfield ravine.
- **Files:** A small `C/CaveRavineBuilder.cs` only if separation materially helps; otherwise use one focused partial WildernessCave implementation. Change CaveSettings/CaveMaster and this README for authored values.
- **Implementation:** Choose a chamber away from junction/mouth and other collision boundaries; split affected floor strips into safe ledge + void + opposing bank; stitch non-planar ravine walls and bottom into the same collidable mesh. Use separate seeded low-frequency path/depth modulation and bounded small relief. Make sure original cave floor polygons under the void are **not rendered or collidable**. The rim is irregular, not a clean geometric slot.
- **Settings:** `RavineEnabled`, `RavineCount` (initial cap **1**), `RavineLength` (~**18–35m**), `RavineWidth` (~**3–7m**), `RavineDepth` (~**5–12m**), `MinimumLedgeWidth` (~**3m**), optional `RockBridgeEnabled`. These are proposed tuning ranges, not a requirement to add every knob immediately.
- **Performance:** Generate once, no per-frame ravine updates, reuse existing stone material/noise and one static concave collision shape or a bounded explicit piece. No unnecessary lights.
- **Pass:** F6 character can approach, follow the ledge and leave on both sides; ravine truly has depth with no invisible floor, no free-fall through missing walls/bottom, no z-fighting or unsealed seam. Check floor normals, cave roof and shelter.
- **Lead:** Codex/ChatGPT Work for the collision/topology-sensitive procedural geometry; Copilot for quick local traversal.

## CV2 — Asymmetric chambers and shelves

- **Objective:** Make large rooms look naturally eroded, not simply uniform radius multipliers.
- **Dependencies:** CV1 or CV0 if ravine is deferred; preserve CV1 ledge clearances when present.
- **Scope:** Seeded left/right width imbalance, ceiling dome variations, alcoves, rock shelves, a few narrow throats, occasional higher/lower terraces and localized roughness. Cross-section radius can vary with **angle** and **distance along the passage**, while respecting the walkable core.
- **Out of scope:** Unlimited caves, stacked tunnels, arbitrary tunnels intersecting mid-wall, wholesale graph/path rewrite.
- **Files:** Existing `C/WildernessCave.Fork.cs`, `C/CaveShapeNoise.cs`, CaveMaster; small helper for local profiles only if required.
- **Implementation:** Apply chamber-scale noise on ring profiles at multiple wavelengths with smooth fade to zero at the entrance, Y ports and stable traversal lane. Vary side walls/roof separately instead of deforming every vertex together. Construct convex-safe shelves/meshes with collision and avoid folds/inverted faces on sharp bends. Keep major traversable floors modestly sloped and show meaningful height differences off-route.
- **Settings:** `ChamberAsymmetry`, `AlcoveFrequency`, `CeilingVariation`, `ShelfFrequency`, `NarrowThroatStrength` with clamps/seed.
- **Performance:** One-time ring deformation and cached noise samples; do not introduce light/process components for every bulge.
- **Pass:** Chambers have visibly nonidentical silhouettes and 8–15m+ ceilings where configured, but no sharp unscripted collisions, self-crossing walls, daylight or impassable stretches.
- **Lead:** Codex for ring-aware deformation; Regular Chat can refine art-direction ranges.

## CV3 — Surface hillside and cave entrance integration

- **Objective:** Make the **above-ground entrance** look like an opening eroded into an existing cliff rather than a protruding polygonal tunnel/collar.
- **Dependencies:** Current `BuildEntrance` apron, cave exterior triplanar material and Terrain3D runtime height adjustments; note old screenshots showing an exposed exterior tunnel and blocky extensions.
- **Scope:** Irregular cliff face surrounding the mouth; eroded rock strata, lateral banks, small gullies/washouts, scree/talus fans near the base, boulder clusters and vegetation clearing/moss transition. Use the existing surface rock/grass texture families and maintain the ground's **organic slope**.
- **Out of scope:** Regenerating the 2km world, new Terrain3D plugin, a literal volumetric cliff mined out of a heightfield, full rivers/hydrology, decorative rock walls around the entire underground route.
- **Files:** `C/WildernessCave.cs` entrance/terrain support; `Shaders/CaveHillside.gdshader`; `Configuration/CaveMaster.tres`; optional small `C/CaveEntranceGeology.cs` and reusable meshes drawn from `WORLD/Content/Rocks`. Keep cave-owned work here.
- **Implementation:** Shape the Terrain3D hillside around the opening in a broad, seeded, **feathered** footprint that follows actual terrain contours, avoiding straight ridges and exposed square faces. Blend a local mesh lip/overhang with overlapping, buried edges, consistent normals and similar rock texel scale; prefer several naturally irregular rock masses over one visible box. Place downhill talus and drainage-like shallow channels aligned to local slope; exclude grass/trees right in the portal opening only. Use seed + slope/height/masks for placements, not an evenly spaced grid.
- **Config:** `EntranceErosionStrength`, `CliffRoughness`, `TalusDensity`, `DrainageStrength`, `EntranceVegetationClearance`, material blend/tint scale. Expose only knobs actually implemented and bound their ranges.
- **Performance:** One-time terrain edit and scatter generation. Prefer shared geometry/instancing; keep cover adjustments **localized**; disabled decorations do not continue processing.
- **Pass:** From outside in walking/flight modes, no detached exterior tunnel, cubic rock plug, daylight seams, silhouette cliff walls ending abruptly or texture-colour discontinuities. No player collision snag at mouth; existing weather shelter and cave access still work.
- **Lead:** Codex/ChatGPT Work for Terrain3D + 3D mesh seam integration; Copilot/user for local screenshot/traversal.

### Surface improvements beyond the cave mouth (broader WORLD phases, not part of CV3)

These would make your main 2km landscape richer and are useful follow-up **WE2/WE7** ideas, not automatic cave scope:

**These now have a dedicated [WORLD surface implementation plan](../../SURFACE_IMPLEMENTATION_PLAN.md) (SF0–SF5).** Cave CV3 remains responsible only for the localized entrance geology; the separate surface plan handles the wider hillside and forest surface.

| Surface feature | Why it helps | Implementation direction |
| --- | --- | --- |
| **Erosion gullies & dry streambeds** | Hillsides feel shaped by drainage instead of continuous smooth noise | Seeded drainage paths in the world terrain authoring stage, shallow cut/height masks and slope texture blending |
| **Distinct ridges, shelves & rock outcrops** | Breaks up rounded hills; provides viewpoints and recognizable routes | Terrain3D macro height masks plus locally placed oriented rock masses; meshes for overhangs |
| **Talus/scree fields** | Natural stone accumulates below cliffs | Slope/altitude based seeded rocks of multiple sizes; optional LOD |
| **Forest edge ecology** | Forests stop looking uniformly scattered | Tree/bush density tied to slope, shade proxy, altitude, dampness and clearing masks |
| **Wet depressions / marshy pockets** | Adds variation in terrain material and nearby atmosphere | Small persistent low areas with darker ground/wetness and controlled local ground haze |
| **Surface landmarks** | Gives exploration a sense of direction | One dramatic exposed cliff, dry ravine, fallen giant trunk or overlook before many small props |

**Prioritization:** I'd do **erosion gullies + rock outcrops + talus** before adding more grass or decorative plants. These change *landforms*, not just prop density. Coordinate broader changes with `WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md` WE2 / WE7, preserving all existing terrain data and user's current fog art direction.

## CV4 — Cave-surface texture, formations and light treatment

- **Objective:** Make close-up cave surfaces read as layered, damp, rough rock with selective large formations.
- **Dependencies:** CV1–CV3 geometry stable, existing stone material and crystals available.
- **Scope:** Seeded stalagmite/stalactite-like clusters, broken pillars, rock ribs, bedding/strata bands, collapsed stones and occasional wet patches or shallow pooled depressions if supported. Vary shape/placement; don't uniformly ornament the tunnel.
- **Out of scope:** Heavy tessellation on all walls, per-fragment raymarched volumes, per-object physics, new lighting engine or complex water sim.
- **Files:** Cave-owned C#/configs/shaders; reuse `WORLD/Content/Shared/C` mesh helpers and existing stone textures/materials.
- **Implementation:** Place formations using chamber/corridor masks and cached ring locations; maintain navigation clearance and avoid blocking junction mouths. Prefer a few bold silhouettes to thousands of tiny meshes. Adjust roughness/moss/wetness by location and projected orientation; balance crystal illumination against dark misty cave mood.
- **Config:** Formation density, minimum clearance, wetness frequency, rock size diversity, light frequency/quality. Add only settings actually implemented.
- **Performance:** Static instanced meshes/materials, low/optional shadows, distance culling and no per-object process. Only enable optional ambience/audio if audible/relevant.
- **Pass:** Close-up rock detail has convincing scale and variation; walking remains smooth; no distracting repeated crystals/placements or major GPU draw increases.
- **Lead:** Codex for generation/material work; Regular Chat for appearance review.

## CV5 — Performance, acceptance and deferred future scope

- **Objective:** Stabilize large cave geometry, collision and authored configurability before expanding the network further.
- **Dependencies:** Selected earlier CV phases implemented and initial visual feedback received.
- **Scope:** Review one-time CPU generation cost, mesh triangles, concave collider cost, terrain update footprint, seam/roof detection and indoor shelter event integration. Add sectional culling only if measurements show it necessary.
- **Out of scope:** Automatic unlimited cave networks, voxel terrain replacement, expensive world scans every frame.
- **Files:** Existing cave C#/master/readmes; lightweight diagnostics under cave ownership or DEBUG as justified.
- **Config:** Preserve authored seed/dimensions and document safe ranges, one-time rebuild conditions and invalid combinations. Don't live-rebuild on every slider move.
- **Performance:** Disabled cave generators sleep completely; static geometry uses engine culling; avoid per-frame noise, raycasts, physics or material writes. If very long underground spaces become costly, partition visually/collision-safe cave sections **without cracking shared seams** and profile on user's GPU first.
- **Pass:** Entrance, all branches, chamber transitions and ravine ledge traversable in F6; no roof/daylight seams, inverted faces, hidden floor collisions, misaligned static colliders, rain/snow entering sealed spaces, or pronounced FPS drop. Report actual measured vs deferred results.
- **Lead:** Codex for targeted fixes; Copilot/local Godot for repeatable build/visual checks.

## Execution and handoff rules

- Read root README + CodePreferences + the cave README and actual current C#/master/shaders before editing. Work on the actual world scene (not new disposable demos).
- Follow established `WORLD/Content/Caves/` paths. Keep one `CaveMaster.tres`, deterministic seeds, separated macro shape / small noise / materials, and modular small helpers only when necessary.
- One GitHub commit per expressly authorized job. Update this plan's state and the owning README accurately. Preserve existing terrain saves, Y-fork ports, Sky3D and weather, robot encounter and first-person controls.
- Proportionate source review and basic compile/launch if available; **do not perform extensive tests unless requested**. Tell the user exactly what to inspect visually and what couldn't be verified.
- Regular Chat can refine documentation, art direction and settings; use Codex/ChatGPT Work for topology-sensitive multi-file implementations; Copilot for local visual/build diagnosis.

**Recommended next Codex request:** “Read README.md, CodePreferences.md, WORLD/Content/Caves/README.md and WORLD/Content/Caves/IMPLEMENTATION_PLAN.md. Perform CV0 briefly and, if there is no blocking issue, implement CV1 only: one actual collidable ravine with a safe ledge in an existing chamber. Don't change the wider WORLD terrain or automatically begin CV2. Commit/push and leave extensive Godot traversal/visual validation to me.”
