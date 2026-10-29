# CM5-A — Coordinated zombie attack pose

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| CM5 | Existing committed standing melee and damage query | Existing | Implemented in source |
| **CM5-A** | Torso/head/arms lunge visual tied to CM5 timing | Regular Chat (contained procedural pose pass) | **Implemented in source; awaiting local validation** |
| CM5-B | Collision-safe physical lunging/forward step, reach tuning | Codex / ChatGPT Work | Planned; not authorized |
| ELD3 | Attack capability restrictions, crawl/bite variations | Codex / ChatGPT Work | Planned; not authorized |

## CM5-A — coordinated zombie lunge animation (2026-10-29)

**Implemented in source; awaiting local Godot validation.** Existing CM5 standing swipe previously animated only both shoulder joints, making the zombie stop and pump its arms with no chest or head momentum. Now both shamblers and runners share one cached `UpperBody` waist pivot (inserted once when their procedural species template is authored, then cloned by existing `ZombieVisualBatcher`). Waist/chest/neck/head/arms reparent under that pivot while both legs remain under the existing root. The visual silhouette at rest is unchanged via `Reparent(..., keepGlobalTransform: true)`. Torso virtual anatomy region now follows the upper pivot; head/arm region pivots remain intact. No body-collision edits, extra material/draw resources, timers, or scans.

The current attack's **existing windup → active → recovery** clock drives an opposing windup lean, side-crossing torso roll and twist, visible forward lunge, matching head sway and asymmetric lead-arm swipe. Attack side alternates each successful commitment with a spawn-seeded initial side; it does not use or perturb roaming RNG. Neutral pose is restored by the existing per-physics species-pose rebuild after completion/cancellation; existing damage contact, timing, facing lock, LOS/cover, mobility, reaction and cooldown are untouched. Corpse pose receives the added waist pivot for relaxed post-death visual settlement. All movement/collision remains grounded; visual-only lunge is not a collision-checked reach extension.

**Tuning:** `ZombieSettings` provides five Inspector values in **Attack lunge pose (visual only)**. Both `ZombieMaster.tres` and `ZombieRunnerMaster.tres` author torso roll 14°, twist 10°, forward lunge 16°, head sway 7° and lateral waist shift 0.075m. These affect animation only, not hit reach/damage.

**Activation/performance:** no extra `_Process` or `_PhysicsProcess`. The existing active-attack physics animation updates five cached joints until recovery; a grounded inactive zombie stays asleep with only its pre-existing staggered sensing timer. The upper pivot is one extra scene-tree node per zombie and its mesh batching remains per-pivot. No additional raycasts, allocations or physics overlap checks during pose.

**Local acceptance:** run `dotnet build war-machine.csproj`; F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. Have a shambler and runner attack while standing still: verify torso, neck/head and arms sway together and return to neutral, with hips and legs grounded. Repeat with multiple attacks (alternating sides), shots/stagger interrupts, missing/severed arms, crawl/immobilized no-strike, death during windup and corpse fall, switch/chase sight, cover/dodge, camera viewpoints and crowds. Compare side-on animation, character skin/material batching, anatomy region shots and FPS with prior CM5. **No local engine compilation, gameplay or performance profile was performed through the GitHub connection.**

## Handoff

Branch: `implement-td0-td1`. Single commit for this authorized visual pass; see Git history for commit SHA. Visual correctness and runtime/body-region verification remain for local acceptance. Do not start CM5-B/ELD3 automatically.

## CM5-A1 — horizontal claw swipe (2026-10-29)

**Source implemented, local validation pending.** After CM5-A's waist/head sway pass, the strike still looked mostly vertical because both arms swept primarily in local X pitch. CM5-A1 replaces that repetitive up/down arc with a **single leading claw held near chest height** (default 78° raised pitch), traversing **110° total horizontal yaw** across the torso during the same 0.12s active window. The opposite arm follows at 72% of lead elevation and lightly counterbalances at 18% of the yaw. The lead alternates left/right per committed swipe using the existing attack-side state.

The same `MeleeAttack` elapsed phase drives an eased 0.45s arm-raising windup, 0.12s side-crossing swipe, and 0.55s recovery; there is no animation player, new node or independent timer. Torso twist is increased from 10° to **18°** to accompany the horizontal stroke, and forward pitch is reduced from 16° to **10°** to avoid the appearance of an overhead hammer-down. Existing roll/head sway/side shift remain unchanged. These are cosmetic presentation parameters: **melee contact origin, arc, damage, cooldown, physics root, limb injury logic and pursuit AI are unchanged**. The runner inherits the same attack overlay and retains its own movement pose.

**Tuning:** Added `ZombieSettings.AttackHorizontalSwipeDegrees` (30–140°, default 110°) and `AttackSwipeArmPitchDegrees` (40–100°, default 78°), authored identically in the regular and runner masters. Torso twist 18°, forward pitch 10° are now authored in both masters. All settings remain in the existing *Attack lunge pose (visual only)* inspector group.

**Performance:** only existing attacking zombies execute the extra few scalar calculations and cached arm pivot writes inside `ApplyAttackPose`. Grounded idle zombies retain their sleep behavior. No new queries, timers, extra scene nodes, mesh regeneration or allocations.

**Acceptance handoff:** `dotnet build war-machine.csproj`; run `WORLD/FirstPerson/FirstPersonWorld.tscn` and `WORLD/World.tscn`. Watch front and side silhouettes in shambler/runner attacks. Expect a raised leading arm to travel sideways across the body rather than both arms pumping, with alternating lead sides and torso/head follow-through. Confirm one damage event per accepted strike, no reach increase, cancellation on shot/shove/death, missing-arm visual handling, corpse handoff, and idle sleep. Godot/SDK rendering and compilation have not been performed from the GitHub connection. CM5-B physical stepping and ELD3 low-body attacks remain unapproved.
