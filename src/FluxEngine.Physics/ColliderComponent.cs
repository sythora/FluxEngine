using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

// base class for anything with a box collider that the physics system should
// know about. registers itself with the scene automatically
public abstract class ColliderComponent : Component
{
    public Vector2 Size = new(32, 32);
    public Vector2 Offset;
    public bool IsTrigger { get; set; }

    public BoxCollider Bounds => new(Entity.Position + Offset, Size);

    public override void OnStart()
    {
        base.OnStart();
        if (Entity.Scene is not null)
            Entity.Scene.RegisterCollider(this);
    }

    // called by Entity when it gets destroyed so the scene drops the collider
    public void OnRemovedFromScene()
    {
        if (Entity.Scene is not null)
            Entity.Scene.UnregisterCollider(this);
    }
}
