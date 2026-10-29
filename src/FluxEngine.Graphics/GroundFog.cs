using FluxEngine.Core;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

public sealed class GroundFog : Component
{
    private float _time;

    public float BaseY { get; set; } = 480f;
    public float ScrollSpeed { get; set; } = 28f;
    public float Alpha { get; set; } = 0.5f;

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        _time += deltaTime;
    }

    public override void OnDraw(Renderer renderer)
    {
        base.OnDraw(renderer);

        var camera = Entity.Scene?.Camera;
        var dayNight = Entity.Scene?.Lighting.DayNight;

        var camX = camera?.Position.X ?? Entity.Position.X;
        var camY = camera?.Position.Y ?? Entity.Position.Y;

        // Adapt fog color to atmosphere
        Color fogTint;
        if (dayNight is null)
        {
            fogTint = new Color(180, 200, 230);
        }
        else
        {
            fogTint = dayNight.Phase switch
            {
                DayNightPhase.Night => new Color(40, 50, 95),
                DayNightPhase.Dawn => new Color(210, 160, 150),
                DayNightPhase.Dusk => new Color(160, 110, 120),
                _ => new Color(200, 215, 235),
            };
        }

        var startX = camX - 520f;
        var endX = camX + 520f;
        var fogY = MathF.Max(BaseY, camY + 170f);

        // Multi-frequency rolling mist puffs, kept soft and sparse
        const float step = 62f;
        var alphaVal = (byte)Math.Clamp(Alpha * 70f, 0, 255);
        var fogColor = new Color(fogTint.R, fogTint.G, fogTint.B, alphaVal);

        for (var x = startX; x <= endX; x += step)
        {
            var wave1 = MathF.Sin((x + _time * ScrollSpeed) * 0.015f) * 20f;
            var wave2 = MathF.Cos((x - _time * (ScrollSpeed * 0.7f)) * 0.025f) * 12f;
            var py = fogY + wave1 + wave2;

            renderer.DrawCircle(new Vector2(x, py), 42f, fogColor);
            renderer.DrawCircle(new Vector2(x + 24f, py + 14f), 34f, fogColor);
        }
    }
}
