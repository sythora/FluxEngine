using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

// center based box, because entities move by their center anyway
public readonly struct BoxCollider(Vector2 center, Vector2 size)
{
    public Vector2 Center { get; } = center;
    public Vector2 Size { get; } = size;

    public Vector2 Half => Size * 0.5f;
    public Vector2 Min => Center - Half;
    public Vector2 Max => Center + Half;
    public Rectangle Bounds => new(Min.X, Min.Y, Size.X, Size.Y);

    public bool Contains(Vector2 point)
        => point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;

    public BoxCollider Moved(Vector2 offset) => new(Center + offset, Size);
    public BoxCollider Resized(Vector2 size) => new(Center, size);
}

public readonly struct CircleCollider(Vector2 center, float radius)
{
    public Vector2 Center { get; } = center;
    public float Radius { get; } = radius;

    public bool Contains(Vector2 point) => (point - Center).LengthSquared <= Radius * Radius;
    public CircleCollider Moved(Vector2 offset) => new(Center + offset, Radius);
}
