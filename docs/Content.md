# Content

Loading and caching assets so you don't read the same file off disk fifty
times a second.

## Texture cache

```csharp
var texture = engine.Assets.LoadTexture(engine.Renderer, "art/player.png");
// asking again returns the same texture, no extra disk read
var sameTexture = engine.Assets.LoadTexture(engine.Renderer, "art/player.png");
```

PNG, JPEG, and BMP files all work. It keys on the full path so
`art/player.png` and `../art/player.png` count as different files if they
resolve differently.

Built-in fonts are embedded resources rather than files on disk:

```csharp
using FluxEngine.Content;

var font = BuiltInFonts.Load(engine.Renderer, BuiltInFont.PressStart2P);
```

See [Fonts.md](Fonts.md) for custom BMFont maps.

## Unloading

```csharp
engine.Assets.UnloadTexture(texture);
```

This disposes the SDL texture too, so make sure nothing is still drawing
with it first.

```csharp
engine.Assets.Clear(); // nukes every cached texture
```

Right now it only caches textures. Fonts load from embedded resources or a
single `.fnt` map; if you want those or sounds cached, keep one instance per
game. The demo does this with its HUD fonts.
