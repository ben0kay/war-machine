# IX — Physical interactions, persistent items and scavenging

> **Status: IX0–IX8 Planned; DOCUMENTATION ONLY (2026-10-27).** This is a **Codex-ready master plan and design record**, not an instruction to implement every phase. No gameplay logic, scene, item system, collision settings or master resources change with this document. Explicitly authorize one bounded phase/job at a time. Work on `implement-td0-td1` unless instructed otherwise; **one GitHub commit per authorized job**, and brief proportional validation per `CodePreferences.md`.

## Phases at a glance

| Phase | Result | Dependencies | Suggested implementation assistant | Status |
| --- | --- | --- | --- | --- |
| **IX0** | Shared interactable/targeting/actions; thin top-down and FPS adapters | Existing player/camera/UI input | Codex / Work | **Planned** |
| **IX1** | Loose rigid props collide, fall, topple and sleep | IX0 helpful for debug; Godot Jolt already configured | Regular Chat for prefab/settings; Codex / Work for physics issues | **Planned** |
| **IX2** | Pick up light loose props into a **physics-held position**, move, drop and throw | IX0, IX1 | Codex / Work | **Planned** |
| **IX3** | Reusable cupboard hinged door: click toggle overhead, handle drag FPS | IX0; optional IX2 only if sharing drag controls | Codex / Work | **Planned** |
| **IX4** | Melee-breakable planks block doorway until destroyed | IX0, current damage foundation, functional **CM2 melee contact** or later CM3 | Codex / Work | **Planned** |
| **IX5** | Persistent item **identity and exclusive ownership** across World, Inventory, Equipped and PhysicsHeld | IX0; existing weapon slots audit | Codex / Work | **Planned** |
| **IX6** | Place/drop **the same rifle** on table, free hands, retrieve and re-equip | IX5, IX0; shared equipment adapters; CM quick shove optional | Codex / Work | **Planned** |
| **IX7** | Scavenge hidden ammo/food from cupboard; visibility-gated prompts and simple collection | IX0, IX3, IX5, optional IX1/IX2; future PH3 for eating effects | Codex / Work; Regular Chat for content/UI | **Planned** |
| **IX8** | Persist moved objects/door/planks/item identities; cross-view comparison and scale checks | IX1–IX7 relevant slice + approved saving architecture | Codex / Work, Copilot local tests | **Planned** |

**Status vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. Codex must re-read live code before each phase: the repository evolves concurrently. Do not infer that a plan is implemented just because its folder exists.

## 1. Vision — one physical survival world, two interaction experiences

The owner is **intentionally retaining both viewpoints** until side-by-side play makes the better long-term direction clear. The top-down 3D view affords readable movement/combat but makes giant robot scale and multi-floor scavenging harder; roof/wall fading can obscure interior objects. First-person makes towering machines and tactile scavenging more convincing but demands closer-detail models, materials, animation and atmospheric rendering. The dark, dusty/smoky alien-invasion environment and distance fog support a deliberately stylized first-person presentation. **Do not choose a camera mode on behalf of the owner, delete the FPS experiment, fork content or design two different worlds.**

**Desired kitchen encounter (vertical-slice acceptance):** walk into a house, break boards off a blocked doorway using melee, put the equipped rifle carefully on a table (it leaves equipped ownership), open a hinged cupboard, push/lift loose books hiding food or ammunition, collect and possibly consume an item, notice a zombie approaching, use **unarmed quick shove/punch** while the gun is unavailable, recover the *same* rifle with its state intact, equip and shoot. The same physical state should be accessible from overhead and FPS; only targeting/input and animation presentation differ. Player-versus-zombie outgoing attacks require the separately planned COMBAT/PH health phases; the whole narrative is **not** an IX0 acceptance test.

**Carry a broom or bucket without bespoke animations:** lightweight generic objects may simply float in a **physics-controlled carry hold-point** in front of the character, like a Half-Life-style object grab; it remains a world object with collider/physics, **not** an equipped weapon and **not** automatically added to inventory. Release to drop; optional impulse to throw. A bucket may eventually hold contents but no fluid-container implementation is part of this plan. A broom can be physically lifted and thrown even if it has no actual gameplay use. A rifle is different: it has an authored equipped presentation/weapon behaviour and belongs in hands while equipped.

### Interaction examples and categories

