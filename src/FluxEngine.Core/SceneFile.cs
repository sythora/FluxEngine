using System.Text.Json.Serialization;
using FluxEngine.Mathematics;

// serializable scene file. the editor writes this, the game reads it.
// hand-editable JSON if you want
public sealed class SceneFile
{
    public Vector2 PlayerStart { get; set; } = new(100, 400);
    public ColorSave BackgroundColor { get; set; } = new(110, 170, 255, 255);

    public List<PlatformSave> Platforms { get; set; } = [];
    public List<CoinSave> Coins { get; set; } = [];
    public List<SpikeSave> Spikes { get; set; } = [];
    public List<CheckpointSave> Checkpoints { get; set; } = [];
    public List<GoalSave> Goals { get; set; } = [];
    public List<MoverSave> Movers { get; set; } = [];
}

public sealed class ColorSave
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; } = 255;

    public ColorSave() { }
    public ColorSave(byte r, byte g, byte b, byte a = 255) => (R, G, B, A) = (r, g, b, a);
}

public sealed class PlatformSave
{
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; } = 100;
    public float H { get; set; } = 20;
    public bool OneWay { get; set; }
}

public sealed class CoinSave
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class SpikeSave
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class CheckpointSave
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class GoalSave
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class MoverSave
{
    public float StartX { get; set; }
    public float StartY { get; set; }
    public float EndX { get; set; } = 200;
    public float EndY { get; set; }
    public float Speed { get; set; } = 120f;
}
