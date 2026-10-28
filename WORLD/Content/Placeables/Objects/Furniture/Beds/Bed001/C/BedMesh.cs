// Bed001 visual-only procedural asset: one shared bed geometry with four reusable fabric palettes.
// No physics, pickups, materials cloned per instance, or idle processing. Compatible with F1 previews.
using Godot;

[Tool]
public partial class BedMesh : Node3D
{
    public enum BeddingPalette { SlateBlue, MossGreen, WarmSand, DustyRose }

    [Export] public BedSettings Settings { get; set; } = null!;
    [Export] public BeddingPalette BeddingVariant { get; set; } = BeddingPalette.SlateBlue;

    private static readonly BoxMesh Cube = new() { Size = Vector3.One };
    private static readonly StandardMaterial3D[] DuvetPaints =
    {
        Paint(new Color(0.27f, 0.39f, 0.51f)), // Slate blue
        Paint(new Color(0.37f, 0.44f, 0.32f)), // Moss green
        Paint(new Color(0.67f, 0.58f, 0.44f)), // Warm sand
        Paint(new Color(0.55f, 0.38f, 0.40f))  // Dusty rose
    };
    private static readonly StandardMaterial3D[] FoldPaints =
    {
        Paint(new Color(0.42f, 0.53f, 0.62f)),
        Paint(new Color(0.51f, 0.58f, 0.43f)),
        Paint(new Color(0.78f, 0.70f, 0.57f)),
        Paint(new Color(0.68f, 0.52f, 0.53f))
    };
    private static readonly StandardMaterial3D Wood = Paint(new Color(0.30f, 0.20f, 0.13f));
    private static readonly StandardMaterial3D WoodTrim = Paint(new Color(0.39f, 0.27f, 0.18f));
    private static readonly StandardMaterial3D Mattress = Paint(new Color(0.60f, 0.59f, 0.53f));
    private static readonly StandardMaterial3D Sheet = Paint(new Color(0.81f, 0.79f, 0.70f));
    private static readonly StandardMaterial3D Pillow = Paint(new Color(0.88f, 0.85f, 0.76f));

