using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// Deadly spike hazard. Touching it kills the player.
public sealed class Hazard : Component
{
    public int Damage = 1;
    public override void OnStart()
    {
        var sprite = Entity.AddComponent<SpriteRenderer>();
        sprite.Size = new Vector2(18, 18);
        sprite.Color = Color.Red;
        sprite.Layer = 7;
    }

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(
            new BoxCollider(Entity.Position, new Vector2(18, 18)),
            new BoxCollider(other.Position, new Vector2(16, 28)));
}
