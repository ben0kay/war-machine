// Irregular low shrubs with connected woody shoots and pointed folded leaves.
using Godot;

public static class BushGenerator
{
    // =========================================================
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random, Material material)
    {
        using var mesh = new ContentMeshBuilder(material);
        for (int branch = 0; branch < 7; branch++)
        {
            float angle = branch * 2.4f;
            var end = new Vector3(Mathf.Cos(angle) * 0.55f, random.RandfRange(0.65f, 1.05f), Mathf.Sin(angle) * 0.55f) * settings.Size;
            mesh.Tube(Vector3.Zero, end, 0.035f * settings.Size, 0.006f, new Color(0.23f, 0.18f, 0.1f), new Color(0.3f, 0.23f, 0.12f), 4);
            for (int j = 0; j < Mathf.Clamp(settings.Detail, 8, 64); j++)
            {
                float t = random.RandfRange(0.3f, 1);
                var start = end * t;
                float a = random.RandfRange(0, Mathf.Tau);
                var tip = start + new Vector3(Mathf.Cos(a) * 0.43f, random.RandfRange(-0.1f, 0.3f), Mathf.Sin(a) * 0.43f) * settings.Size;
                mesh.Leaf(start, tip, 0.11f * settings.Size, settings.PrimaryColor.Lerp(settings.AccentColor, random.RandfRange(0, 0.3f)));
            }
        }
        return mesh.Finish();
    }
}
