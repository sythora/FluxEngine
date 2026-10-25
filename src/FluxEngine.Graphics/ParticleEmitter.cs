using FluxEngine.Core;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Graphics;

// tiny firework of squares. good for jumps, landings, explosions, that sort
// of thing. no textures, just colored rectangles
public sealed class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public float Size;
    public Color Color;

    public bool Alive => Life > 0;
    public float Progress => 1 - (Life / MaxLife);
}

public sealed class ParticleEmitter : Component
{
    private readonly List<Particle> _particles = [];
    private readonly Rng _rng = new();

    public Color Color = Color.White;
    public float Size = 4f;
    public float Life = 0.4f;
    public float Speed = 80f;
    public int MaxParticles = 40;

    public void Emit(Vector2 position, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (_particles.Count >= MaxParticles)
                break;

            var angle = _rng.NextFloat(0, 360f);
            var velocity = Vector2.FromAngle(angle) * _rng.NextFloat(Speed * 0.5f, Speed);

            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Life = Life,
                MaxLife = Life,
                Size = Size,
                Color = Color
            });
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Position += p.Velocity * deltaTime;
            p.Velocity *= 0.9f;
            p.Life -= deltaTime;

            if (!p.Alive)
                _particles.RemoveAt(i);
            else
                _particles[i] = p;
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        foreach (var p in _particles)
        {
            var color = p.Color.WithAlpha((byte)(255 * (1 - p.Progress)));
            renderer.DrawRectangle(new Rectangle(p.Position.X, p.Position.Y, p.Size, p.Size), color);
        }
    }
}
