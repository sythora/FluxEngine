using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;

// Dynamic parallax background driven by the day/night cycle. Draws a gradient
// sky, animated celestial bodies (textured sun & moon), a twinkling starfield,
// textured drifting clouds, and layered parallax hills that all shift color
// with the time of day.
public sealed class ParallaxBackground : Component
{
    private readonly List<CloudInstance> _clouds = [];
    private readonly List<Star> _stars = [];
    private readonly Rng _rng = new(42);
    private float _time;

    private Texture? _sunTexture;
    private Texture? _moonTexture;
    private Texture? _cloudTexture;

    private record struct CloudInstance(Vector2 Position, float Speed, float Scale);
    private record struct Star(Vector2 Offset, float Phase, float Size, float Brightness);

    public ParallaxBackground()
    {
        for (var i = 0; i < 10; i++)
            _clouds.Add(new CloudInstance(
                new Vector2(_rng.NextFloat(-300, 1600), _rng.NextFloat(-300, -40)),
                _rng.NextFloat(12f, 40f),
                _rng.NextFloat(0.7f, 1.6f)));

        for (var i = 0; i < 80; i++)
            _stars.Add(new Star(
                new Vector2(_rng.NextFloat(-500, 500), _rng.NextFloat(-380, -40)),
                _rng.NextFloat(0f, MathF.Tau),
                _rng.NextFloat(1f, 3.5f),
                _rng.NextFloat(0.4f, 1.0f)));
    }

    public override void OnStart()
    {
        base.OnStart();
        var renderer = Engine.Current?.Renderer;
        if (renderer is null)
            return;

        try { _sunTexture = renderer.LoadTexture(TextureGenerator.Sun()); } catch { }
        try { _moonTexture = renderer.LoadTexture(TextureGenerator.Moon()); } catch { }
        try { _cloudTexture = renderer.LoadTexture(TextureGenerator.Cloud()); } catch { }
    }

    public override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;
        var cameraX = Entity.Scene?.Camera?.Position.X ?? 0;

        for (var i = 0; i < _clouds.Count; i++)
        {
            var cloud = _clouds[i];
            cloud.Position = new Vector2(cloud.Position.X + cloud.Speed * deltaTime, cloud.Position.Y);
            if (cloud.Position.X > cameraX + 1200f)
                cloud.Position = new Vector2(cameraX - 350f, cloud.Position.Y);
            _clouds[i] = cloud;
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        var camera = Entity.Scene?.Camera;
        if (camera is null)
            return;

        var dayNight = Entity.Scene?.Lighting.DayNight;
        var camX = camera.Position.X;
        var camY = camera.Position.Y;
        var halfW = 490f;
        var halfH = 370f;

        DrawSkyGradient(renderer, dayNight, camX, camY, halfW, halfH);
        DrawStarfield(renderer, dayNight, camX, camY);
        DrawCelestialBodies(renderer, dayNight, camX, camY);
        DrawClouds(renderer, dayNight, camX, camY);
        DrawFarRidge(renderer, dayNight, camX, camY);
        DrawMidRidge(renderer, dayNight, camX, camY);
        DrawNearRidge(renderer, dayNight, camX, camY, halfW, halfH);
    }

