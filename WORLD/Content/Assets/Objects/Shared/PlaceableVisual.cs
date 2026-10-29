// Reusable GLB placeable: size + SOC1A shadow policy + optional SOC0 static collision, once.
// No geometry rebuilding, persistent physics simulation, or per-object idle callbacks.
using Godot;

[Tool]
public partial class PlaceableVisual : Node3D
{
    [Export] public PlaceableSizeSettings Settings { get; set; } = null!;
    [ExportGroup("Static collision (SOC0)")]
    // Inspector enum hint uses an int: the F1 menu only enumerates true C# enum
    // variant properties (bed colour/table finish), never this collision selector.
    [Export(PropertyHint.Enum, "None,AutoBox,CustomBoxes")] public int CollisionMode { get; set; }
    [Export] public Godot.Collections.Array<StaticCollisionBox> CollisionBoxes { get; set; } = new();

    // =========================================================
    // Canonical model sizing; the optional static physics body is a root sibling,
    // so its primitive boxes never inherit unsupported nonuniform shape scaling.
    public override void _Ready()
    {
        var model = GetNodeOrNull<Node3D>("Model");
        if (model != null)
        {
            Vector3 raw = Settings?.SizeMultiplier ?? Vector3.One;
            Vector3 scale = new(Mathf.Max(0.05f, raw.X), Mathf.Max(0.05f, raw.Y), Mathf.Max(0.05f, raw.Z));
            model.Scale = scale;
            // Runtime-only: leave imported/editor resources and default settings untouched.
            if (!Engine.IsEditorHint())
            {
                int shadowMode = Settings?.ShadowMode ?? 0;
                if (shadowMode == 1 || shadowMode == 2)
                    ApplyShadowMode(model, shadowMode == 1
                        ? GeometryInstance3D.ShadowCastingSetting.Off
                        : GeometryInstance3D.ShadowCastingSetting.On);
                if (CollisionMode != 0)
                    StaticObjectCollision.Build(this, model, CollisionMode, CollisionBoxes, scale);
            }
        }
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // SOC1A: apply the selected shadow policy once to imported render meshes only.
    // Inherit (0) never visits or rewrites imported shadow settings.
    private static void ApplyShadowMode(Node root, GeometryInstance3D.ShadowCastingSetting mode)
    {
        if (root is MeshInstance3D mesh) mesh.CastShadow = mode;
        foreach (Node child in root.GetChildren()) ApplyShadowMode(child, mode);
    }
}
