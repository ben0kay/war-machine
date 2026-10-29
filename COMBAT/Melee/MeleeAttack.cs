// CM2-A: actor-neutral attack-window and invisible 3D melee contact query.
// Created once per attacker; only Advance() performs physics work while an attack is active.
using Godot;
using System.Collections.Generic;

public enum MeleeAimMode { Horizontal, ThreeDimensional }

public readonly struct MeleeContact
{
    public readonly CollisionObject3D Collider;
    public readonly Node3D Target;
    public readonly int ShapeIndex;
    public readonly Vector3 Position, Direction;

    // =========================================================
    public MeleeContact(CollisionObject3D collider, Node3D target, Vector3 position, Vector3 direction, int shapeIndex = -1)
    { ShapeIndex = shapeIndex; Collider = collider; Target = target; Position = position; Direction = direction; }
}

public sealed class MeleeAttack
{
    private const int MaxCandidates = 32;
    private readonly CollisionObject3D _owner;
    private readonly SphereShape3D _rangeShape = new();
    private readonly PhysicsShapeQueryParameters3D _overlap;
    private readonly PhysicsRayQueryParameters3D _line;
    private MeleeSettings? _profile;
    private float _elapsed;
    private ulong _nextReady;
    private bool _contactUsed;
    private readonly HashSet<ulong> _sweepHits = new(), _sweepCandidates = new();
    private readonly AnatomyRaycaster _anatomyRay = new();
    private readonly List<MeleeContact> _sweepContacts = new(8);
    private ulong _anatomyVersion = ulong.MaxValue;

    public bool IsActive => _profile != null;
    public float ElapsedSeconds => _elapsed;

    // =========================================================
    // Cache physics query objects for reuse across swings. No persistent Area3D or timer.
    public MeleeAttack(CollisionObject3D owner)
    {
        _owner = owner;
        _overlap = new PhysicsShapeQueryParameters3D
        {
            Shape = _rangeShape, Exclude = new Godot.Collections.Array<Rid> { owner.GetRid() },
            CollideWithBodies = true, CollideWithAreas = false
        };
        _line = new PhysicsRayQueryParameters3D
        {
            Exclude = new Godot.Collections.Array<Rid> { owner.GetRid() },
            CollideWithBodies = true, CollideWithAreas = false
        };
    }

    // =========================================================
    // Start on explicit input or AI request; cooldown needs no idle update.
    public bool TryStart(MeleeSettings profile)
    {
        if (!GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree() || IsActive
            || Time.GetTicksMsec() < _nextReady) return false;
        _profile = profile; _elapsed = 0f; _contactUsed = false; _sweepHits.Clear();
        _nextReady = Time.GetTicksMsec()
            + (ulong)Mathf.CeilToInt(Safe(profile.CooldownSeconds, 0.05f, 3f) * 1000f);
        return true;
    }

    // =========================================================
    // Call from the owner's existing physics tick only while IsActive. One hit per attack;
    // an active window crossed in a long frame still receives one collision opportunity.
    public bool Advance(float delta, Vector3 origin, Vector3 aim, MeleeAimMode mode, out MeleeContact contact, Node3D? intendedTarget = null)
    {
        contact = default;
        if (_profile == null) return false;
        if (!GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree())
        { Cancel(); return false; }

        float previous = _elapsed;
        _elapsed += Safe(delta, 0f, 1f);
        float windup = Safe(_profile.WindupSeconds, 0f, 1f);
        float active = Safe(_profile.ActiveSeconds, 0.01f, 1f);
        float recovery = Safe(_profile.RecoverySeconds, 0f, 1f);
        bool contactWindow = previous < windup + active && _elapsed >= windup;
        bool found = contactWindow && !_contactUsed
            && TryFindContact(_profile, origin, aim, mode, out contact, intendedTarget);
        if (found) _contactUsed = true;
        if (_elapsed >= windup + active + recovery) Cancel();
        return found;
    }

