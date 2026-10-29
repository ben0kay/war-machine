// Fridge001: colour/finish variants for one shared imported GLB.
// Keep body and two doors as distinct meshes for future hinge work; no mesh generation or idle ticks.
using Godot;

[Tool]
public partial class FridgeVisual : PlaceableVisual
{
    public enum FridgeFinish { WarmWhite, BrushedSilver, DarkGraphite, SeafoamGreen }
    [Export] public FridgeFinish FinishVariant { get; set; } = FridgeFinish.WarmWhite;

    // Imported GLB node names; one surface per node, no dependence on material index ordering.
    private const string ShellName = "FridgePaint_Shell";
    private const string UpperDoorName = "FridgePaint_UpperDoor";
    private const string LowerDoorName = "FridgePaint_LowerDoor";
    private MeshInstance3D? _shell, _upper, _lower;

    // Two shared materials per finish: chassis and both door panels.
    // Door handles/gaskets/ventilation retain their original neutral metallic/dark GLB materials.
    private static readonly StandardMaterial3D[][] Finishes =
    {
        new[] { Paint(new Color(0.78f, 0.78f, 0.73f), 0.08f, 0.76f),
                Paint(new Color(0.87f, 0.86f, 0.81f), 0.08f, 0.70f) },
        new[] { Paint(new Color(0.53f, 0.57f, 0.59f), 0.72f, 0.36f),
                Paint(new Color(0.64f, 0.67f, 0.69f), 0.75f, 0.33f) },
        new[] { Paint(new Color(0.17f, 0.19f, 0.20f), 0.20f, 0.68f),
                Paint(new Color(0.215f, 0.231f, 0.24f), 0.26f, 0.61f) },
        new[] { Paint(new Color(0.35f, 0.52f, 0.44f), 0.06f, 0.76f),
                Paint(new Color(0.41f, 0.59f, 0.49f), 0.06f, 0.69f) }
    };

    // =========================================================
    // Shared size and static SOC0 collision first, then appearance. No per-frame updates.
    public override void _Ready()
    {
        base._Ready();
        ApplyFinish();
    }

    // =========================================================
    // Explicit runtime palette changes affect only this instance's surface overrides.
    public void SetFinishVariant(FridgeFinish variant)
    {
        if (FinishVariant == variant) return;
        FinishVariant = variant;
        if (IsInsideTree()) ApplyFinish();
    }

    // =========================================================
    // Resolve imported mesh nodes once. Preserve separate door hinge hierarchies.
    private bool FindPaintMeshes()
    {
        if (GodotObject.IsInstanceValid(_shell) && GodotObject.IsInstanceValid(_upper)
            && GodotObject.IsInstanceValid(_lower)) return true;

        var model = GetNodeOrNull<Node3D>("Model");
        if (model == null) return false;
        foreach (Node node in model.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (node is not MeshInstance3D mesh) continue;
            string name = mesh.Name.ToString();
            if (name == ShellName) _shell = mesh;
            else if (name == UpperDoorName) _upper = mesh;
            else if (name == LowerDoorName) _lower = mesh;
        }
        return GodotObject.IsInstanceValid(_shell) && GodotObject.IsInstanceValid(_upper)
            && GodotObject.IsInstanceValid(_lower);
    }

    // =========================================================
    // Shared palette resources are immutable: do not modify imported GLB source materials.
    private void ApplyFinish()
    {
        if (!FindPaintMeshes())
        {
            GD.PushWarning("Fridge001: imported paint meshes not found; check GLB node names.");
            return;
        }

        int index = Mathf.Clamp((int)FinishVariant, 0, Finishes.Length - 1);
        _shell!.SetSurfaceOverrideMaterial(0, Finishes[index][0]);
        _upper!.SetSurfaceOverrideMaterial(0, Finishes[index][1]);
        _lower!.SetSurfaceOverrideMaterial(0, Finishes[index][1]);
    }

    // =========================================================
    private static StandardMaterial3D Paint(Color color, float metallic, float roughness) => new()
    { AlbedoColor = color, Metallic = metallic, Roughness = roughness };
}
