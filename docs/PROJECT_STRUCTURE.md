# Project Structure

```text
FluxEngine.slnx
Directory.Build.props
src/
  FluxEngine.csproj       The single engine project
  FluxEngine.Core/
    Engine.cs             Engine entry point placeholder
    Scene.cs              Scene placeholder
  FluxEngine.Mathematics/
    Vector2.cs            Vector type placeholder
  FluxEngine.Graphics/
    Renderer.cs           Renderer placeholder
    Sprite.cs             Sprite placeholder
  FluxEngine.Platform/
    Window.cs             Window placeholder
  FluxEngine.Input/
    InputManager.cs       Input placeholder
  FluxEngine.Physics/
    Collider.cs           Collision placeholder
  FluxEngine.Audio/
    AudioManager.cs       Audio placeholder
  FluxEngine.Content/
    AssetManager.cs       Content placeholder
tests/                    Automated tests
docs/                     Project documentation
```

There is one .NET project. The folders under `src/` are organizational only,
not separate projects. The listed types are empty placeholders with no engine
behavior. Add future implementation to the matching subsystem folder.

Put test files in `tests/` and documentation in `docs/`. This is structure
only; engine systems have not been implemented.