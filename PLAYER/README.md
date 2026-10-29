> **FPS ownership:** Active FPS presentation is promoted out of DEBUG/TEST. See the [FPS ownership guide](../WORLD/FirstPerson/README.md). Shared gameplay and current tuning are unchanged; local validation pending.

> **PSR0 implemented in source (2026-10-28), awaiting local validation:** PlayerController is now a small lifecycle/API coordinator with private plain helpers under [Input](Input/README.md), [Movement](Movement/README.md) and [Visuals](Visuals/README.md). Existing movement/stance/pose maths, door contact, FPS hooks, events, settings and idle sleep are preserved. Current source bindings: arrows, Shift, Right Ctrl crouch, `/` prone, RMB jump. No scene or terrain changes. [Refactor plan and checks](IMPLEMENTATION_PLAN.md#psr0--player-script-structure-refactor).

> **Current implementation (2026-10-28):** PH0/PH1 now have source implementations: hidden vitality, six independent regions, one-time death/control blocking and an event-driven silhouette. No precision hitboxes, injury consequences or enemy attacks yet. Build/runtime/visual acceptance pending. See IMPLEMENTATION_PLAN.md and Stats/README.md; earlier planned-only descriptions below are historical.

# PLAYER

Active player: `Player.tscn`, `PlayerController.cs`, `PlayerSettings.cs` and `Configuration/PlayerMaster.tres`. Movement, crouch, rig animation and weapon pose stay with the player. `Equipment/PlayerWeaponController.cs` adapts mouse aim/fire to reusable EQUIPMENT mechanics.

`C/PlayerMeshBuilder.cs` builds a lightweight **stylized soldier** with rounded/tapered geometry, a tactical silhouette and code-generated camouflage fabric, while retaining the cached rig, crouch/walk pivots and weapon socket. **C/ means procedural mesh generation only**, not general C# scripts. `Artwork/` is reserved for a future imported model; any replacement must retain or adapt the rig/socket contract needed by PlayerController. Walking uses phase-offset knee flex, ankle roll, torso counter-rotation, shoulder sway and lightly bending elbows driven by actual travel, with tweakable gait settings in PlayerMaster. See `C/README.md` for visual details and checks.

`LegacyFirstPerson/` contains the parked explorer, flight/FPS settings, camera mode button and scene. The original WORLD/testworld.tscn still instances that scene. Its resources are separate from the active player.

WorldController supplies a runtime PlayerSettings copy after safe terrain placement. Controls now include **Q quick shove** in addition to arrows, Shift run, Left Ctrl/C crouch, Right Ctrl prone and mouse aim/left click fire. Shared `Equipment/PlayerQuickMelee.cs` dispatches the COMBAT/Melee contact; pose/animation work is deferred. No new polling loops or behaviour changes are intended by this relocation. Local compilation and gameplay checks remain pending.

The isolated FPS experiment subclasses this controller under WORLD/FirstPerson/PLAYER. Virtual input/direction hooks reuse this same physical movement and crouch implementation. The normal player still uses arrows and cached overhead axes.

FPS refinement adds a minimal virtual UpdateHorizontalVelocity hook; its base implementation preserves the prior MoveToward acceleration formula. Only WORLD/FirstPerson overrides braking/speeds/ground settings. Local build and top-down regression check pending.

## Top-down camera-relative movement — rotation fix

`PlayerController` still caches ground-plane screen-right and screen-forward axes rather than recomputing them every physics tick. The top-down `Camera.SetYaw()` now emits its existing `SettingsChanged` event, which refreshes those axes immediately. This keeps arrow-key walking relative to the *current* screen direction after the camera's 0–360° rotation slider changes, including while a movement key is held; tilt changes are handled as well. A previous camera subscription is removed on reinitialization and unload. No new update loop, movement input mapping, character acceleration, mouse aim, camera behaviour or FPS controls were changed. The FPS test overrides `GetMovementDirection()` and continues sampling the eye camera's current basis for movement.

**Local check pending:** build `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, rotate the Camera slider to 0°, 90°, 180°, 270° and 360°, and verify arrow directions stay screen-relative while moving and after stopping. Briefly F6 the FPS test and verify arrow-key movement with mouse-look is unchanged. This was inspected statically only; no in-engine validation or terrain rebake.

## Quick shove momentum feel

The Q shove is usable while standing, walking or running in both overhead and FPS. Only its impact strength changes: `PlayerQuickMelee` samples real XZ speed once on attack start and scales the reaction through the three new `QuickShoveMaster.tres` movement tuning fields; no extra input, animations or permanent tick loops. Zero-speed pushes are deliberately small, full-speed runs considerably stronger. Knockback remains collision-aware in the zombie receiver and no HP is lost by default. Awaiting in-game tuning.

CM2-D now adds a two-handed side-on gun shove using the existing arm/elbow rig, with an active-only PlayerShovePose helper. Prior no-animation statements describe the earlier CM2-C snapshot. See Equipment/README.md for checks and cancellation. Local animation/build acceptance pending.

## Health/vitals and visual feedback planning (2026-10-28)

**PH0–PH5 remain Planned; no player HP/death logic added yet.** The detailed Codex-ready contract already exists in [PLAYER/IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) and has been updated for the new FEEDBACK ownership. [Stats/README.md](Stats/README.md) reserves the player-owned vitals location, but contains no scripts.

[FEEDBACK/](../FEEDBACK/README.md) is a **documentation/folder scaffold** for reusable health/defence bars and visual hit feedback; its [FB roadmap](../FEEDBACK/IMPLEMENTATION_PLAN.md) is separate from player-health implementation. PH0 first adds actual HP, healing and death plus a tiny diagnostic readout. A later approved FB1 (coordinated with PH1) creates the styled HUD bar under FEEDBACK, composed by UI/GameplayHUD. Enemy bars (FB2) and an actual shield resource/bar (FB3) remain independent.

F10 is an on-demand movement collision probe for both shared-player views: hold movement against a blocker and press it to print existing MoveAndSlide collider paths/normals/contact points once. No additional physics queries or background logging; normal movement and idle sleep are unchanged.

## RC1 top-down recoil (2026-10-28)

`Equipment/TopDownWeaponRecoil.cs` adds recoil animation to **Visual** only; `Equipment/PlayerWeaponController.cs` supplies the top-down camera's up axis to shared accepted-shot recoil and handles `ShotAccepted`. `PlayerController.SetWeaponRecoilPose` adds a small shoulder/arm response to the rig without changing `WeaponMount` or capsule. Both views still share actual firearm mechanics but FPS integration is RC2. The visual pose cancels on Q shove, swap, backpack and focus loss. No constantly running player animation callback; actual build/visual QA is pending.

## PH0/PH1 health-plan revision — anatomical survival (2026-10-28)

The authoritative [implementation plan](IMPLEMENTATION_PLAN.md) now rejects a conventional player 100-HP HUD bar. Future PH0 uses a hidden 100-point **Vitality** reserve for death/critical damage **plus six individual body-region conditions** (head, torso, left/right arms, left/right legs). Injured arms/legs must not directly deplete Vitality just because of ordinary damage. PH1 renders a small six-part coloured silhouette near the hotbar and leaves existing NPC health bars intact. A detailed Health tab, realistic wounds/bleeding/fractures, exact region hurtboxes and their movement/aim penalties are separately gated work. [Design detail](Stats/BODY_CONDITION_DESIGN.md). **Plan only; no player health scripts, collision shapes or UI were created.**

## RMB grounded jump (2026-10-28)

The **shared** PlayerController now accepts **right mouse press** to jump once, only when on the floor, standing, alive, focused and not in an input-blocking menu or over a GUI control. The press is consumed by the next physics tick, then cleared; holding RMB cannot auto-repeat when landing. The new exported `JumpVelocity=6.0`m/s in `PlayerMaster.tres`, combined with existing `Gravity=20`m/s², gives roughly 0.9m ideal ballistic height on level ground before collision. Normal native `MoveAndSlide` handles ceilings, falling and landing. No special jump animation, stamina cost, double-jump, new InputMap action or new always-running callback was added. The existing rest-state physics sleep is preserved once grounded. FPS debug reuses the same impulse but rejects RMB if its mouse is uncaptured. This is implemented in source; local Godot/.NET compilation and actual in-game movement require testing.

## Prone/crawling — shared overhead and FPS stance (2026-10-28)

**Implemented in source; local Godot/.NET build and visual acceptance pending.** The same `PlayerController` implements three mutually exclusive stances, preserving the existing movement/aim/equipment flow. **Right Ctrl toggles prone**, **Left Ctrl or C toggles crouch**, and RMB still performs a grounded standing-only jump. Keypresses are handled in `_UnhandledInput`, respect gameplay/menu/death blocking, ignore key auto-repeat and wake the existing sleeping player physics callback; no new permanent polling loops.

Default tuning comes from `Configuration/PlayerMaster.tres` and `PlayerSettings`:
- Standing: 1.8m vertical capsule, 5m/s walk; Shift sprint retains 1.6×.
- Crouching: 1.4m capsule, 0.45× walk = 2.25m/s.
- **Prone: 0.75m short capsule, 0.17× walk = 0.85m/s** (much slower). Shift cannot sprint; RMB cannot jump until standing.
- `ProneSpeedMultiplier` and `ProneCollisionHeight` are independently adjustable in the player master. The FPS test copies the shared prone defaults alongside its existing FPS walk/run/crouch overrides.

When prone, the existing upper-body, leg/arm and weapon rig pivots take a **simple forward-leaning crawl pose** with alternating small arm/leg motion only during actual travel. No separate animation tree or 3D asset. The short upright capsule is deliberately a **first-pass low-clearance proxy**, *not* a full-length horizontal prone collider or precise limb hitbox; proximity to walls/long torso clipping may still need later geometry work. WeaponAim, recoil and existing body-condition health stay in the shared actor; the FPS camera continues reading the now-shorter `CapsuleHeight` via `RecordPhysicsPose` (approximately 0.60m eye height).

**Stance transitions / clearance:** Right Ctrl returns to the stance that was active before prone, when there is headroom; a blocked return to standing tries crouch instead. Crouch requests while prone also check 1.4m headroom. Standing checks 1.8m headroom. These are cached one-time capsule overlap checks in the physics callback; don't repeatedly poll when refused. `IsCrouching` retains its crouch-only meaning for old observers; `IsProne`, `CurrentStance` and `MovementStanceChanged` make stance visible to later sound/stamina/AI systems.

**Test locally:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Right Ctrl while grounded: avatar folds forward/crawls, capsule and FPS eye lower, slow movement in all directions. Shift gives no speedup, RMB no jump. Right Ctrl again returns to previous stance; Left Ctrl/C crouches independently. Under a low ceiling, release toward full height must be refused; crouch fallback works if space allows. Check aiming, rifle/quick shove, animation/feet on slopes, inventory blocking, F10 collisions, idle physics sleep and reload/reinitialize. **No compiled or rendered checks were run via GitHub tools.**

## L0 — shared hold-to-lean (2026-10-28)

**Implemented in source, awaiting local validation.** Hold **Numpad 1** left or **Numpad 2** right; release to return upright in about 0.18 seconds. Holding both is neutral. Standing and crouching support lean; prone, blocked gameplay, death, focus loss and uncaptured FPS input return to upright. Shared movement speed while held is 0.55× the normal stance-adjusted speed. Top-down rolls and shifts the procedural upper body (not its camera); FPS rolls and shifts the camera and its attached gun. Capsule/colliders, weapon ballistics and zombie perception stay unchanged. Shared tuning lives in `PlayerSettings` / `Configuration/PlayerMaster.tres`; FPS view tuning lives in `FirstPersonMaster.tres`. See [L0 plan](LEAN_IMPLEMENTATION_PLAN.md).
