// Locomotion, camera axes, gravity/jump, door contact and physics sleep; no independent engine callbacks.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerMovement
    {
        private readonly PlayerController _player;
        private Vector3 _screenRight, _screenForward;
        private Camera3D? _movementCamera;
        public bool _reportMovementCollision;
        // =========================================================
        // Plain collaborator: only the controller owns engine callbacks.
        public PlayerMovement(PlayerController player) => _player = player;

        // =========================================================
        // Replace the camera subscription on reinitialization; cache overhead movement axes.
        public void BindCamera(Camera3D camera)
        {
            if (_movementCamera is Camera previous && GodotObject.IsInstanceValid(previous))
                previous.SettingsChanged -= RefreshMovementAxes;
            _movementCamera = camera; RefreshMovementAxes();
            if (camera is Camera view) view.SettingsChanged += RefreshMovementAxes;
        }

        // =========================================================
        // Update screen-relative XZ axes only on camera configuration events.
        private void RefreshMovementAxes()
        {
            if (!GodotObject.IsInstanceValid(_movementCamera)) return;
            _screenRight = _movementCamera!.GlobalBasis.X;
            _screenRight.Y = 0; _screenRight = _screenRight.Normalized();
            _screenForward = -_movementCamera.GlobalBasis.Z;
            _screenForward.Y = 0; _screenForward = _screenForward.Normalized();
        }

        // =========================================================
        // Keep stance, movement, contact pushes, visual updates, notifications and sleep in their original order.
        public void Advance(double delta)
        {
            float dt = (float)delta;
            bool stanceAnimating = _player._stance.Advance(dt);
            // Consume the press once. Upward velocity naturally disables floor snapping; gravity
            // continues while airborne until _player.MoveAndSlide reports landing, then idle physics can sleep.
            bool jumping = _player._input._jumpRequested && _player.IsOnFloor() && !_player.GameplayInputBlocked
                && _player._stance.Current == MovementStance.Standing && _player.GetWindow().HasFocus();
            _player._input._jumpRequested = false;
            Vector2 keys = !_player.GameplayInputBlocked && _player.GetWindow().HasFocus() ? _player.ReadMovementInput().Normalized() : Vector2.Zero;
            Vector3 direction = _player.GetMovementDirection(keys);
            float speed = Mathf.Max(0.1f, _player._settings.WalkSpeed) *
                (_player.IsProne ? Mathf.Clamp(_player._settings.ProneSpeedMultiplier, 0.05f, 0.4f)
                : _player.IsCrouching ? Mathf.Clamp(_player._settings.CrouchSpeedMultiplier, 0.1f, 1f)
                : Input.IsPhysicalKeyPressed(Key.Shift) ? Mathf.Max(1f, _player._settings.RunMultiplier) : 1f);
            Vector3 horizontal = new(_player.Velocity.X, 0, _player.Velocity.Z);
            horizontal = _player.UpdateHorizontalVelocity(horizontal, direction * speed, dt);
            Vector3 before = _player.GlobalPosition;
            float vertical = jumping ? Mathf.Clamp(_player._settings.JumpVelocity, 0f, 12f)
                : _player.IsOnFloor() ? -0.1f : _player.Velocity.Y - Mathf.Max(1, _player._settings.Gravity) * dt;
            _player.Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
            _player.MoveAndSlide();
            // DOOR1: reuse _player.MoveAndSlide contact reports. No additional physics query or timer.
            // A constrained hinged body may not receive enough natural impulse from sliding alone.
            if (horizontal.LengthSquared() > 0.04f) PushContactBody(horizontal, dt);
            if (_reportMovementCollision) ReportMovementCollision();
            Vector3 travel = _player.GlobalPosition - before; travel.Y = 0;
            float actualSpeed = dt > 0 ? travel.Length() / dt : 0;
            _player._animation.Advance(travel, dt, actualSpeed);
            if (stanceAnimating) _player.RaiseWeaponPoseChanged();
            if (before.DistanceSquaredTo(_player.GlobalPosition) > 0.0000001f) _player.RaisePositionChanged();
            if (_player.IsOnFloor() && keys == Vector2.Zero && horizontal.LengthSquared() < 0.0001f && _player._stance._crouch == (_player.IsCrouching ? 1f : 0f) && _player._stance._prone == (_player.IsProne ? 1f : 0f))
            { _player.Velocity = Vector3.Zero; _player._animation._phase = 0; _player._animation.Pose(0, 0); _player.SetPhysicsProcess(false); }
        }

        // =========================================================
        // Preserve the existing single slide-contact door push without extra queries.
        private void PushContactBody(Vector3 horizontal, float delta)
            => CharacterContactPush.TryApply(_player, horizontal, delta);

        // =========================================================
        // F10 reports existing slide contacts once.
        private void ReportMovementCollision()
        {
            _reportMovementCollision = false;
            int count = _player.GetSlideCollisionCount();
            GD.Print($"Movement probe: player={_player.GlobalPosition}, floor={_player.IsOnFloor()}, contacts={count}. Hold movement against the blocker while pressing F10.");
            for (int i = 0; i < count; i++)
            {
                var hit = _player.GetSlideCollision(i);
                string path = hit.GetCollider() is Node node ? node.GetPath().ToString() : "native/non-node collider";
                GD.Print($"Movement blocker: {path}; normal={hit.GetNormal()}; point={hit.GetPosition()}");
                if (hit.GetCollider() is RigidBody3D body && body.GetParent() is ICharacterContactPushable pushable)
                    GD.Print(pushable.GetContactPushDiagnostics());
            }
        }

        // =========================================================
        public Vector3 Direction(Vector2 keys) => (_screenRight * keys.X + _screenForward * keys.Y).Normalized();

        // =========================================================
        public Vector3 HorizontalVelocity(Vector3 current, Vector3 target, float dt)
            => current.MoveToward(target, Mathf.Max(1, _player._settings.Acceleration) * dt);

        // =========================================================
        // Detach the only external movement subscription when the player exits.
        public void Shutdown()
        {
            if (_movementCamera is Camera view && GodotObject.IsInstanceValid(view)) view.SettingsChanged -= RefreshMovementAxes;
            _movementCamera = null;
        }
    }
}