| Thing | Allowed first-pass operations | Representation / persistence |
| --- | --- | --- |
| Books, bottles, loose box, bucket, broom | Walk-bump; physics grab/move/release/throw if within weight/reach limits | `RigidBody3D`, asleep at rest; if persistent, transform and sleep state |
| Big fixed desk, wall, large cupboard frame | Remain static | Static collision; no simulation |
| Hinged cupboard door | Open/close in overhead; precise angular drag in FPS | Shared authored pivot/state/limits, one door angle |
| Wood plank nailed over doorway | Solid blocker; melee damage; destroyed/debris visual | Reuse damage contract and break state |
| Rifle | Equipped → dropped or placed on table → picked up and equipped/inventory | **One item instance**, owner/state transition; not a copied gun |
| Ammunition, food | Visible/reachable pickup into an item collection; future consume/use | Item instance / stack, no permanent loose physics required |
| Zombie | Reacts to attacks using COMBAT contracts | Creature-owned movement/health/reaction, **not** a generic prop |

## 2. Verified current implementation and gaps (2026-10-27 snapshot)

- `project.godot` selects **Jolt Physics** for 3D, with Godot 4.7 C# project features. Godot's `RigidBody3D` and physics sleep are enough for light props, and `CharacterBody3D`/player collisions already exist. **No new physics engine required**. Mass, collision layer/mask, friction, damping and impulses need actual Godot tuning.
- `WORLD/World.tscn` is the active F6 top-down prototype. `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` **inherits the same world** with a distinct player/eye-camera and weapon-input adapter. `WORLD/WorldController.cs` initializes terrain, props, enemies, equipment, sky/weather and scene roots. `WORLD/Content/Objects/` remains the legacy Car001 location. `WORLD/Content/Placeables/Objects/` and `Props/` now include mesh-only Bookcase001 and Book001 GLB designs; no rigid-body prefab exists yet. Current procedural `C/` contents build meshes; `Artwork/` is for replacements.
- `PLAYER/PlayerController.cs` owns `CharacterBody3D` motion, stance, procedural rig and weapon mount; `PLAYER/Equipment/PlayerWeaponController.cs` consumes mouse aim/fire. FPS uses `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs`, with mouse capture and centre-screen ray. Left mouse **already fires**, so interaction input **must arbitrate and consume events** rather than double-activating gunfire. Debug panels/GUI retain first refusal. The top-down roof/wall **visibility fade must not bypass reach or solid-world occlusion**.
- `EQUIPMENT/Shared/WeaponSlots.cs` is currently a **two-firearm test switching mechanism** with lazy per-slot `FirearmController` scene instances (keys 1/2). It is **not an inventory**, does not support a rifle physically dropped/removed from its owner, and does not represent unique magazines, ammunition, condition, ownership or a nine-slot item model. The nine-slot `UI/GameplayHUD/Hotbar` is **visual only**; don't assume keys 3–9 are item slots.
- `ENTITIES/Shared/Combat/` already owns `DamagePacket`, `IDamageReceiver`, `HealthComponent` and damage resolution. `COMBAT/` has **CM0/CM1 impact and bullet reaction implementations** and **CM2-B zombie shove-response code awaiting validation**, while universal quick melee contact/input (the other CM2 stages), bat/knife and zombie outgoing melee remain separate roadmap work. Reuse this; don't create a second impact/damage resolver for breakable boards. See `COMBAT/IMPLEMENTATION_PLAN.md` and re-check current status before IX4/IX6.
- `PLAYER/IMPLEMENTATION_PLAN.md`: PH0 player health and PH3 hunger/thirst are still planned in this snapshot. Do not make a canned food pickup silently reduce hunger before a real survival interface exists.
- **No INTERACTION/ or INVENTORY/ runtime system, physics carry, persistent item identity, breakable doorway plank, hinged cupboard, pickup/consumption, or save integration was found in the reviewed code.** Avoid presenting these as existing features.

## 3. Folder ownership and proposed implementation seams

