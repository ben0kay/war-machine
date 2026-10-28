> **Anatomy/combat/corpse direction (2026-10-28):** [Overall master plan](ENTITIES/TYPES/Zombies/ANATOMY_COMBAT_CORPSE_PLAN.md). Approved overall planning direction for shared anatomical hits, injury-driven zombie movement/attack capabilities and injury-preserving corpse handoff. ELD0–ELD5 remain Planned; existing Combat, Player and ZD phase ownership/statuses are preserved. No anatomy gameplay is implemented by this documentation update.

> **Shared prone locomotion (2026-10-28):** Right Ctrl toggles a first-pass prone/crawl stance in top-down and FPS using the same PlayerController; Left Ctrl/C now controls crouch. Slow prone speed (0.17×, 0.85m/s at base walk), a 0.75m low capsule, active-only crawl pose, overhead clearance tests and FPS eye height follow. This is an upright **low collision proxy**, not a full-length horizontal prone collider. Source implemented, local .NET/Godot acceptance pending. [PLAYER README](PLAYER/README.md).

> **Player PH0/PH1 implemented in source (2026-10-28), awaiting local validation:** hidden vitality, six independent body-condition regions, death/control gates and a compact event-driven silhouette. No generic player HP bar; NPC bars remain. Precision hurtboxes, limb penalties, bleeding, medical items and survival stats are later phases. [Player plan](PLAYER/IMPLEMENTATION_PLAN.md) · [body design](PLAYER/Stats/BODY_CONDITION_DESIGN.md).

# WAR MACHINE

> **Weapons update 2026-10-28:** RC0 shared per-weapon accumulated recoil and recovery are implemented in source (awaiting local validation); **RC1 top-down and RC2 FPS 3D ballistic aim climb and visual gun recoil implemented in source (awaiting local validation)**. [RC plan](EQUIPMENT/Weapons/RECOIL_IMPLEMENTATION_PLAN.md). Existing shot spread and damage are unchanged. **FPS recoil update:** each accepted rifle/machine-gun shot pushes the real first-person camera upward, without automatic downward return. MG FPS-only burst climb now reaches up to 25°; rifle also kicks each slower shot. Top-down RC1 camera and recoil cap remain unchanged.

> **UI implementation update (2026-10-28):** `FEEDBACK/` now contains a generic source-implemented HealthBar and HUD overlay showing a bar only for living entities with shared `HealthComponent` and HP below maximum. Automatic registration supports NPC health; the player now uses the separate PH1 body silhouette. These bars await local build/visual validation; no damage calculations or health values were changed. See [FEEDBACK/IMPLEMENTATION_PLAN.md](FEEDBACK/IMPLEMENTATION_PLAN.md).

> **Working title — atmospheric, top-down 3D survival on a strange, inhabited Earth.**  
> **Primary game:** an angled overhead / isometric-style view of a fully 3D world. **First-person is a side debug prototype**, not a second production direction.  
> **Development status:** early, evolving gameplay prototype on `implement-td0-td1`. This README describes the **game vision, current implementation and system ownership**; feature plans and validation details live in their owning folders.

> **Weapons ammunition update (2026-10-28):** AM0 finite 30-round rifle / 100-round machine-gun magazines, timed reloading, R input, automatic reload on empty and debug unlimited **reserve** ammo are now integrated in shared C# source (pending local build/acceptance). AM1 compact ammo HUD beside the bottom hotbar (right of player-body HUD) is now source-implemented, awaiting local visual validation; AM2 physical ammo/animations remain planned. [AM0–AM2 plan](EQUIPMENT/Weapons/AMMO_IMPLEMENTATION_PLAN.md).

## Game vision

**War Machine** is a sci-fi survival game about staying alive in a dangerous version of Earth. The landscape can feel recognizably earthly—forests, hills, roads, abandoned buildings, caves and changing weather—but the world is strange, unsettling and inhabited by threats the player cannot simply overpower.

**Zombies, large mechanical entities and science-fiction elements** are central to the direction. Other hostile creatures may join the world later, but their nature and gameplay are not decided yet. Exploration, scavenging, stealth, combat and decisions about when to avoid danger should eventually form the core experience. The player is a vulnerable survivor, not an unstoppable action hero.

