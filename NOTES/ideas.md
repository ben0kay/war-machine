# War Machine — Ideas Bank

> **Brainstorming / wishlist, NOT an implementation backlog.** Being listed here does not authorize Codex to build anything. Promote ideas to a separate feature plan only when explicitly requested.
>
> **Game direction:** Earth-like, fully 3D, angled top-down/isometric-style wilderness survival with stylized/painterly graphics (roughly *The Long Dark* in visual ambition rather than photorealism). A balanced eventual mix of **survival, combat and base-building**. Familiar forests, animals and abandoned human places should dominate; advanced machines provide the unsettling sci-fi contrast.
>
> **Current priority:** Get player movement, camera and the enormous robot's movement/patrol/scan feeling good in `WORLD/World.tscn`. A basic zombie wander/chase prototype is already present; **robot-vs-zombie combat and the old ZS sandbox plan are not active tasks**. Legacy first-person work is parked.

## MAJOR feature ideas

These are potential gameplay pillars or substantial systems, not tasks to start immediately.

1. **Earth-like open wilderness.** Forests, hills, rivers, lakes, fields, rural roads, farms, towns, mines and caves with readable terrain, useful landmarks and a world worth exploring.
2. **Wildlife and a functioning natural ecosystem.** Deer, rabbits, fish, birds, wolves and possibly bears. Animals wander, forage, flee danger, migrate or hunt one another, creating believable encounters even when the player is absent.
3. **Hunting, fishing and foraging.** Track and hunt deer for food/hides, fish streams, gather plants and find drinkable water. Tools and weather influence outcomes. Keep survival engaging rather than a constant chore.
4. **Survival and medical needs.** A restrained set of food, hydration, warmth, fatigue, injuries and infection systems; shelter, rest and sensible preparation matter more than micromanaging dozens of meters.
5. **Zombies as recurring nuisances.** Wanderers around buildings and roads, occasional loose groups and hordes, noise-based attraction, pursuit, and environmental persistence. Most are manageable alone but dangerous when they interfere with hunting, scavenging or stealth.
6. **Giant robots as the superior threat.** Powerful walkers that patrol, scan and enforce territories. Cyan searching, then eventual red detection/lock, deliberate aiming and devastating machine-gun or missile attacks. A healthy giant robot should dominate ordinary zombies; most players initially avoid rather than fight it.
7. **A living interaction ecosystem.** Zombies chase people, disturb animals and accidentally trigger robot scans. Robots may obliterate zombie groups, ignore deer, secure facilities or investigate loud disturbances. The player can deliberately exploit these interactions to escape.
8. **Stealth, sound and machine detection.** Crouching, terrain cover, darkness, line of sight, gunshot noise, generators, vehicle engines, alarms and robotic search behaviour. Robots and zombies should react differently to the same disturbance.
9. **Scavenging and expedition loop.** Start at a shelter, venture toward an abandoned store, crash site or communications facility, weigh risk against supplies, then return to recover, craft and prepare for longer trips.
10. **Building and maintaining a survivor base.** Cabins, storage, workbenches, generators, solar power, fences, traps, watchtowers, lighting and defenses. Construction, noise and power signatures might attract machine attention, creating a reason to balance safety with concealment.
11. **Weapons, tools and gear progression.** Start with ordinary Earth equipment—rifles, shotguns, bows, axes, hunting knives and repair tools. Expand into traps, stronger firearms, protective equipment and rare recovered technology. Keep shared systems independent of individual item art.
12. **Robot salvage and recovered technology.** Discover broken or dormant walkers and automated facilities; recover batteries, alloys, optics, actuators and circuits. Use these for better tools, power and eventual anti-machine equipment. Some wrecks remain dangerous.
13. **Automated sci-fi infrastructure.** Active surveillance drones, robot maintenance depots, communications towers, restricted military checkpoints, robotic factories, abandoned laboratories and sealed underground installations embedded in otherwise ordinary countryside.
14. **Persistent, explorable world.** World state retains scavenged containers, cleared or occupied buildings, modified bases, felled resources and notable destroyed machines. Later: region streaming and distant-simulation rules without costly always-active AI.
15. **Mystery and environmental storytelling.** Why are machines still operating? Possible background: human-built autonomous quarantine systems deployed during an outbreak, whose command network failed. Discover fragmented radio logs, old orders and research sites; **keep the robots' origin unresolved until the setting is chosen**.
16. **Weather, seasons and atmosphere affecting play.** Day/night, mist, rain, snow, cold, muddy tracks, visibility, animal behaviour and detection ranges. The painterly countryside should stay readable from the high camera.
17. **Rare futuristic hazards and discoveries.** Abandoned experiments, electrical disturbances, signal dead zones or strange materials, used sparingly so the setting doesn't become a neon sci-fi theme park.
18. **Optional other survivors and human threats.** Isolated survivors, traders, rival scavengers or settlements. Human encounters can create dilemmas beyond zombies and robots; avoid adding complex NPC societies until the core game works.

## MINOR feature ideas

Smaller interactions, atmosphere, polish and memorable encounters that could support the larger pillars.

### Robots and sci-fi details

