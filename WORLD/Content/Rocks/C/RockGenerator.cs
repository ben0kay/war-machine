// Low-poly asymmetric closed boulders, varied deterministically once per shared mesh variant.
using Godot;

public static class RockGenerator
{
    // =========================================================
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random, Material material)
    {
        using var mesh = new ContentMeshBuilder(material);
        const int sides = 9; var ring = new Vector3[sides]; var lower = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.Tau / sides, radius = random.RandfRange(0.7f, 1.15f) * settings.Size;
            ring[i] = new Vector3(Mathf.Cos(angle) * radius, random.RandfRange(0.25f, 0.55f) * settings.Size, Mathf.Sin(angle) * radius * 0.8f);
            lower[i] = new Vector3(ring[i].X * 0.85f, -settings.Size * 0.12f, ring[i].Z * 0.85f);
        }
        var top = new Vector3(0.12f, 0.85f, -0.06f) * settings.Size;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides; var color = settings.PrimaryColor.Lerp(settings.AccentColor, random.RandfRange(0, 0.5f));
            mesh.Face(ring[i], top, ring[next], color);
            mesh.Face(lower[i], ring[i], lower[next], color * 0.85f);
            mesh.Face(lower[next], ring[i], ring[next], color * 0.9f);
            mesh.Face(Vector3.Down * settings.Size * 0.12f, lower[i], lower[next], color);
        }
        return mesh.Finish();
    }
}
