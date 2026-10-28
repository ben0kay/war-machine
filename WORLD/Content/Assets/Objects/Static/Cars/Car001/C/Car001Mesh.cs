// Car001 GLB scene adapter: imported low-poly static artwork, per-instance driver-door hinge,
// palette and a small number of author-owned static collision shapes. No runtime tick.
// Artwork/Car001.glb is the single visual source; keep camera occlusion and existing Car001.tscn.
using Godot;
using System.Collections.Generic;

[Tool]
public partial class Car001Mesh : Node3D
{
    private const string ArtworkPath = "res://WORLD/Content/Assets/Objects/Static/Cars/Car001/Artwork/Car001.glb";
    private static PackedScene? _artworkScene;
    [Export] public Car001Settings Settings { get; set; } = null!;
    [ExportGroup("Per-instance car state")]
    // -1 inherits Car001Master; 0 closed; 1..82 degrees opens the front-left door.
    [Export(PropertyHint.Range, "-1,82,1,suffix:°")]
    public float DriverDoorAngleOverride { get; set; } = -1f;
    private Node3D? _door;

    // =========================================================
    // Build once at scene readiness; Godot shares imported mesh buffers across all instances.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") is Node3D generated)
            _door = generated.FindChild("DriverDoor", true, false) as Node3D;
        else Build();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Retain the original car API. Door artwork and its solid collision share the same pivot.
    public void SetDoorAngle(float degrees)
    {
        DriverDoorAngleOverride = Mathf.Clamp(degrees, 0f, 82f);
        if (GodotObject.IsInstanceValid(_door))
            _door!.RotationDegrees = new Vector3(0, -DriverDoorAngleOverride, 0);
    }

    // =========================================================
    private void Build()
    {
        var settings = Settings ?? new Car001Settings();
        _artworkScene ??= ResourceLoader.Load<PackedScene>(ArtworkPath);
        if (_artworkScene == null)
        {
            GD.PushError($"Car001: missing/import-pending GLB: {ArtworkPath}");
            return;
        }

        // Default GLB is 4.35m long (-Z front), 1.83m wide and about 1.81m tall.
        var generated = new Node3D { Name = "Generated", Scale = new Vector3(
            Mathf.Clamp(settings.Width, 1.55f, 2.3f) / 1.83f,
            Mathf.Clamp(settings.RoofHeight, 1.7f, 2.2f) / 1.81f,
            Mathf.Clamp(settings.Length, 3.6f, 5.5f) / 4.35f) };
        AddChild(generated);

        var artwork = _artworkScene.Instantiate<Node3D>();
        artwork.Name = "Artwork";
        generated.AddChild(artwork);
        ApplyPalette(artwork, settings);

        _door = artwork.FindChild("DriverDoor", true, false) as Node3D;
        if (_door == null)
            GD.PushError("Car001 GLB is missing the named DriverDoor pivot.");

        // A few simple solid shapes, not hundreds of decorative per-triangle colliders.
        var body = new StaticBody3D { Name = "StaticCollision", CollisionLayer = 1, CollisionMask = 1 };
        generated.AddChild(body);
        AddBox(body, "Chassis", new(0, 0.73f, 0), new(1.64f, 0.55f, 4.15f));
        AddBox(body, "Hood", new(0, 1.09f, -1.51f), new(1.50f, 0.15f, 1.13f));
        AddBox(body, "Roof", new(0, 1.70f, 0.27f), new(1.42f, 0.16f, 1.29f));

        // The driver-door collision travels with the imported hinge, including instance overrides.
        if (_door != null)
        {
            var doorBody = new StaticBody3D { Name = "DriverDoorCollision", CollisionLayer = 1, CollisionMask = 1 };
            _door.AddChild(doorBody);
            AddBox(doorBody, "DoorPanel", new(0, 1.18f, 0.59f), new(0.14f, 0.71f, 1.14f));
            float degrees = DriverDoorAngleOverride >= 0 ? DriverDoorAngleOverride : settings.DoorAngle;
            _door.RotationDegrees = new Vector3(0, -Mathf.Clamp(degrees, 0f, 82f), 0);
        }
    }

    // =========================================================
    // Share imported GLB geometry; only the few customisable colour materials are per-instance.
    private static void ApplyPalette(Node root, Car001Settings s)
    {
        var palette = new Dictionary<string, Color>
        {
            ["PaintMain"] = s.Paint,
            ["PaintTop"] = s.Paint.Lightened(0.13f),
            ["PaintShade"] = s.Paint.Darkened(0.25f),
            ["WindowGlass"] = s.Glass,
            ["GlassGlint"] = s.Glass.Lightened(0.16f)
        };
        var cache = new Dictionary<string, StandardMaterial3D>();
        Recolor(root);

        void Recolor(Node node)
        {
            if (node is MeshInstance3D mesh && mesh.Mesh != null)
            {
                for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                {
                    if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source) continue;
                    string name = source.ResourceName;
                    if (!palette.TryGetValue(name, out Color tint)) continue;
                    if (!cache.TryGetValue(name, out var copy))
                    {
                        copy = (StandardMaterial3D)source.Duplicate();
                        copy.AlbedoColor = tint;
                        cache[name] = copy;
                    }
                    mesh.SetSurfaceOverrideMaterial(surface, copy);
                }
            }
            foreach (var child in node.GetChildren()) Recolor(child);
        }
    }

    // =========================================================
    private static void AddBox(StaticBody3D body, string name, Vector3 pos, Vector3 size)
    {
        body.AddChild(new CollisionShape3D { Name = name, Position = pos,
            Shape = new BoxShape3D { Size = size } });
    }
}
