# WAR MACHINE

> **Working title — original sci-fi wilderness exploration / survival project.**  
> **Status:** Pre-production / Godot project foundation. This document describes the intended game and architecture, **not features already implemented**.

## 1. The vision

**War Machine** is a fully 3D, atmospheric wilderness exploration game that may grow into a tense, open-ended sci-fi survival experience. Its first goal is simple: **make an enormous-feeling, believable wilderness that is compelling to walk through.**

Picture an isolated, rugged landscape: dense wet forests, rocky valleys, steep mountains, fog-filled clearings, cave entrances, forgotten installations, changing weather and the distant movement of hostile mechanical machines. The player is small in this environment. The forest and atmosphere do as much work as detailed models or elaborate combat mechanics.

The desired mood takes inspiration from wilderness-set sci-fi survival thrillers involving soldiers and alien machines, but this project is intended to have its **own world, characters, machines and identity**. It is not an adaptation of or an affiliation with any film.

**Explore first. Survive later.** We will establish terrain, convincing vegetation, wind, visibility, lighting, traversal, distance rendering and a sense of place before committing to a complex survival or combat loop.

### Experience pillars

1. **Immersion over graphical spectacle.** Believable scale, composition, sound, light, weather and motion matter more than photorealistic individual assets.
2. **Exploration is the starting gameplay.** Traversing wilderness, finding landmarks and entering caves should be interesting even without crafting or combat.
3. **A persistent, shaped world.** Procedural tools help generate a foundation, after which hand-authored landmarks and adjustments give the world identity.
4. **Unsettling mechanical threats.** Large robots and drones make for compelling sci-fi opponents and can be assembled from modular, rigid components, avoiding some of the work of organic character animation.
5. **Performance from the foundation.** Large views and dense vegetation are core challenges, not polish tasks postponed until the end.
6. **Solo-developer / AI-friendly architecture.** Prefer small, comprehensible, configurable and reusable C# systems that can be developed and tested independently.

## 2. What this game is — and is not

**Intended early experience:** Walk or run through a coherent stretch of wilderness; see mountains and forests far away; discover a cave or abandoned installation; experience weather changes and moving vegetation; eventually hear or spot a patrolling machine in the distance.

**Possible later experience:** Stealth, pursuit, combat, injury, resources, shelters, scavenging, limited base building, robots, perhaps zombies or other threats, and richer survival simulation. These are **future possibilities**, not fixed mechanics.

The landscape is **not** intended as a Minecraft-style world where every surface can be mined or reshaped by the player. There is no requirement for an entirely voxel-based world. Selected high-impact events such as explosions may later scar the terrain, break props or topple vegetation, but **universal terrain destruction is not a foundation requirement**.

### Initial non-goals

- No endless / infinite world requirement.
- No universal dig-anywhere or fully destructible voxel terrain.
- No AAA-quality custom character or environment modelling requirement.
- No giant inventory, crafting tree, hunger system or multiplayer before exploration is enjoyable.
- No complicated simulation running on every tree, rock or remote enemy.
- No isolated showcase that never gets integrated into the real world scene.

## 3. Confirmed technical starting point

The repository currently contains the initial Godot project configuration and [CodePreferences.md](CodePreferences.md); it does **not** yet contain the proposed world or gameplay systems below.

| Item | Current project configuration |
| --- | --- |
| Engine | Godot 4, project features indicate **4.7** |
| Scripting | **C# / .NET** |
| Rendering | **Forward+** |
| Windows rendering driver | **Direct3D 12** |
| 3D physics | **Jolt Physics** |
| Repository default branch | `main` |

Do not treat the engine version shown in `project.godot` as proof that a specific addon is compatible. Check the actual installed Godot version, .NET requirements and addon compatibility during implementation.

### Preferred engine strategy

Stay with **Godot + C#** unless a concrete, measured limitation makes another engine necessary. Build small custom systems only where they add value; reuse Godot's built-in rendering, physics, instancing, resources and editor tools.

**Terrain3D is a candidate, not an installed dependency.** Evaluate its version compatibility, terrain sculpting, terrain holes, material painting, LODs and foliage workflow before adopting it. A custom heightmap/mesh-based terrain path remains an option if necessary. We do **not** need to write an entire terrain renderer merely to start exploring.

