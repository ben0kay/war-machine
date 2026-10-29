# WAR MACHINE

> **Working title:** Atmospheric, fully 3D sci-fi survival on a strange, inhabited Earth.  
> **Current direction:** Angled **top-down / isometric-style survival is the primary gameplay and balance reference**; **first-person is also an explicitly supported gameplay view**, sharing the same world and gameplay foundations. Supporting FPS does not automatically make it an equal design priority or require every future feature in both views.  
> **Development:** Evolving Godot 4 C# prototype, not a finished survival loop. This snapshot was reconciled with the `implement-td0-td1` source on **2026-10-28**. Feature READMEs, plans and code carry the fine details.

**Important for assistants and contributors:** The active development branch is **`implement-td0-td1`**, **not `main`**. Read and write on this branch explicitly unless the owner directs otherwise. [CodePreferences.md](CodePreferences.md) is authoritative for coding style, scope, performance, validation and commit rules.

> **New object/item authoring:** Read the [canonical Object Authoring Guide](WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md) before creating any new GLB furniture, bin, shelter, household item or prop. **SOC0** requires a documented choice of **None / AutoBox / CustomBoxes** (modes 0/1/2). The same guide distinguishes fixed Static collision from future movable Props/IX physics; creating an asset does not authorize placing it permanently in the world.

## Game vision

War Machine takes place on a recognizably earthly but unsettling world: misty forests, rolling terrain, cliffs, roads, abandoned businesses and houses, caves, changing light and weather, and signs of a science-fiction intrusion. **Zombies and imposing mechanical entities** are current threat foundations; other hostile life, wildlife and environmental dangers may broaden the setting later.

The player is a vulnerable survivor. **Explore, assess threats, use cover and stealth, scavenge, fight only when worthwhile, cope with injuries and continue surviving.** The world should feel inhabited and dangerous even when the player is not in combat. A believable convenience store, a dim road or an unusual silhouette through the haze can matter as much as a dramatic encounter.

### Design pillars

- **Threat awareness and survival decisions:** Visibility, noise, distance, line of sight, movement stance, ammunition, injuries and escape routes should influence decisions. Avoid making every encounter a straightforward firefight.
- **Top-down readability, genuine 3D depth:** Buildings, upper floors, obstacles, camera obstruction fading and line-of-sight rules must work in the primary angled view. FPS uses the same underlying world and simulation with its own camera/input/presentation adapters.
- **Distinct threat types:** Shamblers, runners, major machines and eventually other species should present meaningfully different behavior. Anatomy and injury effects should change what an entity can still do, not just reduce a health bar.
- **A lived-in, authored world:** Start with a convincing town and nearby wilderness. Reuse procedural generation for scenery/variants, but keep important streets, interiors, landmarks and encounter positions intentionally authored.
- **Atmosphere without disguising technical problems:** Eerie blue-grey mist, natural terrain, night lighting, weather and distant silhouettes are central to the mood. Fog is visual atmosphere, not a substitute for correct culling, collision or streaming.
- **Achievable and scalable:** Solo-developer-friendly, modular C#, configurable resources, replaceable art and performance-first activation/sleep. Hordes should be possible without giving every idle actor an unnecessary every-frame workload.

### Intended game loop (direction, not a completion claim)

Explore an area → identify opportunities and risks → approach quietly, investigate or avoid → collect useful supplies / manage equipment → escape or fight → handle damage, resources and consequences → decide where to go next.

The currently playable prototype tests only parts of this loop. **Complete looting, usable medical supplies, wider survival needs, persistence, vehicle driving, crafting and base-building are not shipped features.** Do not silently add them because they appear in this vision.

## Which scene is the actual game?

| Scene | Purpose | Run |
| --- | --- | --- |
| [WORLD/World.tscn](WORLD/World.tscn) | **Primary top-down survival game:** player, village, terrain, zombies, robot, combat, HUD and environment | Open and **F6 / Run Current Scene** |
| [WORLD/FirstPerson/FirstPersonWorld.tscn](WORLD/FirstPerson/FirstPersonWorld.tscn) | **Supported FPS gameplay view** inheriting the active world, with view-specific controls, gun presentation, head bob, fog and grass configuration | Open and **F6** |
| [WORLD/testworld.tscn](WORLD/testworld.tscn) | **Parked legacy wilderness/free-flight experiment**, retained with its terrain, caves and original environment work | Currently launched by **F5**, per `project.godot` |

**Do not mistake F5 for the survival game.** The startup setting intentionally still points to the legacy `testworld.tscn`; changing it is a separate authorized decision. New gameplay should normally be built against `World.tscn` and shared gameplay owners, not the parked explorer.

