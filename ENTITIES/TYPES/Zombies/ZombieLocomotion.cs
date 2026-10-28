// Injury-derived mobility, safe low-body transitions and cached procedural poses. No AI, timers or engine callbacks.
using Godot;

public enum ZombieMobility { Walk, Limp, Hop, Crawl, Immobilized }

public sealed class ZombieLocomotion
{
    private readonly ZombieLocomotionSettings _settings;
    private readonly CharacterBody3D _actor;
    private readonly CollisionShape3D _body;
    private readonly Shape3D _standing, _low;
    private readonly Transform3D _standingAt;
    private readonly PhysicsShapeQueryParameters3D _clearance;
    private readonly Node3D _visual, _head, _leftArm, _rightArm, _leftLeg, _rightLeg, _leftKnee, _rightKnee;
    private readonly Vector3 _headRest;
    private AnatomySnapshot _injuries;
    private float _bodyYaw;
    public ZombieMobility Mode { get; private set; }
    public bool IsLowBody { get; private set; }
    public bool WantsLowBody => Mode == ZombieMobility.Crawl || Mode == ZombieMobility.Immobilized;
    public bool BodyPending => WantsLowBody != IsLowBody;
    public bool CanDrive => Mode != ZombieMobility.Immobilized && !BodyPending;
    public float AccelerationScale => Mode == ZombieMobility.Walk ? 1 : Mode == ZombieMobility.Limp ? 0.7f : 0.4f;
    public float GaitScale => Mode == ZombieMobility.Walk ? 1 : 0.7f;

    // =========================================================
    public ZombieLocomotion(CharacterBody3D actor, CollisionShape3D body, Node3D visual, Node3D head,
        ZombieLocomotionSettings settings, Node3D leftArm, Node3D rightArm, Node3D leftLeg,
        Node3D rightLeg, Node3D leftKnee, Node3D rightKnee)
    {
        _actor = actor; _body = body; _visual = visual; _head = head; _headRest = head.Rotation;
        _settings = (ZombieLocomotionSettings)settings.Duplicate(true);
        _leftArm = leftArm; _rightArm = rightArm; _leftLeg = leftLeg; _rightLeg = rightLeg;
        _leftKnee = leftKnee; _rightKnee = rightKnee;
        _standing = body.Shape; _standingAt = body.Transform;
        _low = new CapsuleShape3D { Radius = 0.30f, Height = 1.85f, Margin = 0.002f };
        _clearance = new PhysicsShapeQueryParameters3D { CollisionMask = actor.CollisionMask,
            CollideWithBodies = true, CollideWithAreas = false, Margin = 0.002f,
            Exclude = new Godot.Collections.Array<Rid> { actor.GetRid() } };
    }

    // =========================================================
    private bool Usable(BodyRegion region) => (int)_injuries.State(region) < (int)LimbState.Disabled;

    // =========================================================
    // State changes, not physics polling, choose capabilities. Disabled limbs are unusable even when still visible.
    public void Refresh(in AnatomySnapshot injuries)
    {
        _injuries = injuries;
        int legs = (Usable(BodyRegion.LeftLeg) ? 1 : 0) + (Usable(BodyRegion.RightLeg) ? 1 : 0);
        int arms = (Usable(BodyRegion.LeftArm) ? 1 : 0) + (Usable(BodyRegion.RightArm) ? 1 : 0);
        Mode = legs == 0 ? arms == 0 ? ZombieMobility.Immobilized : ZombieMobility.Crawl
            : legs == 1 ? ZombieMobility.Hop
            : injuries.State(BodyRegion.LeftLeg) != LimbState.Functional || injuries.State(BodyRegion.RightLeg) != LimbState.Functional
                ? ZombieMobility.Limp : ZombieMobility.Walk;
    }

    // =========================================================
    public float Speed(float ordinary)
    {
        if (!CanDrive) return 0;
        return Mode switch
        {
            ZombieMobility.Limp => ordinary * Safe(_settings.LimpMultiplier, 0.05f, 1),
            ZombieMobility.Hop => Mathf.Min(ordinary * Safe(_settings.HopMultiplier, 0.05f, 1), Safe(_settings.MaximumHopSpeed, 0.05f, 3)),
            ZombieMobility.Crawl => Mathf.Min(ordinary, Safe(_settings.CrawlSpeed, 0.05f, 2))
                * (Usable(BodyRegion.LeftArm) && Usable(BodyRegion.RightArm) ? 1 : 0.55f),
            _ => ordinary
        };
    }

