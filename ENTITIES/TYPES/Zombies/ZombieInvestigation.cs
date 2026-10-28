// PER2: last-seen memory, per-episode persistence and safe local wandering.
// Uses ZombieController's existing decision timer/movement; no independent callbacks or navigation system.
using Godot;

public partial class ZombieController
{
    private enum SearchState { None, Travel, Look }
    private SearchState _search;
    private Vector3 _lastSeen;
    private bool _remembered, _thorough, _anchorPending;
    private ulong _searchUntil, _lookUntil;
    private float _lookYaw;
    private PhysicsShapeQueryParameters3D _wanderQuery = null!;
    public bool IsInvestigating => _search != SearchState.None;
    protected bool IsPursuing => IsChasing || _search == SearchState.Travel;

    // =========================================================
    // Clear clues on reset/deactivation. Keeping the existing target registration allows normal reactivation.
    private void ClearInvestigation()
    {
        _search = SearchState.None; _remembered = _thorough = _anchorPending = false;
        _lastSeen = Vector3.Zero; _searchUntil = _lookUntil = 0; _stuck = 0;
    }

    // =========================================================
    // Only confirmed sight can refresh this coordinate. Hidden target transforms never become goals.
    private void UpdateInvestigation(bool wasChasing, ulong now, bool targetValid)
    {
        if (IsChasing)
        {
            _lastSeen = _target!.GlobalPosition; _remembered = true;
            _search = SearchState.None; _anchorPending = false;
            if (!wasChasing) _stuck = 0;
            _goal = _lastSeen; _hasGoal = true; return;
        }
        if (wasChasing)
        {
            if (targetValid && _remembered && _config.LastSeenInvestigationEnabled) BeginInvestigation(now);
            else FinishInvestigation();
        }
        if (!IsInvestigating) return;
        if (!targetValid || !_config.LastSeenInvestigationEnabled || now >= _searchUntil)
        { FinishInvestigation(); return; }
        if (_search == SearchState.Look)
        {
            if (now >= _lookUntil) { FinishInvestigation(); return; }
            // A few modest turns on existing sight ticks, while grounded physics remains asleep.
            float turn = Mathf.Sin((float)(_lookUntil - now) * 0.006f) * 0.55f;
            SetMobilityFacing(_lookYaw + turn);
            UpdatePose(0);
        }
    }

    // =========================================================
    // Draw once per genuine sight-loss episode, from the actor's seeded RNG. Timeout scales with travel.
    private void BeginInvestigation(ulong now)
    {
        float early = SafePolicy(_config.InvestigationWeights.X, 0, 100);
        float middle = SafePolicy(_config.InvestigationWeights.Y, 0, 100);
        float full = SafePolicy(_config.InvestigationWeights.Z, 0, 100);
        float total = early + middle + full;
        float choice = total > 0 ? _random.Randf() * total : 0;
        _thorough = total == 0 || choice >= early + middle;
        float fraction = _thorough ? 1 : SamplePolicy(choice < early
            ? _config.EarlyInvestigationFraction : _config.ModerateInvestigationFraction, 0.05f, 1);
        _goal = GlobalPosition.Lerp(_lastSeen, fraction); _hasGoal = true;
        Vector3 distance = _goal - GlobalPosition; distance.Y = 0;
        float travel = distance.Length() / SafePolicy(_locomotion.Speed(_config.ChaseSpeed), 0.1f, 30);
        float seconds = travel * SamplePolicy(_config.InvestigationTimeScale, 1, 3)
            + SafePolicy(_config.InvestigationTimeAllowance, 0.5f, 20);
        seconds = Mathf.Min(seconds, SafePolicy(_config.InvestigationMaxSeconds, 1, 120));
        _searchUntil = now + Milliseconds(seconds); _search = SearchState.Travel;
        _stuck = 0; _restUntil = 0;
    }

    // =========================================================
    // Arrival is checked by the existing movement callback; only thorough zombies dwell/look around.
    private void InvestigationArrived()
    {
        _hasGoal = false;
        if (!_thorough) { FinishInvestigation(); return; }
        _search = SearchState.Look; _lookYaw = VisualRoot.Rotation.Y;
        _lookUntil = Time.GetTicksMsec() + Milliseconds(SamplePolicy(_config.InvestigationLookSeconds, 0.1f, 5));
        _stuck = 0;
    }

    // =========================================================
    // Stop first; anchor is recorded later from the settled physical body, never from an unreachable clue.
    private void FinishInvestigation()
    {
        _search = SearchState.None; _remembered = false; _lastSeen = Vector3.Zero;
        _searchUntil = _lookUntil = 0; _anchorPending = true;
        Rest();
    }

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
    // Normalize swapped/nonfinite ranges once per episode, not every physics frame.
    private float SamplePolicy(Vector2 range, float minimum, float maximum)
    {
        float a = SafePolicy(range.X, minimum, maximum), b = SafePolicy(range.Y, minimum, maximum);
        return _random.RandfRange(Mathf.Min(a, b), Mathf.Max(a, b));
    }

    // =========================================================
    private static float SafePolicy(float value, float minimum, float maximum)
        => float.IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : minimum;

    // =========================================================
    private static ulong Milliseconds(float seconds) => (ulong)Mathf.CeilToInt(seconds * 1000);

    // =========================================================
    protected override void OnEntityExitTree()
    { ClearInvestigation(); _target = null; _sightTarget = null; _visibility = null; _groundHeight = null; }
}
