# Robot_001 — RM0–RM4 encounter

**Implemented, Awaiting Validation.** Original roughly 11m industrial quadruped with a four-marker patrol, explicit settle/wait, cyan scan sweep, head recovery and resumed walking. Integrated in `WORLD/testworld.tscn` through the world-owned `WORLD/RobotEncounter.tscn`. No damage, pursuit or dynamic spawning. RM5 F9/audio/profiling remains Planned.

## See it in the world

Build the Godot .NET project, then run the main scene (F5). The route is in the existing spawn clearing: centre **10m west / 13m north of spawn**, 20m × 14m. Robot starts at the southwest corner, about **20m west / 6m north of spawn**, walks north, then around the loop. Look ahead and slightly left. Each arrival settles for 0.6s, waits 1.5s, scans 6s and recovers for 0.8s. Console status reports Walking/Settling/Scanning/Recovering and optional player detection. F6 remains camera mode; F9 remains existing environment tuning.

World owner samples saved Terrain3D for route/foot targets, checks every edge's nearby height corridor, and refuses unsafe placement with an error rather than changing terrain. This is an initial meadow route, not obstacle navigation. Current source placement keeps body/feet near the existing 34m tree-free spawn clearing, away from the cave at +48/-28. Actual terrain/gait/rendered appearance still need local confirmation.

## Configuration and reuse

`Configuration/RobotMaster.tres` owns RobotSettings with embedded PatrolSettings/SensorSettings. Every actor deep-copies its master; patrol/sensor also isolate their runtime settings. Shared EntityMaster stays species-neutral.

Art parameters (scale, dimensions, colours, leg lengths, stance, ArtOverride) are spawn-time; recreate the actor after changes. Gait parameters initialize its one driver. Patrol/sensor and beam settings initialize when the world calls InitializeEncounter. Head yaw limits and scan/recovery/sweep settings are read by the coordinator. There is no live mesh rebuild or F9 controls yet. Keep dimensions positive and the stance reachable. Nonuniform actor scale is unsupported.

InitializeEncounter receives explicit world-space route points, an optional foot-target correction function and an optional candidate. It never searches TestWorld paths. To add another robot, duplicate the world encounter scene instance, adjust its SpawnOffset/Route markers and use its own master if desired; no shared-script edits. Ensure another flat clear area and sufficient separation. Terrain corridor checks do not provide obstacle avoidance.

SetWalkPreview(true) provides stationary treadmill art inspection; disable it before patrol. SetMotionSpeed drives gait from actual locomotion events. SetHeadYaw changes only HeadPivot. SetVisualRelevant gates gait; the screen notifier also suspends offscreen gait. Direct actor Activate/Deactivate is supported. World RobotEncounter.SetEncounterEnabled toggles the integrated encounter explicitly.

## Rig contract and performance

Generated hierarchy: VisualRoot → BodyRoot → HeadPivot → SensorSocket (-Z). BodyRoot also owns FrontLeftHip, FrontRightHip, RearLeftHip, RearRightHip; each owns Knee → Foot. Upper/lower bone geometry follows local -Z. ArtOverride must provide this rig below an identity art root, matching lengths/units. An arbitrary GLB needs a rig/socket wrapper. Movement owns the physics root; gait/head own art transforms. One coarse capsule; no limb collision.

Primitive box/sphere resources are shared, three finish materials built once per robot. One robot callback updates all gait joints only while moving/settling and visible; feet stay cached in world space, target corrections run only on initial stance/new swings. Returning onscreen resets a valid stance. Active scanning/recovery keeps the required head callback even offscreen so logical cone direction remains correct. The beam uses shader animation without CPU ticks.

Patrol physics runs only while moving/turning; one timer handles stationary settle/wait. ScanSensor runs at 5Hz only during requested scans with candidates, at most two LOS rays per tick. One stage timer ends scanning/recovery. Stop/hide/deactivation cancels stage/sensor timers, hides the beam, stops/pauses patrol and clears velocity. An interrupted scan resumes by departing normally, not leaving a stale beam or replaying scan callbacks.

World relevance subscribes to explorer ViewPositionChanged with an initial check; wake ≤180m, sleep >240m. Active movement also assesses the boundary in its existing callback. Dormant actors have no distance polling. Explicit disable does not get overwritten by proximity; leaving the scene disconnects viewer and capability events. Static rendering/collision have engine costs; none have been profiled here.

