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
