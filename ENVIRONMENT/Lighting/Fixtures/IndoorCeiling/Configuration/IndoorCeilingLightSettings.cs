// Shared low-energy indoor light configuration; independent of outdoor streetlamp schedule/settings.
using Godot;

[Tool]
[GlobalClass]
public partial class IndoorCeilingLightSettings : Resource
{
    [ExportGroup("Illumination")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Color LightColor { get; set; } = new(0.96f, 0.975f, 1f);
    [Export(PropertyHint.Range, "0,3,0.05")] public float SpotEnergy { get; set; } = 1.05f;
    [Export(PropertyHint.Range, "3,10,0.25")] public float SpotRange { get; set; } = 6.5f;
    [Export(PropertyHint.Range, "40,85,1")] public float SpotAngle { get; set; } = 67f;
    [Export(PropertyHint.Range, "0.4,3,0.1")] public float SpotFalloff { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0,0.15,0.01")] public float FogEnergy { get; set; } = 0f;

    [ExportGroup("Housing")]
    [Export] public Color FrameColor { get; set; } = new(0.76f, 0.79f, 0.81f);
    [Export(PropertyHint.Range, "0.2,2.5,0.1")] public float PanelEmission { get; set; } = 1.25f;
}
