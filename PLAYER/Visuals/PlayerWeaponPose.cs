// Weapon aim, recoil and shove grip presentation only; firing/reload/melee remain in Equipment.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerWeaponPose
    {
        private readonly PlayerController _player;
        public bool _weaponPose;
        private Node3D? _shoveWeapon, _rightGrip, _leftGrip;
        private float _shoveGrip, _recoilArmAngle;
        public float _recoilArmApplied;
        // =========================================================
        // Plain collaborator: only the controller owns engine callbacks.
        public PlayerWeaponPose(PlayerController player) => _player = player;

        // =========================================================
        // Equipment selects the holding pose; the actor still owns its socket and joints.
        public void SetWeaponPose(bool enabled)
        {
            _weaponPose = enabled; if (_player._initialized) _player._animation.Pose(0, 0);
        }

        // =========================================================
        // Aim the rig/socket without changing the physical movement root.
        public void SetWeaponAim(Vector3 target)
        {
            Vector3 horizontal = target - _player.GlobalPosition; horizontal.Y = 0;
            if (horizontal.LengthSquared() < 0.01f) return;
            _player._animation._art.Rotation = new Vector3(0, Mathf.Atan2(-horizontal.X, -horizontal.Z), 0);
            Vector3 direction = target - _player.WeaponMount.GlobalPosition;
            if (direction.LengthSquared() > 0.0001f && Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) < 0.99f)
                _player.WeaponMount.GlobalBasis = Basis.LookingAt(direction, Vector3.Up);
        }

        // =========================================================
        // Apply only the recoil delta to avoid accumulating idle joint rotation.
        public void SetWeaponRecoilPose(float normalizedKick)
        {
            if (!_player._initialized) return;
            float amount = float.IsFinite(normalizedKick) ? Mathf.Clamp(normalizedKick, 0f, 1.5f) : 0f;
            _recoilArmAngle = Mathf.DegToRad(1.8f * amount);
            ApplyWeaponRecoilPose();
        }

        // =========================================================
        // Reapply recoil after locomotion has reset joints.
        public void ApplyWeaponRecoilPose()
        {
            float current = _weaponPose && _shoveGrip <= 0f ? _recoilArmAngle : 0f;
            float change = current - _recoilArmApplied;
            if (Mathf.Abs(change) < 0.000001f) return;
            _player._animation._shoulderGirdle.Rotation += new Vector3(change, 0f, 0f);
            _player._animation._leftArm.Rotation += new Vector3(change * 0.5f, 0f, 0f);
            _player._animation._rightArm.Rotation += new Vector3(change * 0.5f, 0f, 0f);
            _recoilArmApplied = current;
        }

        // =========================================================
        // Preserve two-hand shove grip ownership and pre-initialization handling.
        public void SetShoveGrip(Node3D? weapon, float weight)
        {
            if (_shoveWeapon != weapon)
            {
                _rightGrip = weapon?.GetNodeOrNull<Node3D>("GripRight");
                _leftGrip = weapon?.GetNodeOrNull<Node3D>("GripLeft");
            }
            _shoveWeapon = weapon; _shoveGrip = Mathf.Clamp(weight, 0, 1);
            if (!_player._initialized) return;
            _player._animation._leftArm.RotationDegrees = _player._settings.LeftWeaponArmDegrees;
            _player._animation._rightArm.RotationDegrees = _player._settings.RightWeaponArmDegrees;
            _player._animation._leftElbow.Rotation = _player._animation._rightElbow.Rotation = Vector3.Zero;
            ApplyShoveGrips();
        }

        // =========================================================
        // Solve only active, valid weapon grip targets.
        public void ApplyShoveGrips()
        {
            if (_shoveGrip <= 0 || !GodotObject.IsInstanceValid(_shoveWeapon)) return;
            ReachGrip(_player._animation._rightArm, _player._animation._rightElbow, GodotObject.IsInstanceValid(_rightGrip) ? _rightGrip!.GlobalPosition : _shoveWeapon!.ToGlobal(new Vector3(0, -0.10f, 0.02f)), 1);
            ReachGrip(_player._animation._leftArm, _player._animation._leftElbow, GodotObject.IsInstanceValid(_leftGrip) ? _leftGrip!.GlobalPosition : _shoveWeapon!.ToGlobal(new Vector3(0, -0.10f, -0.40f)), -1);
        }

        // =========================================================
        // Use the original two-bone arm geometry and reach limits.
        private void ReachGrip(Node3D arm, Node3D elbow, Vector3 target, float side)
        {
            const float upper = 0.32f, lower = 0.166f;
            Vector3 shoulder = arm.GlobalPosition, delta = target - shoulder;
            float distance = Mathf.Clamp(delta.Length(), 0.16f, upper + lower - 0.001f);
            Vector3 direction = delta.Normalized();
            if (direction.LengthSquared() < 0.001f) return;
            Vector3 pole = _player._animation._art.GlobalBasis * new Vector3(side, -0.4f, 0.3f);
            Vector3 bend = pole - direction * pole.Dot(direction);
            if (bend.LengthSquared() < 0.001f) bend = Vector3.Up.Cross(direction);
            bend = bend.Normalized();
            float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            Vector3 joint = shoulder + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Basis desired = ArmBasis(joint - shoulder, bend);
            arm.GlobalBasis = arm.GlobalBasis.Slerp(desired, _shoveGrip);
            desired = ArmBasis(shoulder + direction * distance - elbow.GlobalPosition, bend);
            elbow.GlobalBasis = elbow.GlobalBasis.Slerp(desired, _shoveGrip);
        }

        // =========================================================
        // Keep the stable sleeve orientation along local minus Y.
        private static Basis ArmBasis(Vector3 direction, Vector3 bend)
        {
            Vector3 y = -direction.Normalized(), x = bend.Cross(y).Normalized();
            if (x.LengthSquared() < 0.001f) x = Vector3.Right;
            return new Basis(x, y, x.Cross(y).Normalized()).Orthonormalized();
        }
    }
}
