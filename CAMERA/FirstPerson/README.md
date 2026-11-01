> **Current ownership:** Production FPS adapter/profile in `CAMERA/FirstPerson`. [View composition and settings](../../WORLD/FirstPerson/README.md). Older test-folder statements below are historical; current paths and behaviour are defined by the promoted source.

# CAMERA

`FirstPersonCamera.cs` replaces overhead follow with eye-height perspective and mouse look. Settings are in ../../WORLD/FirstPerson/Configuration/FirstPersonMaster.tres. No per-frame follow loop.

Event-driven render translation between physics snapshots replaces tick-only positioning. Mouse rotation uses ScreenRelative immediately and preserves InvertMouseY=true. Rendering sleeps when eye interpolation and head bob have both settled. See parent README for tuning and pending local checks.

## FPS weapon-camera recoil (2026-10-28)

`FirstPersonCamera.SetWeaponRecoilPitch(float degrees)` applies **temporary additive pitch** on top of the user's persistent `_lookPitchDegrees` mouse aim. The existing mouse inversion, sensitivity, yaw and `MaximumPitch` bound remain in force. The FPS equipment adapter drives this only while RC0 gun recoil is active; it smooths the view rise and resets the temporary pitch on interrupt. This avoids a second ballistic recoil offset and prevents losing mouse correction when recoil settles. The camera does **not** add another permanent `_Process` loop. Local FPS and boundary/capture tests pending.

## FPS recoil pitch is now permanent until mouse correction (2026-10-28)

The current `FirstPersonCamera.ApplyWeaponRecoilKick` adds **positive pitch to the same `_lookPitchDegrees` controlled by mouse look**, bounded by existing `MaximumPitch`. It does not maintain a temporary camera offset or automatically subtract the recoil on release. This supersedes the older `SetWeaponRecoilPitch` additive recovery described above. The FPS adapter alone schedules those impulses; the camera has no new process loop. Other camera movement/input and the primary top-down view are unchanged. Use mouse movement to correct aim; local test needed.

## FPS-HB0 walking head bob — implemented in source (2026-10-28)

`FirstPersonHeadBob.cs` is a plain, FPS-only helper with **no extra Node or timer**. The player supplies actual grounded post-slide horizontal travel to `FirstPersonCamera.RecordPhysicsPose()`. The camera's existing interpolation callback adds tiny camera-right and world-up bob offsets, with walk/run/crouch amplitudes and stride lengths from `../../WORLD/FirstPerson/Configuration/FirstPersonMaster.tres`. The head returns smoothly to zero when movement stops; idle processing sleeps. Prone/airborne/stationary/dead/blocked/uncaptured cannot start new bob. Mouse-look/recoil orientation, player capsule, camera interpolation, aiming and top-down view remain unchanged.

**Gentler bob tuning (2026-10-28):** walk 1.2cm vertical / 0.6cm lateral; sprint 1.8cm / 1cm; crouch half strength, prone disabled; return 0.18s. The vertical wave was reduced from 2 cycles per phase rotation to 1, with a slower, alternating side sway. Cycle distance now 2.8m walking and 3.3m running. At configured 5m/s walk and 8m/s run this yields roughly **1.8 / 2.4 vertical bounces per second**, instead of 6.25 / 8.0. This is still distance-paced procedural motion, **not a foot-animation-synchronized step event**. [Plan and validation handoff](HEAD_BOB_IMPLEMENTATION_PLAN.md). **Source implemented, local compile and FPS visual/comfort testing pending.**

## L0a — FPS lean visual smoothing (2026-10-28)

The initial L0 directly assigned camera Z tilt on physics ticks; this could show visible increments on higher-refresh displays and left a stationary sideways eye offset stale after mouse look. `FirstPersonCamera` now records the shared physics lean as a **target** and interpolates its displayed roll and eye shift together at render frequency in the **existing** callback (frame-rate independent exponential blend, authored `LeanRenderSmoothingSeconds = 0.045`). The render callback wakes for changes and sleeps once eye interpolation, head bob and lean are all settled. Mouse look and recoil still change persistent pitch/yaw immediately; they wake a one-frame position refresh when already leaned. FPS values are now **16° tilt, 0.20m shift**, with no additional weapon node/process. Local in-engine checks pending, especially thin-wall clipping.

