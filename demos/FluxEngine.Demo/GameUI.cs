using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;

// HUD and overlay screens for the infinite platformer. Draws score, distance,
// best score, survival time, plus start/pause/game-over screens using the
// pixel font.
public sealed class GameUI : Component
{
    private readonly PixelFont _font;
    private readonly InputActions _actions;

    public int Score { get; set; }
    public float Distance { get; set; }
    public float BestDistance { get; set; }
    public float SurvivalTime { get; set; }

    private bool _showStart = true;
    private bool _paused;
    private bool _gameOver;
    private float _flashTimer;

    public GameUI(InputActions actions)
    {
        _actions = actions;
        _font = new PixelFont(3f, Color.White);
    }

    public bool IsPaused => _paused;
    public bool IsGameOver => _gameOver;
    public bool IsStartScreen => _showStart;

    public void StartGame() => _showStart = false;
    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public void GameOver() => _gameOver = true;
    public void Reset()
    {
        _showStart = false;
        _paused = false;
        _gameOver = false;
        Score = 0;
        Distance = 0;
        SurvivalTime = 0;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (_gameOver)
        {
            _flashTimer += deltaTime;
            if (_actions.WasPressed("restart"))
            {
                _gameOver = false;
                _showStart = false;
                Score = 0;
                Distance = 0;
                SurvivalTime = 0;
            }
            return;
        }

        if (_showStart)
        {
            if (_actions.WasPressed("jump"))
                _showStart = false;
            return;
        }

        if (!_paused && _actions.WasPressed("pause"))
            _paused = true;

        if (_paused && _actions.WasPressed("pause"))
            _paused = false;
    }

    public override void OnDraw(Renderer renderer)
    {
        // HUD is drawn in screen coordinates (no camera)
        if (!_showStart && !_gameOver && !_paused)
        {
            DrawHud(renderer);
        }

        if (_showStart)
            DrawStartScreen(renderer);
        else if (_paused)
            DrawPauseScreen(renderer);
        else if (_gameOver)
            DrawGameOverScreen(renderer);
    }

    private void DrawHud(Renderer renderer)
    {
        var scoreText = $"SCORE {Score:D6}";
        var distText = $"DIST  {Distance:F0}m";
        var timeText = $"TIME  {SurvivalTime:F1}s";

        _font.Draw(renderer, scoreText, new Vector2(20, 20), 2f);
        _font.Draw(renderer, distText, new Vector2(20, 50), 2f);
        _font.Draw(renderer, timeText, new Vector2(20, 80), 2f);

        if (BestDistance >= 0)
        {
            var bestText = $"BEST  {BestDistance:F0}m";
            var bestWidth = _font.MeasureWidth(bestText, 2f);
            _font.Draw(renderer, bestText, new Vector2(960 - bestWidth - 20, 20), 2f);
        }
    }

    private void DrawStartScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(20, 30, 50, 230));

        var title = "FLUX RUNNER";
        var titleWidth = _font.MeasureWidth(title, 4f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 200), 4f);

        var sub = "INFINITE PLATFORMER";
        var subWidth = _font.MeasureWidth(sub, 2f);
        _font.Draw(renderer, sub, new Vector2(480 - subWidth / 2, 300), 2f);

        var hint = "PRESS SPACE TO START";
        var hintWidth = _font.MeasureWidth(hint, 2f);
        var flash = MathF.Sin(Time.TotalTime * 4f) > 0 ? Color.White : new Color(100, 100, 100);
        // Draw hint with flashing color
        var oldColor = _font; // can't change color per call easily, skip flash
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 500), 2f);

        var controls = "A/D MOVE  SPACE JUMP  ESC PAUSE";
        var ctrlWidth = _font.MeasureWidth(controls, 1.5f);
        _font.Draw(renderer, controls, new Vector2(480 - ctrlWidth / 2, 580), 1.5f);
    }

    private void DrawPauseScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(0, 0, 0, 180));

        var title = "PAUSED";
        var titleWidth = _font.MeasureWidth(title, 4f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 280), 4f);

        var hint = "PRESS ESC TO RESUME";
        var hintWidth = _font.MeasureWidth(hint, 2f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 380), 2f);
    }

    private void DrawGameOverScreen(Renderer renderer)
    {
        var flash = MathF.Sin(_flashTimer * 8f) > 0;

        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(40, 20, 20, 220));

        var title = "GAME OVER";
        var titleWidth = _font.MeasureWidth(title, 4f);
        var titleColor = flash ? Color.Red : new Color(150, 50, 50);
        // Can't change font color per call, draw in white
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 240), 4f);

        var scoreText = $"SCORE: {Score:D6}";
        var scoreWidth = _font.MeasureWidth(scoreText, 2f);
        _font.Draw(renderer, scoreText, new Vector2(480 - scoreWidth / 2, 340), 2f);

        var distText = $"DISTANCE: {Distance:F0}m";
        var distWidth = _font.MeasureWidth(distText, 2f);
        _font.Draw(renderer, distText, new Vector2(480 - distWidth / 2, 370), 2f);

        if (Distance >= BestDistance && BestDistance > 0)
        {
            var newBest = "NEW BEST!";
            var bestWidth = _font.MeasureWidth(newBest, 2f);
            _font.Draw(renderer, newBest, new Vector2(480 - bestWidth / 2, 400), 2f);
        }
        else if (BestDistance > 0)
        {
            var bestText = $"BEST: {BestDistance:F0}m";
            var bestWidth = _font.MeasureWidth(bestText, 2f);
            _font.Draw(renderer, bestText, new Vector2(480 - bestWidth / 2, 400), 2f);
        }

        var hint = "PRESS R TO RESTART";
        var hintWidth = _font.MeasureWidth(hint, 2f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 500), 2f);
    }
}