```text
INTERACTION/                                # NEW documentation owner now
  README.md
  IMPLEMENTATION_PLAN.md
  Shared/                                   # IX0+ only
    IInteractable.cs                        # proposed small query/use contract
    InteractionAction.cs                    # proposed action kind/context value type
    InteractionTargeting.cs                 # proposed ray & real-reach/LOS validation
  ViewAdapters/                             # IX0+: NO separate world simulation
    TopDownInteraction.cs                   # mouse target ray, world physical reach checks
    FirstPersonInteraction.cs               # centre-screen aim, drag input/mouse capture
  Physics/                                  # IX1/IX2: generic held-body and impulse logic
    PhysicsCarry.cs                         # one active held body; force/velocity limited
  Doors/                                    # IX3: reusable hinged-door state/drive
    HingedDoor.cs
  Destruction/                              # IX4: breakable adapter to existing damage API
    BreakableObject.cs
  Configuration/                            # Create IX0/IX1 when useful, not this job
    InteractionSettings.cs                  # proposed typed Resource; one master file
    InteractionMaster.tres                  # ranges, keys/limits, carry mass, safety caps
INVENTORY/                                  # CREATE WITH IX5, not now
  README.md
  Shared/
    ItemDefinition.cs                       # static type, mesh/icon, bulk, can-store/can-equip
    ItemInstance.cs                         # persistent unique identity, mutable per-instance data
    ItemOwnership.cs                        # exclusive transitions; no copy-on-pickup
  Configuration/                             # IX5 when item defaults actually exist
    InventoryMaster.tres
EQUIPMENT/                                  # EXISTING, adapt with IX5/IX6
  Shared/WeaponSlots.cs                     # two-gun adapter; bridge, don't copy/replace blindly
  Weapons/Shared/FirearmController.cs       # existing firearm behaviour
PLAYER/Equipment/PlayerWeaponController.cs  # EXISTING overhead input adapter
DEBUG/TEST/FirstPerson/EQUIPMENT/           # EXISTING FPS input adapter
WORLD/Content/Placeables/
  Objects/                                  # fixed model-only Bookcase001 exists
  Props/                                    # Book001 GLB exists; IX1/IX2 still planned
    Shared/                                 # future PhysicsProp.cs / config
    Books/                                  # Book001 model only, no physics yet
    Boxes/ Household/                       # create only with real prefabs
WORLD/Content/Structures/                   # existing building owners
  ...                                       # optional scene-owned cabinets/doorways
COMBAT/                                     # EXISTING impacts + planned melee; do not duplicate
UI/GameplayHUD/Hotbar/                      # EXISTING visual-only scene, later event-bound
```

All names above are **candidate implementations**; keep the simplest contract that works. No generic global interaction singleton, no generic status-effect framework, and do not create unused folders/classes before their respective phases. Respect `C/` = **procedural mesh generation only**, and `Artwork/` = future imported art.

### Core separation and object state machine

**Item definition** (what a rifle/bucket/can *is*) is immutable authoring data; **item instance** (which exact rifle, ammo count when implemented, durability, custom modifications, etc.) is mutable game state. The item instance is *owned exactly once*:

```text
WORLD           -- pick up --> INVENTORY -- equip --> EQUIPPED
  ^                  ^                         |
  |                  |                         |
  +----- drop/place -+<------- unequip --------+
```

`PHYSICS_HELD` is a **temporary grip mode for a world prop**, not a second inventory entry. For a generic broom, WORLD ↔ PHYSICS_HELD ↔ WORLD; it can remain non-inventory entirely. For a bucket, author `CanPhysicsCarry=true` initially, `CanStoreInInventory=false` initially, and later let a container mechanic be added deliberately. For a weapon, default pickup is an inventory/equip transfer with a corresponding hands/view model; dropping or careful placement creates/activates one world representation. Never keep both the equipped gun and the world gun as independently functional duplicates.

A state transition must be **atomic/rollback-safe**: validate eligibility and destination before removing prior ownership; transfer the same identifier, preserve per-instance state; spawn/enable exactly one world physics representation when needed, and despawn/disable it on pickup. Resource `.tres` definitions are immutable authoring defaults; runtime data and serialization must not mutate the master. A physics-held object remains collidable with its world, has bounded carry distance/force, and follows a target point using physics rather than direct body teleports through walls.

**Interaction vs combat:** A selected door handle opens a hinged door; a weapon strike against a boarded entrance uses the **existing damage/impact path**, not a special inventory interaction. An item pickup removes a world representation and transfers ownership; a physical grab leaves the same world object active. A heavy cabinet may be openable but not liftable. Provide explicit capability checks, not a single generic left-click handler doing all actions.

