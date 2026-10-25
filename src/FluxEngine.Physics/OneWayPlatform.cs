using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

// solid only from the top. you can jump up through it from below and land on
// it from above. the rigidbody checks this separately from normal solids
public sealed class OneWayPlatform : ColliderComponent
{
    // how far above the platform the player has to be to count as landing
    public float SnapThreshold = 8f;
}