    // =========================================================
    // Build one fixed arrangement. Changing BeddingVariant only changes shared material references.
    public override void _Ready()
    {
        BuildModel();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Runtime/editor callers may deliberately recolour this same model without creating a new asset.
    public void SetBeddingVariant(BeddingPalette variant)
    {
        if (BeddingVariant == variant) return;
        BeddingVariant = variant;
        if (IsInsideTree()) BuildModel();
    }

    // =========================================================
    // Single-sized used bed, head at -Z and walk-up/foot at +Z. Ground pivot is local Y=0.
    // Soft shapes are stylized: thin overlapping slabs, fold and pillow edges rather than flat colour boxes.
    private void BuildModel()
    {
        var previous = GetNodeOrNull<Node3D>("Generated");
        if (previous != null) { RemoveChild(previous); previous.QueueFree(); }
        var model = new Node3D { Name = "Generated" }; AddChild(model);
        var s = Settings ?? new BedSettings();
        float w = Mathf.Clamp(s.Width, 0.9f, 1.6f), l = Mathf.Clamp(s.Length, 1.8f, 2.4f);
        float halfW = w * 0.5f, halfL = l * 0.5f;
        int palette = Mathf.Clamp((int)BeddingVariant, 0, DuvetPaints.Length - 1);
        var duvet = DuvetPaints[palette];
        var fold = FoldPaints[palette];

        // Solid hardwood frame with open air under the bed; no physics is generated in Phase 1.
        Box(model, "LeftLegHead", new(-halfW + 0.06f, 0.24f, -halfL + 0.09f), new(0.11f, 0.48f, 0.11f), Wood);
        Box(model, "RightLegHead", new(halfW - 0.06f, 0.24f, -halfL + 0.09f), new(0.11f, 0.48f, 0.11f), Wood);
        Box(model, "LeftLegFoot", new(-halfW + 0.06f, 0.24f, halfL - 0.10f), new(0.11f, 0.48f, 0.11f), Wood);
        Box(model, "RightLegFoot", new(halfW - 0.06f, 0.24f, halfL - 0.10f), new(0.11f, 0.48f, 0.11f), Wood);
        Box(model, "LeftRail", new(-halfW + 0.05f, 0.40f, 0), new(0.10f, 0.20f, l - 0.08f), Wood);
        Box(model, "RightRail", new(halfW - 0.05f, 0.40f, 0), new(0.10f, 0.20f, l - 0.08f), Wood);
        Box(model, "FootRail", new(0, 0.42f, halfL - 0.07f), new(w - 0.10f, 0.18f, 0.12f), Wood);
        Box(model, "HeadRail", new(0, 0.42f, -halfL + 0.07f), new(w - 0.10f, 0.18f, 0.12f), Wood);
        Box(model, "SlattedBase", new(0, 0.47f, 0), new(w - 0.14f, 0.055f, l - 0.12f), WoodTrim);

        // Taller headboard makes the silhouette readable in the angled top-down camera.
        Box(model, "Headboard", new(0, 0.83f, -halfL + 0.04f), new(w, 0.60f, 0.095f), Wood);
        Box(model, "HeadboardInset", new(0, 0.85f, -halfL + 0.10f), new(w - 0.18f, 0.38f, 0.035f), WoodTrim);
        Box(model, "HeadboardCap", new(0, 1.15f, -halfL + 0.04f), new(w + 0.07f, 0.065f, 0.15f), WoodTrim);

        // Shared mattress and fitted sheet remain identical across all four palette choices.
        Box(model, "Mattress", new(0, 0.585f, 0.03f), new(w - 0.16f, 0.20f, l - 0.19f), Mattress);
        Box(model, "FittedSheet", new(0, 0.694f, 0.03f), new(w - 0.18f, 0.027f, l - 0.21f), Sheet);
        Box(model, "SheetTurnover", new(0, 0.715f, -halfL + 0.43f), new(w - 0.20f, 0.017f, 0.24f), Sheet);

        // A single coloured duvet and matching folded band. Each palette changes materials, not meshes.
        float duvetCenter = 0.28f, duvetLength = l - 0.80f;
        Box(model, "Duvet", new(0, 0.755f, duvetCenter), new(w - 0.10f, 0.094f, duvetLength), duvet);
        Box(model, "TurnedDuvetEdge", new(0, 0.812f, duvetCenter - duvetLength * 0.5f + 0.055f),
            new(w - 0.075f, 0.032f, 0.15f), fold);
        Box(model, "DuvetFootDrop", new(0, 0.685f, duvetCenter + duvetLength * 0.5f),
            new(w - 0.095f, 0.19f, 0.060f), duvet);
        Box(model, "DuvetFoldLeft", new(-0.25f * w, 0.809f, 0.48f), new(0.018f, 0.009f, 0.46f), fold);
        Box(model, "DuvetFoldRight", new(0.26f * w, 0.809f, 0.70f), new(0.018f, 0.009f, 0.29f), fold);

        // Two separate cream pillows with small border lips. They do not change by palette.
        float pillowX = w * 0.238f, pillowZ = -halfL + 0.30f, pillowWidth = w * 0.43f;
        foreach (float side in new[] { -1f, 1f })
        {
            string name = side < 0 ? "Left" : "Right";
            Box(model, name + "PillowPiping", new(side * pillowX, 0.764f, pillowZ),
                new(pillowWidth + 0.025f, 0.038f, 0.38f), Sheet);
            Box(model, name + "Pillow", new(side * pillowX, 0.795f, pillowZ),
                new(pillowWidth, 0.074f, 0.35f), Pillow);
        }
    }

    // =========================================================
    // All parts share a single unit box mesh; material variants never clone 3D geometry.
    private static void Box(Node3D root, string name, Vector3 position, Vector3 size, Material material)
        => root.AddChild(new MeshInstance3D
        { Name = name, Mesh = Cube, Position = position, Scale = size, MaterialOverride = material });

    // =========================================================
    // Matte opaque cloth/wood PBR, not unique bitmap textures per bed.
    private static StandardMaterial3D Paint(Color color) => new()
    { AlbedoColor = color, Roughness = 0.88f, Metallic = 0.0f };
}
