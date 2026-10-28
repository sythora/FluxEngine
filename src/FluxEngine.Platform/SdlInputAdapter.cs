using SDL2;
using FluxEngine.Input;

namespace FluxEngine.Platform;

internal static class SdlInputAdapter
{
    public static void ProcessEvent(SDL.SDL_Event evt, InputManager input)
    {
        switch (evt.type)
        {
            case SDL.SDL_EventType.SDL_KEYDOWN:
                ProcessKeyboardEvent(evt.key, input, isDown: true);
                break;
            case SDL.SDL_EventType.SDL_KEYUP:
                ProcessKeyboardEvent(evt.key, input, isDown: false);
                break;
            case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
            case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                ProcessGamepadButtonEvent(evt.cbutton, input);
                break;
            case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                ProcessGamepadAxisEvent(evt.caxis, input);
                break;
            case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                break; // Window owns opening and lifetime of controller handles.
            case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                input.DisconnectGamepad(evt.cdevice.which);
                break;
            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                input.SetMousePosition(evt.motion.x, evt.motion.y);
                break;
            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                if (TryMapMouseButton(evt.button.button, out var downButton))
                    input.SetMouseButton(downButton, true);
                break;
            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                if (TryMapMouseButton(evt.button.button, out var upButton))
                    input.SetMouseButton(upButton, false);
                break;
            case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                input.AddMouseScroll(evt.wheel.x, evt.wheel.y);
                break;
            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                ProcessWindowEvent(evt.window, input);
                break;
            case SDL.SDL_EventType.SDL_QUIT:
                input.SetFocus(false);
                break;
        }
    }

    private static void ProcessGamepadButtonEvent(SDL.SDL_ControllerButtonEvent buttonEvent, InputManager input)
    {
        var raw = (int)buttonEvent.button;
        // SDL2-CS exposes the SDL 2.0.14 controller-button enum (through
        // D-pad buttons), while newer SDL2 runtimes also emit the extended
        // SDL 2.0.coming controller button values for PS share/touchpad.
        var button = raw switch
        {
            0 => GamepadButton.A,
            1 => GamepadButton.B,
            2 => GamepadButton.X,
            3 => GamepadButton.Y,
            4 => GamepadButton.Back,
            5 => GamepadButton.Guide,
            6 => GamepadButton.Start,
            7 => GamepadButton.LeftStick,
            8 => GamepadButton.RightStick,
            9 => GamepadButton.LeftShoulder,
            10 => GamepadButton.RightShoulder,
            11 => GamepadButton.DPadUp,
            12 => GamepadButton.DPadDown,
            13 => GamepadButton.DPadLeft,
            14 => GamepadButton.DPadRight,
            15 => GamepadButton.Misc1,
            16 => GamepadButton.Misc2,
            17 => GamepadButton.Misc3,
            18 => GamepadButton.Misc4,
            19 => GamepadButton.Misc5,
            20 => GamepadButton.Touchpad,
            _ => (GamepadButton?)null,
        };
        if (button.HasValue)
            input.SetGamepadButton(buttonEvent.which, button.Value, buttonEvent.state != 0);
    }

    private static void ProcessGamepadAxisEvent(SDL.SDL_ControllerAxisEvent axisEvent, InputManager input)
    {
        if (axisEvent.axis >= (byte)SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_MAX)
            return;

        var axis = (GamepadAxis)axisEvent.axis;
        var value = axis == GamepadAxis.LeftTrigger || axis == GamepadAxis.RightTrigger
            ? Math.Max(0, (axisEvent.axisValue + 32768f) / 65535f)
            : axisEvent.axisValue < 0
                ? axisEvent.axisValue / 32768f
                : axisEvent.axisValue / 32767f;
        input.SetGamepadAxis(axisEvent.which, axis, value);
    }

    private static void ProcessKeyboardEvent(SDL.SDL_KeyboardEvent keyboardEvent, InputManager input, bool isDown)
    {
        if (!TryMapKey(keyboardEvent.keysym.scancode, out var key))
            return;

        if (isDown && keyboardEvent.repeat != 0 && input.IsKeyDown(key))
            return;

        input.SetKey(key, isDown);
    }

