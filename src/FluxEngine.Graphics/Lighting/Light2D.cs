using FluxEngine.Core;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics.Lighting;

public sealed class Light2D : Component
{
    private float _flickerTimer;

    public float Radius { get; set; } = 120f;
    public Color Color { get; set; } = new(255, 230, 180);
    public float Intensity { get; set; } = 1.0f;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enables organic torch/fire flicker.
    /// </summary>
    public bool Flicker { get; set; }

    /// <summary>
    /// Frequency/speed of the flicker oscillation.
    /// </summary>
    public float FlickerSpeed { get; set; } = 7.0f;

    /// <summary>
    /// Percentage variation of light radius when flickering (e.g. 0.12 = +/- 12%).
    /// </summary>
    public float FlickerAmount { get; set; } = 0.12f;

    public Vector2 WorldPosition => (Entity is not null ? Entity.Position : Vector2.Zero) + Offset;

    public float EffectiveRadius
    {
        get
        {
            if (!Flicker || FlickerAmount <= 0f)
                return MathF.Max(0f, Radius);

            var wave = (MathF.Sin(_flickerTimer * FlickerSpeed) * 0.5f)
                     + (MathF.Sin(_flickerTimer * FlickerSpeed * 2.3f) * 0.3f)
                     + (MathF.Sin(_flickerTimer * FlickerSpeed * 4.7f) * 0.2f);

            return MathF.Max(0f, Radius * (1f + wave * FlickerAmount));
        }
    }

    public override void OnStart()
    {
        base.OnStart();
        Entity.Scene?.Lighting.RegisterLight(this);
    }

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        if (Flicker)
            _flickerTimer += deltaTime;
    }

    public void OnRemovedFromScene()
    {
        Entity.Scene?.Lighting.UnregisterLight(this);
    }
}
