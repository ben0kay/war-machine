// Reusable parking/street lamp. One-time low-poly metal model and two native light sources.
// The downlight illuminates the ground; a weaker omni adds diffuse spill by the lamp head.
// No per-frame polling, timer, flicker or world placement. Runtime enable/disable is explicit.
using Godot;

[Tool]
public partial class StreetLampMesh : Node3D
{
    [Export] public StreetLampSettings Settings { get; set; } = null!;

    private SpotLight3D _downlight = null!;
    private OmniLight3D _fill = null!;
    private StandardMaterial3D? _bulbMaterial;
    private bool _lightsOn;

    // Reuse the same box and tapered pole primitives across all instances.
    private static readonly CylinderMesh PoleMesh = new()
    {
        TopRadius = 0.068f, BottomRadius = 0.115f,
        Height = 1f, RadialSegments = 12
    };
    private static readonly CylinderMesh CableMesh = new()
    {
        TopRadius = 0.5f, BottomRadius = 0.5f,
        Height = 1f, RadialSegments = 10
    };

    // =========================================================
    // Light nodes are authored inside the scene; only their settings/positions are changed here.
    public override void _Ready()
    {
        _downlight = GetNode<SpotLight3D>("Lighting/Downlight");
        _fill = GetNode<OmniLight3D>("Lighting/AmbientFill");
        if (GetNodeOrNull<Node3D>("Generated") == null) BuildModel();
        ConfigureLights();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // An external future site/time-of-day controller can toggle this without ticking each lamp.
    // Setting the visible flags false also stops the native GPU lights, not just their colour.
    public void SetLampEnabled(bool enabled)
    {
        _lightsOn = enabled;
        if (!GodotObject.IsInstanceValid(_downlight) || !GodotObject.IsInstanceValid(_fill)) return;
        var s = Settings ?? new StreetLampSettings();
        _downlight.Visible = enabled && s.SpotEnergy > 0f;
        _fill.Visible = enabled && s.FillEnabled && s.FillEnergy > 0f;
        if (_bulbMaterial != null) _bulbMaterial.EmissionEnabled = enabled;
    }

    // =========================================================
    // Copy the typed master once; no live config binding or per-frame light property writes.
    private void ConfigureLights()
    {
        var s = Settings ?? new StreetLampSettings();
        float height = Mathf.Clamp(s.PoleHeight, 4f, 8f);
        float reach = Mathf.Clamp(s.ArmReach, 0.6f, 1.7f);
        Vector3 bulbLocation = new(0, height - 0.48f, reach);

        // Godot SpotLight3D emits along local -Z; rotating -90 degrees around X points -Z down.
        _downlight.Position = bulbLocation;
        _downlight.RotationDegrees = new Vector3(-90f, 0, 0);
        _downlight.LightColor = s.LightColor;
        _downlight.LightEnergy = Mathf.Clamp(s.SpotEnergy, 0, 6f);
        _downlight.SpotRange = Mathf.Clamp(s.SpotRange, 6f, 20f);
        _downlight.SpotAngle = Mathf.Clamp(s.SpotHalfAngle, 20f, 75f);
        _downlight.SpotAttenuation = Mathf.Clamp(s.SpotFalloff, 0.2f, 3f);
        _downlight.SpotAngleAttenuation = Mathf.Clamp(s.ConeEdgeSoftness, 0.3f, 4f);
        _downlight.LightVolumetricFogEnergy = Mathf.Clamp(s.SpotFogEnergy, 0, 1f);
        _downlight.ShadowEnabled = s.SpotShadows;

        // Spherical secondary light: deliberately dim and shadowless to avoid doubling spot costs.
        _fill.Position = bulbLocation;
        _fill.LightColor = s.LightColor;
        _fill.LightEnergy = Mathf.Clamp(s.FillEnergy, 0, 2f);
        _fill.OmniRange = Mathf.Clamp(s.FillRange, 3f, 13f);
        _fill.OmniAttenuation = Mathf.Clamp(s.FillFalloff, 0.2f, 3f);
        _fill.LightVolumetricFogEnergy = Mathf.Clamp(s.FillFogEnergy, 0, 1f);
        _fill.ShadowEnabled = false;

        SetLampEnabled(s.LightsEnabled);
    }

    // =========================================================
    // Physically solid base/pole, raised arm and recessed light fixture.
    // The tall pole is placed at local Y=0 so it can be grounded independently of the site.
    private void BuildModel()
    {
        var s = Settings ?? new StreetLampSettings();
        float h = Mathf.Clamp(s.PoleHeight, 4f, 8f);
        float reach = Mathf.Clamp(s.ArmReach, 0.6f, 1.7f);
        var generated = StructureGeometry.Group(this, "Generated");
        var art = StructureGeometry.Group(generated, "Art");
        var body = StructureGeometry.Body(generated);
        var polePaint = StructureGeometry.Paint(s.MetalColor, 0.55f);
        var housingPaint = StructureGeometry.Paint(s.HousingColor, 0.38f);
        var dark = StructureGeometry.Paint(s.MetalColor.Darkened(0.30f), 0.18f);
        var trim = StructureGeometry.Paint(s.HousingColor.Lightened(0.17f), 0.36f);

        StructureGeometry.Box(art, body, "FootPlate", new(0, 0.09f, 0),
            new(0.54f, 0.18f, 0.54f), dark);
        StructureGeometry.Box(art, null, "FootPlateTrim", new(0, 0.19f, 0),
            new(0.39f, 0.055f, 0.39f), trim);
        StructureGeometry.Box(art, body, "LowerPolePedestal", new(0, 0.39f, 0),
            new(0.22f, 0.54f, 0.22f), polePaint);

        art.AddChild(new MeshInstance3D
        {
            Name = "TaperedLampPole", Mesh = PoleMesh, Position = new Vector3(0, h * 0.5f, 0),
            Scale = new Vector3(1, h, 1), MaterialOverride = polePaint
        });
        body.AddChild(new CollisionShape3D
        {
            Name = "PoleCollision", Position = new Vector3(0, h * 0.5f, 0),
            Shape = new CylinderShape3D { Height = h, Radius = 0.115f }
        });
        StructureGeometry.Box(art, null, "PoleCap", new(0, h - 0.055f, 0),
            new(0.19f, 0.13f, 0.19f), dark);

        // Angled arm has a small rise above the pole, then slopes towards the reflector head.
        StructureGeometry.Box(art, null, "UpperMount", new(0, h - 0.10f, 0.12f),
            new(0.16f, 0.19f, 0.31f), housingPaint);
        StructureGeometry.Box(art, null, "SweptArm", new(0, h - 0.15f, reach * 0.5f),
            new(0.095f, 0.095f, reach), polePaint, 0.13f);
        StructureGeometry.Box(art, null, "NeckCoupler", new(0, h - 0.27f, reach),
            new(0.18f, 0.20f, 0.20f), dark);

        // A broad, shallow LED street fixture with an inset illuminated underside.
        StructureGeometry.Box(art, null, "LampHousing", new(0, h - 0.32f, reach),
            new(0.79f, 0.18f, 0.58f), housingPaint);
        StructureGeometry.Box(art, null, "LampCasingTop", new(0, h - 0.215f, reach),
            new(0.68f, 0.047f, 0.47f), trim);
        StructureGeometry.Box(art, null, "LightBezel", new(0, h - 0.425f, reach),
            new(0.64f, 0.025f, 0.45f), dark);

        _bulbMaterial = new StandardMaterial3D
        {
            AlbedoColor = s.LightColor, Roughness = 0.65f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Emission = s.LightColor, EmissionEnabled = s.LightsEnabled,
            EmissionEnergyMultiplier = 1.2f
        };
        StructureGeometry.Box(art, null, "LEDPanel", new(0, h - 0.446f, reach),
            new(0.56f, 0.014f, 0.37f), _bulbMaterial);

        // Tiny mounting/cable details are one-time static art; no additional collision shapes.
        art.AddChild(new MeshInstance3D
        {
            Name = "WiringConduit", Mesh = CableMesh, Position = new Vector3(0, h - 0.14f, 0.17f),
            RotationDegrees = new Vector3(74f, 0, 0),
            Scale = new Vector3(0.028f, 0.26f, 0.028f), MaterialOverride = dark
        });
    }
}
