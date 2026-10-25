using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

public readonly record struct PhysicsQueryHit(BoxCollider Collider, Entity? Entity);

public readonly record struct RaycastHit(Vector2 Point, Vector2 Normal, float Distance, BoxCollider Collider, Entity? Entity);

public static class PhysicsQueries
{
    public static List<PhysicsQueryHit> OverlapBox(Scene scene, BoxCollider area, bool includeTriggers = true)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var results = new List<PhysicsQueryHit>();
        foreach (var solid in scene.Solids)
        {
            if (Collision.Overlaps(area, solid))
                results.Add(new PhysicsQueryHit(solid, null));
        }

        foreach (var component in scene.Colliders)
        {
            if (!IsQueryable(component, includeTriggers))
                continue;
            var bounds = component.Bounds;
            if (Collision.Overlaps(area, bounds))
                results.Add(new PhysicsQueryHit(bounds, component.Entity));
        }

        return results;
    }

    public static List<PhysicsQueryHit> OverlapCircle(Scene scene, CircleCollider area, bool includeTriggers = true)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var results = new List<PhysicsQueryHit>();
        foreach (var solid in scene.Solids)
        {
            if (Collision.Overlaps(solid, area))
                results.Add(new PhysicsQueryHit(solid, null));
        }

        foreach (var component in scene.Colliders)
        {
            if (!IsQueryable(component, includeTriggers))
                continue;
            var bounds = component.Bounds;
            if (Collision.Overlaps(bounds, area))
                results.Add(new PhysicsQueryHit(bounds, component.Entity));
        }

        return results;
    }

    public static bool Raycast(Scene scene, Vector2 origin, Vector2 direction, float maxDistance, out RaycastHit hit)
    {
        ArgumentNullException.ThrowIfNull(scene);
        hit = default;
        if (!float.IsFinite(maxDistance) || maxDistance < 0 || !float.IsFinite(direction.X) || !float.IsFinite(direction.Y))
            return false;

        var length = direction.Length;
        if (length <= 0 || !float.IsFinite(length))
            return false;
        direction /= length;

        var closest = maxDistance;
        var found = false;
        var bestHit = default(RaycastHit);
        foreach (var solid in scene.Solids)
            TryHit(solid, null);

        foreach (var component in scene.Colliders)
        {
            if (!IsQueryable(component, includeTriggers: true))
                continue;
            TryHit(component.Bounds, component.Entity);
        }

        hit = bestHit;
        return found;

        void TryHit(BoxCollider collider, Entity? entity)
        {
            if (!RayBox(origin, direction, collider, out var distance, out var normal) || distance > closest)
                return;

            closest = distance;
            bestHit = new RaycastHit(origin + direction * distance, normal, distance, collider, entity);
            found = true;
        }
    }

    private static bool IsQueryable(ColliderComponent component, bool includeTriggers)
        => !component.Destroyed && component.Entity is { Enabled: true } && (includeTriggers || !component.IsTrigger);

    private static bool RayBox(Vector2 origin, Vector2 direction, BoxCollider box, out float distance, out Vector2 normal)
    {
        distance = 0;
        normal = Vector2.Zero;
        var min = box.Min;
        var max = box.Max;
        var near = float.NegativeInfinity;
        var far = float.PositiveInfinity;
        var nearNormal = Vector2.Zero;

        if (!Clip(origin.X, direction.X, min.X, max.X, new Vector2(-1, 0), new Vector2(1, 0), ref near, ref far, ref nearNormal) ||
            !Clip(origin.Y, direction.Y, min.Y, max.Y, new Vector2(0, -1), new Vector2(0, 1), ref near, ref far, ref nearNormal))
            return false;

        if (far < 0 || near > far)
            return false;

        if (near >= 0)
        {
            distance = near;
            normal = nearNormal;
        }
        else
        {
            distance = 0;
            normal = -direction;
        }

        return true;
    }

    private static bool Clip(float origin, float direction, float min, float max, Vector2 minNormal, Vector2 maxNormal,
        ref float near, ref float far, ref Vector2 nearNormal)
    {
        if (MathF.Abs(direction) < 1e-8f)
            return origin >= min && origin <= max;

        var first = (min - origin) / direction;
        var second = (max - origin) / direction;
        var firstNormal = minNormal;
        if (first > second)
        {
            (first, second) = (second, first);
            firstNormal = maxNormal;
        }

        if (first > near)
        {
            near = first;
            nearNormal = firstNormal;
        }

        far = MathF.Min(far, second);
        return near <= far;
    }
}
