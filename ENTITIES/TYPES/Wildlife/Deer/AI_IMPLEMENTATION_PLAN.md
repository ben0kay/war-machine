# Deer AI and future hunting — phase checkpoints

**D-AI1: implemented in source, awaiting Godot validation.** Initial deer was stationary, and user requested a deer that walks, notices the player, runs a configurable distance and then wanders around a **new physical anchor**, eventually challenging to hunt.

**D-AI1 implemented:** reuse EntityCore, HealthComponent, VisibilityResolver and player PerceptionHead/Torso. Extract shared bounded ground/clearance/sweep destination sampling from zombie to `ENTITIES/Shared/Movement/GroundGoalSampler`. Deer-specific low-rate wander/detection/flight state with configurable values, damage-triggered fleeing, one moving-only CharacterBody3D controller, and four simple leg pivots. Existing world test deer now calls `InitializeTarget` after placement.

**Not implemented:** animal hearing, navigation around buildings, complex animations, herds, biome population, carcasses/loot, meat, cooking, food/hunger or persistence. Future D-AI2 can improve navigation and panic/awareness, D-AI3 can add hearing; HUNT0 will add meat/carcass/harvesting only if requested.

**Local check:** C# compile, run active world top-down/FPS, test several wander/flee cycles, shots, safe stops/new anchor, slopes/collision, deactivate/scene reload and performance; all currently unverified.

## D-AI1a follow-up: stationary deer (2026-10-28)

Source review identified a plausible stuck-AI cause: requiring full-height capsule CastMotion over a long uneven terrain path can reject all proposed wander and flee goals. GroundGoalSampler now has opt-in relaxed mode for deer only (wider support ray, destination body clearance, chest-height solid obstruction ray, real MoveAndSlide collisions). Zombie calls stay strict by default. Deer tries a short extra wander radius and symmetric escape directions, and prints bounded first-start/first-goal/failure diagnostics. This is a proposed source fix, not proof of Godot runtime motion. Verify actual movement after build, monitor [Deer] Output lines, and report any continued issues.

The procedural deer mesh also received less sausage-like proportions, slender head/neck/hooves and antlers disabled by default. Hunting, animals hearing, carcasses and pathfinding remain separate phases.
