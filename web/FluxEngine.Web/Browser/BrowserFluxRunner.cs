using System.Text.Json;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Web.Browser;

public enum BrowserGameState
{
    Title,
    Dialogue,
    Playing,
    Paused,
    Settings,
    Respawning,
    GameOver,
    Ending,
}

public sealed class BrowserSaveData
{
    public int Version { get; set; } = 1;
    public int CompletedChapters { get; set; }
    public float StoryCheckpointX { get; set; } = StoryCampaign.InitialSpawnX;
    public float SegmentStartX { get; set; } = StoryCampaign.InitialSpawnX;
    public float CheckpointX { get; set; } = StoryCampaign.InitialSpawnX;
    public float CheckpointY { get; set; } = 390;
    public int Score { get; set; }
    public int Signals { get; set; }
    public int MemoryFragments { get; set; }
    public float BestDistance { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool ReducedMotion { get; set; }
    public bool HighContrast { get; set; }
    public string[] Achievements { get; set; } = [];
}

public sealed class BrowserFluxRunner
{
    public const int ViewWidth = 960;
    public const int ViewHeight = 540;

    private const float PlayerSpeed = 255f;
    private const float JumpForce = -680f;
    private const float Gravity = 1550f;
    private const int MaxLives = 3;
    private const float CheckpointSpacing = 820f;
    private static readonly string[] MemoryMessages =
    [
        "PORCH LIGHTS STAY ON.",
        "WE TAKE TURNS WATCHING.",
        "THERE IS ALWAYS ROOM AT HOME.",
    ];

    private static readonly JsonSerializerOptions SaveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Scene _scene;
    private readonly InputActions _actions;
    private readonly StoryCampaign _story = new();
    private readonly Random _random = new(74621);
    private readonly List<RoutePlatform> _platforms = [];
    private readonly List<WorldPickup> _pickups = [];
    private readonly List<WorldHazard> _hazards = [];
    private readonly List<WorldCheckpoint> _checkpoints = [];
    private readonly List<WorldParticle> _particles = [];
    private readonly List<string> _soundEvents = [];
    private readonly HashSet<string> _achievements = [];
    private readonly Entity _player;
    private readonly Rigidbody2D _body;
    private readonly BrowserPlayerController _controller;

    private BrowserGameState _returnState;
    private IReadOnlyList<string> _dialoguePages = [];
    private string _dialogueTitle = "";
    private string _toast = "";
    private string _achievementToast = "";
    private float _toastTimer;
    private float _achievementToastTimer;
    private float _checkpointX = StoryCampaign.InitialSpawnX;
    private float _checkpointY = 390;
    private float _nextPlatformRight;
    private float _lastPlatformY = 440;
    private float _nextCheckpointX;
    private float _difficultyTimer;
    private float _minGap = 58;
    private float _maxGap = 104;
    private float _invulnerabilityTimer;
    private float _respawnTimer;
    private float _saveTimer;
    private float _ambientTime;
    private float _lastSavedDistance;
    private int _platformCount;
    private int _dialoguePage;
    private int _endingPage;
    private int _settingsSelection;
    private int _score;
    private int _signals;
    private int _memoryFragments;
    private int _lives = MaxLives;
    private float _bestDistance;
    private bool _hasSavedRun;

    public BrowserFluxRunner(Scene scene, InputManager input)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(input);

        _scene = scene;
        _actions = input.Actions;
        _scene.BackgroundColor = new Color(11, 18, 36);
        _scene.Camera = new Camera { Position = new Vector2(StoryCampaign.InitialSpawnX + 140, 350) };
        _scene.Solids.Add(new BoxCollider(new Vector2(-60, 300), new Vector2(40, 2000)));

        _actions
            .Bind("move.left", Key.A, Key.Left)
            .Bind("move.right", Key.D, Key.Right)
            .Bind("settings.up", Key.W, Key.Up)
            .Bind("settings.down", Key.S, Key.Down)
            .Bind("jump", Key.Space, Key.Up, Key.Enter)
            .Bind("pause", Key.Escape)
            .Bind("settings", Key.O)
            .Bind("restart", Key.R);

        _player = _scene.Spawn("nova", new Vector2(StoryCampaign.InitialSpawnX, 390));
        _controller = _player.AddComponent(new BrowserPlayerController(_actions, () => AddSound("jump")));
        _body = _player.AddComponent(new Rigidbody2D
        {
            Size = new Vector2(18, 30),
            Gravity = new Vector2(0, Gravity),
            MaxFallSpeed = 1600,
            Drag = 0,
        });
        _player.Enabled = false;

        CreatePlatform(new Vector2(StoryCampaign.InitialSpawnX + 190, 440), 520);
        _nextPlatformRight = StoryCampaign.InitialSpawnX + 450;
        _nextCheckpointX = StoryCampaign.InitialSpawnX + CheckpointSpacing;
        _hasSavedRun = false;
        EnsureRoute();
    }

    public BrowserGameState State { get; private set; } = BrowserGameState.Title;
    public bool SoundEnabled { get; private set; } = true;
    public bool ReducedMotion { get; private set; }
    public bool HighContrast { get; private set; }
    public int Score => _score;
    public int Signals => _signals;
    public int Lives => _lives;
    public int CompletedChapters => _story.CompletedChapters;
    public float Distance => _story.Distance(_player.Position.X);
    public float ChapterProgress => _story.ChapterProgress(_player.Position.X);
    public string ChapterTitle => _story.CurrentChapterTitle;
    public string Toast => _toastTimer > 0 ? _toast : "";
    public string AchievementToast => _achievementToastTimer > 0 ? _achievementToast : "";

    public void ToggleSoundSetting() => ToggleSetting(0);

    public void LoadSave(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var data = JsonSerializer.Deserialize<BrowserSaveData>(json, SaveJsonOptions)
            ?? throw new InvalidDataException("The saved FluxRunner progress is empty.");
        if (data.Version != 1)
            throw new InvalidDataException($"Unsupported FluxRunner save version {data.Version}.");
        ValidateSave(data);

        _story.RestoreProgress(data.CompletedChapters, data.StoryCheckpointX, data.SegmentStartX);
        _checkpointX = data.CheckpointX;
        _checkpointY = data.CheckpointY;
        _score = data.Score;
        _signals = data.Signals;
        _memoryFragments = data.MemoryFragments;
        _bestDistance = data.BestDistance;
        SoundEnabled = data.SoundEnabled;
        ReducedMotion = data.ReducedMotion;
        HighContrast = data.HighContrast;
        foreach (var achievement in data.Achievements)
            _achievements.Add(achievement);

        _player.Position = new Vector2(_checkpointX, _checkpointY);
        _hasSavedRun = _score > 0 || _story.CompletedChapters > 0 ||
            _checkpointX > StoryCampaign.InitialSpawnX;
        EnsureRoute();
    }

