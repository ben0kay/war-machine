# DF2 canopy shader

`CanopyFoliage.gdshader` is the one shared vertex-colour, opaque foliage shader for all DF1 canopy variants in a forest MultiMesh. Its per-instance custom-data red channel starts at opacity 1 and supports ordered dither fade for the existing top-down CameraOcclusion provider. Bark never fades. FPS never registers this provider. No alpha blending, new lights or per-leaf CPU callbacks; canopy wind and detailed sunlight/shadow treatment are later DF3 work.

## DF2 palette fallback correction (2026-10-30)

A local FPS screenshot showed grey/white DF2 broadleaf trunks and crowns. `ForestPlacement.Batch` now explicitly enables `MultiMesh.UseColors` and supplies white per-instance tint, preserving DF1's authored vertex-face colours. The shared foliage shader has an `leaf_fallback_color` parameter sourced from `CanopyTreeMaster.tres → LeafColor` and applies that green only when the received vertex colour is default white. New `CanopyBark.gdshader` follows the same pattern with the master bark colour, retaining scars/face variation when available. No changes to camera fading, patch placement, LOD, trunk collision, tree shape or global tint; no per-frame CPU processing. To verify, pull/build, F6 FPS and look upward inside DF2. Inspect Shader Editor/Output for shader errors; this source-only patch has not been run in Godot.
