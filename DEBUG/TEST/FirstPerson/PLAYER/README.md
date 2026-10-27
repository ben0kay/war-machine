# PLAYER

`FirstPersonPlayer.cs` adapts arrow-key and current camera yaw to the existing shared player physics/crouch. Third-person art is hidden; no duplicate movement or procedural mesh code.

FPS-only acceleration, braking, reversal and ground-contact settings now come from the camera's FirstPersonMaster. It publishes one eye pose after shared physics, including crouch. The shared controller's new velocity hook retains its old default formula for top-down.
