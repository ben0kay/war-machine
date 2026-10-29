// Zombie adapter for shared fixed-point investigation; owns AI goals, sight/hit triggers and safe local wandering.
// Uses ZombieController's existing decision timer/movement; no independent callbacks or navigation system.
using Godot;

public partial class ZombieController
{
    private InvestigationMemory _investigation = null!;
    private bool _anchorPending;
    private float _lookYaw;
    private PhysicsShapeQueryParameters3D _wanderQuery = null!;
    public bool IsInvestigating => _investigation?.IsActive == true;
    protected bool IsPursuing => IsChasing || Attacking || _investigation?.Phase == InvestigationPhase.Travel;

    // =========================================================
    private void ClearInvestigation()
    { _investigation?.Clear(); _anchorPending = false; _stuck = 0; }

    // =========================================================
    private bool InvestigationEnabled(InvestigationClue clue)
        => clue == InvestigationClue.Sound ? _config.HearingEnabled
            : clue == InvestigationClue.Hit ? _config.HitInvestigationEnabled : _config.LastSeenInvestigationEnabled;

    // =========================================================
    // Only real sight can refresh a visual clue. Hidden targets never update an ongoing episode's fixed point.
    private void UpdateInvestigation(bool wasChasing, ulong now, bool targetValid)
    {
        if (IsChasing)
        {
            _investigation.Observe(_target!.GlobalPosition); _anchorPending = false;
            if (!wasChasing) _stuck = 0;
            _goal = _investigation.Point; _hasGoal = true; return;
        }
        if (wasChasing)
        {
            if (targetValid && _investigation.HasPoint && InvestigationEnabled(_investigation.Clue)) BeginInvestigation(now);
            else FinishInvestigation();
        }
        if (!IsInvestigating) return;
        if ((!targetValid && _investigation.Clue != InvestigationClue.Sound) || !InvestigationEnabled(_investigation.Clue) || _investigation.Expired(now))
        { FinishInvestigation(); return; }
        if (_investigation.Phase == InvestigationPhase.Look)
        { SetMobilityFacing(_lookYaw + _investigation.LookOffset(now)); UpdatePose(0); }
    }

    // =========================================================
    // Called for actual bullet contacts before flinch gating, never for nearby gunshot sounds/Q/bat hits.
    private void InvestigateBulletHit(Node3D? source)
    {
        if (!_config.HitInvestigationEnabled || !IsInstanceValid(source) || source == this || source != _target
            || !source!.IsInsideTree() || source.IsQueuedForDeletion()) return;
        _pendingSound = false; _soundRespondAt = 0;
        Vector3 point = source.GlobalPosition; // Snapshot once at contact; never retain a live source reference here.
        if (!_investigation.Remember(point, InvestigationClue.Hit)) return;
        // Confirmed pursuit retains priority; if sight is lost next tick, this newest clue can start the episode.
        if (IsChasing) return;
        BeginInvestigation(Time.GetTicksMsec());
    }

    // =========================================================
    private void BeginInvestigation(ulong now, Vector3? commitmentWeights = null, float durationScale = 1)
    {
        if (!_investigation.Begin(GlobalPosition, _locomotion.Speed(_config.ChaseSpeed), now, _random, commitmentWeights, durationScale)) return;
        _goal = _investigation.Destination; _hasGoal = true; _anchorPending = false;
        _stuck = 0; _restUntil = 0;
        if (_locomotion.CanDrive) SetPhysicsProcess(true);
    }

    // =========================================================
    private void InvestigationArrived()
    {
        _hasGoal = false;
        if (!_investigation.Arrive(Time.GetTicksMsec(), _random)) { FinishInvestigation(); return; }
        _lookYaw = VisualRoot.Rotation.Y; _stuck = 0;
    }

    // =========================================================
    // Species movement still owns settling/home anchoring; shared memory only owns the episode.
    private void FinishInvestigation()
    { _investigation.Clear(); _anchorPending = true; Rest(); }

    // =========================================================
    // Newly selected destinations need actual walkable support, body clearance and a clear direct sweep.
    // Four attempts maximum per wander selection; failed selection rests instead of walking into a trunk.
    private bool TryChooseWanderGoal()
    {
        bool found = GroundGoalSampler.TryWander(this, GetWorld3D().DirectSpaceState,
            _groundRay, _wanderQuery, _bodyShape, _random, _home,
            SafePolicy(_config.WanderRadius, 0, 100), out Vector3 destination);
        if (found) { _goal = destination; _hasGoal = true; }
        return found;
    }

    // =========================================================
    private static float SafePolicy(float value, float minimum, float maximum)
        => float.IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : minimum;

    // =========================================================
    protected override void OnEntityExitTree()
    { UnsubscribeHearing(); _soundDispatcher = null; ClearInvestigation(); _target = null; _sightTarget = null; _visibility = null; _groundHeight = null; }
}
