// Event-redrawn map of nine logical cells and prototype footprints. This is not loaded-world residency.
using Godot;

public partial class SectionMap : Control
{
    private WorldManifest? _manifest;
    private SectionCoordinate _center;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); MouseFilter = MouseFilterEnum.Ignore; }

    // =========================================================
    public void UpdateMap(WorldManifest manifest, SectionCoordinate center)
    { _manifest = manifest; _center = center; QueueRedraw(); }

    // =========================================================
    public override void _Draw()
    {
        if (_manifest == null) return;
        Font font = GetThemeDefaultFont();
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.025f, 0.04f, 0.05f, 0.94f));
        DrawString(font, new Vector2(18, 22), "WS0 · Section ownership · F3 close", fontSize: 13);
        const float cellSize = 88;
        Vector2 origin = new(18, 38);
        var first = new SectionCoordinate(_center.X - 1, _center.Z - 1);
        for (int z = 0; z < 3; z++) for (int x = 0; x < 3; x++)
        {
            var cell = new SectionCoordinate(first.X + x, first.Z + z);
            Rect2 rect = new(origin + new Vector2(x, z) * cellSize, Vector2.One * cellSize);
            bool inside = _manifest.SectionBounds(cell).Intersects(_manifest.Bounds);
            Color fill = cell == _center ? new Color(0.13f, 0.29f, 0.34f) : inside ? new Color(0.09f, 0.13f, 0.15f) : new Color(0.05f, 0.055f, 0.06f);
            DrawRect(rect, fill); DrawRect(rect, new Color(0.35f, 0.43f, 0.45f), false);
            int owners = 0;
            foreach (var placement in _manifest.Placements) if (placement.Owner == cell) owners++;
            DrawString(font, rect.Position + new Vector2(5, 16), $"{cell.X}, {cell.Z}", fontSize: 12);
            DrawString(font, rect.Position + new Vector2(5, 76), inside ? $"{owners} owned" : "outside", fontSize: 11);
        }
        Rect2 worldView = new(_manifest.SectionBounds(first).Position, Vector2.One * (_manifest.SectionSize * 3));
        foreach (var placement in _manifest.Placements)
        {
            if (!placement.Bounds.Intersects(worldView)) continue;
            Rect2 clipped = placement.Bounds.Intersection(worldView);
            Vector2 pos = origin + (clipped.Position - worldView.Position) / _manifest.SectionSize * cellSize;
            DrawRect(new Rect2(pos, clipped.Size / _manifest.SectionSize * cellSize), new Color(0.95f, 0.73f, 0.3f), false, 2);
        }
        DrawString(font, new Vector2(18, 322), $"{_manifest.Placements.Count} recorded · {_manifest.SectionSize:0}m cells", fontSize: 12);
        DrawString(font, new Vector2(18, 344), "Gold: footprints · centre: player cell", fontSize: 11);
    }
}
