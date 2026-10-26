# Individual leaf wind

`LeafWind.gdshader` runs only on generated oak leaf surfaces, not on imported artwork or trunks. The C# generator writes pointed leaf polygons, their colours, and a 0-to-1 attachment weight in UV.y (base fixed, tip movable). The shader uses TIME and world position to introduce independently phased flutter and a shared directional breeze. No per-leaf CPU work or animations are used.

Tune wind in `../Configuration/TreeMaster.tres`; direction/shared global strength are taken from `ENVIRONMENT/Configuration/EnvironmentMaster.tres` when the tree settings are applied. Shader uniforms are not polled per frame. Imported models remain unmodified. For a fast pause test, set LeafWindStrength to zero and relaunch.
