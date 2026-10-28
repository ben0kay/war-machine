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
