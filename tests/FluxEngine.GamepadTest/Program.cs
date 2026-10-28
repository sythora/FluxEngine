using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FluxEngine.Core;
using FluxEngine.Input;
using SDL2;

SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
SDL.SDL_SetHint("SDL_JOYSTICK_RAWINPUT", "0");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI", "1");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS4", "1");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS4_RUMBLE", "1");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS5", "1");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS5_RUMBLE", "1");
SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_XBOX", "1");

using var engine = new Engine("FluxEngine Controller Input", 320, 240);
SDL.SDL_HideWindow(engine.Window.Handle);
ApplicationConfiguration.Initialize();
Application.Run(new ControllerTestForm(engine));

public enum ControllerViewMode
{
    Auto,
    PlayStation,
    Xbox,
    Nintendo,
}

internal sealed class ControllerTestForm : Form
{
    // Clean, natural dark theme inspired by Steam / modern dev tools
    private static readonly Color BgColor = Color.FromArgb(28, 30, 36);
    private static readonly Color SurfaceColor = Color.FromArgb(38, 41, 49);
    private static readonly Color SurfaceHover = Color.FromArgb(48, 52, 63);
    private static readonly Color ShellFill = Color.FromArgb(44, 48, 58);
    private static readonly Color ShellOutline = Color.FromArgb(68, 74, 89);
    private static readonly Color TextPrimary = Color.FromArgb(240, 243, 246);
    private static readonly Color TextSecondary = Color.FromArgb(148, 155, 168);
    private static readonly Color AccentActive = Color.FromArgb(59, 130, 246); // Blue
    private static readonly Color HeldColor = Color.FromArgb(245, 158, 11);    // Amber
    private static readonly Color PassColor = Color.FromArgb(34, 197, 94);     // Green
    private static readonly Color BorderSubtle = Color.FromArgb(54, 58, 70);

    private readonly Engine _engine;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly HashSet<GamepadButton> _passedButtons = [];
    private readonly HashSet<GamepadAxis> _passedAxes = [];
    private readonly Dictionary<GamepadButton, string> _testedLabels = [];

    private ControllerViewMode _viewMode = ControllerViewMode.Auto;
    private string _rumbleMessage = "Ready - Press Space or click 'Test Rumble'";
    private int _rumbleCooldown;
    private int _touchpadClicks;

    // Interactive button rects in virtual coordinates (960 x 680)
    private readonly Dictionary<ControllerViewMode, RectangleF> _tabRects = new()
    {
        [ControllerViewMode.Auto] = new RectangleF(450, 18, 110, 32),
        [ControllerViewMode.PlayStation] = new RectangleF(568, 18, 112, 32),
        [ControllerViewMode.Xbox] = new RectangleF(688, 18, 86, 32),
        [ControllerViewMode.Nintendo] = new RectangleF(782, 18, 92, 32),
    };

    private readonly RectangleF _rumbleBtnRect = new(710, 574, 130, 34);
    private readonly RectangleF _resetBtnRect = new(848, 574, 76, 34);

