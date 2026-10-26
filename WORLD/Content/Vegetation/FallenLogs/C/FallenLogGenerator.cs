// Tapered fallen trunks with cut ends, snapped branch stubs and a narrow moss strip.
using Godot;

public static class FallenLogGenerator
{
    // =========================================================
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random, Material material)
    {
        using var mesh = new ContentMeshBuilder(material);
        float length = settings.Size, radius = length * 0.08f;
        var start = new Vector3(0, radius * 0.65f, -length * 0.5f); var end = start + Vector3.Back * length;
        mesh.Tube(start, end, radius, radius * 0.72f, settings.PrimaryColor, settings.AccentColor, 9);
        for (int i = 0; i < 4; i++)
        {
            var root = start.Lerp(end, random.RandfRange(0.15f, 0.85f));
            mesh.Tube(root, root + new Vector3(i % 2 == 0 ? radius * 1.8f : -radius * 1.8f, radius * 1.2f, 0.1f),
                radius * 0.24f, radius * 0.09f, settings.PrimaryColor, settings.AccentColor, 5);
        }
        mesh.Tube(start + Vector3.Up * radius * 0.8f, end + Vector3.Up * radius * 0.65f, radius * 0.35f, radius * 0.22f,
            new Color(0.17f, 0.24f, 0.09f), new Color(0.17f, 0.24f, 0.09f), 4);
        return mesh.Finish();
    }
}
