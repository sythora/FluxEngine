# Project Structure

```text
FluxEngine.slnx
Directory.Build.props
src/
  FluxEngine.csproj       The engine project
  FluxEngine.Core/
    Engine.cs             Engine loop
    Scene.cs              Scene placeholder
  FluxEngine.Mathematics/
    Vector2.cs            Vector type placeholder
  FluxEngine.Graphics/
    Renderer.cs           Renderer placeholder
    Sprite.cs             Sprite placeholder
  FluxEngine.Platform/
    Window.cs             SDL window and simple rectangle drawing
  FluxEngine.Input/
    InputManager.cs       Platform-independent input state
  FluxEngine.Physics/
    Collider.cs           Collision placeholder
  FluxEngine.Audio/
    AudioManager.cs       Audio placeholder
  FluxEngine.Content/
    AssetManager.cs       Content placeholder
demos/
  FluxEngine.Demo/        WASD window and movement demo
tests/                    Automated tests
docs/                     Project documentation
```

The folders under `src/` are organizational only, not separate projects. The
WASD demo is a separate runnable project and is not compiled into the engine.
Run it with `dotnet run --project demos/FluxEngine.Demo`.

Put test files in `tests/` and documentation in `docs/`.