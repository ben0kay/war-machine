// Stationary streetlamp geometry and collision only; lighting behaviour/settings live in ENVIRONMENT/Lighting.
// The fixture forwards SetLampEnabled to its reusable lighting subscene without running any update loop.
using Godot;

[Tool]
public partial class StreetLampMesh : Node3D
{
    [Export] public StreetLampSettings Settings { get; set; } = null!;

    private StreetLampLighting _lighting = null!;
    private StandardMaterial3D? _bulbMaterial;

    // Reuse one pair of meshes for all fixture instances, including disabled lamps.
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
    // Position a self-contained light scene at the physical lamp head after building the model.
    // Child _Ready executes before parent _Ready, so the light scene is already configured.
    public override void _Ready()
    {
        _lighting = GetNode<StreetLampLighting>("Lighting");
        if (GetNodeOrNull<Node3D>("Generated") == null) BuildModel();
        var s = Settings ?? new StreetLampSettings();
        _lighting.Position = new Vector3(0, Mathf.Clamp(s.PoleHeight, 4f, 8f) - 0.48f,
            Mathf.Clamp(s.ArmReach, 0.6f, 1.7f));
        // The fixture knows its real head height; hide the beam's open end below the ground pivot.
        _lighting.FitVisibleConeToGround(_lighting.Position.Y);
        if (_bulbMaterial != null) _lighting.BindLedPanel(_bulbMaterial);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Preserve the existing public lamp toggle for future site/night scheduling.
    // Illumination, shadow state, fog and emission are owned by ENVIRONMENT/Lighting.
    public void SetLampEnabled(bool enabled)
    {
        if (GodotObject.IsInstanceValid(_lighting)) _lighting.SetEnabled(enabled);
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

        // The fixture owns this physical panel only; the lighting component supplies glow/tint.
        _bulbMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.16f, 0.16f, 0.16f), Roughness = 0.65f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = false
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
