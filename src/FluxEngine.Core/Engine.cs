using FluxEngine.Input;
using FluxEngine.Platform;

namespace FluxEngine.Core;

public sealed class Engine : IDisposable
{
    private readonly Window _window;
    private bool _isRunning;

    public Engine(string title = "FluxEngine", int width = 1280, int height = 720)
    {
        _window = new Window(title, width, height);
    }

    public Window Window => _window;
    public InputManager Input => _window.Input;

    public void Run()
    {
        Run(_ => { });
    }

    public void Run(Action<float> update, Action? render = null)
    {
        ArgumentNullException.ThrowIfNull(update);
        ObjectDisposedException.ThrowIf(!_window.IsOpen, this);
        if (_isRunning)
            throw new InvalidOperationException("The engine is already running.");

        _isRunning = true;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var previousTime = timer.Elapsed.TotalSeconds;

        try
        {
            while (_window.IsOpen)
            {
                _window.BeginFrame();

                var currentTime = timer.Elapsed.TotalSeconds;
                var deltaTime = (float)(currentTime - previousTime);
                previousTime = currentTime;

                update(deltaTime);
                if (!_window.IsOpen)
                    break;

                if (render is not null)
                {
                    render();
                    _window.Present();
                }
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
        _window.Close();
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }
}