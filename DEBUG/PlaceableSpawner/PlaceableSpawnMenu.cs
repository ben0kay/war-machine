// F1 catalogue-driven placeable preview: canonical GLB wrappers, exported enum variations, no physics.
// File scan runs once at startup. Key/button events only; no idle process or spawn-index switches.
using Godot;
using System.Collections.Generic;

public partial class PlaceableSpawnMenu : CanvasLayer
{
    private const int MaxInstances = 24;
    private readonly List<PlaceableEntry> _entries = new();
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
    // Discover canonical objects/props once; options and variant IDs derive from cached entries.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _backdrop = GetNode<ColorRect>("Backdrop");
        _selection = GetNode<OptionButton>("Backdrop/Panel/Margin/Rows/Selection");
        _status = GetNode<Label>("Backdrop/Panel/Margin/Rows/Status");
        _settings = GD.Load<PlaceableSpawnSettings>("res://DEBUG/PlaceableSpawner/Configuration/PlaceableSpawnMaster.tres");
        _backdrop.Hide();
        _entries.AddRange(PlaceableCatalog.Discover());
        PopulateOptions();
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Spawn").Pressed += SpawnSelected;
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Actions/Clear").Pressed += ClearSpawned;
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Actions/Close").Pressed += () => CloseMenu(true);
    }

    // =========================================================
    // Category separators are disabled entries; OptionButton IDs always index _entries, never menu rows.
    private void PopulateOptions()
    {
        _selection.Clear();
        string category = "";
        int first = -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            PlaceableEntry entry = _entries[i];
            if (category != entry.Category) { category = entry.Category; _selection.AddSeparator(category); }
            _selection.AddItem(entry.DisplayName, i);
            if (first < 0) first = _selection.ItemCount - 1;
        }
        if (first >= 0) _selection.Select(first);
        else { _selection.AddItem("No GLB placeables found"); _selection.Disabled = true; }
        GetNode<Button>("Backdrop/Panel/Margin/Rows/Spawn").Disabled = first < 0;
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
        _status.Text = _entries.Count == 0 ? "No eligible GLB object scenes found." : "Temporary placeables · solid collision follows each prefab";
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
    // Preview any discovered asset; no name/index checks. Source scene and authored size master remain intact.
    private void SpawnSelected()
    {
        if (_player == null || _camera == null || _terrain == null || _spawned == null || _settings == null)
        { _status.Text = "World is not ready."; return; }
        if (_spawned.GetChildCount() >= MaxInstances)
        { _status.Text = "Preview limit reached. Clear models."; return; }

        int selectedId = _selection.GetSelectedId();
        if (selectedId < 0 || selectedId >= _entries.Count)
        { _status.Text = "Select a placeable first."; return; }
        PlaceableEntry choice = _entries[selectedId];
        Node3D? instance = choice.CreateInstance();
        if (instance == null) { _status.Text = "Could not instantiate selected placeable; see Godot Output."; return; }

        Vector3 direction = -_camera.GlobalBasis.Z;
        direction.Y = 0;
        direction = direction.LengthSquared() < 0.0001f ? Vector3.Forward : direction.Normalized();
        float spawnDistance = Mathf.Max(_settings.SpawnDistance, choice.MinimumSpawnDistance);
        Vector3 at = _player.GlobalPosition + direction * spawnDistance;
        float ground = _player.GlobalPosition.Y;
        var data = _terrain.Get("data").AsGodotObject();
        if (data != null)
        {
            float height = data.Call("get_height", new Vector3(at.X, 0, at.Z)).AsSingle();
            if (float.IsFinite(height)) ground = Mathf.Max(ground, height);
        }
        string suffix = choice.VariantName.Length == 0 ? "" : "_" + choice.VariantName.Replace(' ', '_');
        instance.Name = "Preview_" + choice.AssetId + suffix;
        _spawned.AddChild(instance);
        instance.GlobalPosition = new Vector3(at.X, ground + (choice.HoverForPreview ? _settings.BookHoverHeight : 0), at.Z);
        instance.Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
        _status.Text = $"Spawned {choice.DisplayName} · {_spawned.GetChildCount()}/{MaxInstances}";
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
    { if (_backdrop != null && _backdrop.Visible) Input.MouseMode = Input.MouseModeEnum.Visible; }
}
