# Baseball bat — CM3

**Source implemented; awaiting local compilation/World and FPS acceptance (2026-10-28).** Press **3** to equip; **LMB click** swings once. Hold does not auto-repeat. **Q** remains the independent zero-damage shove; R does nothing with the bat. Press 1/2 to return to rifle/MG. Ammo HUD hides while holding the bat. The existing hotbar remains a static visual foundation; its decorative first-slot highlight is not an equipment indicator.

`BaseballBat_001.tscn` is the reusable prefab; `C/` builds the replaceable low-poly wood/tape mesh. `GripRight`/`GripLeft` support the existing two-bone grip solver for top-down hands. FPS uses two minimal attached placeholder hands; this is a procedural prototype, not a full imported first-person arm animation rig. The bat coils beside the shoulder, sweeps horizontally across the body, follows through, then returns to hold. Facing locks at swing acceptance; player movement carries the contact origin. Mouse turning cannot sweep the volume an extra time during the same attack. Camera pitch does not turn this horizontal bat attack into a vertical strike; stance changes its height.

## Master profile

`Configuration/BaseballBatMaster.tres`: damage **11**, physical; reach **1.65m**; arc **130°**; thickness **0.20m**; vertical tolerance **0.35m**; max **3 distinct actors** per swing; windup **0.22s**, active **0.20s**, recovery **0.35s**, cooldown **0.85s**. `Melee` impact strength **0.30**, duration **0.18s** reuses zombie flinch/recovery gating; this is a small stagger, not Q's shove launch. Existing bullet-reaction enable/resistance/threshold settings also gate this first melee flinch integration. Per-view offsets and hold angles live in the same profile; no gun HP/ammo/tuning changes.

SeverPower=0: blunt hits damage/disable ELD1 limbs but do not amputate them. Armour resolves exactly once through the existing receiver. Precise region follows the confirmed anatomy shape/line-of-contact, rather than randomly assigning a limb. A swing may hit torso or an obstructing arm depending on pose; head/leg strikes depend on stance, height and real geometry. Ordinary non-anatomical targets retain Unknown-region damage.

## Detection and lifecycle

The bat model is not a permanent solid collider. Its gameplay hitbox is a bounded broad sphere query filtered by the **angular segment crossed during the current active step**, range and height, followed by a cover ray to each eligible candidate. It is a conservative swept-sector approximation, not continuous mesh collision. Anatomical areas participate; registered movement capsules are excluded only from these primary sweep queries. Q keeps its existing single-contact body query. The nearest real ray contact must belong to the candidate actor: walls, closed doors and intervening bodies can obstruct targets. No damage through cover just because an overlap touched it.

At most 64 broad results and 16 cover rays per active step; no global/group scan. Reused dedup IDs prevent multiple limb shapes or repeated physics frames from damaging the same actor again; the authored cap applies across the whole swing. A long frame still covers the crossed angular interval. Very dense crowds can saturate the bounded candidates; no guarantee of nearest-target ordering under saturation. All contacts use shared DamagePacket/CombatImpact/HitTargetResolver. No extra idle raycasts, timers, rigid bodies or simulation. Cancellation on focus loss, menu/input block, death, switch and unload restores the model/grips and clears contacts; cooldown persists. Existing ELD0 pose synchronization remains unchanged.

## Local acceptance — existing worlds only

Run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`.

1. Select 3: see bat and hidden ammo HUD. Click LMB: see coil → horizontal side swing → follow-through/recovery, no shooting/ammo use. Test miss, rapid clicks, held click, moving/sprinting and crouched/prone height.
2. Hit one fresh unarmoured zombie: **100→89 HP once**, mild brief flinch. Hit two/three side-by-side: each takes 11 once; four in arc never exceed three accepted targets per swing. Put a wall/closed rear door between player and target: no damage. Same target overlapping multiple region shapes still loses only 11.
3. Repeat targeted limb hits and check ELD logs; bat should damage/disable, never sever. Check a zombie with an already missing limb: no phantom region contact. Repeat on runner/regular variants, including lethal bat hit preserving existing missing limbs in the corpse.
4. Q with bat: separate zero-damage shove, no simultaneous primary hit. Interrupt windup/active/recovery with 1/2, Escape in FPS, inventory/menu, focus loss and death: no later ghost damage or stuck pose. Switch back to both guns and verify fire/reload/recoil, ammo HUD and Q unchanged.

Only focused source/resource/whitespace and sweep-math checks were performed here; Godot/.NET are unavailable. Actual compilation, visual swing/grip fit, multi-hit physics, cover, flinch feel and performance remain **unverified**. No extra test scenes, inventory/loot, knife, combos, zombie outgoing attacks or limb locomotion were implemented.
