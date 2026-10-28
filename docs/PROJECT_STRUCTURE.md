# Project Structure

```text
FluxEngine.slnx
Directory.Build.props
src/
  FluxEngine.csproj         The engine project
  FluxEngine.Core/
    Engine.cs               Main loop, scene loop, audio and asset access
    Scene.cs                Entity list, solids, camera, update and draw order
    Entity.cs               Entity and Component base class
    Time.cs                 Delta time, total time, fps, time scale
    GameTimer.cs            Countdown timer with repeating mode
    Log.cs                  Console logger with levels
    ObjectPool.cs           Reuse dead objects instead of making new ones
    StateMachine.cs         States with enter and exit callbacks
  FluxEngine.Mathematics/
    Vector2.cs              Vector type with the usual math helpers
    Rectangle.cs            Axis aligned rectangle, top left based
    Color.cs                Byte color with named colors and hex parsing
    MathUtil.cs             Clamp, lerp, remap, smoothing and friends
    Rng.cs                  Random numbers with game friendly helpers
  FluxEngine.Graphics/
    Renderer.cs             SDL draw calls: shapes, sprites, textures
    Sprite.cs               Texture plus an optional source frame
    SpriteRenderer.cs       Component that draws a sprite or a colored box
    Texture.cs              Wraps an SDL texture, owns the dispose
    Camera.cs               Position, zoom and screen shake
  FluxEngine.Platform/
    Window.cs               SDL window, renderer, events, fullscreen
    SdlInputAdapter.cs      Turns SDL events into engine input state
  FluxEngine.Input/
    InputManager.cs         Platform independent keyboard and mouse state
    InputActions.cs         Named bindings like "jump" and "move.left"
    Key.cs                  Key names instead of SDL scancodes
    MouseButton.cs          Mouse button names
  FluxEngine.Physics/
    Collider.cs             Box and circle collider structs
    Collision.cs            Overlap checks and push out math
    Rigidbody2D.cs          Gravity, drag, bounce, lands on scene solids
  FluxEngine.Audio/
    AudioManager.cs         Wav loading and a simple float mixer
    Sound.cs                Decoded audio data
  FluxEngine.Content/
    AssetManager.cs         Texture cache by full path
demos/
  FluxEngine.Demo/          Sandbox demo: player, balls, physics, camera shake
tests/                      Automated tests
docs/                       Project documentation
build/                      Build output, all projects: build/<project>/, tests: build/test/<project>/
```

The folders under `src/` are organizational only, not separate projects. The
demo is a separate runnable project and is not compiled into the engine.
Run it with `dotnet run --project demos/FluxEngine.Demo`.

Put test files in `tests/` and documentation in `docs/`.
