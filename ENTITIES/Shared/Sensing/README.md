# Optional sensors and shared perception — RM3 and proposed PER stages

> **Implementation status:** The existing `ScanSensor` remains implemented as documented below. PER0/PER1 shared natural eyesight and crouch/cover sampling are implemented, awaiting local validation. PER2 memory is implemented; HE0 noise dispatch is implemented; AI hearing reactions and robot resolver integration remain unimplemented. See [PERCEPTION_IMPLEMENTATION_PLAN.md](PERCEPTION_IMPLEMENTATION_PLAN.md) for the Codex-ready staged design, reusable code layout, performance constraints and zombie-first test criteria.

**PER2 implemented; local validation pending:** zombies freeze the last verified position on LOS loss, independently sample early/mid/thorough travel with speed-aware give-up deadlines, and optionally look around. On giving up they settle and relocate their wander anchor to the actual stopping position (owner override of the older return-to-spawn plan). Wander goals get bounded support/clearance/direct-sweep checks; this is not navigation. No new timer or extra sight rays. See the perception plan's current PER2 handoff.

**Hearing proposal:** [HEARING_INVESTIGATION_PLAN.md](HEARING_INVESTIGATION_PLAN.md) describes spatial gameplay sound events (starting with successful gunshots), separate investigation of a recorded noise coordinate, and relocating the configurable wander anchor there. HE0 is implemented; HE1–HE3 and audio-plan AU3 reactions remain proposed; an audible rifle SFX through WorldAudio **does not yet alert AI zombies**. Sound playback, memory of a seen player and hearing of an event are deliberately separate.

Initialize ScanSensor with an actor, socket and ScanSensorSettings. RegisterCandidate/UnregisterCandidate explicitly supply Node3D targets; no groups, world enumeration or beam hitboxes. The full cone angle is in degrees, forward is socket -Z, range is world metres.

StartScan/StopScan are idempotent. Only an enabled sensor on an active actor can start; an empty candidate list leaves its Timer stopped. Registering a target can wake an already-requested scan. Tick first removes unloaded/freed candidates, then rejects by squared range and cone dot product, then performs capped round-robin LOS rays. Default cadence is 0.2s (5Hz) and at most two rays per tick. No valid candidates or inactive scanning means zero raycasts. A cached query excludes the owner's physics body. Hits on the registered target or its ancestor/descendant collider count as the target; a preceding wall blocks it.

TargetEntered, TargetSeen and TargetExited are demonstration events, not combat. Stop reports one exit per seen target and clears state; actor deactivation and ExitTree stop timers and release subscriptions/candidates. Mutate registration outside detection event handlers. Visual-only scanning can disable Sensor.Enabled; the independent beam still works.

Godot's ray query returns a result dictionary; there can be engine/query allocation at the bounded active tick. No allocation-free or measured profiler claim is made. Configure CollisionMask for the terrain/walls you need.

Validation is awaiting local build/runtime. Briefly check an in-cone target, out-of-cone/range target, wall occlusion, zero candidates, stop/restart and actor unload. Main-world integration registers only the explorer camera. No pursuit/damage is implemented.

## PER0 shared geometry contract

IPerceptionTarget provides world-space PerceptionHead/PerceptionTorso. PlayerController derives them from current interpolating capsule height, independently of either camera. VisibilityResolver owns one cached blocker-only ray query with observer/target excluded. CanSee must run in physics context after caller range/cone gates: clear head OR clear torso succeeds, max two rays per call. No timers, scene scans, target ownership, AI reactions or deferred budget state. Callers must budget two rays; this zombie pass checks just one explicitly registered target per timer tick.

PER1 zombies reuse their staggered brain timer and visual heading, with mask 17 (world + trunks). Movement masks are untouched. Glass remains a known blocker classification limitation for PER3. Existing robot ScanSensor below remains unchanged; PER4 will integrate it deliberately.

## HE0 shared noise events

`SoundStimulus` is an immutable world-position/kind/radius/intensity/sequence/source-ID snapshot. `SoundDispatcher` is world-local, has no history or idle loop, and exposes `Emitted`. The existing world AUDIO service owns the shared coordinator (`WorldAudio.SoundEvents`); its `Hearing` property exposes this independent route. Subscribe explicitly, retain the position rather than a live target, filter self/range/intensity in the listener, and unsubscribe on exit/death/deactivation. `IntensityAt(position)` supplies cheap squared-range rejection followed by linear distance falloff. There is no acoustic wall occlusion or navigation. No listeners are installed by HE0, so gunshots do not alert zombies yet. [API/loudness rules](../../../AUDIO/Shared/README.md).
