# Shared corpse lifecycle — ZD0–ZD3

ZD0–ZD3 are source implemented, **awaiting local compilation, native physics, visual and performance acceptance**. Existing normal/runner zombie deaths are the only corpse integrations. Player movement has an optional slowdown receiver, disabled by default; player death, wildlife death, articulated skeleton ragdolls, detached debris, persistence, corpse loot and navigation are not implemented here.

## Ownership and lifecycle

WorldController creates one independent Corpses root and one CorpseManager, shared in World/FPS. The manager receives the player as distance focus. Zombie death captures its monotonic timestamp before deferred handoff, injury snapshot/pose and original visual hierarchy, then shuts down living AI/hearing/vision/collision. Original meshes/materials and missing-limb visibility transfer unchanged. No reconstruction or second damage path.

CorpseBody owns the existing bounded one-box physical fall and species-independent ICorpsePoseAdapter. Supported horizontal bodies freeze with final pose; unsupported/upright/runaway falls still dismiss. Settled bodies retain one frozen proxy on **physics layer 9 (256)**, contacted only by temporary corpse falls (mask 257). Layer 8 is already camera obstruction and remains separate. Actors, firearms, sight/hearing rays and camera queries do not include layer 9. Fallen bodies can layer through native contacts; no simulated limbs or persistent moving corpse physics. Proxy shapes are approximate even for missing limbs, but cannot form phantom living-actor blockers.

## ZD1 — soft piles

Each settled corpse adds one shallow world-aligned CorpseSlowArea box, built once from its transformed proxy bounds. It monitors living layers 2/3 (mask 6), with no solid collision or process callback. ICorpseSlowable is the opt-in species-neutral contract; CorpseMovementModifiers caches the combined multiplier on enter/exit only. One footprint gives 0.8x desired movement speed; additive overlapping penalties cap at 0.65x, never exponential stacking. No change to gravity, shove displacement, input, perception or MoveAndSlide. Both zombies opt in. PlayerSettings.CorpseSlowdownEnabled is explicitly false in PlayerMaster; enable before entering a pile to test both views. Runtime toggle changes while already overlapping require leaving/re-entering the footprint.

Exit, sinking, budget retirement, actor exit or scene teardown releases applicable modifiers. Areas hold only bounded current occupants and skip freed bodies on cleanup. No per-frame proximity scans or extra queries. Inactive actors do not simulate; their cached overlap multiplier remains ready for reactivation while still inside. The player is not slowed until explicitly enabled. Other species can adopt the same interface later.

## ZD2 — aging and sink

One shared 1-second physics timer runs while any corpse is registered. Lifetime defaults to 300s from death, with at most one timer cadence of start delay. It is independent of visibility. Expiry immediately disables pile contact and clears area slowdown, freezes any still-active fall, then starts an 8s downward visual sink (2.2m). The manager enables its frame callback only while sinking; it animates registered sinking roots, never per-limb geometry/materials. No fade/material duplication. At completion the entire body/area/visual hierarchy is freed and deregistered. Empty registry stops the timer and frame callback. Settled bodies have no integrate/process/physics callbacks; their signal-driven areas still monitor overlaps.

## ZD3 — bounded cost

Manager defaults: 96 retained corpses, 8 simultaneous physical falls, physical-fall distance 45m from the supplied player focus. Count checks occur on spawn, not per frame. Nearby recent deaths get fall simulation while slots remain. Distant or overflow deaths use a deterministic immediate lying pose after one support ray and one clearance overlap on a one-shot physics callback. Already-low bodies keep their lying orientation. Unsupported/blocked fallback placement dismisses rather than freezing in air/walls. No backlog or repeated fallback search. Without a focus (standalone fixture), distance filtering is disabled but count limits remain.

At the retention cap the oldest settled corpse retires immediately, releasing effects before new handoff. If none settled, the oldest entry retires. Pressure retirement deliberately skips the long sink to keep a hard cap, including queued-for-deletion bodies whose contact/simulation is disabled immediately. Original ZD0 dismissal still applies to failed falls. Active falls collide with settled piles only, not each other. No live character hard collider is retained.

**Debug → Print corpse budgets** prints retained/max, active falls/max, settled, sinking, fallback and retirement counts on demand. No debug polling. Global caps bound the registry/timer scan/active sink work; native fall work is capped separately. One-shot fallback physics processing stops immediately. This is world-local ownership and world-unload cleanup, not actor/chunk streaming or persistence; current WS2 streams static buildings only. Future actor streaming must explicitly free/deregister associated bodies, not duplicate them on re-entry.

## Settings and validation

Configuration/CorpseMaster.tres owns fall, pile, TTL/sink and budgets. Settings are duplicated and validated before handoff. Maximum caps are manager policy; per-spawn profiles can customize fall/lifetime/pile tuning without bypassing global budgets. Death timestamp defaults to spawn time only for non-zombie direct callers without one.

Performed here: source/lifecycle review, resource/loop/whitespace checks and numeric overlap/cap/lifetime model. No Godot/.NET executable available; **compilation and runtime not verified**. Existing DEBUG/Corpses smoke assertions were updated for the pile-only settled layer; no new test scenes created and the smoke was not run.

Local checks: `dotnet build war-machine.csproj`; F6 World and FirstPersonWorld. Kill both species, including severed/crawling ones, verify final injuries remain through fall and sink. Kill several at one spot and walk zombies through piles: mild capped slowdown, no trapping, and existing shove/gravity intact. Player remains unaffected by default; optionally enable its config and test both views. Inspect layers and verify bullets/sight/camera ignore piles. Use temporary TTL=5/SinkSeconds=2 and small caps (e.g. max corpses 4/falls 2) for expiry/overflow checks, then restore production values. Confirm simultaneous kills never exceed active cap, nearby/distant fallback safely supports or dismisses, oldest settled retires, and no invisible effects after sink or world reload. Check slopes/cliffs/roofs/caves, pile support, frame time and object counts with 20+ deaths. No performance result claimed from source checks.