### Input and geometry principles

- **View-independent authoritative target**: 3D world contact plus actor-to-object max physical reach, floor/height separation, solid obstruction test, enabled/action eligibility. For FPS selection cast from **eye centre**; overhead cast from cursor but reject hits when the corresponding player body/hand is outside reach or a faded wall is physically between them. Also reject through ceiling/floor/faded upstairs furniture. No remotely opening cupboards through roofs.
- **Input arbitration**: do not hijack fire. Suggested consistent `E` interaction focus and `V` quick melee (already proposed in CM). When interaction focus/drag is active, **consume LMB** before it reaches gun trigger; otherwise LMB remains primary firearm/melee attack. Overhead `E` or E+click can open a targeted door and interact with pickups. FPS `E` to engage focus, then hold LMB and move mouse to control the handle; Esc/focus-loss cancels; unmodified LMB continues firing. Final key mapping is an owner-tunable design choice.
- **FPS handle drag**: translate relative mouse delta into requested hinge angular movement with clamped angular speed/limits and optional damping/resistance. Mouse stays captured for look normally; when dragging, temporarily direct mouse motion to interaction instead of camera look, and restore capture/look on release/cancel. Do not require one hand animation per object.
- **Top-down click**: same hinge angle/state, but a click smoothly drives the door to an open/closed target angle; stop at limits/collision. Click target must be interactable and reachable, never a screen-space overlap-only shortcut.
- **Hinge implementation**: start with controlled pivot + physics-collidable moving door (`AnimatableBody3D`/controlled transform approach to evaluate); if physical impacts/obstructions demand a true free hinge later, test `RigidBody3D + HingeJoint3D` with torque, damping and limits. **Never manually teleport a physics-driven rigid hinge each frame**, and don't promise perfectly simulated pressure/friction before validating stability.
- **Physics-held props**: one grabbed object at a time, simple collision shapes, bounded mass (start around 0.1–5 kg as **example only**), reach/distance, applied impulse/velocity/forces, collision rejection, break-grab distance, no pickup through walls. Throw via measured capped impulse; movement and collisions can knock held object against scenery; cut off exertion if jammed rather than tunnelling objects through doors. Avoid self-collision with the player's collision body safely; on release restore masks/owner settings and simulate normally.
- **Player walking into props**: Jolt rigid physics can handle loose books/bottles, but `CharacterBody3D` pushing rigid bodies needs physics tuning/possibly a bounded shove impulse on actual slide contact. Mass should matter: the player should not automatically launch a heavy desk. Start with explicit light prop mass and damping profiles; stairs, slopes, stacks and table friction need runtime review.
- **Visible affordances**: subtle dot/highlight or contextual `E Interact` when reachable; no pickup prompt if item is behind another opaque physical object. FPS precision targets handle; overhead may target whole cupboard front/door for readability. Don't use permanent per-prop raycasts/draw updates.
- **Realism budget**: complex effects are optional. Closed containers/doors keep collision and state; loose props use physics while disturbed then engine sleep; distant props can remain authored static or be serialized/frozen with a safe wake boundary only when later streaming exists. Do not simulate hundreds of items permanently just to preserve their locations.

## IX0 — Minimal shared interaction/targeting foundation

**Objective:** Prove both views can select **one actual 3D object** nearby, request an allowed interaction and display a tiny contextual prompt, without gunfire/UI conflicts.

**Dependencies:** current player cameras, input adapters, physics layers, `WORLD/World.tscn`, FPS inherited scene and any debug GUI.

**Scope/files:** `INTERACTION/Shared/` small `IInteractable` action contract; `INTERACTION/ViewAdapters/` overhead cursor ray + FPS centre-screen ray; world/player composition seam as needed; `INTERACTION/Configuration/` master only if used. One test object that answers `Inspect` or simple toggle; no physics/item state yet. A single scene-local source/target interaction owner; no global manager. Cached queries with appropriate collider-to-ancestor resolution; bounded lookups.

**Out of scope:** rigid props, weapon placement, cupboards, inventory, zombie attacks, UI redesign, player health and saving.

**Preserve:** existing LMB firing/hold, slots 1/2, F6 both scenes, camera rotation, debug sliders, mouse capture/release, roof/wall fade. Validate physical reach through walls and on different floors.

