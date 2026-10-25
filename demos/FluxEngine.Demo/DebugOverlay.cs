using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;

public sealed class DebugOverlay
{
    private readonly InputActions _actions;
    private readonly PixelFont _font = new(2f, new Color(235, 245, 255));

    public DebugOverlay(InputActions actions)
    {
        _actions = actions;
        _actions.Bind("debug.toggle", Key.F3);
    }

    public bool Enabled { get; set; }

    public void Draw(Renderer renderer, Scene scene, float deltaTime)
    {
        if (!Enabled)
            return;

        renderer.Camera = null;
        renderer.DrawRectangle(new Rectangle(8, 8, 224, 64), new Color(12, 16, 24, 220));
        renderer.DrawRectangleOutline(new Rectangle(8, 8, 224, 64), new Color(80, 120, 160));

        var fps = deltaTime > 0 ? 1f / deltaTime : 0f;
        var frameMs = deltaTime * 1000f;
        _font.Draw(renderer, $"FPS {fps:000} FRAME {frameMs:00.0} MS", new Vector2(16, 16));
        _font.Draw(renderer, $"ENTITIES {scene.EntityCount:000}", new Vector2(16, 34));
        _font.Draw(renderer, $"COMPONENTS {scene.ComponentCount:000}", new Vector2(16, 52));
    }
}