## L0b stronger cosmetic corner peek (2026-10-28)

Authored FPS camera tilt is **18°** and lateral eye shift is **0.35m**, increased from 16° and 0.20m after side-by-side in-game screenshots. `FirstPersonCamera.BobbedEye` now clamps configurable side shift at 0.50m rather than 0.25m (with matching `FirstPersonSettings` Inspector hint) so the new value is not silently reduced. Existing event-driven render smoothing, camera-mounted weapon, recoil/mouse input and idle sleep remain unchanged. Verify interior geometry clipping locally; physical cover exposure is still deferred.

## CF0 — Shared event-driven camera feedback (2026-10-31)

**Implemented in source; local build/FPS visual testing pending.** The only feedback owner is `FirstPersonCamera`, using a plain `FirstPersonCameraFeedback` helper alongside `FirstPersonHeadBob`. No new scene node, global singleton, second render loop, rotation controller or top-down changes. Call `camera.TriggerFeedback(CameraFeedbackKind.MeleeSweepHit | MeleeOverheadHit | PlayerDamaged)` for new **confirmed gameplay events**, rather than changing camera transforms from actors/weapons.

Three initial FPS cues, adjustable through **`WORLD/FirstPerson/Configuration/FirstPersonMaster.tres`**: successful bat LMB contact (**0.010 m / 0.11 s**), bat MMB heavy contact (**0.018 m / 0.15 s**), and resolved positive player damage (**0.022 m / 0.18 s**). `CameraFeedbackEnabled=true`, `CameraFeedbackGlobalStrength=1.0`, `CameraFeedbackFrequencyHz=16`, and `CameraFeedbackMaximumOffset=0.04 m`. `0` strength or duration disables an individual cue; the global enabled flag disables all. These are small camera-space X/Y translations, with smooth squared decay, **not permanent mouse movement, pitch, lens FOV or physical knockback**. Exact visible feel needs local inspection.

**Event wiring:** `MeleeWeaponController.ContactConfirmed(MeleeStrikeKind)` emits **once per accepted attack** after the first successful shared hit dispatch; three zombies hit by LMB cause one camera cue, not three. `FirstPersonWeapon` subscribes only while the melee item is selected, so FPS guns keep their ADS/Recoil untouched and the top-down adapter remains unaffected. `PlayerVitals.DamageTaken(float)` emits on a positive resolved incoming hit after updating vitality/anatomy, and `FirstPersonWorld.InitializeEquipment` binds it to the camera **after** `player.Initialize` (camera initialization occurs earlier). Thus both full and limb-only damage can notify, while heals, blocked hits and zero damage cannot.

**Performance and safety:** A reusable **four-slot fixed array** combines near-simultaneous feedback cues with a hard displacement cap. New impulses wake the **existing** FPS camera `_Process`; each render frame advances only active impulses and composes their current offset with the authoritative interpolated eye + current lean/head bob. When the last cue decays, `Offset` is exactly zero and the existing callback sleeps. The helper has no idle processing, raycasts, timers, resource loads or allocations per cue. Focus loss/Escape clears impulses, teleports reset them, and `_ExitTree` unregisters damage events. The existing tiny translational eye offset also shifts camera-centre shot rays and camera-mounted view models slightly; check short-range muzzle-wall interaction for comfort and visual clipping.

**Local check:** `dotnet build war-machine.csproj`; run F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Equip bat slot 3. Miss a zombie (no shake), then land LMB (faint shake) and MMB (slightly stronger shake); hit multiple enemies in one LMB (only one cue). Allow a zombie to actually damage the player and confirm subtle stronger feedback; blocked/zero damage should not trigger. Toggle `CameraFeedbackEnabled=false` and restart, or temporarily raise `CameraFeedbackGlobalStrength` to compare. Confirm normal camera look/recoil, ADS, head bob, lean, capture loss, focus, menus, scene exit, and no idle camera rendering after settling. **Source checks only; Godot compilation/performance/visual acceptance not performed.**

## CF0b — visibility and testability pass (2026-10-31)

