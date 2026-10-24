using SDL2;
using FluxEngine.Input;

namespace FluxEngine.Platform;

public sealed class Window
{
    private IntPtr _handle;
    private bool _isOpen;

    public Window(string title = "FluxEngine", int width = 1280, int height = 720)
    {
        Input = new InputManager();
        Initialize(title, width, height);
    }

    public InputManager Input { get; }
    public IntPtr Handle => _handle;
    public bool IsOpen => _isOpen;

    public void BeginFrame()
    {
        Input.BeginFrame();
        PollEvents();
    }

    public void PollEvents()
    {
        if (!_isOpen)
            return;

        while (SDL.SDL_PollEvent(out var evt) != 0)
        {
            SdlInputAdapter.ProcessEvent(evt, Input);

            if (evt.type == SDL.SDL_EventType.SDL_QUIT)
                _isOpen = false;
        }
    }

    public void Close()
    {
        if (_handle != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(_handle);
            _handle = IntPtr.Zero;
        }

        SDL.SDL_Quit();
        _isOpen = false;
    }

    private void Initialize(string title, int width, int height)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            throw new InvalidOperationException($"Failed to initialize SDL2: {SDL.SDL_GetError()}");

        _handle = SDL.SDL_CreateWindow(
            title,
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            width,
            height,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to create SDL2 window: {SDL.SDL_GetError()}");

        _isOpen = true;
        Input.SetFocus(true);
    }
}