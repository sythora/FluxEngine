namespace FluxEngine.Mathematics;

// tiny wrapper around System.Random with game friendly helpers
public sealed class Rng
{
    private readonly System.Random _random;

    public Rng(int seed) => _random = new System.Random(seed);
    public Rng() : this(Environment.TickCount) { }

    // one shared instance, fine for 99% of games, only bother with your own
    // instance when you need repeatable seed based layouts
    public static Rng Shared { get; } = new();

    // both ends are inclusive, so Next(1, 6) can roll a 6
    public int Next(int min, int max) => _random.Next(min, max + 1);
    public int Next(int maxInclusive) => _random.Next(maxInclusive + 1);

    public float NextFloat() => _random.NextSingle();
    public float NextFloat(float min, float max) => min + _random.NextSingle() * (max - min);

    // true with the given probability, Chance(0.25) fires about 1 in 4 calls
    public bool Chance(float probability) => _random.NextSingle() < probability;

    public bool Flip() => _random.Next(2) == 0;

    // random direction and distance inside a circle
    public Vector2 InsideCircle(float radius)
    {
        var angle = NextFloat(0, 360f);
        return Vector2.FromAngle(angle) * radius * MathF.Sqrt(NextFloat());
    }

    public Vector2 OnCircle(float radius) => Vector2.FromAngle(NextFloat(0, 360f)) * radius;

    public Vector2 InsideRectangle(Rectangle rectangle)
        => new(NextFloat(rectangle.X, rectangle.Right), NextFloat(rectangle.Y, rectangle.Bottom));

    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items is not { Count: > 0 })
            throw new ArgumentException("The list is empty, nothing to pick.", nameof(items));
        return items[Next(items.Count - 1)];
    }

    // shuffles in place, plain fisher yates
    public void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var swap = Next(i);
            (items[i], items[swap]) = (items[swap], items[i]);
        }
    }
}
