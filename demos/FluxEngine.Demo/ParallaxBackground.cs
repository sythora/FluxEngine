using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;

// Layered parallax background for the infinite platformer. Draws sky bands,
// distant hills, and drifting clouds at different scroll rates for depth.
// Uses rectangles and circles only - no textures needed.
public sealed class ParallaxBackground : Component
{
    private readonly List<Cloud> _clouds = [];
    private readonly Rng _rng = new(42);

    private record struct Cloud(Vector2 Position, float Speed, float Scale);

    public ParallaxBackground()
    {
        for (var i = 0; i < 12; i++)
            _clouds.Add(new Cloud(
                new Vector2(_rng.NextFloat(-200, 1600), _rng.NextFloat(-100, 200)),
                _rng.NextFloat(15f, 40f),
                _rng.NextFloat(0.6f, 1.4f)));
    }

    public override void OnUpdate(float deltaTime)
    {
        var cameraX = Entity.Scene?.Camera?.Position.X ?? 0;

        for (var i = 0; i < _clouds.Count; i++)
        {
            var cloud = _clouds[i];
            cloud.Position = new Vector2(cloud.Position.X + cloud.Speed * deltaTime, cloud.Position.Y);

            // Wrap around when off-screen right
            if (cloud.Position.X > cameraX + 1000f)
                cloud.Position = new Vector2(cameraX - 200f, cloud.Position.Y);

            _clouds[i] = cloud;
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        var camera = Entity.Scene?.Camera;
        if (camera is null)
            return;

        var camX = camera.Position.X;
        var camY = camera.Position.Y;
        var halfW = 480f;
        var halfH = 360f;

        // Sky bands form a warm pixel-art dusk palette behind the hills.
        var skyTop = camY - halfH;
        var skyBottom = camY + halfH;
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop, 960, 140), new Color(24, 37, 78));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 140, 960, 140), new Color(42, 62, 108));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 280, 960, 120), new Color(94, 94, 142));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 400, 960, 140), new Color(178, 113, 126));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 540, 960, 180), new Color(230, 154, 126));

        // A blocky sun and tiny pixel stars add a clear horizon focal point.
        var sun = new Vector2(camX + 300f, camY - 150f);
        renderer.DrawCircle(sun, 58f, new Color(244, 181, 126));
        renderer.DrawCircle(sun, 46f, new Color(255, 218, 150));
        renderer.DrawRectangle(new Rectangle(sun.X - 46f, sun.Y + 5f, 92f, 7f), new Color(244, 181, 126));
        renderer.DrawRectangle(new Rectangle(sun.X - 40f, sun.Y + 19f, 80f, 5f), new Color(244, 181, 126));
        foreach (var star in new[]
        {
            new Vector2(camX - 380, camY - 260), new Vector2(camX - 250, camY - 190),
            new Vector2(camX - 60, camY - 285), new Vector2(camX + 110, camY - 230),
            new Vector2(camX + 400, camY - 275),
        })
            renderer.DrawRectangle(new Rectangle(star.X, star.Y, 3, 3), new Color(255, 233, 190));

        // Distant hills (slow parallax)
        var hillOffset = camX * 0.15f;
        for (var i = -1; i <= 3; i++)
        {
            var hx = hillOffset + i * 400f;
            var hy = camY + 150f;
            renderer.DrawCircle(new Vector2(hx, hy), 90f, new Color(35, 55, 90));
            renderer.DrawCircle(new Vector2(hx + 60f, hy + 20f), 70f, new Color(35, 55, 90));
            renderer.DrawCircle(new Vector2(hx - 40f, hy + 30f), 80f, new Color(35, 55, 90));
        }

        // Midground hills
        var midOffset = camX * 0.35f;
        for (var i = -1; i <= 3; i++)
        {
            var hx = midOffset + i * 300f;
            var hy = camY + 180f;
            renderer.DrawCircle(new Vector2(hx, hy), 60f, new Color(50, 80, 110));
        }

        // Clouds (medium parallax)
        var cloudOffset = camX * 0.5f;
        foreach (var cloud in _clouds)
        {
            var cx = cloud.Position.X - cloudOffset;
            var cy = cloud.Position.Y;
            var scale = cloud.Scale;
            var size = 40f * scale;

            renderer.DrawCircle(new Vector2(cx, cy), size, new Color(200, 210, 230, 200));
            renderer.DrawCircle(new Vector2(cx + size * 0.6f, cy), size * 0.8f, new Color(200, 210, 230, 200));
            renderer.DrawCircle(new Vector2(cx - size * 0.5f, cy + size * 0.3f), size * 0.7f, new Color(200, 210, 230, 200));
        }

        // Layered horizon and foreground ridge give the scene more depth.
        renderer.DrawCircle(new Vector2(camX - 160f, camY + 250f), 135f, new Color(63, 83, 112));
        renderer.DrawCircle(new Vector2(camX + 180f, camY + 270f), 165f, new Color(54, 75, 105));
        renderer.DrawRectangle(new Rectangle(camX - halfW, camY + 330f, 960, 30), new Color(36, 55, 82));
        renderer.DrawRectangle(new Rectangle(camX - halfW, camY + 220f, 960, 4), new Color(255, 194, 139));
    }
}
