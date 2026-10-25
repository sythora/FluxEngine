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

        // Sky gradient bands (drawn as rectangles)
        var skyTop = camY - halfH;
        var skyBottom = camY + halfH;

        // Deep sky
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop, 960, 180), new Color(25, 40, 80));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 180, 960, 180), new Color(40, 70, 120));
        renderer.DrawRectangle(new Rectangle(camX - halfW, skyTop + 360, 960, 180), new Color(60, 100, 160));

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

        // Ground line at bottom of screen
        renderer.DrawRectangle(new Rectangle(camX - halfW, camY + 220, 960, 4), new Color(100, 140, 180));
    }
}
