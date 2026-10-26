# Compatibility and Performance

## Tested engines

| Godot | Demo | Core/API | 12 presets | Compatibility |
| --- | --- | --- | --- | --- |
| 4.2.2 | Pass | Pass | Pass | Pass |
| 4.3 | Pass | Pass | Pass | Pass |
| 4.4.1 | Pass | Pass | Pass | Pass |
| 4.5.1 | Pass | Pass | Pass | Pass |
| 4.6.3 | Pass | Pass | Pass | Pass |
| 4.7.1 | Pass | Pass | Pass | Pass |

Forward+/Vulkan is additionally tested on Godot 4.2.2 and 4.7.1. Volumetric fog is enabled only on Forward+; traditional fog and particle mist remain the complete Compatibility fallback.

The current validation machine uses Windows 11 and an NVIDIA GPU. Other operating systems and GPU vendors should be tested in the target game before release.

## Particle allocation budgets

Quality changes alter actual `GPUParticles3D.amount` values. The drop-in scene keeps inactive emitter allocations available so profile changes can transition immediately without recreating systems.

| Quality | Rain + snow | Mist + airborne | Ripples + splashes | Total allocated |
| --- | ---: | ---: | ---: | ---: |
| Low | 870 | 102 | 80 | 1,052 |
| Medium | 2,020 | 244 | 195 | 2,459 |
| High | 3,770 | 642 | 380 | 4,792 |

Only weather channels with non-zero intensity emit particles. `amount_ratio` handles smooth intensity changes while quality selection changes the underlying allocations.

All generated weather particles disable shadow casting. Transparent rain, mist, dust, and splash sprites should not enter shadow-map passes; this is especially important in interiors with many local lights.

## Production integration reference

The particle-only path was also validated in a dense Godot 4.7.1 Compatibility-renderer game scene with an elevated fixed camera and existing authored atmosphere. With environment ownership disabled and a gameplay effect target assigned, the toolkit preserved the host lighting and environment. After particle shadow passes were disabled, the active airborne-plus-mist profile added two draw calls in both tested rooms. Low measured between run-to-run noise and 1.2% frame-time overhead; Medium measured 3.9% in the foundry and 9.2% in the denser workshop on the validation machine. All active samples remained above 166 average FPS.

## Recommendations

- Start with Medium for integrated or lower-power GPUs.
- Use Low for split-screen views, distant secondary cameras, or conservative mobile experiments.
- Use High for desktop showcase scenes after profiling the complete game.
- Disable `RainSplash3D` when the scene does not have an appropriate flat ground height.
- Keep volumetric fog density modest; it is usually more expensive than traditional fog or particle mist.
- Profile the target game rather than extrapolating from the demo's FPS.
- For fixed, isometric, or cinematic cameras, assign `effect_target` to the player or another gameplay anchor instead of centering local effects on the camera.
