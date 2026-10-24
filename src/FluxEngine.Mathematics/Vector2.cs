namespace FluxEngine.Mathematics;

public readonly struct Vector2(float x, float y)
{
    public float X { get; } = x;
    public float Y { get; } = y;

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
    public float Length => MathF.Sqrt(X * X + Y * Y);
    public float LengthSquared => X * X + Y * Y;

    public Vector2 Normalized => Length > 0 ? new Vector2(X / Length, Y / Length) : Zero;

    // example: 
    // Vector2 playerpost = new(100,200)
    // Vector velocity = new(5, 0)
    // playerpost += velocity; and then print it and it would be 105 200
    
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator /(Vector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);
    public static float Distance(Vector2 a, Vector2 b) => (a-b).Length;
    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
    public override string ToString() => $"({X}, {Y})";

}