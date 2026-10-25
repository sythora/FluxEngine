namespace FluxEngine.Mathematics;

public readonly struct Vector2(float x, float y)
{
    public float X { get; } = x;
    public float Y { get; } = y;

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
    public static Vector2 UnitX => new(1, 0);
    public static Vector2 UnitY => new(0, 1);

    public float Length => MathF.Sqrt(X * X + Y * Y);
    public float LengthSquared => X * X + Y * Y;

    public Vector2 Normalized => Length > 0 ? new Vector2(X / Length, Y / Length) : Zero;

    // angle in degrees, atan2 style so it wraps the full 360
    public float AngleDegrees => MathUtil.RadToDeg(MathF.Atan2(Y, X));

    // example:
    // Vector2 playerPos = new(100,200)
    // Vector2 velocity = new(5, 0)
    // playerPos += velocity; and then print it and it would be 105 200

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator - (Vector2 v) => new(-v.X, -v.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator *(float scalar, Vector2 a) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2 operator /(Vector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);
    public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

    public float Dot(Vector2 other) => X * other.X + Y * other.Y;
    public float DistanceTo(Vector2 other) => (this - other).Length;
    public float DistanceSquaredTo(Vector2 other) => (this - other).LengthSquared;

    // rotate around the origin, degrees, matches SDL's y down screen space
    public Vector2 Rotate(float degrees)
    {
        var radians = MathUtil.DegToRad(degrees);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new(X * cos - Y * sin, X * sin + Y * cos);
    }

    // the vector turned 90 degrees
    public Vector2 Perpendicular => new(-Y, X);

    // reflect off a surface given its normal vector, good for ball bouncing
    public Vector2 Reflect(Vector2 normal) => this - (2 * Dot(normal)) * normal;

    // clamp the length without changing direction
    public Vector2 ClampToLength(float maxLength)
    {
        if (Length <= maxLength || Length == 0)
            return this;
        return Normalized * maxLength;
    }

    public Vector2 Rounded => new(MathF.Round(X), MathF.Round(Y));
    public Vector2 Floored => new(MathF.Floor(X), MathF.Floor(Y));
    public Vector2 Ceil => new(MathF.Ceiling(X), MathF.Ceiling(Y));
    public Vector2 Abs => new(MathF.Abs(X), MathF.Abs(Y));

    public void Deconstruct(out float x, out float y) => (x, y) = (X, Y);

    public static Vector2 Min(Vector2 a, Vector2 b) => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));
    public static Vector2 Max(Vector2 a, Vector2 b) => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));

    public static Vector2 FromAngle(float degrees)
    {
        var radians = MathUtil.DegToRad(degrees);
        return new(MathF.Cos(radians), MathF.Sin(radians));
    }

    public static Vector2 Lerp(Vector2 from, Vector2 to, float t)
    {
        t = MathUtil.Clamp01(t);
        return from + (to - from) * t;
    }

    // like Lerp but walks at most maxDelta units per call, use it for smooth camera follow
    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
    {
        var difference = target - current;
        var length = difference.Length;
        if (length <= maxDelta || length <= 0)
            return target;
        return current + difference / length * maxDelta;
    }

    public bool Equals(Vector2 other) => this == other;
    public override bool Equals(object? obj) => obj is Vector2 other && this == other;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
}