The two active views share terrain, buildings, entities, health/damage, inventory, weather and static streaming. Feature-specific FPS adapters live in their owning folders; see [FPS ownership](WORLD/FirstPerson/README.md).

## Current implemented source — snapshot

**Status language:** *Implemented in source* means code/assets are present, **not** that the current branch compiled, rendered correctly, passed local playtesting or demonstrated a performance improvement. Some earlier milestones have owner-confirmed tests; newer work below is generally **awaiting local validation**. Plans retain their own explicit phase states.

| Area | Source-implemented foundation | Not yet complete / important boundary |
| --- | --- | --- |
| **World and locations** | Saved Terrain3D gameplay area, village/road layout, graded sites, convenience store and reusable Building001–004, parked cars, furniture/hedges/lamps, cliff/rock/forest content | Prototype region, not a populated persistent large world; advanced town and terrain authoring still in phases |
| **Player and cameras** | Shared soldier/player, walk/run/crouch/prone, collision and jump, procedural pose; top-down follow/aim/zoom/occlusion; FPS look, body adapter and distance-driven head bob | Prone uses a low proxy capsule; realistic injury movement penalties and detailed player hurtboxes are future work |
| **Player condition** | PH0/PH1 hidden vitality, six body-condition regions, death/control gate and compact event-driven body silhouette | Medical treatment, bleeding, damage-dependent limb impairment and broader survival metabolism not yet implemented |
| **Firearms** | Rifle_001 and MachineGun_001, hitscan, shared damage/obstruction, shot spread, recoil/aim climb, muzzle feedback, ammo magazines, timed R reload and ammo HUD | Physical ammunition pickup/economy, reload animations and a full firearm/inventory interaction loop remain unfinished |
| **Melee and impact** | Q quick shove, equipped slot-3 baseball bat with active horizontal multi-target swing, bullet flinch and collision-aware zombie shove/bat backsteps | Knife/other melee families and zombie attacks against player are not implemented |
| **Zombies and perception** | Standard shamblers and runner variant, wandering, frontal initial detection, head/torso cover rays, pursuit, alerted tracking independent of facing once acquired, last-seen and bullet-hit fixed-point investigation | No obstacle-route navigation, outgoing attacks or AI response to nearby gunshot sounds yet |
| **Anatomy and injury** | Shared region-aware hits, condition/sever states, missing-limb visuals, limp/hop/crawl/immobilize policies, injury-aware corpse handoff | No full attack-capability filtering, detached limb debris or advanced articulated ragdoll |
| **Hitbox performance** | **ELD-PERF1:** one broad native anatomy shape per zombie by default; virtual joint-local boxes tested when firearm/bat queries reach the candidate. Includes bounded ray budgets and diagnostic counters | Correctness and measured FPS gain require local A/B profiling; do not report performance as proven |
| **Corpses** | ZD0 transfers existing zombie art/pose to one bounded temporary physics fall, then freezes settled corpses without active AI/collision | Corpse piles, sinking/expiry, crowd budgets and articulated skeleton physics are later phases |
| **Robots and wildlife** | Robot_001 patrol/scanning prototype; an optional stationary deer preview with a reusable creature/health foundation | Robot combat/strategic AI and wildlife populations/behavior are not yet implemented |
| **Inventory, UI and audio** | PlayerBackpack stack storage and backpack UI, shared hotbar, F1 visual-only placeable spawner, NPC health bars, body/ammo HUD, reusable weapon sound and WorldAudio foundation | World pickups, usable items and integrated loot/ammo storage are unfinished; playing gunshot SFX does **not** alert zombie AI |
| **Environment and scale** | Sky3D day/night, weather, grass/pines/rocks, haze and building exclusions, scheduled streetlamp cones, distance/occlusion fog; render-range policy and WS2 static-building streaming prototype | Broader actor/terrain streaming, persistent state and full-world population budgets are not implemented |

### Current small-area testbed

The [ConvenienceStoreSite001](WORLD/Content/Assets/Sites/ConvenienceStoreSite001/README.md) is a useful shared-world acceptance location. Current authored source has a **24 × 16 m store**, **56 × 36 m asphalt**, **42 marked bays in three rows**, **seven parked cars**, **eight night-scheduled streetlamps** and hedges/access paths. Building001–004 and a robot patrol occupy the surrounding village.

The **store zombie stress option** currently requests **16 extra shamblers + four extra runners** beyond the nominal village nine shamblers + one runner. `StoreStressZombiesEnabled` is on in `WORLD/WorldSettings.cs` unless overridden in the world master. Terrain/obstruction/spawn-clearance rules may skip some candidates; inspect the actual accepted-spawn log. This is a repeatable **population/performance test**, not a finished spawn director.

