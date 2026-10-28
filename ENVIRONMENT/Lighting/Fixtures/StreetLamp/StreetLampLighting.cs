// Shared streetlamp lighting, night glow cone and LED. Registration is event-driven; no lamp tick.
using Godot;

[Tool]
public partial class StreetLampLighting : Node3D
{
    // Only registered runtime lamps are scheduled; disabled editor preview remains unaffected.
    public static event System.Action<StreetLampLighting>? Available;
    public static event System.Action<StreetLampLighting>? Removed;

    [Export] public StreetLampLightingSettings Settings { get; set; } = null!;

    private SpotLight3D _downlight = null!;
    private OmniLight3D _fill = null!;
    private MeshInstance3D? _visibleCone;
    private StandardMaterial3D? _ledMaterial;
    private bool _enabled;
    private bool _registered;

    // Cone geometry and shader are shared across all instances; scale the mesh to the authored preset.
    private static readonly CylinderMesh ConeMesh = new()
    {
        TopRadius = 0.025f, BottomRadius = 0.5f, Height = 1f,
        RadialSegments = 24, Rings = 1, CapTop = false, CapBottom = false
    };

    // =========================================================
    // A scene may instance this at any luminaire head; setup and registration are one-time.
    public override void _Ready()
    {
        _downlight = GetNode<SpotLight3D>("Downlight");
        _fill = GetNode<OmniLight3D>("AmbientFill");
        ConfigureLights();
        SetProcess(false); SetPhysicsProcess(false);
        if (!Engine.IsEditorHint()) { AddToGroup("streetlamp_scheduled"); _registered = true; Available?.Invoke(this); }
    }

    // =========================================================
    // The fixture supplies its unique LED material when the physical pole finishes _Ready.
    public void BindLedPanel(StandardMaterial3D material)
    {
        _ledMaterial = material;
        UpdateLedAppearance();
    }

    // =========================================================
    // OFF genuinely disables all native lights, LED glow and the transparent visual cone.
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
        if (GodotObject.IsInstanceValid(_visibleCone))
            _visibleCone!.Visible = enabled && (Settings?.VisibleConeEnabled ?? true)
                && (Settings?.VisibleConeOpacity ?? 0) > 0;
        UpdateLedAppearance();
    }

    // =========================================================
    // Native spotlight/fill remain as authored; a single unlit transparent cone is optional.
    private void ConfigureLights()
    {
        var s = Settings ?? new StreetLampLightingSettings();
        _downlight.Position = Vector3.Zero;
        _downlight.RotationDegrees = new Vector3(-90f, 0, 0);
        _downlight.LightColor = s.LightColor;
        _downlight.LightEnergy = Mathf.Clamp(s.SpotEnergy, 0f, 6f);
        _downlight.SpotRange = Mathf.Clamp(s.SpotRange, 6f, 20f);
        _downlight.SpotAngle = Mathf.Clamp(s.SpotHalfAngle, 20f, 85f);
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

        CreateVisibleCone(s);
        SetEnabled(s.LightsEnabled);
    }

    // =========================================================
    // No per-lamp shader compilation, extra light, collision, shadow or _Process callback.
    private void CreateVisibleCone(StreetLampLightingSettings s)
    {
        if (!s.VisibleConeEnabled || s.VisibleConeOpacity <= 0f) return;
        float length = Mathf.Clamp(s.VisibleConeLength, 1f, 12f);
        float radius = Mathf.Clamp(s.VisibleConeBaseRadius, 0.5f, 7f);
        var shader = GD.Load<Shader>("res://ENVIRONMENT/Lighting/Fixtures/StreetLamp/VisibleCone.gdshader");
        if (shader == null) { GD.PushWarning("StreetLampLighting: missing visible-cone shader."); return; }
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("beam_color", s.LightColor);
        material.SetShaderParameter("beam_opacity", Mathf.Clamp(s.VisibleConeOpacity, 0f, 0.12f));
        _visibleCone = new MeshInstance3D
        {
            Name = "VisibleLightCone", Mesh = ConeMesh, MaterialOverride = material,
            Position = new Vector3(0, -length * 0.5f, 0),
            Scale = new Vector3(radius * 2f, length, radius * 2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false
        };
        AddChild(_visibleCone);
    }

    // =========================================================
    // Physical fixtures can report their head height once after placement.
    // Extend the cone slightly below the fixture's ground pivot, so the actual
    // terrain occludes the open end rather than exposing a floating light cutoff.
    // Standalone fixtures retain VisibleConeLength as their default length.
    public void FitVisibleConeToGround(float headHeightAboveBase)
    {
        if (!GodotObject.IsInstanceValid(_visibleCone) || !float.IsFinite(headHeightAboveBase)) return;
        var s = Settings ?? new StreetLampLightingSettings();
        float overlap = Mathf.Clamp(s.VisibleConeGroundOverlap, 0.1f, 2f);
        float length = Mathf.Clamp(Mathf.Max(0f, headHeightAboveBase) + overlap, 1f, 12f);
        float diameter = Mathf.Clamp(s.VisibleConeBaseRadius, 0.5f, 7f) * 2f;
        _visibleCone!.Position = new Vector3(0, -length * 0.5f, 0);
        _visibleCone.Scale = new Vector3(diameter, length, diameter);
    }

    // =========================================================
    // One per-instance LED material avoids mutating other lamps on enable/disable.
    private void UpdateLedAppearance()
    {
        if (_ledMaterial == null) return;
        var s = Settings ?? new StreetLampLightingSettings();
        _ledMaterial.AlbedoColor = _enabled ? s.LightColor : new Color(0.16f, 0.16f, 0.16f);
        _ledMaterial.Emission = s.LightColor;
        _ledMaterial.EmissionEnabled = _enabled;
        _ledMaterial.EmissionEnergyMultiplier = 1.2f;
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (_registered) { _registered = false; Removed?.Invoke(this); }
    }
}