    public string SaveToJson()
        => JsonSerializer.Serialize(new BrowserSaveData
        {
            CompletedChapters = _story.CompletedChapters,
            StoryCheckpointX = _story.CheckpointX,
            SegmentStartX = _story.SegmentStartX,
            CheckpointX = _checkpointX,
            CheckpointY = _checkpointY,
            Score = _score,
            Signals = _signals,
            MemoryFragments = _memoryFragments,
            BestDistance = _bestDistance,
            SoundEnabled = SoundEnabled,
            ReducedMotion = ReducedMotion,
            HighContrast = HighContrast,
            Achievements = _achievements.Order().ToArray(),
        });

    public void HandleInput(float deltaTime)
    {
        _ambientTime += deltaTime;
        _toastTimer = MathF.Max(0, _toastTimer - deltaTime);
        _achievementToastTimer = MathF.Max(0, _achievementToastTimer - deltaTime);
        _invulnerabilityTimer = MathF.Max(0, _invulnerabilityTimer - deltaTime);

        if (_actions.WasPressed("settings") && State is BrowserGameState.Title or BrowserGameState.Playing or BrowserGameState.Paused)
        {
            _returnState = State;
            State = BrowserGameState.Settings;
            AddSound("menu");
            return;
        }

        if (State == BrowserGameState.Settings)
        {
            HandleSettingsInput();
            return;
        }

        if (State == BrowserGameState.Title)
        {
            if (_actions.WasPressed("jump"))
            {
                if (_hasSavedRun)
                {
                    if (_story.IsComplete)
                    {
                        State = BrowserGameState.Ending;
                    }
                    else
                    {
                        State = BrowserGameState.Playing;
                        _player.Enabled = true;
                    }
                    AddSound("story");
                }
                else
                {
                    StartNewStory();
                }
            }
            return;
        }

        if (State == BrowserGameState.Dialogue)
        {
            if (_actions.WasPressed("jump"))
                AdvanceDialogue();
            return;
        }

        if (State == BrowserGameState.Playing && _actions.WasPressed("pause"))
        {
            State = BrowserGameState.Paused;
            _player.Enabled = false;
            AddSound("menu");
            return;
        }

        if (State == BrowserGameState.Paused && _actions.WasPressed("pause"))
        {
            Resume();
            return;
        }

        if (State == BrowserGameState.GameOver && (_actions.WasPressed("jump") || _actions.WasPressed("restart")))
            RestartFromCheckpoint();

        if (State == BrowserGameState.Ending && (_actions.WasPressed("jump") || _actions.WasPressed("restart")))
            AdvanceEnding();
    }

    public void HandlePointer(double x, double y)
    {
        if (State == BrowserGameState.Playing)
        {
            if (x > 850 && y < 72)
            {
                State = BrowserGameState.Paused;
                _player.Enabled = false;
                AddSound("menu");
            }
            return;
        }

        switch (State)
        {
            case BrowserGameState.Title:
                if (Inside(x, y, 480, 360, 330, 58))
                {
                    if (_hasSavedRun)
                    {
                        if (_story.IsComplete)
                        {
                            State = BrowserGameState.Ending;
                        }
                        else
                        {
                            State = BrowserGameState.Playing;
                            _player.Enabled = true;
                        }
                    }
                    else
                    {
                        StartNewStory();
                    }
                }
                else if (Inside(x, y, 480, 426, 180, 34))
                {
                    _returnState = BrowserGameState.Title;
                    State = BrowserGameState.Settings;
                }
                break;
            case BrowserGameState.Dialogue:
                if (Inside(x, y, 480, 456, 300, 58))
                    AdvanceDialogue();
                break;
            case BrowserGameState.Paused:
                if (Inside(x, y, 480, 316, 300, 52))
                    Resume();
                else if (Inside(x, y, 480, 382, 300, 52))
                {
                    _returnState = BrowserGameState.Paused;
                    State = BrowserGameState.Settings;
                }
                else if (Inside(x, y, 480, 448, 300, 52))
                    RestartFromCheckpoint();
                break;
            case BrowserGameState.Settings:
                if (Inside(x, y, 480, 300, 370, 48))
                    ToggleSetting(0);
                else if (Inside(x, y, 480, 360, 370, 48))
                    ToggleSetting(1);
                else if (Inside(x, y, 480, 420, 370, 48))
                    ToggleSetting(2);
                else if (Inside(x, y, 480, 484, 300, 46))
                    CloseSettings();
                break;
            case BrowserGameState.GameOver:
                if (Inside(x, y, 480, 390, 320, 58))
                    RestartFromCheckpoint();
                break;
            case BrowserGameState.Ending:
                if (Inside(x, y, 480, 428, 320, 58))
                    AdvanceEnding();
                break;
        }
    }

    public void Update(float deltaTime)
    {
        if (State == BrowserGameState.Respawning)
        {
            _respawnTimer -= deltaTime;
            if (_respawnTimer <= 0)
                Respawn();
            return;
        }

        if (State != BrowserGameState.Playing)
            return;

        _saveTimer += deltaTime;
        _difficultyTimer += deltaTime;
        if (_difficultyTimer >= 12f)
        {
            _difficultyTimer = 0;
            _minGap = MathF.Min(96, _minGap + 4);
            _maxGap = MathF.Min(136, _maxGap + 5);
        }

        _scene.BackgroundColor = CurrentSky;
        var target = new Vector2(_player.Position.X + 145, Math.Clamp(_player.Position.Y, 275, 420));
        _scene.Camera?.Follow(target, ReducedMotion ? 0.25f : 0.12f);
        EnsureRoute();
        CollectPickups();
        CheckHazards();
        CheckCheckpoints();
        RemoveDistantEntities();

        var distance = Distance;
        _bestDistance = MathF.Max(_bestDistance, distance);
        if (distance >= 500)
            UnlockAchievement("ROOFTOP RUNNER");
        if (_saveTimer >= 5f && distance > _lastSavedDistance + 50)
        {
            _saveTimer = 0;
            _lastSavedDistance = distance;
            SaveProgress();
        }
        if (_player.Position.Y > 690)
            TakeHit();

        if (_story.TryCompleteChapter(_player.Position.X, out var nextChapter, out var campaignComplete))
        {
            _checkpointX = _player.Position.X;
            _checkpointY = _player.Position.Y;
            if (campaignComplete)
            {
                State = BrowserGameState.Ending;
                _endingPage = 0;
                _player.Enabled = false;
                AddSound("complete");
                UnlockAchievement("CITY RESTORED");
            }
            else if (nextChapter is not null)
            {
                UnlockAchievement("BEACON KEEPER");
                ShowDialogue(nextChapter.Title, nextChapter.Pages);
                AddSound("story");
            }
            SaveProgress();
        }
    }

