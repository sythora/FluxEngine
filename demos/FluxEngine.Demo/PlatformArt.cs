using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;

public sealed class PlatformArt : Component
{
    public Vector2 Size { get; set; } = new(160, 24);

    public override void OnDraw(Renderer renderer)
    {
        var left = Entity.Position.X - Size.X * 0.5f;
        var top = Entity.Position.Y - Size.Y * 0.5f;

        renderer.DrawRectangle(new Rectangle(left, top + 4, Size.X, Size.Y - 4), new Color(111, 70, 48));
        renderer.DrawRectangle(new Rectangle(left, top + 4, Size.X, MathF.Max(0, Size.Y - 8)), new Color(139, 86, 55));
        renderer.DrawRectangle(new Rectangle(left, top, Size.X, 6), new Color(54, 114, 67));
        renderer.DrawRectangle(new Rectangle(left, top + 2, Size.X, 3), new Color(105, 181, 79));
        renderer.DrawRectangle(new Rectangle(left, top + 5, Size.X, 2), new Color(182, 209, 104));

        for (var x = 12f; x < Size.X - 8; x += 28f)
        {
            renderer.DrawRectangle(new Rectangle(left + x, top + 11, 8, 3), new Color(157, 101, 65));
            renderer.DrawRectangle(new Rectangle(left + x + 12, top + 18, 5, 2), new Color(91, 59, 47));
        }

        renderer.DrawRectangle(new Rectangle(left, top + Size.Y - 3, Size.X, 3), new Color(75, 52, 47));
    }
}
