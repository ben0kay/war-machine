# WAR MACHINE

> **Working title:** Atmospheric, fully 3D sci-fi survival on a strange, inhabited Earth.  
> **Current direction:** **First-person survival is the primary gameplay, presentation and balance reference.** Angled **top-down / isometric-style play remains a supported secondary view**, sharing the same world and gameplay foundations. Prioritize FPS feel and usability when making design tradeoffs; this does not require every future feature to receive equal presentation work in both views.
>
> **Development:** Evolving Godot 4 C# prototype, not a finished survival loop. This snapshot was reconciled with the `implement-td0-td1` source on **2026-10-29**. Feature READMEs, plans and code carry the fine details.

**Important for assistants and contributors:** The active development branch is **`implement-td0-td1`**, **not `main`**. Read and write on this branch explicitly unless the owner directs otherwise. [CodePreferences.md](CodePreferences.md) is authoritative for coding style, scope, performance, validation and commit rules.

> **New object/item authoring:** Read the [canonical Object Authoring Guide](WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md) before creating any new GLB furniture, bin, shelter, household item or prop. **SOC0** requires a documented choice of **None / AutoBox / CustomBoxes** (modes 0/1/2). The same guide distinguishes fixed Static collision from future movable Props/IX physics; creating an asset does not authorize placing it permanently in the world.

> **Object render/debug status:** Shared per-asset `ShadowMode` (Inherit / Disabled / Enabled) is implemented in source as the shadow-only subset of SOC1A. [SOC1 plan](WORLD/Content/Assets/Objects/SOC1_OBJECT_DIAGNOSTICS_PLAN.md) still proposes render caps, F1 inspection, malformed-GLB checks and performance cost reports. Runtime validation pending.