## 4. World design and generation

### World philosophy

The intended world is **finite, expandable and persistent**, not an endless stream of arbitrary noise. Procedural generation is an *authoring accelerator*: produce broad geographic structure, then give areas deliberate character using authored locations and environmental rules.

A **roughly 2 km × 2 km** wilderness is an *illustrative first proving ground*, not a locked final size. It must be large enough to evaluate travel, sightlines, LOD and streaming; expand only after this prototype performs and looks convincing.

### Proposed generation layers

1. **Macro geography:** mountain ranges, ridgelines, valleys, plateaus, drainage corridors and major clearings.
2. **Terrain surface:** slopes, elevation, ground materials, exposed rock and traversable space.
3. **Authored points of interest:** caves, abandoned facilities, ruins, landmarks and future encounter locations.
4. **Environmental regions:** forest density, moisture, altitude, rocky ground, wetlands and vegetation communities.
5. **Procedural detail:** trees, bushes, ferns, grass, fallen logs, stones, debris and local set dressing.
6. **Runtime population:** only relevant nearby threats, encounters, interactive objects and ambient effects.

These layers should support deterministic seeds and stable identifiers, so regeneration does not unpredictably move saved locations or authored edits. Generated defaults, hand-authored overrides and runtime changes must remain conceptually separate.

### Terrain and caves

Use **smooth, natural-looking terrain**, with normal terrain sculpting/painting as the primary shaping workflow. Caves, tunnels, overhangs and particularly dramatic cliffs can use modular meshes, terrain holes, custom geometry and transition dressing. They do not require a fully volumetric world.

For future explosions, investigate **localized and bounded effects** in this order: decals/scorch marks and debris; breakable props and vegetation; selective terrain edits or mesh swaps only where they justify their cost. Save persistent changes explicitly if/when implemented.

## 5. Rendering distances, streaming and performance

Long-range visibility is a first-class feature. A huge landscape does not feel huge if mountains disappear or forests pop into existence close to the player.

**Proposed distance tiers:**

| Distance band | Intended treatment |
| --- | --- |
| Near | Highest justified terrain detail, nearby foliage/grass, collision, interactive objects, active threats |
| Mid | Simplified terrain/trees, selectively reduced shadow and animation cost, dormant or coarse gameplay simulation |
| Far | Low-cost mountains, landscape silhouettes, simplified tree representations/impostors and atmospheric haze |
| Outside active area | Unload or suspend detailed regions while preserving necessary persistent state and distant representations |

Implement a **world streamer** that loads and unloads terrain regions and associated content around the player, with budgets to limit work per frame. Keep gameplay streaming separate from visual LOD: a distant mountain may remain visible even when its collision and interactive objects are absent.

Use Godot facilities where suitable: mesh LOD, visibility ranges, occlusion culling, `MultiMeshInstance3D`, GPU particles and reusable materials. Test tree shadow cost, alpha foliage overdraw, instancing behaviour, grass density, cave visibility and transition popping under real traversal.

Background tasks may prepare **data** safely, but scene-tree and rendering-resource mutations must respect Godot's thread-safety restrictions. Avoid a bespoke streaming framework until a small integrated test demonstrates the requirements.

### Baseline performance rules

Follow [CodePreferences.md](CodePreferences.md), especially **inactive means dormant**.

- No blanket `_Process` / `_PhysicsProcess` on thousands of trees, grass instances, drops or sleeping threats.
- Prefer shared schedulers, signals, explicit activation, pools, region-scoped searches and cached results.
- Zero weather intensity means emissions and unnecessary work truly stop after intentional fade-out.
- Update configuration, shader values and material properties when changed, rather than rewriting identical values every frame.
- Stagger expensive checks and bound procedural generation, streaming and spawning work per frame.
- Profile CPU **and** GPU on target hardware; do not claim a performance improvement without measurements.
- Each system should document its **wake-up trigger**, **idle cost**, **shutdown condition**, **scaling strategy** and **validation**.