## Validation and current limits

Source/lifecycle review, scene/resource reference checks, numerical cone/IK sanity checks and `git diff --check` performed. This host has no dotnet or Godot, so **compilation, scene load, two-cycle patrol, second-instance runtime, foot appearance, fog transparency and FPS are unverified**. No runtime pass is claimed.

Locally: run `dotnet build war-machine.csproj`; run F5 and watch two walk/scan cycles. Check feet/joints, independent head, cone sides/bands/depth/fog, no floor clipping, F6 movement and existing controls. Disable/re-enable while walking and scanning; walk/fly past 240m and return below 180m. Check no stale beam/timer or repeated arrivals. Briefly duplicate the encounter at a clear route with another master. No extensive test suite is required.

The cone has no exact ground footprint, raymarched volume or terrain-conforming projection; transparent foliage ordering and final art quality require user feedback. No terrain/vegetation/weather source settings were changed.

### Patrol/grounding correction — 2026-10-26

Owner reported scanning without visible forward travel followed by falling through terrain. Patrol now records wait completion before invoking ReadyToDepart, so scan pause/resume advances to the next waypoint even if Timer.TimeLeft still reports a tiny positive value during timeout. Robot passes its injected terrain sampler to locomotion as well as visual feet. While walking, locomotion samples current/next/actual x/z and restores the body to the sampled ground height; it does not rely on viewer-centred Terrain3D collision coverage for vertical support. Ordinary actors without a sampler retain gravity. Missing terrain pauses safely. Stationary/disabled states do no grounding polling. Horizontal MoveAndSlide collision is preserved.

Source and whitespace checks only; no Godot/.NET runtime available here. Local check pending: observe departure after a scan, multiple waypoints and no sinking, including when the viewer flies away/returns. This fix has not been rendered or runtime-tested here.

## RM5 update — implemented, awaiting local validation

F9 now has an Entities / Robot_001 section with enable, patrol speed, scale + explicit Apply, leg/beam quality toggles, duration, cone range/full angle, opacity/brightness/band contrast, line speed, sweep amplitude/rate, sensor toggle, audio level/toggle and force scan. Values update runtime copies on change events; no UI polling. Scale is staged; it updates visual/capsule/notifier scale and resets a safe stance without reconstructing geometry. Reset All restores both ordinary and staged values. Art dimensions still require respawn.

Default beam opacity is now 0.30, brightness 2.0 and band contrast 1.5. Baseline body visibility and grazing-angle visibility are stronger, retaining end fading and ordinary depth/fog. No second cone, light or raymarching was added. Opacity and brightness remain separately adjustable. Logical sensor and cone share range/angle configuration; toggling the visual beam does not disable optional sensing.

Default stance narrows from 4.5m to 3.7m per side and hip height rises from 7m to 7.6m, giving a more upright industrial outline. The robot is still a provisional quadruped. Optional quiet synthesized PCM footsteps and scan servos are original runtime-generated/cached streams, not external assets. One landing event per diagonal pair wakes a one-shot spatial thump; servo loops only while an active visible/relevant scan requests it. Stop, hide, disable and exit stop sound. F9 can switch audio off entirely.

Performance handoff: no new idle callbacks; gait remains one visible active callback, logical head sweep/recovery is bounded to its active stage, sensor stays 5Hz/capped LOS, beam uses shader time, UI updates only on events, sounds wake only on requests. Stream data is built once and shared. No actual CPU/GPU timings were measured. Check one/two robots, walking/scanning, settled/disabled, offscreen, far sleep and reactivation locally; cone overdraw and sound balance need owner feedback.

Checks here: source/resource/binding review, whitespace and geometry arithmetic. Godot/.NET are unavailable. Compilation, rendered brightness/stance, real audio, reset, scale, toggle behavior and profiling remain **Awaiting Validation**. The owner previously confirmed RM2–RM4 patrol and scanning worked after the grounding fix; RM5 is new and not yet accepted.

## Current design — heavy cyan biped (supersedes four-leg artwork)