    public void Draw(Renderer renderer)
    {
        renderer.Camera = _scene.Camera;
        DrawWorld(renderer);
        renderer.Camera = null;
        DrawHud(renderer);
        DrawOverlay(renderer);
    }

    public string[] TakeSoundEvents()
    {
        var events = _soundEvents.ToArray();
        _soundEvents.Clear();
        return events;
    }

    private void DrawWorld(Renderer renderer)
    {
        var camera = _scene.Camera?.Position ?? Vector2.Zero;
        var left = camera.X - ViewWidth * 0.8f;
        var right = camera.X + ViewWidth * 0.8f;
        var skyTop = camera.Y - ViewHeight * 1.1f;
        renderer.DrawQuadGradient(
            new Vector2(left, skyTop),
            new Vector2(right, skyTop),
            new Vector2(left, skyTop + ViewHeight * 2.2f),
            new Vector2(right, skyTop + ViewHeight * 2.2f),
            CurrentSky,
            new Color(24, 34, 57));

        DrawStars(renderer, left, right, skyTop);
        DrawCity(renderer, left, right);
        DrawRoute(renderer);
        DrawPickups(renderer);
        DrawPlayer(renderer);
        DrawParticles(renderer);
    }

    private void DrawStars(Renderer renderer, float left, float right, float skyTop)
    {
        var first = (int)MathF.Floor(left / 74);
        var last = (int)MathF.Ceiling(right / 74);
        for (var index = first; index <= last; index++)
        {
            var seed = unchecked((uint)(index * 747796405 + 2891336453));
            seed = (seed >> ((int)(seed >> 28) + 4)) ^ seed;
            seed *= 277803737;
            seed = (seed >> 22) ^ seed;
            var x = index * 74 + (seed % 58);
            var y = skyTop + 40 + ((seed >> 8) % 206);
            var twinkle = ReducedMotion ? 0.65f : 0.5f + MathF.Sin(_ambientTime * 2 + index) * 0.25f;
            renderer.DrawCircle(new Vector2(x, y), (seed % 3) + 1, new Color(188, 206, 245, (byte)(100 + twinkle * 130)));
        }
    }

    private void DrawCity(Renderer renderer, float left, float right)
    {
        var first = (int)MathF.Floor(left / 132);
        var last = (int)MathF.Ceiling(right / 132);
        for (var index = first; index <= last; index++)
        {
            var seed = unchecked((uint)(index * 1597334677 + 3812015801));
            seed ^= seed >> 16;
            var width = 72 + (int)(seed % 49);
            var height = 68 + (int)((seed >> 7) % 105);
            var x = index * 132f;
            var top = 380 - height;
            renderer.DrawRectangle(new Rectangle(x, top, width, height + 200), new Color(17, 26, 47));
            renderer.DrawRectangle(new Rectangle(x + 5, top + 8, width - 10, 3), new Color(52, 72, 103, 140));

            for (var row = 0; row < 3; row++)
            for (var column = 0; column < 3; column++)
            {
                var lit = ((seed >> (row * 3 + column)) & 1) != 0;
                if (!lit)
                    continue;

                var windowY = top + 20 + row * 22;
                var windowX = x + 12 + column * 17;
                renderer.DrawRectangle(
                    new Rectangle(windowX, windowY, 5, 8),
                    new Color(255, 202, 130, 110));
            }
        }

        var horizonY = 386f;
        renderer.DrawRectangle(new Rectangle(left, horizonY, right - left, 200), new Color(12, 19, 34, 190));
        renderer.DrawLine(new Vector2(left, horizonY), new Vector2(right, horizonY), new Color(81, 106, 143, 125));
    }

    private void DrawRoute(Renderer renderer)
    {
        var chapter = _story.CompletedChapters;
        var platformColor = HighContrast
            ? new Color(118, 163, 205)
            : chapter switch
            {
                0 => new Color(75, 110, 142),
                1 => new Color(58, 112, 132),
                _ => new Color(110, 96, 151),
            };

        foreach (var platform in _platforms)
        {
            renderer.DrawRectangle(platform.Bounds, new Color(17, 26, 43));
            renderer.DrawRectangle(
                new Rectangle(platform.Bounds.X, platform.Bounds.Y, platform.Bounds.Width, 8),
                platformColor);
            renderer.DrawRectangle(
                new Rectangle(platform.Bounds.X + 5, platform.Bounds.Y + 9, platform.Bounds.Width - 10, 2),
                new Color(143, 188, 214, 110));
            for (var x = platform.Bounds.X + 20; x < platform.Bounds.X + platform.Bounds.Width - 10; x += 38)
                renderer.DrawLine(new Vector2(x, platform.Bounds.Y + 13), new Vector2(x - 9, platform.Bounds.Y + 19), new Color(35, 54, 74));
        }

        foreach (var hazard in _hazards)
        {
            var pulse = ReducedMotion ? 0 : (MathF.Sin(_ambientTime * 6 + hazard.Position.X) + 1) * 0.5f;
            var color = HighContrast ? new Color(255, 60, 30) : new Color(242, (byte)(70 + pulse * 38), 75);
            renderer.DrawBrowserPolygon(
            [
                new Vector2(hazard.Position.X - 14, hazard.Position.Y + 10),
                new Vector2(hazard.Position.X - 7, hazard.Position.Y - 12),
                new Vector2(hazard.Position.X, hazard.Position.Y + 10),
                new Vector2(hazard.Position.X + 7, hazard.Position.Y - 12),
                new Vector2(hazard.Position.X + 14, hazard.Position.Y + 10),
            ], color, filled: true, screenSpace: false);
            renderer.DrawRectangle(new Rectangle(hazard.Position.X - 15, hazard.Position.Y + 8, 30, 5), new Color(107, 42, 57));
        }

        foreach (var checkpoint in _checkpoints)
        {
            renderer.DrawRectangle(new Rectangle(checkpoint.Position.X - 3, checkpoint.Position.Y - 29, 6, 54), new Color(93, 141, 160));
            renderer.DrawBrowserPolygon(
            [
                new Vector2(checkpoint.Position.X + 2, checkpoint.Position.Y - 28),
                new Vector2(checkpoint.Position.X + 33, checkpoint.Position.Y - 20),
                new Vector2(checkpoint.Position.X + 2, checkpoint.Position.Y - 10),
            ], checkpoint.Activated ? new Color(255, 222, 130) : new Color(115, 207, 206), filled: true, screenSpace: false);
            renderer.DrawCircle(
                new Vector2(checkpoint.Position.X, checkpoint.Position.Y - 30),
                checkpoint.Activated ? 9 : 6,
                checkpoint.Activated ? new Color(255, 224, 142, 220) : new Color(130, 226, 218, 180));
        }
    }

