using System.Drawing;
using FluxEngine.Mathematics;

// one placed object in the editor
public abstract class EditableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "";
    public Vector2 Position { get; set; }
    public bool Selected { get; set; }

    public abstract void Draw(Graphics g, float zoom, Vector2 camera);
    public abstract RectangleF Bounds { get; }
    public abstract string Label { get; }
}

public sealed class PlatformObject : EditableObject
{
    public float W { get; set; } = 100;
    public float H { get; set; } = 20;
    public bool OneWay { get; set; }

    public override RectangleF Bounds => new(Position.X - W / 2, Position.Y - H / 2, W, H);
    public override string Label => OneWay ? "one-way" : "platform";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var rect = new RectangleF(screen.X - W * zoom / 2, screen.Y - H * zoom / 2, W * zoom, H * zoom);
        g.FillRectangle(OneWay ? Brushes.Green : Brushes.DarkGray, rect);
        g.DrawRectangle(Pens.White, rect.X, rect.Y, rect.Width, rect.Height);
        if (Selected)
            g.DrawRectangle(Pens.Yellow, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2);
    }
}

public sealed class CoinObject : EditableObject
{
    public override RectangleF Bounds => new(Position.X - 8, Position.Y - 8, 16, 16);
    public override string Label => "coin";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var size = 16f * zoom;
        g.FillEllipse(Brushes.Gold, screen.X - size / 2, screen.Y - size / 2, size, size);
        g.DrawEllipse(Pens.White, screen.X - size / 2, screen.Y - size / 2, size, size);
        if (Selected)
            g.DrawEllipse(Pens.Yellow, screen.X - size / 2 - 1, screen.Y - size / 2 - 1, size + 2, size + 2);
    }
}

public sealed class SpikeObject : EditableObject
{
    public override RectangleF Bounds => new(Position.X - 9, Position.Y - 9, 18, 18);
    public override string Label => "spike";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var size = 18f * zoom;
        g.FillRectangle(Brushes.Red, screen.X - size / 2, screen.Y - size / 2, size, size);
        g.DrawRectangle(Pens.White, screen.X - size / 2, screen.Y - size / 2, size, size);
        if (Selected)
            g.DrawEllipse(Pens.Yellow, screen.X - size / 2 - 1, screen.Y - size / 2 - 1, size + 2, size + 2);
    }
}

public sealed class CheckpointObject : EditableObject
{
    public override RectangleF Bounds => new(Position.X - 10, Position.Y - 16, 20, 32);
    public override string Label => "checkpoint";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var w = 20f * zoom;
        var h = 32f * zoom;
        g.FillRectangle(Brushes.Green, screen.X - w / 2, screen.Y - h / 2, w, h);
        g.DrawRectangle(Pens.White, screen.X - w / 2, screen.Y - h / 2, w, h);
        if (Selected)
            g.DrawEllipse(Pens.Yellow, screen.X - w / 2 - 1, screen.Y - h / 2 - 1, w + 2, h + 2);
    }
}

public sealed class GoalObject : EditableObject
{
    public override RectangleF Bounds => new(Position.X - 12, Position.Y - 24, 24, 48);
    public override string Label => "goal";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var w = 24f * zoom;
        var h = 48f * zoom;
        g.FillRectangle(Brushes.Gold, screen.X - w / 2, screen.Y - h / 2, w, h);
        g.DrawRectangle(Pens.White, screen.X - w / 2, screen.Y - h / 2, w, h);
        if (Selected)
            g.DrawEllipse(Pens.Yellow, screen.X - w / 2 - 1, screen.Y - h / 2 - 1, w + 2, h + 2);
    }
}

public sealed class MoverObject : EditableObject
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; } = new(200, 0);
    public float Speed { get; set; } = 120f;

    public override RectangleF Bounds => new(Position.X - 10, Position.Y - 10, 20, 20);
    public override string Label => "mover";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var size = 20f * zoom;
        g.FillRectangle(Brushes.Cyan, screen.X - size / 2, screen.Y - size / 2, size, size);
        g.DrawRectangle(Pens.White, screen.X - size / 2, screen.Y - size / 2, size, size);

        var startScreen = (Start - camera) * zoom;
        var endScreen = (End - camera) * zoom;
        g.DrawLine(Pens.Cyan, startScreen.X, startScreen.Y, endScreen.X, endScreen.Y);

        if (Selected)
            g.DrawEllipse(Pens.Yellow, screen.X - size / 2 - 1, screen.Y - size / 2 - 1, size + 2, size + 2);
    }
}

public sealed class PlayerObject : EditableObject
{
    public override RectangleF Bounds => new(Position.X - 14, Position.Y - 22, 28, 44);
    public override string Label => "player";

    public override void Draw(Graphics g, float zoom, Vector2 camera)
    {
        var screen = (Position - camera) * zoom;
        var w = 28f * zoom;
        var h = 44f * zoom;
        g.FillRectangle(Brushes.Yellow, screen.X - w / 2, screen.Y - h / 2, w, h);
        g.DrawRectangle(Pens.White, screen.X - w / 2, screen.Y - h / 2, w, h);
        g.DrawEllipse(Pens.Yellow, screen.X - w / 2 - 1, screen.Y - h / 2 - 1, w + 2, h + 2);
    }
}
