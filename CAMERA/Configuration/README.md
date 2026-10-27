# Configuration / ownership

CameraMaster.tres uses CAMERA/CameraSettings.cs. Projection, FOV, pitch/yaw, distance, zoom and follow settings belong here; runtime tuning edits a copy.

## Top-down rotation and zoom defaults

`CameraYawDegrees` is now adjustable at runtime via the Camera panel's 0–360° rotation slider. The master keeps its initial 45° orientation. Orthographic `MinimumZoom=15` / `MaximumZoom=40`; perspective `MinimumPerspectiveDistance=15` / `MaximumPerspectiveDistance=50` (metres). The perspective bounds are now individually exposed rather than fixed in camera logic. `CameraSize=40` keeps the orthographic authoring default inside the reduced range. The FPS debug camera retains its independent configuration. See [Camera README](../README.md) for validation guidance.
