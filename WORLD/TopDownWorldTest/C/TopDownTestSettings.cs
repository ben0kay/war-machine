// Test-only authoring controls. The preview copies this resource and never edits normal-world masters.
using Godot;

[GlobalClass]
public partial class TopDownTestSettings : Resource
{
    public const string TerrainDirectory = "res://WORLD/TopDownWorldTest/Terrain/Data";
    public const int TerrainSize = 1024;
    [ExportGroup("Preview terrain — bake time")]
    [Export] public int Seed { get; set; } = 48193;
    [Export] public Vector2 SpawnXZ { get; set; } = new(512, 512);
    [Export] public float GroundHeight { get; set; } = 4;
    [Export] public float Relief { get; set; } = 0.15f;
    [Export] public Vector2 HillOffset { get; set; } = new(30, -10);
    [Export] public float HillHeight { get; set; } = 5;
    [Export] public float HillRadius { get; set; } = 24;
    [ExportGroup("Player")]
    [Export] public float WalkSpeed { get; set; } = 5;
    [Export] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export] public float RunMultiplier { get; set; } = 1.6f;
    [Export] public float Acceleration { get; set; } = 24;
    [Export] public float Gravity { get; set; } = 20;
    [Export] public float GaitRate { get; set; } = 1.8f;
    [ExportGroup("Preview lighting")]
    [Export] public float AmbientLightEnergy { get; set; } = 0.8f;
    [Export] public float MoonlightEnergy { get; set; } = 0.8f;
    [ExportGroup("Camera projection")]
    [Export] public bool UsePerspective { get; set; }
    [Export] public float PerspectiveFov { get; set; } = 65;
    [ExportGroup("Orthographic camera")]
    [Export] public float CameraYawDegrees { get; set; } = 45;
    [Export] public float CameraPitchDegrees { get; set; } = 55;
    [Export] public float CameraDistance { get; set; } = 36;
    [Export] public float CameraSize { get; set; } = 48;
    [Export] public float MinimumZoom { get; set; } = 20;
    [Export] public float MaximumZoom { get; set; } = 80;
    [Export] public float ZoomStep { get; set; } = 4;
    [Export] public float FollowSmoothing { get; set; } = 8;
}
