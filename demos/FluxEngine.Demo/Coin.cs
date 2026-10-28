using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// Collectible coin with animated spin and bobbing. Destroys itself when the
// player touches it and notifies the GameManager to add score.
public sealed class Coin : Component
{
    public int Value = 10;
    private AnimatedSpriteRenderer _anim = null!;
    private float _bobTimer;
    private float _startY;

    public override void OnStart()
    {
        _anim = Entity.AddComponent<AnimatedSpriteRenderer>();
        _anim.Color = Color.White;
        _anim.Layer = 8;
        _anim.FrameTime = 0.1f;
        _anim.Loop = true;
        _anim.PlayOnStart = true;

        Entity.Scale = new Vector2(2, 2);
        _startY = Entity.Position.Y;

        var engine = Engine.Current!;
        for (var i = 0; i < 4; i++)
        {
            var texture = engine.Assets.LoadTexture(engine.Renderer, $"assets/coin_{i}.png");
            _anim.Frames.Add(new Sprite(texture));
        }

        _anim.Play();
    }

    public override void OnUpdate(float deltaTime)
    {
        _bobTimer += deltaTime * 4f;
        var bob = MathF.Sin(_bobTimer) * 4f;
        Entity.Position = new Vector2(Entity.Position.X, _startY + bob);
    }

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(
            new BoxCollider(Entity.Position, new Vector2(14, 14)),
            new BoxCollider(other.Position, new Vector2(16, 28)));
}
