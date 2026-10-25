using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Core;

// touch it and you win the level
public sealed class Goal : Component
{
    public bool Reached { get; private set; }

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(new BoxCollider(Entity.Position, new Vector2(24, 48)),
            new BoxCollider(other.Position, new Vector2(20, 20)));

    public void Reach() => Reached = true;
}
