// Reuse existing CharacterBody3D slide contacts; one eligible hinge impulse per moving physics step, no new queries or callbacks.
using Godot;

public static class CharacterContactPush
{
    // =========================================================
    // Shared by the player and the isolated physics regression; retain the actual contact point and into-surface gate.
    public static bool TryApply(CharacterBody3D mover, Vector3 horizontal, float delta)
    {
        Vector3 direction = horizontal.Normalized();
        int contacts = mover.GetSlideCollisionCount();
        for (int i = 0; i < contacts; i++)
        {
            var hit = mover.GetSlideCollision(i);
            if (hit.GetCollider() is not RigidBody3D body
                || body.GetParent() is not ICharacterContactPushable pushable) continue;
            if (direction.Dot(-hit.GetNormal()) < 0.35f) continue;
            if (pushable.TryPushFromCharacter(direction, hit.GetPosition(), horizontal.Length(), delta)) return true;
        }
        return false;
    }
}
