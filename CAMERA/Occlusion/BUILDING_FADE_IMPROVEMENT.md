# OV1 — Correct exterior building fade false positives (Codex implementation plan)

**Status:** OV1-A source audit complete; OV1-B implemented — **Awaiting local build/visual validation**. See current handoff below.
**Owner:** CAMERA/Occlusion and WORLD/Content/Assets/Structures/Shared.
**Target:** `implement-td0-td1` development branch. Do not commit to `main`.
**Read first:** root `README.md`, `CodePreferences.md`, `CAMERA/Occlusion/README.md`, `WORLD/Content/Assets/Structures/Shared/README.md`, and `WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md`.

> The current implementation handoff below supersedes planning assumptions. The source at implementation base 212dbeb already contained bounds-only samples; these were replaced, not assumed correct.
>
> This note **replaces the older, completed building-opacity improvement history**. It is a new plan to fix *premature exterior whole-building fading*, not a request to re-implement the previous 15% opacity pass. The attempted multi-point **bounds-only** OV1-A commit was explicitly rolled back; do not assume any of that code exists.

## 1. The issue — distinguish a visible player from a genuinely hidden player

The player approaches a house from the exterior in the angled top-down 3D view. **Screenshot A:** the player stands outside, beside/in front of the near wall or corner and is already clearly visible to the camera; nevertheless the **entire house fades**. This is incorrect. **Screenshot B:** the player moves behind the house, with walls/roof genuinely covering the player from the camera; fading the house is correct.

**Desired behaviour:** exterior whole-building fade only when the building **actually obscures essentially all of the player silhouette from the active camera**. Being near a building, next to a wall, within an expanded proxy/bounds volume, or partially overlapping a building in screen space must not be sufficient. Restore the house when the player becomes visible again.

| Situation | Expected exterior whole-building result |
| --- | --- |
| Outside, standing against the front/near-side wall, player clearly visible | 100% opaque building |
| Outside, grazing a corner / only a shoulder or leg hidden | Opaque; favour no false-positive fade |
| Outside, almost/completely concealed by actual building wall/roof | Fade the building to existing configured target |
| Moving back into clear camera view | Smooth restore to opaque |
| Inside the structure / stairs / different storey | Existing independent roof, section and floor logic; no new exterior whole-building rule |

**Separate visibility concern:** Transparent walls can expose zombies/entities inside the house or outside directly behind it. **OV1 does not solve this or intentionally alter actor rendering.** A separate **OV2 — hidden interior/occluded entity presentation** needs its own plan and authorization, respecting room/floor visibility, closed buildings vs open canopies, and leaving AI/physics/sensing untouched. Do **not** claim that an OV1-only pass prevents X-ray visibility of zombies.

## 2. Confirmed code and root-cause hypothesis

- `CAMERA/Occlusion/CameraOcclusion.cs` owns the event-invalidated player/camera sight corridor and **one-shot checks (nominally 8/s)**, target fades and transition-only processing. The capsule/physics query there is for other opt-in scenery proxies; the whole-house logic comes from its registered provider.
- `WORLD/Content/Assets/Structures/Shared/BuildingObstructionVisibility.cs` implements exterior whole-building reveal. It checks a merged local AABB first, then the local AABB of each cached visible mesh; **one intersection** of the camera-to-upper-body corridor adds the whole building as an obstruction. Its `OutsideObstructionRadius` is capped at 0.18 m by the current common defaults/masters.
- `CAMERA/Occlusion/CameraOcclusionMath.cs` provides expanded-box segment intersection and transformed bounds. Bounds are conservative; they may include space around rotated, hollow, nonconvex or decorative meshes, and do not represent actual coverage of the player.
- `WORLD/Content/Assets/Structures/Shared/StructureVisibility.cs` connects a prefab's shared floor and outside providers to the overhead camera. `BuildingFloorVisibility.cs` determines inside/level/stair state separately, and `CameraOccluder.cs` composes section/camera/building opacity without modifying physical colliders.
- `WORLD/Content/Assets/Structures/Shared/StructureSettings.cs` / each `Building00xMaster.tres` currently provide `OutsideBuildingOpacity=0.15` and `OutsideObstructionRadius=0.18`. The camera master uses `ObstructionRadius=0.45`, `ObstructedOpacity=0.5`, `ObstructionFadeSeconds=0.2`; the global opacity is not the same as the outside-building override.
- Existing structures have authored/generated wall/roof collision geometry, but its **coverage, physics layers, ownership and alignment with visible art must be audited before trusting it as the source of camera-occlusion truth**. Do not assume every window pane, trim piece, canopy or roof collider exactly matches the rendered shape.
- In the attempted/reverted OV1-A, five silhouette samples were checked **against the same mesh bounding boxes**. The owner correctly observed that all five samples could still intersect overly large/conservative boxes even when the character is visible. **Do not reintroduce multi-sample AABB-only checks as the solution.**

