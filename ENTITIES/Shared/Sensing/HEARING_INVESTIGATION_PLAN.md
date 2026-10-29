> **HE1 clear/blocked first pass (source implemented, awaiting local validation):** Each accepted sound consumed by the existing physics brain gets **one** cached ray from the listener eye to the original recorded muzzle coordinate. Mask 17 checks world/props (including eligible solid doors), excludes self and ignores actors/anatomy areas. A first hit means blocked; there is no counting, recasting or route search. The sampled estimate remains unchanged. Blocked commitment weights blend 85% toward early-only, and the existing capped episode deadline is multiplied by 0.35. Clear-line sight/hit-independent sound policy is unchanged. These are authored `SoundBlockerMask`, `SoundBlockedEarlyBias`, `SoundBlockedTimeoutMultiplier` defaults for both species. Mask 0 disables this check. Retarget cooldown is now **6 seconds**. This explicitly supersedes HE1's original no-acoustic-rays exclusion below; HE2 and full HE3 remain deferred.
>
> Query occurs only once on the existing brain timer after visual/hit priority checks, never every shot or movement tick. Blocker result applies only to this episode; ordinary PER2 sight/hit calls use durationScale=1 and unchanged weights. The shared deadline multiplier changes no stored policy. Blocked does not mean unreachable: this is a cheap commitment heuristic, not sound attenuation or pathfinding. Hidden decorative meshes without solid collision do not block. Terrain counts when intersected; no extra ground checks. No added callbacks, ray loops or new scene nodes.
>
> Source/whitespace/loop and numeric policy checks only; Godot/.NET unavailable. Locally build, then test clear vs solid wall/fence/closed door in both World/FPS and both species. Use missed shots outside vision to isolate hearing. Blocked sounds should mostly yield partial/early searches and shorter deadlines, retain fixed estimates, respect burst cooldown and settle at actual give-up positions. Verify visual and bullet-hit priority, multiple walls still only one ray, actor crowds alone do not mark the line blocked, death/unload cleanup and horde frame time. No measured runtime result claimed.

# Hearing and fixed-point investigation

Required branch: `implement-td0-td1`. Current source supersedes the older exact-sound-anchor proposal: all investigations retain PER2's physically settled wander anchor, never the original or estimated sound coordinate.

| Phase | Scope | Status |
| --- | --- | --- |
| HE0 | World-local immutable sound events, successful-shot emission independent of playback | Implemented; local validation pending |
| HE1 | Event-driven gunshot hearing, uncertain position, distance/intensity commitment, shared investigation and listener cleanup | Implemented; awaiting local build/gameplay/performance validation |
| HE2 | Footsteps, looting, doors, melee and other discrete action emitters | Planned; not implemented |
| HE3 | Acoustic obstruction/reflections, environmental modifiers, spatial indexing/suppression | Planned; not implemented |

## Current HE0 ownership

`WorldAudio` owns `WorldSoundEvents`, whose `Hearing` dispatcher broadcasts immutable `SoundStimulus` values. Successful firearm shots emit at the actual muzzle, with numeric source/emitter IDs and per-world sequence; no retained source node or event history. Invalid/blocked firing attempts emit nothing. Hearing remains independent of audio stream availability, mute, playback distance, voice pool and mixer gains. Effective amplitude is clamped profile/action loudness; hearing radius scales by sqrt(amplitude), intensity falls linearly with distance. No new emitters were added in HE1.

## HE1 implementation

`WORLD/WorldController.cs` binds each successfully placed zombie to `_audio.SoundEvents.Hearing` before target initialization. `ZombieHearing.cs` subscribes only when configured and activated, detaches on deactivation/death/exit or rebinding, and releases its dispatcher on exit. Existing WorldAudio shutdown clears delegates and invalidates emission handles. Both species and both views use this same path; standalone debug corpse fixtures intentionally have no service.

One callback evaluates each gunshot: skip invalid/dead/inactive/uninitialized/disabled listeners and self-events, confirmed chase and direct-hit clues. Squared distance rejects outside-radius events before square root, randomness or estimate work. No sound cone, LOS ray, physical sound propagation or source transform lookup. Unheard shots do not alter goals or anchors.

Each accepted sound stores one compact snapshot: original immutable coordinate (arbitration only), one estimated coordinate, interpolated commitment weights, received strength and sequence/cooldown state. No list/history. Position error is a single seeded, uniform-area horizontal disk sample (`sqrt(random)` radius). The estimate stays fixed throughout travel; no brain-tick reroll. Existing seeded per-zombie RNG produces independent listeners. Sound evaluation may consume RNG; unchanged sight/hit Begin calls retain their original random draw sequence when no sounds intervene.

The existing staggered brain consumes the pending estimate after its visual decision, records a `Sound` clue and invokes the existing `InvestigationMemory.Begin` with a one-episode weight override. Existing shared fractions, speed-aware deadline, cap, dwell, arrival, stuck fallback and locomotion are reused. The optional override does not mutate settings, so sight/bullet-hit probabilities remain 40/35/25 (or their authored values). No new AI, navigation, timers or hearing physics callbacks.