- **Readable scanner states:** cyan cone sweeps while searching; eventual sharp red alert when a valid target is confirmed; head follows target before firing.
- **Robot-versus-zombie spectacle:** machine-gun tracers, a missile on tougher targets, brief impact flashes and a lightweight stylized zombie disintegration/splat effect.
- **Heavy walker presence:** booming mechanical footfalls, subtle camera tremor nearby, leg servos, soil compression and disturbed leaves.
- **Patrol personality:** a robot stops, rechecks a suspicious noise, tilts its head, waits, then resumes its programmed route.
- **Small drones:** a quiet reconnaissance drone flies overhead and can radio a much larger patrol.
- **Robot role differences:** civilian maintenance units, sentries, heavy walkers and rare specialized machines, each with different priorities.
- **Rules of engagement:** most robots ignore normal wildlife; some attack zombies automatically; restricted-zone sentries treat intruders more aggressively.
- **Electronic warnings:** a salvaged handheld radio clicks or emits static when nearby machine sensors become active.
- **Damaged machines:** sparks, intermittent scanners, a limp or missing armor plate; a wreck might reactivate if disturbed.
- **Robot tracks:** huge footprints in mud or snow help the player infer recent patrol routes.
- **Network towers:** powering down or disrupting a relay briefly alters local patrol coverage.
- **Faraway sci-fi hints:** occasional orbital flashes, contrails from autonomous aircraft or a distant silent object overhead.

### Wildlife and nature

- **Deer spook response:** gunfire, robots or zombies scatter a herd and interrupt a hunt.
- **Predator/prey encounters:** wolves stalking deer, scavengers gathering around a carcass, birds startled by the player.
- **Tracking clues:** hoofprints, droppings, broken branches, feeding marks and distant animal calls.
- **Fishing spots:** river pools and lakes yield different catches depending on weather and time of day.
- **Wildlife products:** meat, hides, antlers, feathers or bones that support practical crafting.
- **Changing landscape sound:** insects at dusk, birds at dawn, wind through pines and sudden silence when a patrol passes.
- **Carcasses:** attract scavengers or zombies; decay over time rather than disappearing instantly.

### Zombies and tactical surprises

- **Zombie vs deer:** an infected wanderer startles an animal and unintentionally betrays its own position.
- **Sounds draw crowds:** gunfire, car alarms, breaking windows, machinery and explosions lure zombies from nearby areas.
- **Distract a robot with zombies:** escape while a robot scans or destroys a group—without scripting a guaranteed safe outcome.
- **A few distinctive zombie types:** slow shamblers, faster recently infected, or quieter lurkers; avoid dozens of arbitrary special monsters.
- **Doors and windows:** zombies bang on doors, stumble through open gates or get trapped behind fences.
- **Occasional herds:** small ambient groups move along roads or through clearings, rather than constant large-scale waves.
- **Threat traces:** distant gunfire, wrecked barricades or scattered destroyed zombies hint at events beyond the camera view.

### Survival, exploration and bases

- **Scavenging props:** abandoned vehicles, tool sheds, hunting stands, campsites, field shelters and supply caches.
- **Radio fragments:** emergency broadcasts or automated notices that reveal possible locations and atmosphere.
- **Portable distractions:** alarm clocks, thrown objects, remote noise makers or car horns, affecting zombies and machines differently.
- **Low-tech alternatives:** rain collection barrels, campfires, wood stoves and wind-up tools when electrical devices are risky.
- **Power management:** generators are effective but loud; solar and scavenged batteries are quieter but limited.
- **Hidden base trade-offs:** visible floodlights improve defense but make an outpost easier for drones to spot.
- **Handmade traps:** snares for small game, tripwire alarms, noise decoys and basic perimeter warnings.
- **Navigation tools:** paper maps, binoculars, compass, route markers and eventually a salvaged sensor display.
- **Sealed locations:** electronic locks, power-dependent doors and safe rooms encourage a return trip after upgrading gear.
- **Distinct loot consequences:** carrying fresh meat could attract wildlife; carrying a heavy robot component slows your escape.
- **Weather feedback:** wet clothing, muddy footsteps, snow tracks, wind gusts and thunder masking noises.
- **Abandoned life:** family photos, old roadside signs and ordinary household clutter emphasize that this was once normal Earth.
- **Safe exploration moments:** sunsets, deer in a clearing, a sheltered cabin during rain—quietness makes robot appearances dramatic.

## Ideas to preserve without committing to them

- **Open question:** Are the robots human-made containment/military machines, alien technology, or something else? The quarantine explanation is a possible story, not confirmed canon.
- **Visual rule:** Most of the world should look like recognizable Earth. Advanced machines and rare facilities stand out because they are unusual; avoid covering everything in glowing technology.
- **Threat rule:** A small zombie group is dangerous to a survivor but not to a healthy giant robot. Robots need not be hostile to every living thing.
- **Performance rule:** Prefer small active populations, distance-based relevance, grouped/staggered checks, pooled effects and dormant systems. Large hordes and full-world simulation come much later, if ever.
- **Development rule:** Ideas are separate from authorized tasks. Current focus remains **player/camera and robot mechanics**. Expand only when explicitly requested.

