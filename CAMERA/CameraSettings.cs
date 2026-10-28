// CameraSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class CameraSettings : Resource
{
    [ExportGroup("Scenery obstruction")]
    [Export] public bool ObstructionFadeEnabled { get; set; } = true;
    [Export] public float ObstructedOpacity { get; set; } = 0.5f;
    [Export] public float ObstructionRadius { get; set; } = 0.45f;
    [Export] public float ObstructionChecksPerSecond { get; set; } = 8;
    [Export] public float ObstructionFadeSeconds { get; set; } = 0.2f;
    [ExportGroup("Projection")]
    [Export] public bool UsePerspective { get; set; }
    [Export] public float PerspectiveFov { get; set; } = 65;
    [Export(PropertyHint.Range, "64,2048,1")] public float FarClipDistance { get; set; } = 256;
    [ExportGroup("Angle and follow")]
    [Export] public float CameraYawDegrees { get; set; } = 45;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float OrbitDragSensitivityDegrees { get; set; } = 0.25f;
    [Export] public float CameraPitchDegrees { get; set; } = 35;
    [Export] public float CameraDistance { get; set; } = 25;
    [Export] public float CameraSize { get; set; } = 20;
    [ExportGroup("Zoom limits")]
    [Export] public float MinimumZoom { get; set; } = 15;
    [Export] public float MaximumZoom { get; set; } = 20;
    [Export] public float MinimumPerspectiveDistance { get; set; } = 15;
    [Export] public float MaximumPerspectiveDistance { get; set; } = 25;
    [Export] public float ZoomStep { get; set; } = 4;
    [Export] public float FollowSmoothing { get; set; } = 8;
}
