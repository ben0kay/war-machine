// CameraSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class CameraSettings : Resource
{
    [ExportGroup("Projection")]
    [Export] public bool UsePerspective { get; set; }
    [Export] public float PerspectiveFov { get; set; } = 65;
    [ExportGroup("Angle and follow")]
    [Export] public float CameraYawDegrees { get; set; } = 45;
    [Export] public float CameraPitchDegrees { get; set; } = 55;
    [Export] public float CameraDistance { get; set; } = 36;
    [Export] public float CameraSize { get; set; } = 48;
    [ExportGroup("Zoom limits")]
    [Export] public float MinimumZoom { get; set; } = 20;
    [Export] public float MaximumZoom { get; set; } = 80;
    [Export] public float ZoomStep { get; set; } = 4;
    [Export] public float FollowSmoothing { get; set; } = 8;
}
