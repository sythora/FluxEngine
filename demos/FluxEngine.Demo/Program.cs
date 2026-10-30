using FluxEngine.Audio;
using FluxEngine.Content;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

const int viewWidth = 960;
const int viewHeight = 720;

var screenshotPath = GetOption(args, "--screenshot");
var screenshotFrame = GetIntOption(args, "--frame", 90);
var inMenu = HasFlag(args, "--menu");
var inSettings = HasFlag(args, "--settings");
var inStory = HasFlag(args, "--story");
var renderedFrames = 0;
var screenshotTaken = false;

using var engine = new Engine("FluxRunner - Infinite Platformer", viewWidth, viewHeight);

// ---------------------------------------------------------------------------
// Generate textures (PNG with alpha, decoded by the engine)
// ---------------------------------------------------------------------------
var playerIdle = TextureGenerator.PlayerIdle();
var playerRun0 = TextureGenerator.PlayerRun0();
var playerRun1 = TextureGenerator.PlayerRun1();
var playerJump = TextureGenerator.PlayerJump();
var coinFrames = Enumerable.Range(0, 4).Select(i => TextureGenerator.Coin(i)).ToArray();

// ---------------------------------------------------------------------------
// Load audio (missing files are skipped gracefully)
// ---------------------------------------------------------------------------
Sound? jumpSound = null, landSound = null, coinSound = null, dieSound = null, music = null;

try { jumpSound = engine.Audio.LoadSound("assets/jump.wav"); }
catch (Exception ex) { Log.Warn($"Jump sound: {ex.Message}"); }

try { landSound = engine.Audio.LoadSound("assets/land.wav"); }
catch (Exception ex) { Log.Warn($"Land sound: {ex.Message}"); }

try { coinSound = engine.Audio.LoadSound("assets/coin.wav"); }
catch (Exception ex) { Log.Warn($"Coin sound: {ex.Message}"); }

try { dieSound = engine.Audio.LoadSound("assets/die.wav"); }
catch (Exception ex) { Log.Warn($"Die sound: {ex.Message}"); }

try { music = engine.Audio.LoadSound("assets/music.wav"); }
catch (Exception ex) { Log.Warn($"Music: {ex.Message}"); }

// ---------------------------------------------------------------------------
// Scene setup
// ---------------------------------------------------------------------------
var scene = new Scene
{
    BackgroundColor = new Color(25, 40, 80),
    Camera = new Camera { Position = new Vector2(viewWidth / 2f, viewHeight / 2f) },
};

// Entities
var playerEnt = scene.Spawn("player");
var particlesEnt = scene.Spawn("particles");
var particles = particlesEnt.AddComponent<ParticleEmitter>();
particles.Color = Color.Yellow;
particles.Size = 3f;
particles.Life = 0.3f;
particles.Speed = 90f;
particles.MaxParticles = 60;

var playerCtrl = new PlayerController(engine.Input.Actions, particles);
playerCtrl.Jumped += () => { if (jumpSound is not null) engine.Audio.Play(jumpSound, 0.6f); };
playerCtrl.Landed += () =>
{
    if (landSound is not null) engine.Audio.Play(landSound, 0.5f);
    if (engine.Settings.EnableScreenShake)
        scene.Camera?.AddShake(0.15f);
};
playerEnt.AddComponent(playerCtrl);

var playerLight = playerEnt.AddComponent<Light2D>();
playerLight.Radius = 180f;
playerLight.Color = new Color(255, 220, 160);
playerLight.Flicker = true;
playerLight.FlickerSpeed = 6.0f;
playerLight.FlickerAmount = 0.12f;

scene.Lighting.Enabled = true;
scene.Lighting.DayNight.DayDuration = 60f;
scene.Lighting.DayNight.SetHour(17.5f);

var generatorEnt = scene.Spawn("generator");
var generator = new PlatformGenerator(playerCtrl, Rng.Shared, viewWidth);
generatorEnt.AddComponent(generator);

// The HUD is drawn after the scene in screen space, not as a scene entity, so
// its camera-free drawing cannot affect world rendering.
var hudFont = BuiltInFonts.Load(engine.Renderer, BuiltInFont.PressStart2P);
var smallFont = BuiltInFonts.Load(engine.Renderer, BuiltInFont.Silkscreen);
var ui = new GameUI(engine.Input.Actions, hudFont, smallFont, engine.Settings)
{
    DayNight = scene.Lighting.DayNight,
};

