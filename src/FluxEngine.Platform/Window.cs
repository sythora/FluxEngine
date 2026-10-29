using System.Runtime.InteropServices;
using SDL2;
using FluxEngine.Graphics;
using FluxEngine.Input;

namespace FluxEngine.Platform;

public sealed class Window : IDisposable
{
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_GameControllerGetType(IntPtr gamecontroller);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern ushort SDL_JoystickGetVendor(IntPtr joystick);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern ushort SDL_JoystickGetProduct(IntPtr joystick);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_GameControllerRumble(IntPtr gamecontroller, ushort low_frequency_rumble, ushort high_frequency_rumble, uint duration_ms);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_JoystickRumble(IntPtr joystick, ushort low_frequency_rumble, ushort high_frequency_rumble, uint duration_ms);

    private IntPtr _handle;
    private IntPtr _renderer;
    private bool _isOpen;
    private bool _sdlInitialized;
    private readonly Dictionary<int, IntPtr> _gamepadHandles = [];
    private readonly Dictionary<int, IntPtr> _hapticHandles = [];
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

            if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED)
                TryOpenGamepad(evt.cdevice.which);
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
                CloseGamepad(evt.cdevice.which);

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

        SyncGamepads();
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

        Renderer?.Dispose();

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

        foreach (var haptic in _hapticHandles.Values)
            SDL.SDL_HapticClose(haptic);
        _hapticHandles.Clear();
        foreach (var controller in _gamepadHandles.Values)
            SDL.SDL_GameControllerClose(controller);
        _gamepadHandles.Clear();

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
        SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_RAWINPUT", "0");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS4", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS4_RUMBLE", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS5", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS5_RUMBLE", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_XBOX", "1");
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_HAPTIC) != 0)
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
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC |
                SDL.SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE);

            if (_renderer == IntPtr.Zero)
            {
                _renderer = SDL.SDL_CreateRenderer(
                    _handle,
                    -1,
                    SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                    SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            }

            if (_renderer == IntPtr.Zero)
            {
                _renderer = SDL.SDL_CreateRenderer(
                    _handle,
                    -1,
                    SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE |
                    SDL.SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE);
            }

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
            SDL.SDL_GameControllerEventState(SDL.SDL_ENABLE);
            for (var deviceIndex = 0; deviceIndex < SDL.SDL_NumJoysticks(); deviceIndex++)
                TryOpenGamepad(deviceIndex);
        }
        catch
        {
            Close();
            throw;
        }
    }

    private void TryOpenGamepad(int deviceIndex)
    {
        if (SDL.SDL_IsGameController(deviceIndex) != SDL.SDL_bool.SDL_TRUE)
            return;

        var controller = SDL.SDL_GameControllerOpen(deviceIndex);
        if (controller == IntPtr.Zero)
            return;

        var joystick = SDL.SDL_GameControllerGetJoystick(controller);
        var instanceId = SDL.SDL_JoystickInstanceID(joystick);
        if (instanceId < 0 || _gamepadHandles.ContainsKey(instanceId))
            return;

        _gamepadHandles[instanceId] = controller;
        var haptic = SDL.SDL_HapticOpenFromJoystick(joystick);
        if (haptic != IntPtr.Zero && SDL.SDL_HapticRumbleInit(haptic) == 0)
            _hapticHandles[instanceId] = haptic;
        else if (haptic != IntPtr.Zero)
            SDL.SDL_HapticClose(haptic);
        var name = SDL.SDL_GameControllerName(controller);
        name = string.IsNullOrWhiteSpace(name) ? $"Gamepad {instanceId}" : name;
        var mapping = SDL.SDL_GameControllerMapping(controller) ?? string.Empty;
        var guid = SDL.SDL_JoystickGetGUID(joystick).ToString("N");
        var type = DetectGamepadType(controller, joystick, name, mapping, guid);
        Input.ConnectGamepad(instanceId, name, type, mapping);
    }

    public bool PlayControllerRumble(int instanceId, float strength = 1.0f, int durationMilliseconds = 500)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        if (strength <= 0)
            return false;

        var ms = (uint)Math.Max(50, durationMilliseconds);
        var raw = (ushort)(strength * 65535f);

        if (_gamepadHandles.TryGetValue(instanceId, out var controller) && controller != IntPtr.Zero)
        {
            if (SDL_GameControllerRumble(controller, raw, raw, ms) == 0)
                return true;

            var joystick = SDL.SDL_GameControllerGetJoystick(controller);
            if (joystick != IntPtr.Zero && SDL_JoystickRumble(joystick, raw, raw, ms) == 0)
                return true;
        }

        if (_hapticHandles.TryGetValue(instanceId, out var haptic) && haptic != IntPtr.Zero)
            return SDL.SDL_HapticRumblePlay(haptic, strength, ms) == 0;

        return false;
    }

    private static GamepadType DetectGamepadType(IntPtr controller, IntPtr joystick, string name, string mapping, string guid)
    {
        if (controller != IntPtr.Zero)
        {
            var sdlType = SDL_GameControllerGetType(controller);
            switch (sdlType)
            {
                case 1:
                case 2:
                    return GamepadType.Xbox;
                case 3:
                case 4:
                case 5:
                    return GamepadType.PlayStation;
                case 6:
                case 11:
                case 12:
                case 13:
                    return GamepadType.Nintendo;
            }
        }

        if (joystick != IntPtr.Zero)
        {
            var vendorId = SDL_JoystickGetVendor(joystick);
            if (vendorId == 0x054C)
                return GamepadType.PlayStation;
            if (vendorId == 0x045E)
                return GamepadType.Xbox;
            if (vendorId == 0x057E)
                return GamepadType.Nintendo;
        }

        return DetectGamepadType(name, mapping, guid);
    }

    public static GamepadType DetectGamepadType(string name, string mapping, string guid)
    {
        var identity = $"{name} {mapping} {guid}".ToLowerInvariant();
        if (identity.Contains("playstation") ||
            identity.Contains("dualshock") ||
            identity.Contains("dualsense") ||
            identity.Contains("sony") ||
            identity.Contains("ps5") ||
            identity.Contains("ps4") ||
            identity.Contains("ps3") ||
            identity.Contains("054c") ||
            identity.Contains("4c05"))
            return GamepadType.PlayStation;

        if (identity.Contains("xbox") ||
            identity.Contains("xinput") ||
            identity.Contains("x-box") ||
            identity.Contains("microsoft") ||
            identity.Contains("045e") ||
            identity.Contains("5e04"))
            return GamepadType.Xbox;

        if (identity.Contains("nintendo") ||
            identity.Contains("switch") ||
            identity.Contains("joy-con") ||
            identity.Contains("joycon") ||
            identity.Contains("057e") ||
            identity.Contains("7e05"))
            return GamepadType.Nintendo;

        return string.IsNullOrWhiteSpace(mapping) ? GamepadType.Generic : GamepadType.Xbox;
    }

    private void SyncGamepads()
    {
        foreach (var (instanceId, controller) in _gamepadHandles)
        {
            if (controller == IntPtr.Zero)
                continue;

            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, GamepadButton.A);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, GamepadButton.B);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, GamepadButton.X);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, GamepadButton.Y);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, GamepadButton.Back);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE, GamepadButton.Guide);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, GamepadButton.Start);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK, GamepadButton.LeftStick);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK, GamepadButton.RightStick);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, GamepadButton.LeftShoulder);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, GamepadButton.RightShoulder);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, GamepadButton.DPadUp);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, GamepadButton.DPadDown);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, GamepadButton.DPadLeft);
            SyncButton(instanceId, controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, GamepadButton.DPadRight);
            SyncButton(instanceId, controller, (SDL.SDL_GameControllerButton)15, GamepadButton.Misc1);
            SyncButton(instanceId, controller, (SDL.SDL_GameControllerButton)20, GamepadButton.Touchpad);

            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, GamepadAxis.LeftX, false);
            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, GamepadAxis.LeftY, false);
            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, GamepadAxis.RightX, false);
            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, GamepadAxis.RightY, false);
            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, GamepadAxis.LeftTrigger, true);
            SyncAxis(instanceId, controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, GamepadAxis.RightTrigger, true);
        }
    }

    private void SyncButton(int instanceId, IntPtr controller, SDL.SDL_GameControllerButton sdlButton, GamepadButton button)
    {
        var isDown = SDL.SDL_GameControllerGetButton(controller, sdlButton) != 0;
        Input.SetGamepadButton(instanceId, button, isDown);
    }

    private void SyncAxis(int instanceId, IntPtr controller, SDL.SDL_GameControllerAxis sdlAxis, GamepadAxis axis, bool isTrigger)
    {
        var raw = SDL.SDL_GameControllerGetAxis(controller, sdlAxis);
        var val = isTrigger
            ? Math.Clamp(raw / 32767f, 0f, 1f)
            : raw < 0 ? raw / 32768f : raw / 32767f;
        Input.SetGamepadAxis(instanceId, axis, val);
    }

    private void CloseGamepad(int instanceId)
    {
        if (_hapticHandles.Remove(instanceId, out var haptic))
            SDL.SDL_HapticClose(haptic);
        if (_gamepadHandles.Remove(instanceId, out var controller))
            SDL.SDL_GameControllerClose(controller);
        Input.DisconnectGamepad(instanceId);
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
