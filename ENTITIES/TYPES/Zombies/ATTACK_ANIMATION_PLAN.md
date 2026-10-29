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