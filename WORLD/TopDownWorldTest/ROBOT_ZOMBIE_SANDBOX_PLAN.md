# ZS — Robot-versus-zombie top-down sandbox

> **Plan only. ZS0–ZS3 are Planned and not implemented.**
>
> **Branch:** `implement-td0-td1`. **Owner:** `WORLD/TopDownWorldTest/`. This extends the existing playable orthographic sandbox, **not** the normal first-person world. Keep `WORLD/testworld.tscn`, its Terrain3D data, root robot defaults and existing weather/vegetation unchanged.
>
> **Goal:** See a giant, superior robot go from **cyan scanning → zombie enters cone → RED alert/target lock → visible weapon burst → zombie disintegrates → robot resumes cyan scanning/patrol**. The player can walk nearby, hide, or draw zombies into the robot's field of view. The robot should remain vastly more dangerous than individual zombies.

## Existing code to reuse

Read root `README.md`, `CodePreferences.md`, this folder's `README.md` and `IMPLEMENTATION_PLAN.md`, and the **current branch** source before implementation:

- `topdownWorldTest.tscn`, `C/TopDownWorldTest.cs`, `C/TopDownRobotPlacement.cs`, `C/TopDownPlayer.cs`, and `Configuration/TopDownTestMaster.tres`: TD0–TD2 scene, physical player, camera and **actual Robot_001 instance**, already integrated on the feature branch.
- `ENTITIES/TYPES/Robots/Robot_001/C/RobotController.cs`: existing biped/patrol/scan stage coordinator. The scan beam is owned privately here and the existing stage timer currently ends scans; don't fight its lifecycle from a second script.
- `ENTITIES/Shared/Sensing/ScanSensor.cs`: **already** has `RegisterCandidate`, `UnregisterCandidate`, `TargetEntered`, `TargetSeen`, `TargetExited`, bounded cone + optional LOS checks and a 0.2s scan timer. **Use it**, not a second detector or full scene-tree search.
- `ENTITIES/TYPES/Robots/Shared/Scanning/ScanBeam.cs` / `ScanBeam.gdshader`: single shader-driven cyan cone, no light or CPU beam tick. The current `Configure` method owns colour. Add a *small optional* alert-colour interface only if needed; do not create a duplicate scanning cone.
- `RobotController.InitializeEncounter(route, groundSampler, player)` is already used by the preview. Register/unregister zombies **after** the existing robot initialization; do not replace its route, gait, scan or existing player candidate.

**Status caveat:** TD0–TD2 are implemented on this feature branch but remain unverified by an automated Godot runtime here. TD3 stays a separate optional polish phase. Do not mistakenly assume the scene does not exist because the default `main` branch only carries the original plan.

## Phase overview

| Phase | Small outcome | Status |
| --- | --- | --- |
| **ZS0** | One simple low-poly zombie wanders/chases and has a clean death event | **Planned** |
| **ZS1** | Robot cone detects zombie, changes cyan → red and locks head onto it | **Planned** |
| **ZS2** | Short, visible weapon burst; hit confirmation and zombie disintegration; clean robot recovery | **Planned** |
| **ZS3** | Periodic small groups, configurable caps/cooldowns and a basic repeatable sandbox check | **Planned** |

Implement only user-authorized phases. If authorized together, keep the whole proof small and ship it in **one GitHub commit** to `implement-td0-td1`, not `main`.

## ZS0 — Zombie proof actor

**Goal:** One obviously undead, low-poly humanoid on the existing Terrain3D, with just enough behaviour to create encounters.

