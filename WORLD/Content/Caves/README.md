# Organic terrain-embedded cave

**Location remains `WORLD/Content/Caves/`.** Main-world entrance defaults to 48m east / 28m north of spawn; look for CAVE. `C/CaveSettings.cs` and `Configuration/CaveMaster.tres` own authoring values. No separate WORLD/Caves folder was introduced.

## Shape and entrance

`C/WildernessCave.cs` builds one collidable winding passage after saved Terrain3D loads. `C/CaveShapeNoise.cs` wraps two seeded FastNoiseLite layers: broad low-frequency shape and independent higher-frequency rock relief. Noise changes actual vertices and ridge heights, not only the stone shader.

Default seed 7131; shape frequency 0.055, rock frequency 0.65, width variation 0.24, height variation 0.28, wall relief 0.35m, floor relief 0.1m. Width and height vary smoothly into small bulges/chambers; broad noise also offsets the winding centreline. Floor relief is bounded at 0.2m and walls retain minimum width/height guards. Settings are spawn-time; change master values and restart, not a live cave rebuild.

The previous seven-point arch is replaced by a 24-segment rounded arch with lower wall/floor points, along 72 longitudinal sections. Shared cached rings join every tunnel section. Roughness is suppressed at the flat floor; the floor still rises with the existing route. Static tunnel collision uses the same mesh. Cached local width/height values also inform shelter checks for weather.

The long extruded rock collar and six stacked entrance rocks are removed. A curved eight-band noisy rock apron joins the **exact tunnel mouth vertices** to an irregular semicircular boundary sampled from the surrounding heightfield. Intermediate relief fades to zero at both boundaries. The outer edge and a short downward skirt overlap the terrain to hide interpolation seams. The entrance hole is oriented to the actual mouth direction rather than a world-axis rectangle; the apron spans that opening. This is intended to read as an embedded rocky cliff, without an abruptly ending block.

## Terrain ownership and performance

As before, a bounded one-time authoring pass raises the loaded runtime heightfield over the passage, with a broad feathered ridge and small control-map entrance hole. The ridge now uses broad noise and conservative width/roof bounds to enclose the variable passage. Terrain collision updates once. **No saved .res region is overwritten**, no global rebake, voxel system or digging is added. Initialize on fresh terrain once per launch; repeatedly applying to already changed maps is unsupported.

About 4,500 generated tunnel/entrance triangles at default settings, plus existing crystals/lights; no per-triangle gameplay processing. Mesh/concave collision is built once, native noise resources are disposed afterwards, and both cave callbacks remain disabled. Weather shelter remains event-driven. No persistent noise queries, procedural animation or new terrain owner.

## Verification and limits

Performed source review, whitespace/resource-reference checks and bounded geometry/profile calculations. No Godot or dotnet is installed here: compilation, rendered entrance fit, collision and weather shelter are **Awaiting Validation**, not confirmed. Brief local check: build, run main scene, walk around the entrance and through the passage in F6, check daylight gaps/terrain intersections, floor traversal, crystals and rain suppression. Confirm the new cliff silhouette from outside. Return feedback before adding further detail.

This is one tunnel with a sealed end and modest chamber variation, not a branching cave network. Existing stone material/crystal lighting remains. No ambient-light entrance trigger is implemented. Noise cannot guarantee every extreme hand-edited configuration stays traversable; keep dimensions/relief within the documented ranges.
