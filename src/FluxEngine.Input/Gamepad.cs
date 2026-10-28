namespace FluxEngine.Input;

public enum GamepadButton
{
    A,
    B,
    X,
    Y,
    Back,
    Guide,
    Start,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,
    Misc1,
    Misc2,
    Misc3,
    Misc4,
    Misc5,
    Touchpad,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
}

public enum GamepadType
{
    Unknown,
    Xbox,
    PlayStation,
    Nintendo,
    Generic,
}

public enum GamepadAxis
{
    LeftX,
    LeftY,
    RightX,
    RightY,
    LeftTrigger,
    RightTrigger,
}

public sealed class GamepadDevice
{
    private readonly HashSet<GamepadButton> _heldButtons = [];
    private readonly HashSet<GamepadButton> _pressedButtons = [];
    private readonly HashSet<GamepadButton> _releasedButtons = [];
    private readonly Dictionary<GamepadAxis, float> _axes = [];

    internal GamepadDevice(int instanceId, string name, GamepadType type, string mapping)
    {
        InstanceId = instanceId;
        Name = name;
        Type = type;
        Mapping = mapping;
    }

    public int InstanceId { get; }
    public string Name { get; internal set; }
    public GamepadType Type { get; internal set; }
    public string Mapping { get; internal set; }
    public bool IsPlayStation => Type == GamepadType.PlayStation;
    public bool IsXbox => Type == GamepadType.Xbox;
    public bool IsNintendo => Type == GamepadType.Nintendo;

    public bool IsButtonDown(GamepadButton button) => _heldButtons.Contains(button);
    public bool WasButtonPressed(GamepadButton button) => _pressedButtons.Contains(button);
    public bool WasButtonReleased(GamepadButton button) => _releasedButtons.Contains(button);

    public float GetAxis(GamepadAxis axis) => _axes.GetValueOrDefault(axis);

    internal void UpdateMetadata(string name, GamepadType type, string mapping)
    {
        Name = name;
        Type = type;
        Mapping = mapping;
    }

    internal void SetButton(GamepadButton button, bool isDown)
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

    internal void SetAxis(GamepadAxis axis, float value)
    {
        _axes[axis] = float.IsFinite(value) ? Math.Clamp(value, -1f, 1f) : 0f;
    }

    internal void BeginFrame()
    {
        _pressedButtons.Clear();
        _releasedButtons.Clear();
    }

    internal void Reset()
    {
        _heldButtons.Clear();
        _pressedButtons.Clear();
        _releasedButtons.Clear();
        _axes.Clear();
    }
}
