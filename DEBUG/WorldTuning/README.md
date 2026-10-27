# World tuning moved to shared Overlay

The common controller now lives in [../Overlay/DebugOverlay.cs](../Overlay/DebugOverlay.cs), with its reusable scene [../Overlay/DebugOverlay.tscn](../Overlay/DebugOverlay.tscn). Both top-down and FPS inherit the same scene and common controls. See [../Overlay/README.md](../Overlay/README.md) for adapters, lifecycle and pending local checks. Existing overhead rotation/zoom logic is retained; this refactor does not implement camera movement changes.