    private void DrawPickups(Renderer renderer)
    {
        foreach (var pickup in _pickups)
        {
            var bob = ReducedMotion ? 0 : MathF.Sin(_ambientTime * 3 + pickup.Phase) * 5;
            var position = pickup.Position + new Vector2(0, bob);
            var color = pickup.IsMemory ? new Color(136, 226, 238) : new Color(255, 214, 121);
            renderer.DrawCircle(position, pickup.IsMemory ? 11 : 9, color.WithAlpha(65));
            renderer.DrawCircleOutline(position, pickup.IsMemory ? 7 : 6, new Color(255, 245, 194));
            renderer.DrawCircle(position, 2, Color.White);
            if (pickup.IsMemory)
                renderer.DrawLine(position + new Vector2(-12, 0), position + new Vector2(12, 0), new Color(136, 226, 238, 170));
        }
    }

    private void DrawPlayer(Renderer renderer)
    {
        if (State is BrowserGameState.Title or BrowserGameState.Dialogue or BrowserGameState.Ending)
            return;
        if (_invulnerabilityTimer > 0 && ((int)(_ambientTime * 16) & 1) == 0)
            return;

        var position = _player.Position;
        var motion = ReducedMotion ? 0 : MathF.Sin(_ambientTime * 18) * MathF.Min(2, MathF.Abs(_body.Velocity.X) / 80);
        var facing = _controller.FacingRight ? 1f : -1f;
        var bodyColor = HighContrast ? new Color(255, 244, 208) : new Color(238, 205, 176);
        renderer.DrawRectangle(new Rectangle(position.X - 12, position.Y - 17 + motion, 24, 30), new Color(34, 40, 61));
        renderer.DrawRectangle(new Rectangle(position.X - 10, position.Y - 22 + motion, 20, 15), bodyColor);
        renderer.DrawRectangle(new Rectangle(position.X - 11, position.Y - 26 + motion, 22, 8), new Color(76, 48, 47));
        renderer.DrawRectangle(new Rectangle(position.X - 5 + facing * 3, position.Y - 18 + motion, 3, 3), new Color(37, 39, 54));
        renderer.DrawRectangle(new Rectangle(position.X - 12, position.Y - 5 + motion, 24, 6), new Color(232, 81, 91));
        renderer.DrawRectangle(new Rectangle(position.X - 17 - (facing < 0 ? 6 : 0), position.Y - 4 + motion, 10, 4), new Color(244, 99, 110));
        renderer.DrawRectangle(new Rectangle(position.X - 9, position.Y + 10, 7, 7), new Color(39, 42, 62));
        renderer.DrawRectangle(new Rectangle(position.X + 2, position.Y + 10, 7, 7), new Color(39, 42, 62));
        renderer.DrawRectangle(new Rectangle(position.X - 12, position.Y - 28 + motion, 7, 2), new Color(234, 162, 124));
    }

    private void DrawParticles(Renderer renderer)
    {
        for (var index = _particles.Count - 1; index >= 0; index--)
        {
            var particle = _particles[index];
            var alpha = (byte)Math.Clamp(particle.Remaining / particle.Lifetime * 255, 0, 255);
            renderer.DrawCircle(particle.Position, particle.Size * particle.Remaining / particle.Lifetime, particle.Color.WithAlpha(alpha));
        }
    }

    private void DrawHud(Renderer renderer)
    {
        if (State == BrowserGameState.Title)
            return;

        var panel = HighContrast ? new Color(0, 0, 0, 235) : new Color(10, 16, 29, 218);
        var text = HighContrast ? Color.White : new Color(236, 237, 245);
        renderer.DrawBrowserRectangle(new Rectangle(22, 18, 282, 64), panel);
        renderer.DrawBrowserText($"CHAPTER 0{Math.Min(_story.CompletedChapters + 1, 3)}", new Vector2(38, 40), new Color(170, 211, 235), 12, bold: true);
        renderer.DrawBrowserText(Shorten(_story.CurrentChapterTitle, 27), new Vector2(38, 62), text, 12);

        renderer.DrawBrowserRectangle(new Rectangle(324, 20, 338, 42), panel);
        renderer.DrawBrowserText($"SIGNALS  {_signals:00}", new Vector2(344, 42), new Color(255, 221, 143), 13, bold: true);
        renderer.DrawBrowserText($"DIST  {Math.Max(0, (int)Distance):0000}m", new Vector2(508, 42), text, 11);

        renderer.DrawBrowserRectangle(new Rectangle(22, 92, 640, 8), new Color(33, 42, 62));
        renderer.DrawBrowserRectangle(new Rectangle(22, 92, 640 * Math.Clamp(ChapterProgress, 0, 1), 8), new Color(111, 207, 218));
        renderer.DrawBrowserText($"BEST  {Math.Max(0, (int)_bestDistance):0000}m", new Vector2(682, 30), new Color(171, 183, 208), 11);

        for (var index = 0; index < MaxLives; index++)
        {
            var color = index < _lives ? new Color(244, 124, 138) : new Color(76, 79, 98);
            var x = 814 + index * 25;
            renderer.DrawBrowserCircle(new Vector2(x, 42), 8, color);
            renderer.DrawBrowserText("♥", new Vector2(x, 43), Color.White, 10, centered: true, bold: true);
        }

        renderer.DrawBrowserRectangle(new Rectangle(882, 19, 54, 42), panel, outline: true);
        renderer.DrawBrowserText(State == BrowserGameState.Playing ? "Ⅱ" : "▶", new Vector2(909, 40), text, 20, centered: true, bold: true);

        if (Toast.Length > 0)
        {
            renderer.DrawBrowserRectangle(new Rectangle(309, 111, 342, 34), new Color(15, 23, 39, 225), outline: true);
            renderer.DrawBrowserText(Toast, new Vector2(480, 128), new Color(255, 227, 160), 12, centered: true, bold: true);
        }
        if (AchievementToast.Length > 0)
        {
            renderer.DrawBrowserRectangle(new Rectangle(658, 75, 278, 46), new Color(37, 47, 70, 235), outline: true);
            renderer.DrawBrowserText("SIGNAL MILESTONE", new Vector2(672, 90), new Color(157, 224, 216), 9, bold: true);
            renderer.DrawBrowserText(AchievementToast, new Vector2(672, 107), text, 12, bold: true);
        }
    }

