# Melee foundation (CM2-A)

The actor-neutral `MeleeAttack` helper and `MeleeSettings` resource are available but **not yet wired to input or damage**. `COMBAT/Configuration/QuickShoveMaster.tres` is the initial zero-damage profile.

Create one `MeleeAttack(actorCollisionBody)` per attacking actor when equipment/AI is initialized. Call `TryStart(profile)` on input/AI action; while `IsActive`, call `Advance(delta, bodyOrWeaponOrigin, aimDirection, mode, out contact)` from its existing physics tick. A true result contains the collider, supported actor, closest confirmed contact point and horizontal push direction. Future CM2-C sends it through `HitTargetResolver.Dispatch`, not a second damage system. Call `Cancel()` on interrupt, death, equipment change or unload; no idle callback is introduced.

**View semantics:** `MeleeAimMode.Horizontal` (top-down) projects facing onto XZ and applies a bounded height tolerance, while `ThreeDimensional` (FPS) checks the whole direction including camera pitch. Origin must be near the character's upper body rather than at the overhead camera. The shape query is a temporary sphere around origin filtered by distance, 65° arc, receiver eligibility and a short occlusion ray against the same collision mask. A wall/door must block the ray. Only one closest eligible recipient can be contacted per attack; the latch prevents duplicate hits during active frames.

**Performance:** Cached query resources; at most 32 physics overlap candidates and one short LOS ray per eligible candidate, only during the bounded active attack. There is no scene search, permanent `Area3D`, new timer or per-frame idle work. Timed cooldown expires without polling.

**Limitations:** Contact point uses the first direct child CollisionShape3D as a centre reference, with the collision body origin as a fallback for other shapes; large wildlife or complex mesh colliders may require more precise closest-point sampling in future. Broad-phase candidates have a fixed cap and aren't pre-sorted; very dense crowds should be tested. Swept multi-target swing arcs and per-target dedup are deferred to CM3. CM2-A does not change zombie reactions, bind V/left mouse or animate a shove.

**Local validation:** `dotnet build war-machine.csproj`; later use a temporary caller to exercise TryStart/Advance in top-down and FPS, check in-range, out-of-range, behind-wall, elevated-floor, aimed-high/low FPS misses, no duplicate contact and cancellation. There is no live control to test yet.