    // =========================================================
    private Transform3D LowAt(float yaw)
        => new(new Basis(Vector3.Up, yaw) * new Basis(Vector3.Right, Mathf.Pi * 0.5f), new Vector3(0, 0.34f, 0));

    // =========================================================
    private bool Fits(Shape3D shape, Transform3D local)
    {
        _clearance.Shape = shape; _clearance.Transform = _actor.GlobalTransform * local;
        return _actor.GetWorld3D().DirectSpaceState.IntersectShape(_clearance, 1).Count == 0;
    }

    // =========================================================
    // No root teleport. If a longer low capsule cannot fit, hold current body/pose and wait for the existing AI timer.
    public bool TryBody(float yaw)
    {
        if (!BodyPending) return false;
        Transform3D at = WantsLowBody ? LowAt(yaw) : _standingAt;
        Shape3D shape = WantsLowBody ? _low : _standing;
        if (!Fits(shape, at)) return false;
        _body.Shape = shape; _body.Transform = at;
        IsLowBody = WantsLowBody; _bodyYaw = yaw;
        return true;
    }

    // =========================================================
    // Check midpoint and endpoint of a small bounded yaw step before rotating a long body beside walls/actors.
    public float TryTurn(float desired)
    {
        if (!IsLowBody) return desired;
        float delta = Mathf.Clamp(Mathf.Atan2(Mathf.Sin(desired - _bodyYaw), Mathf.Cos(desired - _bodyYaw)), -0.14f, 0.14f);
        if (Mathf.Abs(delta) < 0.0001f) return _bodyYaw;
        float next = _bodyYaw + delta;
        if (Fits(_low, LowAt(_bodyYaw + delta * 0.5f)) && Fits(_low, LowAt(next)))
        { _bodyYaw = next; _body.Transform = LowAt(next); }
        return _bodyYaw;
    }

    // =========================================================
    // Called after the existing species gait. Missing pivots remain intact; no animation work while sleeping.
    public void ApplyPose(float step)
    {
        float yaw = _visual.Rotation.Y;
        _head.Rotation = _headRest;
        if (IsLowBody)
        {
            _visual.Position = new Vector3(0, 0.34f, 0.82f).Rotated(Vector3.Up, yaw);
            _visual.Rotation = new Vector3(-Mathf.Pi * 0.5f, yaw, 0);
            _head.Rotation = new Vector3(1.2f, _headRest.Y, _headRest.Z);
            float gait = Mode == ZombieMobility.Crawl && !BodyPending ? step : 0;
            _leftArm.Rotation = new Vector3(Usable(BodyRegion.LeftArm) ? 2.8f + gait * 0.2f : 0.15f, 0, -0.12f);
            _rightArm.Rotation = new Vector3(Usable(BodyRegion.RightArm) ? 2.8f - gait * 0.2f : 0.15f, 0, 0.12f);
            _leftLeg.Rotation = _rightLeg.Rotation = Vector3.Zero;
            _leftKnee.Rotation = _rightKnee.Rotation = Vector3.Zero;
            return;
        }
        if (Mode == ZombieMobility.Limp)
        {
            float side = _injuries.State(BodyRegion.LeftLeg) != LimbState.Functional ? -1 : 1;
            _visual.Rotation = new Vector3(_visual.Rotation.X, yaw, side * 0.06f);
            if (side < 0) _leftLeg.Rotation *= 0.35f; else _rightLeg.Rotation *= 0.35f;
        }
        else if (Mode == ZombieMobility.Hop)
        {
            _visual.Position += Vector3.Up * (Mathf.Abs(step) * 0.045f);
            _visual.Rotation = new Vector3(_visual.Rotation.X, yaw, Usable(BodyRegion.LeftLeg) ? -0.06f : 0.06f);
            if (!Usable(BodyRegion.LeftLeg)) { _leftLeg.Rotation = new Vector3(-0.35f, 0, 0); _leftKnee.Rotation = new Vector3(-1.0f, 0, 0); }
            if (!Usable(BodyRegion.RightLeg)) { _rightLeg.Rotation = new Vector3(-0.35f, 0, 0); _rightKnee.Rotation = new Vector3(-1.0f, 0, 0); }
        }
        if (!Usable(BodyRegion.LeftArm)) _leftArm.Rotation = new Vector3(0.12f, 0, -0.10f);
        if (!Usable(BodyRegion.RightArm)) _rightArm.Rotation = new Vector3(0.12f, 0, 0.10f);
    }

    // =========================================================
    private static float Safe(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;
}
