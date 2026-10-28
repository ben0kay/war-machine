// Standing/crouch/prone state, capsule transitions and clearance; driven once by the shared movement step.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerStance
    {
        private readonly PlayerController _player;
        public CollisionShape3D _collider = null!;
        public CapsuleShape3D _capsule = null!;
        private PhysicsShapeQueryParameters3D _standingQuery = null!, _crouchingQuery = null!;
        public float _crouch, _prone;
        public MovementStance Current = MovementStance.Standing;
        private MovementStance _stanceBeforeProne = MovementStance.Standing;
        // =========================================================
        // Plain collaborator: only the controller owns engine callbacks.
        public PlayerStance(PlayerController player) => _player = player;

        // =========================================================
        // Cache original capsule and one-time standing/crouching clearance queries.
        public void Initialize()
        {
            _collider = _player.GetNode<CollisionShape3D>("CollisionShape3D");
            _capsule = (CapsuleShape3D)_collider.Shape.Duplicate(); _collider.Shape = _capsule;
            var standing = (CapsuleShape3D)_capsule.Duplicate(); standing.Height = 1.8f;
            var crouching = (CapsuleShape3D)_capsule.Duplicate(); crouching.Height = 1.4f;
            var exclusion = new Godot.Collections.Array<Rid> { _player.GetRid() };
            _standingQuery = new PhysicsShapeQueryParameters3D { Shape = standing, CollisionMask = _player.CollisionMask,
                Exclude = exclusion };
            _crouchingQuery = new PhysicsShapeQueryParameters3D { Shape = crouching, CollisionMask = _player.CollisionMask,
                Exclude = exclusion };
        }

        // =========================================================
        // Restore the same initial standing state without emitting stance-change events.
        public void Reset()
        {
            Current = MovementStance.Standing; _stanceBeforeProne = MovementStance.Standing;
            _crouch = _prone = 0f; _capsule.Height = 1.8f; _collider.Position = Vector3.Up * 0.9f;
        }

        // =========================================================
        // Consume stance requests, check headroom once and interpolate the existing capsule.
        public bool Advance(float dt)
        {
            if (_player._input._toggleProne)
            {
                _player._input._toggleProne = false;
                if (_player.IsProne)
                {
                    // Return to the original stance if there is headroom; in low spaces try crouch.
                    if (!TrySetStance(_stanceBeforeProne) && _stanceBeforeProne == MovementStance.Standing)
                        TrySetStance(MovementStance.Crouching);
                }
                else if (_player.IsOnFloor())
                {
                    _stanceBeforeProne = Current;
                    TrySetStance(MovementStance.Prone);
                }
            }
            if (_player._input._toggleCrouch)
            {
                _player._input._toggleCrouch = false;
                if (_player.IsProne) TrySetStance(MovementStance.Crouching);
                else TrySetStance(_player.IsCrouching ? MovementStance.Standing : MovementStance.Crouching);
            }
            float previousCrouch = _crouch, previousProne = _prone;
            _crouch = Mathf.MoveToward(_crouch, _player.IsCrouching ? 1f : 0f, dt * 6f);
            _prone = Mathf.MoveToward(_prone, _player.IsProne ? 1f : 0f, dt * 5f);
            bool stanceAnimating = _crouch != previousCrouch || _prone != previousProne;
            if (stanceAnimating)
            {
                // A short upright capsule is an initial prone collision proxy; detailed lying
                // hull/hurtboxes belong to a later physical-pose pass.
                _capsule.Height = Mathf.Max(0.65f, 1.8f - 0.4f * _crouch
                    - (1.8f - Mathf.Clamp(_player._settings.ProneCollisionHeight, 0.65f, 1.15f)) * _prone);
                _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
            }
            return stanceAnimating;
        }

        // =========================================================
        // Growing taller requires clearance; publish the original crouch and full-stance events.
        private bool TrySetStance(MovementStance next)
        {
            if (next == Current) return true;
            if (next == MovementStance.Standing && !HasStanceClearance(_standingQuery, 1.8f)) return false;
            if (next == MovementStance.Crouching && _player.IsProne && !HasStanceClearance(_crouchingQuery, 1.4f)) return false;
            Current = next;
            _player.RaiseStanceChanged(next);
            return true;
        }

        // =========================================================
        // Use the cached destination capsule query, never an idle polling loop.
        private bool HasStanceClearance(PhysicsShapeQueryParameters3D query, float height)
        {
            query.Transform = _player.GlobalTransform * new Transform3D(Basis.Identity,
                Vector3.Up * (height * 0.5f + 0.03f));
            return _player.GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
        }
    }
}
