# AUDIO — shared bounded playback

**AU0 implemented, awaiting local validation. AU1 rifle playback hook implemented; sound asset intentionally unassigned.** HE0 shared action emission is implemented in source; AU2 sources and HE1/AU3 AI responses remain future work.

World instances `AUDIO/WorldAudio.tscn` once under SystemsRoot/Audio; FPS inherits it. Settings come from Configuration/AudioMaster.tres, duplicated at startup. Shared/WorldAudio.cs owns the pool; Shared/AudioCue.cs describes an independent stream/category/priority/spatial/range/gain/pitch cue.

## Plug in the rifle sound

1. Import the desired short non-looping WAV/OGG under AUDIO/OneShots/Weapons/.
2. Open **AUDIO/OneShots/Weapons/RifleShot.tres**, assign **Stream** in the Inspector and save.
3. RifleMaster already references that cue through ShotCue. Its ShotVolumeDb remains -12; tune cue VolumeDb and AudioMaster.WeaponsVolumeDb as needed.

There is no generated/bundled sound in this change. Empty Stream silently skips playback without spending a voice. MachineGunMaster has no ShotCue or ShotSound and remains silent; later create its own cue/stream. Both guns use the same optional API without copying playback code. Legacy ShotSound fallback remains only for unmigrated weapons and is never played alongside ShotCue. RobotAudio and existing addon audio are not migrated and are outside this pool's budgets.

## Budgets and API

Hard nested caps apply to participating requests: default 20 total / 14 one-shots / 6 loops / 5 creature / 2 per emitter / 3 per CueId. The pool creates 20 reusable slots with paired 3D/2D players; **only one player per slot is used**, each with MaxPolyphony=1, so 40 cached nodes do not mean 40 voices. MaxVoices is clamped to 64 at startup. Use unique stable CueId strings for cues (the same rifle ID remains shared across duplicated weapon resources). Empty IDs fall back to resource identity.

PlayOneShot(cue, worldPosition, emitterId, gainDb) returns accepted bool. StartLoop(cue, source, emitterId) returns a unique handle; StopLoop(handle), StopEmitter(emitterId), StopAll() release playback. Spatial requests outside range are rejected before slot use. Selection ranks category-authored priority then distance; an equal/lower-ranked victim must satisfy every saturated nested cap before replacement. If no single victim resolves all limits, the request is rejected. No extra nodes are allocated on demand. Caller-owned streams must not loop for one-shots. For loops, native authored stream looping is preferred; Finished restarts a non-looping stream with a possible seam.

One-shots retain their original muzzle world position, including shots blocked by cover; gameplay probe/raycast start is not used as the audio origin. Equip passes an explicit world service through existing slots/adapters. Unequip/focus cancellation stops that weapon emitter's active sounds; completed voices recycle on Finished. Missing/capped/muted audio never gates firing, damage or any future AI hearing stimulus. HE0 gameplay noise dispatch is implemented; no AI listening/reaction is implemented yet.

## Listener, lifetime and performance

One AudioListener3D is attached at player head height and oriented by existing camera ViewChanged signals, in both views. The overhead camera's elevation does not move the ears away from the player. Stance changes adjust height. Rebind/exit disconnect camera/stance events and release listener state.

No manager Process/PhysicsProcess callback for idle or one-shot-only use. Native audio playback is intentional active mixer work. Only ongoing loops wake a single 5Hz source follow/relevance check; lost/far sources stop and the manager sleeps after the last loop. Explicit StopLoop/StopEmitter stops burning/AI/etc. state changes; no generic entity-state dependency is assumed. OFF through RuntimeSettings.Enabled plus ApplySettings stops all; master volume/category gains apply once per request. Call ApplySettings after runtime changes (it stops existing playback). Pool-size increases need scene reload; reductions are enforced on new requests. No new buses or global bus mutations, occlusion rays, resource searches or per-creature timers. Existing external playback is unchanged.

## Local validation

Source/loop/resource wiring and a numeric nested-budget model were checked. **Compilation, Godot runtime, listening and visual/performance acceptance are unverified: no Godot/.NET executable here.** Run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. At night, fire/switch both guns while moving; verify brief warm lighting, no shadows/stuck light, focus loss and reload. Assign a short **non-looping** WAV/OGG to RifleShot.tres Stream, fire/hold and move, verify exactly one sound per accepted shot and no MG sound. Check the Remote tree for one Audio service/player listener; inspect stopping/switching/unload and OFF. Stress caps with temporary cue requests; do not assume measured performance from source checks. No terrain rebake needed.

## HE0 — shared action sounds (2026-10-28)

Use `WorldAudio.SoundEvents.Emit` to feed the existing pooled playback and an independent gameplay dispatcher from one action. [Shared API and loudness rules](Shared/README.md). Gunshots from both current weapon masters opt in through ShotEmission: rifle base hearing radius 80m, machine gun 70m, Loudness 1. Existing rifle ShotCue and empty Stream remain unchanged; MG remains audibly silent. Assign playback streams later without changing hearing.

Playback caps are unchanged (including 20 total / 14 one-shots / 6 loops). One accepted shot makes one gameplay event and at most one playback request, even with cover blocking the muzzle. Audio Enabled/mute/capping never disables the hearing dispatcher. Loudness 0 suppresses both outputs; radius 0 suppresses only hearing. Invalid positions/loudness are rejected. Personal volume controls affect playback only. Unmigrated legacy ShotSound remains outside the pool as before; no duplicate pooled/legacy sound plays.

No zombie, deer or robot response changes, footsteps or extra audio assets are included. The coordinator/dispatcher have no per-frame work and shut down with their world. Source/resource checks were performed; build, in-game event counting and listening remain pending locally.
