# Optional sensors and shared perception — RM3 and proposed PER stages

> **Implementation status:** The existing `ScanSensor` remains implemented as documented below. Shared natural eyesight, crouch/cover target sampling, visibility-memory and hearing are **not yet implemented**. See [PERCEPTION_IMPLEMENTATION_PLAN.md](PERCEPTION_IMPLEMENTATION_PLAN.md) for the Codex-ready staged design, reusable code layout, performance constraints and zombie-first test criteria.

Initialize ScanSensor with an actor, socket and ScanSensorSettings. RegisterCandidate/UnregisterCandidate explicitly supply Node3D targets; no groups, world enumeration or beam hitboxes. The full cone angle is in degrees, forward is socket -Z, range is world metres.

StartScan/StopScan are idempotent. Only an enabled sensor on an active actor can start; an empty candidate list leaves its Timer stopped. Registering a target can wake an already-requested scan. Tick first removes unloaded/freed candidates, then rejects by squared range and cone dot product, then performs capped round-robin LOS rays. Default cadence is 0.2s (5Hz) and at most two rays per tick. No valid candidates or inactive scanning means zero raycasts. A cached query excludes the owner's physics body. Hits on the registered target or its ancestor/descendant collider count as the target; a preceding wall blocks it.

TargetEntered, TargetSeen and TargetExited are demonstration events, not combat. Stop reports one exit per seen target and clears state; actor deactivation and ExitTree stop timers and release subscriptions/candidates. Mutate registration outside detection event handlers. Visual-only scanning can disable Sensor.Enabled; the independent beam still works.

Godot's ray query returns a result dictionary; there can be engine/query allocation at the bounded active tick. No allocation-free or measured profiler claim is made. Configure CollisionMask for the terrain/walls you need.

Validation is awaiting local build/runtime. Briefly check an in-cone target, out-of-cone/range target, wall occlusion, zero candidates, stop/restart and actor unload. Main-world integration registers only the explorer camera. No pursuit/damage is implemented.