**Performance:** update target only on relevant mouse/camera/player motion with short bounded casts; in FPS, center-screen re-target at a conservative relevant frequency **only while aiming at interactables/in interaction mode**, not a global all-props physics tick. Hide prompt/unsubscribe when not focused or world unloads.

**Pass:** same reachable test prop works in both views; beyond-range, upstairs-through-floor, faded-wall and GUI hits fail; interacting never fires an accidental shot. Build and F6 both modes if possible. **Codex/Work** (multi-adapter input), Regular Chat can tune prompts/settings, Copilot handles local checks.

## IX1 — Physical loose props and player contact

**Objective:** One book stack (3–6 individual books), one box, one bottle or bucket can rest on a table, fall when displaced, collide with the player and other props, and sleep reliably.

**Dependencies:** current Jolt 3D, existing terrain/floors/collision; IX0 only for user-driven debug affordances.

**Scope/files:** `WORLD/Content/Placeables/Props/Shared/` generic `RigidBody3D` scene with simple collider and authored `PhysicsPropSettings` (mass, friction via physics material, bounce, damping, sleep, permitted grabbing); small sample prefabs and `Props/README.md`, local master. Preserve scene-owned placement. Do **not** make world scenery generic rigid bodies; no page-level/book-page simulation. If current player slide collision does not naturally yield satisfying pushes, add only a narrowly guarded contact impulse when colliding with eligible light bodies.

**Out of scope:** persistent inventory, item ownership, throw, interactive containers, food/loot, physics stacks across entire world.

**Preserve:** `Terrain3D` and saved region data, existing buildings/structure fading, zombie/player collision, weapon hits. Don't create new terrain bakes.

**Performance:** let Jolt sleep stable rigid objects; avoid per-prop C# `_Process` / `_PhysicsProcess`; safe collision layers and shape budget, selective activation later after profiling. World collision remains authoritative.

**Pass:** walking against light props nudges them without pushing stationary house/cupboard; props fall/stack/settle, no exploding/jittering books, stable collision on table/slopes, FPS and overhead same physics. Validate at ~6, ~30 and ~100 staged props briefly with profiler if available. **Regular Chat** may handle isolated prop scene/config; **Codex/Work** for collision tuning.

## IX2 — Physics grab, hold in front and throw (no per-object hand animations)

**Objective:** Pick up a suitable light prop (book, broomstick, bucket), hold it floating naturally ahead of the player, move it around, drop or throw. **Never place an ordinary grabbed prop in inventory by default.**

**Dependencies:** IX0, IX1 and cached player/view references; reuse generic object permissions, max-mass config and visibility/reach.

**Scope/files:** `INTERACTION/Physics/PhysicsCarry.cs` or equivalent single reusable player carry capability; `INTERACTION/Configuration/` carry settings; subtle UI state. Both camera adapters supply desired world-space hold target and throw aim, while one shared physics implementation handles body motion, collision, release and force caps. Consider `RigidBody3D` force/velocity-to-target with damped spring, never teleport physics through walls. Controlled rotate object as later optional functionality.

**Out of scope:** bucket liquids, furniture relocation by cranes, two-handed art, prop-inventory, per-item animations, HL2 gravity-gun launching, multiplayer and save.

**Preserve:** player walking/crouch, camera zoom/yaw, FPS mouse look, firing vs interaction focus, equipment switching, actual collision/terrain.

**Performance:** only the **single held prop** receives carry target work while active; Jolt handles any freely moving/sleeping bodies. On release, throw, out-of-range obstruction, focus loss, player death, scene unload or object deletion, clear state and restore physics/collision flags. No idle carry loop.

**Pass:** lift broom/book/bucket below mass limit; reject heavy item and blocked reach; hold and move without tunnelling through wall/door, throw within bounded impulse, object then falls/sleeps, gun isn't accidentally discharged, same in FPS/overhead. **Codex/Work** for stable rigid-body manipulation; **Regular Chat** balances mass/impulse.

## IX3 — Cupboard door and selective mouse-drag opening

**Objective:** One authentic, reusable cupboard with two hinge-operated doors that can remain partly open and block nearby object movement.

**Dependencies:** IX0 and existing building/interior placement. IX2 optional.

