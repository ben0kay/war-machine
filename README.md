# WAR MACHINE

> **Working title — original 3D sci-fi wilderness exploration game.**  
> **Current status:** Early Godot project foundation. This README is the **first stop** for anyone (including AI assistants) working on the game. It describes the intended game and overall ownership of systems; it is **not** an implementation plan.

## Game vision

**War Machine** begins as a **3D wilderness to explore**: vast-feeling forests, steep mountains and rocky valleys, caves, abandoned places, shifting weather, damp foliage and distant atmospheric scenery. It should feel isolated, immersive and believable without requiring photorealistic models or AAA production values.

The atmosphere takes loose inspiration from wilderness sci-fi thrillers featuring soldiers and alien machines, but the world, fiction and designs should be original. The long-term possibility of dangerous mechanical enemies, survival, stealth, combat or even zombies remains open. **None of those systems are needed for the initial environment-focused project.**

The first question is not “is there enough gameplay?” It is: **is walking through this world interesting, convincing and enjoyable?**

### Design pillars

- **Explore first, survival later.** Movement, terrain, long views, environmental variation, weather, sound and landmarks come before enemy encounters or crafting.
- **The landscape is the protagonist.** A coherent forested valley with convincing wind and mist matters more than individually spectacular assets.
- **Large-feeling but manageable.** Build a persistent, finite, expandable world with a mixture of procedural generation and authored detail.
- **Natural terrain, not a universal voxel sandbox.** Smooth mountains and sculptable ground; caves can be separate geometry. The player does not need to dig or rebuild the entire world. Limited explosion damage may be explored much later.
- **Visual consistency over complexity.** Reusable rocks, trees, foliage and materials can convey scale and realism when composed well.
- **Efficient by design.** Streaming, distant scenery, vegetation LOD and low idle cost are foundational features.
- **Solo-developer and AI-friendly.** Prefer maintainable C# code, modular systems, clear ownership and a playable integrated main world.

## Starting technical foundation

The repository was initialized as a **Godot 4 C#/.NET** project. Its checked-in `project.godot` indicates Godot **4.7** project features, **Forward+** rendering, **Direct3D 12** for Windows and **Jolt Physics**. Actual engine/addon installations must be verified in the project; the config is not proof of compatibility.

**Terrain3D** is checked into `addons/terrain_3d` (v1.0.2) with its demo assets. **Sky3D 2.1** is installed and integrated as the sky/day-night owner. The playable entry is `WORLD/testworld.tscn`: saved valley terrain, slope-blended cliff/grass, shared GPU wind grass and reusable free-flight exploration. C# build and headless integration checks passed under Godot 4.5.1 .NET; visual/performance acceptance and the configured local 4.7/D3D12 runtime remain pending. See `WORLD/WE0_WE1_VALIDATION.md`. Use Godot built-ins where possible, including lighting, fog, instancing and LOD. Prefer lightweight custom C# glue over replacing entire engine subsystems.

## World philosophy

Build **one real playable world scene**, not disconnected feature demos whose systems later need reassembly. The initial environment scene is deliberately named **`WORLD/testworld.tscn`**; despite the name, it is the reusable playable foundation, with any later promotion into a main world handled deliberately. Every accepted system should be integrated and validated there.

The world should combine:

- **Geography:** ranges, ridgelines, valleys, clearings, cliffs, water corridors and traversable routes.
- **Terrain materials:** soil, rock, grass, mud and other appropriate surfaces, blended naturally.
- **Vegetation:** forests, trees, shrubs, grass, ferns, fallen logs, small rocks and regional variation.
- **Atmosphere:** lighting, sky, moving clouds, wind, rain, fog, mist, storms and suitable ambient sound.
- **Places:** cave entrances and interiors, notable formations, isolated structures and recognizable landmarks.
- **Persistence:** stable seeds and stored authored changes, allowing procedural tools to help create a world without endlessly reshuffling it.

The player should see convincing distant mountain and forest silhouettes while close foliage has richer detail and interaction. Visual LOD, gameplay activation and asset streaming are related but distinct responsibilities.

## High-level system design

The following are **planned ownership boundaries**, not a list of existing directories or implemented classes.

| Area | Responsibility |
| --- | --- |
| `CORE/` | Application bootstrap, shared reusable utilities, common coordination |
| `WORLD/` | Main world scene, terrain, generation, geographic regions, streaming and landmarks |
| `VEGETATION/` | Procedural plant placement, foliage rendering/LOD and wind response |
| `ENVIRONMENT/` | Sky, time, light, fog, weather, environmental ambience and shared wind conditions |
| `PLAYER/` | Exploration controller, camera, traversal and immediate player interaction |
| `ASSETS/` | Organized source/imported terrain, plants, rocks, cave modules and audio |
| `SAVE/` | Persistent seeds, authored overrides and later world-state changes |
| `DEBUG/` | Diagnostic overlays, profiling helpers, quality and distance controls |
| `ENTITIES/` | Reserved for future machines, NPCs and world interactions; **not initial scope** |
| `UI/`, `FX/` | Shared interface and effects as features require them |

Create folders, subfolders and scripts **when implementing their owning features**, not all at once. Major uppercase folders should have their own README and a central, easy-to-tune configuration resource or master config where useful. Avoid redundant managers, massive scene trees, duplicated settings and tight cross-system coupling.

### System integration principles

- **World** coordinates terrain and region/asset loading; terrain data is not the owner of weather or gameplay.
- **Environment** holds shared conditions such as sunlight, wind and precipitation; vegetation and effects consume these conditions instead of each maintaining an independent update loop.
- **Vegetation** owns efficient instances and distance transitions; procedural placement should respect geographic region rules.
- **Save** handles serialization of persistent data; do not conflate generated defaults, editor overrides and runtime changes.
- **Debug** observes the integrated world without becoming a required part of gameplay.
- Inactive systems should be genuinely dormant: no unnecessary polling, emitter work, repeated shader writes or full-world searches.

## Development rules and where to look next

**[CodePreferences.md](CodePreferences.md) is authoritative for code style, documentation, performance and AI collaboration.** It calls for C#, simple/modular structure, comments above methods with visible separators, brief per-script headers, folder READMEs, clearly grouped settings, performance-first activation/sleep design, and **one commit per authorized job**.

**Implementation plans do not belong in this root README.** The active environment-first plan, its phase status, dependencies and test criteria live here:

**[WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md](WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md)**

AI workflow: read this README → read CodePreferences → read the owning folder's README and plan → inspect actual scripts/scenes/assets → work only on the explicitly requested phase → validate and update owning docs. Never assume planned features already exist.

**North star:** *Make the wilderness itself worth exploring before adding survival systems or robotic enemies.*
