// One explicit gait driver for four two-bone legs. No nodes, callbacks, terrain polling or per-frame allocation.
using Godot;

public sealed class MechanicalLegs
{
    private readonly Node3D _visual, _body;
    private readonly Node3D[] _hips = new Node3D[4], _knees = new Node3D[4], _feet = new Node3D[4];
    private readonly Vector3[] _hipOffsets = new Vector3[4], _anchors = new Vector3[4];
    private readonly Vector3[] _planted = new Vector3[4], _from = new Vector3[4], _to = new Vector3[4];
    private readonly bool[] _swinging = new bool[4];
    private readonly float _height, _upper, _lower;
    private float _phase, _settle;
    public float StrideLength { get; set; }
    public float CyclesPerSecond { get; set; }
    public float Lift { get; set; }
    public float StanceFraction { get; set; }
    public float Bob { get; set; }
    public float PitchDegrees { get; set; }
    public float SettleSeconds { get; set; }
    public event System.Action? FootPlanted;
    public bool IsSettling => _settle > 0;
    // Optional world-space foot placement correction, called only when selecting a target.
    public System.Func<Vector3, Vector3>? ResolveFootTarget { get; set; }

    // =========================================================
    // Explicit rig references keep this reusable with other art and avoid any scene search in Tick.
    public MechanicalLegs(Node3D visual, Node3D body, Node3D[] hips, Vector3[] hipOffsets,
        float height, float upper, float lower, float stanceWidth)
    {
        _visual = visual; _body = body; _height = height; _upper = upper; _lower = lower;
        for (int i = 0; i < 4; i++)
        {
            _hips[i] = hips[i]; _knees[i] = hips[i].GetNode<Node3D>("Knee");
            _feet[i] = _knees[i].GetNode<Node3D>("Foot"); _hipOffsets[i] = hipOffsets[i];
            _anchors[i] = new Vector3((i % 2 == 0 ? -1 : 1) * stanceWidth, 0, hipOffsets[i].Z);
        }
        ResetPose();
    }

    // =========================================================
    // Place a neutral stance once, e.g. on spawn or after a deliberate visual reposition.
    public void ResetPose()
    {
        _phase = 0; _settle = 0; _body.Position = Vector3.Up * _height; _body.Rotation = Vector3.Zero;
        for (int i = 0; i < 4; i++)
        {
            _swinging[i] = false;
            _planted[i] = Target(_visual.ToGlobal(_anchors[i]));
            _from[i] = _to[i] = _planted[i];
            Solve(i, _planted[i]);
        }
    }

    // =========================================================
    // Finish lifted feet over a short bounded interval, then the owner disables its callback.
    public void BeginSettle() => _settle = Mathf.Max(0.01f, SettleSeconds);

    // =========================================================
    // Advance diagonal pairs. Planted targets stay fixed in world space as the physics body moves.
    public void Tick(float delta, float speed, bool preview)
    {
        bool settling = _settle > 0;
        if (settling) _settle = Mathf.Max(0, _settle - delta);
        else _phase = Mathf.PosMod(_phase + delta * Mathf.Max(0.01f, CyclesPerSecond) * speed, 1);
        float weight = settling ? _settle / Mathf.Max(0.01f, SettleSeconds) : 1;
        _body.Position = Vector3.Up * (_height + Mathf.Sin(_phase * Mathf.Tau * 2) * Bob * weight);
        _body.Rotation = new Vector3(Mathf.DegToRad(PitchDegrees) * Mathf.Sin(_phase * Mathf.Tau) * weight, 0, 0);
        bool planted = false;
        float stance = Mathf.Clamp(StanceFraction, 0.5f, 0.9f);
        for (int i = 0; i < 4; i++)
        {
            float phase = Mathf.PosMod(_phase + (i == 0 || i == 3 ? 0 : 0.5f), 1);
            bool swing = !settling && phase >= stance;
            if (swing && !_swinging[i])
            {
                _from[i] = _planted[i];
                _to[i] = Target(_visual.ToGlobal(_anchors[i] + Vector3.Forward * StrideLength * 0.5f));
            }
            if (settling)
            {
                _planted[i] = _planted[i].Lerp(_to[i], Mathf.Min(1, delta / Mathf.Max(delta, _settle)));
            }
            else if (swing)
            {
                float t = (phase - stance) / (1 - stance);
                float smooth = t * t * (3 - 2 * t);
                _planted[i] = _from[i].Lerp(_to[i], smooth) + _visual.GlobalBasis.Y * (Mathf.Sin(t * Mathf.Pi) * Lift);
            }
            else
            {
                if (_swinging[i]) { _planted[i] = _to[i]; planted = true; }
                // Preview uses a treadmill stance; real locomotion leaves feet planted in world space.
                if (preview) _planted[i] = _visual.ToGlobal(_anchors[i] + Vector3.Forward * StrideLength * (0.5f - phase / stance));
            }
            if (!swing && !settling) _to[i] = _planted[i];
            _swinging[i] = swing;
            Solve(i, _planted[i]);
        }
        if (planted) FootPlanted?.Invoke();
    }

    // =========================================================
    // Optional ground/height provider is injected by integration, never searched for here.
    private Vector3 Target(Vector3 position) => ResolveFootTarget?.Invoke(position) ?? position;

    // =========================================================
    // Solve in art-local units; knee bends outward, while feet retain a level pad orientation.
    private void Solve(int i, Vector3 worldFoot)
    {
        Vector3 hip = _visual.ToLocal(_body.ToGlobal(_hipOffsets[i]));
        Vector3 foot = _visual.ToLocal(worldFoot);
        Vector3 direction = foot - hip;
        float distance = Mathf.Clamp(direction.Length(), Mathf.Abs(_upper - _lower) + 0.01f, _upper + _lower - 0.01f);
        direction = direction.Normalized();
        Vector3 outward = new(i % 2 == 0 ? -1 : 1, 0, 0);
        Vector3 bend = outward - direction * outward.Dot(direction);
        if (bend.LengthSquared() < 0.001f) bend = Vector3.Back - direction * Vector3.Back.Dot(direction);
        float along = (_upper * _upper - _lower * _lower + distance * distance) / (2 * distance);
        Vector3 knee = hip + direction * along + bend.Normalized() * Mathf.Sqrt(Mathf.Max(0, _upper * _upper - along * along));
        // Clamp unreachable targets to preserve segment lengths rather than stretching the rig.
        foot = hip + direction * distance;
        Aim(_hips[i], _visual.ToGlobal(hip), _visual.ToGlobal(knee));
        Aim(_knees[i], _visual.ToGlobal(knee), _visual.ToGlobal(foot));
        _feet[i].GlobalTransform = new Transform3D(_visual.GlobalBasis, _visual.ToGlobal(foot));
    }

    // =========================================================
    // Bones point along local -Z; avoid the vertical LookAt singularity.
    private void Aim(Node3D bone, Vector3 origin, Vector3 end)
    {
        Vector3 direction = (end - origin).Normalized();
        Vector3 up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.98f ? Vector3.Back : Vector3.Up;
        float scale = _visual.GlobalBasis.X.Length();
        bone.GlobalTransform = new Transform3D(Basis.LookingAt(direction, up).Scaled(Vector3.One * scale), origin);
    }
}