**Scope/files:** `INTERACTION/Doors/HingedDoor.cs` or a small reusable door scene, one `DoorSettings` Resource, reusable cupboard prefab under world props/structures, visual pivot and correct collision. Shared state: `AngleDegrees`, min/max, `Locked`, `OpenTarget`/manipulation state. Top-down: reachable click requests motorized smooth open/close. FPS: target handle then direct hold-drag motion controlling the same angle; clamped rate, bounds and damping; release halfway preserves current angle. Cursor/weapon arbitration and camera capture restored on release; door obstruction should stop rather than clip a book.

**Out of scope:** full physically free hinge/swinging from explosions, cabinet loot tables, per-model grip/hand animation, audible creak asset and saving (IX8).

**Preserve:** same scene door for both modes, moving collision vs player and cabinet frame, roof/wall occlusion rules, existing gun/weapon input.

**Performance:** door updates only while angular movement/tween/drag is active; dormant at rest. No per-frame hinge work when fully stopped; physics-joint option must be measured before adoption.

**Pass:** top-down one click opens to target and another closes; FPS slow mouse drag opens slowly and quick drag faster, leaves halfway open; constraints and collision behave, no shot when dragging, no remote interaction through wall. **Codex/Work** for hinge/input integration; Regular Chat for authored appearance.

## IX4 — Destructible board across doorway

**Objective:** A doorway is physically blocked until an authorized melee attack breaks one or more mounted wooden planks; player can then pass through.

**Dependencies:** current `ENTITIES/Shared/Combat` and `COMBAT/Shared/` contract, **functional CM2 melee detector and action** (or CM3); IX0 interaction visibility optional, never rewrite damage. No user-facing board destruction until an actual melee hit path exists.

**Scope/files:** `INTERACTION/Destruction/BreakableObject.cs` optionally `IDamageReceiver` on a prop root, `HealthSettings` profile (board strength/armour) via existing resolver; prefab plank under world objects/structures with stable collision while intact, break state, optional bounded small rigid debris on lethal hit; update authoring docs. Real collision barrier only removed on break (deferred-safe collision disable). Keep player/AI navigation/LOS collision consistency when board breaks.

**Out of scope:** runtime boolean mesh cutting, voxel destructible wood, building-wide structural collapse, door hinges, animation asset import, new damage solver.

**Preserve:** zombie hit reactions, weapon damage, wall blocking, structure visibility, player collision and F6 both views.

**Performance:** no idle polling for breakables; only hits invoke health; on break spawn **few** debris bodies or cosmetic particles, sleep/free them by policy; memory/save state later IX8.

**Pass:** plank blocks walking/shots/interaction until damage thresholds met; qualifying melee breaks it once, clears navigation and physics path, residual debris doesn't trap actor or clip downstairs floor; FPS and top-down identical. **Codex/Work** for combat and physics coordination, Regular Chat for balance.

## IX5 — Item identity, ownership and tiny inventory backbone

**Objective:** One real item has one stable identity and exactly one owner: world, carried (if supported), player inventory or equipped. Store definitions vs runtime instances distinctly; avoid copying firearm scenes as substitutes for items.

**Dependencies:** IX0; live two-slot `WeaponSlots`/firearm adapter audit; current nine-slot visual hotbar must not silently become logic. IX1 only needed if first world item is physics-simulated.

**Scope/files:** create `INVENTORY/README.md`, small immutable `ItemDefinition` Resource, light mutable `ItemInstance` record with stable ID and optional typed state, per-player inventory owner and explicit atomic move methods. Begin with **one rifle** and simple food/ammo type definitions; defer full magazine/inventory grid and gear stats. World pickup is one representation holding item identity. `INVENTORY/Configuration/InventoryMaster.tres` only for actually adjustable capacity/mass/etc. `EQUIPMENT/Shared/WeaponSlots.cs` remains a test adapter or receives a minimal bridge, without breaking its current F6 setup.

**Out of scope:** full 9-slot hotbar system, loot spawning, recipe/crafting, economy, magazine simulation, persistence (IX8), complex containers, all prop classes being inventory items.

**Preserve:** weapon scenes and per-gun shot stats, slots 1/2 until deliberately transitioned, FPS and top-down shared weapon instances, no duplicated usable gun after moving item. `CanPickupToInventory`, `CanEquip`, `CanPhysicsCarry` and `CanPlace` may differ independently.