**Working diagnosis:** a conservative **intersection test** is being treated as proof of **visual obstruction**. To fix the reported screenshot, narrow-phase confirmation must use relevant actual surfaces (or a demonstrably geometry-faithful equivalent) and check a meaningful portion of the player's silhouette. Keep the AABB tests only for cheap candidate rejection.

## 3. OV plan / status

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **OV1-A** | Focused diagnosis and collision-coverage audit for the false-positive screenshot | Regular Chat for review; Codex for code inspection; Copilot/owner for local debug | **Source audit complete; screenshot validation deferred** |
| **OV1-B** | Implement shared, geometry-confirmed exterior fade decision with player-silhouette coverage and stable transitions | **Codex / ChatGPT Work** | **Implemented — Awaiting Validation** |
| **OV2 (separate)** | Stop camera transparency exposing otherwise hidden zombies/entities | Separate planning task | **Not included in OV1** |

### OV1-A — Identify the falsely reported blocker before changing behaviour

**Objective:** Determine *which* building mesh AABB is producing the false positive, how the active camera ray travels relative to the player, and which existing solid colliders can provide truthful blocking checks.

**Scope:** Read `CameraOcclusion.cs`, `BuildingObstructionVisibility.cs`, `CameraOcclusionMath.cs`, `StructureVisibility.cs`, `BuildingFloorVisibility.cs`, `CameraOccluder.cs`, active building `C/` generators, scenes and masters. Check first-person-vs-top-down registration and player sample heights/stance (`PlayerController`).

**Approach:** Reproduce the two screenshot locations where possible. For a **temporary opt-in debug path only**, identify building instance, camera→player segment, bounding candidate and intersecting mesh/collider. Examine door and window holes, porch/canopy, wall and roof geometry, collider hierarchy and collision masks. Determine whether actual physics rays can unambiguously attribute a hit to the relevant building without mistaking terrain, props or other actors for that building. Audit all four current Building00x masters/prefabs and a future prefab contract, not a named-lodge-only shortcut.

**Out of scope:** No production fade behaviour change, no change to general foliage radius, no new AI rendering rules, no permanent expensive per-frame debug overlay.

**Performance:** Debug disabled by default and removed or dormant after diagnosis. Avoid background scanning or per-structure ticks. Reuse existing registration/lifecycle.

**Pass criteria:** Explain a concrete false-positive path, identify trustworthy collision mask/ownership for building solids, identify gaps that need a small proxy or exception, and recommend an implementation that doesn't require wholesale new colliders. Codex can review source; local owner/Copilot can supply concise visual results. Record findings here before authorizing OV1-B.

### OV1-B — Confirm true geometric obstruction before whole-building fade

**Objective:** A building must fade when it genuinely hides the player from the camera, **not** merely when a broad/inflated mesh AABB intersects the view.

**Proposed implementation:**

1. **Keep the cheap broad phase.** Continue the existing whole-building bounds and visible-part candidate checks; exit early for unrelated structures. Do not use those tests to make the final fade decision.
2. **Use camera-correct segments.** For a small fixed set of head/upper torso/left-right body/lower-body sample points derived from the player's actual standing/crouching dimensions, project each sample from the actual active camera (correct origin for perspective and orthographic). Clamp the segment to the sampled player point; a wall *behind* the player must never count as covering the player.
3. **Narrow-phase geometry confirmation.** Confirm hits against the building's **actual relevant opaque solid collision geometry** (or a narrowly authored, geometry-faithful visibility proxy if collision omits a crucial roof/wall). Attribute each hit to this structure by cached ownership/approved masks. Do not count terrain, player/other actors, unrelated structures, furniture or collision outside the visual wall as proof that this particular house hides the player. Handle window/door gaps, thin trim and roof slopes. Avoid treating broad AABBs themselves as blockers.
4. **Conservative coverage decision.** Default to requiring **all meaningful sampled points** to be blocked by the same candidate building before whole-building fading; favor keeping the house solid at partial cover. This is a tunable policy only if testing demonstrates that all-points is too strict. Start without a new global threshold if a fixed five-point contract is sufficient. Ensure that one clear sample vetoes a false-positive fade.
5. **Stable edges.** Add minimal enter/exit hysteresis (e.g. consecutive valid observations, distinct enter/exit coverage, or a small bounded delay) *only if needed* to avoid rapid opacity switching around corners. Must recover from camera/player movement and use no permanent polling. Existing fade interpolation and per-structure outside-opacity override remain single owners.
6. **Preserve sections and lifecycle.** Exterior decision applies only when outside. Indoors keep current roof/wall/floor/landing behaviour; don't cross-wire `SetWholeBuildingMode`, `SetBuildingOpacity` or section camera factors. Cache/cancel/refresh ownership on structure spawn, unload and explicit `Rebuild/Refresh`. FPS debug scene should remain opaque without registering top-down occlusion.