**Source implemented; local Godot build/visual validation pending.** The initial CF0 effect was translation only, just 1–2.2cm for 0.11–0.18s. At medium/far visual distance this could be very difficult to perceive. CF0b retains the **same gameplay event routing and camera helper**, but adds a short **presentation-only roll** to the existing lean/roll writer in `FirstPersonCamera.AdvanceVisualLean`. The feedback helper returns a temporary `RollDegrees`; the camera composes `-lean * LeanCameraDegrees + RollDegrees` once per active frame, and returns exactly to the preexisting lean roll after the cue. The camera's `_lookPitchDegrees`, mouse look, recoil, player capsule and all damage/ballistic code remain unchanged. Roll around camera Z does not shift its centre-forward direction, though existing tiny position shake still changes the camera ray origin slightly.

The now-more-visible starting cues, all authored in `WORLD/FirstPerson/Configuration/FirstPersonMaster.tres`:

| Event | Translation amplitude | Roll amplitude | Duration |
| --- | ---: | ---: | ---: |
| LMB bat confirmed hit | 0.014m | 0.85° | 0.18s |
| MMB bat confirmed hit | 0.022m | 1.30° | 0.22s |
| Player receives resolved damage | 0.030m | 1.80° | 0.26s |

Global frequency lowered from **16Hz to 11Hz**, maximum combined translation raised to **0.06m**, new maximum combined roll **2.5°**. The fixed four-slot impulse cap, squared decay and event-only lifecycle are unchanged. Zero translation **and** zero roll disables a cue; either can be tuned independently.

**Diagnose without combat:** With FPS mouse captured, press **F9** to trigger the exact same `PlayerDamaged` feedback preset without modifying health. This direct test is gated by `CameraFeedbackDebugTestKeyEnabled` (true while developing; false to remove test input). It prints `[CameraFeedback] F9 direct test requested...` once per key press. If F9 visibly shakes but bat impacts do not, enable `CameraFeedbackDebugLogEvents=true` and watch Godot Output for `[CameraFeedback] MeleeSweepHit: triggered` / `MeleeOverheadHit: triggered` / `PlayerDamaged: triggered`; missing events point to contact/wiring rather than rendering. Input blocked, mouse uncaptured and unfocused windows do not fire test or regular feedback.

**QA:** Build C#, F6 FPS scene; look at building/forest horizon, tap F9: a very brief visible tilt and positional bump should fade exactly to original aim. Try LMB bat vs a miss, MMB bat vs confirmed hit, and take zombie damage; F9 should be comparable to injury response. Walk, lean and aim during feedback, verify no permanent tilt after the effect, head bob or recoil interruptions, death/scene unload/teleport/capture reset; check F6 top-down untouched. No extra node, idle loop, raycast or allocation; camera still sleeps when feedback and existing movement effects settle.

## FPS-HB1 — More noticeable speed-driven bob (2026-11-01)

**Implemented in source; awaiting Godot/.NET build and visual comfort testing.** The old small walk/run targets (1.2/1.8cm vertical) and 0.06s filtering visibly diminished the camera's head bob. `FirstPersonHeadBob.Record` now uses actual post-`MoveAndSlide` horizontal movement **without requiring a pressed movement key**. Actual distance drives cycle frequency; actual speed scales amplitude (0 at rest, increasing through walk to up to 1.2× at faster run speeds) and chooses the existing walk→run interpolation. Deceleration naturally tapers the bob; moving into a wall causes none. Crouch remains reduced; prone, airborne, blocked/dead, uncaptured and unfocused cannot start new bob.

`WORLD/FirstPerson/Configuration/FirstPersonMaster.tres` defaults: walk **0.026m vertical / 0.014m lateral**, run **0.041m vertical / 0.023m lateral**, walk/run stride **2.8m / 3.3m**, new editable `HeadBobFollowSeconds=0.035`, unchanged `HeadBobReturnSeconds=0.18`. These are authored targets; effective amplitude also depends on actual speed and filtered response, not a promise of exact on-screen motion. Existing eye interpolation, camera feedback, recoil, lean, weapon sway/ballistics, top-down mode and sleeping-at-idle behaviour are preserved.

**Check locally:** F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`, walk, sprint, release movement keys while coasting, stand still, push against walls, crouch/prone/jump and toggle ADS while moving. Inspect camera comfort and wall/door clipping. If too much, reduce walk/run amplitudes or increase `HeadBobFollowSeconds` in the master. Source-only static validation; compilation and visual acceptance pending.
