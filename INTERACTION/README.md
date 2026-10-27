# INTERACTION — shared physical world interactions (planned)

> **Status: design/roadmap only. No runtime interaction feature added.** Start with [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) (**IX0–IX8**, each independently authorized). Plan reflects the owner's 2026-10-27 discussion of physics props, Half-Life-style floating object carry, cupboards, boarded doors, hidden scavenging and rifle placement.

## Principle: one world, different input adapters

War Machine's top-down and first-person scenes already share the same world, terrain, actors and weapon internals. Every cupboard, loose book, plank, ammo pickup and dropped rifle should remain **one authoritative 3D object/state**, independent of camera mode. Only selection, control feel and viewmodel animation may vary:

- **Top-down:** aim using the mouse cursor projected into the world, then physically validate player reach, solid cover and correct floor. Click opens a hinged cupboard smoothly; don't let translucent roof/wall fading enable remote access.
- **First-person:** centre-screen selection and precise handle targeting; a focused mouse-drag gesture can move the same cupboard hinge proportionally to mouse motion. While dragging, consume weapon fire/look input correctly and restore captured mouse/look afterward.
- **Both:** use Jolt `RigidBody3D` mass/drag/friction and sleep for loose props, with one shared physics carry/grab/throw action. A bucket/broom/book need not use player hand animations and **need not be in inventory**; they can physically float in front of the player and fall/settle when released. Weapons use their own equipped presentation instead.

## Folder boundaries

`INTERACTION/` = target eligibility, real-world reach, action intents, physics grab/throw, door and destruction orchestration. `INVENTORY/` (to be created in IX5) = unique item definitions/instances, single ownership and state transitions. `EQUIPMENT/` = held weapon behaviour and sockets. `COMBAT/` and `ENTITIES/Shared/Combat/` = melee, impacts and damage (already partly implemented). `WORLD/Content/Placeables/Props/` = new model library with Book001 art (IX physics prefabs still planned); `WORLD/Content/Placeables/Objects/` = fixed model library with Bookcase001. `PLAYER/` = movement/pose, `DEBUG/TEST/FirstPerson/` = input/camera presentation adapter. Save integration will be part of an eventual shared persistence owner, **not** a new competing one here.

**Key distinction:** `WORLD`, `PHYSICS_HELD`, `INVENTORY`, `EQUIPPED` are location/ownership modes of a thing, not four separate item clones. Some props are deliberately world-only and can never be stowed/equipped. An equipped rifle has a persistent item identity and may be carefully placed back into the physical world. Gun ammunition and item condition are **future mutable state**, not currently working features.

## Immediate proposal

Start **IX0** (common selection and input arbitration) with both F6 camera modes. Continue with IX1 movable books/bucket and IX2 floating physics pickup/throw. Only then add cupboards (IX3), melee planks after CM2 contact (IX4), item identity/inventory (IX5), rifle placement/retrieval (IX6), scavenging (IX7), persistent save (IX8). Codex can tackle an explicitly approved slice when available. No action taken automatically by this plan.

## Artwork library now exists (not implemented gameplay)

[Placeables/README](../WORLD/Content/Placeables/README.md) is the authoritative future library. Bookcase001 and Book001 are original **GLB mesh-only** designs; they have no game scenes, collision, interaction or world placement. IX0–IX8 remain Planned.