    private static void ProcessWindowEvent(SDL.SDL_WindowEvent windowEvent, InputManager input)
    {
        switch (windowEvent.windowEvent)
        {
            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                input.SetFocus(true);
                break;
            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                input.SetFocus(false);
                break;
            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                input.SetMouseInside(true);
                break;
            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                input.SetMouseInside(false);
                break;
            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                input.SetFocus(false);
                break;
        }
    }

    private static bool TryMapKey(SDL.SDL_Scancode scancode, out Key key)
    {
        switch (scancode)
        {
            case SDL.SDL_Scancode.SDL_SCANCODE_A: key = Key.A; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_B: key = Key.B; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_C: key = Key.C; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_D: key = Key.D; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_E: key = Key.E; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F: key = Key.F; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_G: key = Key.G; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_H: key = Key.H; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_I: key = Key.I; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_J: key = Key.J; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_K: key = Key.K; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_L: key = Key.L; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_M: key = Key.M; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_N: key = Key.N; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_O: key = Key.O; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_P: key = Key.P; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_Q: key = Key.Q; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_R: key = Key.R; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_S: key = Key.S; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_T: key = Key.T; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_U: key = Key.U; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_V: key = Key.V; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_W: key = Key.W; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_X: key = Key.X; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_Y: key = Key.Y; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_Z: key = Key.Z; return true;

            case SDL.SDL_Scancode.SDL_SCANCODE_0: key = Key.D0; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_1: key = Key.D1; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_2: key = Key.D2; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_3: key = Key.D3; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_4: key = Key.D4; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_5: key = Key.D5; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_6: key = Key.D6; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_7: key = Key.D7; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_8: key = Key.D8; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_9: key = Key.D9; return true;

            case SDL.SDL_Scancode.SDL_SCANCODE_SPACE: key = Key.Space; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_RETURN: key = Key.Enter; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE: key = Key.Escape; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_TAB: key = Key.Tab; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE: key = Key.Backspace; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_DELETE: key = Key.Delete; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_INSERT: key = Key.Insert; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_HOME: key = Key.Home; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_END: key = Key.End; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP: key = Key.PageUp; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN: key = Key.PageDown; return true;

            case SDL.SDL_Scancode.SDL_SCANCODE_LEFT: key = Key.Left; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT: key = Key.Right; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_UP: key = Key.Up; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_DOWN: key = Key.Down; return true;

            case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT: key = Key.LeftShift; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT: key = Key.RightShift; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL: key = Key.LeftControl; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL: key = Key.RightControl; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_LALT: key = Key.LeftAlt; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_RALT: key = Key.RightAlt; return true;

            case SDL.SDL_Scancode.SDL_SCANCODE_F1: key = Key.F1; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F2: key = Key.F2; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F3: key = Key.F3; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F4: key = Key.F4; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F5: key = Key.F5; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F6: key = Key.F6; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F7: key = Key.F7; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F8: key = Key.F8; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F9: key = Key.F9; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F10: key = Key.F10; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F11: key = Key.F11; return true;
            case SDL.SDL_Scancode.SDL_SCANCODE_F12: key = Key.F12; return true;

            default:
                key = Key.Unknown;
                return false;
        }
    }

    private static bool TryMapMouseButton(uint button, out MouseButton mouseButton)
    {
        switch (button)
        {
            case SDL.SDL_BUTTON_LEFT:
                mouseButton = MouseButton.Left;
                return true;
            case SDL.SDL_BUTTON_RIGHT:
                mouseButton = MouseButton.Right;
                return true;
            case SDL.SDL_BUTTON_MIDDLE:
                mouseButton = MouseButton.Middle;
                return true;
            case SDL.SDL_BUTTON_X1:
                mouseButton = MouseButton.X1;
                return true;
            case SDL.SDL_BUTTON_X2:
                mouseButton = MouseButton.X2;
                return true;
            default:
                mouseButton = default;
                return false;
        }
    }
}
