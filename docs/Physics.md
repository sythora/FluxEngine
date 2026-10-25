# Physics

Rigid bodies, colliders, and pushing boxes out of each other. It's 2D, axis
aligned, and dumb as hell. Good for platformers, not for stacking crates.

## Colliders

`BoxCollider` and `CircleCollider` are plain structs. They don't do anything
by themselves, they just hold position and size.

```csharp
var floor = new BoxCollider(new Vector2(400, 500), new Vector2(800, 40));
var playerHitbox = new BoxCollider(new Vector2(50, 50), new Vector2(20, 40));
var coin = new CircleCollider(new Vector2(100, 100), 8);
```

Check overlap:

```csharp
if (Collision.Overlaps(playerHitbox, coin))
    CollectCoin();
```

Get the smallest push to separate two boxes:

```csharp
var push = Collision.Separate(playerHitbox, floor);
player.Position += push;
```

## Rigidbody2D

Add this to an entity and it moves by velocity, applies gravity, drag, and
bounces off scene solids.

```csharp
var body = Entity.AddComponent<Rigidbody2D>();
body.Size = new Vector2(32, 48);
body.Gravity = new Vector2(0, 1500);
body.Drag = 4f;
body.Bounciness = 0.5f;
body.MaxFallSpeed = 2200;
body.UseGravity = true;
```

Properties:

- `Velocity` - current speed, set it to jump or kick
- `Gravity` - added every frame when `UseGravity` is on
- `Drag` - 0 means slides forever, higher slows down faster
- `Bounciness` - 0 is dead stop, 1 keeps all speed, 0.5 is half
- `MaxFallSpeed` - terminal velocity so you don't fall through floors
- `OnGround` - true when it landed on something this frame
- `TouchingWall` - true when it hit a wall sideways

It only collides with `Scene.Solids`. Add your level geometry there:

```csharp
scene.Solids.Add(new BoxCollider(new Vector2(400, 520), new Vector2(800, 40)));
```

No rotation, no stacked boxes pushing each other. If you need that you'll
have to write it yourself.

## Example

A ball that falls and bounces:

```csharp
var ball = scene.Spawn("ball", new Vector2(200, 100));
var sprite = ball.AddComponent<SpriteRenderer>();
sprite.Size = new Vector2(24, 24);
sprite.Color = Color.Red;

var body = ball.AddComponent<Rigidbody2D>();
body.Size = sprite.Size;
body.Bounciness = 0.7f;
```
