// Sparse seeded curves steer cave layout; independent from the fine rock-detail noise.
using Godot;

public static class CaveRouteBuilder
{
    // =========================================================
    // Smooth spaced lateral control points into a forward-only cave route, with a graded descent.
    public static Vector3[] Build(CaveSettings settings, CaveShapeNoise noise, Vector3 start,
        Vector3 heading, float length, int sections, float descent, int routeId, float bendScale = 1f)
    {
        heading.Y = 0; heading = heading.Normalized();
        Vector3 lateral = heading.Cross(Vector3.Up);
        float spacing = Mathf.Clamp(settings.MacroBendSpacing, 22f, 65f);
        int intervals = Mathf.Max(3, Mathf.CeilToInt(length / spacing));
        float span = length / intervals;
        float strength = Mathf.Clamp(settings.MacroBendAmplitude, 0, 22) * bendScale;
        float maxSlope = Mathf.Tan(Mathf.DegToRad(Mathf.Clamp(settings.CorridorWanderDegrees, 0, 65)));
        var offsets = new float[intervals + 1];
        var rng = new RandomNumberGenerator { Seed = (ulong)(uint)unchecked(settings.Seed * 397 ^ routeId * 7919) };
        // Keep a straight initial approach to the terrain entrance and the three-way junction.
        for (int i = 2; i < intervals; i++)
        {
            float proposed = rng.RandfRange(-strength, strength);
            offsets[i] = Mathf.Clamp(proposed, offsets[i - 1] - span * maxSlope, offsets[i - 1] + span * maxSlope);
        }
        offsets[intervals] = offsets[intervals - 1];
        var path = new Vector3[sections + 1];
        path[0] = start;
        float maxGrade = Mathf.Clamp(settings.MaximumGrade, 0.05f, 0.45f);
        descent = Mathf.Clamp(descent, 0, length * maxGrade * 0.85f);
        for (int i = 1; i <= sections; i++)
        {
            float t = (float)i / sections, travel = t * length;
            int segment = Mathf.Min(intervals - 1, Mathf.FloorToInt(travel / span));
            float u = Mathf.Clamp(travel / span - segment, 0, 1);
            float shift = Mathf.CubicInterpolate(offsets[segment], offsets[segment + 1],
                offsets[Mathf.Max(0, segment - 1)], offsets[Mathf.Min(intervals, segment + 2)], u);
            Vector3 point = start + heading * travel + lateral * shift;
            float vertical = noise.Broad(point + Vector3.Up * (91 + routeId * 17))
                * Mathf.Clamp(settings.CorridorVerticalNoise, 0, 1) * Mathf.Sin(t * Mathf.Pi);
            float desiredY = start.Y - descent * t + vertical;
            Vector3 previous = path[i - 1];
            float step = new Vector2(point.X - previous.X, point.Z - previous.Z).Length();
            point.Y = previous.Y - Mathf.Clamp(previous.Y - desiredY, 0, maxGrade * step);
            path[i] = point;
        }
        return path;
    }
}
