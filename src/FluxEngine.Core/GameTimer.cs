namespace FluxEngine.Core;

// simple timer that counts down, tick it from your update loop
public sealed class GameTimer
{
    public float Duration { get; set; }
    public bool Repeating { get; set; }
    public bool Paused { get; set; }

    // fired when it hits zero
    public Action? OnTick { get; set; }

    private float _elapsed;

    public GameTimer(float duration, bool repeating = false)
    {
        Duration = duration;
        Repeating = repeating;
    }

    public float Progress => Duration <= 0 ? 1 : Mathematics.MathUtil.Clamp01(_elapsed / Duration);
    public float Remaining => MathF.Max(0, Duration - _elapsed);

    // true on the frame it fires
    public bool Update(float deltaTime)
    {
        if (Paused || Duration <= 0)
            return false;

        _elapsed += deltaTime;

        if (_elapsed < Duration)
            return false;

        // keep the leftover time so repeating timers don't drift
        _elapsed -= Duration;

        if (!Repeating)
            Paused = true;

        OnTick?.Invoke();
        return true;
    }

    public void Restart()
    {
        _elapsed = 0;
        Paused = false;
    }

    public void Stop()
    {
        _elapsed = 0;
        Paused = true;
    }
}
