using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;

// HUD and overlay screens for the infinite platformer. Draws score, distance,
// best score, survival time, plus start/pause/game-over screens using the
// pixel font.
public sealed class GameUI : Component
{
    private readonly BitmapFont _font;
    private readonly InputActions _actions;

    public int Score { get; set; }
    public float Distance { get; set; }
    public float BestDistance { get; set; }
    public float SurvivalTime { get; set; }

    private bool _showStart = true;
    private bool _paused;
    private bool _gameOver;
    private float _flashTimer;

    public GameUI(InputActions actions, BitmapFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _actions = actions;
        _font = font;
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
        renderer.Camera = null;

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

        _font.Draw(renderer, scoreText, new Vector2(24, 20), Color.White, 2f);
        _font.Draw(renderer, distText, new Vector2(24, 46), Color.White, 2f);
        _font.Draw(renderer, timeText, new Vector2(24, 72), Color.White, 2f);

        if (BestDistance >= 0)
        {
            var bestText = $"BEST  {BestDistance:F0}M";
            var bestWidth = _font.MeasureWidth(bestText, 2f);
            _font.Draw(renderer, bestText, new Vector2(960 - bestWidth - 24, 20), Color.White, 2f);
        }
    }

    private void DrawStartScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(15, 22, 38, 220));

        var title = "FLUX RUNNER";
        var titleWidth = _font.MeasureWidth(title, 4f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 170), Color.White, 4f);

        var sub = "INFINITE PLATFORMER";
        var subWidth = _font.MeasureWidth(sub, 2f);
        _font.Draw(renderer, sub, new Vector2(480 - subWidth / 2, 240), new Color(255, 218, 150), 2f);

        var hint = "PRESS SPACE OR GAMEPAD A TO START";
        var hintWidth = _font.MeasureWidth(hint, 1.6f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 430), Color.White, 1.6f);

        var controls = "A/D MOVE  SPACE/A JUMP  ESC/START PAUSE  R RESTART";
        var ctrlWidth = _font.MeasureWidth(controls, 1.4f);
        _font.Draw(renderer, controls, new Vector2(480 - ctrlWidth / 2, 510), new Color(185, 220, 255), 1.4f);
    }

    private void DrawPauseScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(0, 0, 0, 190));

        var title = "PAUSED";
        var titleWidth = _font.MeasureWidth(title, 4f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 280), Color.White, 4f);

        var hint = "PRESS ESC OR GAMEPAD START TO RESUME";
        var hintWidth = _font.MeasureWidth(hint, 1.6f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 370), Color.White, 1.6f);
    }

    private void DrawGameOverScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(30, 15, 20, 225));

        var title = "GAME OVER";
        var titleWidth = _font.MeasureWidth(title, 4f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 210), new Color(255, 120, 120), 4f);

        var scoreText = $"SCORE: {Score:D6}";
        var scoreWidth = _font.MeasureWidth(scoreText, 2f);
        _font.Draw(renderer, scoreText, new Vector2(480 - scoreWidth / 2, 310), Color.White, 2f);

        var distText = $"DISTANCE: {Distance:F0}M";
        var distWidth = _font.MeasureWidth(distText, 2f);
        _font.Draw(renderer, distText, new Vector2(480 - distWidth / 2, 345), Color.White, 2f);

        if (Distance >= BestDistance && BestDistance > 0)
        {
            var newBest = "NEW BEST!";
            var bestWidth = _font.MeasureWidth(newBest, 2f);
            _font.Draw(renderer, newBest, new Vector2(480 - bestWidth / 2, 380), new Color(255, 218, 150), 2f);
        }
        else if (BestDistance > 0)
        {
            var bestText = $"BEST: {BestDistance:F0}M";
            var bestWidth = _font.MeasureWidth(bestText, 2f);
            _font.Draw(renderer, bestText, new Vector2(480 - bestWidth / 2, 380), Color.White, 2f);
        }

        var hint = "PRESS SPACE / A / R TO RESPAWN";
        var hintWidth = _font.MeasureWidth(hint, 1.6f);
        _font.Draw(renderer, hint, new Vector2(480 - hintWidth / 2, 470), Color.White, 1.6f);
    }
}
