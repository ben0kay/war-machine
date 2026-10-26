// Low-poly asymmetric closed boulders, varied deterministically once per shared mesh variant.
using Godot;

public static class RockGenerator
{
    // =========================================================
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random, Material material)
    {
        using var mesh = new ContentMeshBuilder(material);
        int sides = random.RandiRange(7, 11);
        var stretch = new Vector3(random.RandfRange(0.65f, 1.6f), random.RandfRange(0.4f, 1.5f), random.RandfRange(0.6f, 1.4f)); var ring = new Vector3[sides]; var lower = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.Tau / sides, radius = random.RandfRange(0.7f, 1.15f) * settings.Size;
            ring[i] = new Vector3(Mathf.Cos(angle) * radius, random.RandfRange(0.25f, 0.55f) * settings.Size, Mathf.Sin(angle) * radius * 0.8f) * stretch;
            lower[i] = new Vector3(ring[i].X * 0.85f, -settings.Size * 0.12f, ring[i].Z * 0.85f);
        }
        var top = new Vector3(random.RandfRange(-0.35f, 0.35f), random.RandfRange(0.65f, 1.15f), random.RandfRange(-0.3f, 0.3f)) * settings.Size * stretch;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides; var color = settings.PrimaryColor.Lerp(settings.AccentColor, random.RandfRange(0, 0.5f));
            mesh.RockFace(ring[i], top, ring[next], color, Vector3.Up * settings.Size * 0.2f);
            mesh.RockFace(lower[i], ring[i], lower[next], color * 0.85f, Vector3.Up * settings.Size * 0.2f);
            mesh.RockFace(lower[next], ring[i], ring[next], color * 0.9f, Vector3.Up * settings.Size * 0.2f);
            mesh.RockFace(Vector3.Down * settings.Size * 0.12f, lower[i], lower[next], color, Vector3.Up * settings.Size * 0.2f);
        }
        return mesh.Finish();
    }
}
