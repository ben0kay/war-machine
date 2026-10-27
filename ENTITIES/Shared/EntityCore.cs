// Minimal mobile actor: isolated runtime settings and explicit lifecycle, with no simulation loop.
using Godot;

public partial class EntityCore : CharacterBody3D
{
    [Signal] public delegate void ActiveChangedEventHandler(bool active);
    [ExportGroup("Configuration")]
    [Export] public EntitySettings Settings { get; set; } = null!;
    [ExportGroup("References")]
    [Export] public Node3D VisualRoot { get; set; } = null!;

    public EntitySettings RuntimeSettings { get; private set; } = null!;
    public bool IsActive { get; private set; }
    private bool _initialized;
    private bool _exiting;

    // =========================================================
    // Children remain dormant until their Ready callbacks finish and the actor explicitly activates.
    public override void _EnterTree()
    {
        _exiting = false;
        ProcessMode = ProcessModeEnum.Disabled;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Copy the master once per actor; preserve runtime edits if this same actor leaves and re-enters.
    public override void _Ready()
    {
        RuntimeSettings ??= Settings != null ? (EntitySettings)Settings.Duplicate(true) : new EntitySettings();
        _initialized = true;
        if (RuntimeSettings.StartActive) Activate();
    }

    // =========================================================
    // Wake optional components through one transition; the base itself never starts a frame callback.
    public void Activate()
    {
        if (!_initialized || _exiting || !IsInsideTree() || IsActive) return;
        IsActive = true;
        ProcessMode = ProcessModeEnum.Inherit;
        OnActivated();
        EmitSignal(SignalName.ActiveChanged, true);
    }

    // =========================================================
    // Components stop timers/effects in their hook or signal handler before inherited processing sleeps.
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        Velocity = Vector3.Zero;
        OnDeactivated();
        EmitSignal(SignalName.ActiveChanged, false);
        SetProcess(false); SetPhysicsProcess(false);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    // =========================================================
    // Future species start only their attached capabilities here, after child Ready has completed.
    protected virtual void OnActivated() { }

    // =========================================================
    // Future species cancel timers, stop gait, hide beams and suspend sensing here; keep this idempotent.
    protected virtual void OnDeactivated() { }

    // =========================================================
    // Release external subscriptions even for an actor that was never active.
    protected virtual void OnEntityExitTree() { }

    // =========================================================
    // Stop before freeing/unloading; request Ready again for safe reuse of this node after reattachment.
    public override void _ExitTree()
    {
        _exiting = true;
        Deactivate();
        OnEntityExitTree();
        _initialized = false;
        RequestReady();
    }
}
