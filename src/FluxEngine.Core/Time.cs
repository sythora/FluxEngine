namespace FluxEngine.Core;

// static clock, no need to pass delta time to every little helper
public static class Time
{
    // already multiplied by TimeScale
    public static float DeltaTime { get; internal set; }

    // real time between frames, ignores TimeScale
    public static float UnscaledDeltaTime { get; private set; }

    public static float TotalTime { get; private set; }
    public static int FrameCount { get; private set; }

    // 0 freezes the game, 0.5 is slow motion, 2 is double speed
    public static float TimeScale { get; set; } = 1f;

    // smoothed, so it doesn't flicker every frame
    public static float Fps { get; private set; }

    internal static void Tick(float unscaledDeltaTime)
    {
        UnscaledDeltaTime = unscaledDeltaTime;
        DeltaTime = unscaledDeltaTime * TimeScale;
        TotalTime += DeltaTime;
        FrameCount++;

        if (unscaledDeltaTime > 0)
            Fps = Mathematics.MathUtil.Lerp(Fps, 1f / unscaledDeltaTime, 0.1f);
    }

    internal static void Reset()
    {
        DeltaTime = 0;
        UnscaledDeltaTime = 0;
        TotalTime = 0;
        FrameCount = 0;
        Fps = 0;
    }
}
