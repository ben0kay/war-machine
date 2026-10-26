// Seeded, layered cave geometry noise. Used only during construction; no gameplay update callback.
using Godot;

public sealed class CaveShapeNoise : System.IDisposable
{
    private readonly FastNoiseLite _broad, _detail;
    private readonly CaveSettings _settings;

    // =========================================================
    // Independent scales shape broad chambers and smaller rock relief without per-vertex random seams.
    public CaveShapeNoise(CaveSettings settings)
    {
        _settings = settings;
        _broad = new FastNoiseLite { Seed = settings.Seed, Frequency = Mathf.Max(0.001f, settings.ShapeFrequency),
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, FractalOctaves = 3 };
        _detail = new FastNoiseLite { Seed = settings.Seed + 913, Frequency = Mathf.Max(0.01f, settings.RockFrequency),
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, FractalOctaves = 3 };
    }

    // =========================================================
    // World-coordinate noise stays deterministic for the same seed and cave location.
    public float Broad(Vector3 point) => _broad.GetNoise3D(point.X, point.Y, point.Z);

    // =========================================================
    // Small relief is separately tunable from passage size/clearance.
    public float Detail(Vector3 point) => _detail.GetNoise3D(point.X, point.Y, point.Z);

    // =========================================================
    // Width/height clamps preserve a generous walkable core even with extreme authored variation.
    public float Width(Vector3 point) => Mathf.Max(2, _settings.Width) * (1 + Broad(point) * Mathf.Clamp(_settings.WidthNoise, 0, 0.4f));

    // =========================================================
    // Offset sampling prevents perfectly correlated width and ceiling changes.
    public float Height(Vector3 point) => Mathf.Max(3, _settings.Height) * (1 + Broad(point + Vector3.One * 71) * Mathf.Clamp(_settings.HeightNoise, 0, 0.4f));

    // =========================================================
    // Free native noise resources after the one-shot mesh/terrain construction pass.
    public void Dispose() { _broad.Dispose(); _detail.Dispose(); }
}
