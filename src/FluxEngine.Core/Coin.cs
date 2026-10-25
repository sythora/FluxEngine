using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Core;

// collectible. spins and bobs, destroys itself when the player touches it
public sealed class Coin : Component
{
    public int Value = 1;
    public float SpinSpeed = 4f;
    public float BobHeight = 4f;
    public float BobSpeed = 4f;

    private float _startY;
    private float _time;

    public override void OnStart() => _startY = Entity.Position.Y;

    public override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;
        Entity.Rotation += SpinSpeed * deltaTime;
        Entity.Position = new Vector2(Entity.Position.X, _startY + MathF.Sin(_time * BobSpeed) * BobHeight);
    }

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(new BoxCollider(Entity.Position, new Vector2(16, 16)),
            new BoxCollider(other.Position, new Vector2(24, 24)));
}
