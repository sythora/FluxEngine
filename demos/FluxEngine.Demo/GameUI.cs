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
        _font = new PixelFont(2f, Color.White);
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
        _flashTimer = 0f;
        Score = 0;
        Distance = 0;
        SurvivalTime = 0;
    }

    public override void OnUpdate(float deltaTime)
    {
        _flashTimer += deltaTime;
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
        var distText = $"DIST  {Distance:F0}M";
        var timeText = $"TIME  {SurvivalTime:F1}S";

        _font.Draw(renderer, scoreText, new Vector2(24, 20), 1.4f);
        _font.Draw(renderer, distText, new Vector2(24, 46), 1.4f);
        _font.Draw(renderer, timeText, new Vector2(24, 72), 1.4f);

        if (BestDistance >= 0)
        {
            var bestText = $"BEST  {BestDistance:F0}M";
            var bestWidth = _font.MeasureWidth(bestText, 1.4f);
            _font.Draw(renderer, bestText, new Vector2(960 - bestWidth - 24, 20), 1.4f);
        }
    }

    private void DrawStartScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(15, 22, 38, 220));

        var title = "FLUX RUNNER";
        var titleWidth = _font.MeasureWidth(title, 3.2f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 170), 3.2f);

        var sub = "INFINITE PLATFORMER";
        var subWidth = _font.MeasureWidth(sub, 1.6f);
        _font.Draw(renderer, sub, new Vector2(480 - subWidth / 2, 240), 1.6f);

        var hint = "PRESS SPACE OR GAMEPAD A TO START";
        var hintWidth = _font.MeasureWidth(hint, 1.3f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 430), 1.3f);

        var controls = "A/D MOVE  SPACE/A JUMP  ESC/START PAUSE  R RESTART";
        var ctrlWidth = _font.MeasureWidth(controls, 1.1f);
        _font.Draw(renderer, controls, new Vector2(480 - ctrlWidth / 2, 510), 1.1f);
    }

    private void DrawPauseScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(0, 0, 0, 190));

        var title = "PAUSED";
        var titleWidth = _font.MeasureWidth(title, 3f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 280), 3f);

        var hint = "PRESS ESC OR GAMEPAD START TO RESUME";
        var hintWidth = _font.MeasureWidth(hint, 1.3f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 370), 1.3f);
    }

    private void DrawGameOverScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(30, 15, 20, 225));

        var title = "GAME OVER";
        var titleWidth = _font.MeasureWidth(title, 3.2f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 210), 3.2f);

        var scoreText = $"SCORE: {Score:D6}";
        var scoreWidth = _font.MeasureWidth(scoreText, 1.5f);
        _font.Draw(renderer, scoreText, new Vector2(480 - scoreWidth / 2, 310), 1.5f);

        var distText = $"DISTANCE: {Distance:F0}M";
        var distWidth = _font.MeasureWidth(distText, 1.5f);
        _font.Draw(renderer, distText, new Vector2(480 - distWidth / 2, 345), 1.5f);

        if (Distance >= BestDistance && BestDistance > 0)
        {
            var newBest = "NEW BEST!";
            var bestWidth = _font.MeasureWidth(newBest, 1.5f);
            _font.Draw(renderer, newBest, new Vector2(480 - bestWidth / 2, 380), 1.5f);
        }
        else if (BestDistance > 0)
        {
            var bestText = $"BEST: {BestDistance:F0}M";
            var bestWidth = _font.MeasureWidth(bestText, 1.5f);
            _font.Draw(renderer, bestText, new Vector2(480 - bestWidth / 2, 380), 1.5f);
        }

        var hint = "PRESS SPACE / A / R TO RESPAWN";
        var hintWidth = _font.MeasureWidth(hint, 1.3f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 470), 1.3f);
    }
}
