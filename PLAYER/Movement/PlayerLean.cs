// Shared held-key lean state: smoothed cosmetic pose and speed restriction, no extra Node or callback.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerLean
    {
        private readonly PlayerController _player;
        private bool _leftHeld, _rightHeld;
        public float Amount { get; private set; } // -1 left, +1 right.
        public bool IsTransitioning { get; private set; }
        public bool IsActive => Target != 0f;

        // =========================================================
        // Keep the independent key states; holding both returns to neutral.
        public PlayerLean(PlayerController player) => _player = player;

        // =========================================================
        public void SetHeld(Key key, bool held)
        {
            if (key == Key.Kp1) _leftHeld = held;
            else if (key == Key.Kp2) _rightHeld = held;
            else return;
            _player.SetPhysicsProcess(true);
        }

        // =========================================================
        public void ClearHeld() { _leftHeld = _rightHeld = false; }

        // =========================================================
        public void Reset() { ClearHeld(); Amount = 0f; IsTransitioning = false; }

        // =========================================================
        // Blends only in the existing player physics callback, which can sleep at rest.
        public bool Advance(float dt)
        {
            float target = Target, before = Amount;
            float duration = Mathf.Clamp(_player._settings.LeanTransitionSeconds, 0.05f, 0.5f);
            Amount = Mathf.MoveToward(Amount, target, Mathf.Max(0f, dt) / duration);
            IsTransitioning = Amount != target;
            return before != Amount;
        }

        // =========================================================
        // No physical capsule, zombie sight or anatomy changes in this first cosmetic pass.
        private float Target => !_player.GameplayInputBlocked && !_player.IsDead && _player.GetWindow().HasFocus()
            && !_player.IsProne && _player.CanAcceptLeanInput() && _leftHeld != _rightHeld
                ? (_leftHeld ? -1f : 1f) : 0f;
    }
}
