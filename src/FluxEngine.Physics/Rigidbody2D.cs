using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// dumb platformer body. boxes only, no rotation, no stacking.
// moves X then Y, pushes out of solids on each axis so you don't get stuck
// in corners. rides moving platforms and lands on one-way platforms from above
public sealed class Rigidbody2D : Component
{
    public Vector2 Velocity;
    public Vector2 Gravity = new(0, 1500);

    // 0 slides forever, higher stops faster
    public float Drag;

    // 0 dead stop, 1 keeps all speed, 0.5 half bounce
    public float Bounciness;

    public float MaxFallSpeed = 2200;
    public bool UseGravity = true;
    public Vector2 Size = new(32, 32);

    public bool OnGround { get; private set; }
    public bool TouchingWall { get; private set; }

    public override void OnUpdate(float deltaTime)
    {
        if (UseGravity)
            Velocity += Gravity * deltaTime;

        if (Drag > 0)
            Velocity *= MathF.Max(0, 1 - Drag * deltaTime);

        if (Velocity.Y > MaxFallSpeed)
            Velocity = new Vector2(Velocity.X, MaxFallSpeed);

        var scene = Entity.Scene;
        if (scene is null)
            return;

        OnGround = false;
        TouchingWall = false;

        // --- move X, resolve X against everything solid ---
        Entity.Position += new Vector2(Velocity.X * deltaTime, 0);

        var bodyX = new BoxCollider(Entity.Position, Size);
        ResolveSolids(scene.Solids, bodyX, out var _);
        ResolveColliders(scene.Colliders, bodyX, out var _);

        // --- move Y, resolve Y against everything solid ---
        Entity.Position += new Vector2(0, Velocity.Y * deltaTime);

        var bodyY = new BoxCollider(Entity.Position, Size);
        ResolveSolids(scene.Solids, bodyY, out var hitGround);
        ResolveColliders(scene.Colliders, bodyY, out var hitPlatform);

        // ride moving platforms we're standing on
        if (hitPlatform is MovingPlatform platform)
        {
            Entity.Position += platform.MoveDelta;
            Velocity = new Vector2(platform.MoveDelta.X / deltaTime, Velocity.Y);
        }
    }

    private void ResolveSolids(IReadOnlyList<BoxCollider> solids, BoxCollider body, out ColliderComponent? platformHit)
    {
        platformHit = null;

        foreach (var solid in solids)
        {
            var push = Collision.Separate(body, solid);
            if (push == Vector2.Zero)
                continue;

            Entity.Position += push;
            body = new BoxCollider(Entity.Position, Size);

            if (push.Y < 0)
            {
                OnGround = true;
                Velocity = new Vector2(Velocity.X, MathF.Abs(Velocity.Y) < 40 ? 0 : -Velocity.Y * Bounciness);
            }
            else if (push.Y > 0)
            {
                if (Velocity.Y < 0)
                    Velocity = new Vector2(Velocity.X, -Velocity.Y * Bounciness);
            }
            else
            {
                TouchingWall = true;
                Velocity = new Vector2(0, Velocity.Y);
            }
        }
    }

    private void ResolveColliders(IReadOnlyList<ColliderComponent> colliders, BoxCollider body, out ColliderComponent? platformHit)
    {
        platformHit = null;

        foreach (var collider in colliders)
        {
            var other = collider.Bounds;
            var push = Collision.Separate(body, other);
            if (push == Vector2.Zero)
                continue;

            // one-way platforms: only solid from above, only when falling
            if (collider is OneWayPlatform oneWay)
            {
                var platformTop = other.Center.Y - other.Size.Y * 0.5f;
                var playerBottom = Entity.Position.Y + Size.Y * 0.5f;

                // not falling? ignore
                if (Velocity.Y <= 0)
                    continue;

                // The body has already advanced this frame, so allow a small
                // amount of downward penetration before snapping it to the top.
                // Without this tolerance, normal frame steps can cross the top
                // by a few pixels and fall through one-way platforms.
                if (playerBottom - platformTop > oneWay.SnapThreshold)
                    continue;
            }

            Entity.Position += push;
            body = new BoxCollider(Entity.Position, Size);

            if (push.Y < 0)
            {
                OnGround = true;
                Velocity = new Vector2(Velocity.X, MathF.Abs(Velocity.Y) < 40 ? 0 : -Velocity.Y * Bounciness);
                platformHit = collider;
            }
            else if (push.Y > 0)
            {
                if (Velocity.Y < 0)
                    Velocity = new Vector2(Velocity.X, -Velocity.Y * Bounciness);
            }
            else
            {
                TouchingWall = true;
                Velocity = new Vector2(0, Velocity.Y);
            }
        }
    }
}
