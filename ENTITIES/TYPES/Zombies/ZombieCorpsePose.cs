// Standard/runner biped adapter: cache existing joints and their death-time rotations, then relax asymmetrically. No mesh/material changes.
using Godot;

public sealed class ZombieCorpsePose : ICorpsePoseAdapter
{
    private readonly Node3D[] _joints;
    private readonly Vector3[] _from, _to;

    // =========================================================
    public ZombieCorpsePose(uint seed, params Node3D[] joints)
    {
        _joints = joints; _from = new Vector3[joints.Length]; _to = new Vector3[joints.Length];
        var random = new RandomNumberGenerator { Seed = seed ^ 0x9e3779b9u };
        for (int i = 0; i < joints.Length; i++)
        {
            _from[i] = joints[i].Rotation;
            bool knee = i >= 4 && i <= 5;
            _to[i] = new Vector3(knee ? random.RandfRange(-0.65f, -0.08f) : random.RandfRange(-0.35f, 0.45f),
                random.RandfRange(-0.15f, 0.15f), random.RandfRange(-0.3f, 0.3f));
        }
    }

    // =========================================================
    public void Apply(float progress)
    {
        float weight = Mathf.SmoothStep(0, 1, progress);
        for (int i = 0; i < _joints.Length; i++)
            if (GodotObject.IsInstanceValid(_joints[i])) _joints[i].Rotation = _from[i].Lerp(_to[i], weight);
    }
}
