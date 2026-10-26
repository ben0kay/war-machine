# Robots

RM1 adds the original procedural quadruped in `Robot_001/`. Robot-only reusable visual gait lives in `Shared/Movement/MechanicalLegs.cs`. Shared actor lifecycle stays in `ENTITIES/Shared`; patrol, sensor and beam are not implemented.

The gait is a plain C# helper driven by one RobotController callback. It accepts explicit rig references and numerical settings, with an optional injected foot-target correction function. It does not depend on RobotSettings, player nodes or Terrain3D. No limb has its own script or collider.