**Performance:** no per-item frame callbacks, event-driven ownership notifications and UI refresh. Item definitions static; dynamic state updated only on real mutation; scene creation only on enter-world/equip.

**Pass:** verify legal World→Inventory→Equipped→World transitions, unowned/invalid requests reject without losing item; repeated pickup/putdown leaves exactly one rifle, identity intact and no two owners. **Codex/Work** cross-equipment architecture; Regular Chat can define sample item data and UX.

## IX6 — Set down/retrieve equipped rifle, deliberately leave hands free

**Objective:** Player places their currently equipped rifle on a nearby table at a stable valid point, can leave it there while opening cupboards or fighting unarmed, and can retrieve the **same** rifle later.

**Dependencies:** IX5, IX0; IX1/IX2 carry physics can be reused for dropped gun but not required for inventory transitions; CM2 full quick melee input if punching/shoving desired. Does not depend on PH0 if test zombie cannot yet hurt the player.

**Scope/files:** item ownership bridge for `EQUIPMENT/`, `PLAYER/Equipment/` and FPS adapter; world item scene with realistic gun visual/collider; placement surface probe/clearance, orientation, static settle or initially simulated `RigidBody3D`; click/E pickup. On placing: cancel trigger/cooldowns/flash/sound, unequip and hide/detach wielded gun representation, put authoritative item instance into world state; player can now be unarmed. On retrieving: check reach, transfer ownership atomically, remove world representation, reactivate equipped view model with same identity/state. Distinguish careful **Place** versus gravity **Drop/Throw**. If no suitable table, reject placement gracefully without deleting weapon.

**Out of scope:** holster/grab animations for every item, magazine/reload system, dynamic zombie attacks, save game, drag-and-drop inventory UI, automatic hand magnetization.

**Preserve:** current gun muzzle aim, cover collision, FOV/mount alignment in FPS, 1/2 test slots or explicitly authored bridge, audio/light cooldown cleanup, button priority. Do not spawn two independent active firearm controllers for one item.

**Performance:** placement and retrieval are event-driven; world gun sleeps when resting; equip model only processes when actively fired/rendering effects as before.

**Pass:** rifle placed on table, hands empty; retrieving restores same ID and whatever ammo/condition **exists in game at that time** (today there is no implemented magazine/condition system); old world gun disappears exactly once; clicking cupboard never fires; both camera views preserve same interaction semantics. **Codex/Work** multi-owner transitions; Copilot local physics/input checks.

## IX7 — Visible scavenging behind movable clutter + food/ammo pickups

**Objective:** Cabinet/books/box can physically conceal a pickup; moving obstructing props reveals it and allows a visible/reachable interaction prompt and inventory transfer.

**Dependencies:** IX0, IX3 for cabinet scene, IX5 for item identity; IX1/IX2 if actually moving books rather than using a simple hinged cover. PH3 for real hunger/food consumption only, **not** for mere food pickup.

**Scope/files:** one authored kitchen POI reused by both views. Place a few rigid books in front of an ammo box or canned food. Prompt uses same collider/visibility/reach filter as IX0, plus relevant cover and collider order; no automatic pickup of objects behind opaque cover. Food/ammo item definitions and simple pickup; optional `Use` callback placeholder that does **not** fabricate hunger effects. Inventory updated by event only, UI prompt only while targeted. Shared cupboard control and world prop lifecycle.

**Out of scope:** full procedural loot tables, nutritional simulation, full inventory UI, stacking hundreds of loose rounds, zombie combat encounter scripting, dynamic item generation.

**Preserve:** interaction between doors, physics books and pickup collider; gun input, wall/floor fade checks and both camera modes.

**Performance:** no per-pickup idle searches. Resolve selection when aim/target changes and on relevant object motion; physics books sleep afterward. A limited number of individually physical books; other books can be static decorative meshes.

**Pass:** cannot interact with hidden ammo through book stack or cupboard door; push/move books, reveal ammo and collect; inventory receives it exactly once, world icon disappears; FPS and overhead can access it without reaching through faded wall. **Codex/Work** for multi-system slice, Regular Chat for layout/content.

## IX8 — Persistence, scale and mode comparison (deliberately later)

**Objective:** The world remembers physical changes: shattered board, cupboard angle, items moved/collected/placed, and item instance/ownership. Both presentation modes interpret a **single saved state** in equivalent gameplay terms, then use the exact same test kitchen to compare experience.

