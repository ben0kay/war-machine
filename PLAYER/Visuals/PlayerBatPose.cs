// Top-down one-handed baseball-bat pose. Plain reusable player-rig collaborator, no independent processing.
// Equipment advances its already-authoritative swing clock; this helper makes the visible arm follow the grip.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerBatPose
    {
        private readonly PlayerController _player;
        private Node3D? _bat, _rightGrip;
        private float _swing, _torsoYaw, _torsoRoll, _shoulderYaw;

        // =========================================================
        public PlayerBatPose(PlayerController player) => _player = player;

        // =========================================================
        // Select/unselect updates the idle arm immediately; no per-frame poll while standing still.
        public void Equip(Node3D? bat)
        {
            RemoveAdditive();
            _bat = bat;
            _rightGrip = GodotObject.IsInstanceValid(bat) ? bat!.GetNodeOrNull<Node3D>("GripRight") : null;
            _swing = 0f;
            if (_player._initialized) _player._animation.Pose(0, 0);
        }

        // =========================================================
        // -1 is a right-arm windup, +1 a crossed-body follow-through, 0 the normal hold.
        public void SetSwing(float amount)
        {
            _swing = float.IsFinite(amount) ? Mathf.Clamp(amount, -1f, 1f) : 0f;
            Apply();
        }

        // =========================================================
        // Base locomotion overwrites torso and shoulders, so discard the old additive tracking.
        public void BasePoseRebuilt()
        { _torsoYaw = _torsoRoll = _shoulderYaw = 0f; }

        // =========================================================
        public void Suspend() => RemoveAdditive();

        // =========================================================
        // Player's existing upper/lower arm solver connects the glove to the bat's real GripRight.
        // The free left arm counterbalances; leg and gait poses remain owned by PlayerAnimation.
        public void Apply()
        {
            RemoveAdditive();
            if (!_player._initialized || !GodotObject.IsInstanceValid(_bat) || !GodotObject.IsInstanceValid(_rightGrip)
                || _player._weapon.IsShovePoseActive) return;
            float swing = _swing;
            var rig = _player._animation;
            _torsoYaw = Mathf.DegToRad(16f * swing);
            _torsoRoll = Mathf.DegToRad(-4f * swing);
            _shoulderYaw = Mathf.DegToRad(9f * swing);
            rig._upperBody.Rotation += new Vector3(0f, _torsoYaw, _torsoRoll);
            rig._shoulderGirdle.Rotation += new Vector3(0f, _shoulderYaw, 0f);
            // Not a rifle's two-hand grip: the left arm stays free for balance.
            rig._leftArm.RotationDegrees = new Vector3(22f + 9f * swing, -4f, -12f - 10f * swing);
            rig._leftElbow.Rotation = new Vector3(-0.18f, 0f, 0f);
            _player._weapon.ReachBatGrip(_rightGrip!);
        }

        // =========================================================
        // Remove only the offsets we introduced; restore no unrelated recoil or gait rotation.
        private void RemoveAdditive()
        {
            if (!_player._initialized) return;
            var rig = _player._animation;
            if (_torsoYaw != 0f || _torsoRoll != 0f)
                rig._upperBody.Rotation -= new Vector3(0f, _torsoYaw, _torsoRoll);
            if (_shoulderYaw != 0f)
                rig._shoulderGirdle.Rotation -= new Vector3(0f, _shoulderYaw, 0f);
            _torsoYaw = _torsoRoll = _shoulderYaw = 0f;
        }
    }
}
