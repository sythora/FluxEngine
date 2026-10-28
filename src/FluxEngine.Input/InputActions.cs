namespace FluxEngine.Input;

// named bindings so game code says "jump" instead of listing five keys
public sealed class InputActions
{
    private readonly InputManager _input;
    private readonly Dictionary<string, List<Key>> _keys = [];
    private readonly Dictionary<string, List<MouseButton>> _buttons = [];
    private readonly Dictionary<string, List<GamepadButton>> _gamepadButtons = [];
    private readonly Dictionary<string, List<(GamepadAxis Axis, float Direction, float Deadzone)>> _gamepadAxes = [];

    internal InputActions(InputManager input) => _input = input;

    public InputActions Bind(string action, params Key[] keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_keys.TryGetValue(action, out var bound))
            _keys[action] = bound = [];

        bound.AddRange(keys);
        return this;
    }

    public InputActions BindGamepadButton(string action, params GamepadButton[] buttons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_gamepadButtons.TryGetValue(action, out var bound))
            _gamepadButtons[action] = bound = [];

        bound.AddRange(buttons);
        return this;
    }

    public InputActions BindGamepadAxis(string action, GamepadAxis axis, float direction = 1f, float deadzone = 0.2f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (direction is not (-1f or 1f))
            throw new ArgumentOutOfRangeException(nameof(direction), "Axis direction must be -1 or 1.");
        if (deadzone is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(deadzone), "Axis deadzone must be in [0, 1).");

        if (!_gamepadAxes.TryGetValue(action, out var bound))
            _gamepadAxes[action] = bound = [];

        bound.Add((axis, direction, deadzone));
        return this;
    }

    public InputActions BindButton(string action, params MouseButton[] buttons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_buttons.TryGetValue(action, out var bound))
            _buttons[action] = bound = [];

        bound.AddRange(buttons);
        return this;
    }

    public void Unbind(string action)
    {
        _keys.Remove(action);
        _buttons.Remove(action);
        _gamepadButtons.Remove(action);
        _gamepadAxes.Remove(action);
    }

    public void Clear()
    {
        _keys.Clear();
        _buttons.Clear();
        _gamepadButtons.Clear();
        _gamepadAxes.Clear();
    }

    public bool IsDown(string action)
    {
        if (_keys.TryGetValue(action, out var keys))
        {
            foreach (var key in keys)
            {
                if (_input.IsKeyDown(key))
                    return true;
            }
        }

        if (_buttons.TryGetValue(action, out var buttons))
        {
            foreach (var button in buttons)
            {
                if (_input.IsMouseButtonDown(button))
                    return true;
            }
        }

        if (_gamepadButtons.TryGetValue(action, out var gamepadButtons))
        {
            foreach (var gamepad in _input.Gamepads)
            foreach (var button in gamepadButtons)
                if (gamepad.IsButtonDown(button))
                    return true;
        }

        if (_gamepadAxes.TryGetValue(action, out var axes))
        {
            foreach (var gamepad in _input.Gamepads)
            foreach (var (axis, direction, deadzone) in axes)
                if (gamepad.GetAxis(axis) * direction > deadzone)
                    return true;
        }

        return false;
    }

    public bool WasPressed(string action)
    {
        if (_keys.TryGetValue(action, out var keys))
        {
            foreach (var key in keys)
            {
                if (_input.WasKeyPressed(key))
                    return true;
            }
        }

        if (_buttons.TryGetValue(action, out var buttons))
        {
            foreach (var button in buttons)
            {
                if (_input.WasMouseButtonPressed(button))
                    return true;
            }
        }

        if (_gamepadButtons.TryGetValue(action, out var gamepadButtons))
        {
            foreach (var gamepad in _input.Gamepads)
            foreach (var button in gamepadButtons)
                if (gamepad.WasButtonPressed(button))
                    return true;
        }

        return false;
    }

    public bool WasReleased(string action)
    {
        if (_keys.TryGetValue(action, out var keys))
        {
            foreach (var key in keys)
            {
                if (_input.WasKeyReleased(key))
                    return true;
            }
        }

        if (_buttons.TryGetValue(action, out var buttons))
        {
            foreach (var button in buttons)
            {
                if (_input.WasMouseButtonReleased(button))
                    return true;
            }
        }

        if (_gamepadButtons.TryGetValue(action, out var gamepadButtons))
        {
            foreach (var gamepad in _input.Gamepads)
            foreach (var button in gamepadButtons)
                if (gamepad.WasButtonReleased(button))
                    return true;
        }

        return false;
    }

    // -1 when only the negative is held, 1 when only the positive is, 0 otherwise
    public float Axis(string negative, string positive)
    {
        var value = (IsDown(positive) ? 1f : 0f) - (IsDown(negative) ? 1f : 0f);

        foreach (var gamepad in _input.Gamepads)
        {
            if (_gamepadAxes.TryGetValue(positive, out var positiveAxes))
                foreach (var (axis, direction, deadzone) in positiveAxes)
                {
                    var axisValue = gamepad.GetAxis(axis) * direction;
                    if (axisValue > deadzone)
                        value = axisValue;
                }

            if (_gamepadAxes.TryGetValue(negative, out var negativeAxes))
                foreach (var (axis, direction, deadzone) in negativeAxes)
                {
                    var axisValue = gamepad.GetAxis(axis) * direction;
                    if (axisValue > deadzone)
                        value = -axisValue;
                }
        }

        return Math.Clamp(value, -1f, 1f);
    }
}