> **Stylized scattered pines (PINE-ST1, source implemented):** [Pine art replacement](VEGETATION/Trees/Pine/README.md#pine-st1--stylized-pinetree001-model-source-switch-2026-10-30) now selects the new `ASSETS/Vegetation/Trees/PineTree001` glTF pack for the regular randomly spawned village-area pines, with three authored LOD ranges (40/70/96m), preserving placements/collision/top-down fade/chunk streaming. Set `PineMaster.tres → UseStylizedPine=false` to restore old procedural trees; grass and DF2 broadleaf forest unchanged. Godot compilation/render/FPS validation pending.

> **Dense woodland prototype — DF0–DF2 source implemented, awaiting validation:** [Forest phases](VEGETATION/Forest/FOREST_IMPLEMENTATION_PLAN.md) include an enabled east-side test region (subject to DF0 survey approval), three DF1 broadleaf tree variants and **DF2 seeded/streamed MultiMesh canopy placement with trunk collision** when DF0 preflight approves. `VEGETATION/Forest/Configuration/ForestMaster.tres → Enabled` is now true for FPS/World F6 testing; set false to revert. No saved terrain edits, global lighting or undergrowth work. DF3–DF5 remain planned; runtime visuals and frame time unverified.

## Game vision

War Machine takes place on a recognizably earthly but unsettling world: misty forests, rolling terrain, cliffs, roads, abandoned businesses and houses, caves, changing light and weather, and signs of a science-fiction intrusion. **Zombies and imposing mechanical entities** are current threat foundations; other hostile life, wildlife and environmental dangers may broaden the setting later.

The player is a vulnerable survivor. **Explore, assess threats, use cover and stealth, scavenge, fight only when worthwhile, cope with injuries and continue surviving.** The world should feel inhabited and dangerous even when the player is not in combat. A believable convenience store, a dim road or an unusual silhouette through the haze can matter as much as a dramatic encounter.

### Design pillars

- **Threat awareness and survival decisions:** Visibility, noise, distance, line of sight, movement stance, ammunition, injuries and escape routes should influence decisions. Avoid making every encounter a straightforward firefight.
- **First-person presence and physical space:** Eye-level exploration, readable threats, responsive movement, corner peeking, weapon handling, doors, interiors and vertical spaces should feel convincing in FPS. The secondary top-down view retains its camera obstruction fading, angled readability and aiming adapters over the same 3D simulation.
- **Distinct threat types:** Shamblers, runners, major machines and eventually other species should present meaningfully different behavior. Anatomy and injury effects should change what an entity can still do, not just reduce a health bar.
- **A lived-in, authored world:** Start with a convincing town and nearby wilderness. Reuse procedural generation for scenery/variants, but keep important streets, interiors, landmarks and encounter positions intentionally authored.
- **Atmosphere without disguising technical problems:** Eerie blue-grey mist, natural terrain, night lighting, weather and distant silhouettes are central to the mood. Fog is visual atmosphere, not a substitute for correct culling, collision or streaming.
- **Achievable and scalable:** Solo-developer-friendly, modular C#, configurable resources, replaceable art and performance-first activation/sleep. Hordes should be possible without giving every idle actor an unnecessary every-frame workload.

### Intended game loop (direction, not a completion claim)

Explore an area → identify opportunities and risks → approach quietly, investigate or avoid → collect useful supplies / manage equipment → escape or fight → handle damage, resources and consequences → decide where to go next.

The currently playable prototype tests only parts of this loop. **Complete looting, usable medical supplies, wider survival needs, persistence, vehicle driving, crafting and base-building are not shipped features.** Do not silently add them because they appear in this vision.

## Potential long-term gameplay direction — exploratory, not approved scope

> **Design discussion as of 2026-10-29.** This describes an appealing **possible direction**, not a final genre decision, implementation approval, feature commitment or claim that these systems exist. Preserve the current gameplay foundation and the FPS-first/shared-top-down architecture. Future assistants should distinguish these ideas from the implemented-source snapshot below and should not silently implement them.

War Machine may evolve beyond a zombie-focused survival prototype into an **atmospheric, semi-realistic, open-world survival and anomalous-exploration sandbox**. Zombies remain a straightforward threat in abandoned settlements, but are **not necessarily the game's main identity**. Forest wildlife (deer, bears and potentially predators), autonomous machines, flying surveillance drones, and unexplained/alien phenomena could make the broader world dangerous and unpredictable. Prefer believable reactions, limited shared perception and species-specific behaviour over assuming every entity always attacks. Visual direction favors grounded *stylized/semi-realistic* coherence and good performance, not obligatory photorealism; see the [visual quality roadmap](VISUAL_QUALITY_ROADMAP.md).

**Possible opening:** Begin as an ordinary survivor in or near a quiet, abandoned house/town, with little information and no imposed checklist. The player might shelter inside for days, ration supplies, observe hazards and gradually scavenge nearby. Alternatively, they might venture into wilderness immediately. Discovery should arise from exploration, environmental clues, radio signals and player curiosity rather than an intrusive quest chain.

**Emergent survival and logistics:** Supplies have meaningful volume, weight and storage implications. Bags, water jugs and equipment may need to be carried in multiple trips to a recovered vehicle. A car could become a mobile cache but require fuel, maintenance, inspectable/repairable parts and careful load choices; breaking down far from shelter creates a survival situation. Players may instead establish camps and live off the land. Avoid arbitrary tedium (e.g., transferring dozens of identical items individually) by supporting containers and sensible bulk handling. Vehicle simulation, physical carrying and persistent storage are **prospective**, not currently complete.

**One coherent resource economy, several routes:** Towns provide finished goods, food, medicine and salvage; wilderness provides renewable water, wood, hunting and forage; mines/caves provide bulk raw materials and discoveries; industrial sites provide machinery and components; anomalous areas provide rare scientific/alien opportunities. Scavenging ready-made parts should remain valuable alongside mining, metallurgy, repairing and manufacturing. Do not force a modern survivor to smelt iron for trivial items readily salvaged from existing infrastructure. Mining should offer self-sufficiency, scale, special deposits and access to unusual places, **not** an unrelated mandatory crafting ladder. Prototype a small scavenge → gather/salvage → repair/craft loop before expanding to deep crafting or editable voxel terrain.

**Player progression is primarily capability and access:** Survive → establish shelter/supplies → become mobile or self-sufficient → explore farther → investigate anomalies → influence the world. Better tools, knowledge, equipment, transport, repairs and environmental protection should unlock new possibilities rather than depending entirely on character levels or stronger guns. Optional late-game objectives might include restoring communications, accessing a sealed research facility, investigating deep anomalous structures, or containing/understanding the cause of the world's changes. Aim for meaningful mystery and possible endgame outcomes **without requiring a linear campaign or ending the sandbox save**. The catastrophe's exact explanation remains undecided.

**Implementation philosophy if this direction is chosen:** Codex/Work handles carefully scoped, cross-system foundations; smaller isolated additions can then expand validated interfaces without rewriting gameplay. Reuse existing sensing, anatomy, interactions, inventory, rendering and streaming owners—**do not introduce competing universal systems**. Start with concrete slices (e.g., convincing deer → defensive bear; basic hovering drone; portable container → vehicle storage; authored cave resource deposit → one repair recipe; one mysterious discoverable site). Keep inactive entities dormant and distant populations inexpensive; integrate world changes with planned WS3 persistence and WS4 relevance before claiming fully simulated open-world behaviour. No automatic implementation of these ideas.

## Which scene is the actual game?

| Scene | Purpose | Run |
| --- | --- | --- |
| [WORLD/World.tscn](WORLD/World.tscn) | **Shared gameplay world / secondary top-down view:** player, village, terrain, zombies, robot, combat, HUD and environment | Open and **F6 / Run Current Scene** |
| [WORLD/FirstPerson/FirstPersonWorld.tscn](WORLD/FirstPerson/FirstPersonWorld.tscn) | **Primary FPS gameplay entry** inheriting the shared world, with view-specific controls, weapon presentation, head bob, leaning, fog and grass configuration | Open and **F6** |
| [WORLD/testworld.tscn](WORLD/testworld.tscn) | **Parked legacy wilderness/free-flight experiment**, retained with its terrain, caves and original environment work | Currently launched by **F5**, per `project.godot` |

**Do not mistake F5 for the survival game.** The startup setting intentionally still points to the legacy `testworld.tscn`; changing it is a separate authorized decision. Use `FirstPersonWorld.tscn` as the primary gameplay acceptance scene. Build shared gameplay through `World.tscn` and its feature owners; use its top-down view for relevant compatibility checks.

The two active views share terrain, buildings, entities, health/damage, inventory, weather and static streaming. Feature-specific FPS adapters live in their owning folders; see [FPS ownership](WORLD/FirstPerson/README.md). FPS-first is a design priority, not a request to duplicate the world, rename shared base classes or change startup configuration.

## Current implemented source — snapshot

**Status language:** *Implemented in source* means code/assets are present, **not** that the current branch compiled, rendered correctly, passed local playtesting or demonstrated a performance improvement. Some earlier milestones have owner-confirmed tests; newer work below is generally **awaiting local validation**. Plans retain their own explicit phase states.

| Area | Source-implemented foundation | Not yet complete / important boundary |
| --- | --- | --- |
| **World and locations** | Saved Terrain3D gameplay area, village/road layout, graded sites, convenience store, reusable Building001–004 and a two-storey police-station prototype, parked cars, GLB household/street objects, furniture/hedges/lamps and shared swing-door helpers, cliff/rock/forest content | Prototype region, not a populated persistent large world; advanced town and terrain authoring still in phases |
| **Player and cameras** | Shared soldier/player, walk/run/crouch/prone, collision and jump, procedural pose; top-down follow/aim/zoom/occlusion; FPS look, body adapter, distance-driven head bob and render-smoothed held lean/corner peek | Prone uses a low proxy capsule; realistic injury movement penalties and detailed player hurtboxes are future work |
| **Player condition** | PH0/PH1 hidden vitality, six body-condition regions, death/control gate and body silhouette; PH2 stamina for sprint/jump/accepted shots with delayed recovery and temporary bottom-right meter (awaiting local validation) | Medical treatment, bleeding, damage-dependent limb impairment and broader survival metabolism not yet implemented |
| **Firearms** | Rifle_001 and MachineGun_001, hitscan, shared damage/obstruction, shot spread, recoil/aim climb, muzzle feedback, ammo magazines, timed R reload and ammo HUD | Physical ammunition pickup/economy, reload animations and a full firearm/inventory interaction loop remain unfinished |
| **Melee and impact** | Q quick shove, equipped slot-3 baseball bat with active horizontal multi-target swing, bullet flinch, collision-aware zombie shove/bat backsteps and CM5 standing zombie strikes (awaiting local validation) | Knife/other melee families, ELD3 injury-based attacks and crawling attacks remain future work |
| **Zombies and perception** | Standard shamblers and runner variant, wandering, frontal initial detection, head/torso cover rays, pursuit, alerted tracking independent of facing once acquired, last-seen and bullet-hit fixed-point investigation | HE1 uncertain gunshot investigation with distance-based estimates/commitment and one blocker ray for shorter blocked searches (awaiting local validation); no obstacle-route navigation |
| **Anatomy and injury** | Shared region-aware hits, condition/sever states, missing-limb visuals, limp/hop/crawl/immobilize policies, injury-aware corpse handoff | No full attack-capability filtering or detached limb debris |
| **Hitbox performance** | **ELD-PERF1:** one broad native anatomy shape per zombie by default; virtual joint-local boxes tested when firearm/bat queries reach the candidate. Includes bounded ray budgets and diagnostic counters; compatible same-pivot zombie visuals also use shared batched templates (ZM-PERF2A) and native lower-polygon mesh LOD (ZM-PERF2B; triangle cost only) | Correctness and measured FPS gain require local A/B profiling; do not report performance as proven |
| **Corpses** | ZD0–ZD4 transfer original injury-preserving art; nearby budgeted deaths use connected torso/head/limb physics for up to 4s, then bake the pose; soft piles, aging/sinking and retained/active caps remain (awaiting local validation) | Player slowdown is opt-in off; distant/overflow deaths use a cheap static fallback; imported skeleton adapters and persistence remain later work |
| **Robots and wildlife** | Robot_001 patrol/scanning prototype; an optional stationary deer preview with a reusable creature/health foundation | Robot combat/strategic AI and wildlife populations/behavior are not yet implemented |
| **Interactions** | IX0 hold-E/LMB targeting, shared reach/solid cover, contextual prompt and optional nearby test switch; source implemented, awaiting local validation | Carrying, cupboards, pickups, item identity and persistence remain later IX phases |
| **Inventory, UI and audio** | PlayerBackpack stack storage and backpack UI, shared hotbar, F1 visual-only placeable spawner, NPC health bars, body/ammo HUD, reusable weapon sound and WorldAudio foundation | World pickups, usable items and integrated loot/ammo storage are unfinished; HE1 gunshot events alert zombies independently of actual audio playback |
| **Environment and scale** | Sky3D day/night, weather, grass/pines/rocks, haze and building exclusions, scheduled streetlamp cones, distance/occlusion fog; render-range policy and WS2 static-building streaming prototype | Broader actor/terrain streaming, persistent state and full-world population budgets are not implemented |

### Current development emphasis

Recent commits focus on making close encounters and authored places feel physical: smoother FPS leaning and a larger lateral corner peek; FPS firearm/bat presentation; speed-sensitive swinging doors; a two-storey police-station shell/interior with stairs and ceiling lights; reusable GLB household/street objects; and shared static collision/shadow controls. Shared threat work adds telegraphed zombie strikes with torso/head lunges, uncertain gunshot investigation, injury-aware movement and bounded corpse piles/cleanup. Zombie visual batching, on-demand limb hit refinement and render diagnostics support horde performance investigation.

These are prototype foundations, not proof of a complete survival loop or measured optimization. Prioritize eye-level movement, aiming, cover, interior traversal, threat readability and encounter feel in FPS while preserving shared simulation and the existing secondary view.

### Current small-area testbed

The [ConvenienceStoreSite001](WORLD/Content/Assets/Sites/ConvenienceStoreSite001/README.md) is a useful shared-world acceptance location. Current authored source has a **24 × 16 m store**, **56 × 36 m asphalt**, **42 marked bays in three rows**, **seven parked cars**, **eight night-scheduled streetlamps** and hedges/access paths. Building001–004 and a robot patrol occupy the surrounding village.

The **store zombie stress option** currently requests **16 extra shamblers + four extra runners** beyond the nominal village nine shamblers + one runner. `StoreStressZombiesEnabled` is on in `WORLD/WorldSettings.cs` unless overridden in the world master. Terrain/obstruction/spawn-clearance rules may skip some candidates; inspect the actual accepted-spawn log. This is a repeatable **population/performance test**, not a finished spawn director.

The reusable F1 placeable catalogue previews eligible visual asset wrappers; it is **not** a complete persistent in-world placement or physics-item system. Static world streaming currently covers the four integrated village buildings and a separate proof; the store site and many other world systems remain resident.

## Gameplay system boundaries

### AI, combat, anatomy, corpses

Keep the responsibilities separate:

1. **Perception and investigation** recognize visible targets or verified clues. Initial zombie sight uses a frontal cone; alerted sight can track from behind when line of sight stays clear. Last-seen and direct bullet-hit investigation store a fixed point, not live omniscient tracking. HE1 receives HE0 gunshot events and investigates a single uncertain estimate, with distance-based commitment and burst retarget limits. One blocker ray biases obstructed sound clues toward earlier abandonment and shorter timeouts; it does not count walls or find routes. Local validation remains pending.
2. **Combat** determines hit contact, cover, damage packets, recoil, reactions and attack windows. The existing shared damage/impact resolution is the authority; do not create parallel pipelines for each view/weapon.
3. **Anatomy** records region condition, capability changes and severing independently of overall vitality. Deferred broad candidate + virtual per-limb refinement is the current default, not eight constantly synchronized native shapes per moving zombie.
4. **Locomotion/animation** interpret injury state using the existing actor movement callback and rig; they do not run duplicate AI loops. Crawling and hopping are first-pass behaviors, not final realistic biomechanics.
5. **Corpses** receive the existing visuals and injury pose after death; their bounded fall and settled state are separate from a living entity. Nearby budgeted zombie deaths now use short-lived articulated physics over the procedural rig, then bake the actual limb pose into dormant art/static pile shapes; distant/overflow deaths keep the cheap fallback.

**Current limitations:** Standing zombies can damage the player through CM5 telegraphed melee, including committed misses/recovery and a coordinated torso/head lunge. Low-body/immobilized zombies do not attack in this pass; full injury-based attack selection (ELD3), later anatomy phases and robust obstacle pathfinding remain future work. HE1 hearing and ZD1–ZD3 corpse handling are implemented in source, awaiting local validation; their presence does not authorize later phases.

### Views and assets

FPS is the primary reference for new gameplay feel, balance and usability; top-down remains a secondary supported view. Preserve existing shared behavior and check relevant compatibility without assuming equal view-specific polish or automatically implementing every feature twice. Gameplay rules should live with shared PLAYER, ENTITIES, EQUIPMENT, COMBAT and WORLD owners. Top-down and FPS may differ in input, camera, screen-space aiming, recoil/animation presentation and visibility, but not silently fork health, zombie AI, inventory or world state.

World content is built from reusable scenes. For generated art, **`C/` contains C# mesh construction** and **`Artwork/` holds future imported GLB/texture replacements**. Stable scene/gameplay contracts must survive visual swaps. Separate cosmetic appearance, physics/collision, interactability and persistent identity; one does not imply the others.

### Performance and future world size

Favor **inactive means dormant**: movement/AI work only when needed, low-rate/staggered decision updates, explicit wake/sleep, bounded physics queries, event-driven UI, pooled/shared geometry, GPU foliage batches and render distances appropriate to the current view. A visual fog overlay does not suspend invisible actors.

World direction is a **large finite, expandable and partly authored town/wilderness**, not a fully allocated infinite sandbox. WS0 section identity, WS1 fog-aware render policy and WS2 bounded static-building load/unload are in source. **WS3 save round-trip, WS4 mobile actor ownership/relevance, WS5 authoring and WS6/WS7 larger populated regions are still planned.** Grow only after validating representative encounter and streaming budgets; don't overwrite saved terrain while exploring or adjusting gameplay.

**New building workflow:** read the [architectural design guide](WORLD/Content/Assets/Structures/BUILDING_DESIGN_GUIDE.md) first (PoliceStation001 lessons, safe stairs, floor/partition geometry), followed by the [technical building authoring contract](WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md) (standalone editor preview, collisions, ground haze exclusions, top-down fades/FPS, aim mapping and performance). Both apply to every new building.

## Project ownership map

| Folder | Responsibility |
| --- | --- |
| [WORLD](WORLD/README.md) | Primary scene/composition, gameplay terrain, village layout, reusable structures/props, static streaming, render policy; legacy environment scene retained |
| [PLAYER](PLAYER/README.md) | Shared player lifecycle, input/movement/stances, visuals, vitals and player equipment adapters |
| [CAMERA](CAMERA/README.md) | Primary FPS camera, head bob and lean; secondary follow/zoom camera and obstruction fading |
| [ENTITIES](ENTITIES/README.md) | Shared actors, sensing/investigation, health/anatomy/corpses, zombies, robot and wildlife |
| [COMBAT](COMBAT/README.md) | Shared impact/contact execution, melee and reactions; general health authority remains in ENTITIES |
| [EQUIPMENT](EQUIPMENT/README.md) | Firearms/melee equipment, stats, ammo/recoil, weapon slots and view-specific visuals |
| [INTERACTION](INTERACTION/README.md) | IX0 shared targeting/actions, view/input adapters and physical-contact helpers; staged carry/throw, cupboards, destruction, world pickup/placement and scavenging |
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
2. Run `dotnet build war-machine.csproj` when available. For primary gameplay use **F6** on [WORLD/FirstPerson/FirstPersonWorld.tscn](WORLD/FirstPerson/FirstPersonWorld.tscn); use **F6** on [WORLD/World.tscn](WORLD/World.tscn) for the secondary top-down view. **F5 still opens the legacy scene.**
3. The shared player code currently binds **arrow keys for movement, Shift to run, Right Ctrl to crouch, `/` to toggle prone and RMB to jump**. Mouse aims in top-down and looks in FPS; LMB fires/uses equipped primary attack, **Q** shoves, **R** reloads and number keys select equipped slots. Hold **Numpad 1 / Numpad 2** to lean left/right; FPS rolls/shifts the eye camera while top-down leans the soldier only. Escape releases FPS mouse capture. Check [current input source](PLAYER/Input/PlayerInput.cs): some older feature docs describe different crouch/prone bindings.
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

## Visual quality direction

See the root [Visual Quality Roadmap](VISUAL_QUALITY_ROADMAP.md) for the **high-level FPS art and graphics direction**: one polished woodland/roadside benchmark first, then vegetation, terrain materials, lighting, props and performance-aware expansion. It is a **vision/priority outline**, not approval to implement; detailed DF, SF and environment work remains in the owning feature plans.

**North star:** *Build an atmospheric, replayable survival experience where an eerie lived-in Earth, dangerous encounters, physical cover and meaningful injury/resource decisions make exploration worth the risk—without losing sight of scope or performance.*
