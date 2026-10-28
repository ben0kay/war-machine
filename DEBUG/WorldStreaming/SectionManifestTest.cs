// Standalone WS0 data-contract checks. No terrain, source asset instantiation or authored file writes.
using Godot;
using System;
using System.IO;

public partial class SectionManifestTest : Node
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public void Run()
    {
        try
        {
            var settings = new WorldSectionSettings { LogicalBounds = new Rect2(-256, -256, 512, 512) };
            WorldManifest manifest = WorldManifest.Create(settings, 48193);
            Check(manifest.SectionAt(new Vector2(-0.1f, -0.1f)) == new SectionCoordinate(-1, -1), "negative floor");
            Check(manifest.SectionAt(new Vector2(128, 0)) == new SectionCoordinate(1, 0), "exact positive boundary");
            Check(manifest.SectionAt(new Vector2(-128, 0)) == new SectionCoordinate(-1, 0), "exact negative boundary");
            Check(manifest.Contains(new Vector2(-256, -256)) && !manifest.Contains(new Vector2(256, 0)), "half-open world bounds");
            Check(manifest.CoveredSections(new Rect2(0, 0, 128, 128)).Count == 1, "half-open footprint coverage");
            var actor = new Node3D(); AddChild(actor);
            actor.GlobalTransform = new Transform3D(Basis.FromEuler(new Vector3(0, 0.4f, 0)), new Vector3(0, 7, 0));
            var placement = WorldPlacementRecord.Capture("authored.test.001", "structure.building.001", actor,
                new Rect2(-10, -10, 20, 20), manifest, 77);
            placement.Overrides["test.variant"] = "example";
            manifest.Placements.Add(placement);
            Check(manifest.CoveredSections(placement.Bounds).Count == 4 && manifest.Placements.Count == 1, "four references, one owner");
            string json = manifest.ToJson(); WorldManifest restored = WorldManifest.FromJson(json);
            Check(restored.WorldId == manifest.WorldId && restored.Seed == 48193 && restored.SchemaVersion == 1, "world identity round-trip");
            WorldPlacementRecord copy = restored.Placements[0];
            Check(copy.InstanceId == placement.InstanceId && copy.AssetId == placement.AssetId && copy.Owner == placement.Owner, "placement identity round-trip");
            Check(copy.Seed == 77 && copy.Overrides["test.variant"] == "example", "seed/override round-trip");
            for (int i = 0; i < 12; i++) Check(copy.Transform[i] == placement.Transform[i], "transform round-trip");
            for (int i = 0; i < 4; i++) Check(copy.Footprint[i] == placement.Footprint[i], "footprint round-trip");
            restored.Placements.Add(copy); Reject(restored, "duplicate ID"); restored.Placements.RemoveAt(1);
            copy.Owner = new SectionCoordinate(1, 1); Reject(restored, "wrong owner"); copy.Owner = placement.Owner;
            copy.AssetId = "unknown"; Reject(restored, "unknown asset"); copy.AssetId = placement.AssetId;
            restored.SchemaVersion = 2; Reject(restored, "future schema");
            settings.Origin = new Vector2(16, -16);
            Check(WorldManifest.Create(settings, 1).SectionAt(new Vector2(15.9f, -16)) == new SectionCoordinate(-1, 0), "offset origin");
            GD.Print($"[WS0] PASS: {_checks} checks. Runtime heading/grid/placement checks remain manual."); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[WS0] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private void Reject(WorldManifest manifest, string message)
    { bool rejected = false; try { manifest.Validate(); } catch (InvalidDataException) { rejected = true; } Check(rejected, message); }

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
