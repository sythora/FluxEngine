using System.Diagnostics;
using FluxEngine.Audio;
using FluxEngine.Content;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Platform;

namespace FluxEngine.Core;

public sealed class Engine : IDisposable
{
    private readonly Window _window;
    private bool _isRunning;
    private AudioManager? _audio;
    private EngineSettings _settings = new();

    public Engine(string title = "FluxEngine", int width = 1280, int height = 720)
    {
        _settings = EngineSettings.Load();
        Current = this;
        _window = new Window(title, width, height, _settings.Vsync);
        _settings.Changed += OnSettingsChanged;
    }

    // so components can grab input and shutdown without being handed the engine
    public static Engine? Current { get; private set; }

    public Window Window => _window;
    public InputManager Input => _window.Input;
    public InputActions Actions => _window.Input.Actions;
    public Renderer Renderer => _window.Renderer;
    public EngineSettings Settings
    {
        get => _settings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_settings, value))
                return;

            _settings.Changed -= OnSettingsChanged;
            _settings = value;
            _settings.Changed += OnSettingsChanged;
            ApplySettings();
        }
    }

    // texture cache, shared by everything
    public AssetManager Assets { get; } = new();

    // audio init happens the first time you touch this
    public AudioManager Audio => _audio ??= CreateAudioManager();

    public float TimeScale
    {
        get => Time.TimeScale;
        set => Time.TimeScale = value;
    }

    // how often fixed updates run when you call Run(scene)
    public float FixedTimeStep { get; set; } = 1f / 60f;

    public void Run() => Run(_ => { });

    // old school way, no scene, you own everything
    public void Run(Action<float> update, Action? render = null)
    {
        ArgumentNullException.ThrowIfNull(update);
        ObjectDisposedException.ThrowIf(!_window.IsOpen, this);

        if (_isRunning)
            throw new InvalidOperationException("The engine is already running.");

        _isRunning = true;
        Time.Reset();

        var timer = Stopwatch.StartNew();
        var frameTimer = Stopwatch.StartNew();
        var previousTime = timer.Elapsed.TotalSeconds;

        try
        {
            while (_window.IsOpen)
            {
                frameTimer.Restart();
                _window.BeginFrame();

                var currentTime = timer.Elapsed.TotalSeconds;
                var unscaledDeltaTime = (float)(currentTime - previousTime);
                previousTime = currentTime;
                Time.Tick(unscaledDeltaTime);

                update(Time.DeltaTime);

                if (!_window.IsOpen)
                    break;

                if (render is not null)
                {
                    render();
                    _window.Present();
                }

                LimitFrameRate(frameTimer);
            }
        }
        finally
        {
            _isRunning = false;
            Shutdown();
        }
    }

    // scene way, engine owns the loop, fixed updates and the final draw
    public void Run(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ObjectDisposedException.ThrowIf(!_window.IsOpen, this);

        if (_isRunning)
            throw new InvalidOperationException("The engine is already running.");

        _isRunning = true;
        Time.Reset();

        var timer = Stopwatch.StartNew();
        var frameTimer = Stopwatch.StartNew();
        var previousTime = timer.Elapsed.TotalSeconds;
        var accumulator = 0.0;

        try
        {
            scene.Start();

            while (_window.IsOpen)
            {
                frameTimer.Restart();
                _window.BeginFrame();

                var currentTime = timer.Elapsed.TotalSeconds;
                var unscaledDeltaTime = (float)(currentTime - previousTime);
                previousTime = currentTime;
                Time.Tick(unscaledDeltaTime);

                accumulator += Time.DeltaTime;

                // alt tab for ten minutes and this stops trying to catch up
                if (accumulator > 0.25)
                    accumulator = 0.25;

                while (accumulator >= FixedTimeStep)
                {
                    scene.FixedUpdate(FixedTimeStep);
                    accumulator -= FixedTimeStep;
                }

                scene.Update(Time.DeltaTime);

                if (!_window.IsOpen)
                    break;

                var background = scene.BackgroundColor;
                _window.Clear(background.R, background.G, background.B);
                scene.Draw(_window.Renderer);
                _window.Present();
                LimitFrameRate(frameTimer);
            }
        }
        finally
        {
            _isRunning = false;
            Shutdown();
        }
    }

    public void Shutdown()
    {
        _audio?.Dispose();
        Assets.Clear();
        _window.Close();
    }

    private AudioManager CreateAudioManager()
    {
        var audio = new AudioManager
        {
            MasterVolume = Settings.MasterVolume,
            SfxVolume = Settings.SfxVolume,
            MusicVolume = Settings.MusicVolume,
        };
        return audio;
    }

    private void LimitFrameRate(Stopwatch frameTimer)
    {
        if (_window.IsVSyncEnabled || Settings.TargetFps <= 0)
            return;

        var targetFrameSeconds = 1d / Settings.TargetFps;
        var remaining = targetFrameSeconds - frameTimer.Elapsed.TotalSeconds;
        if (remaining > 0)
            Thread.Sleep(TimeSpan.FromSeconds(remaining));
    }

    private void OnSettingsChanged(EngineSettings settings)
    {
        if (ReferenceEquals(settings, _settings))
            ApplySettings();
    }

    private void ApplySettings()
    {
        if (_window.IsOpen)
            _window.SetVSync(Settings.Vsync);

        if (_audio is not null)
        {
            _audio.MasterVolume = Settings.MasterVolume;
            _audio.SfxVolume = Settings.SfxVolume;
            _audio.MusicVolume = Settings.MusicVolume;
        }
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        Shutdown();
        GC.SuppressFinalize(this);
    }
}
