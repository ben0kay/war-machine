# StreetLampMesh — physical fixture only

`StreetLampMesh.cs` builds the pole, base, arm, housing, recessed LED panel and static collision once. The root scene instances `ENVIRONMENT/Lighting/Fixtures/StreetLamp/StreetLampLighting.tscn` at `Lighting`. The parent moves the lighting instance to the head position (derived from `PoleHeight` / `ArmReach`) and passes a per-instance LED panel material for emission/tint.

No spotlight, omni, fog, shadow or lighting settings are configured here. `SetLampEnabled(bool)` still delegates to `StreetLampLighting.SetEnabled(bool)` as a stable site API. Both classes keep processing disabled while idle, with no flicker or other timer.
