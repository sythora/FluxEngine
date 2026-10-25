namespace FluxEngine.Mathematics;

// small helpers that show up in basically every game
public static class MathUtil
{
    public const float Epsilon = 1e-6f;

    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    public static float Clamp01(float value) => Clamp(value, 0, 1);

    // t = 0 gives from, t = 1 gives to
    public static float Lerp(float from, float to, float t)
        => from + (to - from) * Clamp01(t);

    // turns a value back into a t between 0 and 1
    public static float InverseLerp(float from, float to, float value)
    {
        if (MathF.Abs(to - from) < Epsilon)
            return 0;
        return Clamp01((value - from) / (to - from));
    }

    // linear interpolation with a smooth ease at the ends
    public static float SmoothStep(float from, float to, float t)
    {
        t = Clamp01(InverseLerp(from, to, t));
        return t * t * (3 - 2 * t);
    }

    // rescales a value between 0 and 1
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        => Lerp(toMin, toMax, InverseLerp(fromMin, fromMax, value));

    // wraps a value, bigger than power goes back to the start
    public static float Repeat(float value, float length)
    {
        if (length <= Epsilon)
            return 0;
        return Clamp(value - MathF.Floor(value / length) * length, 0, length);
    }

    // bounces a value back, good for ping pong animations
    public static float PingPong(float value, float length)
    {
        var wrapped = Repeat(value, length * 2);
        return wrapped > length ? length * 2 - wrapped : wrapped;
    }

    // moves current toward target without going past it
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;
        return current + MathF.Sign(target - current) * maxDelta;
    }

    public static bool Approximately(float a, float b)
        => MathF.Abs(a - b) < Epsilon;

    public static float DegToRad(float degrees) => degrees * (MathF.PI / 180f);
    public static float RadToDeg(float radians) => radians * (180f / MathF.PI);

    // frame rate independent smoothing, put this in your update loop and it
    // settles on the same pace at 30 or 144 fps
    public static float SmoothTowards(float current, float target, float speed, float deltaTime)
        => Lerp(current, target, 1 - MathF.Exp(-speed * deltaTime));
}
