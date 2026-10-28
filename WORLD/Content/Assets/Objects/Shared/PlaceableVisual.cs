// Reusable GLB-scene presentation: apply each asset size master once; no idle processing.
using Godot;

[Tool]
public partial class PlaceableVisual : Node3D
{
    [Export] public PlaceableSizeSettings Settings { get; set; } = null!;

    // =========================================================
    // Scale only the imported model container; preserve scene-root transform and shared GLB assets.
    public override void _Ready()
    {
        var model = GetNodeOrNull<Node3D>("Model");
        if (model != null)
        {
            Vector3 size = Settings?.SizeMultiplier ?? Vector3.One;
            model.Scale = new Vector3(Mathf.Max(0.05f, size.X),
                Mathf.Max(0.05f, size.Y), Mathf.Max(0.05f, size.Z));
        }
        SetProcess(false); SetPhysicsProcess(false);
    }
}
