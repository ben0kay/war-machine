// Cached passage meshes and a shared-port Y junction; no overlapping capped tunnels or runtime callbacks.
using Godot;
using System.Collections.Generic;

public partial class WildernessCave
{
    private sealed class Passage
    {
        public Vector3[] Centers = System.Array.Empty<Vector3>();
        public Vector3[,] Rings = new Vector3[0, 0];
        public float[] Widths = System.Array.Empty<float>(), Heights = System.Array.Empty<float>();
    }
    private Vector2[] _junctionPolygon = System.Array.Empty<Vector2>();
    private float _junctionFloor;

    // =========================================================
    // Cache every shared cross-section vertex once. Floor relief is lower amplitude than wall relief.
    private Passage CreatePassage(Vector3[] centers, Vector2[] profile, CaveShapeNoise noise, float widthScale, int chambers, float routeLength)
    {
        int count = centers.Length;
        var passage = new Passage { Centers = centers, Rings = new Vector3[count, profile.Length], Widths = new float[count], Heights = new float[count] };
        for (int i = 0; i < count; i++)
        {
            Vector3 tangent = centers[Mathf.Min(count - 1, i + 1)] - centers[Mathf.Max(0, i - 1)]; tangent.Y = 0;
            Vector3 right = tangent.Normalized().Cross(Vector3.Up);
            // One smooth broad width/ceiling expansion per chamber; exact passage ports stay unchanged.
            float pulse = ChamberInfluence(i * routeLength / (count - 1f), routeLength, chambers, noise);
            passage.Widths[i] = noise.Width(centers[i]) * widthScale
                * (1 + pulse * (Mathf.Clamp(Settings.ChamberWidthMultiplier, 1, 3) - 1));
            passage.Heights[i] = noise.Height(centers[i])
                * (1 + pulse * (Mathf.Clamp(Settings.ChamberHeightMultiplier, 1, 3) - 1));
            for (int j = 0; j < profile.Length; j++)
            {
                Vector2 p = profile[j];
                Vector3 point = centers[i] + right * p.X * passage.Widths[i] + Vector3.Up * p.Y * passage.Heights[i];
                if (j <= FloorSegments)
                    point.Y += noise.Detail(point * 0.6f) * Mathf.Clamp(Settings.FloorRelief, 0, 0.15f);
                else
                {
                    float relief = noise.Detail(point) * Mathf.Clamp(Settings.WallRelief, 0, 0.6f) * Mathf.SmoothStep(0, 0.3f, p.Y);
                    point += (right * p.X + Vector3.Up * p.Y).Normalized() * relief;
                }
                passage.Rings[i, j] = point;
            }
        }
        return passage;
    }

    // =========================================================
    // Smooth chamber pulses are zero at tunnel ends, protecting shared fork and mouth seams.
    private float ChamberInfluence(float travel, float length, int count, CaveShapeNoise noise)
    {
        count = Mathf.Clamp(count, 0, 4);
        float halfLength = Mathf.Clamp(Settings.ChamberHalfLength, 9, 32), amount = 0;
        for (int i = 0; i < count; i++)
        {
            float center = length * (i + 1f) / (count + 1f);
            float shift = noise.Broad(new Vector3(i * 37, Settings.Seed * 0.01f, 313))
                * halfLength * 0.2f;
            float closeness = 1 - Mathf.Clamp(Mathf.Abs(travel - center - shift) / halfLength, 0, 1);
            amount = Mathf.Max(amount, Mathf.SmoothStep(0, 1, closeness));
        }
        return amount;
    }

    // =========================================================
    // Two separately winding descending branches start beyond a broad junction chamber.
    private void CreateBranches(Passage main, Vector2[] profile, CaveShapeNoise noise)
    {
        Vector3 end = main.Centers[^1], direction = end - main.Centers[^2]; direction.Y = 0; direction = direction.Normalized();
        Vector3 right = direction.Cross(Vector3.Up);
        float junction = Mathf.Clamp(Settings.JunctionLength, 10, 20);
        float length = Mathf.Clamp(Settings.BranchLength, 25, 220);
        int sections = Mathf.CeilToInt(length / 1.35f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 start = end + direction * junction + right * side * 7;
            Vector3 heading = (direction + right * side * 0.55f).Normalized();
            // Each fork uses independent seeded bends, constrained to forward travel.
            var path = CaveRouteBuilder.Build(Settings, noise, start, heading, length, sections,
                Settings.BranchDescent, side < 0 ? 1 : 2, 0.7f);
            _passages.Add(CreatePassage(path, profile, noise, 0.85f, Settings.BranchChamberCount, length));
        }
    }

    // =========================================================
    // Actual surface cover, not an arbitrary Y=0 flag, decides whether a route needs entrance terrain support.
    private bool HasNaturalCover(GodotObject data, Passage passage)
    {
        float cover = Mathf.Max(0, Settings.MinimumRoofCover);
        float relief = Mathf.Clamp(Settings.WallRelief, 0, 0.6f);
        for (int i = 0; i < passage.Centers.Length; i++)
        {
            Vector3 center = passage.Centers[i];
            Vector3 tangent = passage.Centers[Mathf.Min(i + 1, passage.Centers.Length - 1)] - passage.Centers[Mathf.Max(0, i - 1)];
            tangent.Y = 0; Vector3 across = tangent.Normalized().Cross(Vector3.Up) * (passage.Widths[i] + relief);
            foreach (Vector3 point in new[] { center - across, center, center + across })
            {
                float surface = data.Call("get_height", point).AsSingle();
                if (!float.IsFinite(surface) || surface < center.Y + passage.Heights[i] + relief + cover) return false;
            }
        }
        return true;
    }

