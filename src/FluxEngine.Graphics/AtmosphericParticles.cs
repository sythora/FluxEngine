using FluxEngine.Core;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

public sealed class AtmosphericParticles : Component
{
    private readonly List<Particle> _particles = [];
    private readonly Rng _rng = new(1337);
    private float _time;

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public float AlphaPhase;
        public float BaseAlpha;
        public Color BaseColor;
    }

    public int MaxParticles { get; set; } = 45;
    public float SpawnRadiusX { get; set; } = 600f;
    public float SpawnRadiusY { get; set; } = 400f;

    public override void OnStart()
    {
        base.OnStart();
        InitParticles();
    }

    private void InitParticles()
    {
        _particles.Clear();
        var center = Entity.Position;

        for (var i = 0; i < MaxParticles; i++)
        {
            _particles.Add(new Particle
            {
                Position = new Vector2(
                    center.X + _rng.NextFloat(-SpawnRadiusX, SpawnRadiusX),
                    center.Y + _rng.NextFloat(-SpawnRadiusY, SpawnRadiusY)),
                Velocity = new Vector2(_rng.NextFloat(-15f, 25f), _rng.NextFloat(-10f, 10f)),
                Size = _rng.NextFloat(1.5f, 3.5f),
                AlphaPhase = _rng.NextFloat(0f, MathF.Tau),
                BaseAlpha = _rng.NextFloat(0.4f, 0.95f),
                BaseColor = new Color(255, 230, 140),
            });
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        _time += deltaTime;

        var center = Entity.Position;
        var dayNight = Entity.Scene?.Lighting.DayNight;
        var isNight = dayNight?.IsNight ?? false;

        for (var i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];

            // Gentle sinusoidal air drift
            var driftY = MathF.Sin(_time * 1.5f + p.AlphaPhase) * 12f * deltaTime;
            p.Position = new Vector2(
                p.Position.X + p.Velocity.X * deltaTime,
                p.Position.Y + p.Velocity.Y * deltaTime + driftY);

            // Recycle particles that drift out of bounds
            if (p.Position.X < center.X - SpawnRadiusX)
                p.Position = new Vector2(center.X + SpawnRadiusX, center.Y + _rng.NextFloat(-SpawnRadiusY, SpawnRadiusY));
            else if (p.Position.X > center.X + SpawnRadiusX)
                p.Position = new Vector2(center.X - SpawnRadiusX, center.Y + _rng.NextFloat(-SpawnRadiusY, SpawnRadiusY));

            if (p.Position.Y < center.Y - SpawnRadiusY)
                p.Position = new Vector2(center.X + _rng.NextFloat(-SpawnRadiusX, SpawnRadiusX), center.Y + SpawnRadiusY);
            else if (p.Position.Y > center.Y + SpawnRadiusY)
                p.Position = new Vector2(center.X + _rng.NextFloat(-SpawnRadiusX, SpawnRadiusX), center.Y - SpawnRadiusY);

            // Night fireflies glow lime-gold, daytime motes glow soft warm white
            p.BaseColor = isNight
                ? new Color(200, 255, 120)
                : new Color(255, 235, 175);

            _particles[i] = p;
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        base.OnDraw(renderer);

        for (var i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            var pulse = (MathF.Sin(_time * 3f + p.AlphaPhase) + 1f) * 0.5f;
            var alpha = (byte)(p.BaseAlpha * pulse * 230f);
            if (alpha < 10)
                continue;

            var drawColor = new Color(p.BaseColor.R, p.BaseColor.G, p.BaseColor.B, alpha);
            renderer.DrawCircle(p.Position, p.Size, drawColor);
        }
    }
}
