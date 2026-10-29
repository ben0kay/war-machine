// Cached procedural rig and event-driven locomotion posing; no animation callback or timer of its own.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerAnimation
    {
        private readonly PlayerController _player;
        public Node3D _art = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
        public Node3D _leftFoot = null!, _rightFoot = null!, _leftKnee = null!, _rightKnee = null!, _upperBody = null!;
        public Node3D _shoulderGirdle = null!, _leftElbow = null!, _rightElbow = null!;
        public float _phase;
        // =========================================================
        // Plain collaborator: only the controller owns engine callbacks.
        public PlayerAnimation(PlayerController player) => _player = player;

        // =========================================================
        // Generate the existing mesh once and cache the same rig/socket references.
        public void Initialize()
        {
            var mesh = new PlayerMeshBuilder(_player, _player._settings);
            _art = mesh.Art; _upperBody = mesh.UpperBody; _shoulderGirdle = mesh.ShoulderGirdle;
            _leftArm = mesh.LeftArm; _rightArm = mesh.RightArm;
            _leftElbow = mesh.LeftElbow; _rightElbow = mesh.RightElbow;
            _leftLeg = mesh.LeftLeg; _rightLeg = mesh.RightLeg;
            _leftKnee = mesh.LeftKnee; _rightKnee = mesh.RightKnee;
            _leftFoot = mesh.LeftFoot; _rightFoot = mesh.RightFoot; _player.WeaponMount = mesh.WeaponMount;
        }

        // =========================================================
        // Preserve facing, stride clock and walk/crawl gait from actual collision-resolved travel.
        public void Advance(Vector3 travel, float dt, float actualSpeed)
        {
            if (actualSpeed > 0.05f)
            {
                float yaw = Mathf.Atan2(-travel.X, -travel.Z);
                if (!_player._weapon._weaponPose) _art.Rotation = new Vector3(0, Mathf.LerpAngle(_art.Rotation.Y, yaw, 1 - Mathf.Exp(-14 * dt)), 0);
                float gaitReference = _player.IsProne ? 2f : Mathf.Max(0.1f, _player._settings.WalkSpeed);
                _phase += dt * Mathf.Tau * Mathf.Max(0.1f, _player._settings.GaitRate) * actualSpeed / gaitReference;
                // Crawl uses a short, alternating elbow/leg movement instead of a walking stride.
                float liftPhase = Mathf.Cos(_phase);
                float gaitStrength = Mathf.Clamp(actualSpeed / gaitReference, 0f, 1f);
                Pose(Mathf.Sin(_phase) * (_player.IsProne ? 0.13f : 0.5f),
                    Mathf.Abs(Mathf.Sin(_phase * 2)) * (_player.IsProne ? 0.008f : 0.025f),
                    Mathf.Max(0, liftPhase) * gaitStrength, Mathf.Max(0, -liftPhase) * gaitStrength, gaitStrength);
            }
            else Pose(0, 0);
        }

        // =========================================================
        // Apply the existing rig pose, then shove grips and recoil in their original order.
        public void Pose(float angle, float bob, float leftLift = 0, float rightLift = 0, float gaitStrength = 0)
        {
            float stride = angle * Mathf.Lerp(1, 0.4f, _player._stance._crouch) * (1f - 0.4f * _player._stance._prone);
            float motion = gaitStrength * Mathf.Lerp(1f, 0.35f, _player._stance._crouch) * (1f - 0.4f * _player._stance._prone);
            float aimDamping = _player._weapon._weaponPose ? 0.25f : 1f;
            float swing = Mathf.Sin(_phase), across = Mathf.Cos(_phase);
            float torsoTwist = Mathf.DegToRad(Mathf.Clamp(_player._settings.TorsoTwistDegrees, 0, 8)) * motion * aimDamping;
            float torsoRoll = Mathf.DegToRad(Mathf.Clamp(_player._settings.TorsoRollDegrees, 0, 5)) * motion * aimDamping;
            float shoulderSway = Mathf.DegToRad(Mathf.Clamp(_player._settings.ShoulderSwayDegrees, 0, 6)) * motion;
            _art.Position = Vector3.Up * (bob * Mathf.Lerp(1, 0.35f, _player._stance._crouch)
                - 0.72f * (1 - Mathf.Cos(1.15f * _player._stance._crouch)) - 0.30f * _player._stance._prone);
            // Lean upper body forward to the ground: the head/arms lead the crawl along local -Z.
            // Cosmetic torso roll and shift do not rotate the physics body or detection volumes.
            float lean = _player._lean.Amount;
            _upperBody.Position = Vector3.Up * 0.85f + Vector3.Right * (lean * Mathf.Clamp(_player._settings.LeanTorsoShift, 0f, 0.15f));
            _upperBody.Rotation = new Vector3(0.18f * _player._stance._crouch - 1.26f * _player._stance._prone,
                -swing * torsoTwist, across * torsoRoll - Mathf.DegToRad(Mathf.Clamp(_player._settings.LeanBodyDegrees, 0f, 20f)) * lean);
            _shoulderGirdle.Rotation = new Vector3(0, -swing * shoulderSway * 0.3f * aimDamping,
                Mathf.Sin(_phase * 2f) * shoulderSway * 0.5f * (_player._weapon._weaponPose ? 0.45f : 1f));
            _leftArm.Rotation = new Vector3(-stride - 0.4f * _player._stance._crouch + 1.8f * _player._stance._prone, 0, 0);
            _rightArm.Rotation = new Vector3(stride - 0.4f * _player._stance._crouch + 1.8f * _player._stance._prone, 0, 0);
            if (_player._weapon._weaponPose)
            {
                // Keep hands and equipped rifle forward when the parent torso lies horizontal.
                _leftArm.RotationDegrees = _player._settings.LeftWeaponArmDegrees + new Vector3(55f * _player._stance._prone, 0, 0);
                _rightArm.RotationDegrees = _player._settings.RightWeaponArmDegrees + new Vector3(55f * _player._stance._prone, 0, 0);
            }
            // Slight forearm lag follows the opposite leg; keep near zero when hands support the rifle.
            float elbowFlex = Mathf.DegToRad(Mathf.Clamp(_player._settings.ElbowFlexDegrees, 0, 35)) * motion;
            float armFreedom = _player._weapon._weaponPose ? 0.07f : 1f;
            _leftElbow.Rotation = new Vector3(elbowFlex * (0.55f + 0.45f * Mathf.Max(0, -swing)) * armFreedom, 0, 0);
            _rightElbow.Rotation = new Vector3(elbowFlex * (0.55f + 0.45f * Mathf.Max(0, swing)) * armFreedom, 0, 0);
            _leftLeg.Rotation = new Vector3(stride + 1.15f * _player._stance._crouch - 1.08f * _player._stance._prone, 0, 0);
            _rightLeg.Rotation = new Vector3(-stride + 1.15f * _player._stance._crouch - 1.08f * _player._stance._prone, 0, 0);
            // Crouch retains its existing deep knee bend; walking adds up to ~35 degrees on the airborne leg.
            float kneeBend = Mathf.DegToRad(Mathf.Clamp(_player._settings.KneeBendDegrees, 0, 60)) * (1f - _player._stance._crouch) * (1f - _player._stance._prone);
            _leftKnee.Rotation = new Vector3(-2.3f * _player._stance._crouch + 0.70f * _player._stance._prone - kneeBend * leftLift, 0, 0);
            _rightKnee.Rotation = new Vector3(-2.3f * _player._stance._crouch + 0.70f * _player._stance._prone - kneeBend * rightLift, 0, 0);
            // Small toe/heel movement while swinging; feet return to their original planted pose at rest.
            float footRoll = Mathf.DegToRad(Mathf.Clamp(_player._settings.FootRollDegrees, 0, 20)) * (1f - _player._stance._crouch) * Mathf.Sin(_phase);
            _leftFoot.Rotation = new Vector3(-_leftLeg.Rotation.X - _leftKnee.Rotation.X + footRoll * leftLift, 0, 0);
            _rightFoot.Rotation = new Vector3(-_rightLeg.Rotation.X - _rightKnee.Rotation.X - footRoll * rightLift, 0, 0);
            _player._weapon.ResetBatBasePose();
            _player._weapon.ApplyShoveGrips();
            _player._weapon._recoilArmApplied = 0f; // Locomotion pose rewrote joint rotations above.
            _player._weapon.ApplyWeaponRecoilPose();
        }
    }
}