    // =========================================================
    // Branch interiors share their actual port rings with the junction and close only their distant ends.
    private void BuildPassage(ContentMeshBuilder mesh, Passage passage, bool capEnd)
    {
        int last = passage.Centers.Length - 1, count = passage.Rings.GetLength(1);
        for (int i = 0; i < last; i++) for (int j = 0; j < count; j++)
        {
            int next = (j + 1) % count;
            Vector3 center = passage.Centers[i].Lerp(passage.Centers[i + 1], 0.5f) + Vector3.Up * Settings.Height * 0.45f;
            Inward(mesh, passage.Rings[i, j], passage.Rings[i + 1, j], passage.Rings[i, next], center);
            Inward(mesh, passage.Rings[i, next], passage.Rings[i + 1, j], passage.Rings[i + 1, next], center);
        }
        if (!capEnd) return;
        Vector3 cap = passage.Centers[last] + Vector3.Up * passage.Heights[last] * 0.4f;
        Vector3 inside = passage.Centers[last - 1] + Vector3.Up * 2;
        for (int j = 0; j < count; j++) Inward(mesh, passage.Rings[last, j], passage.Rings[last, (j + 1) % count], cap, inside);
    }

    // =========================================================
    // Ordered chamber boundary walks across the main port, right port, then left port; no wall caps at ports.
    private void BuildJunction(ContentMeshBuilder mesh, Passage main, Passage left, Passage right, CaveShapeNoise noise)
    {
        var floor = new List<Vector3>(); var roof = new List<Vector3>();
        int last = main.Centers.Length - 1, count = main.Rings.GetLength(1), roofStart = FloorSegments + 1;
        for (int j = 0; j <= FloorSegments; j++) floor.Add(main.Rings[last, j]);
        for (int j = count - 1; j >= roofStart; j--) roof.Add(main.Rings[last, j]);
        foreach (var port in new[] { right, left })
        {
            for (int j = FloorSegments; j >= 0; j--) floor.Add(port.Rings[0, j]);
            for (int j = roofStart; j < count; j++) roof.Add(port.Rings[0, j]);
        }
        _junctionPolygon = new Vector2[floor.Count];
        for (int i = 0; i < floor.Count; i++) _junctionPolygon[i] = new Vector2(floor[i].X, floor[i].Z);
        Vector3 center = (main.Centers[last] + left.Centers[0] + right.Centers[0]) / 3;
        _junctionFloor = center.Y;
        BuildJunctionFan(mesh, floor, center, noise, true);
        BuildJunctionFan(mesh, roof, center + Vector3.Up * Settings.Height * 1.15f, noise, false);
        Vector3 interior = center + Vector3.Up * 2;
        JoinWall(mesh, main.Rings[last, FloorSegments], right.Rings[0, FloorSegments], main.Rings[last, roofStart], right.Rings[0, roofStart], interior);
        JoinWall(mesh, right.Rings[0, 0], left.Rings[0, FloorSegments], right.Rings[0, count - 1], left.Rings[0, roofStart], interior);
        JoinWall(mesh, left.Rings[0, 0], main.Rings[last, 0], left.Rings[0, count - 1], main.Rings[last, count - 1], interior);
    }

    // =========================================================
    // Radial subdivisions put subtle noise on the chamber floor while pinning all tunnel port vertices.
    private void BuildJunctionFan(ContentMeshBuilder mesh, List<Vector3> boundary, Vector3 center, CaveShapeNoise noise, bool floor)
    {
        const int bands = 8;
        var vertices = new Vector3[bands + 1, boundary.Count];
        for (int band = 0; band <= bands; band++) for (int j = 0; j < boundary.Count; j++)
        {
            float t = (float)band / bands;
            Vector3 p = center.Lerp(boundary[j], t);
            p.Y += noise.Detail(p * 0.6f) * (floor ? Mathf.Clamp(Settings.FloorRelief, 0, 0.15f) : 0.18f) * Mathf.Sin(t * Mathf.Pi);
            vertices[band, j] = p;
        }
        Vector3 interior = center + Vector3.Up * (floor ? 2 : -2);
        for (int band = 0; band < bands; band++) for (int j = 0; j < boundary.Count; j++)
        {
            int next = (j + 1) % boundary.Count;
            if (band > 0) Inward(mesh, vertices[band, j], vertices[band + 1, j], vertices[band, next], interior);
            Inward(mesh, vertices[band, next], vertices[band + 1, j], vertices[band + 1, next], interior);
        }
    }

    // =========================================================
    // Sparse branch markers retain the existing cave lighting style without additional shadowed lights.
    private void AddCrystal(Vector3 center, float width, bool shadows)
    {
        var crystal = new MeshInstance3D { Position = center + new Vector3(width * 0.65f, 0.7f, 0),
            Mesh = new PrismMesh { Size = new Vector3(0.45f, 1.2f, 0.45f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = Settings.LightColor, EmissionEnabled = true,
                Emission = Settings.LightColor, EmissionEnergyMultiplier = 2 } };
        AddChild(crystal);
        AddChild(new OmniLight3D { Position = crystal.Position + Vector3.Up, LightColor = Settings.LightColor,
            LightEnergy = Settings.LightEnergy, OmniRange = 16, ShadowEnabled = shadows,
            DistanceFadeEnabled = true, DistanceFadeBegin = 90, DistanceFadeLength = 20 });
    }

    // =========================================================
    // Only the three chamber side gaps receive walls; the three passage mouths stay open.
    private static void JoinWall(ContentMeshBuilder mesh, Vector3 a, Vector3 b, Vector3 topA, Vector3 topB, Vector3 inside)
    { Inward(mesh, a, topA, b, inside); Inward(mesh, b, topA, topB, inside); }
}
