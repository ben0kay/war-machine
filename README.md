# WAR MACHINE

> **Current direction:** Fully 3D, angled **orthographic/isometric-style** survival game with stylized low-poly art.  
> **Active development:** Player movement/camera feel and the existing giant robot's movement, patrol, and scanning.  
> **Working branch:** `implement-td0-td1`. First-person wilderness systems are **parked, not deleted**. This README is the first stop for project vision and development priorities, not a phase implementation plan.

## Current game direction — top-down 3D first

**War Machine is now being developed around an angled, orthographic camera overlooking a fully 3D world.** Think isometric-style movement and interactions, not 2D sprites or a fixed-perspective 2D engine. The active scene is [World](WORLD/README.md) on the `implement-td0-td1` branch.

### The immediate priority: make movement and the robot feel right

1. **Player feel:** camera-relative walking/running, acceleration and braking, turning, proper ground contact, clearly readable simple low-poly human animation.
2. **Camera feel:** smooth follow, comfortable angle, orthographic zoom and stable composition so the character and enormous robot remain easy to read.
3. **Robot_001 feel:** continue refining the existing heavy cyan **biped**—proper forward movement, foot placement, gait, stop/settle timing, head sweeping, cyan scanning, presence and scale from the overhead camera.
4. **Keep the experiment simple:** use the current flat-ish Terrain3D preview with a small hill. Live day/night and weather tuning reuse the existing sky and precipitation systems. No mandate to bring over trees, rocks, caves, combat or other systems yet.

**What success looks like right now:** walking the character around the test terrain and watching the real robot patrol, stop and scan feels enjoyable and convincing **before** expanding gameplay.

### Longer-term vision — not an implementation order

The intended destination is an **atmospheric sci-fi wilderness game balancing survival, combat and base-building**, with exploration, scavenging and a persistent home/base supporting each other. The player is vulnerable; **giant robots are the superior, intimidating threats** while **zombies may later provide smaller, recurring nuisance threats** and complicate escapes or exploration. Robots could eventually fight or obliterate zombies, but that is *future design context*, **not a current feature request or zombie implementation plan**. The owner has now authorized a small basic zombie population in the top-down preview: slow wandering and arms-out range pursuit only; combat and robot interaction remain future work.

Art direction should be **stylized, low-poly and readable**, with painterly landscapes, restrained palettes and atmospheric lighting reminiscent in *level of graphical ambition* of *The Long Dark*. No requirement for photorealism or extremely detailed models. Everything should remain original, and the orthographic camera should show convincing silhouettes and animation at gameplay zoom.

### First-person work: parked, retained

`WORLD/testworld.tscn` remains the original 3D first-person/free-flight wilderness scene, with Terrain3D, vegetation, caves, weather, sky and the existing robot encounter. **Do not delete, overwrite or continue expanding that work by default.** It is preserved as a reference/asset base for possible reuse; its first-person environmental roadmaps are **paused**. The project's F5 main-scene setting still points there until the user explicitly decides to change it; open the orthographic scene and use **Run Current Scene (F6)** to play the active prototype.

## Starting technical foundation

The repository was initialized as a **Godot 4 C#/.NET** project. Its checked-in `project.godot` indicates Godot **4.7** project features, **Forward+** rendering, **Direct3D 12** for Windows and **Jolt Physics**. Actual engine/addon installations must be verified in the project; the config is not proof of compatibility.

**Terrain3D** is checked into `addons/terrain_3d` (v1.0.2) with its demo assets. **Sky3D 2.1** is installed and integrated as the sky/day-night owner. The parked original-world scene is `WORLD/testworld.tscn`: saved valley terrain, slope-blended cliff/grass, shared GPU wind grass and reusable free-flight exploration. **Active experimentation** is in `WORLD/World.tscn` (separate simple Terrain3D, low-poly player, orthographic camera and an instance of the same Robot_001). C# build and headless integration checks passed under Godot 4.5.1 .NET; visual/performance acceptance and the configured local 4.7/D3D12 runtime remain pending. See `WORLD/WE0_WE1_VALIDATION.md`. Use Godot built-ins where possible, including lighting, fog, instancing and LOD. Prefer lightweight custom C# glue over replacing entire engine subsystems.

## World and gameplay philosophy

The **active prototype is a small, deliberately separate scene**, to evaluate the player/robot/camera without destabilizing the older wilderness showcase. Build and tune work that is explicitly requested **inside `WORLD/`**, reusing shared robot code where appropriate. Do not automatically merge it into the legacy TestWorld or promote it to the project startup scene.

