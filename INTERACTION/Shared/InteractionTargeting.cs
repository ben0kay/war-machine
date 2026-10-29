// IX0 bounded camera selection, followed by mandatory player reach/solid cover. No visual-fade bypass on use.
using Godot;

public sealed class InteractionTargeting
{
    private readonly PhysicsRayQueryParameters3D _cameraRay, _reachRay;
    private readonly PlayerController _player;
    private readonly Camera3D _camera;
    private readonly IInteractionView _view;
    private readonly float _reach;

    // =========================================================
    public InteractionTargeting(PlayerController player, Camera3D camera, IInteractionView view, InteractionSettings settings)
    {
        _player = player; _camera = camera; _view = view;
        _reach = float.IsFinite(settings.ReachMeters) ? Mathf.Clamp(settings.ReachMeters, 0.5f, 4) : 2.5f;
        var exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
        _cameraRay = new PhysicsRayQueryParameters3D { CollisionMask = settings.QueryMask | 17u, Exclude = exclude,
            CollideWithAreas = true, CollideWithBodies = true, HitFromInside = false };
        _reachRay = new PhysicsRayQueryParameters3D { CollisionMask = settings.QueryMask | 17u, Exclude = exclude,
            CollideWithAreas = true, CollideWithBodies = true, HitFromInside = true };
    }

    // =========================================================
    // A click uses the fresh query in this physics callback, never a stale prompt's cached target.
    public bool TrySelect(out IInteractable? target, out InteractionContext context, out InteractionAction action)
    {
        target = null; context = default; action = default;
        Vector2 point = _view.AimPosition(_player.GetViewport());
        _cameraRay.From = _camera.ProjectRayOrigin(point);
        float distance = Mathf.Min(Mathf.Clamp(_camera.Far, 8, 2048),
            _cameraRay.From.DistanceTo(_player.PerceptionTorso) + _reach + 0.05f);
        _cameraRay.To = _cameraRay.From + _camera.ProjectRayNormal(point) * distance;
        var space = _player.GetWorld3D().DirectSpaceState;
        Godot.Collections.Dictionary hit;
        if (_view.UseFadedCameraSelection)
        {
            if (!FadeAwareAim.TryHitResult(space, _cameraRay, out hit, 4)) return false;
        }
        else hit = space.IntersectRay(_cameraRay);
        if (hit.Count == 0 || Resolve(hit["collider"].AsGodotObject() as Node) is not IInteractable candidate) return false;
        Vector3 position = hit["position"].AsVector3(), origin = _player.PerceptionTorso;
        if (!position.IsFinite() || origin.DistanceSquaredTo(position) > _reach * _reach) return false;
        _reachRay.From = origin; _reachRay.To = position + (position - origin).Normalized() * 0.02f;
        var cover = space.IntersectRay(_reachRay);
        if (cover.Count > 0 && !ReferenceEquals(Resolve(cover["collider"].AsGodotObject() as Node), candidate)) return false;
        context = new InteractionContext(_player, position);
        if (!candidate.TryGetAction(context, out action)) return false;
        target = candidate; return true;
    }

    // =========================================================
    // Bounded ancestor walk; no global registry, group scan or per-prop search callback.
    private static IInteractable? Resolve(Node? node)
    {
        for (int depth = 0; depth < 8 && GodotObject.IsInstanceValid(node); depth++, node = node!.GetParent())
        {
            if (node!.IsQueuedForDeletion() || !node.IsInsideTree()) return null;
            if (node is IInteractable target) return target;
        }
        return null;
    }
}
