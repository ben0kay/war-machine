// One bounded collider-to-actor walk; existing damage first, optional impact second. No extra physics queries.
using Godot;

public static class HitTargetResolver
{
    // =========================================================
    // Null/nonpositive damage is omitted. Stop at the first eligible actor; scenery never causes another ray.
    public static bool Dispatch(Node? collider, Node3D? source, DamagePacket? damage, CombatImpact? impact)
    {
        if (GodotObject.IsInstanceValid(source) && GodotObject.IsInstanceValid(collider)
            && (source == collider || source!.IsAncestorOf(collider!))) return false;
        int depth = 0;
        for (Node? node = collider; node != null && depth < 32; node = node.GetParent(), depth++)
        {
            if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion()) return false;
            if (node == source) return false;
            if (node is not IDamageReceiver && node is not IImpactReceiver) continue;
            if (damage.HasValue && damage.Value.Amount > 0 && node is IDamageReceiver receiver)
                receiver.ReceiveHit(damage.Value);
            // A lethal hit may synchronously free/queue the actor. Never send its subsequent reaction.
            if (impact.HasValue && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion() && node is IImpactReceiver target)
                target.ReceiveImpact(impact.Value);
            return true;
        }
        return false;
    }
}
