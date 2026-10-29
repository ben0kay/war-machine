// Reusable indoor ceiling luminaire: native downward SpotLight3D, shared emissive low-poly casing.
// Unlike scheduled outdoor streetlamps this is always-on and uses no cone, shadow or per-frame loop.
using Godot;

[Tool]
public partial class IndoorCeilingLight : Node3D
{
    [Export] public IndoorCeilingLightSettings Settings { get; set; } = null!;
    [Export] public bool IsCorridor { get; set; } = false;

    // =========================================================
    // Parent building configures IsCorridor before adding this scene; the tool preview is real native light.
    public override void _Ready()
    {
        var s = Settings ?? new IndoorCeilingLightSettings();
        var beam = GetNode<SpotLight3D>("Downlight");
        beam.Position = new Vector3(0, -0.16f, 0);
        beam.RotationDegrees = new Vector3(-90, 0, 0);
        beam.LightColor = s.LightColor;
        beam.LightEnergy = s.Enabled ? Mathf.Clamp(s.SpotEnergy, 0f, 3f) : 0f;
        beam.SpotRange = Mathf.Clamp(s.SpotRange, 3f, 10f);
        beam.SpotAngle = Mathf.Clamp(s.SpotAngle, 40f, 85f);
        beam.SpotAttenuation = Mathf.Clamp(s.SpotFalloff, 0.4f, 3f);
        beam.LightVolumetricFogEnergy = Mathf.Clamp(s.FogEnergy, 0f, 0.15f);
        beam.ShadowEnabled = false;
        beam.Visible = s.Enabled;
        var art = GetNodeOrNull<Node3D>("Generated");
        if (art == null)
        {
            art = new Node3D { Name = "Generated" };
            AddChild(art);
            IndoorCeilingFixtureGeometry.Build(art, IsCorridor, s);
        }
        SetProcess(false); SetPhysicsProcess(false);
    }
}