Owner requested two substantial legs and head-side hardware using their reference for broad proportions. Robot_001 now generates an original roughly 10–11m biped: broad layered chest, undercarriage fins, reinforced hips, thick armoured thighs/shins, large knee bearings, hydraulic rails and three-toe feet. Two angled sensor pods flank the independently turning head, with cyan inset arrays and dark protective ribs. The main scan beam still attaches to the central head SensorSocket; side pods are visual hardware, not weapons/extra beams. Armour is brighter gunmetal for readability through fog, all powered accents cyan.

Rig now has `LeftHip` and `RightHip`, each Knee → Foot, instead of four corner hips. ArtOverride must match this current two-leg hierarchy. Generic Entity scene, waypoint patrol, sensing, head sweep, beam and lifecycle are preserved. RobotMaster explicitly owns current body/leg dimensions/colours; old four-leg appearance notes describe superseded art only.

Shared MechanicalLegs accepts two or four injected legs and optional per-leg bend directions. Robot_001 injects forward knee bends and alternating 0/0.5 phases, small support-side body shift/roll and lower lift/bob. The helper retains diagonal-pair support for future quadrupeds. One driver, cached foot targets, no per-leg callbacks and no extra collision shapes. Two legs reduce the number of active IK solves; no measured performance claim is made.

Brief source/reference checks, two-leg IK/phase calculations and resource checks performed. Godot/.NET unavailable: compilation, the actual biped gait/silhouette, head-pod sweep clearance, grounded feet and scanner alignment still need owner inspection. Check a full walk/stop/scan cycle and F9 scale/disable/re-enable. No existing imported custom art has been migrated automatically.

## Ownership cleanup

RobotController.cs, RobotSettings.cs and RobotAudio.cs live directly here. C/ retains RobotMeshBuilder.cs only.


## Industrial intimidation visual pass — 2026-10-27

Robot_001's **existing original C# biped** receives additional industrial/mechanical art, not a new actor or a separate model. Only `C/RobotMeshBuilder.cs`, art settings and this README change. The existing cyan cone scanner, logical LOS, movement, audio, damage assumptions, physical capsule, two-hip IK rig and head rotation remain untouched.

- **Stronger silhouette:** interlocking angled chest plating, raised shoulder overhangs, stronger lower-leg/foot plating, bulky engine side housings, dorsal cooler fins and rear grilles.
- **Industrial detail:** rivet/bolt caps, guarded sensor pod cowls, ventilator ribs, leg pistons/hydraulic-looking rods, knee/foot reinforcement and underbelly bracing.
- **Power cabling:** short dark/steel cylinder links form bent cable runs on the torso, each upper leg and shin. They are **attached to the matching BodyRoot / LeftHip / RightHip / Knee** transform (not stretched between joints), so the standard gait follows them without any per-frame cable simulation.
- **Restrained cyan accents:** tiny head dots, side reactor/engine status lines, pod windows and knee indicators, all using one muted emissive accent material built once. The bright scanner optics, original `SensorSocket` and `ScanBeam` remain the dominant cyan effect. Small muted gold/khaki service ID panels and slash markings suggest military hazard markings without glowing neon stripes.

**Inspector defaults:** `RobotSettings.ExtraMeshDetail = true` enables the added pieces. Set it false (and respawn the robot) to return to its previous art silhouette. `DetailGlowMultiplier = 0.30` scales only new small indicator emission relative to the existing main sensor energy, and can be set to 0 for non-glowing detail while leaving the scanner intact. These are **spawn-time** visual settings in `Configuration/RobotMaster.tres`; they don't require continuous processor calls or redraw logic.

**Socket/rig contract:** unchanged `BodyRoot`, `HeadPivot/SensorSocket`, `LeftHip/Knee/Foot` and `RightHip/Knee/Foot` names and coordinates. The original scene, `ArtOverride` path, raycaster, animated pivot geometry, `ModelScale`, collision, camera relevance and scan VFX setup are untouched. Decorative accents have no collision and must not occlude gameplay vision physics. The additional meshes share cached unit primitives/material instances within each robot, with no extra timers or CPU update callbacks. However, **more mesh surfaces increase render submissions/draw cost**; the detail toggle is provided for comparison and future LOD/batching if robot counts increase.

