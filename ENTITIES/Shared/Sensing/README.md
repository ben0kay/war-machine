# Optional sensors and shared perception — RM3 and proposed PER stages

> **Implementation status:** The existing `ScanSensor` remains implemented as documented below. PER0/PER1 shared natural eyesight and crouch/cover sampling are implemented, awaiting local validation. Memory/hearing and robot resolver integration remain unimplemented. See [PERCEPTION_IMPLEMENTATION_PLAN.md](PERCEPTION_IMPLEMENTATION_PLAN.md) for the Codex-ready staged design, reusable code layout, performance constraints and zombie-first test criteria.

**Hearing proposal:** [HEARING_INVESTIGATION_PLAN.md](HEARING_INVESTIGATION_PLAN.md) describes spatial sound events (starting with successful gunshots), per-zombie investigation of a recorded noise position, and relocating the configurable wander anchor there. HE0–HE3 are proposed only; no hearing behaviour is implemented yet.

Initialize ScanSensor with an actor, socket and ScanSensorSettings. RegisterCandidate/UnregisterCandidate explicitly supply Node3D targets; no groups, world enumeration or beam hitboxes. The full cone angle is in degrees, forward is socket -Z, range is world metres.

StartScan/StopScan are idempotent. Only an enabled sensor on an active actor can start; an empty candidate list leaves its Timer stopped. Registering a target can wake an already-requested scan. Tick first removes unloaded/freed candidates, then rejects by squared range and cone dot product, then performs capped round-robin LOS rays. Default cadence is 0.2s (5Hz) and at most two rays per tick. No valid candidates or inactive scanning means zero raycasts. A cached query excludes the owner's physics body. Hits on the registered target or its ancestor/descendant collider count as the target; a preceding wall blocks it.

TargetEntered, TargetSeen and TargetExited are demonstration events, not combat. Stop reports one exit per seen target and clears state; actor deactivation and ExitTree stop timers and release subscriptions/candidates. Mutate registration outside detection event handlers. Visual-only scanning can disable Sensor.Enabled; the independent beam still works.

Godot's ray query returns a result dictionary; there can be engine/query allocation at the bounded active tick. No allocation-free or measured profiler claim is made. Configure CollisionMask for the terrain/walls you need.

Validation is awaiting local build/runtime. Briefly check an in-cone target, out-of-cone/range target, wall occlusion, zero candidates, stop/restart and actor unload. Main-world integration registers only the explorer camera. No pursuit/damage is implemented.

## PER0 shared geometry contract

IPerceptionTarget provides world-space PerceptionHead/PerceptionTorso. PlayerController derives them from current interpolating capsule height, independently of either camera. VisibilityResolver owns one cached blocker-only ray query with observer/target excluded. CanSee must run in physics context after caller range/cone gates: clear head OR clear torso succeeds, max two rays per call. No timers, scene scans, target ownership, AI reactions or deferred budget state. Callers must budget two rays; this zombie pass checks just one explicitly registered target per timer tick.

PER1 zombies reuse their staggered brain timer and visual heading, with mask 17 (world + trunks). Movement masks are untouched. Glass remains a known blocker classification limitation for PER3. Existing robot ScanSensor below remains unchanged; PER4 will integrate it deliberately.
