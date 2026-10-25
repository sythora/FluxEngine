using SDL2;
using FluxEngine.Graphics;
using FluxEngine.Input;

namespace FluxEngine.Platform;

public sealed class Window : IDisposable
{
    private IntPtr _handle;
    private IntPtr _renderer;
    private bool _isOpen;
    private bool _sdlInitialized;
    private int _width;
    private int _height;

    public Window(string title = "FluxEngine", int width = 1280, int height = 720)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Window width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Window height must be positive.");

        _width = width;
        _height = height;
        Input = new InputManager();
        Initialize(title, width, height);
    }

    public InputManager Input { get; }

    // the drawing helpers, wraps the SDL renderer
    public Renderer Renderer { get; private set; } = null!;

    public IntPtr Handle => _handle;
    public bool IsOpen => _isOpen;
    public int Width => _width;
    public int Height => _height;

    public bool IsFullscreen
    {
        get
        {
            if (_handle == IntPtr.Zero)
                return false;

            var flags = (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(_handle);
            return (flags & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
        }
    }

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

            if (evt.type == SDL.SDL_EventType.SDL_QUIT ||
                (evt.type == SDL.SDL_EventType.SDL_WINDOWEVENT &&
                 evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE))
            {
                _isOpen = false;
            }

            // keep Width and Height honest when the user drags the border
            if (evt.type == SDL.SDL_EventType.SDL_WINDOWEVENT &&
                evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
            {
                SDL.SDL_GetWindowSize(_handle, out _width, out _height);
            }
        }
    }

    public void Clear(byte red = 24, byte green = 28, byte blue = 38, byte alpha = 255)
    {
        EnsureOpen();
        CheckSdlResult(SDL.SDL_SetRenderDrawColor(_renderer, red, green, blue, alpha), "set clear color");
        CheckSdlResult(SDL.SDL_RenderClear(_renderer), "clear window");
    }

    public void FillRectangle(int x, int y, int width, int height, byte red, byte green, byte blue, byte alpha = 255)
    {
        EnsureOpen();

        if (width <= 0 || height <= 0)
            return;

        CheckSdlResult(SDL.SDL_SetRenderDrawColor(_renderer, red, green, blue, alpha), "set draw color");
        var rectangle = new SDL.SDL_Rect { x = x, y = y, w = width, h = height };
        CheckSdlResult(SDL.SDL_RenderFillRect(_renderer, ref rectangle), "draw rectangle");
    }

    public void SetTitle(string title)
    {
        EnsureOpen();
        SDL.SDL_SetWindowTitle(_handle, title);
    }

    public void SetSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Window size must be positive.");

        EnsureOpen();
        SDL.SDL_SetWindowSize(_handle, width, height);
        _width = width;
        _height = height;
    }

    public void SetPosition(int x, int y)
    {
        EnsureOpen();
        SDL.SDL_SetWindowPosition(_handle, x, y);
    }

    public void CenterOnScreen()
    {
        EnsureOpen();
        SDL.SDL_SetWindowPosition(_handle, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);
    }

    public void SetFullscreen(bool fullscreen)
    {
        EnsureOpen();

        var flags = fullscreen
            ? SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP
            : 0;

        CheckSdlResult(SDL.SDL_SetWindowFullscreen(_handle, (uint)flags), "toggle fullscreen");
    }

    public void Present()
    {
        EnsureOpen();
        SDL.SDL_RenderPresent(_renderer);
    }

    public void Close()
    {
        _isOpen = false;

        if (_renderer != IntPtr.Zero)
        {
            SDL.SDL_DestroyRenderer(_renderer);
            _renderer = IntPtr.Zero;
        }

        if (_handle != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(_handle);
            _handle = IntPtr.Zero;
        }

        if (_sdlInitialized)
        {
            SDL.SDL_Quit();
            _sdlInitialized = false;
        }
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private void Initialize(string title, int width, int height)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            throw new InvalidOperationException($"Failed to initialize SDL2: {SDL.SDL_GetError()}");

        _sdlInitialized = true;

        try
        {
            _handle = SDL.SDL_CreateWindow(
                title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                width,
                height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create SDL2 window: {SDL.SDL_GetError()}");

            _renderer = SDL.SDL_CreateRenderer(
                _handle,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (_renderer == IntPtr.Zero)
            {
                _renderer = SDL.SDL_CreateRenderer(
                    _handle,
                    -1,
                    SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
            }

            if (_renderer == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create SDL2 renderer: {SDL.SDL_GetError()}");

            Renderer = new Renderer(_handle, _renderer);
            _isOpen = true;
            Input.SetFocus(true);
        }
        catch
        {
            Close();
            throw;
        }
    }

    private void EnsureOpen()
    {
        ObjectDisposedException.ThrowIf(!_isOpen, this);
    }

    private static void CheckSdlResult(int result, string operation)
    {
        if (result != 0)
            throw new InvalidOperationException($"Failed to {operation}: {SDL.SDL_GetError()}");
    }
}