**Local validation required:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, inspect from the top-down angle and FPS test, and observe a full walk/stop/scan cycle. Look particularly at the side pod silhouette, foot clearances, bent cable attachment and cyan brightness in fog/night. Turn `ExtraMeshDetail` off on a test resource then reload to compare the original. If any geometry intersects awkwardly while bending, adjust only its local art offset—never a gait pivot or scanner socket. Code-review-only changes: no Godot/.NET compile or render pass was performed in this environment.

## MAT1 — procedural PBR surface finishes (implemented, awaiting Godot validation)

Material-only pass for the existing two-legged Robot_001. **No PNG textures, imported 3D assets, new lights, environment glow/bloom, AI, scanner-state changes, additional collisions or animation were added.** The old robot anatomy and socket/IK hierarchy are identical.

- `Artwork/Shaders/WeatheredMetal.gdshader` calculates paint variation, edge chips, grime, directional scuffs, metal/roughness variation for large **painted armour** plates.
- `Artwork/Shaders/IndustrialSurface.gdshader` gives **brushed steel, joint metal and rubber/cables** different roughness, metalness and surface grain without an image asset.
- `Artwork/Materials/` has four editable `ShaderMaterial` presets: `WeatheredArmour.tres`, `BrushedSteel.tres`, `JointMetal.tres`, and `IndustrialRubber.tres`. Individual presets link to one of the two shader files.
- `C/RobotMaterialFactory.cs` duplicates each needed preset **once per robot finish**, applies `RobotMaster.tres` parameters to the duplicates, and sends them to `RobotMeshBuilder.cs`. It never mutates the source presets, repeatedly loads materials during gait, or creates one resource per body plate.
- Primitive parts are already constructed as shared box/sphere/cylinder meshes scaled per piece. The shaders compute **object-local procedural coordinates multiplied by the mesh's geometry scale**, so weathering patterns follow each animated thigh/shin/head/foot part without sliding across the terrain. No authored UV maps are needed. These are shader illusions, not true displaced geometry/normal-map scratches.

### Configuration / rollback

In `Configuration/RobotMaster.tres`, under **MAT1 surfaces**, spawn-time tuning defaults are:

| Field | Default | Meaning |
| --- | --- | --- |
| `ProceduralMaterialsEnabled` | `true` | Use MAT1 shaders; set false and respawn to use previous flat materials |
| `ArmourWear` | 0.38 | Edge chip / exposed metal prevalence on armour |
| `ArmourDirt` | 0.28 | Uneven darker grime on armour; subdued dirt on metal joints |
| `ArmourScratches` | 0.30 | Long shallow scuffs on armour |
| `ArmourMetallic` | 0.75 | Armour PBR metallic response |
| `ArmourRoughness` | 0.60 | Average armour microsurface roughness |

`ArmourColor` and `JointColor` still define the main palette. `ExtraMeshDetail` still toggles geometry independently of the material system. **Settings are read when the robot is spawned**, not dynamically during F9 slider updates; resummon/reload to inspect a new material setting.

All cyan main optics, small indicator emission, scan-beam rendering and emission settings remain unchanged. **MAT0 (bloom/lighting) is deliberately deferred**.

### Performance / validation handoff

Two shared shader programs, four `.tres` templates and at most seven duplicated **material instances per robot** (armour, joints, insets, edges, recesses, cables and exposed steel), not hundreds. These shaders use a small number of math/noise evaluations on visible pixels; they have **more per-pixel GPU cost than the flat materials** even when actor CPU callbacks remain exactly as before. No extra nodes, polling, per-frame parameter updates, CPU terrain queries or additional texture-memory uploads.

Build `dotnet build war-machine.csproj`, then run `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` in Godot. Inspect armour chips vs. broad clean areas, brushed pistons, truly matte cables, cyan readability, fog visibility, night shading and moving joints/feet for texture swimming. Change one `ArmourWear` or `ArmourDirt` setting, respawn, and compare. Turn `ProceduralMaterialsEnabled=false` and respawn to compare old vs new. Check shader compile and look for **magenta/pink fallback or warnings**. For many robots, measure GPU before considering shader complexity/LOD reductions; no measured speedup is claimed.

**Validation actually done here:** source, shader/preset path checks and shallow syntax review only. Godot/.NET compile and rendered appearance remain awaiting the owner's local inspection.