The reusable F1 placeable catalogue previews eligible visual asset wrappers; it is **not** a complete persistent in-world placement or physics-item system. Static world streaming currently covers the four integrated village buildings and a separate proof; the store site and many other world systems remain resident.

## Gameplay system boundaries

### AI, combat, anatomy, corpses

Keep the responsibilities separate:

1. **Perception and investigation** recognize visible targets or verified clues. Initial zombie sight uses a frontal cone; alerted sight can track from behind when line of sight stays clear. Last-seen and direct bullet-hit investigation store a fixed point, not live omniscient tracking. HE0 sound events exist, but HE1 AI listening/attraction does not.
2. **Combat** determines hit contact, cover, damage packets, recoil, reactions and attack windows. The existing shared damage/impact resolution is the authority; do not create parallel pipelines for each view/weapon.
3. **Anatomy** records region condition, capability changes and severing independently of overall vitality. Deferred broad candidate + virtual per-limb refinement is the current default, not eight constantly synchronized native shapes per moving zombie.
4. **Locomotion/animation** interpret injury state using the existing actor movement callback and rig; they do not run duplicate AI loops. Crawling and hopping are first-pass behaviors, not final realistic biomechanics.
5. **Corpses** receive the existing visuals and injury pose after death; their bounded fall and settled state are separate from a living entity. A frozen body is not a full skeletal ragdoll.

**Current limitations:** Zombies still do not attack the player. Hearing-based reaction to shots and robust obstacle pathfinding remain distinct future features. ELD3–ELD5 and ZD1–ZD3 are not automatically authorized by the existing implementation.

### Views and assets

Gameplay rules should live with shared PLAYER, ENTITIES, EQUIPMENT, COMBAT and WORLD owners. Top-down and FPS may differ in input, camera, screen-space aiming, recoil/animation presentation and visibility, but not silently fork health, zombie AI, inventory or world state.

World content is built from reusable scenes. For generated art, **`C/` contains C# mesh construction** and **`Artwork/` holds future imported GLB/texture replacements**. Stable scene/gameplay contracts must survive visual swaps. Separate cosmetic appearance, physics/collision, interactability and persistent identity; one does not imply the others.

### Performance and future world size

Favor **inactive means dormant**: movement/AI work only when needed, low-rate/staggered decision updates, explicit wake/sleep, bounded physics queries, event-driven UI, pooled/shared geometry, GPU foliage batches and render distances appropriate to the current view. A visual fog overlay does not suspend invisible actors.

World direction is a **large finite, expandable and partly authored town/wilderness**, not a fully allocated infinite sandbox. WS0 section identity, WS1 fog-aware render policy and WS2 bounded static-building load/unload are in source. **WS3 save round-trip, WS4 mobile actor ownership/relevance, WS5 authoring and WS6/WS7 larger populated regions are still planned.** Grow only after validating representative encounter and streaming budgets; don't overwrite saved terrain while exploring or adjusting gameplay.

## Project ownership map

| Folder | Responsibility |
| --- | --- |
| [WORLD](WORLD/README.md) | Primary scene/composition, gameplay terrain, village layout, reusable structures/props, static streaming, render policy; legacy environment scene retained |
| [PLAYER](PLAYER/README.md) | Shared player lifecycle, input/movement/stances, visuals, vitals and player equipment adapters |
| [CAMERA](CAMERA/README.md) | Main follow/zoom camera, obstruction fading, FPS camera and head bob |
| [ENTITIES](ENTITIES/README.md) | Shared actors, sensing/investigation, health/anatomy/corpses, zombies, robot and wildlife |
| [COMBAT](COMBAT/README.md) | Shared impact/contact execution, melee and reactions; general health authority remains in ENTITIES |
| [EQUIPMENT](EQUIPMENT/README.md) | Firearms/melee equipment, stats, ammo/recoil, weapon slots and view-specific visuals |
| [INTERACTION](INTERACTION/README.md) | Physical-contact helpers and a staged plan for carry/throw, cupboards, destruction, world pickup/placement and scavenging |
| [INVENTORY](INVENTORY/README.md) | Implemented backpack and stack storage, not a full persistent item-world integration |
| [UI](UI/GameplayHUD/README.md) / [FEEDBACK](FEEDBACK/README.md) | Hotbar, backpack, ammo/body HUD, NPC bars and reusable feedback |
| [AUDIO](AUDIO/README.md) | Shared audio cues, playback and world sound events; playback and AI hearing are different concerns |
| [ENVIRONMENT](ENVIRONMENT/README.md) | Sky, time, weather, fog/haze, lighting and fixtures |
| [VEGETATION](VEGETATION/README.md) | Grass, pines, GPU/instanced vegetation, rendering/relevance and settings |
| [DEBUG](DEBUG/README.md) | Tuning, diagnostics, test scenes, spawn tools and performance observations |
| [NOTES](NOTES/README.md) | Exploratory concepts only; not an approved implementation backlog |