- **Art:** Reuse the primitive humanoid approach from `TopDownPlayer` but give it an original hunched silhouette, slower, uneven walking, muted colours and distinct face/head. No skeletal import, gore framework or complex graphics.
- **Physical:** A simple `CharacterBody3D` capsule with `MoveAndSlide`, terrain-grounded movement and sensible robot/player collision masks. Give the robot scanner an explicit torso/head aim target; the entity's root is near the feet and the sensor socket is high above it.
- **Logic:** Wanders between short seeded waypoints; notices nearby player at a modest radius and can shamble toward them. For now proximity can be threatening but **no need for a complete damage/survival system**. Turn/walk animation only when moving. Optional simple lunge cue when very close.
- **API:** `Kill/Desintegrate` (use a consistently spelled public name in code), `Died` event, `IsAlive` and one-off cleanup. Death idempotent: unregister targeting and collision first, play bounded effect, then release.
- **Perf:** No per-bone processors, no global zombie search, no expensive navigation on the unobstructed test hillside, dormant when disabled/out of active area. Keep initial count at **1–3**.
- **Pass:** Zombie appears, animates, moves on the ground, can be lured near robot, and disappears cleanly when explicitly killed.

## ZS1 — Red acquisition and physical lock-on

**Goal:** Preserve the normal cyan sweep until a valid candidate actually crosses its scan cone.

- Register zombies with the **existing** `Robot.Sensor`; use `TargetEntered` for acquisition and `TargetExited` / target state for loss. Use existing range, cone and bounded LOS, not a distance-only shortcut. Ensure collision layers, ray exclusions and target aim height are correct for small zombies.
- When a zombie is detected **during a real scanning stage**, pause the sweep, change that same cone from cyan to **strong red**, rotate the head toward the zombie's upper body, and dwell for a short, visible **lock-on delay** (~0.8–1.2s). Avoid instant fire; the player should clearly read *acquire → aim → fire*.
- Important architectural point: `RobotController` owns both the sweep in `_Process` and the scan/recovery timer. Extend it through a **small optional target-acquisition/combat hook or coordinator state**, instead of externally forcing `HeadPivot.Rotation` every frame against the existing sweep. Preserve the exact existing patrol/scan when there are no combat targets and preserve main-world behaviour by default.
- If the zombie dies, despawns, exits a valid LOS/range window or the robot deactivates during lock, cancel safely, return the cone to cyan and resume the normal bounded scan/recovery. Don't leave a red beam, stuck timer or permanently paused patrol.
- **Target priority for this first pass:** zombies are the only shootable targets. The existing player candidate may still be detected; optionally display a player-warning/alert indication, but **do not kill or damage the player in this sandbox yet**.
- **Pass:** Repeated valid entries trigger red head tracking exactly once per acquisition; a passing non-target does not; cancelled scans reliably reset to cyan.

## ZS2 — One cheap robot weapon and stylized splat

**Goal:** Visually establish how overpowering the robot is without a full weapon framework.

- **Weapon choice for this prototype:** a **short machine-gun burst** (easier, cheaper and clearer than missiles); reserve guided missiles for a later pass if the scene looks promising. Use one simple muzzle-point/socket near the upper body and a brief muzzle flash/tracer sequence.
- After the lock-on dwell, fire several visible shots across ~0.5–1.0s toward the acquired zombie. Simple **hitscan with short-lived tracers** is enough. Confirm hit position/line of sight against the preview terrain; do not shoot through a hill or another blocking body just because a target is registered.
- On the first confirmed lethal hit (or one brief burst), kill the zombie exactly once and show a **small low-poly fragmentation / ash-puff disintegration** that lasts ~0.5–1.5s. Reuse a small pooled one-shot effect or one lightweight particle emitter; no blood decals, rigidbody piles or hundreds of physics fragments.
- Robot keeps its normal 3D biped body, gait, cyan beam and audio. Red alert and weapon flash are temporary. After shooting, play a tiny recovery delay; restore cyan and allow the **existing** patrol/scan state machine to proceed. If target is lost or out of LOS, don't fabricate a hit.
- **Pass:** One zombie crosses cone → red lock → tracer burst → visible splat → robot returns to ordinary patrol without double shots or stuck timers.

## ZS3 — Periodic mini-sandbox

**Goal:** Repeat the scene without restarting Godot to see emergent moments.

