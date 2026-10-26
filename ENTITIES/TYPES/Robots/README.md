# Robots

RM1–RM4 implement the original procedural quadruped in `Robot_001/`. Robot-only reusable visual gait lives in `Shared/Movement/MechanicalLegs.cs`. Shared actor lifecycle stays in `ENTITIES/Shared`; generic patrol/sensor live in ENTITIES/Shared; independent beam VFX lives in Shared/Scanning. TestWorld uses the complete encounter. See Robot_001/README.md for tuning, validation and limitations.

The gait is a plain C# helper driven by one RobotController callback. It accepts explicit rig references and numerical settings, with an optional injected foot-target correction function. It does not depend on RobotSettings, player nodes or Terrain3D. No limb has its own script or collider.
