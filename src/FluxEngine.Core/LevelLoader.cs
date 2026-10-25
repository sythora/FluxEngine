using FluxEngine.Mathematics;

// loads and saves levels as plain text. format:
// PLAYER x y
// PLATFORM x y w h
// ONEWAY x y w h
// MOVER x1 y1 x2 y2 speed
// COIN x y
// SPIKE x y
// CHECKPOINT x y
// GOAL x y
// BG r g b
public static class LevelLoader
{
    private static readonly System.Globalization.CultureInfo Invariant = System.Globalization.CultureInfo.InvariantCulture;

    public static LevelData Load(string path)
    {
        var level = new LevelData();
        if (!File.Exists(path))
            return level;

        foreach (var line in File.ReadAllLines(path))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0].StartsWith("#"))
                continue;

            try
            {
                switch (parts[0].ToUpperInvariant())
                {
                    case "PLAYER":
                        Require(parts, 3, line);
                        level.PlayerStart = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2]));
                        break;

                    case "PLATFORM":
                    case "ONEWAY":
                        Require(parts, 5, line);
                        level.Platforms.Add(new PlatformDef
                        {
                            Position = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])),
                            Size = new Vector2(ParseFloat(parts[3]), ParseFloat(parts[4])),
                        });
                        break;

                    case "MOVER":
                        Require(parts, 5, line);
                        level.Movers.Add(new MoverDef
                        {
                            Start = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])),
                            End = new Vector2(ParseFloat(parts[3]), ParseFloat(parts[4])),
                            Speed = parts.Length > 5 ? ParseFloat(parts[5]) : 120f,
                        });
                        break;

                    case "COIN":
                        Require(parts, 3, line);
                        level.Coins.Add(new CoinDef { Position = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])) });
                        break;

                    case "SPIKE":
                        Require(parts, 3, line);
                        level.Spikes.Add(new SpikeDef { Position = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])) });
                        break;

                    case "CHECKPOINT":
                        Require(parts, 3, line);
                        level.Checkpoints.Add(new CheckpointDef { Position = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])) });
                        break;

                    case "GOAL":
                        Require(parts, 3, line);
                        level.Goals.Add(new GoalDef { Position = new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])) });
                        break;

                    case "BG":
                        Require(parts, 4, line);
                        level.BackgroundColor = new Color(
                            byte.Parse(parts[1], Invariant),
                            byte.Parse(parts[2], Invariant),
                            byte.Parse(parts[3], Invariant));
                        break;

                    default:
                        throw new FormatException($"Unknown record type '{parts[0]}'.");
                }
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                throw new FormatException($"Invalid level data: '{line}'. {exception.Message}", exception);
            }
        }

        return level;
    }

    public static void Save(string path, LevelData level)
    {
        var lines = new List<string>
        {
            "# FluxEngine level file",
            string.Create(Invariant, $"PLAYER {level.PlayerStart.X} {level.PlayerStart.Y}"),
            $"BG {level.BackgroundColor.R} {level.BackgroundColor.G} {level.BackgroundColor.B}",
            "",
        };

        foreach (var p in level.Platforms)
            lines.Add(string.Create(Invariant, $"PLATFORM {p.Position.X} {p.Position.Y} {p.Size.X} {p.Size.Y}"));
        foreach (var m in level.Movers)
            lines.Add(string.Create(Invariant, $"MOVER {m.Start.X} {m.Start.Y} {m.End.X} {m.End.Y} {m.Speed}"));
        foreach (var c in level.Coins)
            lines.Add(string.Create(Invariant, $"COIN {c.Position.X} {c.Position.Y}"));
        foreach (var s in level.Spikes)
            lines.Add(string.Create(Invariant, $"SPIKE {s.Position.X} {s.Position.Y}"));
        foreach (var c in level.Checkpoints)
            lines.Add(string.Create(Invariant, $"CHECKPOINT {c.Position.X} {c.Position.Y}"));
        foreach (var g in level.Goals)
            lines.Add(string.Create(Invariant, $"GOAL {g.Position.X} {g.Position.Y}"));

        File.WriteAllText(path, string.Join("\n", lines));
    }

    private static void Require(string[] parts, int count, string line)
    {
        if (parts.Length < count)
            throw new FormatException($"Expected at least {count} fields in '{line}'.");
    }

    private static float ParseFloat(string value) => float.Parse(value, Invariant);
}
