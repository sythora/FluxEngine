using FluxEngine.Core;
using FluxEngine.Mathematics;

// Converts the editor's editable-object model to the format written to disk.
// The editable objects are authoritative for every object collection; scene
// metadata such as background color remains on the existing shared SceneFile.
public static class SceneObjectSerializer
{
    public static SceneFile Capture(IEnumerable<EditableObject> objects, SceneFile? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(objects);

        var scene = new SceneFile
        {
            PlayerStart = metadata?.PlayerStart ?? new Vector2(100, 400),
            BackgroundColor = metadata?.BackgroundColor ?? new ColorSave(110, 170, 255, 255),
        };

        foreach (var obj in objects)
        {
            switch (obj)
            {
                case PlayerObject player:
                    scene.PlayerStart = player.Position;
                    break;
                case PlatformObject platform:
                    scene.Platforms.Add(new PlatformSave
                    {
                        X = platform.Position.X - platform.W * 0.5f,
                        Y = platform.Position.Y - platform.H * 0.5f,
                        W = platform.W,
                        H = platform.H,
                        OneWay = platform.OneWay,
                    });
                    break;
                case CoinObject coin:
                    scene.Coins.Add(new CoinSave { X = coin.Position.X, Y = coin.Position.Y });
                    break;
                case SpikeObject spike:
                    scene.Spikes.Add(new SpikeSave { X = spike.Position.X, Y = spike.Position.Y });
                    break;
                case CheckpointObject checkpoint:
                    scene.Checkpoints.Add(new CheckpointSave { X = checkpoint.Position.X, Y = checkpoint.Position.Y });
                    break;
                case GoalObject goal:
                    scene.Goals.Add(new GoalSave { X = goal.Position.X, Y = goal.Position.Y });
                    break;
                case MoverObject mover:
                    scene.Movers.Add(new MoverSave
                    {
                        StartX = mover.Start.X,
                        StartY = mover.Start.Y,
                        EndX = mover.End.X,
                        EndY = mover.End.Y,
                        Speed = mover.Speed,
                    });
                    break;
            }
        }

        return scene;
    }
}
