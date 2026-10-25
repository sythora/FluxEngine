# Content

Loading and caching assets so you don't read the same file off disk fifty
times a second.

## Texture cache

```csharp
var texture = engine.Assets.LoadTexture(engine.Renderer, "art/player.bmp");
// asking again returns the same texture, no extra disk read
var sameTexture = engine.Assets.LoadTexture(engine.Renderer, "art/player.bmp");
```

It keys on the full path so `art/player.bmp` and `../art/player.bmp` count
as different files if they resolve differently.

## Unloading

```csharp
engine.Assets.UnloadTexture(texture);
```

This disposes the SDL texture too, so make sure nothing is still drawing
with it first.

```csharp
engine.Assets.Clear(); // nukes every cached texture
```

Right now it only caches textures. If you want fonts or sounds cached you'll
have to add that yourself.
