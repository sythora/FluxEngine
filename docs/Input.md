# Input System

This is the inputs part of FluxEngine. It is made to keep game code away from raw SDL events. The engine should ask `InputManager` for state, not read platform event structs directly.

## InputManager

`InputManager` tracks keyboard state, mouse state, scrolling, mouse position, focus, and inside-window status.

It keeps a few different kinds of state:

- held keys/buttons: currently down right now
- pressed this frame: just became down in this frame
- released this frame: just became up in this frame

This is important because game code usually wants to know 3 things:

- is this key held down?
- was this key pressed this frame?
- was this key released this frame?

Same for mouse buttons.

### Basic behavior

`BeginFrame()` clears the per-frame temp state. This makes it safe to process SDL events and then let the game read input during the same frame.

Example flow:

1. SDL event queue gets polled
2. SDL event is translated to `SetKey`, `SetMouseButton`, `SetMousePosition`, etc.
3. game logic calls `IsKeyDown`, `WasKeyPressed`, `WasMouseButtonReleased`, etc.
4. new frame starts and temp states get reset

### Mouse

Mouse state includes:

- current position (`MouseX`, `MouseY`)
- delta movement (`MouseDeltaX`, `MouseDeltaY`)
- wheel scroll (`ScrollX`, `ScrollY`)
- whether mouse is inside the window (`IsMouseInside`)

Delta is based on movement since last frame. Scroll accumulates per frame.

### Focus

`HasFocus` tells if the window has focus. If focus is lost, input is cleared so stuck keys/buttons don't stay active.

## Input Actions

Raw keys work fine but hardcoding `Key.W` in ten places is pain when you add
controller support or a rebind menu. `InputManager.Actions` gives names to
inputs instead:

```csharp
var actions = engine.Input.Actions;

actions.Bind("jump", Key.Space, Key.W);
actions.Bind("move.left", Key.A, Key.Left);
actions.Bind("move.right", Key.D, Key.Right);
actions.BindButton("shoot", MouseButton.Left);
```

Then game code asks for the action, not the key:

```csharp
if (actions.WasPressed("jump"))
    Jump();

var move = actions.Axis("move.left", "move.right"); // -1, 0 or 1
```

You can bind more than one key to the same action. `IsDown` checks held keys
and mouse buttons, `WasPressed` and `WasReleased` only fire on the frame the
thing changed. `Unbind(name)` removes one action, `Clear()` removes all.

Bindings live on the input manager, so bind them once before `engine.Run`.

## Testing

Run the input test from the repo folder:

```text
python tests/input_test.py
```

The Python test builds FluxEngine, loads the built `FluxEngine.dll` with Python.NET, and calls the actual `InputManager` methods from Python. It does not copy the input logic into a fake Python version or start another test program. It checks key and mouse press/release states, frame clearing, mouse delta and scroll accumulation, and focus reset.

You can also pick the build config:

```text
python tests/input_test.py --configuration Release
```

You need Python, the .NET SDK/runtime, and Python.NET. Install Python.NET with:

```text
python -m pip install pythonnet
```

`ctypes` cannot call a regular managed .NET DLL directly; Python.NET hosts .NET and lets Python call the managed types.

## SDL platform side

The platform window polls SDL events. `SdlInputAdapter` maps supported SDL keys and buttons into the engine enums, then updates `InputManager`. Unknown keys/buttons are skipped. Game code does not need to use SDL event types itself.
