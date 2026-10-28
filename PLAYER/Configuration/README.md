# Configuration / ownership

PlayerMaster.tres uses PLAYER/PlayerSettings.cs for active movement/crouch/gait/weapon pose. The FPS PlayerConfig master is separate under PLAYER/LegacyFirstPerson/Configuration.

## PH0 body-condition authoring plan (not implemented)

When PH0 is authorized, the existing `PLAYER/Configuration/PlayerMaster.tres` will export a grouped typed `PlayerVitalsSettings` profile with initial hidden Vitality=100, six body-region condition defaults=100 and shared generic armour/resistance values. Deep-duplicate runtime profiles and never write current health or wounds back to authoring resources. Body regions and hit-routing policy live under `PLAYER/Stats/`; FEEDBACK owns display thresholds. Actual injury modifiers and precision hitboxes are later phases. See `../Stats/BODY_CONDITION_DESIGN.md`.

## Grounded jump tuning (2026-10-28)

`PlayerMaster.tres` now has `JumpVelocity = 6.0` (m/s upward impulse) in the existing `PlayerSettings` movement group. Shared player gravity remains `Gravity=20`m/s² by default; theoretical peak height on level ground is roughly 0.9m. This tuning applies to the normal overhead player and the derived FPS debug actor. Right mouse button triggers a single floor-only jump, not auto-repeat; crouched actors and blocked/GUI input do not jump. The player capsule, horizontal movement and existing ceiling/ground collisions remain native Godot physics.
