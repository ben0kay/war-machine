# Shared one-shot audio

`AUDIO/` contains reusable sound infrastructure and genuinely cross-feature cues, **not** the per-item asset library for every weapon or actor. The rifle cue, WAV and tracked import metadata are now in `EQUIPMENT/Weapons/Rifles/Rifle_001/Audio/`. `AUDIO/Shared/` retains the common `AudioCue`, `WorldAudio` and `WorldSoundEvents` classes. Keep new weapon/player/creature/prop-specific sounds with their owning features.