var bgEnt = scene.Spawn("background");
var bg = bgEnt.AddComponent<ParallaxBackground>();

// ---------------------------------------------------------------------------
// Cinematic Visual FX (driven by engine.Settings)
// ---------------------------------------------------------------------------

// God rays - volumetric light shafts streaking through the atmosphere
var godRaysEnt = scene.Spawn("god_rays");
var godRays = godRaysEnt.AddComponent<GodRaysRenderer>();
godRays.RayCount = 5;
godRays.Intensity = 0.8f;

// Atmospheric particles - fireflies at night, warm dust motes by day
var motesEnt = scene.Spawn("atmo_particles");
motesEnt.Position = new Vector2(viewWidth / 2f, viewHeight / 2f);
var atmoParticles = motesEnt.AddComponent<AtmosphericParticles>();
atmoParticles.MaxParticles = 50;
atmoParticles.SpawnRadiusX = 600f;
atmoParticles.SpawnRadiusY = 400f;

// Ground fog - rolling mist that hugs the platforms
var fogEnt = scene.Spawn("ground_fog");
var fog = fogEnt.AddComponent<GroundFog>();
fog.BaseY = 480f;
fog.ScrollSpeed = 25f;
fog.Alpha = 0.4f;

// ---------------------------------------------------------------------------
// Game state
// ---------------------------------------------------------------------------

var state = 0; // 0=StartMenu, 1=Playing, 2=Paused, 3=GameOver, 4=Settings, 5=Dialogue, 6=Ending
var returnState = 0;
var story = new StoryCampaign();
ui.ChapterTitle = story.CurrentChapterTitle;
var debugOverlay = new DebugOverlay(engine.Input.Actions);
var score = 0;
var bestDistance = 0f;
var hasBestDistance = false;
var distance = 0f;
var survivalTime = 0f;
var startX = 100f;
ui.ChapterProgress = story.ChapterProgress(startX);
playerEnt.Position = new Vector2(startX, 300f);
// Left barrier only: blocks walking backwards into negative space, with no floor or right wall to obstruct the run
scene.Solids.Add(new BoxCollider(new Vector2(-40, 360), new Vector2(40, 2000)));

// Start music (loops, safe if no audio device)
if (music is not null)
    engine.Audio.Play(music, volume: 0.4f, loop: true, bus: AudioBus.Music);

// Input bindings
var actions = engine.Input.Actions;
actions.Bind("move.left", Key.A, Key.Left);
actions.Bind("move.right", Key.D, Key.Right);
actions.BindGamepadAxis("move.left", GamepadAxis.LeftX, direction: -1f);
actions.BindGamepadAxis("move.right", GamepadAxis.LeftX, direction: 1f);
actions.BindGamepadButton("move.left", GamepadButton.DPadLeft);
actions.BindGamepadButton("move.right", GamepadButton.DPadRight);
actions.Bind("jump", Key.Space, Key.Up);
actions.BindGamepadButton("jump", GamepadButton.A);
actions.Bind("pause", Key.Escape);
actions.BindGamepadButton("pause", GamepadButton.Start);
actions.Bind("restart", Key.R, Key.Space, Key.Enter);
actions.BindGamepadButton("restart", GamepadButton.Y, GamepadButton.A, GamepadButton.Start);
actions.Bind("settings", Key.O);
actions.BindGamepadButton("settings", GamepadButton.Y);

var respawnCooldown = 0f;

// Respawn at the latest story checkpoint and rebuild the nearby route.
void RestartGame()
{
    if (state == 3)
    {
        bestDistance = MathF.Max(bestDistance, distance);
        hasBestDistance = true;
    }

    state = 1;
    respawnCooldown = 0f;
    ui.Reset();
    story.RespawnAtCheckpoint();
    startX = story.CheckpointX;
    ui.ChapterTitle = story.CurrentChapterTitle;
    distance = story.Distance(startX);
    ui.ChapterProgress = story.ChapterProgress(startX);
    survivalTime = 0f;
    ui.Score = score;
    ui.Distance = distance;
    ui.SurvivalTime = survivalTime;

    // Reset player
    playerEnt.Position = new Vector2(startX, 300f);
    playerCtrl.ResetJumpState();
    var body = playerEnt.GetComponent<Rigidbody2D>();
    if (body is not null)
        body.Velocity = Vector2.Zero;

    // Reset camera position to starter platform
    if (scene.Camera is not null)
        scene.Camera.Position = new Vector2(startX + viewWidth / 2f, viewHeight / 2f);

    // Destroy all existing platforms, coins, hazards
    foreach (var tag in new[] { "platform", "coin", "hazard" })
    {
        foreach (var ent in scene.FindByTag(tag).ToArray())
            scene.Destroy(ent);
    }

    // Restore the left boundary for the new run.
    scene.Solids.Clear();
    scene.Solids.Add(new BoxCollider(new Vector2(-40, 360), new Vector2(40, 2000)));

    // Reset generator
    generator.Reset(startX);
}

