using FluxEngine.Mathematics;

namespace FluxEngine.Input;

public sealed class InputManager
{
    private InputActions? _actions;

    // named bindings, sits on top of the raw key checks below
    public InputActions Actions => _actions ??= new InputActions(this);

    private readonly HashSet<Key> _heldKeys = [];
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<Key> _releasedKeys = [];

    private readonly HashSet<MouseButton> _heldButtons = [];
    private readonly HashSet<MouseButton> _pressedButtons = [];
    private readonly HashSet<MouseButton> _releasedButtons = [];

    private bool _hasMousePosition;

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }

    public float ScrollX { get; private set; }
    public float ScrollY { get; private set; }

    public Vector2 MousePosition => new(MouseX, MouseY);

    public bool HasFocus { get; private set; } = true;
    public bool IsMouseInside { get; private set; }

    public bool AnyKeyDown => _heldKeys.Count > 0;
    public bool AnyKeyPressed => _pressedKeys.Count > 0;
    public bool AnyMouseButtonDown => _heldButtons.Count > 0;
    public bool AnyMouseButtonPressed => _pressedButtons.Count > 0;

    public bool IsKeyDown(Key key) => _heldKeys.Contains(key);
    public bool WasKeyPressed(Key key) => _pressedKeys.Contains(key);
    public bool WasKeyReleased(Key key) => _releasedKeys.Contains(key);

    public bool IsMouseButtonDown(MouseButton button) =>
        _heldButtons.Contains(button);

    public bool WasMouseButtonPressed(MouseButton button) =>
        _pressedButtons.Contains(button);

    public bool WasMouseButtonReleased(MouseButton button) =>
        _releasedButtons.Contains(button);

    // Clear temporary input states at the start of each frame.
    public void BeginFrame()
    {
        _pressedKeys.Clear();
        _releasedKeys.Clear();
        _pressedButtons.Clear();
        _releasedButtons.Clear();

        MouseDeltaX = 0;
        MouseDeltaY = 0;
        ScrollX = 0;
        ScrollY = 0;
    }

    public void SetKey(Key key, bool isDown)
    {
        if (key == Key.Unknown)
            return;

        if (isDown)
        {
            if (_heldKeys.Add(key))
                _pressedKeys.Add(key);
        }
        else if (_heldKeys.Remove(key))
        {
            _releasedKeys.Add(key);
        }
    }

    public void SetMouseButton(MouseButton button, bool isDown)
    {
        if (isDown)
        {
            if (_heldButtons.Add(button))
                _pressedButtons.Add(button);
        }
        else if (_heldButtons.Remove(button))
        {
            _releasedButtons.Add(button);
        }
    }

    public void SetMousePosition(int x, int y)
    {
        if (_hasMousePosition)
        {
            MouseDeltaX += x - MouseX;
            MouseDeltaY += y - MouseY;
        }

        MouseX = x;
        MouseY = y;
        _hasMousePosition = true;
    }

    public void AddMouseScroll(float x, float y)
    {
        ScrollX += x;
        ScrollY += y;
    }

    public void SetMouseInside(bool isInside)
    {
        IsMouseInside = isInside;
    }

    public void SetFocus(bool hasFocus)
    {
        HasFocus = hasFocus;

        if (!hasFocus)
            Reset();
    }

    // Clear all tracked input when focus is lost or the engine resets.
    public void Reset()
    {
        _heldKeys.Clear();
        _pressedKeys.Clear();
        _releasedKeys.Clear();

        _heldButtons.Clear();
        _pressedButtons.Clear();
        _releasedButtons.Clear();

        MouseDeltaX = 0;
        MouseDeltaY = 0;
        ScrollX = 0;
        ScrollY = 0;
        _hasMousePosition = false;
    }
}
