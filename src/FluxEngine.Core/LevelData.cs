using FluxEngine.Mathematics;

// data for one level. saved to / loaded from a plain text file so you can
// build levels without recompiling
public sealed class LevelData
{
    public Vector2 PlayerStart = new(100, 400);
    public Color BackgroundColor = new(110, 170, 255);

    public readonly List<PlatformDef> Platforms = [];
    public readonly List<CoinDef> Coins = [];
    public readonly List<SpikeDef> Spikes = [];
    public readonly List<CheckpointDef> Checkpoints = [];
    public readonly List<GoalDef> Goals = [];
    public readonly List<MoverDef> Movers = [];
}

public sealed class PlatformDef
{
    public Vector2 Position;
    public Vector2 Size;
}

public sealed class CoinDef
{
    public Vector2 Position;
}

public sealed class SpikeDef
{
    public Vector2 Position;
}

public sealed class CheckpointDef
{
    public Vector2 Position;
}

public sealed class GoalDef
{
    public Vector2 Position;
}

public sealed class MoverDef
{
    public Vector2 Start;
    public Vector2 End;
    public float Speed = 120f;
}