    public ControllerTestForm(Engine engine)
    {
        _engine = engine;
        Text = "Controller Test";
        ClientSize = new Size(960, 680);
        MinimumSize = new Size(800, 600);
        BackColor = BgColor;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;

        KeyDown += HandleKeyDown;
        MouseDown += HandleMouseDown;

        _timer.Tick += (_, _) => PollController();
        _timer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    private ControllerViewMode CurrentLayout
    {
        get
        {
            if (_viewMode != ControllerViewMode.Auto)
                return _viewMode;

            var pad = _engine.Input.FirstGamepad;
            if (pad is null)
                return ControllerViewMode.Xbox;

            if (pad.IsPlayStation)
                return ControllerViewMode.PlayStation;
            if (pad.IsNintendo)
                return ControllerViewMode.Nintendo;
            return ControllerViewMode.Xbox;
        }
    }

    private void TriggerRumble()
    {
        var pad = _engine.Input.FirstGamepad;
        if (pad is null)
        {
            _rumbleMessage = "No controller connected to rumble";
            Invalidate();
            return;
        }

        // Full max strength (1.0f = 65535 raw) on both motors for strong feedback
        var success = _engine.Window.PlayControllerRumble(pad.InstanceId, 1.0f, 600);
        _rumbleMessage = success
            ? $"Vibrating {pad.Name} at 100% power"
            : "Vibration not supported by this driver/connection";
        _rumbleCooldown = 25; // Visual flash
        Invalidate();
    }

    private void ResetHistory()
    {
        _passedButtons.Clear();
        _passedAxes.Clear();
        _testedLabels.Clear();
        _touchpadClicks = 0;
        _rumbleMessage = "Test history reset";
        Invalidate();
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        var scale = Math.Min(ClientSize.Width / 960f, ClientSize.Height / 680f);
        var offsetX = (ClientSize.Width - 960 * scale) / 2f;
        var offsetY = (ClientSize.Height - 680 * scale) / 2f;
        var vx = (e.X - offsetX) / scale;
        var vy = (e.Y - offsetY) / scale;

        foreach (var (mode, rect) in _tabRects)
        {
            if (rect.Contains(vx, vy))
            {
                _viewMode = mode;
                Invalidate();
                return;
            }
        }

        if (_rumbleBtnRect.Contains(vx, vy))
        {
            TriggerRumble();
            return;
        }

        if (_resetBtnRect.Contains(vx, vy))
        {
            ResetHistory();
            return;
        }
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            TriggerRumble();
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.R or Keys.Back or Keys.Delete)
        {
            ResetHistory();
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.D1 or Keys.NumPad1)
        {
            _viewMode = ControllerViewMode.Auto;
            Invalidate();
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.D2 or Keys.NumPad2)
        {
            _viewMode = ControllerViewMode.PlayStation;
            Invalidate();
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.D3 or Keys.NumPad3)
        {
            _viewMode = ControllerViewMode.Xbox;
            Invalidate();
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.D4 or Keys.NumPad4)
        {
            _viewMode = ControllerViewMode.Nintendo;
            Invalidate();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Tab)
        {
            _viewMode = (ControllerViewMode)(((int)_viewMode + 1) % 4);
            Invalidate();
            e.Handled = true;
        }
    }