    // =========================================================
    // Bounded side sweep for damaging melee. Shared timing/occlusion/receiver rules; one hit per actor per attack.
    public IReadOnlyList<MeleeContact> AdvanceSweep(float delta, Vector3 origin, Vector3 aim)
    {
        _sweepContacts.Clear();
        if (_profile == null) return _sweepContacts;
        if (!GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree()) { Cancel(); return _sweepContacts; }
        MeleeSettings profile = _profile;
        float previous = _elapsed; _elapsed += Safe(delta, 0, 1);
        float windup = Safe(profile.WindupSeconds, 0, 1), active = Safe(profile.ActiveSeconds, 0.01f, 1);
        int cap = Mathf.Clamp(profile.MaximumTargets, 1, 8);
        aim.Y = 0;
        if (Finite(origin) && Finite(aim) && aim.LengthSquared() > 0.000001f
            && previous < windup + active && _elapsed >= windup && _sweepHits.Count < cap)
        {
            aim = aim.Normalized();
            if (_anatomyVersion != AnatomyHitboxes.RegistryVersion)
            {
                AnatomyHitboxes.ConfigureShotQuery(_line, _owner);
                _overlap.Exclude = _line.Exclude;
                _anatomyVersion = AnatomyHitboxes.RegistryVersion;
            }
            _overlap.CollideWithAreas = true; _line.HitFromInside = true;
            float range = Safe(profile.Range, 0.25f, 5);
            _rangeShape.Radius = range; _overlap.Transform = new Transform3D(Basis.Identity, origin);
            _overlap.CollisionMask = _line.CollisionMask = profile.HitMask;
            float arc = Mathf.DegToRad(Safe(profile.ArcDegrees, 10, 180));
            float from = arc * (0.5f - Mathf.Clamp((previous - windup) / active, 0, 1));
            float to = arc * (0.5f - Mathf.Clamp((_elapsed - windup) / active, 0, 1));
            int rays = 0; _sweepCandidates.Clear();
            foreach (Godot.Collections.Dictionary result in _owner.GetWorld3D().DirectSpaceState.IntersectShape(_overlap, 64))
            {
                if (_sweepHits.Count >= cap || rays >= 16) break;
                if (result["collider"].AsGodotObject() is not CollisionObject3D body
                    || !GodotObject.IsInstanceValid(body) || body.IsQueuedForDeletion()) continue;
                Node3D? actor = FindReceiver(body, _owner);
                if (actor == null || _sweepHits.Contains(actor.GetInstanceId()) || !_sweepCandidates.Add(actor.GetInstanceId())) continue;
                if (!TrySweepContact(body, actor, origin, aim, profile, range, from, to, ref rays, out MeleeContact contact)) continue;
                _sweepHits.Add(actor.GetInstanceId()); _sweepContacts.Add(contact);
            }
        }
        if (_elapsed >= windup + active + Safe(profile.RecoverySeconds, 0, 1))
        { _profile = null; _elapsed = 0; }
        return _sweepContacts;
    }

    // =========================================================
    // Refine each broad actor only during this active sector, preserving per-region height/arc and real cover.
    private bool TrySweepContact(CollisionObject3D body, Node3D actor, Vector3 origin, Vector3 aim,
        MeleeSettings profile, float range, float from, float to, ref int rays, out MeleeContact contact)
    {
        contact = default;
        AnatomyHitboxes? anatomy = body as AnatomyHitboxes;
        int parts = anatomy?.PartCount ?? 1;
        var space = _owner.GetWorld3D().DirectSpaceState;
        for (int part = 0; part < parts && rays < 16; part++)
        {
            Vector3 centre;
            if (anatomy != null) { if (!anatomy.TryPartCentre(part, out centre)) continue; }
            else centre = ContactCentre(body);
            Vector3 offset = centre - origin;
            if (!Finite(offset) || Mathf.Abs(offset.Y) > Safe(profile.VerticalTolerance, 0.05f, 3)) continue;
            offset.Y = 0;
            float distance = offset.Length();
            if (distance < 0.001f || distance > range) continue;
            float angle = Mathf.Atan2(aim.Cross(offset / distance).Y, aim.Dot(offset / distance));
            float thickness = Mathf.Atan2(Safe(profile.SweepThickness, 0.01f, 0.5f), distance);
            if (angle > from + thickness || angle < to - thickness) continue;
            _line.From = origin; _line.To = centre;
            bool hasHit = _anatomyRay.TryHit(space, _line, out AnatomyRayHit hit, 16 - rays);
            rays += _anatomyRay.LastQueryCount;
            if (!hasHit || hit.Collider is not CollisionObject3D first || !IsDescendantOf(first, actor)) continue;
            Vector3 direction = centre - _owner.GlobalPosition; direction.Y = 0;
            if (direction.LengthSquared() < 0.001f) direction = aim;
            contact = new MeleeContact(first, actor, hit.Position, direction.Normalized(), hit.ShapeIndex);
            return true;
        }
        return false;
    }

