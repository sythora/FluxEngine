# Getting Started

Make a window, spawn some stuff, run it.

```csharp
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;

using var engine = new Engine("my game", 960, 640);

engine.Actions.Bind("jump", Key.Space, Key.W);

var scene = new Scene();
scene.BackgroundColor = Color.CornflowerBlue;

var player = scene.Spawn("player", new Vector2(480, 320));
var sprite = player.AddComponent<SpriteRenderer>();
sprite.Size = new Vector2(32, 48);
sprite.Color = Color.Yellow;

var body = player.AddComponent<Rigidbody2D>();
body.Size = sprite.Size;
body.Gravity = new Vector2(0, 1500);

scene.Solids.Add(new BoxCollider(new Vector2(480, 520), new Vector2(960, 40)));

engine.Run(scene);
```

The scene loop handles fixed updates, variable updates, drawing, and presenting
the window. You don't have to write a loop yourself unless you want to.

## The old school way

If you don't want scenes, just pass update and render callbacks:

```csharp
using var engine = new Engine("my game", 960, 640);
var x = 100f;

engine.Run(
    deltaTime =>
    {
        if (engine.Input.IsKeyDown(Key.D)) x += 200 * deltaTime;
    },
    () =>
    {
        engine.Window.Clear(18, 22, 32);
        engine.Window.FillRectangle((int)x, 200, 40, 40, 255, 100, 100);
    });
```

## Build and run

```text
dotnet build FluxEngine.slnx
dotnet run --project demos/FluxEngine.Demo
```

SDL2 has to be findable as `SDL2.dll`. On Windows put it next to the exe or
somewhere on PATH. If the window never opens, that's almost always why.

## FluxRunner demo controls

- A/D or Left/Right - move
- Space/Up or gamepad A - jump and double jump
- Escape or gamepad Start - pause/resume
- O or gamepad Y - open settings from the start/pause screen
- R - restart after game over
- F3 - toggle the debug overlay

The settings screen can save graphics, audio, VSync, and frame-rate options to
`settings.json`. See [Settings.md](Settings.md).
