// Imported Table001 GLB material variants; the shared PlaceableVisual handles size without generating geometry.
using Godot;

[Tool]
public partial class TableVisual : PlaceableVisual
{
    public enum WoodFinish { WeatheredOak, DarkWalnut, FadedGrey }
    [Export] public WoodFinish FinishVariant { get; set; } = WoodFinish.WeatheredOak;

    // Table001 GLB ordered surfaces: top planks, alternating planks, legs/aprons, brass pegs.
    private const int WoodSurfaces = 3, TotalSurfaces = 4;
    private MeshInstance3D? _tableMesh;

    // Each finish shares exactly three static wood materials across all Table001 instances.
    private static readonly StandardMaterial3D[][] Finishes =
    {
        new[] { Paint(new Color(0.45f, 0.30f, 0.19f)), Paint(new Color(0.40f, 0.265f, 0.165f)), Paint(new Color(0.30f, 0.20f, 0.13f)) },
        new[] { Paint(new Color(0.27f, 0.16f, 0.10f)), Paint(new Color(0.235f, 0.14f, 0.085f)), Paint(new Color(0.17f, 0.10f, 0.065f)) },
        new[] { Paint(new Color(0.41f, 0.40f, 0.36f)), Paint(new Color(0.36f, 0.35f, 0.32f)), Paint(new Color(0.29f, 0.28f, 0.255f)) }
    };

    // =========================================================
    // Scene and imported GLB are ready before this method; no ongoing processing.
    public override void _Ready()
    {
        base._Ready();
        ApplyFinish();
    }

    // =========================================================
    // Explicit palette changes only reassign materials; mesh shape/resources remain shared.
    public void SetFinishVariant(WoodFinish variant)
    {
        if (FinishVariant == variant) return;
        FinishVariant = variant;
        if (IsInsideTree()) ApplyFinish();
    }

    // =========================================================
    // Cache the single imported mesh. Per-instance surface overrides never mutate GLB source materials.
    private void ApplyFinish()
    {
        if (!GodotObject.IsInstanceValid(_tableMesh))
            foreach (Node node in GetNode<Node3D>("Model").FindChildren("*", "MeshInstance3D", true, false))
                if (node is MeshInstance3D mesh && mesh.Mesh?.GetSurfaceCount() == TotalSurfaces)
                { _tableMesh = mesh; break; }

        if (!GodotObject.IsInstanceValid(_tableMesh))
        {
            GD.PushWarning("Table001: expected a four-surface imported GLB mesh for wood finishes.");
            return;
        }

        int finish = Mathf.Clamp((int)FinishVariant, 0, Finishes.Length - 1);
        for (int i = 0; i < WoodSurfaces; i++)
            _tableMesh!.SetSurfaceOverrideMaterial(i, Finishes[finish][i]);
    }

    // =========================================================
    private static StandardMaterial3D Paint(Color color) => new()
    { AlbedoColor = color, Roughness = 0.85f, Metallic = 0 };
}