// Start all components' OnStart callbacks
scene.Start();

// Screenshot flags make the start, settings, and gameplay screens reproducible.
if (screenshotPath is not null && inSettings)
{
    state = 4;
    ui.OpenSettings();
}
else if (screenshotPath is not null && inStory)
{
    state = 5;
    ui.ShowDialogue("A SIGNAL IN THE DARK", StoryCampaign.Prologue);
}
else if (screenshotPath is not null && !inMenu)
{
    state = 1;
    ui.StartGame();
}

// ---------------------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------------------
var accumulator = 0.0;
const float fixedStep = 1f / 60f;

engine.Run(
    update: deltaTime =>
    {
        if (screenshotTaken)
        {
            engine.Window.Close();
            return;
        }

        // --- Input handling that works in all states ---
        if (actions.WasPressed("debug.toggle"))
            debugOverlay.Enabled = !debugOverlay.Enabled;

        if (engine.Input.WasKeyPressed(Key.T))
            scene.Lighting.DayNight.AddHours(2f);

        if (engine.Input.WasKeyPressed(Key.L))
            engine.Settings.EnableLighting = !engine.Settings.EnableLighting;

        // Toggle individual FX with number keys
        if (engine.Input.WasKeyPressed(Key.D1))
            engine.Settings.EnableVignette = !engine.Settings.EnableVignette;
        if (engine.Input.WasKeyPressed(Key.D2))
            engine.Settings.EnableGodRays = !engine.Settings.EnableGodRays;
        if (engine.Input.WasKeyPressed(Key.D3))
            engine.Settings.EnableAtmosphericParticles = !engine.Settings.EnableAtmosphericParticles;
        if (engine.Input.WasKeyPressed(Key.D4))
            engine.Settings.EnableFog = !engine.Settings.EnableFog;
        if (engine.Input.WasKeyPressed(Key.D5))
            engine.Settings.EnableScreenShake = !engine.Settings.EnableScreenShake;

        // Save settings
        if (engine.Input.WasKeyPressed(Key.F5))
        {
            if (engine.Settings.Save())
                Log.Info("Settings saved.");
        }

        if ((state is 0 or 1 or 2) && actions.WasPressed("settings"))
        {
            returnState = state;
            state = 4;
            ui.OpenSettings();
        }

        if (state == 4)
        {
            var gamepad = engine.Input.FirstGamepad;
            if (engine.Input.WasKeyPressed(Key.Escape) || gamepad?.WasButtonPressed(GamepadButton.B) == true)
            {
                ui.CloseSettings();
                state = returnState;
                return;
            }

            if (engine.Input.WasKeyPressed(Key.Up) || gamepad?.WasButtonPressed(GamepadButton.DPadUp) == true)
                ui.MoveSettingsSelection(-1);
            if (engine.Input.WasKeyPressed(Key.Down) || gamepad?.WasButtonPressed(GamepadButton.DPadDown) == true)
                ui.MoveSettingsSelection(1);

            var adjustment = 0;
            if (engine.Input.WasKeyPressed(Key.Left) || gamepad?.WasButtonPressed(GamepadButton.DPadLeft) == true)
                adjustment = -1;
            if (engine.Input.WasKeyPressed(Key.Right) || gamepad?.WasButtonPressed(GamepadButton.DPadRight) == true)
                adjustment = 1;

            if (adjustment != 0)
            {
                ui.AdjustSelectedSetting(engine.Settings, adjustment);
                ApplyRuntimeSettings();
            }

            if (engine.Input.WasKeyPressed(Key.Enter) ||
                engine.Input.WasKeyPressed(Key.Space) ||
                gamepad?.WasButtonPressed(GamepadButton.A) == true)
            {
                if (ui.IsSaveSettingSelected)
                {
                    if (engine.Settings.Save())
                    {
                        Log.Info("Settings saved.");
                        ui.CloseSettings();
                        state = returnState;
                    }
                }
                else
                {
                    ui.AdjustSelectedSetting(engine.Settings, 1);
                    ApplyRuntimeSettings();
                }
            }

            return;
        }

        if (state == 0 && actions.WasPressed("jump"))
        {
            state = 5;
            ui.ShowDialogue("A SIGNAL IN THE DARK", StoryCampaign.Prologue);
            return;
        }

        if (state == 5 && actions.WasPressed("jump"))
        {
            if (ui.AdvanceDialogue())
                state = 1;
        }

        if (state == 6 && (actions.WasPressed("jump") || actions.WasPressed("restart")))
        {
            story.Reset();
            score = 0;
            RestartGame();
            state = 5;
            ui.ShowDialogue("A SIGNAL IN THE DARK", StoryCampaign.Prologue);
        }

        if (state == 1 && actions.WasPressed("pause"))
        {
            state = 2;
            ui.Pause();
        }

        if (state == 2 && actions.WasPressed("pause"))
        {
            state = 1;
            ui.Resume();
        }

        if (state == 3)
        {
            respawnCooldown += deltaTime;
            ui.OnUpdate(deltaTime);
            if (actions.WasPressed("restart") || actions.WasPressed("jump") || respawnCooldown >= 1.2f)
            {
                RestartGame();
            }
        }

        // --- Only simulate when playing ---
        if (state != 1)
            return;

        accumulator += deltaTime;
        if (accumulator > 0.25f)
            accumulator = 0.25f;

        while (accumulator >= fixedStep)
        {
            scene.FixedUpdate(fixedStep);
            accumulator -= fixedStep;
        }

        scene.Update(deltaTime);

        // --- Game logic ---

        // Distance traveled (player moves right from start)
        distance = story.Distance(playerEnt.Position.X);
        ui.ChapterProgress = story.ChapterProgress(playerEnt.Position.X);
        if (story.TryCompleteChapter(playerEnt.Position.X, out var nextChapter, out var campaignComplete))
        {
            distance = story.Distance(playerEnt.Position.X);
            ui.ChapterProgress = story.ChapterProgress(playerEnt.Position.X);
            bestDistance = MathF.Max(bestDistance, distance);
            hasBestDistance = true;
            ui.Distance = distance;
            ui.BestDistance = bestDistance;
            ui.Score = score;
            ui.SurvivalTime = survivalTime;

            if (campaignComplete)
            {
                state = 6;
                ui.ShowEnding();
            }
            else if (nextChapter is not null)
            {
                ui.ChapterTitle = nextChapter.Title;
                scene.Lighting.DayNight.SetHour(19f + story.CompletedChapters * 1.5f);
                state = 5;
                ui.ShowDialogue(nextChapter.Title, nextChapter.Pages);
            }

            return;
        }

        if (distance > bestDistance || !hasBestDistance)
        {
            bestDistance = distance;
            hasBestDistance = true;
        }
        ui.Distance = distance;
        ui.BestDistance = hasBestDistance ? bestDistance : 0;

        // Survival time
        survivalTime += deltaTime;
        ui.SurvivalTime = survivalTime;

        // --- Coin collection ---
        foreach (var coinEnt in scene.FindByTag("coin").ToArray())
        {
            var coin = coinEnt.GetComponent<Coin>();
            if (coin is null)
                continue;

            if (coin.TouchedBy(playerEnt))
            {
                scene.Destroy(coinEnt);
                score += coin.Value;
                ui.Score = score;
                if (coinSound is not null)
                    engine.Audio.Play(coinSound, volume: 0.6f);
            }
        }

        // --- Hazard collision ---
        foreach (var hazardEnt in scene.FindByTag("hazard").ToArray())
        {
            var hazard = hazardEnt.GetComponent<Hazard>();
            if (hazard is not null && hazard.TouchedBy(playerEnt))
            {
                state = 3;
                respawnCooldown = 0f;
                bestDistance = MathF.Max(bestDistance, distance);
                hasBestDistance = true;
                ui.BestDistance = bestDistance;
                ui.Score = score;
                ui.Distance = distance;
                ui.SurvivalTime = survivalTime;
                ui.GameOver();
                if (dieSound is not null)
                    engine.Audio.Play(dieSound, volume: 0.7f);
                break;
            }
        }

        // --- Fall death & respawn ---
        var camY = scene.Camera?.Position.Y ?? 360f;
        if (playerEnt.Position.Y > camY + 240f || playerEnt.Position.Y > 600f)
        {
            state = 3;
            respawnCooldown = 0f;
            bestDistance = MathF.Max(bestDistance, distance);
            hasBestDistance = true;
            ui.BestDistance = bestDistance;
            ui.Score = score;
            ui.Distance = distance;
            ui.SurvivalTime = survivalTime;
            ui.GameOver();
            if (dieSound is not null)
                engine.Audio.Play(dieSound, volume: 0.7f);
        }
    },
    render: () =>
    {
        // Dynamically tint the background clear color from the day/night cycle
        var ambientBg = scene.Lighting.Enabled
            ? scene.Lighting.AmbientColor
            : scene.BackgroundColor;
        engine.Window.Clear(ambientBg.R, ambientBg.G, ambientBg.B);

        // Camera follows player smoothly during play, framed on playable zone
        if (state == 1 && scene.Camera is not null)
        {
            var targetCamY = Math.Clamp(playerEnt.Position.Y, 260f, 440f);
            scene.Camera.Follow(new Vector2(playerEnt.Position.X + 140f, targetCamY), 0.12f);
        }

        // Keep atmospheric entities centered on the camera so particles and fog
        // follow the player throughout the run.
        if (scene.Camera is not null)
        {
            motesEnt.Position = scene.Camera.Position;
            fogEnt.Position = scene.Camera.Position;
            godRaysEnt.Position = scene.Camera.Position;
        }

        // Apply settings toggles to visual systems every frame
        var settings = engine.Settings;
        scene.Lighting.Enabled = settings.EnableLighting;
        godRays.Entity.Enabled = settings.EnableGodRays;
        atmoParticles.Entity.Enabled = settings.EnableAtmosphericParticles;
        fog.Entity.Enabled = settings.EnableFog;
        scene.Camera!.MaxShakeOffset = settings.EnableScreenShake ? 10f : 0f;

        scene.Draw(engine.Renderer);

        // Cinematic vignette overlay (after scene, before HUD)
        if (settings.EnableVignette)
            engine.Renderer.DrawVignette(0.75f);

        ui.OnDraw(engine.Renderer);
        debugOverlay.Draw(engine.Renderer, scene, Time.UnscaledDeltaTime);

        if (screenshotPath is null)
            DrawGamepadStatus(engine.Renderer, smallFont, engine.Input.FirstGamepad);

        if (screenshotPath is not null && !screenshotTaken)
        {
            renderedFrames++;

            if (renderedFrames >= screenshotFrame)
            {
                engine.Renderer.SaveScreenshot(screenshotPath);
                screenshotTaken = true;
            }
        }
    });