    private void DrawSkyGradient(Renderer renderer, DayNightCycle? dn, float camX, float camY, float halfW, float halfH)
    {
        Color topColor, midColor, bottomColor;

        if (dn is null)
        {
            topColor = new Color(24, 37, 78);
            midColor = new Color(94, 94, 142);
            bottomColor = new Color(230, 154, 126);
        }
        else
        {
            switch (dn.Phase)
            {
                case DayNightPhase.Night:
                    topColor = new Color(8, 12, 28);
                    midColor = new Color(14, 22, 52);
                    bottomColor = new Color(22, 32, 68);
                    break;
                case DayNightPhase.Dawn:
                    topColor = new Color(45, 55, 110);
                    midColor = new Color(170, 110, 120);
                    bottomColor = new Color(250, 170, 110);
                    break;
                case DayNightPhase.Dusk:
                    topColor = new Color(38, 42, 85);
                    midColor = new Color(160, 90, 100);
                    bottomColor = new Color(240, 145, 85);
                    break;
                default: // Day
                    topColor = new Color(55, 120, 210);
                    midColor = new Color(120, 175, 240);
                    bottomColor = new Color(180, 215, 255);
                    break;
            }
        }

        var skyTop = camY - halfH;
        var bandHeight = halfH * 2 / 5f;

        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop, halfW * 2, bandHeight), topColor);
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + bandHeight, halfW * 2, bandHeight),
            Color.Lerp(topColor, midColor, 0.5f));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + bandHeight * 2, halfW * 2, bandHeight), midColor);
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + bandHeight * 3, halfW * 2, bandHeight),
            Color.Lerp(midColor, bottomColor, 0.5f));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + bandHeight * 4, halfW * 2, bandHeight), bottomColor);
    }

    private void DrawStarfield(Renderer renderer, DayNightCycle? dn, float camX, float camY)
    {
        // Stars only visible at night and dusk/dawn edges
        float starAlpha;
        if (dn is null)
        {
            starAlpha = 0.6f;
        }
        else
        {
            starAlpha = dn.Phase switch
            {
                DayNightPhase.Night => 1.0f,
                DayNightPhase.Dawn => MathF.Max(0f, 1.0f - (dn.TimeOfDay - 5f) / 2.5f),
                DayNightPhase.Dusk => MathF.Max(0f, (dn.TimeOfDay - 18f) / 2.5f),
                _ => 0f,
            };
        }

        if (starAlpha < 0.02f)
            return;

        for (var i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            var twinkle = (MathF.Sin(_time * 2.8f + star.Phase) + 1f) * 0.5f;
            var alpha = (byte)(star.Brightness * twinkle * starAlpha * 255f);
            if (alpha < 8)
                continue;

            // Stars parallax very slowly against camera
            var sx = camX + star.Offset.X * 0.92f;
            var sy = camY + star.Offset.Y;

            var starColor = new Color(235, 230, 255, alpha);
            renderer.DrawRectangle(new Rectangle(sx, sy, star.Size, star.Size), starColor);
        }
    }

    private void DrawCelestialBodies(Renderer renderer, DayNightCycle? dn, float camX, float camY)
    {
        if (dn is null)
        {
            DrawSunFallback(renderer, camX, camY);
            return;
        }

        // Sun arcs across the sky from dawn to dusk
        if (dn.Phase is DayNightPhase.Dawn or DayNightPhase.Day or DayNightPhase.Dusk)
        {
            // Map the sun's arc across hours 5.5 to 20.5
            var sunProgress = MathUtil.Clamp01((dn.TimeOfDay - 5.5f) / 15f);
            var sunAngle = MathF.PI * sunProgress; // 0 → π, rises left, arcs overhead, sets right
            var sunX = camX + MathF.Cos(sunAngle) * 400f;
            var sunY = camY - MathF.Sin(sunAngle) * 280f - 40f;

            if (_sunTexture is not null)
            {
                var size = 96f;
                var sprite = new Sprite { Texture = _sunTexture };
                renderer.DrawSprite(sprite, new Vector2(sunX, sunY), scale: new Vector2(size / _sunTexture.Width, size / _sunTexture.Height));
            }
            else
            {
                renderer.DrawCircle(new Vector2(sunX, sunY), 48f, new Color(255, 218, 150));
                renderer.DrawCircle(new Vector2(sunX, sunY), 38f, new Color(255, 240, 200));
            }
        }

        // Moon arcs across the night sky
        if (dn.Phase is DayNightPhase.Night or DayNightPhase.Dusk or DayNightPhase.Dawn)
        {
            float moonProgress;
            if (dn.TimeOfDay >= 18f)
                moonProgress = (dn.TimeOfDay - 18f) / 12f;
            else
                moonProgress = (dn.TimeOfDay + 6f) / 12f;

            var moonAngle = MathF.PI * moonProgress;
            var moonX = camX - MathF.Cos(moonAngle) * 360f;
            var moonY = camY - MathF.Sin(moonAngle) * 250f - 60f;

            // Fade moon in/out at the edges
            var moonAlpha = dn.Phase switch
            {
                DayNightPhase.Night => 1.0f,
                DayNightPhase.Dusk => MathUtil.Clamp01((dn.TimeOfDay - 19f) / 1.5f),
                DayNightPhase.Dawn => MathUtil.Clamp01((7f - dn.TimeOfDay) / 1.5f),
                _ => 0f,
            };

            if (moonAlpha > 0.05f && _moonTexture is not null)
            {
                var size = 80f;
                var tint = new Color(255, 255, 255, (byte)(moonAlpha * 255f));
                var sprite = new Sprite { Texture = _moonTexture };
                renderer.DrawSprite(sprite, new Vector2(moonX, moonY),
                    scale: new Vector2(size / _moonTexture.Width, size / _moonTexture.Height),
                    tint: tint);
            }
            else if (moonAlpha > 0.05f)
            {
                var alpha = (byte)(moonAlpha * 255f);
                renderer.DrawCircle(new Vector2(moonX, moonY), 32f, new Color(200, 220, 255, alpha));
            }
        }
    }

    private void DrawSunFallback(Renderer renderer, float camX, float camY)
    {
        var sun = new Vector2(camX + 300f, camY - 150f);
        renderer.DrawCircle(sun, 58f, new Color(244, 181, 126));
        renderer.DrawCircle(sun, 46f, new Color(255, 218, 150));
    }

    private void DrawFarRidge(Renderer renderer, DayNightCycle? dn, float camX, float camY)
    {
        var color = dn?.Phase switch
        {
            DayNightPhase.Night => new Color(15, 25, 50),
            DayNightPhase.Dawn => new Color(72, 72, 108),
            DayNightPhase.Dusk => new Color(62, 55, 92),
            DayNightPhase.Day => new Color(58, 95, 145),
            _ => new Color(35, 55, 90),
        };

        DrawRidgeSlices(renderer, camX, camY, color, parallax: 0.12f, baseOffset: 300f, amplitude: 70f, wavelength: 420f, seed: 0f);
    }

    private void DrawMidRidge(Renderer renderer, DayNightCycle? dn, float camX, float camY)
    {
        var color = dn?.Phase switch
        {
            DayNightPhase.Night => new Color(19, 30, 55),
            DayNightPhase.Dawn => new Color(92, 88, 118),
            DayNightPhase.Dusk => new Color(80, 66, 100),
            DayNightPhase.Day => new Color(72, 112, 158),
            _ => new Color(50, 80, 110),
        };

        DrawRidgeSlices(renderer, camX, camY, color, parallax: 0.28f, baseOffset: 345f, amplitude: 46f, wavelength: 290f, seed: 130f);
    }

    private void DrawNearRidge(Renderer renderer, DayNightCycle? dn, float camX, float camY, float halfW, float halfH)
    {
        var color = dn?.Phase switch
        {
            DayNightPhase.Night => new Color(10, 17, 36),
            DayNightPhase.Dawn => new Color(55, 45, 70),
            DayNightPhase.Dusk => new Color(48, 38, 64),
            DayNightPhase.Day => new Color(40, 70, 105),
            _ => new Color(30, 48, 72),
        };

        DrawRidgeSlices(renderer, camX, camY, color, parallax: 0.55f, baseOffset: 400f, amplitude: 20f, wavelength: 205f, seed: 260f);
    }

    private void DrawRidgeSlices(
        Renderer renderer,
        float camX,
        float camY,
        Color color,
        float parallax,
        float baseOffset,
        float amplitude,
        float wavelength,
        float seed)
    {
        var bottomY = camY + 430f;
        var offsetX = camX * parallax;
        var halfW = 490f;
        var left = (int)(camX - halfW - 12f);
        var right = (int)(camX + halfW + 12f);
        var step = 6f;

        for (var x = (float)left; x <= right; x += step)
        {
            var phase = (x - offsetX + seed) / wavelength;
            var wave = 0.62f * MathF.Sin(phase) + 0.38f * MathF.Sin(phase * 2.17f + 1.3f);
            var ridgeY = camY + baseOffset - amplitude * wave;

            var height = (int)(bottomY - ridgeY);
            if (height <= 0)
                continue;

            renderer.DrawRectangle(new Rectangle(x, ridgeY, step, height), color);
        }
    }

    private void DrawClouds(Renderer renderer, DayNightCycle? dn, float camX, float camY)
    {
        var cloudOffset = camX * 0.5f;

        // Cloud tint adapts to atmosphere
        Color cloudTint;
        byte cloudAlpha;

        if (dn is null)
        {
            cloudTint = new Color(200, 210, 230);
            cloudAlpha = 170;
        }
        else
        {
            switch (dn.Phase)
            {
                case DayNightPhase.Night:
                    cloudTint = new Color(50, 60, 90);
                    cloudAlpha = 110;
                    break;
                case DayNightPhase.Dawn:
                    cloudTint = new Color(240, 180, 150);
                    cloudAlpha = 160;
                    break;
                case DayNightPhase.Dusk:
                    cloudTint = new Color(220, 130, 100);
                    cloudAlpha = 140;
                    break;
                default:
                    cloudTint = new Color(235, 240, 255);
                    cloudAlpha = 170;
                    break;
            }
        }

        foreach (var cloud in _clouds)
        {
            var cx = cloud.Position.X - cloudOffset;
            var cy = camY + cloud.Position.Y;
            var scale = cloud.Scale;

            if (_cloudTexture is not null)
            {
                var w = 64f * scale * 2.5f;
                var h = 32f * scale * 2.5f;
                var tint = new Color(cloudTint.R, cloudTint.G, cloudTint.B, cloudAlpha);
                var sprite = new Sprite { Texture = _cloudTexture };
                renderer.DrawSprite(sprite, new Vector2(cx, cy),
                    scale: new Vector2(w / _cloudTexture.Width, h / _cloudTexture.Height),
                    tint: tint);
            }
            else
            {
                var size = 40f * scale;
                var fallbackColor = new Color(cloudTint.R, cloudTint.G, cloudTint.B, cloudAlpha);
                renderer.DrawCircle(new Vector2(cx, cy), size, fallbackColor);
                renderer.DrawCircle(new Vector2(cx + size * 0.6f, cy), size * 0.8f, fallbackColor);
                renderer.DrawCircle(new Vector2(cx - size * 0.5f, cy + size * 0.3f), size * 0.7f, fallbackColor);
            }
        }
    }

}
