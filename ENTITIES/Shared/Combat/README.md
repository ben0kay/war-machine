# Shared damage pipeline

> **Feedback API addition (2026-10-28):** The existing `HealthComponent` now joins the `health_components` group on Ready and emits `HealthChanged(HealthComponent)` on Configure and successful HP loss for read-only UI consumers. `Damaged`, `Died`, damage formulas and zombie health behavior are unchanged. The feedback manager uses this event for bars **only below full health**. PH0 may reuse the event for future heal updates rather than creating a second incompatible signal.

**Implemented; awaiting local .NET/Godot validation.** This code has no dependency
on the rifle, zombies, EntityCore, terrain or a particular hit-detection technique.

`Attack detection → IDamageReceiver.ReceiveHit(DamagePacket) → HealthComponent.ApplyDamage → DamageResolver.Resolve → health/reaction events`

- `DamagePacket.cs`: readonly value packet with amount, type, source and impact
  position/direction. Also contains DamageResult (resolved amount/mitigation).
- `IDamageReceiver.cs`: common hit entry point on an actor/destructible root or ancestor.
- `DamageResolver.cs`: stateless rules, no raycasts or target searches.
- `HealthSettings.cs`: resource for maximum HP, flat armour and per-type resistances.
- `HealthComponent.cs`: runtime health and Damaged/Died C# events; no processing loop.

## Current rules

Types: Physical, Energy, Explosive, Fire. These are explicit enum values; extend
that enum and HealthSettings when a new type is needed. Each packet has one type.
For Physical, subtract nonnegative flat Armour first. Other types bypass Armour.
Then multiply by `(1 - matchingResistance)`. Resistance is a fraction, so 0.2 is
20%; negative values are vulnerabilities (bounded at -1), and 1 grants immunity.
No random rolls or hidden weapon-specific multipliers. Invalid/nonpositive input
is ignored; defences are bounded to prevent NaN propagation or negative damage.

Health clamps at zero and latches death before callbacks. Hits after death are
ignored. DamageResult.Amount describes incoming resolved damage, including
potential overkill, not necessarily actual HP removed. Mitigated never goes below
zero. Configure explicitly initializes/resets health and duplicates the authored
profile; it does not mutate the .tres. The target gates eligibility and owns its
reaction: removal now, future falling/sparks/breaking later.

## Current integration

RifleMaster.tres: Damage 95, DamageKind Physical (0).
ZombieMaster.tres: embedded HealthProfile, MaxHealth 100, Armour/resistances 0.
First hit leaves 5 HP; second kills. Zombie movement/awareness and robot are unchanged.

For a deer, robot or destructible: supply a HealthSettings profile, initialize a
HealthComponent, implement IDamageReceiver by forwarding to it, and subscribe to
its events for that target's reactions. No zombie base class is required.

An axe/laser/rocket performs its own hit detection and sends the same packet.
Explosion radius/falloff belongs to the attack; it sends a scaled packet to each
eligible target. No axes, lasers, rockets, knockback, DoT/status effects, armour
penetration, healing, player health or robot damage are implemented by this pass.

Performance: packets/results are value types; shared resolver is stateless;
health is event-driven with zero idle callbacks/timers/world scans. Only attacks
that actually hit invoke resolution. Scene ownership frees each component with
its actor. Current zombie death still stops its timer/motion and queues removal.

## Brief local checks

Build `dotnet build war-machine.csproj`, then F6 the top-down scene. Confirm 95
physical versus 100 HP takes two hits. In the embedded ZombieMaster health profile,
20 Armour should leave 25 HP after one hit (95 - 20 = 75). With zero armour and
0.2 PhysicalResistance, a hit should deal 76 and leave 24 HP. Energy should bypass
Armour but use EnergyResistance. Restore test defences to zero afterwards.
Confirm multiple lethal hits do not run duplicate death logic, and misses/UI
clicks do not apply damage. Compilation/runtime checks are deferred because this
environment has no Godot/.NET runtime; source/resource wiring was reviewed only.

## CM0 / CM1 impact integration

COMBAT/Shared now provides a separate optional impact contract and one contact dispatcher. Firearms still send the same DamagePacket through IDamageReceiver/HealthComponent; damage resolves before impact and death wins. A zero-damage impact omits health entirely. Zombie bullet reactions are recipient-owned, not a second DamageResolver or health event hack. See ../../../COMBAT/README.md for values, anti-stun-lock and local checks. CM2+ melee remains planned. This folder remains the only damage/health implementation, unchanged by CM0/CM1.
