// Optional waypoint component: one motion callback while travelling, one stopped timer, explicit pause/resume.
using Godot;
using System;

public partial class PatrolController : Node
{
    public enum PatrolState { Stopped, Walking, Settling, Waiting, Paused }
    public event Action? WalkingStarted;
    public event Action<int>? Arrived;
    public event Action? StoppedAtWaypoint;
    public event Action? ReadyToDepart;
    public event Action<float>? SpeedChanged;
    public event Action? Moved;
    public PatrolState State { get; private set; }
    public int WaypointIndex { get; private set; }
    private EntityCore _actor = null!;
    private PatrolSettings _settings = null!;
    private Vector3[] _route = Array.Empty<Vector3>();
    private Timer _timer = null!;
    private PatrolState _resumeState;
    private double _remaining;
    private float _speed, _lastReported = -1;
    private bool _initialized, _resumeOnActivate, _waitCompleted;
    private Func<Vector3, Vector3>? _groundTarget;

    // =========================================================
    // No processing until a caller injects an actor and cached world-space route.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Reinitialization replaces subscriptions once; route points are copied rather than searched each frame.
    public void Initialize(EntityCore actor, PatrolSettings settings, Vector3[] route, Func<Vector3, Vector3>? groundTarget = null)
    {
        if (_initialized) { StopPatrol(); _actor.ActiveChanged -= OnActorActiveChanged; }
        _actor = actor; _settings = (PatrolSettings)settings.Duplicate(true); _route = (Vector3[])route.Clone();
        _groundTarget = groundTarget; _waitCompleted = false;
        _actor.FloorSnapLength = 0.5f;
        if (_timer == null)
        {
            _timer = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
            AddChild(_timer); _timer.Timeout += OnTimeout;
        }
        _actor.ActiveChanged += OnActorActiveChanged; _initialized = true;
    }

    // =========================================================
    // UI changes update the isolated motion settings without resetting the route or dwell timer.
    public void ApplySettings(PatrolSettings settings)
    { if (_initialized) _settings = (PatrolSettings)settings.Duplicate(true); }

    // =========================================================
    // Start is idempotent; it never silently resets an already-running route.
    public void StartPatrol()
    {
        if (!_initialized || _route.Length == 0 || State != PatrolState.Stopped) return;
        WaypointIndex = 0; BeginWalking();
        if (!_actor.IsActive) { PausePatrol(); _resumeOnActivate = true; }
    }

    // =========================================================
    // Preserve the current waypoint, phase and timer remainder without retaining motion.
    public void PausePatrol()
    {
        if (!_initialized || State == PatrolState.Paused || State == PatrolState.Stopped) return;
        _resumeState = State; _remaining = _timer.TimeLeft; _timer.Stop();
        State = PatrolState.Paused; SetPhysicsProcess(false); Halt();
    }

    // =========================================================
    // A paused zero-time wait departs directly, avoiding duplicate arrival/scan callbacks.
    public void ResumePatrol()
    {
        if (!_initialized || !_actor.IsActive || State != PatrolState.Paused) return;
        State = _resumeState;
        if (State == PatrolState.Walking) { SetPhysicsProcess(true); WalkingStarted?.Invoke(); }
        else if (State == PatrolState.Waiting && _waitCompleted) NextWaypoint();
        else if (_remaining > 0) _timer.Start(_remaining);
        else if (State == PatrolState.Waiting) NextWaypoint();
        else AdvancePhase();
    }

    // =========================================================
    // Stop clears timer and restart state; a later Start begins at the first waypoint.
    public void StopPatrol()
    {
        if (!_initialized) return;
        _timer.Stop(); _remaining = 0; _resumeOnActivate = false;
        State = PatrolState.Stopped; SetPhysicsProcess(false); Halt();
    }