**Owning files likely involved:** `CAMERA/Occlusion/CameraOcclusion.cs` and/or a small shared geometry-visibility helper, `CAMERA/Occlusion/CameraOcclusionMath.cs` where actually helpful, `WORLD/Content/Assets/Structures/Shared/BuildingObstructionVisibility.cs`, `StructureVisibility.cs`, `StructureSettings.cs` only if real tunables are needed, and owning README/authoring docs. Keep the mesh generator and WorldController free of building-name special cases.

**Existing behaviour to preserve:** current 0.15 outside target, global 0.5 foliage/ordinary scenery target, existing 0.2s fade smoothing, collision and weapon/AI physics masks, indoor floor and roof composition, window/door openings, ground-floor furniture visibility, stairs, FPS debug experiment and automatic future prefabs.

**Config/data:** Prefer the existing per-structure master copied at runtime; keep current authoring files compatible and avoid requiring manual edits to all masters for a code-only correction. If a new option is justified, export it under the existing outside-obstruction group and document migration/defaults.

**Performance & lifecycle:** Use the existing camera/player/stance/view-change invalidations and bounded 8Hz timer; **no per-frame raycasts**, no new `_Process` on each building, no global scene scans, no allocations/mesh enumeration in hot paths. Cache collider identity and sparse sample offsets at attach/rebuild time. Only run a bounded few narrow-phase physics queries for candidate building(s); stop after a clear sample. Once no movement/settings change occurs and a fade is settled, all timers and interpolation should sleep. Respect spawn/unload, world streaming and explicit refresh. Measure worst case with several buildings intersecting the view before claiming cost is negligible.

**Acceptance tests (local owner/Copilot, brief):**

- Build with `dotnet build war-machine.csproj` (or the current checked-in .NET project), open `WORLD/World.tscn`, run **F6**. Do not use F5 (legacy scene).
- Reproduce Screenshot A: player just outside, pressed against near/front wall, fully visible → **building remains opaque**, including after stopping; test front, sides, porch and corners of Buildings 001–004.
- Reproduce Screenshot B: player walks completely behind wall/roof → **that building fades** to existing outside opacity; walking back to clear visibility restores it smoothly.
- Test partial body obstruction, high/low camera tilt, orbit, perspective and orthographic, zoom, standing/crouching; walls beyond the player and background objects must not trigger this building's fade.
- Test door/window openings and sloped roof/canopy: missing colliders should be reported rather than silently falling back to incorrect giant bounds.
- Enter/exit lodge and navigate stairs/upper floor; indoor presentation unchanged. Compare petrol station open canopy against enclosed shop. Verify trees/foliage and other camera fades unchanged.
- Spawn/unload/rebuild a prefab; ensure no stale collider cache or transparency. Check camera stationary, animation settled and obstructed view for idle callbacks/query load.
- **Important security/gameplay boundary:** Do not claim that zombies are hidden behind a faded structure; note that OV2 is still required.

**Suggested owner:** Codex for multi-file camera/structure integration; owner/Copilot for short in-Godot visual/performance confirmation. Keep changes independently reviewable. **One authorized implementation = one commit on dev**; source review/compile and runtime visual acceptance are distinct statuses.

## 4. Boundaries and handoff

**Do not implement this plan simply because it exists.** Authorization to replace this Markdown is for planning **only**. OV1-A and OV1-B require separate explicit approval; OV2 is a separate feature. Before coding, check latest branch state (an earlier five-sample OV1-A commit was rolled back), inspect real C# sources, and update this plan with any verified collision-coverage findings. Do not resurrect deprecated mesh-AABB-only coverage logic.

**Next recommended action:** start with a short OV1-A collision/geometry audit, then implement OV1-B directly against real building blockers. If source inspection already establishes a reliable geometry test, combine the *diagnosis within* an explicitly authorized OV1-B job rather than making extra scaffolding. Document changed files, commit SHA, build check performed/deferred, and owner visual/performance checks; mark **Awaiting Validation** until locally accepted.

## OV1-A/B — geometry-confirmed exterior visibility (current)