The aim is a world that encourages the player to ask: *What is out there? Is it safe to approach? Do I have the supplies and the time? Will moving quietly be better than firing?* A meaningful survival experience matters more than filling the world with disconnected mechanics.

This is **not a first-person shooter being converted to top-down**. The primary game is designed for the overhead viewpoint from the outset, with readable encounters, approachable controls and a manageable development scope for a solo developer.

### Design pillars

- **Survive by understanding the world.** Exploration, threat awareness, resource decisions, cautious movement and selective combat should reinforce one another.
- **Stealth matters.** Cover, line of sight, crouching and eventually sound should give the player ways to avoid, misdirect or escape threats. Stealth is an important part of survival, not a requirement that every encounter be solved silently.
- **Layered threats.** Zombies create immediate, recurring danger; large robots/machines should feel imposing and unpredictable. Future sci-fi creatures can add variety when their role is established. Different threats should eventually interact where it benefits the game.
- **Atmosphere and geography matter.** Forests, cliffs, structures, fog, weather, lighting and distant silhouettes create a believable but eerie Earth. Locations should be interesting to discover and useful to navigate.
- **Fully 3D, built for the overhead view.** The default presentation is angled orthographic/isometric-style, with adjustable camera options for development. Favor clear silhouettes, readable combat/animation and practical visibility through roofs, walls and vegetation.
- **Stylized rather than photorealistic.** Consistent low-poly/procedural art, grounded proportions and atmospheric presentation are more valuable than AAA asset detail. Models can be replaced with authored artwork later.
- **Solo-developer scope and performance first.** Build a playable loop gradually, reuse modular C# systems and configurable resources, keep inactive work dormant, and avoid sprawling features before their gameplay value is proven.

### Intended survival loop — vision, not a claim of completed features

Explore and investigate locations → find useful supplies or opportunities → assess zombies, machines and environmental risks → move quietly, escape or fight → survive the consequences and continue exploring. Scavenging, broader survival needs, progression and a persistent shelter/base are **possible extensions of this loop**, not commitments to a specific crafting, hunger or building design yet.

## Primary top-down game and first-person experiments

### Primary: `WORLD/World.tscn`

The active game prototype uses a **3D angled overhead camera**, defaulting to orthographic projection. Camera-relative movement, mouse aiming, a player-follow camera and scenery fading are part of the top-down presentation. It is the reference view for gameplay, balance, visibility and future development decisions.

**Develop gameplay for the top-down experience by default.** New mechanics should live in shared gameplay owners where practical rather than be tied to an FPS camera. Changing the camera for a debug comparison must not silently redefine the project's main direction.

### Side experiment: `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`

The FPS test intentionally remains available to see how the **same playable world** looks and feels from eye level. It inherits the active world and reuses terrain, buildings, entities, equipment, weather and shared gameplay logic, while substituting first-person input/camera/presentation. It is **a debug/prototyping tool, not an alternative main game or an obligation to support every future feature equally in both views**.

Keep its specialized work under `DEBUG/TEST/FirstPerson/` whenever possible. Shared improvements can flow to both perspectives where appropriate, but **do not prioritize FPS-specific polish over top-down gameplay** without an explicit request.

### Older first-person wilderness: retained, parked

`WORLD/testworld.tscn` and `PLAYER/LegacyFirstPerson/` preserve an earlier environment/free-flight experiment with terrain, vegetation, caves and robot work. Its historical environment plans and assets are useful reference material, but the old first-person roadmap is **not** the main game plan. Do not delete it or automatically resume it. This legacy scene is still set as the project's F5 startup scene; use **F6 / Run Current Scene** for the active top-down game or the dedicated FPS debug test.

## Current implementation snapshot

The following describes systems visible in the `implement-td0-td1` source, **not a statement that every feature has passed in-game validation**:

