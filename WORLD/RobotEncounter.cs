// World-owned encounter placement: saved terrain sampling and explorer-event relevance; no terrain writes or idle loop.
using Godot;

public partial class RobotEncounter : Node3D
{
    [Export] public Vector2 SpawnOffset { get; set; } = new(-10, -13);
    [Export] public RobotController Robot { get; set; } = null!;
    [Export] public Node3D RouteMarkers { get; set; } = null!;
    private ExplorationController _explorer = null!;
    private GodotObject _terrainData = null!;
    private bool _initialized, _distanceSleeping;
    private Vector3 _viewerPosition;

    // =========================================================
    // Placement happens only after the existing world owner initializes saved Terrain3D.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Author-visible markers define the route; cache ground points once, never rebake or flatten terrain.
    public void Initialize(Node3D terrain, ExplorationController explorer, WorldConfig world)
    {
        Disconnect();
        _terrainData = terrain.Get("data").AsGodotObject(); _explorer = explorer;
        GlobalPosition = new Vector3(world.SpawnXZ.X + SpawnOffset.X, 0, world.SpawnXZ.Y + SpawnOffset.Y);
        var children = RouteMarkers.GetChildren();
        var route = new Vector3[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Node3D marker) { Fail("Route children must be 3D markers."); return; }
            route[i] = GroundTarget(marker.GlobalPosition);
            if (!float.IsFinite(route[i].Y)) { Fail("Missing terrain below robot route."); return; }
            marker.GlobalPosition = route[i];
        }
        if (route.Length < 2) { Fail("Robot needs at least two route markers."); return; }
        // Check every route edge, including closure, and the foot corridor, not just four endpoints.
        for (int i = 0; i < route.Length; i++)
        {
            Vector3 a = route[i], b = route[(i + 1) % route.Length];
            int steps = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / 2));
            for (int j = 0; j <= steps; j++)
            {
                Vector3 point = a.Lerp(b, (float)j / steps);
                float height = GroundTarget(point).Y;
                foreach (Vector3 offset in FootCorridor)
                {
                    float nearby = GroundTarget(point + offset).Y;
                    if (!float.IsFinite(height) || !float.IsFinite(nearby) || Mathf.Abs(nearby - height) > 1.5f)
                    { Fail("Robot route is too steep or missing saved terrain. Move the encounter markers to a meadow."); return; }
                }
            }
        }
        Robot.GlobalPosition = route[^1] + Vector3.Up * 0.08f;
        Vector3 heading = route[0] - route[^1]; heading.Y = 0;
        Robot.Rotation = new Vector3(0, Mathf.Atan2(-heading.X, -heading.Z), 0);
        Robot.InitializeEncounter(route, GroundTarget, explorer.GetNode<Camera3D>("Camera3D"));
        _initialized = true; _viewerPosition = explorer.GlobalPosition;
        explorer.ViewPositionChanged += OnViewMoved;
        Robot.Patrol.Moved += CheckDistance;
        Robot.StageChanged += OnStageChanged;
        Robot.Sensor.TargetEntered += OnTargetEntered;
        Robot.Sensor.TargetExited += OnTargetExited;
        OnViewMoved(_viewerPosition);
        GD.Print($"Robot_001 patrol at {Robot.GlobalPosition}; four-marker loop near spawn. Cyan scan follows each stop.");
    }

    private static readonly Vector3[] FootCorridor = { new(-5, 0, 0), new(5, 0, 0), new(0, 0, -5), new(0, 0, 5) };

    // =========================================================
    // Called only on initial placement/new foot targets; preserve x/z and leave no terrain subscriptions.
    private Vector3 GroundTarget(Vector3 point)
    {
        float height = _terrainData.Call("get_height", point).AsSingle();
        return new Vector3(point.X, height + 0.03f, point.Z);
    }

    // =========================================================
    // Existing explorer transform events wake dormant actors without a distance polling callback.
    private void OnViewMoved(Vector3 position) { _viewerPosition = position; CheckDistance(); }

    // =========================================================
    // Active locomotion also checks relevance in its existing callback; asleep actors never tick here.
    private void CheckDistance()
    {
        if (!_initialized) return;
        var s = Robot.RuntimeRobotSettings;
        if (!s.EncounterEnabled) { Robot.Deactivate(); return; }
        float wake = Mathf.Max(1, s.WakeDistance), sleep = Mathf.Max(wake + 1, s.SleepDistance);
        float squared = Robot.GlobalPosition.DistanceSquaredTo(_viewerPosition);
        if (squared > sleep * sleep && Robot.IsActive)
        { _distanceSleeping = true; Robot.Deactivate(); }
        else if (_distanceSleeping && squared <= wake * wake && Robot.IsVisibleInTree())
        { _distanceSleeping = false; Robot.Activate(); }
    }

    // =========================================================
    // Explicit debug/config changes wake the encounter directly, without polling its master resource.
    public void SetEncounterEnabled(bool enabled)
    {
        if (!_initialized) return;
        Robot.RuntimeRobotSettings.EncounterEnabled = enabled;
        if (!enabled) { _distanceSleeping = false; Robot.Deactivate(); }
        else { Robot.Activate(); CheckDistance(); }
    }

    // =========================================================
    // Low-frequency console status gives a useful diagnosis without adding UI or an idle process loop.
    private void OnStageChanged(string stage) => GD.Print($"Robot_001: {stage}");

    // =========================================================
    // Sensing is demonstrative only: no chase, damage or gameplay consequence.
    private void OnTargetEntered(Node3D target) => GD.Print($"Robot_001 scan sees {target.Name}");

    // =========================================================
    // Report cone/LOS exit once, never every frame.
    private void OnTargetExited(Node3D target) => GD.Print($"Robot_001 scan lost {target.Name}");

    // =========================================================
    // Invalid placement stops the actor visibly in logs rather than regenerating terrain or falling forever.
    private void Fail(string message) { Robot.Deactivate(); Robot.Hide(); GD.PushError(message); }

    // =========================================================
    // Reinitialization/unload releases all references to the explorer and robot events.
    private void Disconnect()
    {
        if (!_initialized) return;
        if (GodotObject.IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= OnViewMoved;
        if (GodotObject.IsInstanceValid(Robot))
        {
            Robot.Patrol.Moved -= CheckDistance; Robot.StageChanged -= OnStageChanged;
            Robot.Sensor.TargetEntered -= OnTargetEntered; Robot.Sensor.TargetExited -= OnTargetExited;
            Robot.Deactivate();
        }
        _initialized = false;
    }

    // =========================================================
    // Entire encounter unloads without a global registry or lingering viewer callback.
    public override void _ExitTree() => Disconnect();
}