Each owning folder's README and master configuration should describe current code, tunable values, known limits, planned phases and validation. This root document explains the overall direction and **should not accumulate dated patch notes or contradictory phase histories**.

## Run and validate

1. Use a compatible **Godot 4 .NET** editor and .NET SDK; check the actual installed engine/addon versions. Source project config currently advertises Godot **4.7**, **Forward+**, Windows **D3D12**, **Jolt Physics** and physics interpolation.
2. Run `dotnet build war-machine.csproj` when available. For gameplay use **F6** on [WORLD/World.tscn](WORLD/World.tscn); use **F6** on [WORLD/FirstPerson/FirstPersonWorld.tscn](WORLD/FirstPerson/FirstPersonWorld.tscn) to compare FPS. **F5 still opens the legacy scene.**
3. The shared player code currently binds **arrow keys for movement, Shift to run, Right Ctrl to crouch, `/` to toggle prone and RMB to jump**. Mouse aims in top-down and looks in FPS; LMB fires/uses equipped primary attack, **Q** shoves, **R** reloads and number keys select equipped slots. Escape releases FPS mouse capture. Check [current input source](PLAYER/Input/PlayerInput.cs): some older feature docs describe different crouch/prone bindings.
4. Use the debug overlay and [anatomy A/B checks](ENTITIES/Shared/Anatomy/DEFERRED_HIT_DETECTION.md) for the current horde. Record accepted spawn counts, zombie idle/moving costs, hit correctness, visibility, physics, corpse settle behavior and FPS/frametime before claiming optimization benefits.
5. When the active terrain is missing, follow [WORLD setup](WORLD/README.md) / [terrain generation](WORLD/Generation/README.md) for the explicit, protected one-time bake/migration. **Do not rebake or overwrite authored terrain on normal startup.**

A successful push or source review is **not proof of compilation, local playability or visual/performance acceptance**. Respect the local test handoffs in each feature README.

## Feature documentation / next decisions

Use these documents for the precise implementation and phase boundaries rather than duplicating their history here:

- [World and town](WORLD/README.md), [static streaming roadmap](WORLD/WORLD_STREAMING_IMPLEMENTATION_PLAN.md), [convenience-store site](WORLD/Content/Assets/Sites/ConvenienceStoreSite001/README.md) and [object authoring guide](WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md).
- [Player](PLAYER/README.md), [body-condition design](PLAYER/Stats/BODY_CONDITION_DESIGN.md), [player phased plan](PLAYER/IMPLEMENTATION_PLAN.md) and [FPS view](WORLD/FirstPerson/README.md).
- [Zombies](ENTITIES/TYPES/Zombies/README.md), [shared perception](ENTITIES/Shared/Sensing/README.md), [investigation](ENTITIES/Shared/Investigation/README.md), [anatomy/injury](ENTITIES/Shared/Anatomy/README.md), [unified anatomy/combat/corpse plan](ENTITIES/TYPES/Zombies/ANATOMY_COMBAT_CORPSE_PLAN.md), [corpse foundation](ENTITIES/Shared/Corpses/README.md) and [deferred hitbox performance](ENTITIES/Shared/Anatomy/DEFERRED_HIT_DETECTION.md).
- [Weapons](EQUIPMENT/Weapons/README.md), [ammo plan](EQUIPMENT/Weapons/AMMO_IMPLEMENTATION_PLAN.md), [recoil plan](EQUIPMENT/Weapons/RECOIL_IMPLEMENTATION_PLAN.md), [bat](EQUIPMENT/Melee/BaseballBat_001/README.md), [combat](COMBAT/README.md) and [interaction phases](INTERACTION/IMPLEMENTATION_PLAN.md).
- [UI](UI/GameplayHUD/README.md), [inventory](INVENTORY/README.md), [audio](AUDIO/README.md), [world atmosphere](ENVIRONMENT/README.md) and [ideas](NOTES/ideas.md).

**Development workflow:** Read this README → [CodePreferences.md](CodePreferences.md) → owning README/plan → **actual current branch source and configuration**. Separate implemented, awaiting validation, accepted and proposed work. Draft/review phased plans where useful; implement **only explicitly authorized work**, verify the branch before writing, keep **one commit per job**, do proportional checks and update the owning documents without rewriting unrelated systems.

**North star:** *Build an atmospheric, replayable survival experience where an eerie lived-in Earth, dangerous encounters, physical cover and meaningful injury/resource decisions make exploration worth the risk—without losing sight of scope or performance.*
