# DumpsterBin001 master

`DumpsterBinMaster.tres` is the shared `PlaceableSizeSettings` resource with `SizeMultiplier = Vector3(1,1,1)`. It scales the original 3.48m-wide fixed dumpster's visual X width, Y height and Z depth; the existing SOC0 AutoBox collider gets the same scaling once upon runtime scene instantiation. Do not reinterpret multiplier values as absolute metres. No dedicated dumpster C# code or active physics simulation.