| Area | Present in the working code | Important limits |
| --- | --- | --- |
| Player and camera | Reusable 3D player, walk/run, crouch, ground collision, procedural animation; angled follow camera with zoom and optional perspective projection | Core movement and visual feel continue to be tuned |
| Weapons and damage | Reusable rifle/hitscan firing, shot spread foundation, impact feedback, damage packet/resolver and zombie health/death | No complete ammo/reload/inventory system; player survival/health is not a completed gameplay loop |
| Zombies | Spawned reusable zombies, wandering, frontal vision, head/torso line-of-sight and cover checks, pursuit and damage reception | No attacks on the player, navigation around obstacles, hearing or last-known-position investigation yet |
| Mechanical entities | Reusable Robot_001 patrol, movement, animated mechanical body and scanning work | Robot combat and wider threat interactions remain future work |
| World and props | Terrain3D gameplay terrain, hills/cliff testing, pines/grass/rocks, three building prefabs, a car and collision-aware scenery placement | This is a contained prototype scene, not a finished populated or streamed survival world |
| Visibility and atmosphere | Roof/wall/upper-floor camera fading, overhead distance fog, Sky3D day/night and weather controls | Scene visibility and many artistic/performance choices still need local visual acceptance |
| FPS debug | Separate first-person scene using the shared world and entities | A camera/input test, with intentionally limited FPS-specific presentation |

**Stealth status:** crouching, physical cover and zombie vision are real foundations. Sound-based attraction (gunshots, movement and other noise), investigation of a remembered sound/last-seen position, and more sophisticated stealth AI are **planned/proposed**, not implemented. See [shared sensing](ENTITIES/Shared/Sensing/README.md) and its hearing plan. An audible gunshot effect is **not** the same as an AI hearing system.

**Still exploratory:** looting, inventory, consumable survival needs, player injury/death consequences, base-building, wider zombie/robot interactions, additional creature types and large-world persistence. They are part of the design space, not features to claim as shipped or to add automatically.

## World and art direction

The game should feel like an eerie, lived-in Earth with a science-fiction intrusion rather than a generic featureless test map. Aim for natural terrain and vegetation, atmospheric depth, weather, abandoned human structures and occasional signs of something unfamiliar. Mist, long shadows and distant forms can make exploration tense without requiring maximal graphical detail.

Maintain a mix of procedural **world creation tools** and editable/author-placed points of interest. Terrain need not be universally destructible or voxel-based; caves and interiors may use authored meshes and separate geometry. Keep access/visibility readable from the overhead camera, especially around multi-level buildings. Procedurally generated placeholder models under `C/` should remain replaceable by future `Artwork/` assets without rewriting gameplay.

The goal is not to implement everything at once. Prioritize a small, convincing survival encounter loop in the existing playable world before enlarging its geography or layering on complex systems.

## Technical foundation and organization

This is a **Godot 4 C#/.NET** project. The checked-in config targets Godot **4.7** features, **Forward+**, Windows **Direct3D 12** and **Jolt Physics**. Terrain3D and Sky3D are integrated; the repository also includes environment/weather tools and existing generated content. These version/configuration details are not a guarantee of local compatibility. Earlier targeted checks do not substitute for validating the current working branch in Godot.

The major current or anticipated owners are:

| Folder | Responsibility |
| --- | --- |
| `WORLD/` | Shared playable world, terrain, structures, roads/landmarks, placement, region/world work and scene composition; historical wilderness retained separately |
| `PLAYER/` | Active player body, movement, stances, animation and player-owned equipment integration; old explorer parked under `LegacyFirstPerson/` |
| `CAMERA/` | Primary angled camera, configuration, zoom/projection and overhead occlusion/fading |
| `ENTITIES/` | Shared actor foundations, sensing and the currently implemented generic damage primitives; zombie and robot-specific behaviour/art |
| `COMBAT/` | Shared melee/impact roadmap; CM0/CM1 reaction code now exists and later CM phases remain planned; generic health/damage stays in ENTITIES for now |
| `INTERACTION/` | **New plan only** for physics props, lift/throw, cupboards, breakable boards, item state/placement and view-specific interaction adapters |
| `INVENTORY/` (future) | Persistent item identity/ownership and state transitions; create when IX5 is authorized, not yet implemented |
| `EQUIPMENT/` | Shared weapon/tool foundations, weapon scenes, tunable stats, shot and damage integration |
| `ENVIRONMENT/` | Sky, lighting, fog, rain, snow, wind and common environmental conditions |
| `VEGETATION/` | Shared grass and plant/tree placement, rendering and distance/idle-cost controls |
| `DEBUG/` | Tuning tools, diagnostics and the **non-primary FPS test** |
| `NOTES/` | Concepts, design notes and ideas; not automatically authorized implementation |
| `UI/` | Gameplay HUD screen-space layout and static hotbar; future status widgets arranged here, not health state |
| `FEEDBACK/` | **New folder/documentation foundation only:** FB0 scaffold for future reusable health/defence bars and impact effects; no UI implemented |