    private void DrawOverlay(Renderer renderer)
    {
        if (State == BrowserGameState.Playing)
            return;

        renderer.DrawBrowserRectangle(new Rectangle(0, 0, ViewWidth, ViewHeight), new Color(5, 9, 20, State == BrowserGameState.Respawning ? (byte)130 : (byte)198));

        switch (State)
        {
            case BrowserGameState.Title:
                DrawTitle(renderer);
                break;
            case BrowserGameState.Dialogue:
                DrawDialogue(renderer);
                break;
            case BrowserGameState.Paused:
                DrawMenu(renderer, "PAUSED", "The signal can wait. Nova can't.", ["RESUME", "SETTINGS", "RETURN TO CHECKPOINT"]);
                break;
            case BrowserGameState.Settings:
                DrawSettings(renderer);
                break;
            case BrowserGameState.Respawning:
                DrawCenteredTitle(renderer, "SIGNAL LOST", "Reconnecting at the last lantern...");
                renderer.DrawBrowserRectangle(new Rectangle(330, 330, 300, 8), new Color(52, 62, 86));
                renderer.DrawBrowserRectangle(new Rectangle(330, 330, 300 * (1 - Math.Clamp(_respawnTimer / 0.72f, 0, 1)), 8), new Color(125, 220, 213));
                break;
            case BrowserGameState.GameOver:
                DrawCenteredTitle(renderer, "THE LIGHT FADES", $"Signals secured: {_signals:00}   ·   Distance: {(int)Distance}m");
                DrawButton(renderer, 480, 390, 320, 58, "TRY AGAIN  ↗", primary: true);
                renderer.DrawBrowserText("PRESS SPACE OR CLICK TO RESTART", new Vector2(480, 446), new Color(161, 168, 188), 10, centered: true);
                break;
            case BrowserGameState.Ending:
                DrawEnding(renderer);
                break;
        }
    }

    private void DrawTitle(Renderer renderer)
    {
        renderer.DrawBrowserText("FLUXENGINE  /  BROWSER EDITION", new Vector2(480, 166), new Color(140, 213, 218), 12, centered: true, bold: true);
        renderer.DrawBrowserText("AFTERLIGHT", new Vector2(480, 231), new Color(248, 236, 219), 52, centered: true, bold: true);
        renderer.DrawBrowserText("THE CITY WENT DARK. ONE SIGNAL IS STILL CALLING.", new Vector2(480, 275), new Color(190, 195, 211), 14, centered: true);
        renderer.DrawBrowserText($"PERSONAL BEST  {(int)_bestDistance:0000}m   ·   ARCHIVES  {_achievements.Count}/5",
            new Vector2(480, 314), new Color(155, 173, 194), 10, centered: true);
        DrawButton(renderer, 480, 360, 330, 58, _hasSavedRun ? "CONTINUE THE SIGNAL  ↗" : "BEGIN THE STORY  ↗", primary: true);
        DrawButton(renderer, 480, 426, 200, 38, "SETTINGS", primary: false);
        renderer.DrawBrowserText("A / D OR ← / →  MOVE     SPACE  JUMP ×2     ESC  PAUSE", new Vector2(480, 484), new Color(145, 153, 175), 10, centered: true);
    }

    private void DrawDialogue(Renderer renderer)
    {
        renderer.DrawBrowserText("STORY  /  SIGNAL ARCHIVE", new Vector2(480, 151), new Color(135, 209, 214), 11, centered: true, bold: true);
        renderer.DrawBrowserText(_dialogueTitle, new Vector2(480, 203), new Color(246, 232, 210), 25, centered: true, bold: true);
        var page = _dialoguePages.Count > 0 ? _dialoguePages[Math.Clamp(_dialoguePage, 0, _dialoguePages.Count - 1)] : "";
        DrawWrappedText(renderer, page, 480, 280, 640, new Color(199, 205, 219), 15, centered: true);
        renderer.DrawBrowserText($"{_dialoguePage + 1:00}  /  {_dialoguePages.Count:00}", new Vector2(480, 392), new Color(135, 146, 170), 10, centered: true);
        DrawButton(renderer, 480, 456, 300, 58, _dialoguePage + 1 < _dialoguePages.Count ? "CONTINUE  ↗" : "FOLLOW THE SIGNAL  ↗", primary: true);
        renderer.DrawBrowserText("SPACE / ENTER OR CLICK TO CONTINUE", new Vector2(480, 503), new Color(145, 153, 175), 9, centered: true);
    }

    private void DrawMenu(Renderer renderer, string title, string subtitle, string[] options)
    {
        DrawCenteredTitle(renderer, title, subtitle);
        DrawButton(renderer, 480, 316, 300, 52, options[0], primary: true);
        DrawButton(renderer, 480, 382, 300, 52, options[1], primary: false);
        DrawButton(renderer, 480, 448, 300, 52, options[2], primary: false);
        renderer.DrawBrowserText("ESC TO RESUME", new Vector2(480, 492), new Color(141, 150, 171), 9, centered: true);
    }

    private void DrawSettings(Renderer renderer)
    {
        DrawCenteredTitle(renderer, "SETTINGS", "Your browser saves these choices automatically.");
        DrawButton(renderer, 480, 300, 370, 48, $"SOUND   {(SoundEnabled ? "ON" : "OFF")}", primary: _settingsSelection == 0);
        DrawButton(renderer, 480, 360, 370, 48, $"REDUCED MOTION   {(ReducedMotion ? "ON" : "OFF")}", primary: _settingsSelection == 1);
        DrawButton(renderer, 480, 420, 370, 48, $"HIGH CONTRAST   {(HighContrast ? "ON" : "OFF")}", primary: _settingsSelection == 2);
        DrawButton(renderer, 480, 484, 300, 46, "DONE", primary: false);
        renderer.DrawBrowserText("↑ / ↓ SELECT     SPACE TO TOGGLE     ESC TO CLOSE", new Vector2(480, 520), new Color(141, 150, 171), 9, centered: true);
    }

