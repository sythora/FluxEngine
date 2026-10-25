using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// click-to-place level editor. press E to toggle edit mode.
// 1 platform, 2 coin, 3 spike, 4 checkpoint, 5 goal, 6 mover
// S save, L load, arrow keys move the cursor
public sealed class LevelEditor : Component
{
    private readonly LevelData _level;
    private readonly string _savePath;

    private bool _editMode;
    private int _tool = 1; // 1 platform, 2 coin, 3 spike, 4 checkpoint, 5 goal, 6 mover
    private Vector2 _cursor;
    private Vector2 _dragStart;
    private bool _dragging;

    public LevelEditor(LevelData level, string savePath = "level.txt")
    {
        _level = level;
        _savePath = savePath;
        _cursor = level.PlayerStart;
    }

    public bool EditMode => _editMode;

    public override void OnUpdate(float deltaTime)
    {
        var input = Engine.Current!.Input;
        var actions = input.Actions;

        if (actions.WasPressed("edit"))
            _editMode = !_editMode;

        if (!_editMode)
            return;

        // move cursor with arrow keys or mouse
        var move = new Vector2(
            actions.Axis("edit.left", "edit.right"),
            actions.Axis("edit.up", "edit.down"));

        if (move != Vector2.Zero)
            _cursor += move * 200f * deltaTime;

        var mouse = input.MousePosition;
        var cam = Entity.Scene!.Camera!;
        _cursor = cam.ScreenToWorld(mouse, Engine.Current!.Window.Width, Engine.Current!.Window.Height);

        // place with left click
        if (input.WasMouseButtonPressed(MouseButton.Left))
        {
            _dragStart = _cursor;
            _dragging = true;
        }

        if (input.IsMouseButtonDown(MouseButton.Left) && _dragging)
        {
            // placeholder - actual placement on release
        }

        if (input.WasMouseButtonReleased(MouseButton.Left) && _dragging)
        {
            _dragging = false;
            Place(_cursor);
        }

        // tool selection
        if (actions.WasPressed("tool1")) _tool = 1;
        if (actions.WasPressed("tool2")) _tool = 2;
        if (actions.WasPressed("tool3")) _tool = 3;
        if (actions.WasPressed("tool4")) _tool = 4;
        if (actions.WasPressed("tool5")) _tool = 5;
        if (actions.WasPressed("tool6")) _tool = 6;

        // save / load
        if (actions.WasPressed("save"))
        {
            LevelLoader.Save(_savePath, _level);
            Log.Info("saved " + _savePath);
        }

        if (actions.WasPressed("load"))
        {
            var loaded = LevelLoader.Load(_savePath);
            _level.PlayerStart = loaded.PlayerStart;
            _level.BackgroundColor = loaded.BackgroundColor;
            _level.Platforms.Clear();
            _level.Platforms.AddRange(loaded.Platforms);
            _level.Coins.Clear();
            _level.Coins.AddRange(loaded.Coins);
            _level.Spikes.Clear();
            _level.Spikes.AddRange(loaded.Spikes);
            _level.Checkpoints.Clear();
            _level.Checkpoints.AddRange(loaded.Checkpoints);
            _level.Goals.Clear();
            _level.Goals.AddRange(loaded.Goals);
            _level.Movers.Clear();
            _level.Movers.AddRange(loaded.Movers);
            Log.Info("loaded " + _savePath);
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        if (!_editMode)
            return;

        // draw cursor
        renderer.DrawRectangleOutline(
            new Rectangle(_cursor.X - 8, _cursor.Y - 8, 16, 16),
            Color.White);

        // draw tool label
        var toolName = _tool switch
        {
            1 => "platform",
            2 => "coin",
            3 => "spike",
            4 => "checkpoint",
            5 => "goal",
            6 => "mover",
            _ => "?"
        };

        renderer.DrawRectangle(new Rectangle(10, 10, 100, 16), new Color(0, 0, 0, 120));
        // text renderer missing, so just draw a colored box per tool
        var toolColor = _tool switch
        {
            1 => Color.White,
            2 => Color.Yellow,
            3 => Color.Red,
            4 => Color.Green,
            5 => Color.Gold,
            6 => Color.Cyan,
            _ => Color.Gray
        };
        renderer.DrawRectangle(new Rectangle(12, 12, 12, 12), toolColor);

        // draw ghost of what's being placed
        if (_tool == 1)
            renderer.DrawRectangleOutline(new Rectangle(_cursor.X - 50, _cursor.Y - 10, 100, 20), Color.White);
        else if (_tool == 6)
            renderer.DrawCircleOutline(_cursor, 12, Color.Cyan);
        else
            renderer.DrawRectangleOutline(new Rectangle(_cursor.X - 8, _cursor.Y - 8, 16, 16), toolColor);
    }

    private void Place(Vector2 pos)
    {
        switch (_tool)
        {
            case 1:
                _level.Platforms.Add(new PlatformDef { Position = pos, Size = new Vector2(100, 20) });
                break;
            case 2:
                _level.Coins.Add(new CoinDef { Position = pos });
                break;
            case 3:
                _level.Spikes.Add(new SpikeDef { Position = pos });
                break;
            case 4:
                _level.Checkpoints.Add(new CheckpointDef { Position = pos });
                break;
            case 5:
                _level.Goals.Add(new GoalDef { Position = pos });
                break;
            case 6:
                _level.Movers.Add(new MoverDef { Start = pos, End = pos + new Vector2(200, 0), Speed = 120f });
                break;
        }
    }
}