    // =========================================================
    // Turn towards the target, brake by remaining distance and move the physics root only.
    public override void _PhysicsProcess(double delta)
    {
        if (!_actor.IsActive || State != PatrolState.Walking) return;
        float dt = (float)delta;
        // Terrain3D collision follows the viewer. A distant actor must not depend on its collision patch.
        if (_groundTarget != null && !GroundActor()) return;
        Vector3 offset = _route[WaypointIndex] - _actor.GlobalPosition; offset.Y = 0;
        float distance = offset.Length();
        if (distance <= Mathf.Max(0.05f, _settings.ArrivalRadius))
        {
            State = PatrolState.Settling; SetPhysicsProcess(false); Halt();
            _timer.Start(Mathf.Max(0.01f, _settings.SettleSeconds)); Arrived?.Invoke(WaypointIndex); return;
        }
        Vector3 direction = offset / distance;
        float wantedYaw = Mathf.Atan2(-direction.X, -direction.Z);
        float yawDelta = Mathf.Wrap(wantedYaw - _actor.Rotation.Y, -Mathf.Pi, Mathf.Pi);
        float maxTurn = Mathf.DegToRad(Mathf.Max(1, _settings.TurnDegreesPerSecond)) * dt;
        _actor.Rotation = new Vector3(0, _actor.Rotation.Y + Mathf.Clamp(yawDelta, -maxTurn, maxTurn), 0);
        float braking = Mathf.Max(0.1f, _settings.Braking);
        float desired = Mathf.Min(Mathf.Max(0, _settings.Speed), Mathf.Sqrt(2 * braking * Mathf.Max(0, distance - _settings.ArrivalRadius * 0.5f)));
        desired *= Mathf.Max(0, (-_actor.GlobalBasis.Z).Dot(direction));
        _speed = Mathf.MoveToward(_speed, desired, (desired < _speed ? braking : Mathf.Max(0.1f, _settings.Acceleration)) * dt);
        Vector3 horizontal = direction * Mathf.Min(_speed, distance / Mathf.Max(dt, 0.001f));
        float vertical = _actor.IsOnFloor() ? -0.1f : _actor.Velocity.Y - _settings.Gravity * dt;
        if (_groundTarget != null)
        {
            Vector3 nextGround = _groundTarget(_actor.GlobalPosition + horizontal * dt);
            if (!float.IsFinite(nextGround.Y)) { MissingGround(); return; }
            vertical = (nextGround.Y - _actor.GlobalPosition.Y) / Mathf.Max(dt, 0.001f);
        }
        _actor.Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        _actor.MoveAndSlide();
        if (_groundTarget != null && !GroundActor()) return;
        ReportSpeed(new Vector2(_actor.GetRealVelocity().X, _actor.GetRealVelocity().Z).Length());
        Moved?.Invoke();
    }

    // =========================================================
    // Explicit height support keeps a terrain actor grounded even when dynamic physics coverage moves away.
    private bool GroundActor()
    {
        Vector3 position = _actor.GlobalPosition;
        Vector3 ground = _groundTarget!(position);
        if (!float.IsFinite(ground.Y)) { MissingGround(); return false; }
        position.Y = ground.Y; _actor.GlobalPosition = position;
        _actor.Velocity = new Vector3(_actor.Velocity.X, 0, _actor.Velocity.Z);
        return true;
    }

    // =========================================================
    // Missing saved terrain pauses safely instead of integrating gravity into an endless fall.
    private void MissingGround()
    { PausePatrol(); GD.PushWarning("Patrol paused: no finite terrain height at actor/next step."); }

    // =========================================================
    // One timer owns both stationary phases; no waiting physics callback remains enabled.
    private void OnTimeout() => AdvancePhase();

    // =========================================================
    // Signals may synchronously pause/stop the component, so recheck state before proceeding.
    private void AdvancePhase()
    {
        if (State == PatrolState.Settling)
        {
            State = PatrolState.Waiting; _timer.Start(Mathf.Max(0.01f, _settings.WaitSeconds));
            StoppedAtWaypoint?.Invoke();
        }
        else if (State == PatrolState.Waiting)
        {
            // Timer.TimeLeft may still contain a tiny positive remainder inside its timeout callback.
            // Record completion before listeners pause us for scanning, rather than replaying this wait.
            _waitCompleted = true;
            ReadyToDepart?.Invoke();
            if (State == PatrolState.Waiting) NextWaypoint();
        }
    }

    // =========================================================
    // Advance once; finishing a non-looping route remains dormant.
    private void NextWaypoint()
    {
        if (++WaypointIndex >= _route.Length)
        {
            if (!_settings.Loop) { StopPatrol(); return; }
            WaypointIndex = 0;
        }
        BeginWalking();
    }

    // =========================================================
    // Wake only for the moving phase; arrival always shuts this callback down.
    private void BeginWalking() { _waitCompleted = false; State = PatrolState.Walking; SetPhysicsProcess(true); WalkingStarted?.Invoke(); }

    // =========================================================
    // Publish actual speed only when changed; zero always settles the visual gait.
    private void ReportSpeed(float speed)
    {
        if (Mathf.Abs(speed - _lastReported) < 0.01f && !(speed == 0 && _lastReported != 0)) return;
        _lastReported = speed; SpeedChanged?.Invoke(speed);
    }

    // =========================================================
    // Clearing velocity does not move the actor or force a new floor query while waiting.
    private void Halt() { _speed = 0; _actor.Velocity = Vector3.Zero; ReportSpeed(0); }

    // =========================================================
    // Actor deactivation pauses timers explicitly before inherited process mode is disabled.
    private void OnActorActiveChanged(bool active)
    {
        if (!active) { _resumeOnActivate = State != PatrolState.Paused && State != PatrolState.Stopped; PausePatrol(); }
        else if (_resumeOnActivate) { _resumeOnActivate = false; ResumePatrol(); }
    }

    // =========================================================
    // Stop and detach even when this child exits before the actor's own cleanup hook.
    public override void _ExitTree()
    {
        if (!_initialized) return;
        StopPatrol(); _actor.ActiveChanged -= OnActorActiveChanged;
        _timer.Timeout -= OnTimeout; _initialized = false;
    }
}
