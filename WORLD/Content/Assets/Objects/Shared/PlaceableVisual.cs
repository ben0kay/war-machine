// Reusable GLB placeable: apply size once, optionally generate cheap static collision once.
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
            // Tool previews remain ephemeral; create actual colliders only in the game.
            if (!Engine.IsEditorHint() && CollisionMode != 0)
                StaticObjectCollision.Build(this, model, CollisionMode, CollisionBoxes, scale);
        }
        SetProcess(false); SetPhysicsProcess(false);
    }
}
