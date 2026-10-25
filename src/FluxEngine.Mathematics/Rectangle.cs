namespace FluxEngine.Mathematics;

// axis aligned float rectangle, X and Y are the top left corner
public readonly struct Rectangle(float x, float y, float width, float height)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    public static Rectangle FromCenter(Vector2 center, Vector2 size)
        => new(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f, size.X, size.Y);

    public static Rectangle FromPoints(Vector2 cornerA, Vector2 cornerB)
    {
        var min = Vector2.Min(cornerA, cornerB);
        var max = Vector2.Max(cornerA, cornerB);
        return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }

    public bool Contains(Vector2 point)
        => point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;

    public bool Contains(Rectangle other)
        => other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;

    public bool Intersects(Rectangle other)
        => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    // grows the rectangle outwards by the same amount on all sides
    public Rectangle Inflate(float amount)
        => new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    public Rectangle OffsetBy(Vector2 offset) => new(X + offset.X, Y + offset.Y, Width, Height);

    public void Deconstruct(out float x, out float y, out float width, out float height)
        => (x, y, width, height) = (X, Y, Width, Height);

    public override string ToString() => $"({X}, {Y}, {Width}x{Height})";
}
