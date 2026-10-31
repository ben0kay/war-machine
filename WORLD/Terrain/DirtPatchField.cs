// DTP1: stable organic soil shapes, evaluated in world XZ so Terrain3D regions never tile.
using Godot;
using System;
using System.Collections.Generic;

public sealed class DirtPatchField : IDisposable
{
    public readonly struct Mark
    {
        public readonly Vector2 A, B;
        public readonly float WidthA, WidthB, Angle;
        public readonly bool Trail;
        public readonly Rect2 Bounds;
        public Mark(Vector2 a, Vector2 b, float widthA, float widthB, float angle, bool trail, float pad)
        {
            A = a; B = b; WidthA = widthA; WidthB = widthB; Angle = angle; Trail = trail;
            Vector2 lo = (trail ? a.Min(b) : a), hi = (trail ? a.Max(b) : a);
            float radius = Mathf.Max(widthA, widthB) + pad;
            Bounds = new Rect2(lo - Vector2.One * radius, hi - lo + Vector2.One * (radius * 2f));
        }
    }

    private readonly List<Mark> _marks = new();
    private readonly FastNoiseLite _nx, _nz, _detail;
    private readonly float _warp, _feather, _strength;
    public IReadOnlyList<Mark> Marks => _marks;
    public Rect2 Area { get; }

    // =========================================================
    // Generate a set of overlapping blots and broken curved ribbons, never grid-aligned decals.
    public DirtPatchField(DirtPatchSettings config, Vector2 origin, Vector2 spawnXZ)
    {
        float half = Mathf.Clamp(config.AreaHalfExtent, 100f, 512f);
        Area = new Rect2(origin - Vector2.One * half, Vector2.One * (2f * half));
        _warp = Mathf.Clamp(config.WarpMetres, 0f, 6f);
        _feather = Mathf.Clamp(config.FeatherMetres, 0.25f, 5f);
        _strength = config.SingleSpawnPreviewPatch ? 1f : Mathf.Clamp(config.Strength, 0f, 1f);
        _nx = new FastNoiseLite { Seed = config.Seed + 11, Frequency = 0.065f };
        _nz = new FastNoiseLite { Seed = config.Seed + 37, Frequency = 0.067f };
        _detail = new FastNoiseLite { Seed = config.Seed + 71, Frequency = 0.14f };
        var rng = new RandomNumberGenerator { Seed = unchecked((ulong)(uint)config.Seed) };
        float pad = _warp + _feather + 2f;
        // Keep the owner-validated patch near spawn in BOTH modes; the full-world
        // mode adds seeded clusters/trails around it rather than making it disappear.
        Vector2 center = spawnXZ + new Vector2(-20f, -8f);
        _marks.Add(new Mark(center, center, 11.5f, 7.5f, 0.17f, false, pad));
        if (config.SingleSpawnPreviewPatch) return;
        int low = Mathf.Clamp(config.MinBlotches, 1, 8);
        int high = Mathf.Clamp(Mathf.Max(low, config.MaxBlotches), 1, 8);
        for (int i = 0; i < Mathf.Clamp(config.ClusterCount, 0, 300); i++)
        {
            Vector2 anchor = Point(rng);
            for (int j = 0, n = rng.RandiRange(low, high); j < n; j++)
            {
                Vector2 p = anchor + Polar(rng.RandfRange(0f, 16f), rng.RandfRange(0f, Mathf.Tau));
                float r = rng.Randf() < .24f ? rng.RandfRange(1.2f, 3.5f) : rng.RandfRange(3.5f, 12f);
                float aspect = rng.RandfRange(.55f, 1.55f);
                _marks.Add(new Mark(p, p, r * aspect, r / aspect, rng.RandfRange(0f, Mathf.Tau), false, pad));
            }
        }
        for (int i = 0; i < Mathf.Clamp(config.TrailCount, 0, 40); i++)
        {
            Vector2 p = Point(rng);
            float heading = rng.RandfRange(0f, Mathf.Tau), length = rng.RandfRange(18f, 45f);
            float width = rng.RandfRange(1.3f, 3.8f);
            int segments = rng.RandiRange(3, 6);
            for (int j = 0; j < segments; j++)
            {
                heading += rng.RandfRange(-.55f, .55f);
                Vector2 q = p + Polar(length / segments, heading);
                float next = Mathf.Clamp(width * rng.RandfRange(.7f, 1.3f), 1f, 5.5f);
                _marks.Add(new Mark(p, q, width, next, 0f, true, pad));
                p = q; width = next;
            }
        }
        // A few authored witness patches just outside the real parking lot and road edge.
        // Unlike a random sparse field, these make the first FPS visual check possible from spawn.
        if (config.ShowNearSpawnPatches)
        {
            float padding = _warp + _feather + 2f;
            Vector2 a = spawnXZ + new Vector2(-25f, 12f);
            Vector2 b = spawnXZ + new Vector2(-46f, 32f);
            _marks.Add(new Mark(a, a, 12f, 8f, .34f, false, padding));
            _marks.Add(new Mark(b, b, 8f, 12f, -.6f, false, padding));
            _marks.Add(new Mark(a, b, 3.5f, 2.5f, 0f, true, padding));
            Vector2 c = spawnXZ + new Vector2(62f, 25f);
            _marks.Add(new Mark(c, c, 13f, 7f, -.5f, false, padding));
        }
    }

    // =========================================================
    // Signed distance to a distorted ellipse or tapered line; smooth and colour-modulated.
    public float Coverage(Mark mark, Vector2 p)
    {
        if (!mark.Bounds.HasPoint(p)) return 0f;
        Vector2 q = p + new Vector2(_nx.GetNoise2D(p.X, p.Y), _nz.GetNoise2D(p.X, p.Y)) * _warp;
        float distance;
        if (mark.Trail)
        {
            Vector2 ab = mark.B - mark.A;
            float t = Mathf.Clamp((q - mark.A).Dot(ab) / Mathf.Max(.0001f, ab.LengthSquared()), 0f, 1f);
            distance = q.DistanceTo(mark.A + t * ab) - Mathf.Lerp(mark.WidthA, mark.WidthB, t);
        }
        else
        {
            Vector2 local = (q - mark.A).Rotated(-mark.Angle);
            float normalized = new Vector2(local.X / mark.WidthA, local.Y / mark.WidthB).Length();
            distance = (normalized - 1f) * Mathf.Min(mark.WidthA, mark.WidthB);
        }
        float tEdge = Mathf.Clamp(.5f - distance / (2f * _feather), 0f, 1f);
        float soft = tEdge * tEdge * (3f - 2f * tEdge);
        float border = Mathf.Min(Mathf.Min(p.X - Area.Position.X, Area.End.X - p.X),
            Mathf.Min(p.Y - Area.Position.Y, Area.End.Y - p.Y));
        float f = Mathf.Clamp(border / 14f, 0f, 1f);
        return Mathf.Clamp(soft * (0.91f + .09f * _detail.GetNoise2D(p.X, p.Y))
            * _strength * f * f * (3f - 2f * f), 0f, 1f);
    }

    private Vector2 Point(RandomNumberGenerator rng) =>
        Area.Position + Area.Size * new Vector2(rng.Randf(), rng.Randf());
    private static Vector2 Polar(float r, float a) => new(Mathf.Cos(a) * r, Mathf.Sin(a) * r);

    // =========================================================
    public void Dispose() { _nx.Dispose(); _nz.Dispose(); _detail.Dispose(); }
}
