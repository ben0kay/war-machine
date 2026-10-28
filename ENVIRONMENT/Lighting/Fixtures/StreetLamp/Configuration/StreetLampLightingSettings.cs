// Reusable native streetlamp illumination; independent of the physical lamp pole/housing.
using Godot;

[Tool]
[GlobalClass]
public partial class StreetLampLightingSettings : Resource
{
    [ExportGroup("Light controls")]
    [Export] public bool LightsEnabled { get; set; } = true;
    [Export] public Color LightColor { get; set; } = new(1.0f, 0.82f, 0.61f);
    [Export(PropertyHint.Range, "0,6,0.05")] public float SpotEnergy { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "6,20,0.5")] public float SpotRange { get; set; } = 13f;
    [Export(PropertyHint.Range, "20,75,1")] public float SpotHalfAngle { get; set; } = 51f;
    [Export(PropertyHint.Range, "0.2,3,0.1")] public float SpotFalloff { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.3,4,0.1")] public float ConeEdgeSoftness { get; set; } = 1.45f;
    [Export] public bool SpotShadows { get; set; } = false;

    [ExportGroup("Dispersed fill")]
    [Export] public bool FillEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,2,0.05")] public float FillEnergy { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "3,13,0.5")] public float FillRange { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.2,3,0.1")] public float FillFalloff { get; set; } = 1.0f;

    [ExportGroup("Fog")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float SpotFogEnergy { get; set; } = 0.25f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float FillFogEnergy { get; set; } = 0.05f;
}
