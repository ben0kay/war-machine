// Preview-only adapter for the real Robot_001: explicit route, test terrain sampler and optional test-player target.
using Godot;

public partial class RobotPlacement : Node3D
{
    [Export] public RobotController Robot { get; set; } = null!;
    [Export] public Node3D RouteMarkers { get; set; } = null!;
    [Export] public Vector2 SpawnOffset { get; set; } = new(-24, 8);
    private GodotObject _terrainData = null!;

    // =========================================================
    // Native actor lifecycle stays dormant until saved terrain and player placement have succeeded.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Editable markers are local to this adapter; generic robot code receives only cached world points.
    public bool Initialize(Node3D terrain, Node3D player, WorldSettings settings, Godot.Collections.Array<Rect2>? occupied = null)
    {
        Robot.Deactivate();
        _terrainData = terrain.Get("data").AsGodotObject();
        Vector2 origin = settings.VillageEnabled ? settings.VillageOriginXZ : settings.SpawnXZ;
        GlobalPosition = new Vector3(origin.X + SpawnOffset.X, 0, origin.Y + SpawnOffset.Y);
        float scale = Mathf.Clamp(Robot.RuntimeRobotSettings?.ModelScale ?? 1, 0.25f, 4);
        float clearance = 8 * scale + 2;
        var markers = RouteMarkers.GetChildren();
        if (markers.Count < 3 || markers.Count > 4) return Fail("Use three or four preview patrol markers.");
        var route = new Vector3[markers.Count];
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] is not Node3D marker) return Fail("Preview robot route children must be 3D markers.");
            route[i] = GroundTarget(marker.GlobalPosition);
            if (!float.IsFinite(route[i].Y)) return Fail("Preview terrain is missing below a robot waypoint.");
            marker.GlobalPosition = route[i];
        }
        // Check complete edges and the feet corridor once, rather than assuming clear endpoint heights.
        for (int i = 0; i < route.Length; i++)
        {
            Vector3 a = route[i], b = route[(i + 1) % route.Length];
            if (a.DistanceSquaredTo(b) < 1) return Fail("Preview robot waypoints must be at least one metre apart.");
            int steps = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / 2));
            for (int step = 0; step <= steps; step++)
            {
                Vector3 p = a.Lerp(b, (float)step / steps);
                if (occupied != null)
                    foreach (Rect2 footprint in occupied)
                        if (footprint.Grow(clearance).HasPoint(new Vector2(p.X, p.Z)))
                            return Fail("Robot swept body/feet corridor intersects village scenery.");
                float ground = GroundTarget(p).Y;
                if (!float.IsFinite(ground)) return Fail("Preview robot route crosses missing terrain.");
                foreach (var offset in FootCorridor)
                {
                    float nearby = GroundTarget(p + offset * scale).Y;
                    if (!float.IsFinite(nearby) || Mathf.Abs(nearby - ground) > 1.5f)
                        return Fail("Move the preview robot route to level terrain away from the hill.");
                }
            }
        }
        Robot.GlobalPosition = route[^1];
        Vector3 heading = route[0] - route[^1]; heading.Y = 0;
        Robot.Rotation = new Vector3(0, Mathf.Atan2(-heading.X, -heading.Z), 0);
        Robot.InitializeEncounter(route, GroundTarget, player);
        Show();
        if (Robot.RuntimeRobotSettings.EncounterEnabled) Robot.Activate();
        GD.Print($"Top-down Robot_001: real shared patrol/scanner initialized at {Robot.GlobalPosition}.");
        return true;
    }

    // =========================================================
    // Use the same authored closed route for initial tree/rock exclusions, independent of player spawn.
    public Godot.Collections.Array<Rect2> GetRouteReservations(WorldSettings settings, float margin)
    {
        var areas = new Godot.Collections.Array<Rect2>();
        var markers = RouteMarkers.GetChildren();
        Vector2 origin = (settings.VillageEnabled ? settings.VillageOriginXZ : settings.SpawnXZ) + SpawnOffset;
        float scale = Mathf.Clamp(Robot.RuntimeRobotSettings?.ModelScale ?? 1, 0.25f, 4);
        margin = Mathf.Max(margin, 8 * scale + 2);
        for (int i = 0; i < markers.Count; i++)
        {
            Vector3 a = ((Node3D)markers[i]).Position, b = ((Node3D)markers[(i + 1) % markers.Count]).Position;
            Vector2 low = new Vector2(a.X, a.Z).Min(new Vector2(b.X, b.Z)) - Vector2.One * margin;
            Vector2 high = new Vector2(a.X, a.Z).Max(new Vector2(b.X, b.Z)) + Vector2.One * margin;
            areas.Add(new Rect2(origin + low, high - low));
        }
        return areas;
    }

    private static readonly Vector3[] FootCorridor = { Vector3.Left * 5, Vector3.Right * 5, Vector3.Forward * 5, Vector3.Back * 5 };

    // =========================================================
    // The real patrol uses this sampler for body support and feet; no normal-world data/paths are consulted.
    private Vector3 GroundTarget(Vector3 point)
    {
        point.Y = _terrainData.Call("get_height", point).AsSingle() + 0.05f;
        return point;
    }

    // =========================================================
    // Invalid placement leaves the robot hidden/dormant while the rest of the preview remains usable.
    private bool Fail(string reason) { Robot.Deactivate(); Hide(); GD.PushError(reason); return false; }

    // =========================================================
    // Reuse actor shutdown for all timers/audio/effects, without introducing another behavior loop.
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(Robot)) Robot.Deactivate(); }
}
