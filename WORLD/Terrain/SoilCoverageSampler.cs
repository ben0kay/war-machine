// DTP2: read-only Terrain3D 1.0.2 control-map sampler with one cached buffer per native region.
// No painting, duplicated dirt generator or per-frame sampling.
using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

public sealed class SoilCoverageSampler
{
    private const uint DirtTextureId = 2u; // TerrainAssets: 0 Cliff, 1 Grass, 2 Forest Dirt
    private readonly GodotObject _data;
    private readonly int _regionSize;
    private readonly float _spacing;
    private readonly Dictionary<Vector2I, byte[]?> _regions = new();

    // =========================================================
    public SoilCoverageSampler(Node3D terrain)
    {
        _data = terrain.Get("data").AsGodotObject();
        _regionSize = terrain.Get("region_size").AsInt32();
        _spacing = terrain.Get("vertex_spacing").AsSingle();
        if (_regionSize <= 0 || !float.IsFinite(_spacing) || _spacing <= 0f)
            GD.PushWarning("DTP2: invalid Terrain3D region size/spacing; soil filtering skipped.");
    }

    // =========================================================
    public void Invalidate() => _regions.Clear();

    // =========================================================
    // Uses the saved native manual-paint blend, not an independently generated dirt mask.
    // Height-blending in the GPU material can shift the apparent boundary slightly.
    public float Coverage(Vector2 worldXZ)
    {
        if (_regionSize <= 0 || !float.IsFinite(_spacing) || _spacing <= 0f) return 0f;
        float tx = worldXZ.X / _spacing, tz = worldXZ.Y / _spacing;
        int regionX = Mathf.FloorToInt(tx / _regionSize), regionZ = Mathf.FloorToInt(tz / _regionSize);
        var key = new Vector2I(regionX, regionZ);
        if (!_regions.TryGetValue(key, out byte[]? pixels))
        {
            pixels = ReadRegion(key);
            _regions.Add(key, pixels);
        }
        if (pixels == null) return 0f;
        int x = Mathf.Clamp(Mathf.FloorToInt(tx - regionX * _regionSize), 0, _regionSize - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(tz - regionZ * _regionSize), 0, _regionSize - 1);
        uint control = BinaryPrimitives.ReadUInt32LittleEndian(
            pixels.AsSpan((z * _regionSize + x) * sizeof(uint), sizeof(uint)));
        if ((control & 5u) != 0u) return 0f; // Autoshader or terrain hole
        uint baseId = (control >> 27) & 31u, overlayId = (control >> 22) & 31u;
        float blend = ((control >> 14) & 255u) / 255f;
        return Mathf.Lerp(baseId == DirtTextureId ? 1f : 0f,
            overlayId == DirtTextureId ? 1f : 0f, blend);
    }

    // =========================================================
    // One native image/byte-copy per region on first use; each candidate uses only raw bytes.
    private byte[]? ReadRegion(Vector2I location)
    {
        var region = _data.Call("get_region", location).AsGodotObject() as Resource;
        if (region == null) return null;
        var image = region.Call("get_map", 1).AsGodotObject() as Image;
        if (image == null) return null;
        if (image.GetFormat() != Image.Format.Rf ||
            image.GetWidth() != _regionSize || image.GetHeight() != _regionSize)
        {
            GD.PushWarning("DTP2: unsupported control map in Terrain3D region " + location);
            return null;
        }
        byte[] pixels = image.GetData();
        if (pixels.Length != _regionSize * _regionSize * sizeof(uint))
        {
            GD.PushWarning("DTP2: invalid control map byte length at " + location);
            return null;
        }
        return pixels;
    }
}