**Dependencies:** relevant IX1–IX7 slice accepted; explicit coordination with a separately authorized **save/persistence plan**. **Do not invent a separate interaction-only save system** that competes with eventual `SAVE/` world serialization. User can compare views manually before saves.

**Scope/files:** explicit serializable record types for world prop transform (pose + appropriate motion/rest state), stable prefab/world instance ID, board intact/broken, cupboard `AngleDegrees`, pickup existence, item identity and exactly one owner; restore ordering and streaming/chunk load/unload contract. Reconcile invalid positions and duplicate identities safely; save changed data on explicit game save rather than per physics frame. Optional debug overlays/metrics. Include one "kitchen encounter" playtest in both F6 scenes with identical authored content. Any mode switch that reloads a scene must first save/restore state; do not claim live cross-camera changes persist automatically.

**Out of scope:** complete persistent open world, multiplayer/netcode, inventory migration of every existing world asset, automatic autosave tick, indefinite ragdoll debris save.

**Preserve:** saved Terrain3D data, building structures, zoning/streaming boundaries, camera fade, world actor config; no unexpected rebake/reset.

**Performance:** only record dirty items and meaningful settled rigid transforms, not frame-by-frame writes. Distant/nonrelevant props can be frozen or reconstructed from serialized state where safe; profile physics bodies awake vs asleep, active interactions, object allocation, visibility and GPU cost. Release subscriptions and physics carry state on chunk unload/reload.

**Pass:** placed rifle still on table after reload with same identity, no duplicate inventory/world instance; scattered books/cupboard/broken planks restore, collected ammo doesn't reappear; UI/input works in FPS and overhead; unloaded objects resume stable. **Codex/Work** for save integration and physics lifecycle; Copilot for local persistence testing.

## 4. Recommended implementation order and staged decisions

**Smallest worthwhile initial request to Codex:** implement **IX0 only**, with one non-combat toggle target and both cameras. Review in Godot. Then **IX1** (books and a small box with real physics) and **IX2** (float-carried book/bucket/broom). This demonstrates the distinctive hands-free physics manipulation without committing to item/inventory complexity or high-detail animations.

Next do **IX3** cupboards. **IX4** breakable plank should wait until the shared CM2 melee detection/input works; existing CM0/CM1 alone do not enable plank-breaking strikes. For the rifle-on-table scene, do **IX5 then IX6** as separate reviewable contracts before IX7 scavenging. IX8 waits for a wider save architecture. *The phases are not permission to start one another.*

**Potential simplifications to consider before coding:** one actual item of each class; only one held physics prop per player; generic books/bucket/broom do not need item identity until they must persist; one cabinet and one model rather than an interaction framework for every hinge; simple joint angles before physical hinges; a single contextual prompt instead of a large radial action menu; no universal mass/inventory coupling; no forced one-/two-hand procedural animations. The same item can later gain new capabilities without rewriting its physics.

**Design tradeoffs open for owner review:** interaction activation input (`E` vs contextual click), whether LMB FPS handle drag requires holding E/focus first, placing rifle versus tossing rifle, approximate carry mass/reach/collision behaviour, whether selected loose props rotate while held, and exact selection highlighting. Use provisional, configurable defaults rather than block the plan with decisions.

## 5. Codex handoff and minimal validation

Before a phase, Codex should read `CodePreferences.md`, root `README.md`, this plan, `INTERACTION/README.md` and the phase's actual current source. Then: report the narrow intended changes; implement **that phase only**; update owning READMEs and phase state; push one non-force commit to development branch; report commit SHA, files, actual validation and deferred Godot tasks.

**Current validation for this plan:** documentation/source audit only; no gameplay/physics code created, no `.tres` or `.tscn` changed, no build/runtime test applicable.

**Baseline each implementation phase:** `dotnet build war-machine.csproj` if executable; F6 `WORLD/World.tscn`, and F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` for shared input/physics. Check UI click refusal, held gun cooldown cancellation, Esc/focus loss, reach and wall/floor barriers, collision masks, rigid sleep/reawaken, death/unload, no extra idle `_Process` or per-prop raycast. Do not rebake saved terrain. Focused source review is enough here; owner performs expanded visual and performance checks. **No phase may claim Accepted without owner in-game review.**
