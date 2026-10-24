using FluxEngine.Platform;

namespace FluxEngine.Core;

public sealed class Engine
{
    private readonly Window _window;

    public Engine(string title = "FluxEngine", int width = 1280, int height = 720)
    {
        _window = new Window(title, width, height);
    }

    public Window Window => _window;

    public void Run()
    {
        while (_window.IsOpen)
        {
            _window.BeginFrame();
        }
    }

    public void Shutdown()
    {
        _window.Close();
    }
}