Terrain3D remains useful for the 3D orthographic world. Keep its preview terrain data separate from the original world; use low-poly/stylized meshes, clear lighting and restrained visual complexity while gameplay feel is being tested. The longer-term landscape may grow to include natural terrain, forests, landmarks, caves and buildable locations, but **these are not current prototype tasks**.

Avoid unrelated frameworks and polish. Prioritize responsive controls, readable robot behaviour and performance-first activation over feature count.

## High-level system design

The following are **planned ownership boundaries**, not a list of existing directories or implemented classes.

| Area | Responsibility |
| --- | --- |
| `CORE/` | Application bootstrap, shared reusable utilities, common coordination |
| `WORLD/` | Active `World.tscn` gameplay world; parked `testworld.tscn`, terrain, generation, geographic regions and landmarks |
| `VEGETATION/` | Procedural plant placement, foliage rendering/LOD and wind response |
| `ENVIRONMENT/` | Sky, time, light, fog, weather, environmental ambience and shared wind conditions |
| `CAMERA/` | Active Camera.cs, reusable camera scene and projection/follow settings |
| `PLAYER/` | Existing first-person controller (parked); active player lives under `PLAYER/`, camera under `CAMERA/` |
| `ASSETS/` | Organized source/imported terrain, plants, rocks, cave modules and audio |
| `SAVE/` | Persistent seeds, authored overrides and later world-state changes |
| `DEBUG/` | Diagnostic overlays, profiling helpers, quality and distance controls |
| `ENTITIES/` | Shared robot foundation, zombies and reusable packet/resolver/health mechanics under Shared/Combat; each actor owns its reactions. Robot damage remains unimplemented. |
| `EQUIPMENT/` | Future reusable firearms, melee weapons and tools; `C/` houses Godot C# procedural mesh builders only, `Artwork/` holds later imported assets. Rifle_001 is implemented using shared firearm mechanics and editable `.tres` stats; active top-down player equips it by default. Awaiting local validation. |
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

**Implementation plans do not belong in the root README.** Current focus, setup and phase status:

**[WORLD/README.md](WORLD/README.md)** — current working scene, run instructions and priority: player, orthographic camera and robot mechanics.

**[NOTES/ideas.md](NOTES/ideas.md)** — brainstorming bank split into major and minor feature ideas; **not** an authorized implementation roadmap.

**[WORLD/RESTRUCTURE.md](WORLD/RESTRUCTURE.md)** — TD0–TD2 implemented on `implement-td0-td1` with local validation/feel checks outstanding; TD3 planned. **The old ZS combat plan remains cancelled; the newly authorized basic wander/chase zombies are documented under ENTITIES/TYPES/Zombies.**

**[ENTITIES/IMPLEMENTATION_PLAN.md](ENTITIES/IMPLEMENTATION_PLAN.md)** — historical reusable robot implementation notes. Existing robot functionality can be refined for the top-down prototype when explicitly requested.

**Parked legacy/environment plans:** [WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md](WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md), [WORLD/SURFACE_IMPLEMENTATION_PLAN.md](WORLD/SURFACE_IMPLEMENTATION_PLAN.md), and [WORLD/Content/Caves/IMPLEMENTATION_PLAN.md](WORLD/Content/Caves/IMPLEMENTATION_PLAN.md). Retain them and their assets, but do not resume by default.

AI workflow: read this README → read CodePreferences → read the owning folder's README and plan → inspect actual scripts/scenes/assets → work only on the explicitly requested phase → validate and update owning docs. Never assume planned features already exist.

**Current north star:** *First make the low-poly 3D player pleasant to control and the towering scanning robot convincing from the angled orthographic camera. Expand into survival, combat, building and eventually zombies only when explicitly requested.*

Parked first-person world content is retained in `WORLD/Content/`: vegetation, cave and rocks. It is **not** automatically part of the simple top-down test. See `WORLD/Content/README.md`.

## Active ownership after cleanup

Run **WORLD/World.tscn with F6**. CAMERA/Camera.cs and Camera.tscn own the active camera; PLAYER/PlayerController.cs, Player.tscn and PlayerSettings own the player. C/ contains procedural mesh builders only; Artwork/ holds future model replacements. Player, camera, world and environment masters are separate. First-person assets are parked under PLAYER/LegacyFirstPerson; F5 remains the original world. Existing user-local top-down terrain is copied to WORLD/Terrain/Gameplay without deleting the old bake. See WORLD/RESTRUCTURE.md for the migration and pending local validation.
