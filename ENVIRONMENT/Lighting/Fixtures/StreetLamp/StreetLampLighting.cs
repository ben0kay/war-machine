// Self-contained streetlamp illumination: downward spotlight, subtle omni spill and LED emission.
// The physical fixture only supplies head position and a material. No timers, polling or frame callbacks.
using Godot;

[Tool]
public partial class StreetLampLighting : Node3D
{
    [Export] public StreetLampLightingSettings Settings { get; set; } = null!;

    private SpotLight3D _downlight = null!;
    private OmniLight3D _fill = null!;
    private StandardMaterial3D? _ledMaterial;
    private bool _enabled;

    // =========================================================
    // A scene may instance this at any luminaire head: no knowledge of pole height or world sites.
    public override void _Ready()
    {
        _downlight = GetNode<SpotLight3D>("Downlight");
        _fill = GetNode<OmniLight3D>("AmbientFill");
        ConfigureLights();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // The fixture passes in its unique LED material after the lighting subscene is ready.
    // No material resource is mutated if the fixture has no physical LED panel.
    public void BindLedPanel(StandardMaterial3D material)
    {
        _ledMaterial = material;
        UpdateLedAppearance();
    }

    // =========================================================
    // Future site/night controls can directly switch native lights and LED emission.
    // OFF disables both GPU lights; no light is left invisibly processing.
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (GodotObject.IsInstanceValid(_downlight))
            _downlight.Visible = enabled && _downlight.LightEnergy > 0f;
        if (GodotObject.IsInstanceValid(_fill))
        {
            var s = Settings ?? new StreetLampLightingSettings();
            _fill.Visible = enabled && s.FillEnabled && _fill.LightEnergy > 0f;
        }
        UpdateLedAppearance();
    }

    // =========================================================
    // Bright and wide downward illumination plus low-energy, shadowless omni spill.
    private void ConfigureLights()
    {
        var s = Settings ?? new StreetLampLightingSettings();
        // Native SpotLight3D beams along -Z. Rotate -90° around X to aim at local -Y.
        _downlight.Position = Vector3.Zero;
        _downlight.RotationDegrees = new Vector3(-90f, 0, 0);
        _downlight.LightColor = s.LightColor;
        _downlight.LightEnergy = Mathf.Clamp(s.SpotEnergy, 0f, 6f);
        _downlight.SpotRange = Mathf.Clamp(s.SpotRange, 6f, 20f);
        _downlight.SpotAngle = Mathf.Clamp(s.SpotHalfAngle, 20f, 75f);
        _downlight.SpotAttenuation = Mathf.Clamp(s.SpotFalloff, 0.2f, 3f);
        _downlight.SpotAngleAttenuation = Mathf.Clamp(s.ConeEdgeSoftness, 0.3f, 4f);
        _downlight.LightVolumetricFogEnergy = Mathf.Clamp(s.SpotFogEnergy, 0f, 1f);
        _downlight.ShadowEnabled = s.SpotShadows;

        _fill.Position = Vector3.Zero;
        _fill.LightColor = s.LightColor;
        _fill.LightEnergy = Mathf.Clamp(s.FillEnergy, 0f, 2f);
        _fill.OmniRange = Mathf.Clamp(s.FillRange, 3f, 13f);
        _fill.OmniAttenuation = Mathf.Clamp(s.FillFalloff, 0.2f, 3f);
        _fill.LightVolumetricFogEnergy = Mathf.Clamp(s.FillFogEnergy, 0f, 1f);
        _fill.ShadowEnabled = false;

        SetEnabled(s.LightsEnabled);
    }

    // =========================================================
    // Only a per-instance panel is supplied, so changing the glow cannot change other lamps.
    private void UpdateLedAppearance()
    {
        if (_ledMaterial == null) return;
        var s = Settings ?? new StreetLampLightingSettings();
        _ledMaterial.AlbedoColor = _enabled ? s.LightColor : new Color(0.16f, 0.16f, 0.16f);
        _ledMaterial.Emission = s.LightColor;
        _ledMaterial.EmissionEnabled = _enabled;
        _ledMaterial.EmissionEnergyMultiplier = 1.2f;
    }
}
