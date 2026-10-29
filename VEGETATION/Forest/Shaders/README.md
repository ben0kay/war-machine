# DF2 canopy shader

`CanopyFoliage.gdshader` is the one shared vertex-colour, opaque foliage shader for all DF1 canopy variants in a forest MultiMesh. Its per-instance custom-data red channel starts at opacity 1 and supports ordered dither fade for the existing top-down CameraOcclusion provider. Bark never fades. FPS never registers this provider. No alpha blending, new lights or per-leaf CPU callbacks; canopy wind and detailed sunlight/shadow treatment are later DF3 work.
