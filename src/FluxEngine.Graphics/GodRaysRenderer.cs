using FluxEngine.Core;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// Volumetric light shafts fanning out from the sun's position. Each beam is a
// filled gradient quad that fades to nothing along its length, so shafts melt
// into the air instead of looking like hard strokes.
public sealed class GodRaysRenderer : Component
{
    private float _time;

    public int RayCount { get; set; } = 5;
    public float BeamAngleDegrees { get; set; } = 55f;
    public float TopBeamWidth { get; set; } = 26f;
    public float BeamLength { get; set; } = 780f;
    public float Intensity { get; set; } = 1.0f;

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

        // No god rays deep at night
        if (dayNight is not null && dayNight.IsNight)
            return;

        var camX = camera?.Position.X ?? Entity.Position.X;
        var camY = camera?.Position.Y ?? Entity.Position.Y;

        // Beam color/tint follows the sky: warm at dawn/dusk, pale white by day
        Color beamColor;
        float strength;

        if (dayNight is null)
        {
            beamColor = new Color(255, 230, 165);
            strength = 0.32f;
        }
        else
        {
            switch (dayNight.Phase)
            {
                case DayNightPhase.Dawn:
                    beamColor = new Color(255, 190, 130);
                    strength = 0.30f;
                    break;
                case DayNightPhase.Dusk:
                    beamColor = new Color(255, 165, 105);
                    strength = 0.34f;
                    break;
                case DayNightPhase.Day:
                    beamColor = new Color(255, 250, 225);
                    strength = 0.20f;
                    break;
                default:
                    return;
            }
        }

        strength *= Intensity;

        // Mirror the ParallaxBackground sun arc so shafts visibly leave the sun
        Vector2 sunPosition;
        if (dayNight is not null)
        {
            var sunProgress = MathUtil.Clamp01((dayNight.TimeOfDay - 5.5f) / 15f);
            var sunAngle = MathF.PI * sunProgress;
            sunPosition = new Vector2(
                camX + MathF.Cos(sunAngle) * 400f,
                camY - MathF.Sin(sunAngle) * 280f - 40f);
        }
        else
        {
            sunPosition = new Vector2(camX + 300f, camY - 150f);
        }

        // Shafts point away from the sun and fall toward the ground: a sun on
        // the left casts rays down-right, and vice versa.
        var slantSign = sunPosition.X >= camX ? -1f : 1f;
        var rad = BeamAngleDegrees * MathF.PI / 180f;
        var dir = new Vector2(MathF.Cos(rad) * slantSign, MathF.Sin(rad));
        dir = dir / MathF.Max(dir.Length, 0.0001f);

        // Perpendicular across the beam's width
        var side = new Vector2(-dir.Y, dir.X);

        for (var i = 0; i < RayCount; i++)
        {
            // Beams spread out from the sun with slight per-beam shimmer
            var spread = (i - (RayCount - 1) * 0.5f) * (TopBeamWidth * 4.5f);
            var shimmer = MathF.Sin(_time * 1.4f + i * 1.9f) * 0.18f + 0.82f;

            // Fade outer beams and stagger their lengths a little
            var edgeFalloff = 1f - MathF.Abs(i - (RayCount - 1) * 0.5f) / ((RayCount - 1) * 0.5f + 1f);
            var topAlpha = (byte)Math.Clamp(255f * strength * shimmer * (0.45f + 0.55f * edgeFalloff), 0, 255);
            if (topAlpha < 4)
                continue;

            var length = BeamLength * (0.75f + 0.25f * edgeFalloff);
            var origin = sunPosition + side * spread;
            var topColor = new Color(beamColor.R, beamColor.G, beamColor.B, topAlpha);
            var bottomColor = new Color(beamColor.R, beamColor.G, beamColor.B, 0);

            var topStart = origin - side * (TopBeamWidth * 0.5f);
            var topEnd = origin + side * (TopBeamWidth * 0.5f);
            var bottomWidth = TopBeamWidth * 4f;
            var bottomStart = origin + dir * length - side * (bottomWidth * 0.5f);
            var bottomEnd = origin + dir * length + side * (bottomWidth * 0.5f);

            renderer.DrawQuadGradient(topStart, topEnd, bottomStart, bottomEnd, topColor, bottomColor);
        }
    }
}
