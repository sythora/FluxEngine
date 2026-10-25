using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

// overlap checks and the push out math. static on purpose, no state here
public static class Collision
{
    // touching edges count as overlapping, that lets boxes rest on floors
    public static bool Overlaps(BoxCollider a, BoxCollider b)
        => a.Min.X <= b.Max.X && a.Max.X >= b.Min.X &&
           a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y;

    public static bool Overlaps(CircleCollider a, CircleCollider b)
    {
        var radii = a.Radius + b.Radius;
        return (a.Center - b.Center).LengthSquared <= radii * radii;
    }

    public static bool Overlaps(BoxCollider box, CircleCollider circle)
    {
        var closest = ClosestPoint(box, circle.Center);
        return (closest - circle.Center).LengthSquared <= circle.Radius * circle.Radius;
    }

    public static bool Overlaps(CircleCollider circle, BoxCollider box) => Overlaps(box, circle);

    public static Vector2 ClosestPoint(BoxCollider box, Vector2 point)
        => new(
            MathUtil.Clamp(point.X, box.Min.X, box.Max.X),
            MathUtil.Clamp(point.Y, box.Min.Y, box.Max.Y));

    // smallest push that gets a out of b. returns zero when they don't touch
    public static Vector2 Separate(BoxCollider a, BoxCollider b)
    {
        var overlapX = MathF.Min(a.Max.X, b.Max.X) - MathF.Max(a.Min.X, b.Min.X);
        var overlapY = MathF.Min(a.Max.Y, b.Max.Y) - MathF.Max(a.Min.Y, b.Min.Y);

        if (overlapX <= 0 || overlapY <= 0)
            return Vector2.Zero;

        // shove out on whichever axis needs less movement
        if (overlapX < overlapY)
            return new Vector2(overlapX * (a.Center.X < b.Center.X ? -1 : 1), 0);

        return new Vector2(0, overlapY * (a.Center.Y < b.Center.Y ? -1 : 1));
    }
}
