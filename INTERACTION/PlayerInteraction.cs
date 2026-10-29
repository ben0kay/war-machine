// IX0 scene-local focus owner. Hold E, aim, click; one coarse physics timer while focused, zero idle queries.
using Godot;
using System;

public partial class PlayerInteraction : Node
{
    public event Action<string>? PromptChanged;
    private PlayerController _player = null!;
    private IInteractionView _view = null!;
    private InteractionTargeting _targeting = null!;
    private InteractionSettings _settings = null!;
    private Timer _timer = null!;
    private bool _focused, _useRequested, _ready;
    private string _prompt = "";

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(PlayerController player, Camera3D camera, IInteractionView view, InteractionSettings settings)
    {
        if (_ready) return;
        _player = player; _view = view; _settings = (InteractionSettings)settings.Duplicate(true);
        _targeting = new InteractionTargeting(player, camera, view, _settings);
        _timer = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_timer); _timer.Timeout += UpdateTarget;
        player.Interaction = this; player.GameplayInputBlockChanged += OnInputBlocked; _ready = true;
    }

    // =========================================================
    // Releases/Escape clear focus even when GUI handles that event afterward.
    public override void _Input(InputEvent input)
    {
        if (!_ready || input is not InputEventKey key) return;
        if ((!key.Pressed && key.PhysicalKeycode == _settings.FocusKey)
            || (key.Pressed && key.PhysicalKeycode == Key.Escape)) SetFocused(false);
    }

    // =========================================================
    // GUI has first refusal. Interaction owns LMB while focused; E never changes mouse capture.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_ready || HandlePrimaryInput(input)) return;
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == _settings.FocusKey && CanFocus())
        { SetFocused(true); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // Weapon adapters call this too; correctness does not depend on scene-tree input dispatch order.
    public bool HandlePrimaryInput(InputEvent input)
    {
        if (!_ready || !_focused || input is not InputEventMouseButton mouse || mouse.ButtonIndex != MouseButton.Left) return false;
        bool allowed = CanFocus();
        if (!allowed) SetFocused(false);
        else if (mouse.Pressed) { _useRequested = true; _timer.Start(0.001); }
        GetViewport().SetInputAsHandled(); return true;
    }

    // =========================================================
    private bool CanFocus() => _settings.Enabled && IsInstanceValid(_player) && !_player.GameplayInputBlocked
        && !_player.IsDead && _player.IsInsideTree() && !_player.IsQueuedForDeletion() && GetWindow().HasFocus() && _view.CanFocus
        && GetViewport().GuiGetHoveredControl() == null && GetViewport().GuiGetFocusOwner() == null;

    // =========================================================
    private void SetFocused(bool focused)
    {
        if (_focused == focused) return;
        _focused = focused; _useRequested = false;
        if (IsInstanceValid(_player)) _player.SetInteractionMode(focused);
        if (_focused != focused) return; // A listener may synchronously cancel focus.
        if (focused)
        { Publish($"Aim at a nearby object · LMB use · release {OS.GetKeycodeString(_settings.FocusKey)} to exit"); _timer.Start(0.001); }
        else { _timer.Stop(); Publish(""); }
    }

    // =========================================================
    // Coarse mode-only refresh also detects moving doors/targets without any per-prop subscriptions.
    private void UpdateTarget()
    {
        if (!CanFocus() || !Input.IsPhysicalKeyPressed(_settings.FocusKey)) { SetFocused(false); return; }
        bool use = _useRequested; _useRequested = false;
        if (_targeting.TrySelect(out IInteractable? target, out InteractionContext context, out InteractionAction action))
        {
            if (use)
            {
                target!.TryInteract(context, action.Kind);
                if (!_focused) return;
                if (target is not Node node || !IsInstanceValid(node) || node.IsQueuedForDeletion()
                    || !target.TryGetAction(context, out action))
                { Publish("No available action"); ScheduleNext(); return; }
            }
            Publish($"LMB · {action.Label}  [release {OS.GetKeycodeString(_settings.FocusKey)} to exit]");
        }
        else Publish($"No reachable object · release {OS.GetKeycodeString(_settings.FocusKey)} to exit");
        ScheduleNext();
    }

    // =========================================================
    private void ScheduleNext()
    {
        if (_focused) _timer.Start(float.IsFinite(_settings.TargetIntervalSeconds)
            ? Mathf.Clamp(_settings.TargetIntervalSeconds, 0.05f, 0.5f) : 0.15f);
    }

    // =========================================================
    private void Publish(string prompt) { if (_prompt == prompt) return; _prompt = prompt; PromptChanged?.Invoke(prompt); }
    // =========================================================
    private void OnInputBlocked(bool blocked) { if (blocked) SetFocused(false); }
    // =========================================================
    public override void _Notification(int what)
    { if (_ready && what == NotificationApplicationFocusOut) SetFocused(false); }

    // =========================================================
    public override void _ExitTree()
    {
        if (!_ready) return;
        SetFocused(false); _timer.Stop();
        if (IsInstanceValid(_player)) { _player.GameplayInputBlockChanged -= OnInputBlocked; _player.Interaction = null; }
        PromptChanged = null; _ready = false;
    }
}
