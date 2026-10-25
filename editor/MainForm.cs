using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FluxEngine.Core;
using FluxEngine.Mathematics;

// standalone scene editor. runs outside the engine, saves .scene files
// that the game loads at runtime. unity-lite: place objects, drag them,
// edit properties, save
public sealed partial class MainForm : Form
{
    private Panel _canvasPanel;
    private PictureBox _canvas;
    private ToolStrip _toolbar;
    private Panel _props;
    private Label _propTitle;
    private FlowLayoutPanel _propFields;

    private SceneFile _scene = new();
    private string? _currentPath;
    private readonly List<EditableObject> _objects = [];
    private EditableObject? _selected;
    private string _currentTool = "platform";
    private Vector2 _camera = new(400, 320);
    private float _zoom = 1f;
    private bool _gridSnap = true;
    private float _gridSize = 32f;

    private Point _lastMouse;
    private bool _panning;
    private bool _dragging;
    private Vector2 _dragStart;
    private EditableObject? _dragObject;

    public MainForm()
    {
        Text = "FluxEngine Editor";
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(800, 600);

        BuildMenu();
        BuildToolbar();
        BuildCanvas();
        BuildProperties();
        BuildStatus();

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
    }

    private void BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");

        file.DropDownItems.Add("New", null, (_, _) => NewFile());
        file.DropDownItems.Add("Open...", null, (_, _) => OpenFile());
        file.DropDownItems.Add("Save", null, (_, _) => SaveFile());
        file.DropDownItems.Add("Save As...", null, (_, _) => SaveFileAs());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());

        menu.Items.Add(file);
        Controls.Add(menu);
        MainMenuStrip = menu;
    }

    private void BuildToolbar()
    {
        _toolbar = new ToolStrip();
        _toolbar.Dock = DockStyle.Top;
        _toolbar.ImageScalingSize = new Size(24, 24);

        void AddTool(string name, string label, System.Drawing.Color color)
        {
            var btn = new ToolStripButton(label)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                BackColor = color,
                Margin = new Padding(2),
            };
            btn.Click += (_, _) => SetTool(name);
            _toolbar.Items.Add(btn);
        }

        AddTool("player", "Player", System.Drawing.Color.Yellow);
        AddTool("platform", "Platform", System.Drawing.Color.DarkGray);
        AddTool("coin", "Coin", System.Drawing.Color.Gold);
        AddTool("spike", "Spike", System.Drawing.Color.Red);
        AddTool("checkpoint", "Checkpoint", System.Drawing.Color.Green);
        AddTool("goal", "Goal", System.Drawing.Color.Gold);
        AddTool("mover", "Mover", System.Drawing.Color.Cyan);

        _toolbar.Items.Add(new ToolStripSeparator());

        var snapBtn = new ToolStripButton("Grid Snap")
        {
            CheckOnClick = true,
            Checked = true,
        };
        snapBtn.Click += (_, _) => _gridSnap = snapBtn.Checked;
        _toolbar.Items.Add(snapBtn);

        var zoomIn = new ToolStripButton("Zoom +");
        zoomIn.Click += (_, _) => { _zoom = Math.Min(4f, _zoom + 0.25f); _canvas.Invalidate(); };
        _toolbar.Items.Add(zoomIn);

        var zoomOut = new ToolStripButton("Zoom -");
        zoomOut.Click += (_, _) => { _zoom = Math.Max(0.25f, _zoom - 0.25f); _canvas.Invalidate(); };
        _toolbar.Items.Add(zoomOut);

        Controls.Add(_toolbar);
    }

    private void BuildCanvas()
    {
        _canvasPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = System.Drawing.Color.FromArgb(40, 40, 45),
        };

        _canvas = new PictureBox
        {
            Size = new Size(2400, 1600),
            BackColor = System.Drawing.Color.FromArgb(40, 40, 45),
            SizeMode = PictureBoxSizeMode.AutoSize,
        };

        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        _canvas.MouseWheel += Canvas_MouseWheel;
        _canvas.Paint += Canvas_Paint;

        _canvasPanel.Controls.Add(_canvas);
        Controls.Add(_canvasPanel);
    }

    private void BuildProperties()
    {
        _props = new Panel
        {
            Dock = DockStyle.Right,
            Width = 260,
            BackColor = System.Drawing.Color.FromArgb(50, 50, 55),
        };

        _propTitle = new Label
        {
            Text = "Select an object",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = System.Drawing.Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Padding = new Padding(8),
        };

        _propFields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8),
        };

        _props.Controls.Add(_propFields);
        _props.Controls.Add(_propTitle);
        Controls.Add(_props);
    }

    private void BuildStatus()
    {
        var status = new StatusStrip();
        var label = new ToolStripStatusLabel("Ready");
        status.Items.Add(label);
        Controls.Add(status);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        _objects.Clear();
        AddDefaultObjects();
        RebuildProperties();
        _canvas.Invalidate();
    }

    private void AddDefaultObjects()
    {
        _objects.Add(new PlayerObject { Position = new Vector2(100, 400), Selected = true });
        _selected = _objects[0];

        _objects.Add(new PlatformObject { Position = new Vector2(800, 620), W = 1600, H = 40 });
        _objects.Add(new PlatformObject { Position = new Vector2(400, 480), W = 200, H = 20 });
        _objects.Add(new PlatformObject { Position = new Vector2(700, 400), W = 200, H = 20 });
        _objects.Add(new PlatformObject { Position = new Vector2(1000, 320), W = 200, H = 20 });
        _objects.Add(new CoinObject { Position = new Vector2(150, 420) });
        _objects.Add(new CoinObject { Position = new Vector2(450, 440) });
        _objects.Add(new CoinObject { Position = new Vector2(750, 360) });
        _objects.Add(new SpikeObject { Position = new Vector2(600, 600) });
        _objects.Add(new SpikeObject { Position = new Vector2(620, 600) });
        _objects.Add(new CheckpointObject { Position = new Vector2(800, 580) });
        _objects.Add(new GoalObject { Position = new Vector2(1500, 520) });
        _objects.Add(new MoverObject
        {
            Position = new Vector2(400, 300),
            Start = new Vector2(400, 300),
            End = new Vector2(900, 300),
            Speed = 120,
        });

        _selected = _objects[0];
        RebuildProperties();
    }

    private void SetTool(string tool)
    {
        _currentTool = tool;
        _selected = null;
        RebuildProperties();
        _canvas.Invalidate();
    }

    private void NewFile()
    {
        _scene = new SceneFile();
        _currentPath = null;
        _objects.Clear();
        AddDefaultObjects();
        Text = "FluxEngine Editor - new scene";
        _canvas.Invalidate();
    }

    private void OpenFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "FluxEngine Scene (*.scene)|*.scene|All files (*.*)|*.*",
            Title = "Open Scene",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        _scene = EditorSceneSerializer.Load(dlg.FileName);
        _currentPath = dlg.FileName;
        LoadSceneIntoObjects();
        Text = $"FluxEngine Editor - {dlg.FileName}";
        _canvas.Invalidate();
    }

    private bool SaveFile()
    {
        if (_currentPath is null)
            return SaveFileAs();

        _scene = CaptureSceneFromObjects();
        EditorSceneSerializer.Save(_currentPath, _scene);
        Text = $"FluxEngine Editor - {_currentPath}";
        return true;
    }

    private bool SaveFileAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "FluxEngine Scene (*.scene)|*.scene|All files (*.*)|*.*",
            Title = "Save Scene",
            FileName = "level.scene",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;

        _currentPath = dlg.FileName;
        _scene = CaptureSceneFromObjects();
        EditorSceneSerializer.Save(_currentPath, _scene);
        Text = $"FluxEngine Editor - {_currentPath}";
        return true;
    }

    // The editable objects are the live model; capture them before every save.
    private SceneFile CaptureSceneFromObjects()
        => SceneObjectSerializer.Capture(_objects, _scene);

    private void LoadSceneIntoObjects()
    {
        _objects.Clear();

        _objects.Add(new PlayerObject { Position = _scene.PlayerStart });

        foreach (var p in _scene.Platforms)
            _objects.Add(new PlatformObject
            {
                Position = new Vector2(p.X + p.W / 2, p.Y + p.H / 2),
                W = p.W,
                H = p.H,
                OneWay = p.OneWay,
            });

        foreach (var c in _scene.Coins)
            _objects.Add(new CoinObject { Position = new Vector2(c.X, c.Y) });

        foreach (var s in _scene.Spikes)
            _objects.Add(new SpikeObject { Position = new Vector2(s.X, s.Y) });

        foreach (var cp in _scene.Checkpoints)
            _objects.Add(new CheckpointObject { Position = new Vector2(cp.X, cp.Y) });

        foreach (var g in _scene.Goals)
            _objects.Add(new GoalObject { Position = new Vector2(g.X, g.Y) });

        foreach (var m in _scene.Movers)
            _objects.Add(new MoverObject
            {
                Position = new Vector2(m.StartX, m.StartY),
                Start = new Vector2(m.StartX, m.StartY),
                End = new Vector2(m.EndX, m.EndY),
                Speed = m.Speed,
            });

        _selected = _objects.Count > 0 ? _objects[0] : null;
        RebuildProperties();
    }

    private void AddObjectAt(Vector2 worldPos)
    {
        EditableObject obj = _currentTool switch
        {
            "player" => new PlayerObject { Position = worldPos },
            "platform" => new PlatformObject { Position = worldPos },
            "coin" => new CoinObject { Position = worldPos },
            "spike" => new SpikeObject { Position = worldPos },
            "checkpoint" => new CheckpointObject { Position = worldPos },
            "goal" => new GoalObject { Position = worldPos },
            "mover" => new MoverObject { Position = worldPos, Start = worldPos, End = worldPos + new Vector2(200, 0) },
            _ => new PlatformObject { Position = worldPos },
        };

        if (_gridSnap)
        {
            obj.Position = new Vector2(
                MathF.Round(obj.Position.X / _gridSize) * _gridSize,
                MathF.Round(obj.Position.Y / _gridSize) * _gridSize);
        }

        _objects.Add(obj);
        _selected = obj;
        RebuildProperties();
        _canvas.Invalidate();
    }

    private void RebuildProperties()
    {
        _propFields.Controls.Clear();

        if (_selected is null)
        {
            _propTitle.Text = "Select an object";
            return;
        }

        _propTitle.Text = $"{_selected.Label}";

        void AddField(string label, Func<float> get, Action<float> set)
        {
            var row = new Panel { Height = 28, Width = 230 };
            var lbl = new Label { Text = label, Width = 70, Height = 20, ForeColor = System.Drawing.Color.White };
            var num = new NumericUpDown
            {
                Width = 120,
                Height = 22,
                Minimum = -9999,
                Maximum = 9999,
                DecimalPlaces = 1,
                Increment = 1,
                Value = (decimal)get(),
            };
            num.ValueChanged += (_, _) => { set((float)num.Value); _canvas.Invalidate(); };
            row.Controls.Add(lbl);
            row.Controls.Add(num);
            _propFields.Controls.Add(row);
        }

        AddField("X", () => _selected.Position.X, v => _selected.Position = new Vector2(v, _selected.Position.Y));
        AddField("Y", () => _selected.Position.Y, v => _selected.Position = new Vector2(_selected.Position.X, v));

        if (_selected is PlatformObject p)
        {
            AddField("W", () => p.W, v => p.W = v);
            AddField("H", () => p.H, v => p.H = v);
            var oneWayBtn = new CheckBox { Text = "One-way", Checked = p.OneWay, Width = 100 };
            oneWayBtn.CheckedChanged += (_, _) => { p.OneWay = oneWayBtn.Checked; _canvas.Invalidate(); };
            _propFields.Controls.Add(oneWayBtn);
        }

        if (_selected is MoverObject m)
        {
            AddField("Start X", () => m.Start.X, v => m.Start = new Vector2(v, m.Start.Y));
            AddField("Start Y", () => m.Start.Y, v => m.Start = new Vector2(m.Start.X, v));
            AddField("End X", () => m.End.X, v => m.End = new Vector2(v, m.End.Y));
            AddField("End Y", () => m.End.Y, v => m.End = new Vector2(m.End.X, v));
            AddField("Speed", () => m.Speed, v => m.Speed = v);
        }

        var delBtn = new Button { Text = "Delete", Width = 100, Height = 28, BackColor = System.Drawing.Color.DarkRed, ForeColor = System.Drawing.Color.White };
        delBtn.Click += (_, _) =>
        {
            _objects.Remove(_selected!);
            _selected = _objects.Count > 0 ? _objects[0] : null;
            RebuildProperties();
            _canvas.Invalidate();
        };
        _propFields.Controls.Add(delBtn);
    }

    private Vector2 ScreenToWorld(Point screen)
    {
        return new Vector2(
            _camera.X + screen.X / _zoom,
            _camera.Y + screen.Y / _zoom);
    }

    private Point WorldToScreen(Vector2 world)
    {
        return new Point(
            (int)((world.X - _camera.X) * _zoom),
            (int)((world.Y - _camera.Y) * _zoom));
    }

    private EditableObject? PickObject(Vector2 world)
    {
        for (var i = _objects.Count - 1; i >= 0; i--)
        {
            var obj = _objects[i];
            if (obj.Bounds.Contains(world.X, world.Y))
                return obj;
        }
        return null;
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.Clear(System.Drawing.Color.FromArgb(40, 40, 45));

        if (_gridSnap)
        {
            var startX = (int)(_camera.X / _gridSize) * (int)_gridSize;
            var startY = (int)(_camera.Y / _gridSize) * (int)_gridSize;

            using var pen = new Pen(System.Drawing.Color.FromArgb(60, 60, 65), 1);
            for (var x = startX; x < _camera.X + _canvas.Width / _zoom; x += (int)_gridSize)
            {
                var sx = WorldToScreen(new Vector2(x, 0)).X;
                g.DrawLine(pen, sx, 0, sx, _canvas.Height);
            }
            for (var y = startY; y < _camera.Y + _canvas.Height / _zoom; y += (int)_gridSize)
            {
                var sy = WorldToScreen(new Vector2(0, y)).Y;
                g.DrawLine(pen, 0, sy, _canvas.Width, sy);
            }
        }

        foreach (var obj in _objects)
            obj.Draw(g, _zoom, _camera);

        if (_selected is null && !_panning)
        {
            var world = ScreenToWorld(_lastMouse);
            if (_gridSnap)
                world = new Vector2(MathF.Round(world.X / _gridSize) * _gridSize, MathF.Round(world.Y / _gridSize) * _gridSize);

            var screen = WorldToScreen(world);
            using var pen = new Pen(System.Drawing.Color.White, 1);
            g.DrawLine(pen, screen.X - 8, screen.Y, screen.X + 8, screen.Y);
            g.DrawLine(pen, screen.X, screen.Y - 8, screen.X, screen.Y + 8);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        _lastMouse = e.Location;

        if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Space))
        {
            _panning = true;
            _canvas.Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        var world = ScreenToWorld(e.Location);

        if (_gridSnap)
            world = new Vector2(MathF.Round(world.X / _gridSize) * _gridSize, MathF.Round(world.Y / _gridSize) * _gridSize);

        var picked = PickObject(world);

        if (picked is not null)
        {
            _selected = picked;
            _dragging = true;
            _dragObject = picked;
            _dragStart = picked.Position;
            RebuildProperties();
        }
        else
        {
            AddObjectAt(world);
            _dragging = false;
        }

        _canvas.Invalidate();
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        _lastMouse = e.Location;

        if (_panning)
        {
            var delta = new Point(e.X - _lastMouse.X, e.Y - _lastMouse.Y);
            _camera = new Vector2(_camera.X - delta.X / _zoom, _camera.Y - delta.Y / _zoom);
            _lastMouse = e.Location;
            _canvas.Invalidate();
            return;
        }

        if (_dragging && _dragObject is not null && e.Button == MouseButtons.Left)
        {
            var world = ScreenToWorld(e.Location);
            if (_gridSnap)
                world = new Vector2(MathF.Round(world.X / _gridSize) * _gridSize, MathF.Round(world.Y / _gridSize) * _gridSize);

            var offset = world - _dragStart;
            _dragObject.Position = _dragStart + offset;
            _canvas.Invalidate();
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _panning = false;
        _dragging = false;
        _dragObject = null;
        _canvas.Cursor = Cursors.Default;
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        var oldZoom = _zoom;
        if (e.Delta > 0)
            _zoom = Math.Min(4f, _zoom + 0.25f);
        else
            _zoom = Math.Max(0.25f, _zoom - 0.25f);

        if (_zoom != oldZoom)
        {
            var mouseWorld = ScreenToWorld(e.Location);
            var newMouseWorld = ScreenToWorld(e.Location);
            _camera = new Vector2(
                _camera.X + (mouseWorld.X - newMouseWorld.X),
                _camera.Y + (mouseWorld.Y - newMouseWorld.Y));
            _canvas.Invalidate();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_currentPath is not null)
        {
            _scene = CaptureSceneFromObjects();
            EditorSceneSerializer.Save(_currentPath, _scene);
        }
    }
}
