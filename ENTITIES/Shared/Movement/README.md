# Shared waypoint patrol — RM2

`PatrolController` is an optional Node initialized with an EntityCore, PatrolSettings and an explicit array of world-space waypoints. It copies route/settings, caches its actor, and never references robot art, terrain node paths or sensing.

`StartPatrol()` starts a stopped route. Repeated calls do not reset it. `PausePatrol()` preserves the waypoint, stationary phase and timer remainder, clearing velocity immediately. `ResumePatrol()` restores that phase; a finished wait goes to the next point without repeating ReadyToDepart. `StopPatrol()` cancels timers and resets the restart state. A zero/one-point empty route does not cause an automatic scene search. Start with at least two valid reachable points.

WalkingStarted, Arrived(index), StoppedAtWaypoint, ReadyToDepart and SpeedChanged events are the component contract. ReadyToDepart listeners can pause the patrol for another capability; robot scanning uses this. Moved lets an existing active motion callback assess distance relevance without another polling loop.

Only walking/turning enables PhysicsProcess. Arrival clears velocity and uses one one-shot physics Timer for settle/wait. Actor deactivation explicitly pauses the timer; reactivation resumes it. Exit stops the timer and disconnects actor/timeout handlers. Settings are copied; the species master owns authored motion defaults.

Kinematic gravity/floor snap and real horizontal speed are used. This is a direct flat-meadow route, not obstacle avoidance/navigation. Place reachable paths; it will not find a way around walls. No global manager or pathfinding framework is introduced.

Validation: source/lifecycle review and resource checks only; local `dotnet build war-machine.csproj` and a short two-cycle world check are pending because Godot/.NET are unavailable here. Check no sliding after stop, paused dwell remainder, disable/re-enable, and no repeated scan before departure.

### Patrol/grounding correction — 2026-10-26

Owner reported scanning without visible forward travel followed by falling through terrain. Patrol now records wait completion before invoking ReadyToDepart, so scan pause/resume advances to the next waypoint even if Timer.TimeLeft still reports a tiny positive value during timeout. Robot passes its injected terrain sampler to locomotion as well as visual feet. While walking, locomotion samples current/next/actual x/z and restores the body to the sampled ground height; it does not rely on viewer-centred Terrain3D collision coverage for vertical support. Ordinary actors without a sampler retain gravity. Missing terrain pauses safely. Stationary/disabled states do no grounding polling. Horizontal MoveAndSlide collision is preserved.

Source and whitespace checks only; no Godot/.NET runtime available here. Local check pending: observe departure after a scan, multiple waypoints and no sinking, including when the viewer flies away/returns. This fix has not been rendered or runtime-tested here.

## Wildlife relaxed goal mode (2026-10-28)

GroundGoalSampler.TryGoal and TryWander now accept optional strictSweep and maxHeightChange parameters. Zombies continue to use the original strict capsule CastMotion and 0.75m height window. Deer opts into relaxed terrain support and a chest-height line-of-sight ray between start/goal, while still checking destination capsule clearance; its real CharacterBody3D.MoveAndSlide retains obstacle collisions. This avoids some false rejections on uneven ground without creating navigation or additional continuous physics queries. Local testing is still required.