Some planned owners (for example persistence and inventory) are **not fully implemented**. Prefer folder READMEs, central configuration resources, replaceable assets and small reusable helpers over unnecessary managers or huge scene trees. Use events, staggered checks, bounded sensing, streaming/relevance and actual sleep/deactivation to keep idle cost low.

## Opening and testing

1. Open the project in a compatible **Godot .NET** editor and build the C# project.
2. For the **main top-down prototype**, open `WORLD/World.tscn` and press **F6**. Arrow keys move relative to the view; Shift runs; Right Ctrl crouches; mouse aims; left click/hold fires; mouse wheel zooms. Scene-local tuning/debug controls expose further settings.
3. For the **first-person comparison only**, open `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` and press **F6**. Read its [test README](DEBUG/TEST/FirstPerson/README.md) for mouse capture and controls.
4. **F5 currently launches the parked `WORLD/testworld.tscn`**, not the top-down game. Do not change the startup scene without authorization.

If the active scene reports missing gameplay terrain, follow [WORLD/README.md](WORLD/README.md) to perform the one-time saved-terrain bake/migration. Do not overwrite existing terrain data or rebake as a normal runtime step.

## Working rules and detailed references

**[CodePreferences.md](CodePreferences.md) is authoritative** for C# style, comments/regions, folder documentation, master configurations, performance-first execution, brief proportional validation, phased planning, assistant allocation and **one commit per authorized task**.

Start with this README for intent, then read the owning README, relevant plan and **current code/scene/config** before making changes. Distinguish implemented code, pending local validation, proposed features and ideas. Documentation updates must not silently authorize an unrelated implementation.

- [WORLD/README.md](WORLD/README.md) — active world, terrain, structures, scenery and scene setup
- [PLAYER/README.md](PLAYER/README.md) and [CAMERA/README.md](CAMERA/README.md) — player and main overhead camera
- [PLAYER/IMPLEMENTATION_PLAN.md](PLAYER/IMPLEMENTATION_PLAN.md) — **PH0–PH5 Planned**, Codex-ready player health/healing/death and later survival stats
- [FEEDBACK/IMPLEMENTATION_PLAN.md](FEEDBACK/IMPLEMENTATION_PLAN.md) — **FB0 folders/docs present; FB1–FB4 Planned** for shared player/enemy health bars, optional depletable defence bars and feedback effects
- [ENTITIES/TYPES/Zombies/README.md](ENTITIES/TYPES/Zombies/README.md) — current zombies and explicit limitations
- [ENTITIES/Shared/Sensing/README.md](ENTITIES/Shared/Sensing/README.md) — shared visibility, hearing proposal and perception phases
- [EQUIPMENT/Weapons/README.md](EQUIPMENT/Weapons/README.md) — shared gun systems and accuracy work
- [EQUIPMENT/Weapons/RECOIL_IMPLEMENTATION_PLAN.md](EQUIPMENT/Weapons/RECOIL_IMPLEMENTATION_PLAN.md) — RC0–RC2 source implemented (local testing pending): cumulative recoil, top-down aim climb and FPS presentation; separate from random spread
- [COMBAT/IMPLEMENTATION_PLAN.md](COMBAT/IMPLEMENTATION_PLAN.md) — impact and stagger foundation implemented/awaiting validation; remaining quick melee, bat, knife and zombie outgoing attacks tracked there
- [INTERACTION/IMPLEMENTATION_PLAN.md](INTERACTION/IMPLEMENTATION_PLAN.md) — **new master plan (IX0–IX8; nothing implemented):** physical props and carrying/throwing, cupboards, destructible planks, item identity, rifle placement and scavenging; see [INTERACTION overview](INTERACTION/README.md)
- [WORLD/Content/Assets/Objects/README.md](WORLD/Content/Assets/Objects/README.md) — **new model library:** fixed Objects versus pickup-able Props, Bookcase001 and Book001 original GLBs, future furnishing catalogue
- [DEBUG/TEST/FirstPerson/README.md](DEBUG/TEST/FirstPerson/README.md) — optional FPS comparison
- [WORLD/RESTRUCTURE.md](WORLD/RESTRUCTURE.md) and [WORLD/WORLD_SCENE_RESTRUCTURE_PLAN.md](WORLD/WORLD_SCENE_RESTRUCTURE_PLAN.md) — current world restructuring/validation history
- [NOTES/ideas.md](NOTES/ideas.md) — optional ideas, not an approved roadmap
- [ENTITIES/IMPLEMENTATION_PLAN.md](ENTITIES/IMPLEMENTATION_PLAN.md), [WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md](WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md) and [WORLD/Content/Caves/IMPLEMENTATION_PLAN.md](WORLD/Content/Caves/IMPLEMENTATION_PLAN.md) — feature/historical plans, not automatic next steps

