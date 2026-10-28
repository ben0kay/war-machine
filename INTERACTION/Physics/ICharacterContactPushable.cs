// Minimal reuse boundary for contact-driven hinged objects, independent of IX0 pickup/interact input.
// Implemented by a door root; the player's existing slide contact supplies one candidate per physics step.
using Godot;

public interface ICharacterContactPushable
{
    // =========================================================
    // Apply a small physical push only when a moving player contacts this object's rigid leaf.
    // True means this contact was handled; prevents duplicate impulses from solver contacts.
    bool TryPushFromCharacter(Vector3 movementDirection, Vector3 contactPoint, float speed, float delta);
}