- A **test-only** spawn controller periodically introduces **1–3 low-poly zombies**, optionally one loose group of 3–5, from safe positions outside immediate player/robot collision. Initial interval ~**15–30s**, hard cap **8–12 living zombies**. These are provisional tuning ranges, not immutable numbers.
- Zombies may wander toward the player and into the robot's path; the player can lure them intentionally. Robot only attacks a valid sensed zombie, never teleports/turrets onto unseen enemies.
- Add settings in the existing **test-only** `TopDownTestSettings.cs` / `TopDownTestMaster.tres`: zombies enabled, spawn interval/count/cap, wander speed, notice radius, acquisition seconds, beam alert colour, burst shots/timing and splat effect quality. Only expose knobs that are actually implemented, with sensible clamps.
- A lightweight debug label can show **Zombies alive / Robot stage / Lock target**, updated on signals/state changes. An optional **Spawn Now** key/button is useful for quick testing; not essential.
- **Performance:** spawn/cleanup on an infrequent timer, movement/physics only for active nearby zombies, stagger low-frequency awareness checks, one currently locked robot target, use existing scanner's limited LOS budget, reuse meshes/materials, no frame-based full-scene searches. A dozen zombies is enough for this proof; don't build a huge horde framework yet.
- **Pass:** Zombie arrivals continue for several cycles, occasional robot splats are clearly readable, hard cap respected, disposing a zombie unregisters it from `ScanSensor`, robot survives several attack cycles, zero zombies eventually returns to dormant sensor checks.

## Suggested files (create only when needed)

```text
WORLD/TopDownWorldTest/
├── topdownWorldTest.tscn           # Extend the existing scene
├── C/
│   ├── TopDownWorldTest.cs        # Existing composition
│   ├── TopDownRobotPlacement.cs   # Existing real robot adapter
│   ├── TestZombie.cs              # Low-poly actor + movement/death
│   ├── ZombieSpawnDirector.cs     # Test-only timed spawns, registration
│   └── RobotZombieSandbox.cs      # Event bridge / encounter orchestration
├── Configuration/
│   └── TopDownTestMaster.tres      # Existing preview master, add small controls
├── ROBOT_ZOMBIE_SANDBOX_PLAN.md
└── README.md
```

If red-beam/weapon coordination requires a few **shared robot methods** (for example an optional `TryAcquireCombatTarget` / `CancelCombatTarget` or beam-colour setter), keep those tightly scoped and **OFF by default outside the sandbox**, so existing TestWorld robot patrol and scanning remain unchanged.

## Acceptance and boundaries

1. **Hierarchy:** robots annihilate zombies quickly and look intimidating; zombies are a nuisance to the player, not a serious danger to the robot.
2. **Readable timing:** blue/cyan search, decisive RED lock, focused weapon flash and visible disappearance; return to blue/cyan afterward.
3. **Robustness:** no stuck red beam, lock-on a deleted target, duplicate death events, dead zombies in sensor candidate lists, timers after scene unload or loss of original patrol.
4. **FPS:** camera type alone does not guarantee speed. Keep the same low-poly art, fixed limited area and small zombie cap. Scene visibility, active physics/AI, scan rays, shadowing and transparent VFX drive actual cost. Check FPS/frametime locally against a similar camera composition when practical; do not claim measured improvements without measurements.
5. **Isolation:** Never populate the original `WORLD/testworld.tscn` with zombies, change its weather/vegetation/terrain data, or weaken the main robot's existing non-combat flow.
6. **Validation:** brief file/reference/source checks and build/launch when available, no extensive tests unless requested. Update this plan + feature README, **one commit per authorized job**, push the existing feature branch, and state precisely which gameplay/visual checks remain for the user.

## Codex prompt

Read `CodePreferences.md`, root `README.md`, `WORLD/TopDownWorldTest/README.md`, this plan and current robot/scanner code on **branch implement-td0-td1**. Implement **ZS0–ZS2** as a small, clear encounter first: low-poly animated zombie on the preview Terrain3D; real Robot_001 scanning cyan, turning its existing beam RED on valid cone detection, holding eye/head lock briefly, firing a short machine-gun/tracer burst, disintegrating the zombie and cleanly returning to its existing patrol/scan. Preserve original world and robot behaviour outside the preview. Don't implement full survival, horde AI or missile physics. Keep validation brief and commit/push once. Implement **ZS3 periodic spawning** only when separately authorized.
