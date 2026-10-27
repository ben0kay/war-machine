# CAMERA

`FirstPersonCamera.cs` replaces overhead follow with eye-height perspective and mouse look. Settings are in ../CONFIGURATION/FirstPersonMaster.tres. No per-frame follow loop.

Event-driven render translation between physics snapshots replaces tick-only positioning. Mouse rotation uses ScreenRelative immediately and preserves InvertMouseY=true. Rendering sleeps when settled; no camera head bob. See parent README for tuning and pending local checks.