Aim for adjustable quality tiers early: view distance, vegetation density, shadow distance, fog, particles, reflections and other expensive effects.

## 6. Environmental and atmospheric systems

This project's visual identity comes from **layers working together**, not any one expensive rendering feature.

| System | Desired behaviour |
| --- | --- |
| Day / lighting | Coherent sun, ambient light, shadows, exposure and time-of-day transitions |
| Wind | Shared wind field driving trees, branches, grass, particles and relevant sound cues |
| Weather | Clear/overcast, light/heavy rain, mist, storms and lightning; snow only if useful later |
| Fog | Height-/region-aware mist, atmospheric depth, local fog pockets and changing visibility |
| Vegetation | Environmental variation by terrain and moisture; varied scale, orientation and clusters |
| Water / wetness | Streams, puddles, damp rock/ground cues and eventual terrain-following water features |
| Audio | Spatial wind, rustling vegetation, distant machine movement, rain and environmental ambience |

Use Godot's **Forward+ lighting and volumetric fog** where appropriate; build small controllers around them rather than replacing the renderer. Expensive effects should be adjustable or optional. Avoid a forced aesthetic of bright, pristine or perfectly clean materials: the target is damp, rugged, lived-in wilderness.

### Asset strategy

Do not hand-model hundreds of intricate unique props before validating the environment. Start with a **small consistent kit** of good terrain materials, trees, undergrowth, rocks, cave modules and modular hard-surface machine parts. Use licensed assets where suitable, reuse with scale/material/placement variation, and reserve bespoke work for silhouettes and landmarks that define the game.

## 7. Exploration, enemies and future survival

**Exploration first:** build a satisfying first- or third-person traversal controller (camera choice remains open), sensible collision, terrain traversal, and meaningful sights and destinations in the **main world scene**.

**Mechanical enemies later:** start with a single large patrolling robot whose presence can be noticed through sound, motion, terrain occlusion and occasional distant sightings. Rigid components, animation rigs, procedural leg targeting and rotating weapon assemblies reduce dependence on organic animation, but believable locomotion and navigation still require work.

**Future expansion:** machine perception, patrol state, investigation, chase, combat, damage, spawning and persistence; optional zombies, resource collection and survival loops afterward. Keep all of these decoupled from terrain generation so the exploration experience remains usable without them.

## 8. Proposed project structure (not created yet)

The following is a **design target**, not a claim about existing folders. Keep major systems easy to locate and make the owning folder responsible for its settings and documentation.

```text
res://
  README.md                    # This vision and architecture overview
  CodePreferences.md           # Authoritative coding / workflow preferences
  project.godot
  CORE/
    README.md
    CoreConfig.cs              # Shared defaults / common services
    Shared/
  WORLD/
    README.md
    WorldConfig.cs             # World size, seed, streaming settings
    WorldRoot.tscn             # Integrated playable world scene
    Generation/
    Terrain/
    Streaming/
    Regions/
    PointsOfInterest/
  VEGETATION/
    README.md
    VegetationConfig.cs
    Placement/
    Rendering/
    Wind/
  ENVIRONMENT/
    README.md
    EnvironmentConfig.cs
    Lighting/
    Weather/
    Atmosphere/
    Audio/
  PLAYER/
    README.md
    PlayerConfig.cs
  ENTITIES/
    README.md
    EntityConfig.cs
    Robots/
  FX/
    README.md
    FxConfig.cs
  SAVE/
    README.md
    SaveConfig.cs
  UI/
    README.md
    UiConfig.cs
  DEBUG/
    README.md
    DebugConfig.cs
  ASSETS/
    README.md
    Terrain/
    Vegetation/
    Rocks/
    Robots/
    Audio/
```