    private void DrawEnding(Renderer renderer)
    {
        renderer.DrawBrowserText("SIGNAL ARCHIVE  /  COMPLETE", new Vector2(480, 150), new Color(144, 225, 214), 11, centered: true, bold: true);
        renderer.DrawBrowserText("THE CITY IS AWAKE", new Vector2(480, 211), new Color(250, 239, 213), 34, centered: true, bold: true);
        var page = StoryCampaign.Ending[Math.Clamp(_endingPage, 0, StoryCampaign.Ending.Count - 1)];
        DrawWrappedText(renderer, page, 480, 294, 660, new Color(198, 208, 221), 15, centered: true);
        renderer.DrawBrowserText($"SIGNALS  {_signals:00}     MEMORIES  {_memoryFragments:00}     BEST  {(int)_bestDistance}m",
            new Vector2(480, 369), new Color(255, 220, 144), 12, centered: true);
        renderer.DrawBrowserText($"{_endingPage + 1:00}  /  {StoryCampaign.Ending.Count:00}",
            new Vector2(480, 396), new Color(135, 146, 170), 10, centered: true);
        DrawButton(renderer, 480, 428, 320, 58,
            _endingPage + 1 < StoryCampaign.Ending.Count ? "CONTINUE  ↗" : "PLAY AGAIN  ↗", primary: true);
    }

    private static void DrawCenteredTitle(Renderer renderer, string title, string subtitle)
    {
        renderer.DrawBrowserText(title, new Vector2(480, 218), new Color(247, 235, 215), 32, centered: true, bold: true);
        renderer.DrawBrowserText(subtitle, new Vector2(480, 260), new Color(180, 190, 208), 13, centered: true);
    }

    private static void DrawButton(Renderer renderer, float x, float y, float width, float height, string label, bool primary)
    {
        var fill = primary ? new Color(110, 205, 207) : new Color(24, 34, 54, 245);
        var foreground = primary ? new Color(11, 25, 39) : new Color(218, 225, 237);
        renderer.DrawBrowserRectangle(new Rectangle(x - width / 2, y - height / 2, width, height), fill, outline: !primary);
        renderer.DrawBrowserText(label, new Vector2(x, y), foreground, primary ? 13 : 11, centered: true, bold: true);
    }

    private static void DrawWrappedText(Renderer renderer, string text, float centerX, float centerY, int maxCharacters, Color color, float size, bool centered)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (candidate.Length > maxCharacters && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }
        if (current.Length > 0)
            lines.Add(current);