    private void PollController()
    {
        _engine.Window.BeginFrame();
        var pad = _engine.Input.FirstGamepad;

        if (_rumbleCooldown > 0)
        {
            _rumbleCooldown--;
            if (_rumbleCooldown == 0 && _rumbleMessage.StartsWith("Vibrating"))
                _rumbleMessage = "Ready - Press Space or click 'Test Rumble'";
        }

        if (pad is not null)
        {
            var layout = CurrentLayout;
            foreach (var button in Enum.GetValues<GamepadButton>())
            {
                if (pad.WasButtonPressed(button) || pad.IsButtonDown(button))
                {
                    if (_passedButtons.Add(button))
                        _testedLabels[button] = FormatButtonName(layout, button);
                }

                if (pad.WasButtonPressed(button) && layout == ControllerViewMode.PlayStation && button == GamepadButton.Touchpad)
                {
                    _touchpadClicks++;
                }
            }

            foreach (var axis in Enum.GetValues<GamepadAxis>())
            {
                var val = pad.GetAxis(axis);
                if (axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger)
                {
                    if (val > 0.10f)
                        _passedAxes.Add(axis);
                }
                else if (MathF.Abs(val) > 0.20f)
                {
                    _passedAxes.Add(axis);
                }
            }
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        var scale = Math.Min(ClientSize.Width / 960f, ClientSize.Height / 680f);
        g.TranslateTransform((ClientSize.Width - 960 * scale) / 2, (ClientSize.Height - 680 * scale) / 2);
        g.ScaleTransform(scale, scale);

        var pad = _engine.Input.FirstGamepad;
        var layout = CurrentLayout;

        DrawHeader(g, pad, layout);

        if (layout == ControllerViewMode.PlayStation)
            DrawPlayStation(g, pad);
        else if (layout == ControllerViewMode.Nintendo)
            DrawNintendo(g, pad);
        else
            DrawXbox(g, pad);

        DrawReadoutsAndActions(g, pad, layout);
    }

    private void DrawHeader(Graphics g, GamepadDevice? pad, ControllerViewMode layout)
    {
        using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Point);
        using var titleBrush = new SolidBrush(TextPrimary);
        g.DrawString("Controller Test", titleFont, titleBrush, 36, 16);

        // Status pill
        var isConnected = pad is not null;
        var statusColor = isConnected ? PassColor : Color.FromArgb(156, 163, 175);
        using var dotBrush = new SolidBrush(statusColor);
        g.FillEllipse(dotBrush, 38, 48, 8, 8);

        using var statusFont = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var statusBrush = new SolidBrush(isConnected ? TextPrimary : TextSecondary);
        var statusText = isConnected
            ? $"{pad!.Name}  •  {pad.Type} layout detected"
            : "No controller detected (connect via USB or Bluetooth)";
        g.DrawString(statusText, statusFont, statusBrush, 52, 43);

        // Layout Tabs
        using var tabFont = new Font("Segoe UI", 9, FontStyle.Regular, GraphicsUnit.Point);
        foreach (var (mode, rect) in _tabRects)
        {
            var isSelected = _viewMode == mode;
            var text = mode switch
            {
                ControllerViewMode.Auto => _viewMode == ControllerViewMode.Auto ? $"Auto ({layout})" : "Auto",
                ControllerViewMode.PlayStation => "PlayStation",
                ControllerViewMode.Xbox => "Xbox",
                ControllerViewMode.Nintendo => "Nintendo",
                _ => mode.ToString(),
            };

            using var bg = new SolidBrush(isSelected ? AccentActive : SurfaceColor);
            using var border = new Pen(isSelected ? AccentActive : BorderSubtle, 1);
            g.FillRoundedRectangle(bg, rect, 5);
            g.DrawRoundedRectangle(border, rect, 5);

            using var fontBrush = new SolidBrush(isSelected ? Color.White : TextSecondary);
            var size = g.MeasureString(text, tabFont);
            g.DrawString(text, tabFont, fontBrush, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
        }

        using var sepPen = new Pen(BorderSubtle, 1);
        g.DrawLine(sepPen, 36, 68, 924, 68);
    }

    private void DrawPlayStation(Graphics g, GamepadDevice? pad)
    {
        // Simple, clean controller silhouette
        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddBezier(210, 260, 215, 210, 255, 195, 305, 198);
        path.AddBezier(305, 198, 350, 202, 395, 225, 480, 225);
        path.AddLine(480, 225, 480, 225);
        path.AddBezier(480, 225, 565, 225, 610, 202, 655, 198);
        path.AddBezier(655, 198, 705, 195, 745, 210, 750, 260);
        path.AddLine(750, 260, 730, 375);
        path.AddBezier(730, 375, 720, 435, 685, 455, 655, 445);
        path.AddBezier(655, 445, 625, 435, 600, 390, 580, 345);
        path.AddLine(580, 345, 380, 345);
        path.AddBezier(380, 345, 360, 390, 335, 435, 305, 445);
        path.AddBezier(305, 445, 275, 455, 240, 435, 230, 375);
        path.CloseFigure();

        using var fill = new SolidBrush(ShellFill);
        using var stroke = new Pen(ShellOutline, 1.5f);
        g.FillPath(fill, path);
        g.DrawPath(stroke, path);

        // Triggers and Bumpers
        DrawTriggerBar(g, pad, GamepadAxis.LeftTrigger, "L2", new RectangleF(225, 142, 90, 24));
        DrawBumper(g, pad, GamepadButton.LeftShoulder, "L1", new RectangleF(225, 172, 90, 28));

        DrawTriggerBar(g, pad, GamepadAxis.RightTrigger, "R2", new RectangleF(645, 142, 90, 24));
        DrawBumper(g, pad, GamepadButton.RightShoulder, "R1", new RectangleF(645, 172, 90, 28));

        // D-Pad (Top-Left)
        DrawDpad(g, pad, new PointF(295, 285));

        // Symmetrical Sticks (Bottom)
        DrawStick(g, pad, true, new PointF(395, 380), "L3");
        DrawStick(g, pad, false, new PointF(565, 380), "R3");

        // Face Buttons (Top-Right)
        DrawFaceButton(g, pad, GamepadButton.Y, "△", new PointF(665, 245), Color.FromArgb(52, 211, 153));
        DrawFaceButton(g, pad, GamepadButton.B, "○", new PointF(705, 285), Color.FromArgb(248, 113, 113));
        DrawFaceButton(g, pad, GamepadButton.A, "✕", new PointF(665, 325), Color.FromArgb(96, 165, 250));
        DrawFaceButton(g, pad, GamepadButton.X, "□", new PointF(625, 285), Color.FromArgb(244, 114, 182));

        // Center: Touchpad + Share + Options + PS + Mute
        DrawRectButton(g, pad, GamepadButton.Touchpad, "Touchpad", new RectangleF(410, 215, 140, 50));
        DrawRectButton(g, pad, GamepadButton.Back, "Share", new RectangleF(355, 265, 48, 26));
        DrawRectButton(g, pad, GamepadButton.Start, "Options", new RectangleF(557, 265, 52, 26));
        DrawCircleButton(g, pad, GamepadButton.Guide, "PS", new PointF(480, 325), 32);
        DrawRectButton(g, pad, GamepadButton.Misc1, "Mute", new RectangleF(460, 358, 40, 20));
    }

    private void DrawXbox(Graphics g, GamepadDevice? pad)
    {
        // Authentic Xbox asymmetrical shell
        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddBezier(205, 250, 210, 195, 255, 180, 310, 185);
        path.AddBezier(310, 185, 360, 190, 420, 215, 480, 215);
        path.AddLine(480, 215, 480, 215);
        path.AddBezier(480, 215, 540, 215, 600, 190, 650, 185);
        path.AddBezier(650, 185, 705, 180, 750, 195, 755, 250);
        path.AddLine(755, 250, 735, 385);
        path.AddBezier(735, 385, 725, 445, 685, 465, 650, 455);
        path.AddBezier(650, 455, 615, 445, 580, 395, 560, 345);
        path.AddLine(560, 345, 400, 345);
        path.AddBezier(400, 345, 380, 395, 345, 445, 310, 455);
        path.AddBezier(310, 455, 275, 465, 235, 445, 225, 385);
        path.CloseFigure();

        using var fill = new SolidBrush(ShellFill);
        using var stroke = new Pen(ShellOutline, 1.5f);
        g.FillPath(fill, path);
        g.DrawPath(stroke, path);

        // Triggers and Bumpers
        DrawTriggerBar(g, pad, GamepadAxis.LeftTrigger, "LT", new RectangleF(225, 136, 90, 24));
        DrawBumper(g, pad, GamepadButton.LeftShoulder, "LB", new RectangleF(225, 166, 90, 28));

        DrawTriggerBar(g, pad, GamepadAxis.RightTrigger, "RT", new RectangleF(645, 136, 90, 24));
        DrawBumper(g, pad, GamepadButton.RightShoulder, "RB", new RectangleF(645, 166, 90, 28));

        // Asymmetrical: Left Stick at Top-Left!
        DrawStick(g, pad, true, new PointF(295, 265), "LS");

        // D-Pad at Bottom-Left!
        DrawDpad(g, pad, new PointF(370, 385));

        // Right Stick at Bottom-Right!
        DrawStick(g, pad, false, new PointF(585, 385), "RS");

        // Face Buttons at Top-Right (A, B, X, Y)
        DrawFaceButton(g, pad, GamepadButton.Y, "Y", new PointF(665, 225), Color.FromArgb(251, 191, 36));
        DrawFaceButton(g, pad, GamepadButton.B, "B", new PointF(705, 265), Color.FromArgb(239, 68, 68));
        DrawFaceButton(g, pad, GamepadButton.A, "A", new PointF(665, 305), Color.FromArgb(34, 197, 94));
        DrawFaceButton(g, pad, GamepadButton.X, "X", new PointF(625, 265), Color.FromArgb(59, 130, 246));

        // Center: Xbox Guide + View + Menu + Share
        DrawCircleButton(g, pad, GamepadButton.Guide, "Xbox", new PointF(480, 235), 42);
        DrawRectButton(g, pad, GamepadButton.Back, "View", new RectangleF(415, 280, 48, 26));
        DrawRectButton(g, pad, GamepadButton.Start, "Menu", new RectangleF(497, 280, 48, 26));
        DrawRectButton(g, pad, GamepadButton.Misc1, "Share", new RectangleF(462, 320, 36, 20));
    }

    private void DrawNintendo(Graphics g, GamepadDevice? pad)
    {
        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddBezier(205, 250, 210, 195, 255, 180, 310, 185);
        path.AddBezier(310, 185, 360, 190, 420, 215, 480, 215);
        path.AddLine(480, 215, 480, 215);
        path.AddBezier(480, 215, 540, 215, 600, 190, 650, 185);
        path.AddBezier(650, 185, 705, 180, 750, 195, 755, 250);
        path.AddLine(755, 250, 735, 385);
        path.AddBezier(735, 385, 725, 445, 685, 465, 650, 455);
        path.AddBezier(650, 455, 615, 445, 580, 395, 560, 345);
        path.AddLine(560, 345, 400, 345);
        path.AddBezier(400, 345, 380, 395, 345, 445, 310, 455);
        path.AddBezier(310, 455, 275, 465, 235, 445, 225, 385);
        path.CloseFigure();

        using var fill = new SolidBrush(ShellFill);
        using var stroke = new Pen(ShellOutline, 1.5f);
        g.FillPath(fill, path);
        g.DrawPath(stroke, path);

        DrawTriggerBar(g, pad, GamepadAxis.LeftTrigger, "ZL", new RectangleF(225, 136, 90, 24));
        DrawBumper(g, pad, GamepadButton.LeftShoulder, "L", new RectangleF(225, 166, 90, 28));

        DrawTriggerBar(g, pad, GamepadAxis.RightTrigger, "ZR", new RectangleF(645, 136, 90, 24));
        DrawBumper(g, pad, GamepadButton.RightShoulder, "R", new RectangleF(645, 166, 90, 28));

        DrawStick(g, pad, true, new PointF(295, 265), "L");
        DrawDpad(g, pad, new PointF(370, 385));
        DrawStick(g, pad, false, new PointF(585, 385), "R");

        // Nintendo layout: X top, A right, B bottom, Y left
        DrawFaceButton(g, pad, GamepadButton.Y, "X", new PointF(665, 225), Color.FromArgb(156, 163, 175));
        DrawFaceButton(g, pad, GamepadButton.B, "A", new PointF(705, 265), Color.FromArgb(156, 163, 175));
        DrawFaceButton(g, pad, GamepadButton.A, "B", new PointF(665, 305), Color.FromArgb(156, 163, 175));
        DrawFaceButton(g, pad, GamepadButton.X, "Y", new PointF(625, 265), Color.FromArgb(156, 163, 175));

        DrawRectButton(g, pad, GamepadButton.Back, "−", new RectangleF(415, 265, 36, 24));
        DrawRectButton(g, pad, GamepadButton.Start, "+", new RectangleF(509, 265, 36, 24));
        DrawCircleButton(g, pad, GamepadButton.Misc1, "Cap", new PointF(440, 305), 24);
        DrawCircleButton(g, pad, GamepadButton.Guide, "Home", new PointF(520, 305), 24);
    }

    private void DrawTriggerBar(Graphics g, GamepadDevice? pad, GamepadAxis axis, string label, RectangleF rect)
    {
        var value = Math.Clamp(pad?.GetAxis(axis) ?? 0, 0, 1);
        var isHeld = value > 0.05f;
        var isPassed = _passedAxes.Contains(axis);

        using var bg = new SolidBrush(SurfaceColor);
        using var stroke = new Pen(isHeld ? HeldColor : isPassed ? PassColor : BorderSubtle, 1);
        g.FillRoundedRectangle(bg, rect, 4);

        if (value > 0)
        {
            var fillW = (rect.Width - 4) * value;
            using var fillBrush = new SolidBrush(isHeld ? HeldColor : PassColor);
            g.FillRoundedRectangle(fillBrush, new RectangleF(rect.X + 2, rect.Y + 2, fillW, rect.Height - 4), 3);
        }

        g.DrawRoundedRectangle(stroke, rect, 4);

        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(value > 0.45f ? Color.Black : TextPrimary);
        var text = $"{label}  {(int)(value * 100)}%";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
    }

    private void DrawBumper(Graphics g, GamepadDevice? pad, GamepadButton button, string label, RectangleF rect)
    {
        var isHeld = pad?.IsButtonDown(button) == true;
        var isPassed = _passedButtons.Contains(button);

        using var bg = new SolidBrush(isHeld ? HeldColor : isPassed ? PassColor : SurfaceColor);
        using var stroke = new Pen(isHeld ? HeldColor : isPassed ? PassColor : BorderSubtle, 1);
        g.FillRoundedRectangle(bg, rect, 5);
        g.DrawRoundedRectangle(stroke, rect, 5);

        using var font = new Font("Segoe UI", 9, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(isHeld || isPassed ? Color.Black : TextPrimary);
        var size = g.MeasureString(label, font);
        g.DrawString(label, font, textBrush, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
    }

    private void DrawDpad(Graphics g, GamepadDevice? pad, PointF center)
    {
        var ox = center.X;
        var oy = center.Y;

        DrawDir(GamepadButton.DPadUp, new RectangleF(ox - 16, oy - 46, 32, 28));
        DrawDir(GamepadButton.DPadDown, new RectangleF(ox - 16, oy + 18, 32, 28));
        DrawDir(GamepadButton.DPadLeft, new RectangleF(ox - 46, oy - 16, 28, 32));
        DrawDir(GamepadButton.DPadRight, new RectangleF(ox + 18, oy - 16, 28, 32));

        using var hub = new SolidBrush(SurfaceColor);
        g.FillRectangle(hub, ox - 16, oy - 16, 32, 32);

        void DrawDir(GamepadButton btn, RectangleF rect)
        {
            var isHeld = pad?.IsButtonDown(btn) == true;
            var isPassed = _passedButtons.Contains(btn);
            using var fill = new SolidBrush(isHeld ? HeldColor : isPassed ? PassColor : SurfaceColor);
            using var border = new Pen(BorderSubtle, 1);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private void DrawStick(Graphics g, GamepadDevice? pad, bool left, PointF center, string label)
    {
        var btn = left ? GamepadButton.LeftStick : GamepadButton.RightStick;
        var axX = left ? GamepadAxis.LeftX : GamepadAxis.RightX;
        var axY = left ? GamepadAxis.LeftY : GamepadAxis.RightY;
        var x = pad?.GetAxis(axX) ?? 0;
        var y = pad?.GetAxis(axY) ?? 0;
        var isHeld = pad?.IsButtonDown(btn) == true;
        var isPassed = _passedButtons.Contains(btn) || (_passedAxes.Contains(axX) && _passedAxes.Contains(axY));

        // Outer well
        using var wellBg = new SolidBrush(Color.FromArgb(20, 22, 27));
        using var wellRing = new Pen(isHeld ? HeldColor : isPassed ? PassColor : BorderSubtle, 1.5f);
        g.FillEllipse(wellBg, center.X - 38, center.Y - 38, 76, 76);
        g.DrawEllipse(wellRing, center.X - 38, center.Y - 38, 76, 76);

        // Center crosshair
        using var crossPen = new Pen(Color.FromArgb(40, 44, 54), 1);
        g.DrawLine(crossPen, center.X - 10, center.Y, center.X + 10, center.Y);
        g.DrawLine(crossPen, center.X, center.Y - 10, center.X, center.Y + 10);

        // Stick knob
        var knobX = center.X - 14 + x * 20;
        var knobY = center.Y - 14 + y * 20;
        using var knobFill = new SolidBrush(isHeld ? HeldColor : SurfaceHover);
        using var knobBorder = new Pen(Color.FromArgb(100, 110, 130), 1.5f);
        g.FillEllipse(knobFill, knobX, knobY, 28, 28);
        g.DrawEllipse(knobBorder, knobX, knobY, 28, 28);

        using var font = new Font("Segoe UI", 8, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(isHeld ? Color.Black : TextPrimary);
        var size = g.MeasureString(label, font);
        g.DrawString(label, font, textBrush, knobX + 14 - size.Width / 2, knobY + 14 - size.Height / 2);
    }

    private void DrawFaceButton(Graphics g, GamepadDevice? pad, GamepadButton btn, string label, PointF center, Color normalColor)
    {
        var isHeld = pad?.IsButtonDown(btn) == true;
        var isPassed = _passedButtons.Contains(btn);

        using var fill = new SolidBrush(isHeld ? HeldColor : isPassed ? PassColor : SurfaceColor);
        using var border = new Pen(isHeld ? HeldColor : isPassed ? PassColor : Color.FromArgb(75, 82, 98), 1.5f);
        g.FillEllipse(fill, center.X - 16, center.Y - 16, 32, 32);
        g.DrawEllipse(border, center.X - 16, center.Y - 16, 32, 32);

        using var font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(isHeld || isPassed ? Color.Black : normalColor);
        var size = g.MeasureString(label, font);
        g.DrawString(label, font, brush, center.X - size.Width / 2, center.Y - size.Height / 2 - 1);
    }

    private void DrawCircleButton(Graphics g, GamepadDevice? pad, GamepadButton btn, string label, PointF center, float diameter = 32)
    {
        var r = diameter / 2f;
        var isHeld = pad?.IsButtonDown(btn) == true;
        var isPassed = _passedButtons.Contains(btn);

        using var fill = new SolidBrush(isHeld ? HeldColor : isPassed ? PassColor : SurfaceColor);
        using var border = new Pen(isHeld ? HeldColor : isPassed ? PassColor : BorderSubtle, 1.5f);
        g.FillEllipse(fill, center.X - r, center.Y - r, diameter, diameter);
        g.DrawEllipse(border, center.X - r, center.Y - r, diameter, diameter);

        using var font = new Font("Segoe UI", diameter > 36 ? 8.5f : 7.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(isHeld || isPassed ? Color.Black : TextPrimary);
        var size = g.MeasureString(label, font);
        g.DrawString(label, font, brush, center.X - size.Width / 2, center.Y - size.Height / 2);
    }

    private void DrawRectButton(Graphics g, GamepadDevice? pad, GamepadButton btn, string label, RectangleF rect)
    {
        var isHeld = pad?.IsButtonDown(btn) == true;
        var isPassed = _passedButtons.Contains(btn);

        using var fill = new SolidBrush(isHeld ? HeldColor : isPassed ? PassColor : SurfaceColor);
        using var border = new Pen(isHeld ? HeldColor : isPassed ? PassColor : BorderSubtle, 1);
        g.FillRoundedRectangle(fill, rect, 5);
        g.DrawRoundedRectangle(border, rect, 5);

        using var font = new Font("Segoe UI", rect.Height > 30 ? 9 : 8, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(isHeld || isPassed ? Color.Black : TextPrimary);
        var size = g.MeasureString(label, font);
        g.DrawString(label, font, brush, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
    }

    private void DrawReadoutsAndActions(Graphics g, GamepadDevice? pad, ControllerViewMode layout)
    {
        using var sep = new Pen(BorderSubtle, 1);
        g.DrawLine(sep, 36, 490, 924, 490);

        var lx = pad?.GetAxis(GamepadAxis.LeftX) ?? 0;
        var ly = pad?.GetAxis(GamepadAxis.LeftY) ?? 0;
        var rx = pad?.GetAxis(GamepadAxis.RightX) ?? 0;
        var ry = pad?.GetAxis(GamepadAxis.RightY) ?? 0;
        var lt = pad?.GetAxis(GamepadAxis.LeftTrigger) ?? 0;
        var rt = pad?.GetAxis(GamepadAxis.RightTrigger) ?? 0;

        using var monoFont = new Font("Consolas", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var labelFont = new Font("Segoe UI", 9, FontStyle.Regular, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(TextPrimary);
        using var mutedBrush = new SolidBrush(TextSecondary);

        // Sticks and Triggers Readouts
        g.DrawString($"Left Stick:   X: {lx:+0.00;-0.00; 0.00}   Y: {ly:+0.00;-0.00; 0.00}", monoFont, textBrush, 36, 502);
        g.DrawString($"Right Stick:  X: {rx:+0.00;-0.00; 0.00}   Y: {ry:+0.00;-0.00; 0.00}", monoFont, textBrush, 36, 524);
        g.DrawString($"Triggers:     Left: {(int)(lt * 100),3}%   Right: {(int)(rt * 100),3}%", monoFont, textBrush, 36, 546);

        // Tested Controls Summary
        var testedStr = _testedLabels.Count == 0
            ? "No inputs tested yet"
            : "Tested: " + string.Join(", ", _testedLabels.OrderBy(p => p.Key).Select(p => p.Value));
        g.DrawString(testedStr, labelFont, mutedBrush, 36, 574);

        if (layout == ControllerViewMode.PlayStation && _touchpadClicks > 0)
        {
            g.DrawString($"Touchpad clicks: {_touchpadClicks}", labelFont, textBrush, 36, 596);
        }

        // Action Buttons
        // Test Rumble Button
        using var rumbleBg = new SolidBrush(_rumbleCooldown > 0 ? HeldColor : SurfaceColor);
        using var rumbleBorder = new Pen(_rumbleCooldown > 0 ? HeldColor : BorderSubtle, 1);
        g.FillRoundedRectangle(rumbleBg, _rumbleBtnRect, 5);
        g.DrawRoundedRectangle(rumbleBorder, _rumbleBtnRect, 5);

        using var btnFont = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var rumbleTextBrush = new SolidBrush(_rumbleCooldown > 0 ? Color.Black : TextPrimary);
        var rSize = g.MeasureString("Test Rumble", btnFont);
        g.DrawString("Test Rumble", btnFont, rumbleTextBrush,
            _rumbleBtnRect.X + (_rumbleBtnRect.Width - rSize.Width) / 2,
            _rumbleBtnRect.Y + (_rumbleBtnRect.Height - rSize.Height) / 2);

        // Reset Button
        using var resetBg = new SolidBrush(SurfaceColor);
        using var resetBorder = new Pen(BorderSubtle, 1);
        g.FillRoundedRectangle(resetBg, _resetBtnRect, 5);
        g.DrawRoundedRectangle(resetBorder, _resetBtnRect, 5);

        using var resetTextBrush = new SolidBrush(TextPrimary);
        var clrSize = g.MeasureString("Reset", btnFont);
        g.DrawString("Reset", btnFont, resetTextBrush,
            _resetBtnRect.X + (_resetBtnRect.Width - clrSize.Width) / 2,
            _resetBtnRect.Y + (_resetBtnRect.Height - clrSize.Height) / 2);

        // Rumble status line
        using var statusFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        g.DrawString(_rumbleMessage, statusFont, mutedBrush, 500, 552);

        // Footer hints
        g.DrawString("Shortcuts:  Space = Rumble  •  R = Reset  •  1-4 = Switch Layout", statusFont, mutedBrush, 36, 626);
    }

    private static string FormatButtonName(ControllerViewMode layout, GamepadButton btn)
    {
        if (layout == ControllerViewMode.PlayStation)
        {
            return btn switch
            {
                GamepadButton.A => "Cross",
                GamepadButton.B => "Circle",
                GamepadButton.X => "Square",
                GamepadButton.Y => "Triangle",
                GamepadButton.Back => "Share",
                GamepadButton.Start => "Options",
                GamepadButton.LeftShoulder => "L1",
                GamepadButton.RightShoulder => "R1",
                GamepadButton.LeftStick => "L3",
                GamepadButton.RightStick => "R3",
                GamepadButton.Touchpad => "Touchpad",
                GamepadButton.Guide => "PS",
                GamepadButton.Misc1 => "Mute",
                GamepadButton.DPadUp => "Up",
                GamepadButton.DPadDown => "Down",
                GamepadButton.DPadLeft => "Left",
                GamepadButton.DPadRight => "Right",
                _ => btn.ToString(),
            };
        }

        if (layout == ControllerViewMode.Nintendo)
        {
            return btn switch
            {
                GamepadButton.A => "B",
                GamepadButton.B => "A",
                GamepadButton.X => "Y",
                GamepadButton.Y => "X",
                GamepadButton.Back => "Minus",
                GamepadButton.Start => "Plus",
                GamepadButton.LeftShoulder => "L",
                GamepadButton.RightShoulder => "R",
                GamepadButton.LeftStick => "Left Stick",
                GamepadButton.RightStick => "Right Stick",
                GamepadButton.Guide => "Home",
                GamepadButton.Misc1 => "Capture",
                GamepadButton.DPadUp => "Up",
                GamepadButton.DPadDown => "Down",
                GamepadButton.DPadLeft => "Left",
                GamepadButton.DPadRight => "Right",
                _ => btn.ToString(),
            };
        }

        return btn switch
        {
            GamepadButton.A => "A",
            GamepadButton.B => "B",
            GamepadButton.X => "X",
            GamepadButton.Y => "Y",
            GamepadButton.Back => "View",
            GamepadButton.Start => "Menu",
            GamepadButton.LeftShoulder => "LB",
            GamepadButton.RightShoulder => "RB",
            GamepadButton.LeftStick => "Left Stick",
            GamepadButton.RightStick => "Right Stick",
            GamepadButton.Guide => "Xbox",
            GamepadButton.Misc1 => "Share",
            GamepadButton.DPadUp => "Up",
            GamepadButton.DPadDown => "Down",
            GamepadButton.DPadLeft => "Left",
            GamepadButton.DPadRight => "Right",
            _ => btn.ToString(),
        };
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rect, float radius)
    {
        using var path = CreateRoundedPath(rect, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF rect, float radius)
    {
        using var path = CreateRoundedPath(rect, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
