namespace FluxEngine.Core;

public sealed class SceneLoop
{
    private readonly Scene _scene;
    private readonly float _fixedTimeStep;
    private readonly float _maxFrameDelta;
    private float _accumulator;

    public SceneLoop(Scene scene, float fixedTimeStep = 1f / 60f, float maxFrameDelta = 0.25f)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!float.IsFinite(fixedTimeStep) || fixedTimeStep <= 0)
            throw new ArgumentOutOfRangeException(nameof(fixedTimeStep), "Fixed timestep must be positive and finite.");
        if (!float.IsFinite(maxFrameDelta) || maxFrameDelta <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFrameDelta), "Maximum frame delta must be positive and finite.");

        _scene = scene;
        _fixedTimeStep = fixedTimeStep;
        _maxFrameDelta = maxFrameDelta;
    }

    public bool IsRunning { get; private set; }
    public float Interpolation => _accumulator / _fixedTimeStep;

    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException("The scene loop is already running.");

        Time.Reset();
        _accumulator = 0;
        IsRunning = true;
        try
        {
            _scene.Start();
        }
        catch
        {
            IsRunning = false;
            throw;
        }
    }

    public void AdvanceFrame(float unscaledDeltaTime)
    {
        ObjectDisposedException.ThrowIf(!IsRunning, this);
        if (!float.IsFinite(unscaledDeltaTime) || unscaledDeltaTime < 0)
            throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime), "Frame delta must be non-negative and finite.");

        unscaledDeltaTime = Math.Min(unscaledDeltaTime, _maxFrameDelta);
        Time.Tick(unscaledDeltaTime);
        _accumulator = Math.Min(_maxFrameDelta, _accumulator + Time.DeltaTime);

        while (_accumulator >= _fixedTimeStep)
        {
            _scene.FixedUpdate(_fixedTimeStep);
            _accumulator -= _fixedTimeStep;
        }

        _scene.Update(Time.DeltaTime);
    }

    public void Stop() => IsRunning = false;
}