Priority: confirmed visual chase > actual bullet-hit clue > heard sound > wandering. Sound may replace a last-seen search, but not an active direct-hit episode. A direct bullet contact clears a pending sound; confirmed sight replaces the sound episode normally. Sound episodes need no live source/target; moving after firing never updates the estimate. Giving up settles and anchors at the actor's real stopping point, including partial travel and blocked paths. No teleport or exact-sound anchor.

## Authored configuration

Both standard and runner masters explicitly author the same **Gunshot hearing (HE1)** fields in `ZombieSettings`:

| Field | Default / meaning |
| --- | --- |
| HearingEnabled | true |
| MinimumAudibleIntensity | 0.08; received intensity threshold |
| SoundUncertaintyDistances | (10, 110)m; normalized distance endpoints |
| SoundUncertaintyRadii | (1, 18)m maximum error radius |
| SoundNearCommitmentWeights | (0.05, 0.15, 0.80) early/moderate/thorough |
| SoundFarCommitmentWeights | (0.80, 0.18, 0.02) early/moderate/thorough |
| SoundResponseChance | (1, 0.25), near/strong to far/weak |
| SoundRetargetCooldownSeconds | 6s; also throttles a probabilistic no-response decision |
| SoundStrongerRatio | 1.25; received strength relative to accepted sound |
| SoundNewSourceDistance | 12m horizontal displacement for a newer, comparably audible source |

Normalize distance `t = clamp((distance-near)/(far-near),0,1)`. Error radius interpolates min/max using `t*t`, a continuous curve matching requested increasing uncertainty. Commitment/response use `weakness = max(t, 1-clamp(receivedIntensity,0,1))`. Close but quiet shots can therefore be weak evidence. Weights are sanitized/normalized by the shared helper.

Illustrative rifle at loudness 1 (calculated policy, not runtime measurements):

| Distance | Maximum error | Response chance | Thorough chance if responding |
| --- | --- | --- | --- |
| 20m | 1.17m | 88% | 68% |
| 50m | 3.72m | 70% | 49% |
| 75m | 8.18m | 51% | 29% |
| 100m | 14.77m | 33% | 10% |

Rifle emission radius is now 130m, MG 120m, both on existing ShotEmission profiles at loudness 1. Minimum intensity reduces actual response reach further (approximately 119.6m/110.4m). No playback volume or audio cue range changed. Shared deadlines still cap travel at the existing InvestigationMaxSeconds (60s by default); thorough commitment does not guarantee distant arrival, especially for injured slow actors.

## Repeated gunfire and cost

Before retargeting an active/pending sound episode, a newer sequence must pass cooldown AND be at least 25% stronger, OR be displaced at least 12m and retain at least 90% of prior strength. Comparable fire at the same location cannot reroll or extend the episode, even after cooldown. Different weapons at the same location are treated by the same evidence rule. After an episode finishes a later shot can start another. Rejected chance attempts reserve cooldown so automatic fire does not reroll response every shot.

Broadcast remains O(active listeners) per discrete event, with cheap gates and no scene/group scan, raycast, material writes or physics objects. Near-constant gunfire still dispatches one cheap callback per listener per shot; profiling must determine whether a future spatial index is needed. Pending state is consumed only on the existing brain timer. Movement wakes only when the shared episode requires it and retains normal settling/sleep. Death/corpse handoff and unload unsubscribe; corpses never listen.

## Validation and local handoff

Source/lifecycle/priority review, C# loop/whitespace and resource reference checks plus a small numeric policy model were performed. No Godot/.NET binary is available here: compilation, actual native movement, both-view gameplay and horde frame-time measurements are **unverified**. One HE1 commit only; no test scenes added.

Locally run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`:

1. Fire behind normal zombies/runners outside vision range; compare 20/50/75/100m and beyond hearing radius. Use misses to isolate hearing from bullet-hit clues.
2. Several listeners should choose distinct fixed estimates and commitments. Move away unseen after firing; they must not follow the new player position.
3. Confirm real sight overrides sound and actual bullet-hit search prevents lower-priority sounds replacing it. Existing last-seen/hit probabilities and attack behaviour remain unchanged.
4. Hold automatic fire at one location: no continually changing destination. After cooldown, test a significantly stronger or displaced source; no response is also valid at weak ranges.
5. Check early/full search, looking, stuck timeout, settled wander anchoring, injured movement, dead/inactive listeners, scene exit/reload and no stale delegates/duplicate reception.
6. Compare idle and active horde frame time; mute/disable audio and ensure gameplay hearing remains functional. No performance improvement is claimed without measurements.

No HE2/HE3, acoustic rays, hearing grid, attack changes, new debug UI, rendering changes or movement overhaul is included.