    // =========================================================
    // Abort safely on focus loss, weapon swap, death, deactivation or unload.
    // Preserve attack cooldown so interruption cannot reset the rate limit.
    public void Cancel() { _profile = null; _elapsed = 0f; _contactUsed = false; _sweepContacts.Clear(); _sweepHits.Clear(); _sweepCandidates.Clear(); }

    // =========================================================
    // Broad phase finds overlapping bodies; arc/height/reach and an occlusion ray
    // accept the closest eligible actor only. No damage/impact is dispatched in CM2-A.
    private bool TryFindContact(MeleeSettings profile, Vector3 origin, Vector3 aim,
        MeleeAimMode mode, out MeleeContact contact, Node3D? intendedTarget)
    {
        contact = default;
        if (!Finite(origin) || !Finite(aim)) return false;
        if (mode == MeleeAimMode.Horizontal) aim.Y = 0f;
        if (aim.LengthSquared() < 0.000001f) return false;
        aim = aim.Normalized();

        float range = Safe(profile.Range, 0.25f, 5f);
        float vertical = Safe(profile.VerticalTolerance, 0.25f, 3f);
        float facingDot = Mathf.Cos(Mathf.DegToRad(Safe(profile.ArcDegrees, 10f, 180f) * 0.5f));
        _rangeShape.Radius = range;
        _overlap.Transform = new Transform3D(Basis.Identity, origin);
        _overlap.CollisionMask = profile.HitMask;
        _line.CollisionMask = profile.HitMask;

        var space = _owner.GetWorld3D().DirectSpaceState;
        float bestDistance = float.MaxValue;
        bool selected = false;
        foreach (Godot.Collections.Dictionary result in space.IntersectShape(_overlap, MaxCandidates))
        {
            if (!result.ContainsKey("collider") || result["collider"].AsGodotObject() is not CollisionObject3D body
                || !GodotObject.IsInstanceValid(body) || body.IsQueuedForDeletion()) continue;
            Node3D? actor = FindReceiver(body, _owner);
            if (actor == null || (intendedTarget != null && actor != intendedTarget)) continue;
            Vector3 centre = ContactCentre(body);
            Vector3 offset = centre - origin;
            if (!Finite(offset) || Mathf.Abs(offset.Y) > vertical) continue;
            Vector3 facing = offset;
            if (mode == MeleeAimMode.Horizontal) facing.Y = 0f;
            float distance = facing.LengthSquared();
            if (distance < 0.000001f || distance > range * range || distance >= bestDistance) continue;
            if (aim.Dot(facing / Mathf.Sqrt(distance)) < facingDot) continue;

            // Overlap alone is insufficient: a closed door/wall may be between bodies.
            // The first ray collision must belong to this actor, never the obstruction.
            _line.From = origin; _line.To = centre;
            var blocking = space.IntersectRay(_line);
            if (blocking.Count == 0 || !blocking.ContainsKey("collider")
                || blocking["collider"].AsGodotObject() is not Node first
                || !IsDescendantOf(first, actor)) continue;

            Vector3 push = centre - _owner.GlobalPosition; push.Y = 0f;
            if (push.LengthSquared() < 0.000001f) { push = aim; push.Y = 0f; }
            if (push.LengthSquared() < 0.000001f) push = Vector3.Forward;
            contact = new MeleeContact(body, actor, blocking["position"].AsVector3(), push.Normalized());
            bestDistance = distance; selected = true;
        }
        return selected;
    }

    // =========================================================
    // Use the collision-shape centre for humanoids; this avoids aiming at their feet in FPS.
    private static Vector3 ContactCentre(CollisionObject3D body)
    {
        foreach (Node child in body.GetChildren())
            if (child is CollisionShape3D shape && !shape.Disabled) return shape.GlobalPosition;
        return body.GlobalPosition;
    }

    // =========================================================
    // Same bounded ancestor eligibility rules as the CM0 hit dispatcher.
    private static Node3D? FindReceiver(Node body, Node owner)
    {
        for (Node? node = body; node != null; node = node.GetParent())
        {
            if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion() || node == owner) return null;
            if (node is Node3D actor && (node is IDamageReceiver || node is IImpactReceiver)) return actor;
            // The typical actor hierarchy is shallow; forbid a full-world parent traversal.
            if (node.GetParent() == null) return null;
        }
        return null;
    }

    // =========================================================
    private static bool IsDescendantOf(Node node, Node ancestor)
    {
        for (Node? current = node; current != null; current = current.GetParent())
            if (current == ancestor) return true;
        return false;
    }

    // =========================================================
    private static bool Finite(Vector3 v)
    { return float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z); }

    // =========================================================
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }
}
