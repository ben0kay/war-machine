// CM2-D: bounded two-handed cross-gun shove, advanced by the existing melee adapter only.
// The real gun turns broadside; gameplay aim/contact stays independent of this cosmetic transform.
using Godot;

public sealed class PlayerShovePose
{
    private readonly PlayerController _player;
    private Node3D? _weapon, _hands;
    private Transform3D _rest;
    private bool _firstPerson;

    // =========================================================
    public PlayerShovePose(PlayerController player) { _player = player; }

    // =========================================================
    // Capture the selected weapon's authored view fit; cancel always restores it exactly.
    public void Start(Node3D weapon, bool firstPerson)
    {
        Cancel(); _weapon = weapon; _rest = weapon.Transform; _firstPerson = firstPerson;
        if (firstPerson && weapon is not MeleeWeaponController)
        {
            _hands = weapon.GetNodeOrNull<Node3D>("ShoveHands");
            if (_hands == null) _hands = ShoveHandsBuilder.Build(weapon);
            _hands.Show();
        }
    }

    // =========================================================
    // Turn across the chest during windup, extend at contact, then return smoothly to aim.
    public void Apply(float elapsed, MeleeSettings settings)
    {
        if (!GodotObject.IsInstanceValid(_weapon)) { Cancel(); return; }
        float windup = Safe(settings.WindupSeconds, 0, 1);
        float active = Safe(settings.ActiveSeconds, 0.01f, 1);
        float recovery = Safe(settings.RecoverySeconds, 0, 1);
        float turn = elapsed < windup ? Smooth(elapsed / Mathf.Max(windup, 0.001f))
            : elapsed < windup + active ? 1 : 1 - Smooth((elapsed - windup - active) / Mathf.Max(recovery, 0.001f));
        float push = elapsed < windup ? 0 : elapsed < windup + active
            ? Smooth((elapsed - windup) / Mathf.Max(active * 0.45f, 0.001f))
            : 1 - Smooth((elapsed - windup - active) / Mathf.Max(recovery, 0.001f));
        Basis rotation = new(Vector3.Up, Mathf.DegToRad(Safe(settings.ShoveSidewaysDegrees, 60, 100)) * turn);
        // Rotate about the receiver/handguard midpoint rather than swinging the muzzle as a club.
        Vector3 pivot = new(0, 0, -0.25f);
        Vector3 centre = _firstPerson ? new Vector3(-0.17f, 0.08f, 0.06f) : new Vector3(-0.24f, 0, 0.35f);
        Vector3 offset = centre * turn + Vector3.Forward * Safe(settings.ShoveExtension, 0, 0.25f) * push;
        _weapon!.Transform = _rest * new Transform3D(rotation, pivot - rotation * pivot + offset);
        if (!_firstPerson) _player.SetShoveGrip(_weapon, turn);
    }

    // =========================================================
    // No timer/process of its own; restore on completion, focus/menu interruption, switch or unload.
    public void Cancel()
    {
        if (GodotObject.IsInstanceValid(_weapon)) _weapon!.Transform = _rest;
        if (GodotObject.IsInstanceValid(_hands)) _hands!.Hide();
        if (!_firstPerson && GodotObject.IsInstanceValid(_player)) _player.SetShoveGrip(null, 0);
        _weapon = null; _hands = null;
    }

    // =========================================================
    private static float Smooth(float value)
    { float t = Mathf.Clamp(value, 0, 1); return t * t * (3 - 2 * t); }

    // =========================================================
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }
}
