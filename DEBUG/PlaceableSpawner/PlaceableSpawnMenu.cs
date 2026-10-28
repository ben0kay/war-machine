// F1 placeable preview: GLB scene wrappers with shared per-asset size masters; no physics.
using Godot;

public partial class PlaceableSpawnMenu : CanvasLayer
{
    private const string BookcasePath = "res://WORLD/Content/Assets/Objects/Static/Furniture/Bookcases/Bookcase001/Bookcase001.tscn";
    private const string BookPath = "res://WORLD/Content/Assets/Objects/Props/Books/Book001/Book001.tscn";
    private const string BedPath = "res://WORLD/Content/Assets/Objects/Static/Furniture/Beds/Bed001/Bed001.tscn";
    private const string TablePath = "res://WORLD/Content/Assets/Objects/Static/Furniture/Tables/Table001/Table001.tscn";
    private const int MaxInstances = 24;
    private PlayerController? _player;
    private Camera3D? _camera;
    private Node3D? _terrain;
    private Node3D? _spawned;
    private ColorRect _backdrop = null!;
    private OptionButton _selection = null!;
    private Label _status = null!;
    private PlaceableSpawnSettings _settings = null!;
    private bool _wasCaptured;

    // =========================================================
    // Everything is driven by key/button events; neither the UI nor instances poll each frame.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _backdrop = GetNode<ColorRect>("Backdrop");
        _selection = GetNode<OptionButton>("Backdrop/Panel/Margin/Rows/Selection");
        _status = GetNode<Label>("Backdrop/Panel/Margin/Rows/Status");
        _backdrop.Hide();
        _selection.AddItem("OBJECT · Bookcase001");
        _selection.AddItem("PROP · Book001");
        _selection.AddItem("OBJECT · Bed001 · Slate Blue");
        _selection.AddItem("OBJECT · Bed001 · Moss Green");
        _selection.AddItem("OBJECT · Bed001 · Warm Sand");
        _selection.AddItem("OBJECT · Bed001 · Dusty Rose");
        _selection.AddItem("OBJECT · Table001 · Weathered Oak");
        _selection.AddItem("OBJECT · Table001 · Dark Walnut");
        _selection.AddItem("OBJECT · Table001 · Faded Grey");
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Spawn").Pressed += SpawnSelected;
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Actions/Clear").Pressed += ClearSpawned;
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Actions/Close").Pressed += () => CloseMenu(true);
        _settings = GD.Load<PlaceableSpawnSettings>("res://DEBUG/PlaceableSpawner/Configuration/PlaceableSpawnMaster.tres");
    }

    // =========================================================
    // Existing shared world provides both camera presentations without duplicate spawn logic.
    public void Initialize(PlayerController player, Camera3D camera, Node3D terrain, Node3D propsRoot)
    {
        _player = player; _camera = camera; _terrain = terrain;
        _spawned = new Node3D { Name = "DebugSpawnedPlaceables" };
        propsRoot.AddChild(_spawned);
    }

    // =========================================================
    // F1 opens/closes; Escape closes and leaves FPS mouse released for normal recapture.
    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        bool toggle = key.Keycode == Key.F1 || key.PhysicalKeycode == Key.F1;
        if (toggle)
        {
            if (_backdrop.Visible) CloseMenu(true);
            else if (_player != null && _camera != null) OpenMenu();
            GetViewport().SetInputAsHandled();
        }
        else if (_backdrop.Visible && (key.Keycode == Key.Escape || key.PhysicalKeycode == Key.Escape))
        { CloseMenu(false); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // Backdrop intercepts mouse clicks so spawning cannot also fire weapons or recapture FPS aiming.
    private void OpenMenu()
    {
        _wasCaptured = Input.MouseMode == Input.MouseModeEnum.Captured;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _status.Text = "Visual preview only · no collision or gravity";
        _backdrop.Show();
    }

    // =========================================================
    private void CloseMenu(bool restoreCapture)
    {
        _backdrop.Hide();
        if (restoreCapture && _wasCaptured) Input.MouseMode = Input.MouseModeEnum.Captured;
        _wasCaptured = false;
    }

    // =========================================================
    // Ground against standing height or sampled Terrain3D height; book hovers for a legible model preview.
    private void SpawnSelected()
    {
        if (_player == null || _camera == null || _terrain == null || _spawned == null || _settings == null)
        { _status.Text = "World is not ready."; return; }
        if (_spawned.GetChildCount() >= MaxInstances)
        { _status.Text = "Preview limit reached. Clear models."; return; }

        bool isBook = _selection.Selected == 1;
        bool isBed = _selection.Selected >= 2 && _selection.Selected <= 5;
        bool isTable = _selection.Selected >= 6 && _selection.Selected <= 8;
        string path = isTable ? TablePath : isBed ? BedPath : isBook ? BookPath : BookcasePath;
        var source = ResourceLoader.Load<PackedScene>(path);
        if (source == null) { _status.Text = "Could not load model/scene; see Godot Output."; return; }
        if (source.Instantiate() is not Node3D instance)
        { _status.Text = "Placeable does not have a Node3D root."; return; }
        // Select the palette before AddChild/_Ready; Bed001 swaps materials without rebuilding geometry.
        if (isBed && instance is BedVisual bed)
            bed.BeddingVariant = (BedVisual.BeddingPalette)(_selection.Selected - 2);
        if (isTable && instance is TableVisual table)
            table.FinishVariant = (TableVisual.WoodFinish)(_selection.Selected - 6);

        Vector3 direction = -_camera.GlobalBasis.Z;
        direction.Y = 0;
        direction = direction.LengthSquared() < 0.0001f ? Vector3.Forward : direction.Normalized();
        Vector3 at = _player.GlobalPosition + direction * _settings.SpawnDistance;
        float ground = _player.GlobalPosition.Y;
        var data = _terrain.Get("data").AsGodotObject();
        if (data != null)
        {
            float height = data.Call("get_height", new Vector3(at.X, 0, at.Z)).AsSingle();
            if (float.IsFinite(height)) ground = Mathf.Max(ground, height);
        }
        instance.Name = isTable ? $"Preview_Table001_{_selection.Selected - 6}" : isBed ? $"Preview_Bed001_{_selection.Selected - 2}" : isBook ? "Preview_Book001" : "Preview_Bookcase001";
        _spawned.AddChild(instance);
        instance.GlobalPosition = new Vector3(at.X, ground + (isBook ? _settings.BookHoverHeight : 0), at.Z);
        instance.Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
        _status.Text = $"Spawned {instance.Name} · {_spawned.GetChildCount()}/{MaxInstances}";
    }

    // =========================================================
    // Remove only session-owned previews. Real props, buildings and terrain are never touched.
    private void ClearSpawned()
    {
        if (_spawned == null) return;
        foreach (Node n in _spawned.GetChildren()) { _spawned.RemoveChild(n); n.QueueFree(); }
        _status.Text = "Cleared all debug models.";
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (_backdrop != null && _backdrop.Visible) Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
