using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Physics;

// platform that moves back and forth between two points. anything standing
// on top gets carried along
public sealed class MovingPlatform : ColliderComponent
{
    public Vector2 Start;
    public Vector2 End;
    public float Speed = 100f;
    public bool StartActive = true;

    private Vector2 _position;
    private float _t;
    private int _dir = 1;

    // how far the platform moved this frame, so riders can ride it
    public Vector2 MoveDelta { get; private set; }

    public override void OnStart()
    {
        base.OnStart();
        _position = StartActive ? Start : End;
        _t = 0;
        Entity.Position = _position;
    }

    public override void OnFixedUpdate(float deltaTime)
    {
        var previous = _position;
        var target = _dir > 0 ? End : Start;
        _position = Vector2.MoveTowards(_position, target, Speed * deltaTime);
        MoveDelta = _position - previous;
        Entity.Position = _position;

        if ((_position - target).LengthSquared < 1f)
            _dir *= -1;
    }
}