static void DrawGamepadStatus(Renderer renderer, BitmapFont font, GamepadDevice? gamepad)
{
    renderer.Camera = null;
    renderer.DrawRectangle(new Rectangle(12, 650, 570, 52), new Color(12, 16, 24, 210));
    renderer.DrawRectangleOutline(new Rectangle(12, 650, 570, 52), new Color(80, 120, 160));

    if (gamepad is null)
    {
        font.Draw(renderer, "GAMEPAD: NONE - CONNECT A CONTROLLER TO TEST", new Vector2(22, 662), new Color(230, 240, 255), 1.1f);
        return;
    }

    var name = gamepad.Name.ToUpperInvariant();
    if (name.Length > 36)
        name = name[..36];
    font.Draw(renderer, $"GAMEPAD {gamepad.InstanceId}: {name}", new Vector2(22, 658), new Color(230, 240, 255), 1.1f);
    font.Draw(renderer,
        $"STICK X {gamepad.GetAxis(GamepadAxis.LeftX):0.00}  A {(gamepad.IsButtonDown(GamepadButton.A) ? "DOWN" : "UP")}  B {(gamepad.IsButtonDown(GamepadButton.B) ? "DOWN" : "UP")}",
        new Vector2(22, 676), new Color(230, 240, 255), 1.1f);
}

void ApplyRuntimeSettings()
{
    engine.Settings.NotifyChanged();
}

static string? GetOption(string[] arguments, string name)
{
    for (var i = 0; i < arguments.Length - 1; i++)
    {
        if (arguments[i] == name)
            return arguments[i + 1];
    }

    return null;
}

static int GetIntOption(string[] arguments, string name, int fallback)
    => int.TryParse(GetOption(arguments, name), out var value) && value > 0 ? value : fallback;

static bool HasFlag(string[] arguments, string name)
    => Array.IndexOf(arguments, name) >= 0;
