// Small closed kraft cardboard box for F1 catalogue preview and future IX1 handling.
// Lightweight primitive meshes are built once, with no ongoing processing.
using Godot;

[Tool]
public partial class CardboardBoxVisual : PlaceableVisual
{
    private const float Width = 0.42f, Height = 0.26f, Depth = 0.30f;
    private static StandardMaterial3D? _cardboard, _tape, _fold;

    // =========================================================
    // Create the visual geometry before shared sizing and shadow settings run.
    // Do not add static collision: IX1 will own the future movable body.
    public override void _Ready()
    {
        Node3D model = GetNode<Node3D>("Model");
        if (model.GetNodeOrNull<MeshInstance3D>("Body") == null)
        {
            AddBox(model, "Body", new Vector3(Width, Height, Depth),
                new Vector3(0, Height * 0.5f, 0), ref _cardboard, new Color("#947045"));
            AddBox(model, "PackingTape", new Vector3(0.046f, 0.0016f, Depth - 0.008f),
                new Vector3(0, Height + 0.001f, 0), ref _tape, new Color("#ae9161"));
            AddBox(model, "FrontFold", new Vector3(Width - 0.012f, 0.0012f, 0.002f),
                new Vector3(0, Height + 0.0007f, Depth * 0.5f - 0.010f),
                ref _fold, new Color("#755331"));
            AddBox(model, "BackFold", new Vector3(Width - 0.012f, 0.0012f, 0.002f),
                new Vector3(0, Height + 0.0007f, -Depth * 0.5f + 0.010f),
                ref _fold, new Color("#755331"));
        }
        base._Ready();
    }

    // =========================================================
    // Reuse shared materials rather than allocating one per box instance.
    private static void AddBox(Node3D parent, string name, Vector3 size, Vector3 position,
        ref StandardMaterial3D? material, Color colour)
    {
        material ??= new StandardMaterial3D
        {
            AlbedoColor = colour, Roughness = 0.95f, Metallic = 0.0f
        };
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = new BoxMesh { Size = size, Material = material },
            Position = position
        });
    }
}