        for (var index = 0; index < lines.Count; index++)
            renderer.DrawBrowserText(lines[index], new Vector2(centerX, centerY + index * (size * 1.6f)), color, size, centered);
    }

    private void CreatePlatform(Vector2 center, float width)
    {
        var entity = _scene.Spawn("route-platform", center);
        var collider = entity.AddComponent<OneWayPlatform>();
        collider.Size = new Vector2(width, 22);
        _platforms.Add(new RoutePlatform(entity, new Rectangle(center.X - width / 2, center.Y - 11, width, 22)));
        _lastPlatformY = center.Y;
    }

    private void EnsureRoute()
    {
        var spawnAhead = _player.Position.X + ViewWidth * 1.6f;
        while (_nextPlatformRight < spawnAhead)
        {
            var gap = _minGap + (float)_random.NextDouble() * (_maxGap - _minGap);
            var width = 165 + (float)_random.NextDouble() * 84;
            var verticalOffset = (float)(_random.NextDouble() * 90 - 45);
            var y = Math.Clamp(_lastPlatformY + verticalOffset, 326, 448);
            var centerX = _nextPlatformRight + gap + width / 2;
            CreatePlatform(new Vector2(centerX, y), width);
            _nextPlatformRight = centerX + width / 2;
            _platformCount++;

            if (_platformCount % 2 == 0)
                AddPickup(new Vector2(centerX, y - 54), isMemory: false);
            if (_platformCount % 5 == 0)
                AddPickup(new Vector2(centerX - 28, y - 57), isMemory: true);
            if (_platformCount > 2 && _platformCount % 4 == 0)
                AddHazard(new Vector2(centerX + width * 0.25f, y - 19));
            if (_nextPlatformRight >= _nextCheckpointX)
            {
                AddCheckpoint(new Vector2(centerX, y - 22));
                _nextCheckpointX += CheckpointSpacing;
            }
        }
    }

    private void AddPickup(Vector2 position, bool isMemory)
    {
        var entity = _scene.Spawn(isMemory ? "memory-signal" : "signal-shard", position);
        _pickups.Add(new WorldPickup(entity, position, isMemory, (float)_random.NextDouble() * MathF.Tau));
    }

    private void AddHazard(Vector2 position)
    {
        var entity = _scene.Spawn("roof-spike", position);
        var collider = entity.AddComponent<ColliderComponentProxy>();
        collider.Size = new Vector2(26, 20);
        _hazards.Add(new WorldHazard(entity, position, collider));
    }

    private void AddCheckpoint(Vector2 position)
    {
        var entity = _scene.Spawn("signal-lantern", position);
        var collider = entity.AddComponent<ColliderComponentProxy>();
        collider.Size = new Vector2(30, 58);
        _checkpoints.Add(new WorldCheckpoint(entity, position, collider));
    }

    private void CollectPickups()
    {
        var playerBounds = new BoxCollider(_player.Position, _body.Size);
        foreach (var pickup in _pickups.ToArray())
        {
            var pickupBounds = new BoxCollider(pickup.Position, new Vector2(24, 24));
            if (!Collision.Overlaps(playerBounds, pickupBounds))
                continue;

            _scene.Destroy(pickup.Entity);
            _pickups.Remove(pickup);
            _signals++;
            _score += pickup.IsMemory ? 50 : 10;
            _hasSavedRun = true;
            SpawnBurst(pickup.Position, pickup.IsMemory ? new Color(131, 224, 236) : new Color(255, 219, 136), 12);
            if (pickup.IsMemory)
            {
                _memoryFragments++;
                var memoryMessage = MemoryMessages[(_memoryFragments - 1) % MemoryMessages.Length];
                ShowToast($"MEMORY {_memoryFragments:00}  ·  {memoryMessage}");
                UnlockAchievement("MEMORY KEEPER");
            }
            else
            {
                ShowToast("+10 SIGNAL");
            }
            UnlockAchievement("FIRST LIGHT");
            AddSound(pickup.IsMemory ? "memory" : "coin");
            SaveProgress();
        }
    }

    private void CheckHazards()
    {
        if (_invulnerabilityTimer > 0)
            return;

        var playerBounds = new BoxCollider(_player.Position, _body.Size);
        foreach (var hazard in _hazards)
        {
            if (Collision.Overlaps(playerBounds, hazard.Collider.Bounds))
            {
                TakeHit();
                return;
            }
        }
    }

    private void CheckCheckpoints()
    {
        var playerBounds = new BoxCollider(_player.Position, _body.Size);
        foreach (var checkpoint in _checkpoints)
        {
            if (checkpoint.Activated || !Collision.Overlaps(playerBounds, checkpoint.Collider.Bounds))
                continue;

            checkpoint.Activated = true;
            _checkpointX = checkpoint.Position.X - 22;
            _checkpointY = checkpoint.Position.Y - 30;
            ShowToast("LANTERN LIT  ·  CHECKPOINT SAVED");
            SpawnBurst(checkpoint.Position, new Color(139, 232, 219), 22);
            AddSound("checkpoint");
            _hasSavedRun = true;
            SaveProgress();
        }
    }

    private void RemoveDistantEntities()
    {
        var cleanupX = _player.Position.X - ViewWidth * 1.8f;
        foreach (var platform in _platforms.Where(platform => platform.Bounds.X + platform.Bounds.Width < cleanupX).ToArray())
        {
            _platforms.Remove(platform);
            _scene.Destroy(platform.Entity);
        }
        foreach (var hazard in _hazards.Where(hazard => hazard.Position.X < cleanupX).ToArray())
        {
            _hazards.Remove(hazard);
            _scene.Destroy(hazard.Entity);
        }
        foreach (var pickup in _pickups.Where(pickup => pickup.Position.X < cleanupX).ToArray())
        {
            _pickups.Remove(pickup);
            _scene.Destroy(pickup.Entity);
        }
        foreach (var checkpoint in _checkpoints.Where(checkpoint => checkpoint.Position.X < cleanupX).ToArray())
        {
            _checkpoints.Remove(checkpoint);
            _scene.Destroy(checkpoint.Entity);
        }
    }

    private void TakeHit()
    {
        if (State != BrowserGameState.Playing || _invulnerabilityTimer > 0)
            return;

        _lives--;
        _player.Enabled = false;
        _scene.Camera?.AddShake(ReducedMotion ? 0 : 0.65f);
        SpawnBurst(_player.Position, new Color(246, 116, 121), 18);
        AddSound("hit");
        if (_lives <= 0)
        {
            State = BrowserGameState.GameOver;
            _bestDistance = MathF.Max(_bestDistance, Distance);
            SaveProgress();
        }
        else
        {
            State = BrowserGameState.Respawning;
            _respawnTimer = 0.72f;
        }
    }

    private void Respawn()
    {
        _player.Position = new Vector2(_checkpointX, _checkpointY);
        _body.Velocity = Vector2.Zero;
        _controller.ResetJumpState();
        _invulnerabilityTimer = 1.3f;
        _player.Enabled = true;
        State = BrowserGameState.Playing;
        if (_scene.Camera is not null)
            _scene.Camera.Position = new Vector2(_checkpointX + 145, _checkpointY);
    }

    private void RestartFromCheckpoint()
    {
        _score = Math.Max(0, _score);
        _lives = MaxLives;
        _player.Position = new Vector2(_checkpointX, _checkpointY);
        _body.Velocity = Vector2.Zero;
        _controller.ResetJumpState();
        _invulnerabilityTimer = 1.3f;
        _player.Enabled = true;
        State = BrowserGameState.Playing;
        if (_scene.Camera is not null)
            _scene.Camera.Position = new Vector2(_checkpointX + 145, _checkpointY);
        EnsureRoute();
        SaveProgress();
    }

    private void StartNewStory()
    {
        _story.Reset();
        _score = 0;
        _signals = 0;
        _memoryFragments = 0;
        _lives = MaxLives;
        _bestDistance = MathF.Max(_bestDistance, 0);
        _checkpointX = StoryCampaign.InitialSpawnX;
        _checkpointY = 390;
        _player.Position = new Vector2(_checkpointX, _checkpointY);
        _body.Velocity = Vector2.Zero;
        _player.Enabled = false;
        _controller.ResetJumpState();
        _hasSavedRun = false;
        ResetRoute();
        ShowDialogue("A SIGNAL IN THE DARK", StoryCampaign.Prologue);
        AddSound("story");
        SaveProgress();
    }

    private void ResetRoute()
    {
        foreach (var entity in _platforms.Select(item => item.Entity)
                     .Concat(_pickups.Select(item => item.Entity))
                     .Concat(_hazards.Select(item => item.Entity))
                     .Concat(_checkpoints.Select(item => item.Entity)))
            _scene.Destroy(entity);

        _platforms.Clear();
        _pickups.Clear();
        _hazards.Clear();
        _checkpoints.Clear();
        _platformCount = 0;
        _lastPlatformY = 440;
        _nextPlatformRight = StoryCampaign.InitialSpawnX + 450;
        _nextCheckpointX = StoryCampaign.InitialSpawnX + CheckpointSpacing;
        _minGap = 58;
        _maxGap = 104;
        _difficultyTimer = 0;
        _scene.Update(0);
        CreatePlatform(new Vector2(StoryCampaign.InitialSpawnX + 190, 440), 520);
        EnsureRoute();
        if (_scene.Camera is not null)
            _scene.Camera.Position = new Vector2(StoryCampaign.InitialSpawnX + 145, 350);
    }

    private void StartPlaying()
    {
        State = BrowserGameState.Playing;
        _player.Enabled = true;
        _controller.ResetJumpState();
        _scene.Camera?.AddShake(ReducedMotion ? 0 : 0.14f);
    }

    private void ShowDialogue(string title, IReadOnlyList<string> pages)
    {
        _dialogueTitle = title;
        _dialoguePages = pages;
        _dialoguePage = 0;
        State = BrowserGameState.Dialogue;
        _player.Enabled = false;
    }

    private void AdvanceDialogue()
    {
        if (_dialoguePage + 1 < _dialoguePages.Count)
        {
            _dialoguePage++;
            AddSound("story");
            return;
        }

        StartPlaying();
    }

    private void AdvanceEnding()
    {
        if (_endingPage + 1 < StoryCampaign.Ending.Count)
        {
            _endingPage++;
            AddSound("story");
            return;
        }

        StartNewStory();
    }

    private void Resume()
    {
        State = BrowserGameState.Playing;
        _player.Enabled = true;
        AddSound("menu");
    }

    private void CloseSettings()
    {
        State = _returnState;
        _player.Enabled = State == BrowserGameState.Playing;
        SaveProgress();
    }

    private void HandleSettingsInput()
    {
        if (_actions.WasPressed("pause"))
        {
            CloseSettings();
            return;
        }

        var movedSelection = false;
        if (_actions.WasPressed("settings.up"))
        {
            _settingsSelection = (_settingsSelection + 2) % 3;
            movedSelection = true;
        }
        if (_actions.WasPressed("settings.down"))
        {
            _settingsSelection = (_settingsSelection + 1) % 3;
            movedSelection = true;
        }
        if ((!movedSelection && _actions.WasPressed("jump")) ||
            _actions.WasPressed("move.left") || _actions.WasPressed("move.right"))
            ToggleSetting(_settingsSelection);
    }

    private void ToggleSetting(int setting)
    {
        switch (setting)
        {
            case 0:
                SoundEnabled = !SoundEnabled;
                break;
            case 1:
                ReducedMotion = !ReducedMotion;
                break;
            case 2:
                HighContrast = !HighContrast;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(setting));
        }

        if (SoundEnabled)
            AddSound("menu");
        SaveProgress();
    }

    private void UnlockAchievement(string title)
    {
        if (!_achievements.Add(title))
            return;

        _achievementToast = title;
        _achievementToastTimer = 3;
        AddSound("achievement");
        SaveProgress();
    }

    private void ShowToast(string message)
    {
        _toast = message;
        _toastTimer = 2.2f;
    }

    private void AddSound(string sound) => _soundEvents.Add(sound);

    private void SaveProgress()
    {
        _hasSavedRun = _score > 0 || _story.CompletedChapters > 0 ||
            _checkpointX > StoryCampaign.InitialSpawnX;
        _lastSavedDistance = MathF.Max(_lastSavedDistance, Distance);
        SaveRequested?.Invoke(SaveToJson());
    }

    public event Action<string>? SaveRequested;

    private void SpawnBurst(Vector2 position, Color color, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var angle = (float)_random.NextDouble() * MathF.Tau;
            var speed = 35 + (float)_random.NextDouble() * 115;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _particles.Add(new WorldParticle(position, velocity, color, 0.55f, 0.55f, 2 + (float)_random.NextDouble() * 3));
        }
    }

    public void UpdateEffects(float deltaTime)
    {
        for (var index = _particles.Count - 1; index >= 0; index--)
        {
            var particle = _particles[index];
            var remaining = particle.Remaining - deltaTime;
            if (remaining <= 0)
            {
                _particles.RemoveAt(index);
                continue;
            }

            var velocity = particle.Velocity + new Vector2(0, 120 * deltaTime);
            _particles[index] = particle with
            {
                Position = particle.Position + velocity * deltaTime,
                Velocity = velocity,
                Remaining = remaining,
            };
        }
    }

    private static bool Inside(double x, double y, float centerX, float centerY, float width, float height)
        => x >= centerX - width / 2 && x <= centerX + width / 2 &&
           y >= centerY - height / 2 && y <= centerY + height / 2;

    private static string Shorten(string text, int maxLength)
        => text.Length <= maxLength ? text : $"{text[..(maxLength - 1)]}…";

    private Color CurrentSky
        => _story.CompletedChapters switch
        {
            0 => new Color(13, 24, 48),
            1 => new Color(9, 37, 55),
            _ => new Color(35, 24, 57),
        };

    private static void ValidateSave(BrowserSaveData data)
    {
        if (data.Score < 0 || data.Signals < 0 || data.MemoryFragments < 0 ||
            !float.IsFinite(data.BestDistance) || data.BestDistance < 0 ||
            !float.IsFinite(data.CheckpointX) || data.CheckpointX < StoryCampaign.InitialSpawnX ||
            !float.IsFinite(data.CheckpointY) || data.CheckpointY is < -1000 or > 1000)
            throw new InvalidDataException("The saved FluxRunner progress contains invalid values.");
        ArgumentNullException.ThrowIfNull(data.Achievements);
    }

    private sealed record RoutePlatform(Entity Entity, Rectangle Bounds);
    private sealed record WorldPickup(Entity Entity, Vector2 Position, bool IsMemory, float Phase);
    private sealed record WorldHazard(Entity Entity, Vector2 Position, ColliderComponentProxy Collider);
    private sealed record WorldParticle(Vector2 Position, Vector2 Velocity, Color Color, float Lifetime, float Remaining, float Size);

    private sealed class WorldCheckpoint(Entity entity, Vector2 position, ColliderComponentProxy collider)
    {
        public Entity Entity { get; } = entity;
        public Vector2 Position { get; } = position;
        public ColliderComponentProxy Collider { get; } = collider;
        public bool Activated { get; set; }
    }

    private sealed class ColliderComponentProxy : ColliderComponent { }

    private sealed class BrowserPlayerController(InputActions actions, Action onJump) : Component
    {
        private readonly float _speed = PlayerSpeed;
        private readonly float _jumpForce = JumpForce;
        private Rigidbody2D _body = null!;
        private float _coyoteTime;
        private float _jumpBuffer;
        private int _jumpsRemaining = 2;
        private bool _jumpHeld;
        private bool _wasGrounded;

        public bool FacingRight { get; private set; } = true;

        public override void OnStart()
            => _body = Entity.GetComponent<Rigidbody2D>() ?? throw new InvalidOperationException("The runner needs a rigidbody.");

        public override void OnUpdate(float deltaTime)
        {
            var horizontal = actions.Axis("move.left", "move.right");
            var targetVelocity = horizontal * _speed;
            var acceleration = horizontal != 0 && MathF.Sign(_body.Velocity.X) != MathF.Sign(targetVelocity) ? 4200 : 2200;
            var velocityX = MathUtil.MoveTowards(_body.Velocity.X, targetVelocity, acceleration * deltaTime);
            _body.Velocity = new Vector2(velocityX, _body.Velocity.Y);
            if (horizontal != 0)
                FacingRight = horizontal > 0;

            var grounded = _body.OnGround;
            if (grounded)
            {
                _coyoteTime = 0.11f;
                _jumpsRemaining = 2;
            }
            else
            {
                _coyoteTime = MathF.Max(0, _coyoteTime - deltaTime);
            }

            if (actions.WasPressed("jump"))
                _jumpBuffer = 0.14f;
            else
                _jumpBuffer = MathF.Max(0, _jumpBuffer - deltaTime);

            if (_jumpBuffer > 0 && (grounded || _coyoteTime > 0 || _jumpsRemaining > 0))
            {
                _body.Velocity = new Vector2(_body.Velocity.X, _jumpForce);
                _jumpBuffer = 0;
                _coyoteTime = 0;
                _jumpsRemaining--;
                _jumpHeld = true;
                onJump();
            }

            if (_jumpHeld && actions.WasReleased("jump"))
            {
                if (_body.Velocity.Y < 0)
                    _body.Velocity = new Vector2(_body.Velocity.X, _body.Velocity.Y * 0.55f);
                _jumpHeld = false;
            }

            if (!grounded && _wasGrounded)
                _jumpsRemaining = Math.Max(1, _jumpsRemaining);
            _wasGrounded = grounded;
        }

        public void ResetJumpState()
        {
            _coyoteTime = 0;
            _jumpBuffer = 0;
            _jumpsRemaining = 2;
            _jumpHeld = false;
            _wasGrounded = false;
        }
    }
}
