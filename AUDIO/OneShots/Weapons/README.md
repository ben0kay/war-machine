# Weapon one-shots

## RifleShot_Original.wav — live, authored first pass

A **0.82-second, 44.1kHz, mono, PCM16 original synthesized rifle report** was created for this repository, with a sharp initial impulse, a low-frequency body, a small mechanical clack and short outdoor reflection tail. This is procedural audio synthesis, **not a licensed recording, downloaded gunshot, sample from another game or generative model sound**. No third-party sample/assets are used.

The WAV lives beside `RifleShot.tres`, which now has `Stream = ExtResource("2")` referencing this file. `Rifle_001/Configuration/RifleMaster.tres` **already** references `RifleShot.tres` as `ShotCue`, and `FirearmController` sends every accepted shot through `WorldSoundEvents.Emit` to the shared pooled 3D audio. There is no new player node, weapon script, processing loop or duplicated legacy playback.

**Existing tuning:** `RifleMaster.tres` has `ShotVolumeDb=-12`; `RifleShot.tres` adds `VolumeDb=0`, `PitchVariation=0.025`, `Spatial=true`, `MaxDistance=90`. The WAV is intentionally short and unlooped. For loudness changes, start by editing `ShotVolumeDb`; don't normalize the WAV differently every time. Variation uses the existing 2.5% pitch randomization per shot.

**AI hearing remains independent** of the audible stream. HE0 rifle base hearing range is 80m, but zombies do not have HE1 listeners yet: adding this audible gunshot alone does not make them investigate it.

**Quality note:** This is a lightweight *synthesized* prototype rather than a field-recorded rifle. If the user wants more natural detail, replace only `RifleShot_Original.wav` with a properly licensed asset or update the `Stream` reference to a new authored WAV. The system/pool don't need rewriting.

**Local test:** import the WAV in Godot, open `RifleShot.tres` and inspect `Stream`. Run `dotnet build war-machine.csproj` and F6 `WORLD/World.tscn` (FPS inherits the same weapon/audio). Shoot once/hold; expect a short positional rifle crack once per accepted shot, no continuous loop/stacking beyond pool limits. If quiet, adjust `ShotVolumeDb` from -12 toward -8/-6 dB carefully. Test restart/weapon switch/unequip and check no double sound. Actual Godot/ear quality, import and performance are not verified remotely.
