# CAMERA

`FirstPersonCamera.cs` replaces overhead follow with eye-height perspective and mouse look. Settings are in ../CONFIGURATION/FirstPersonMaster.tres. No per-frame follow loop.

Event-driven render translation between physics snapshots replaces tick-only positioning. Mouse rotation uses ScreenRelative immediately and preserves InvertMouseY=true. Rendering sleeps when settled; no camera head bob. See parent README for tuning and pending local checks.

## FPS weapon-camera recoil (2026-10-28)

`FirstPersonCamera.SetWeaponRecoilPitch(float degrees)` applies **temporary additive pitch** on top of the user's persistent `_lookPitchDegrees` mouse aim. The existing mouse inversion, sensitivity, yaw and `MaximumPitch` bound remain in force. The FPS equipment adapter drives this only while RC0 gun recoil is active; it smooths the view rise and resets the temporary pitch on interrupt. This avoids a second ballistic recoil offset and prevents losing mouse correction when recoil settles. The camera does **not** add another permanent `_Process` loop. Local FPS and boundary/capture tests pending.

## FPS recoil pitch is now permanent until mouse correction (2026-10-28)

The current `FirstPersonCamera.ApplyWeaponRecoilKick` adds **positive pitch to the same `_lookPitchDegrees` controlled by mouse look**, bounded by existing `MaximumPitch`. It does not maintain a temporary camera offset or automatically subtract the recoil on release. This supersedes the older `SetWeaponRecoilPitch` additive recovery described above. The FPS adapter alone schedules those impulses; the camera has no new process loop. Other camera movement/input and the primary top-down view are unchanged. Use mouse movement to correct aim; local test needed.

## FPS-HB0 walking head bob — implemented in source (2026-10-28)

`FirstPersonHeadBob.cs` is a plain, FPS-only helper with **no extra Node or timer**. The player supplies actual grounded post-slide horizontal travel to `FirstPersonCamera.RecordPhysicsPose()`. The camera's existing interpolation callback adds tiny camera-right and world-up bob offsets, with walk/run/crouch amplitudes and stride lengths from `../CONFIGURATION/FirstPersonMaster.tres`. The head returns smoothly to zero when movement stops; idle processing sleeps. Prone/airborne/stationary/dead/blocked/uncaptured cannot start new bob. Mouse-look/recoil orientation, player capsule, camera interpolation, aiming and top-down view remain unchanged.

Defaults: walk 2.5cm up/down and 1.2cm side-to-side; sprint 4cm and 2cm; crouch half strength, prone disabled; return 0.18s. [Plan and validation handoff](HEAD_BOB_IMPLEMENTATION_PLAN.md). **Source implemented, local compile and FPS visual/comfort testing pending.**
