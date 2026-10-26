# Organic terrain-embedded cave

**Next cave expansion plan:** [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) — CV0–CV5 for underground ravines, asymmetric chambers, entrance hillside geology and performance. All phases are planned; this is not implementation authorization.


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

## Entrance correction and Y extension — current revision

Owner screenshots showed folded/sawtooth entrance faces while the interior was acceptable. The ground-projected outer loop was not a valid ordered arch. It is now an ordered larger rim in the mouth plane, with each upper vertex seeking a buried landing behind the cliff. Exterior triangles share a consistent forward orientation; the floor faces upward. Small apron relief is reduced to 0.1m. The same cached inner rim still joins the tunnel; no independent random seam is introduced. Actual rendered fit remains to be checked locally.

Default main passage is now **120m**, followed by a **14m junction chamber** and **two 75m branches**, each gently descending **4m**. `C/WildernessCave.Fork.cs` owns passage caches, branch paths, the shared-port chamber surfaces and terminal caps. The main end is open into the junction; branch starts have no caps. A chamber floor, roof and three connecting wall strips join the three exact port loops rather than overlapping closed tubes. Each branch retains seeded width/wall/ceiling variation and a sealed far end. Sparse unshadowed branch crystal lights extend the existing guidance style. Main lights retain their existing shadow setting.

Floor has six subdivisions across each section and low-amplitude 0.1m noise, vs 0.35m wall relief. Junction floor is subdivided too, with noise pinned at the shared port boundaries. Route floor remains traversable by design; collision uses those same generated triangles. Weather shelter includes both branches and the junction polygon. All paths contribute to the runtime covering ridge; terrain maps/collision are refreshed once after the combined authoring pass. Saved regions remain untouched. Location stays `WORLD/Content/Caves/`.

Validation: brief source/resource checks and a representative numerical mesh-topology construction confirmed both branch port joins/chamber are closed except the intended main entrance, without non-manifold edges. This is **not a Godot runtime/render test**. Godot/.NET unavailable: locally build and walk through the entrance, floor, junction and both branches; inspect the cliff from outside and rain shelter inside. Previous single-tunnel/triangle-count notes above describe the earlier revision; this extended network supersedes them. No new per-frame cave processing.

## Underground corridor generation — current revision

The generator no longer raises the floor to match the surface or drives it with a fixed sinusoidal path. FastNoiseLite steers a bounded heading field; world-space noise also adds small vertical variation. The 120m main route descends **30m relative to the entrance**, then the two 75m branches descend **12m more**. Maximum grade defaults to 0.35 (35%, not degrees); per-step clamps prevent abrupt drops and uphill reversals. Depth is bounded against route length/grade. Branch steering stays around two separate outward headings so branches do not fold back across the junction. Existing wall/ceiling/floor noise remains.

New CaveMaster parameters: DescentDepth 30, MaximumGrade 0.35, CorridorWanderDegrees 35, CorridorVerticalNoise 0.5, MinimumRoofCover 3, BranchDescent 12. Legacy sine Bend/HeightVariation settings are removed; heading/vertical noise now owns those responsibilities. No arbitrary world Y=0 is treated as physical earth. Actual terrain height plus the generated roof/cover decides whether a route is buried.

Naturally covered branches skip terrain embedding altogether. For the main approach, the existing bounded entrance-support pass only edits cells that still need roof cover (plus the doorway hole); deeper cells keep the original terrain. Underground geometry is just passage floor/walls/ceiling and collision, with the existing shared-port fork and terminal caps. No surrounding rock volumes, separate underground terrain, global rebake or saved-region write. Surface support still handles shallow approaches/low terrain where required.

Checks: source/resource review, bounded representative descent calculations and whitespace. Local build/render/walking are pending because Godot/.NET are unavailable. Check descending grade from the mouth to junction/both branches, roof intersections/daylight, entrance join, collisions and shelter. No new runtime cave callbacks; all route/cover sampling happens once at startup.

## Exterior hillside material

The entrance apron now uses its own `Configuration/CaveExterior.tres` / `Shaders/CaveHillside.gdshader`, sharing the same packed rock023 albedo/normal and ground037 albedo as `WORLD/Terrain/TerrainAssets.tres`. Only BuildEntrance receives this material: the approved interior retains its previous stone material/scale.

Default exterior texture scale is 0.25 instead of the interior stone's 0.8, giving roughly 3.2× larger visible rock features. Sharper world-space triplanar weights preserve block detail instead of blending projections broadly. Normal strength 1.0 and terrain-like roughness/cool macro tint replace the smoother moss/wet stone finish; upward ledges blend the terrain ground texture. This approximates the hillside appearance on an arbitrary mesh, not the entire Terrain3D material pipeline.

`CaveMaster` owns ExteriorTextureScale and ExteriorNormalStrength (spawn-time values). No cave shape, descent, noise, floor, junction, collision or terrain texture settings change in this revision. Resource/source checks only; visual colour/scale match remains pending local acceptance because Godot/.NET are unavailable here.

 
## Winding layout and organic chamber extensions — latest revision

The main corridor is now **190m / 144 sections**, with two **125m branches**. `C/CaveRouteBuilder.cs` generates broad seeded lateral offsets at spaced control points, interpolated with Catmull-Rom curves. The mouth and branch entries remain initially straight for stable joins. Default `MacroBendSpacing=32m`, `MacroBendAmplitude=16m`, `CorridorWanderDegrees=55` (bounds lateral turn slope); branches use 70% bend amplitude. All paths progress forward and descend within the original grade limit. Existing small rock noise remains independent.

Cached passage rings now smoothly widen into **two main chambers and one per branch**, using `ChamberHalfLength=19m`, `ChamberWidthMultiplier=2.2` and `ChamberHeightMultiplier=2`. No chamber expansion reaches the three junction ports or the entrance; floor noise remains low, and collision uses the exact generated geometry. This first pass creates **big irregular tunnel bulges**, not independent spherical rooms, vertical ravines or additional forks.

Terrain cover now follows *both horizontal axes* of the curved passage and each section's interpolated width/ceiling, rather than a straight Z envelope and one constant passage size. Only deficient runtime heightmap cells are raised; entrance hole and saved-region protections remain. No per-frame cave noise/update loops. Very extreme bends or chamber values can still produce overlaps, so keep the recommended values until visually validated.

**Deferred local checks:** Compile with Godot/.NET, F6-walk from mouth through both branches; check genuinely obscured curved sightlines, chamber space, ceiling cover/daylight gaps, chamber collisions, fork seams, weather shelter, and entrance appearance. This revision was source-reviewed but not visually validated on Windows. Older 120m/75m measurements in the history above are superseded.