**Implemented; Awaiting local compilation/visual validation.** This supersedes the historical bounds-only OV1-A descriptions above. Bounding boxes alone no longer approve exterior fades. Five head/torso/screen-side/lower-body segments must each intersect actual cached opaque structural mesh triangles belonging to the same building. One clear sample keeps/restores it opaque. Each segment is projected from the active perspective/orthographic camera and stops 1cm before its player sample. OutsideObstructionRadius only pads broad rejection; it never thickens final geometry hits. Samples use transitioning CapsuleHeight for standing/crouching.

**OV1-A source audit:** Buildings 001–004 all declare shared visibility and structural visual roles. Their collision helpers use solid layer 1, also used by unrelated world geometry. Main box walls and sloped roof slabs generally match art, but collision is not a complete visual truth: Building001 Gable and Shared StructureGeometry.GableEnd create visible triangular caps without physics, Building002 PorchRoof has null collision, and stair ramps deliberately differ from treads. Window panes have separate collision; current Paint helpers render their coloured panes opaque (no alpha transparency mode), so those panes really do block, while empty door gaps do not. Whole-mesh AABBs include the empty upper corners of triangular gables; radius expansion also reaches outside a visible wall. Either can approve the former false fade, even for five samples. Exact screenshot camera coordinates were not available and the screenshot has not been reproduced here.

**Chosen correction:** cache real render-mesh faces at attach/Refresh/Rebuild instead of adding physics proxies or trusting mismatched colliders. Only metadata roles walls/roof/deck qualify; contents/stairs/unclassified art, characters and unrelated structures/terrain never supply blocker triangles. Repeated mesh resources share a face array within this provider. Per-sample whole bounds, then exact part bounds, reject before triangle tests. This is geometry confirmation, not another five-ray bounds approximation. It covers visual-only gables/porch roofs without changing generators, physical masks, weapons or AI. No new collider, master migration, rendering pass or building-name cases.

**Material/authoring limits:** native opaque BaseMaterial3D surfaces qualify; alpha/blended/cutout or custom-shader meshes fail open, with a warning for unapproved shaders. A mixed-material mesh containing a nonopaque surface is skipped as a whole; split opaque shell and glass into separate MeshInstances. Unknown/no-face geometry warns instead of reverting to boxes. Optional structure_camera_blocker=false on a mesh/ancestor excludes it. GPU-deformed/skinned geometry, shader cutouts and exact pixel coverage are not supported; use separate static opaque structural meshes and call Refresh/Rebuild after art, material, visibility metadata or relative transform changes. Cosmetic GeometryInstance transparency is deliberately ignored: a faded building still blocks the hypothetical opaque sightline, avoiding fade/restore feedback. Explicit Visible=false art is skipped.

**Performance/lifecycle:** only existing camera/player/stance invalidations at the bounded camera cadence (nominal 8Hz) invoke checks. Per provider/check: at most five samples and 16,384 narrow-phase triangle tests shared across those samples, early-out on any clear sample. Budget exhaustion keeps the house opaque and warns once, rather than guessing a fade. Work scales with candidate structural parts; complex imported shells may need simpler meshes. Arrays/transforms cache at setup; no hot-path mesh extraction, physics queries, new timer or building frame callback. Normal successful checks allocate no managed contact arrays. Existing opacity interpolation alone runs while changing, then sleeps. Shutdown clears cached parts/section references and disconnects movement; normal Refresh/Rebuild re-caches. No performance measurement claimed. Existing 0.15 outside target, 0.2s transitions, indoor floors/roof/walls/stairs, foliage and FPS opt-out remain unchanged. No hysteresis timer added without visual evidence of edge jitter.

**Checks run:** diff/loop syntax and lifecycle/material ownership review; numeric segment/triangle/coverage checks for real wall, visible front-side player, corner with one clear sample, gable-box empty corner, wall beyond endpoint, reverse winding, degenerate/coplanar cases. Godot and .NET unavailable: compilation not verified. Numeric checks are a model of the math, not an engine integration test.

**Local handoff:** run `dotnet build war-machine.csproj`; open WORLD/World.tscn and F6. For all four buildings, stand pressed against front/side walls and corners (solid), move fully behind (fade), return (restore), stop at each point (settles), and test partial cover/crouch/camera yaw/tilt/zoom in both projections. Inspect door openings, current opaque panes, sloped/gable/porch roofs, station canopy occupancy and lodge stairs/floors. Try spawn/unload/Refresh and FPS regression. Profile several candidate buildings while moving; confirm stationary checks and settled interpolation sleep. Do not treat this as hiding zombies exposed by transparency: OV2 remains separate.
