using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Core;

// deadly. touch it and you die
public sealed class Hazard : Component
{
    public int Damage = 1;

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(new BoxCollider(Entity.Position, new Vector2(24, 24)),
            new BoxCollider(other.Position, new Vector2(20, 20)));
}
