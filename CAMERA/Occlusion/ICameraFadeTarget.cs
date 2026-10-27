// Scenery-only rendering contract. Entities never register as camera obstruction targets.
using Godot;
using System.Collections.Generic;

public interface ICameraFadeTarget
{
    bool IsValid { get; }
    void SetCameraOpacity(float opacity);
}

public interface ICameraOcclusionProvider
{
    event System.Action? OcclusionChanged;
    void CollectObstructions(Vector3 from, Vector3 to, float radius, HashSet<ICameraFadeTarget> results);
}
