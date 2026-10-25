using FluxEngine.Audio;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

const int viewWidth = 960;
const int viewHeight = 720;

using var engine = new Engine("FluxRunner - Infinite Platformer", viewWidth, viewHeight);

// ---------------------------------------------------------------------------
// Generate textures (BMP only, since SDL core has no PNG decoder)
// ---------------------------------------------------------------------------
var playerIdle = BmpGenerator.PlayerIdle();
var playerRun0 = BmpGenerator.PlayerRun0();
var playerRun1 = BmpGenerator.PlayerRun1();
var playerJump = BmpGenerator.PlayerJump();
var coinFrames = Enumerable.Range(0, 4).Select(i => BmpGenerator.Coin(i)).ToArray();

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
playerCtrl.Landed += () => { if (landSound is not null) engine.Audio.Play(landSound, 0.5f); };
playerEnt.AddComponent(playerCtrl);

var generatorEnt = scene.Spawn("generator");
var generator = new PlatformGenerator(playerCtrl, Rng.Shared, viewWidth);
generatorEnt.AddComponent(generator);

var uiEnt = scene.Spawn("ui");
var ui = new GameUI(engine.Input.Actions);
uiEnt.AddComponent(ui);

var bgEnt = scene.Spawn("background");
var bg = bgEnt.AddComponent<ParallaxBackground>();

// ---------------------------------------------------------------------------
// Game state
// ---------------------------------------------------------------------------

var state = 0; // 0=StartMenu, 1=Playing, 2=Paused, 3=GameOver
var debugOverlay = new DebugOverlay(engine.Input.Actions);
var score = 0;
var bestDistance = 0f;
var hasBestDistance = false;
var distance = 0f;
var survivalTime = 0f;
var startX = 100f;
playerEnt.Position = new Vector2(startX, 300f);
scene.Solids.Add(new BoxCollider(new Vector2(800, 624), new Vector2(3200, 40)));
scene.Solids.Add(new BoxCollider(new Vector2(-16, 320), new Vector2(40, 1280)));
scene.Solids.Add(new BoxCollider(new Vector2(1616, 320), new Vector2(40, 1280)));

// Start music (loops, safe if no audio device)
if (music is not null)
    engine.Audio.Play(music, volume: 0.4f, loop: true);

// Input bindings
var actions = engine.Input.Actions;
actions.Bind("move.left", Key.A, Key.Left);
actions.Bind("move.right", Key.D, Key.Right);
actions.Bind("jump", Key.Space, Key.Up);
actions.Bind("pause", Key.Escape);
actions.Bind("restart", Key.R);

// Helper to restart the game from scratch
void RestartGame()
{
    if (state == 3)
    {
        bestDistance = MathF.Max(bestDistance, distance);
        hasBestDistance = true;
    }

    state = 1;
    ui.Reset();
    distance = 0f;
    survivalTime = 0f;
    score = 0;
    startX = 100f;

    // Reset player
    playerEnt.Position = new Vector2(startX, 300f);
    playerCtrl.ResetJumpState();
    var body = playerEnt.GetComponent<Rigidbody2D>();
    if (body is not null)
        body.Velocity = Vector2.Zero;

    // Destroy all existing platforms, coins, hazards
    foreach (var tag in new[] { "platform", "coin", "hazard" })
    {
        foreach (var ent in scene.FindByTag(tag).ToArray())
            scene.Destroy(ent);
    }

    // Restore the base floor and bounds for the new run.
    scene.Solids.Clear();
    scene.Solids.Add(new BoxCollider(new Vector2(800, 624), new Vector2(3200, 40)));
    scene.Solids.Add(new BoxCollider(new Vector2(-16, 320), new Vector2(40, 1280)));
    scene.Solids.Add(new BoxCollider(new Vector2(1616, 320), new Vector2(40, 1280)));

    // Reset generator
    generator.Reset();
}

// Start all components' OnStart callbacks
scene.Start();

// ---------------------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------------------
var accumulator = 0.0;
const float fixedStep = 1f / 60f;

engine.Run(
    update: deltaTime =>
    {
        // --- Input handling that works in all states ---
        if (actions.WasPressed("debug.toggle"))
            debugOverlay.Enabled = !debugOverlay.Enabled;

        if (state == 0 && actions.WasPressed("jump"))
        {
            state = 1;
            ui.StartGame();
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

        if (state == 3 && actions.WasPressed("restart"))
        {
            RestartGame();
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
        distance = MathF.Max(distance, playerEnt.Position.X - startX);
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

        // --- Fall death ---
        var camY = scene.Camera?.Position.Y ?? 0;
        if (playerEnt.Position.Y > camY + 400f)
        {
            state = 3;
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
        engine.Window.Clear(scene.BackgroundColor.R, scene.BackgroundColor.G, scene.BackgroundColor.B);

        // Camera follows player smoothly during play
        if (state == 1 && scene.Camera is not null)
            scene.Camera.Follow(playerEnt.Position, 0.1f);

        scene.Draw(engine.Renderer);
        ui.OnDraw(engine.Renderer);
        debugOverlay.Draw(engine.Renderer, scene, Time.UnscaledDeltaTime);

        engine.Window.Present();
    });
