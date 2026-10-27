# AUDIO — future Codex implementation plan

**Current status:** AU0 pool/listener and AU1 rifle playback binding are implemented, awaiting local validation. The rifle cue intentionally has no Stream: owner requested a plug-in foundation, not asset generation. AU2/AU3 remain unimplemented. See README for current API/budgets/limitations; phase descriptions below are original design context. Read root `README.md`, `CodePreferences.md`, `AUDIO/README.md` and the related gameplay sources at current HEAD. Use branch `implement-td0-td1` (**not main**). Do **only authorized phases**, one focused commit and brief validation. No runtime audio code or sound files should be assumed.

## AU0 — central playback pool and listener

Implement a single instance under the world's existing systems root. Use reusable `AudioStreamPlayer3D` for world sources and `AudioStreamPlayer` for UI, with a cue Resource describing stream, category, priority, spatial flags, distance, volume and optional pitch variation. Design a small API such as `PlayOneShot(cue, position, sourceId?)`, `StartLoop(cue, source)`, `StopLoop(handle)` and `StopAll()`. Avoid references from Shared code to concrete zombies, guns or environment systems.

Implement hard global + nested budgets from AudioMaster: 20 total, 14 one-shots, 6 loops, 5 creature, 2/emitter and 3/same cue. All simultaneous constraints apply. Filter far-away/unimportant events first; reject or steal low-priority voices when full, never allocate extra. Use `Finished` to recycle one-shots. Follow loop emitters only while relevant, using bounded shared update work and reliable stop/free on scene unload or lost owner. Reused nodes should be reset completely between owners. Manager and processing dormant when OFF or empty.

Plan 3D positioning around a player-centred listener for elevated top-down camera, without breaking the FPS debug scene. Future mixer buses/category gains should be validated and not mutate unrelated audio settings globally. No environmental occlusion raycasts by default; keep headroom for 50 zombies, vegetation and fog.

**Acceptance:** simultaneous requests never exceed cap, a loop stops on demand and actor death, one-shots keep their original positions, important nearby cues survive crowded scenes, OFF has no voices/process activity, one correct listener in both views.

## AU1 — rifle-only shot sound

Add or generate an authorized rifle-shot audio asset in `OneShots/Weapons/` and bind it to the reusable cue config. On accepted `FirearmController.TryFire()`, request spatial one-shot at muzzle world position. Only Rifle_001 should use it initially. Existing `FirearmSettings.ShotSound` and old `AudioStreamPlayer3D` may need a clean migration to avoid **two** rifle sounds, while preserving shared gun support, other weapons, hit logic, projectiles and cooldown. Shot audio must not fire from rejected trigger presses. Test rapidly held shots and moving player.

## AU2 — opt-in gameplay sound sources

Gradually add footsteps (actual stride events, surface/stance variations), zombie vocalizations, deer calls, robot steps/servo (preserve `RobotAudio.cs` cues), machinery loops, wind/rain/storm ambience and interaction/UI sounds. Avoid per-zombie audio nodes and 50 concurrent groans. Start/stop ambient sounds from the state owner; no permanent creature audio processing.

## AU3 — AI hearing (separate from playback)

Follow `ENTITIES/Shared/Sensing/HEARING_INVESTIGATION_PLAN.md` and PER5. Shot/action events should independently dispatch both playback requests and immutable-position AI sound stimuli. Muting the master or refusing a voice must **not** prevent zombies investigating a gunshot. AudioManager doesn't own perception, pursuit or changing wander anchors. Spatial occlusion/muffling is a later opt-in bounded feature.

**For every phase:** update README, keep settings exposed, check lifecycle and zero-idle costs, run a basic Godot/.NET build if available, and leave full listening/performance testing to owner.
