// WS0 finite-world description and geometry only. No scene instantiation, file writes, timers or save-game state.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class WorldManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string WorldId { get; set; } = "";
    public int Seed { get; set; }
    public float OriginX { get; set; }
    public float OriginZ { get; set; }
    public float SectionSize { get; set; } = 128;
    public float[] WorldBounds { get; set; } = new float[] { 0, 0, 1024, 1024 };
    public List<WorldPlacementRecord> Placements { get; set; } = new();
    [JsonIgnore] public Rect2 Bounds => new(WorldBounds[0], WorldBounds[1], WorldBounds[2], WorldBounds[3]);

    // =========================================================
    public static WorldManifest Create(WorldSectionSettings settings, int seed)
    {
        var result = new WorldManifest { WorldId = settings.WorldId, Seed = seed, OriginX = settings.Origin.X,
            OriginZ = settings.Origin.Y, SectionSize = settings.SectionSize,
            WorldBounds = new[] { settings.LogicalBounds.Position.X, settings.LogicalBounds.Position.Y,
                settings.LogicalBounds.Size.X, settings.LogicalBounds.Size.Y } };
        result.Validate(); return result;
    }

    // =========================================================
    public SectionCoordinate SectionAt(Vector2 point)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentException("Section position must be finite.");
        return new SectionCoordinate(checked((int)Math.Floor(((double)point.X - OriginX) / SectionSize)),
            checked((int)Math.Floor(((double)point.Y - OriginZ) / SectionSize)));
    }

    // =========================================================
    public Rect2 SectionBounds(SectionCoordinate cell)
        => new(OriginX + cell.X * SectionSize, OriginZ + cell.Z * SectionSize, SectionSize, SectionSize);

    // =========================================================
    public bool Contains(Vector2 point) => Bounds.HasPoint(point);

    // =========================================================
    // Half-open coverage: a footprint ending exactly on an edge does not touch the next cell.
    // A dependency is a reference, never a second placement or a second owner.
    public List<SectionCoordinate> CoveredSections(Rect2 footprint)
    {
        var result = new List<SectionCoordinate>();
        if (!footprint.Intersects(Bounds)) return result;
        Rect2 clipped = footprint.Intersection(Bounds);
        SectionCoordinate first = SectionAt(clipped.Position);
        int lastX = checked((int)Math.Ceiling(((double)clipped.End.X - OriginX) / SectionSize) - 1);
        int lastZ = checked((int)Math.Ceiling(((double)clipped.End.Y - OriginZ) / SectionSize) - 1);
        long width = (long)lastX - first.X + 1, height = (long)lastZ - first.Z + 1;
        if (width < 0 || height < 0 || width > 4096 || height > 4096 || width * height > 4096)
            throw new InvalidDataException("A prototype placement exceeds the 4096 dependency-cell limit.");
        for (long z = first.Z; z <= lastZ; z++) for (long x = first.X; x <= lastX; x++) result.Add(new SectionCoordinate((int)x, (int)z));
        return result;
    }

    // =========================================================
    public string ToJson() { Validate(); return JsonSerializer.Serialize(this); }

    // =========================================================
    public static WorldManifest FromJson(string json)
    {
        var result = JsonSerializer.Deserialize<WorldManifest>(json) ?? throw new InvalidDataException("Missing world manifest.");
        result.Validate(); return result;
    }

    // =========================================================
    // Fail explicitly on incompatible versions, malformed geometry, duplicate IDs or inconsistent ownership.
    public void Validate()
    {
        if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(WorldId) || !float.IsFinite(SectionSize)
            || SectionSize < 32 || SectionSize > 512 || !float.IsFinite(OriginX) || !float.IsFinite(OriginZ)
            || !Finite(WorldBounds, 4) || WorldBounds[2] <= 0 || WorldBounds[3] <= 0 || Placements == null)
            throw new InvalidDataException("Invalid WS0 world settings/schema.");
        if (!float.IsFinite(Bounds.End.X) || !float.IsFinite(Bounds.End.Y)) throw new InvalidDataException("World bounds overflow.");
        SectionAt(Bounds.Position); SectionAt(Bounds.End);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var placement in Placements)
        {
            if (placement == null || string.IsNullOrWhiteSpace(placement.InstanceId) || !ids.Add(placement.InstanceId)
                || string.IsNullOrWhiteSpace(placement.AssetId) || !WorldAssetCatalog.TryResolve(placement.AssetId, out _)
                || !Finite(placement.Transform, 12) || !Finite(placement.Footprint, 4)
                || placement.Footprint[2] <= 0 || placement.Footprint[3] <= 0 || placement.Overrides == null)
                throw new InvalidDataException("Invalid placement, duplicate instance ID or unknown asset ID.");
            float determinant = placement.WorldTransform.Basis.Determinant();
            if (!float.IsFinite(determinant) || !float.IsFinite(placement.Bounds.End.X) || !float.IsFinite(placement.Bounds.End.Y))
                throw new InvalidDataException("Placement geometry overflow.");
            Vector3 at = placement.WorldTransform.Origin;
            if (!Contains(new Vector2(at.X, at.Z)) || placement.Owner != SectionAt(new Vector2(at.X, at.Z))
                || Mathf.Abs(determinant) < 0.000001f)
                throw new InvalidDataException("Placement anchor outside world, wrong owner or singular transform.");
            CoveredSections(placement.Bounds);
        }
    }

    // =========================================================
    private static bool Finite(float[]? values, int count)
    {
        if (values == null || values.Length != count) return false;
        foreach (float value in values) if (!float.IsFinite(value)) return false;
        return true;
    }
}
