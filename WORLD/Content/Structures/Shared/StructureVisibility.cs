// Prefab opt-in and runtime lifecycle for shared multi-storey building visibility. FPS never attaches a context.
using Godot;

public partial class StructureVisibility : Node
{
    public static event System.Action<StructureVisibility>? Available;
    public static event System.Action<StructureVisibility>? Removed;
    [Export] public StructureSettings Settings { get; set; } = null!;
    private PlayerController? _player;
    private CameraOcclusion? _camera;
    private BuildingFloorVisibility? _floors;
    private BuildingObstructionVisibility? _outside;

    // =========================================================
    public override void _Ready()
    { SetProcess(false); SetPhysicsProcess(false); Available?.Invoke(this); }

    // =========================================================
    // Parent generators finish Ready before deferred initialization reads metadata. Safe for later-spawned prefabs.
    public void Attach(PlayerController player, CameraOcclusion camera)
    {
        if (_camera != null) return;
        _player = player; _camera = camera; CallDeferred(nameof(InitializeVisibility));
    }

    // =========================================================
    public void InitializeVisibility()
    {
        if (_camera == null || !IsInstanceValid(_camera) || !IsInsideTree() || _floors != null) return;
        var building = GetParent<Node3D>();
        if (Settings == null) { GD.PushWarning($"{building.Name}: StructureVisibility needs a StructureSettings master."); return; }
        var layout = StructureLayout.Read(building);
        if (layout.Zones.Count == 0) { GD.PushWarning($"{building.Name}: declare structure_occupied_bounds before visibility initialization."); return; }
        var profile = (StructureSettings)Settings.Duplicate(true);
        _floors = new BuildingFloorVisibility { Name = "FloorVisibility" }; building.AddChild(_floors);
        _floors.Initialize(building, _player!, profile, layout);
        _outside = new BuildingObstructionVisibility { Name = "OutsideVisibility" }; building.AddChild(_outside);
        _outside.Initialize(building, _player!, profile.OutsideBuildingOpacity, layout.Zones.ToArray(), () => _floors.IsInside);
        _camera.RegisterProvider(_outside);
    }

    // =========================================================
    // Procedural regeneration is explicit: stop/restore old ownership before replacing Generated, then call Attach.
    public void Detach()
    {
        if (IsInstanceValid(_outside))
        {
            if (IsInstanceValid(_camera)) _camera!.UnregisterProvider(_outside!);
            _outside!.Shutdown(); _outside.GetParent().RemoveChild(_outside); _outside.QueueFree();
        }
        if (IsInstanceValid(_floors))
        { _floors!.Shutdown(); _floors.GetParent().RemoveChild(_floors); _floors.QueueFree(); }
        _outside = null; _floors = null; _camera = null; _player = null;
    }

    // =========================================================
    // Recommended procedural regeneration hook: preserve context while removing old owners before geometry changes.
    public void Rebuild(System.Action generate)
    {
        var player = _player; var camera = _camera; Detach(); generate();
        if (IsInstanceValid(player) && IsInstanceValid(camera)) Attach(player!, camera!);
    }

    // =========================================================
    // Call after synchronous generated-art rebuild; no regeneration polling and no duplicate provider subscriptions.
    public void Refresh()
    {
        var player = _player; var camera = _camera; Detach();
        if (IsInstanceValid(player) && IsInstanceValid(camera)) Attach(player!, camera!);
    }

    // =========================================================
    public override void _ExitTree() { Removed?.Invoke(this); Detach(); }
}
