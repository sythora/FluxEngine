using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Core;

// touch it to set your respawn point
public sealed class Checkpoint : Component
{
    public bool Activated { get; private set; }
    public Vector2 SpawnPoint => Entity.Position;

    public bool TouchedBy(Entity other) =>
        Collision.Overlaps(new BoxCollider(Entity.Position, new Vector2(20, 20)),
            new BoxCollider(other.Position, new Vector2(16, 16)));

    public void Activate() => Activated = true;
}
