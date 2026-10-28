# Lighting — reusable local illumination

**LI0 implemented, awaiting local validation. LI1 campfire remains proposed only.**

FirearmShotEffects creates one optional cached MuzzleFlashLight per weapon at initialization. FirearmSettings.EmitMuzzleLight defaults false; **RifleMaster and MachineGunMaster explicitly enable it** for this authorized shared-light pass. Audio selection is independent: only the rifle has a cue slot.

ENVIRONMENT/Lighting/Configuration/LightingMaster.tres is the authoring source: Enabled, MuzzleLightEnabled, colour (warm 1/0.78/0.52), energy 2, range 4m, duration 0.07s and MaxTransientLights=4. A copy is read when a gun's effect initializes. Settings changes in the master apply next run/effect creation.

Effects/MuzzleFlash/MuzzleFlashLight.cs uses a shared active-lease counter capped by the common master. Refreshing an already-lit gun does not claim another lease; expiry, StopLight and node exit release once. This is a bounded cached-light implementation, not a new always-running lighting manager or campfire system. The common master must remain consistent between emitters; independent conflicting per-emitter budget overrides are not supported.

Accepted shots trigger at the actual muzzle, including cover-blocked shots. The existing active FirearmShotEffects render callback advances the independent light lifetime and follows the same interpolated muzzle as the visible flash. No added light Process callback/timer, rays, shadows or per-shot allocation. OFF/zero light energy stops the light; between flashes LightEnergy=0 and Visible=false. Unequip/focus cancellation uses existing CancelFeedback; scene exit releases the lease. No WorldEnvironment, sun or global rendering edits.

Real light duration comes solely from LightingMaster.MuzzleLightDurationSeconds; the visible flash still uses FirearmSettings.FlashDuration and the tracer uses EffectDuration. Machine-gun mesh flash remains 0.05s and real light starts at 0.07s. These are deliberately independent visual timings. Future suppressors must disable both visual flash and EmitMuzzleLight. Existing shader-only grass may not respond to point lights; inspect Forward+ lit surfaces/night/haze locally. Shadowless light can bleed through nearby walls; no cover-ray/shadow solution is added.

Campfire flame/fuel/flicker, persistent-light relevance/shadows, robot lights and lightning are future separately authorized work in IMPLEMENTATION_PLAN.md.

## Local validation

Source/loop/resource wiring and a numeric nested-budget model were checked. **Compilation, Godot runtime, listening and visual/performance acceptance are unverified: no Godot/.NET executable here.** Run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`. At night, fire/switch both guns while moving; verify brief warm lighting, no shadows/stuck light, focus loss and reload. Assign a short **non-looping** WAV/OGG to RifleShot.tres Stream, fire/hold and move, verify exactly one sound per accepted shot and no MG sound. Check the Remote tree for one Audio service/player listener; inspect stopping/switching/unload and OFF. Stress caps with temporary cue requests; do not assume measured performance from source checks. No terrain rebake needed.

## Static fixture lighting — StreetLamp refactor

Streetlamp illumination now belongs entirely under `Fixtures/StreetLamp/`, separate from the physical `WORLD/Content/Assets/Objects/Static/Lighting/StreetLamp001/` prefab. `StreetLampLighting.tscn` owns one downward SpotLight3D and shadowless OmniLight3D with `StreetLampLighting.cs` and a typed `Configuration/StreetLampLightingMaster.tres`. It accepts an optional per-instance LED material from its parent for glow and exposes `SetEnabled(bool)`; no per-frame code runs. The model/physics/metal colours remain world-content owned, and the public physical lamp `SetLampEnabled(bool)` still works via delegation. All original light energy, radius, colour, fog and shadow defaults are retained.

This specialized fixture preset does not replace or duplicate the global `Configuration/LightingMaster.tres` used by muzzle flashes, and does not affect Sky3D or existing weapon lighting. Future PL2 time-of-day/flicker integrations should use the fixture enable methods rather than adding a parallel world-light controller. [Fixture details](Fixtures/StreetLamp/README.md).
