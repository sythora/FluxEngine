# Graphics

Drawing stuff to the screen. All the actual SDL calls are hidden behind this so
you don't have to touch pointers.

## Renderer

This is where drawing happens. You get it from `Engine.Renderer` or the scene passes it to `OnDraw`.

```csharp
var renderer = Engine.Current!.Renderer;

renderer.Clear(Color.CornflowerBlue);
renderer.DrawRectangle(new Rectangle(10, 10, 100, 50), Color.Red);
renderer.DrawRectangleOutline(new Rectangle(10, 10, 100, 50), Color.White);
renderer.DrawLine(new Vector2(0, 0), new Vector2(100, 100), Color.Yellow);
renderer.DrawCircle(new Vector2(50, 50), 30, Color.Green);
renderer.DrawCircleOutline(new Vector2(100, 100), 20, Color.Orange);
renderer.DrawPolygon(points, Color.Pink);
```

Rectangles and circles use world units. If there's a camera on the scene it
converts them to screen pixels automatically. If you pass `null` to `Scene.Camera`
then world coords are just screen coords.

## Sprites and textures

Load a BMP (that's all SDL core gives us without extra libraries):

```csharp
var texture = engine.Renderer.LoadTexture("art/player.bmp");
var sprite = new Sprite(texture);
```

Or through the asset cache so you don't load the same file twice:

```csharp
var texture = engine.Assets.LoadTexture(engine.Renderer, "art/player.bmp");
```

Then draw it with a `SpriteRenderer` component:

```csharp
var sprite = Entity.AddComponent<SpriteRenderer>();
sprite.Sprite = new Sprite(texture);
sprite.Size = new Vector2(32, 48);
sprite.Color = Color.White;
sprite.Layer = 1;
```

Higher layer means it draws later, so it sits on top of lower layers.

Frames from a sprite sheet:

```csharp
var frame = sprite.WithFrame(1, 0, 32, 32); // column, row, cell width, height
```

## Camera

Put one on `Scene.Camera`. It handles zoom and converting world to screen.

```csharp
scene.Camera = new Camera
{
    Position = new Vector2(480, 320),
    Zoom = 2f
};
```

`Follow(target, smoothing)` moves the camera toward a position every frame.
Good for tracking the player.

`AddShake(amount)` adds screen shake. The offset is trauma squared so little
hits are subtle and big hits are obvious. It decays on its own.

```csharp
scene.Camera.AddShake(0.8f); // explosion or hit
```

You can also grab the shake offset yourself if you want to shake something else:

```csharp
var shake = scene.Camera.ShakeOffset;
```

## Colors

There's a bunch of named colors so you don't have to remember RGB:

`White`, `Black`, `Red`, `Green`, `Blue`, `Yellow`, `Orange`, `Purple`, `Pink`,
`Cyan`, `Magenta`, `Brown`, `Gray`, `LightGray`, `DarkGray`, `CornflowerBlue`,
`Transparent`.

Hex strings work too:

```csharp
var color = Color.FromHex("#ff8800");
var shorty = Color.FromHex("f80");
```
