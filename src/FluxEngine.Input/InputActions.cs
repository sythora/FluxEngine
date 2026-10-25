namespace FluxEngine.Input;

// named bindings so game code says "jump" instead of listing five keys
public sealed class InputActions
{
    private readonly InputManager _input;
    private readonly Dictionary<string, List<Key>> _keys = [];
    private readonly Dictionary<string, List<MouseButton>> _buttons = [];

    internal InputActions(InputManager input) => _input = input;

    public InputActions Bind(string action, params Key[] keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_keys.TryGetValue(action, out var bound))
            _keys[action] = bound = [];

        bound.AddRange(keys);
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
    }

    public void Clear()
    {
        _keys.Clear();
        _buttons.Clear();
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

        return false;
    }

    // -1 when only the negative is held, 1 when only the positive is, 0 otherwise
    public float Axis(string negative, string positive)
        => (IsDown(positive) ? 1 : 0) - (IsDown(negative) ? 1 : 0);
}