These are **proposed** paths and configuration names. Only create folders and scripts when the owning feature is implemented. Use a clear "master config" in each major uppercase system folder for frequently adjusted parameters (C# Resource/inspector-exposed config where appropriate). Avoid duplicated sources of truth and avoid turning configuration classes into giant god objects.

### Scene and responsibility boundaries

- A **game/bootstrap scene** owns application-level services and enters the **real WorldRoot**.
- **WorldRoot** owns terrain/region streaming, placement coordination and world-level references.
- **Environment** controls shared time, weather, wind and light — individual trees do not each run a weather poll.
- **Vegetation** owns placement and rendering/LOD, not terrain generation or save format.
- **Entities** owns mechanical enemy behaviour, lifecycle and activation, not world topology.
- **Save** owns serialization and stable world changes, not gameplay rules.
- **Debug** provides instrumentation, overlays and quality controls without becoming a gameplay dependency.

Keep the scene tree readable: use logical shared parent nodes, groups only where justified and reusable helpers instead of thousands of cluttering manager nodes.

## 9. Coding and collaboration conventions

[CodePreferences.md](CodePreferences.md) is the **source of truth** for working conventions. Important expectations include:

- **C# first**, modular and uncomplicated. Prefer reusable helpers and data-driven options to deep inheritance or sprawling systems.
- Explain each script's purpose in a short **top-of-file comment**.
- Put explanatory comments **above methods**, with the preferred `=========================================================` separator, and use regions where they improve navigation.
- Group related inspector parameters with meaningful categories / exported configuration.
- Keep code vertically compact when it remains readable; avoid needless allocations and repeated tree searches in hot paths.
- Each major feature folder receives a **README.md** documenting its responsibilities, dependencies and configurable values.
- Read the root README, CodePreferences and owning feature notes before changes. Update the relevant documentation when behaviour changes.
- **One GitHub commit per authorized job.**
- Plan nontrivial work in reviewable, feature-prefixed phases; distinguish **Planned**, **In Progress**, **Implemented**, **Awaiting Validation** and **Accepted**.
- Do **not** automatically start subsequent phases. Document what was tested, what remains unverified and any local checks deferred to Copilot.

Prefer validating systems **in the actual world scene**. Temporary isolated test scenes may help diagnose a subsystem but must not be the only place a feature works.

## 10. High-level roadmap — proposed, not authorized implementation

This is a **directional sequence**, not the detailed implementation plan. Before coding a phase, inspect the real project and write a scoped plan under its owning folder as described in CodePreferences.

| Phase | Goal | Status |
| --- | --- | --- |
| WM0 | Audit project setup, choose terrain path, define performance baseline and establish playable WorldRoot | Planned |
| WM1 | Build natural, sculptable terrain and simple player traversal in the real world | Planned |
| WM2 | Prove world-region loading, terrain LOD, far-distance scenery and profiling | Planned |
| WM3 | Add scalable forest/rock/grass placement, vegetation LOD and shared wind | Planned |
| WM4 | Add lighting, atmospheric fog, dynamic weather, audio and quality controls | Planned |
| WM5 | Add exploration landmarks, cave transitions and authored world overrides | Planned |
| WM6 | Introduce one convincing mechanical patrol with visibility, navigation and sound | Planned |
| WM7 | Evaluate selective destruction and the first actual survival/combat loop | Planned |

### First experiential milestone

A player can launch the **main world**, walk through a convincing 3D forested valley, see hills/mountains in the distance, approach a cave, experience wind/rain/fog changes, and observe acceptable streaming/LOD behaviour while moving. A single distant robot encounter is the following milestone — **not a blocker** to proving the world itself.

Before expanding the map or adding crafting, measure frame time, memory use, vegetation cost, streaming hitches and the quality of distant views. If the wilderness does not already feel worth exploring, fix that first.

## 11. Open design decisions

These are deliberately **unlocked**, to be answered with small prototypes rather than assumptions:

- First-person, third-person or switchable camera?
- Which Godot 4 / Terrain3D versions are compatible and stable for this repository?
- Terrain addon versus custom terrain foundation?
- Final art direction: grounded realism versus lightly stylized naturalism?
- Target PC hardware, frame-rate goal and initial density / view-distance budgets?
- Fixed authored map, seed-generated map plus editing, or a hybrid authoring workflow?
- Where, if anywhere, is persistent terrain deformation worth its cost?

---

**Guiding principle:** *Make walking through the wilderness feel amazing before building a giant survival game on top of it.*  
**Project rules:** [CodePreferences.md](CodePreferences.md)