**North star:** *Build an atmospheric, mechanically interesting top-down survival game about navigating a hostile, unfamiliar Earth—using stealth, exploration and carefully chosen combat to survive zombies and science-fiction threats. Keep the first-person test useful, but keep the main game achievable for one developer.*

> **Wildlife preview (2026-10-28):** `ENTITIES/TYPES/Wildlife/Deer/` now contains an inherited passive deer entity, 120 HP and a basic procedural animal mesh. One stationary deer is configured to spawn near the active world's player for shooting tests, with an opt-out in `WORLD/Configuration/WorldMaster.tres`. AI and wildlife populations are not implemented. See [deer README](ENTITIES/TYPES/Wildlife/Deer/README.md).

> **Debug F1 placeable menu (2026-10-28):** Dynamic canonical GLB visual-wrapper discovery is implemented in source for `WORLD/Content/Assets/Objects/{Static,Props}`, with automatic Bed/Table exported enum variants and Hedge001 inclusion. Procedural physics-bearing Car001/StreetLamp001 are intentionally outside the visual-only F1 catalogue. New correctly authored `PlaceableVisual` asset scenes appear on next world load with **no manual spawner registration**. See [F1 spawner docs](DEBUG/PlaceableSpawner/README.md). Local Godot/.NET checks pending.

> **Streetlamp lighting update (2026-10-28):** Source-implemented faint warm-yellow visible cone under each reusable streetlamp, with no collision/shadow or per-lamp tick. A single Sky3D-driven night schedule now switches all existing/future lamp instances ON at **18:30** and OFF at **06:30** in-game; configure globally in [StreetLampScheduleMaster.tres](ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampScheduleMaster.tres), and the cone's intensity/length/radius in [StreetLampLightingMaster.tres](ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres). Local C# build, shader compile and visual/FPS acceptance pending.

> **Streetlamp cone/schedule tweak (2026-10-28):** The visual beam now fits each StreetLamp001 head height and extends 0.85m beneath its base to hide the former floating end; the global Sky3D switch-on changes to **17:15** (was 18:30), while OFF stays **06:30**. Configure [light cone](ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres) and [shared schedule](ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampScheduleMaster.tres). Source implemented; local Godot build and visual verification pending.

> **Convenience-store parking expansion (PARK3, 2026-10-28):** Site-only 3-row layout now marks **42 bays** (14 original + 28 new) on the existing **56×36m** asphalt, retains two 6m driving aisles and a 9.3m central entry corridor, and adds four interior night-scheduled StreetLamp001 fixtures (8 total) with noncolliding concrete pads. Two existing roadside lamps move outward to clear new stalls. Original seven cars, hedges, store, road and terrain-support contract remain. Reusable default ParkingLot001 is still single-row. [Site README](WORLD/Content/Assets/Sites/ConvenienceStoreSite001/README.md). Source implemented; local Godot check pending.
