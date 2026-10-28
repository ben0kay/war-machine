# OV1 — Correct exterior building fade false positives (Codex implementation plan)

**Status:** Proposed / planning only — **no OV1 gameplay change is currently implemented**.  
**Owner:** CAMERA/Occlusion and WORLD/Content/Structures/Shared.  
**Target:** `implement-td0-td1` development branch. Do not commit to `main`.  
**Read first:** root `README.md`, `CodePreferences.md`, `CAMERA/Occlusion/README.md`, `WORLD/Content/Structures/Shared/README.md`, and `WORLD/Content/Structures/BUILDING_AUTHORING_README.md`.

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
- `WORLD/Content/Structures/Shared/BuildingObstructionVisibility.cs` implements exterior whole-building reveal. It checks a merged local AABB first, then the local AABB of each cached visible mesh; **one intersection** of the camera-to-upper-body corridor adds the whole building as an obstruction. Its `OutsideObstructionRadius` is capped at 0.18 m by the current common defaults/masters.
- `CAMERA/Occlusion/CameraOcclusionMath.cs` provides expanded-box segment intersection and transformed bounds. Bounds are conservative; they may include space around rotated, hollow, nonconvex or decorative meshes, and do not represent actual coverage of the player.
- `WORLD/Content/Structures/Shared/StructureVisibility.cs` connects a prefab's shared floor and outside providers to the overhead camera. `BuildingFloorVisibility.cs` determines inside/level/stair state separately, and `CameraOccluder.cs` composes section/camera/building opacity without modifying physical colliders.
- `WORLD/Content/Structures/Shared/StructureSettings.cs` / each `Building00xMaster.tres` currently provide `OutsideBuildingOpacity=0.15` and `OutsideObstructionRadius=0.18`. The camera master uses `ObstructionRadius=0.45`, `ObstructedOpacity=0.5`, `ObstructionFadeSeconds=0.2`; the global opacity is not the same as the outside-building override.
- Existing structures have authored/generated wall/roof collision geometry, but its **coverage, physics layers, ownership and alignment with visible art must be audited before trusting it as the source of camera-occlusion truth**. Do not assume every window pane, trim piece, canopy or roof collider exactly matches the rendered shape.
- In the attempted/reverted OV1-A, five silhouette samples were checked **against the same mesh bounding boxes**. The owner correctly observed that all five samples could still intersect overly large/conservative boxes even when the character is visible. **Do not reintroduce multi-sample AABB-only checks as the solution.**

**Working diagnosis:** a conservative **intersection test** is being treated as proof of **visual obstruction**. To fix the reported screenshot, narrow-phase confirmation must use relevant actual surfaces (or a demonstrably geometry-faithful equivalent) and check a meaningful portion of the player's silhouette. Keep the AABB tests only for cheap candidate rejection.

## 3. OV plan / status

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **OV1-A** | Focused diagnosis and collision-coverage audit for the false-positive screenshot | Regular Chat for review; Codex for code inspection; Copilot/owner for local debug | **Planned** |
| **OV1-B** | Implement shared, geometry-confirmed exterior fade decision with player-silhouette coverage and stable transitions | **Codex / ChatGPT Work** | **Planned — not authorized by this document** |
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

**Owning files likely involved:** `CAMERA/Occlusion/CameraOcclusion.cs` and/or a small shared geometry-visibility helper, `CAMERA/Occlusion/CameraOcclusionMath.cs` where actually helpful, `WORLD/Content/Structures/Shared/BuildingObstructionVisibility.cs`, `StructureVisibility.cs`, `StructureSettings.cs` only if real tunables are needed, and owning README/authoring docs. Keep the mesh generator and WorldController free of building-name special cases.

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
