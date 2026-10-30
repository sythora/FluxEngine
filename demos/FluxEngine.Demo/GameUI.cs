using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Input;
using FluxEngine.Mathematics;

// HUD and overlay screens for the infinite platformer. Draws score, distance,
// best score, survival time, plus start/pause/game-over screens using the
// pixel font.
public sealed class GameUI : Component
{
    private readonly BitmapFont _font;
    private readonly InputActions _actions;
    private readonly EngineSettings _settings;
    private int _selectedSetting;
    private bool _showSettings;
    private const int SaveSettingIndex = 11;
    private static readonly string[] SettingLabels =
    [
        "LIGHTING",
        "VIGNETTE",
        "GOD RAYS",
        "ATMOSPHERIC PARTICLES",
        "FOG",
        "SCREEN SHAKE",
        "MASTER VOLUME",
        "SFX VOLUME",
        "MUSIC VOLUME",
        "VSYNC",
        "FRAME RATE LIMIT",
        "SAVE AND RETURN",
    ];

    public int Score { get; set; }
    public float Distance { get; set; }
    public float BestDistance { get; set; }
    public float SurvivalTime { get; set; }
    public DayNightCycle? DayNight { get; set; }

    private bool _showStart = true;
    private bool _paused;
    private bool _gameOver;
    private float _flashTimer;

    public GameUI(InputActions actions, BitmapFont font, EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(settings);
        _actions = actions;
        _font = font;
        _settings = settings;
    }

    public bool IsPaused => _paused;
    public bool IsGameOver => _gameOver;
    public bool IsStartScreen => _showStart;
    public bool IsSaveSettingSelected => _selectedSetting == SaveSettingIndex;

    public void StartGame() => _showStart = false;
    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public void GameOver() => _gameOver = true;
    public void OpenSettings() => _showSettings = true;
    public void CloseSettings() => _showSettings = false;

    public void MoveSettingsSelection(int direction)
    {
        _selectedSetting = (_selectedSetting + direction + SettingLabels.Length) % SettingLabels.Length;
    }

    public void AdjustSelectedSetting(EngineSettings settings, int direction)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (direction == 0)
            return;

        switch (_selectedSetting)
        {
            case 0: settings.EnableLighting = !settings.EnableLighting; break;
            case 1: settings.EnableVignette = !settings.EnableVignette; break;
            case 2: settings.EnableGodRays = !settings.EnableGodRays; break;
            case 3: settings.EnableAtmosphericParticles = !settings.EnableAtmosphericParticles; break;
            case 4: settings.EnableFog = !settings.EnableFog; break;
            case 5: settings.EnableScreenShake = !settings.EnableScreenShake; break;
            case 6: settings.MasterVolume += direction * 0.1f; break;
            case 7: settings.SfxVolume += direction * 0.1f; break;
            case 8: settings.MusicVolume += direction * 0.1f; break;
            case 9: settings.Vsync = !settings.Vsync; break;
            case 10:
                int[] frameRates = [30, 60, 120, 0];
                var frameRateIndex = Array.IndexOf(frameRates, settings.TargetFps);
                if (frameRateIndex < 0)
                    frameRateIndex = 1;
                else
                    frameRateIndex = (frameRateIndex + Math.Sign(direction) + frameRates.Length) % frameRates.Length;
                settings.TargetFps = frameRates[frameRateIndex];
                break;
        }

        settings.NotifyChanged();
    }

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

        if (_showSettings)
        {
            DrawSettingsScreen(renderer);
            return;
        }

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

        if (DayNight is not null)
        {
            var phaseText = $"{DayNight.FormattedTime} {DayNight.Phase.ToString().ToUpperInvariant()}";
            var phaseColor = DayNight.Phase switch
            {
                DayNightPhase.Dawn => new Color(255, 190, 130),
                DayNightPhase.Day => new Color(255, 255, 180),
                DayNightPhase.Dusk => new Color(255, 140, 90),
                _ => new Color(160, 190, 255),
            };
            _font.Draw(renderer, phaseText, new Vector2(24, 98), phaseColor, 2f);
        }

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

        var settingsHint = "PRESS O OR GAMEPAD Y FOR SETTINGS";
        var settingsHintWidth = _font.MeasureWidth(settingsHint, 1.4f);
        _font.Draw(renderer, settingsHint, new Vector2(480 - settingsHintWidth / 2, 470), new Color(255, 218, 150), 1.4f);

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

        var settingsHint = "PRESS O OR GAMEPAD Y FOR SETTINGS";
        var settingsHintWidth = _font.MeasureWidth(settingsHint, 1.4f);
        _font.Draw(renderer, settingsHint, new Vector2(480 - settingsHintWidth / 2, 415), new Color(255, 218, 150), 1.4f);
    }

    private void DrawSettingsScreen(Renderer renderer)
    {
        renderer.DrawRectangle(new Rectangle(0, 0, 960, 720), new Color(8, 12, 24, 238));
        renderer.DrawRectangle(new Rectangle(90, 58, 780, 604), new Color(20, 32, 54, 250));
        renderer.DrawRectangleOutline(new Rectangle(90, 58, 780, 604), new Color(100, 150, 210));

        var title = "SETTINGS";
        var titleWidth = _font.MeasureWidth(title, 3f);
        _font.Draw(renderer, title, new Vector2(480 - titleWidth / 2, 82), Color.White, 3f);

        for (var index = 0; index < SettingLabels.Length; index++)
        {
            var y = 160 + index * 34;
            var selected = index == _selectedSetting;
            if (selected)
                renderer.DrawRectangle(new Rectangle(122, y - 5, 716, 30), new Color(62, 92, 130, 230));

            var color = selected ? new Color(255, 224, 150) : new Color(220, 232, 248);
            _font.Draw(renderer, SettingLabels[index], new Vector2(138, y), color, 1.5f);
            _font.Draw(renderer, GetSettingValue(index), new Vector2(690, y), color, 1.5f);
        }

        _font.Draw(renderer, "UP/DOWN SELECT  LEFT/RIGHT CHANGE", new Vector2(168, 590), Color.White, 1.25f);
        _font.Draw(renderer, "ENTER/A SELECT  ESC/B BACK (NO SAVE)", new Vector2(145, 615), new Color(185, 220, 255), 1.25f);
        _font.Draw(renderer, "FPS LIMIT IS USED ONLY WHEN VSYNC IS OFF", new Vector2(162, 640), new Color(185, 220, 255), 1.15f);
    }

    private string GetSettingValue(int index)
    {
        return index switch
        {
            0 => OnOff(_settings.EnableLighting),
            1 => OnOff(_settings.EnableVignette),
            2 => OnOff(_settings.EnableGodRays),
            3 => OnOff(_settings.EnableAtmosphericParticles),
            4 => OnOff(_settings.EnableFog),
            5 => OnOff(_settings.EnableScreenShake),
            6 => $"{_settings.MasterVolume * 100:000}%",
            7 => $"{_settings.SfxVolume * 100:000}%",
            8 => $"{_settings.MusicVolume * 100:000}%",
            9 => OnOff(_settings.Vsync),
            10 => _settings.TargetFps == 0 ? "UNLIMITED" : $"{_settings.TargetFps} FPS",
            _ => "ENTER",
        };
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";

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
