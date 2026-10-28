> **Current implementation (2026-10-28):** PH0/PH1 now have source implementations: hidden vitality, six independent regions, one-time death/control blocking and an event-driven silhouette. No precision hitboxes, injury consequences or enemy attacks yet. Build/runtime/visual acceptance pending. See IMPLEMENTATION_PLAN.md and Stats/README.md; earlier planned-only descriptions below are historical.

# PLAYER

Active player: `Player.tscn`, `PlayerController.cs`, `PlayerSettings.cs` and `Configuration/PlayerMaster.tres`. Movement, crouch, rig animation and weapon pose stay with the player. `Equipment/PlayerWeaponController.cs` adapts mouse aim/fire to reusable EQUIPMENT mechanics.

`C/PlayerMeshBuilder.cs` builds a lightweight **stylized soldier** with rounded/tapered geometry, a tactical silhouette and code-generated camouflage fabric, while retaining the cached rig, crouch/walk pivots and weapon socket. **C/ means procedural mesh generation only**, not general C# scripts. `Artwork/` is reserved for a future imported model; any replacement must retain or adapt the rig/socket contract needed by PlayerController. Walking uses phase-offset knee flex, ankle roll, torso counter-rotation, shoulder sway and lightly bending elbows driven by actual travel, with tweakable gait settings in PlayerMaster. See `C/README.md` for visual details and checks.

`LegacyFirstPerson/` contains the parked explorer, flight/FPS settings, camera mode button and scene. The original WORLD/testworld.tscn still instances that scene. Its resources are separate from the active player.

WorldController supplies a runtime PlayerSettings copy after safe terrain placement. Controls now include **Q quick shove** in addition to arrows, Shift run, Right Ctrl crouch and mouse aim/left click fire. Shared `Equipment/PlayerQuickMelee.cs` dispatches the COMBAT/Melee contact; pose/animation work is deferred. No new polling loops or behaviour changes are intended by this relocation. Local compilation and gameplay checks remain pending.

The isolated FPS experiment subclasses this controller under DEBUG/TEST/FirstPerson/PLAYER. Virtual input/direction hooks reuse this same physical movement and crouch implementation. The normal player still uses arrows and cached overhead axes.

FPS refinement adds a minimal virtual UpdateHorizontalVelocity hook; its base implementation preserves the prior MoveToward acceleration formula. Only DEBUG/TEST/FirstPerson overrides braking/speeds/ground settings. Local build and top-down regression check pending.

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
