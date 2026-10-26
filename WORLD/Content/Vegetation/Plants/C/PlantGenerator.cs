// Small flower clumps with green stems/leaves and coloured petals; all geometry is batched.
using Godot;

public static class PlantGenerator
{
    // =========================================================
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random, Material material)
    {
        using var mesh = new ContentMeshBuilder(material);
        int stems = Mathf.Clamp(settings.Detail / 8, 3, 8);
        for (int i = 0; i < stems; i++)
        {
            var root = new Vector3(random.RandfRange(-0.35f, 0.35f), 0, random.RandfRange(-0.35f, 0.35f));
            float height = random.RandfRange(0.65f, 1.1f) * settings.Size;
            var top = root + new Vector3(0.08f, height, 0.04f);
            mesh.Tube(root, top, 0.014f, 0.008f, settings.PrimaryColor, settings.PrimaryColor, 3);
            for (int leaf = 0; leaf < 3; leaf++)
            {
                float angle = leaf * 2.4f + i;
                var start = root.Lerp(top, 0.2f + leaf * 0.19f);
                mesh.Leaf(start, start + new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)) * 0.32f * settings.Size, 0.065f * settings.Size, settings.PrimaryColor);
            }
            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.Tau / 5;
                mesh.Leaf(top, top + new Vector3(Mathf.Cos(angle) * 0.18f, 0.055f, Mathf.Sin(angle) * 0.18f) * settings.Size,
                    0.075f * settings.Size, settings.AccentColor * random.RandfRange(0.85f, 1.1f));
            }
            mesh.Tube(top, top + Vector3.Up * 0.035f, 0.045f, 0.035f, new Color(0.8f, 0.57f, 0.12f), new Color(0.9f, 0.7f, 0.16f), 5);
        }
        return mesh.Finish();
    }
}
