using System.Text.Json;
using FluxEngine.Audio;
using FluxEngine.Content;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;
using Color = FluxEngine.Mathematics.Color;
using FluxEngine.Physics;
using StbImageSharp;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Near(float actual, float expected, string message)
    => Check(MathF.Abs(actual - expected) < 0.001f, $"{message} Expected {expected}, got {actual}.");

var background = new ColorSave(12, 34, 56, 255);
var liveObjects = new List<EditableObject>
{
    new PlayerObject { Position = new Vector2(123.5f, 234.5f) },
    new PlatformObject { Position = new Vector2(300, 400), W = 160, H = 24, OneWay = true },
    new CoinObject { Position = new Vector2(500, 200) },
    new SpikeObject { Position = new Vector2(600, 300) },
    new CheckpointObject { Position = new Vector2(700, 310) },
    new GoalObject { Position = new Vector2(900, 200) },
    new MoverObject
    {
        Position = new Vector2(1000, 250), Start = new Vector2(1000, 250),
        End = new Vector2(1200, 260), Speed = 77,
    },
};

var edited = SceneObjectSerializer.Capture(liveObjects, new SceneFile
{
    PlayerStart = new Vector2(1, 2), BackgroundColor = background,
});
Check(edited.PlayerStart == new Vector2(123.5f, 234.5f), "Player edits must be saved.");
Check(edited.BackgroundColor.R == 12 && edited.BackgroundColor.G == 34, "Background metadata must be preserved.");
Check(edited.Platforms.Count == 1 && edited.Platforms[0].X == 220 && edited.Platforms[0].Y == 388, "Platform bounds must round-trip from center coordinates.");
Check(edited.Platforms[0].OneWay && edited.Platforms[0].W == 160 && edited.Platforms[0].H == 24, "Platform properties must be saved.");
Check(edited.Coins.Count == 1 && edited.Spikes.Count == 1 && edited.Checkpoints.Count == 1 && edited.Goals.Count == 1, "All placed object types must be saved.");
Check(edited.Movers.Count == 1 && edited.Movers[0].EndX == 1200 && edited.Movers[0].Speed == 77, "Mover endpoints and speed must be saved.");

var scenePath = TempPath(".scene");
try
{
    EditorSceneSerializer.Save(scenePath, edited);
    var reopened = EditorSceneSerializer.Load(scenePath);
    Check(reopened.PlayerStart == edited.PlayerStart, "Player start must persist in JSON.");
    Check(reopened.BackgroundColor.R == edited.BackgroundColor.R, "Background color must persist in JSON.");
    var reopenedObjects = SceneObjectsFromFile(reopened);
    var savedAgain = SceneObjectSerializer.Capture(reopenedObjects, reopened);
    Check(Serialize(edited) == Serialize(savedAgain), "Save/reopen/save must preserve every serialized scene value.");

    var enginePath = TempPath(".scene");
    try
    {
        SceneSerializer.Save(enginePath, edited);
        var engineLoaded = SceneSerializer.Load(enginePath);
        Check(engineLoaded.PlayerStart == edited.PlayerStart && engineLoaded.Platforms.Count == edited.Platforms.Count,
            "The engine serializer must read and write the scene-file format.");
    }
    finally
    {
        File.Delete(enginePath);
    }
}
finally
{
    File.Delete(scenePath);
}

TestCollisionResolution();
TestPhysicsQueries();
TestAnimationClips();
TestEntityLifecycle();
TestSceneLoopAndBrowserRenderer();
TestGameTimer();
TestLevelLoader();
TestGameManager();
TestAudioManager();
TestEngineSettings();
TestStoryCampaign();
TestPngRoundTrip();
TestBuiltInFontMaps();
TestDayNightAndLighting();
Console.WriteLine("FluxEngine regression tests passed.");

static List<EditableObject> SceneObjectsFromFile(SceneFile scene)
{
    var objects = new List<EditableObject> { new PlayerObject { Position = scene.PlayerStart } };
    objects.AddRange(scene.Platforms.Select(p => new PlatformObject
    {
        Position = new Vector2(p.X + p.W / 2, p.Y + p.H / 2), W = p.W, H = p.H, OneWay = p.OneWay,
    }));
    objects.AddRange(scene.Coins.Select(c => new CoinObject { Position = new Vector2(c.X, c.Y) }));
    objects.AddRange(scene.Spikes.Select(s => new SpikeObject { Position = new Vector2(s.X, s.Y) }));
    objects.AddRange(scene.Checkpoints.Select(c => new CheckpointObject { Position = new Vector2(c.X, c.Y) }));
    objects.AddRange(scene.Goals.Select(g => new GoalObject { Position = new Vector2(g.X, g.Y) }));
    objects.AddRange(scene.Movers.Select(m => new MoverObject
    {
        Position = new Vector2(m.StartX, m.StartY), Start = new Vector2(m.StartX, m.StartY),
        End = new Vector2(m.EndX, m.EndY), Speed = m.Speed,
    }));
    return objects;
}

static string Serialize(SceneFile scene) => JsonSerializer.Serialize(scene, new JsonSerializerOptions
{
    Converters = { new Vector2JsonConverter() }, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
});

static void TestCollisionResolution()
{
    var a = new BoxCollider(new Vector2(0, 0), new Vector2(10, 10));
    var b = new BoxCollider(new Vector2(8, 0), new Vector2(10, 10));
    Check(Collision.Overlaps(a, b), "Overlapping boxes should register collision.");
    Check(Collision.Separate(a, b) == new Vector2(-2, 0), "Boxes should separate along the smallest overlap axis.");
    Check(Collision.Overlaps(new CircleCollider(new Vector2(0, 0), 2), new CircleCollider(new Vector2(4, 0), 2)),
        "Touching circles should register collision.");
}

static void TestPhysicsQueries()
{
    var scene = new Scene();
    var staticSolid = new BoxCollider(new Vector2(10, 10), new Vector2(8, 8));
    scene.Solids.Add(staticSolid);

    var triggerEntity = scene.Spawn(new Vector2(14, 10), "trigger");
    var trigger = triggerEntity.AddComponent<LifecycleCollider>();
    trigger.Size = new Vector2(6, 6);
    trigger.IsTrigger = true;

    var colliderEntity = scene.Spawn("solid collider");
    var collider = colliderEntity.AddComponent<LifecycleCollider>();
    collider.Size = new Vector2(4, 4);
    colliderEntity.Position = new Vector2(30, 10);
    scene.Start();

    var boxHits = PhysicsQueries.OverlapBox(scene, new BoxCollider(new Vector2(10, 10), new Vector2(12, 12)));
    Check(boxHits.Count == 2 && boxHits.Any(hit => hit.Entity is null) && boxHits.Any(hit => hit.Entity == triggerEntity),
        "Box overlap queries should include static solids and entity colliders.");
    var filteredHits = PhysicsQueries.OverlapCircle(scene, new CircleCollider(new Vector2(10, 10), 8), includeTriggers: false);
    Check(filteredHits.Count == 1 && filteredHits[0].Entity is null,
        "Circle overlap queries should support excluding trigger colliders.");

    Check(PhysicsQueries.Raycast(scene, new Vector2(0, 10), new Vector2(2, 0), 30, out var hit), "Raycast should hit the nearest collider.");
    Near(hit.Distance, 6, "Raycast distance should use normalized direction and point to the nearest hit.");
    Check(hit.Point == new Vector2(6, 10) && hit.Normal == new Vector2(-1, 0) && hit.Entity is null,
        "Raycast should report the hit point, surface normal, and static-solid identity.");
    Check(!PhysicsQueries.Raycast(scene, Vector2.Zero, Vector2.UnitY, 5, out _), "Raycast should miss beyond its maximum distance.");
    Check(!PhysicsQueries.Raycast(scene, Vector2.Zero, Vector2.Zero, 30, out _), "Zero-direction raycasts should safely miss.");
}

static void TestAnimationClips()
{
    var first = new Sprite();
    var second = new Sprite();
    var animation = new AnimatedSpriteRenderer { PlayOnStart = false };
    animation.AddClip(new AnimationClip("Run", [first, second], frameTime: 0.1f));
    animation.AddClip(new AnimationClip("Death", [first, second], frameTime: 0.1f, loop: false));

    Check(animation.Play("Run") && animation.CurrentClipName == "Run", "Named clips should start and expose their active name.");
    animation.OnUpdate(0.1f);
    Check(animation.CurrentFrame == 1, "Clip frame time should advance to the next sprite.");
    animation.PlaybackSpeed = 2;
    animation.OnUpdate(0.05f);
    Check(animation.CurrentFrame == 0, "Playback speed should scale frame advancement and looping should wrap.");

    Check(animation.Play("Death"), "A registered non-looping clip should play.");
    animation.OnUpdate(0.2f);
    Check(animation.IsFinished && animation.CurrentFrame == 1, "Non-looping clips should stop on their final frame.");
    Check(!animation.Play("Missing"), "Playing an unknown clip should return false.");
    Check(animation.RemoveClip("Death") && !animation.Clips.ContainsKey("Death"), "Clips should be removable by name.");

    try
    {
        _ = new AnimationClip("Invalid", [first], 0);
        throw new InvalidOperationException("Zero-duration clips should be rejected.");
    }
    catch (ArgumentOutOfRangeException)
    {
    }
}

static void TestEntityLifecycle()
{
    var scene = new Scene();
    var entity = scene.Spawn("test");
    var component = entity.AddComponent<LifecycleProbe>();
    var collider = entity.AddComponent<LifecycleCollider>();
    scene.Start();
    Check(component.Starts == 1 && scene.Colliders.Count == 1, "Components should start once and register colliders.");

    component.Destroy();
    scene.Update(0);
    Check(component.Updates == 0 && entity.Components.Count == 1, "Destroyed components should be pruned without updating.");
    Check(scene.Colliders.Count == 1, "Removing an unrelated component should retain the collider.");

    collider.Destroy();
    scene.Update(0);
    Check(scene.Colliders.Count == 0 && entity.Components.Count == 0, "Removing a collider component should unregister it.");

    var destroyed = scene.Spawn("destroyed");
    destroyed.AddComponent<LifecycleProbe>();
    destroyed.Destroy();
    scene.Update(0);
    Check(scene.Find("destroyed") is null && destroyed.Scene is null, "Destroyed entities should be detached from their scene.");

    var clearEntity = scene.Spawn("clear");
    var reusable = clearEntity.AddComponent<LifecycleCollider>();
    scene.Start();
    scene.Clear();
    Check(scene.EntityCount == 0 && scene.Colliders.Count == 0 && clearEntity.Scene is null && reusable.Entity is null,
        "Clearing a scene should unregister colliders and detach reusable components.");

    var nextScene = new Scene();
    var nextEntity = nextScene.Spawn("reused");
    nextEntity.AddComponent(reusable);
    nextScene.Start();
    Check(nextScene.Colliders.Count == 1, "A component detached by Clear should be addable to another scene.");
}

static void TestGameTimer()
{
    var fired = 0;
    var repeating = new GameTimer(0.5f, repeating: true) { OnTick = () => fired++ };
    Check(!repeating.Update(0.49f) && fired == 0, "Timer must not fire before its duration.");
    Check(repeating.Update(0.51f) && fired == 1, "Repeating timer should fire at its duration.");
    Near(repeating.Progress, 1, "Repeating timer progress is the fraction elapsed during the next interval.");
    repeating.Paused = true;
    Check(!repeating.Update(1) && fired == 1, "Paused timers must not advance.");

    var once = new GameTimer(1f, repeating: false);
    Check(once.Update(1f) && once.Paused, "One-shot timer should pause after firing.");
    Check(!once.Update(1f), "One-shot timer should not fire twice.");
    once.Restart();
    Check(!once.Paused && once.Remaining == 1, "Restart should reset timer state.");
}

static void TestSceneLoopAndBrowserRenderer()
{
    var scene = new Scene();
    var probe = scene.Spawn("loop-probe").AddComponent<SceneLoopProbe>();
    var loop = new SceneLoop(scene, fixedTimeStep: 0.1f, maxFrameDelta: 0.25f);
    loop.Start();
    loop.AdvanceFrame(0.35f);

    Check(probe.FixedUpdates == 2, "SceneLoop should clamp long frame deltas and run fixed updates.");
    Check(probe.VariableUpdates == 1, "SceneLoop should run one variable update per frame.");
    Near(probe.LastDeltaTime, 0.25f, "SceneLoop clamped delta");
    Near(loop.Interpolation, 0.5f, "SceneLoop interpolation fraction");
    loop.Stop();

    var renderScene = new Scene();
    renderScene.Spawn("box", new Vector2(30, 40)).AddComponent(new SpriteRenderer
    {
        Size = new Vector2(20, 12),
        Color = new Color(120, 80, 200),
    });

    var renderer = Renderer.CreateBrowser(100, 80);
    renderer.BeginBrowserFrame(Color.Black);
    renderScene.Draw(renderer);
    Check(renderer.BrowserCommands.Count == 2, "Browser renderer should collect a clear and sprite-box command.");
    Check(renderer.BrowserCommands[0].Type == "clear", "Browser renderer should clear each frame.");
    Check(renderer.BrowserCommands[1].Type == "rectangle", "Browser renderer should preserve scene sprite drawing.");
    Check(renderer.BrowserCommands[1].Values.SequenceEqual(new float[] { 20, 34, 20, 12 }),
        "Browser renderer should record the expected centered rectangle.");

    renderer.DrawBrowserText("SIGNAL", new Vector2(10, 12), Color.White, centered: true);
    renderer.DrawBrowserPolygon(
        [new Vector2(1, 2), new Vector2(3, 4), new Vector2(5, 6)],
        Color.Cyan,
        filled: true,
        screenSpace: false);
    Check(renderer.BrowserCommands[2].Type == "text" && renderer.BrowserCommands[2].Text == "SIGNAL",
        "Browser renderer should preserve text labels for Canvas rendering.");
    Check(renderer.BrowserCommands[2].ScreenSpace, "Browser HUD commands should opt out of the camera transform.");
    Check(renderer.BrowserCommands[3].Type == "filled-polygon" && !renderer.BrowserCommands[3].ScreenSpace,
        "Browser renderer should record filled world-space polygons.");
}

static void TestLevelLoader()
{
    var path = TempPath(".txt");
    try
    {
        var level = new LevelData
        {
            PlayerStart = new Vector2(1.25f, 2.5f),
            BackgroundColor = new FluxEngine.Mathematics.Color(11, 22, 33),
        };
        level.Platforms.Add(new PlatformDef { Position = new Vector2(50.5f, 60.25f), Size = new Vector2(80, 12) });
        level.Coins.Add(new CoinDef { Position = new Vector2(90, 100) });
        level.Spikes.Add(new SpikeDef { Position = new Vector2(110, 120) });
        level.Checkpoints.Add(new CheckpointDef { Position = new Vector2(130, 140) });
        level.Goals.Add(new GoalDef { Position = new Vector2(150, 160) });
        level.Movers.Add(new MoverDef { Start = new Vector2(170, 180), End = new Vector2(190, 200), Speed = 77 });

        LevelLoader.Save(path, level);
        var loaded = LevelLoader.Load(path);
        Check(loaded.PlayerStart == level.PlayerStart && loaded.BackgroundColor == level.BackgroundColor, "Level player and background should round-trip.");
        Check(loaded.Platforms.Count == 1 && loaded.Platforms[0].Position == level.Platforms[0].Position, "Platforms should round-trip.");
        Check(loaded.Coins.Count == 1 && loaded.Spikes.Count == 1 && loaded.Checkpoints.Count == 1 && loaded.Goals.Count == 1 && loaded.Movers.Count == 1,
            "All supported level records should load.");
        Near(loaded.Movers[0].Speed, 77, "Mover speed should round-trip.");

        File.WriteAllText(path, "PLAYER nope 2");
        try
        {
            LevelLoader.Load(path);
            throw new InvalidOperationException("Malformed level lines should report an error.");
        }
        catch (FormatException exception)
        {
            Check(exception.Message.Contains("PLAYER nope 2", StringComparison.Ordinal), "Malformed level errors should identify the bad line.");
        }
    }
    finally
    {
        File.Delete(path);
    }
}

static void TestGameManager()
{
    var scene = new Scene();
    var player = scene.Spawn(new Vector2(10, 20), "player");
    var body = player.AddComponent<Rigidbody2D>();
    body.Velocity = new Vector2(30, 40);
    var manager = scene.Spawn("manager").AddComponent(new GameManager(player));
    manager.AddScore(25);
    Check(manager.Score == 25 && manager.Lives == 3, "Game manager should track score and initial lives.");
    manager.Die();
    Check(manager.Lives == 2 && !player.Enabled && !manager.GameOver, "A non-final death should begin a respawn.");
    manager.OnUpdate(1.1f);
    Check(player.Enabled && player.Position == new Vector2(10, 20) && body.Velocity == Vector2.Zero, "Respawn should restore position and clear velocity.");
    manager.Die();
    manager.OnUpdate(1.1f);
    manager.Die();
    manager.OnUpdate(1.1f);
    Check(manager.Lives == 0 && manager.GameOver, "Losing the final life should end the game.");
    manager.ReachGoal();
    Check(manager.Won, "Reaching the goal should record a win.");
}

static string TempPath(string extension) => Path.Combine(Path.GetTempPath(), $"fluxengine-{Guid.NewGuid():N}{extension}");

static void TestAudioManager()
{
    var path = TempPath(".wav");
    WriteTestWave(path);

    try
    {
        using var audio = new AudioManager();
        audio.SetPaused(true);
        var sound = audio.LoadSound(path);
        var sameSound = audio.LoadSound(Path.Combine(Path.GetDirectoryName(path)!, ".", Path.GetFileName(path)));

        Check(ReferenceEquals(sound, sameSound) && audio.SoundCount == 1, "Audio loads should be cached by normalized full path.");
        Check(MathF.Abs(sound.Duration - 4f / 22050f) < 0.000001f,
            "WAV duration should reflect the source frame count and sample rate.");
        Check(audio.IsPaused, "Pausing before device creation should retain the requested state.");

        var invalidPath = TempPath(".wav");
        try
        {
            File.WriteAllBytes(invalidPath, "not a wave file"u8.ToArray());
            try
            {
                audio.LoadSound(invalidPath);
                throw new InvalidOperationException("Malformed WAV data should be rejected.");
            }
            catch (InvalidDataException)
            {
            }

            Check(audio.SoundCount == 1, "Invalid WAV files should not enter the sound cache.");
        }
        finally
        {
            File.Delete(invalidPath);
        }

        if (audio.IsAvailable)
        {
            audio.Play(sound);
            audio.Play(sound);
            Check(audio.ActiveVoiceCount == 2, "Audio playback should allow overlapping voices.");
            audio.StopAll();
            Check(audio.ActiveVoiceCount == 0, "StopAll should clear every active voice.");
        }

        Check(audio.UnloadSound(sound) && audio.SoundCount == 0, "Unloading a cached sound should release its cache entry.");
        audio.ClearSounds();
    }
    finally
    {
        File.Delete(path);
    }
}

static void WriteTestWave(string path)
{
    const int sampleRate = 22050;
    const short channels = 1;
    const short bitsPerSample = 16;
    const int dataLength = 8;

    using var writer = new BinaryWriter(File.Create(path));
    writer.Write("RIFF"u8);
    writer.Write(36 + dataLength);
    writer.Write("WAVE"u8);
    writer.Write("fmt "u8);
    writer.Write(16);
    writer.Write((short)1);
    writer.Write(channels);
    writer.Write(sampleRate);
    writer.Write(sampleRate * channels * bitsPerSample / 8);
    writer.Write((short)(channels * bitsPerSample / 8));
    writer.Write(bitsPerSample);
    writer.Write("data"u8);
    writer.Write(dataLength);
    writer.Write((short)0);
    writer.Write((short)4096);
    writer.Write((short)0);
    writer.Write((short)-4096);
}

static void TestEngineSettings()
{
    var directory = Path.Combine(Path.GetTempPath(), $"fluxengine-{Guid.NewGuid():N}");
    var path = Path.Combine(directory, "settings.json");

    try
    {
        var settings = new EngineSettings
        {
            EnableLighting = false,
            EnableVignette = false,
            EnableGodRays = false,
            EnableAtmosphericParticles = false,
            EnableFog = false,
            EnableScreenShake = false,
            Vsync = false,
            TargetFps = 120,
            MasterVolume = 0.7f,
            SfxVolume = 0.6f,
            MusicVolume = 0.4f,
        };

        Check(settings.Save(path), "Settings should save to a newly created directory.");

        var loaded = EngineSettings.Load(path);
        Check(!loaded.EnableLighting && !loaded.EnableVignette && !loaded.EnableGodRays
            && !loaded.EnableAtmosphericParticles && !loaded.EnableFog && !loaded.EnableScreenShake,
            "Graphics settings should survive a save/load round trip.");
        Check(!loaded.Vsync && loaded.TargetFps == 120, "Display settings should survive a save/load round trip.");
        Near(loaded.MasterVolume, 0.7f, "Master volume should survive a save/load round trip.");
        Near(loaded.SfxVolume, 0.6f, "SFX volume should survive a save/load round trip.");
        Near(loaded.MusicVolume, 0.4f, "Music volume should survive a save/load round trip.");

        var bounded = new EngineSettings
        {
            TargetFps = -1,
            MasterVolume = 1.5f,
            SfxVolume = -0.5f,
            MusicVolume = float.NaN,
        };
        Check(bounded.TargetFps == 0, "Negative target frame rates should select the unlimited setting.");
        Near(bounded.MasterVolume, 1f, "Volume should be clamped to its maximum.");
        Near(bounded.SfxVolume, 0f, "Volume should be clamped to its minimum.");
        Near(bounded.MusicVolume, 0f, "Non-finite volume should safely mute its bus.");
    }
    finally
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}

static void TestStoryCampaign()
{
    var story = new StoryCampaign();
    Check(StoryCampaign.Prologue.Count >= 2 && StoryCampaign.Chapters.Count == 3 && StoryCampaign.Ending.Count >= 1,
        "The campaign should provide a prologue, three chapters, and an ending.");
    Check(story.Distance(StoryCampaign.InitialSpawnX) == 0, "A new campaign should begin at zero progress.");
    Near(story.ChapterProgress(StoryCampaign.InitialSpawnX + StoryCampaign.ChapterLength / 2f), 0.5f,
        "Chapter progress should be reported as a normalized value.");
    Check(!story.TryCompleteChapter(StoryCampaign.InitialSpawnX + StoryCampaign.ChapterLength - 1, out _, out _),
        "A chapter should not complete before the route threshold.");

    var firstThreshold = StoryCampaign.InitialSpawnX + StoryCampaign.ChapterLength;
    Check(story.TryCompleteChapter(firstThreshold, out var secondChapter, out var completed),
        "Reaching a chapter threshold should advance the campaign.");
    Check(secondChapter == StoryCampaign.Chapters[1] && !completed,
        "Completing the first chapter should return the next chapter.");
    Check(story.CheckpointX == firstThreshold && story.Distance(firstThreshold) == StoryCampaign.ChapterLength,
        "Chapter transitions should save a checkpoint and preserve route progress.");

    story.RespawnAtCheckpoint();
    Check(story.Distance(firstThreshold) == StoryCampaign.ChapterLength,
        "Respawning at a checkpoint should preserve completed chapter progress.");
    Check(story.TryCompleteChapter(firstThreshold + StoryCampaign.ChapterLength, out var finalChapter, out completed)
        && finalChapter == StoryCampaign.Chapters[2] && !completed,
        "The second chapter should unlock the final chapter.");
    Check(story.TryCompleteChapter(firstThreshold + StoryCampaign.ChapterLength * 2, out _, out completed)
        && completed && story.IsComplete,
        "The last chapter should complete the campaign.");

    story.Reset();
    Check(!story.IsComplete && story.CheckpointX == StoryCampaign.InitialSpawnX,
        "Starting a new story should reset chapter and checkpoint progress.");

    story.RestoreProgress(1, firstThreshold, firstThreshold);
    Check(story.CompletedChapters == 1 && story.Distance(firstThreshold + 50) == StoryCampaign.ChapterLength + 50,
        "Saved story progress should restore chapter and distance state.");
}

static void TestPngRoundTrip()
{
    var pixels = new byte[]
    {
        255, 0, 0, 255,
        0, 255, 0, 128,
        0, 0, 255, 64,
        10, 20, 30, 0,
    };

    using var memory = new MemoryStream();
    PngWriter.Write(memory, 2, 2, pixels);
    var png = memory.ToArray();
    var decoded = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);

    Check(decoded.Width == 2 && decoded.Height == 2, "PNG dimensions should survive a round trip.");
    Check(decoded.Data.SequenceEqual(pixels), "The PNG writer should preserve RGBA pixels and alpha exactly.");
}

static void TestBuiltInFontMaps()
{
    var press = BmFont.Parse(BuiltInFonts.ReadFontMap(BuiltInFont.PressStart2P));
    Check(press.Name == "Press Start 2P" && press.Glyphs.Count == 95, "Press Start 2P should expose the full printable ASCII map.");
    Check(press.LineHeight == 8, "Press Start 2P should use its 8px line height.");
    Check(press.Glyphs['A'].Source.Width == 8 && press.Glyphs['A'].Source.Height == 8 && press.Glyphs['A'].Advance == 8,
        "Press Start 2P should be a monospaced 8x8 font.");

    var silk = BmFont.Parse(BuiltInFonts.ReadFontMap(BuiltInFont.Silkscreen));
    Check(silk.Name == "Silkscreen" && silk.Glyphs.Count == 95, "Silkscreen should expose the full printable ASCII map.");
    Check(silk.Glyphs['A'].Advance == 6 && silk.Glyphs[' '].Advance == 4,
        "Silkscreen should keep its variable width advances.");
    Check(silk.Glyphs.Values.All(glyph => glyph.Source.Width > 0 && glyph.Source.Height > 0),
        "Every built-in glyph should have a valid atlas rectangle.");
}

static void TestDayNightAndLighting()
{
    var cycle = new DayNightCycle(12f, 120f);
    Near(cycle.TimeOfDay, 12f, "Initial hour should be 12:00.");
    Near(cycle.NormalizedTime, 0.5f, "Normalized time at noon should be 0.5.");
    Check(cycle.FormattedTime == "12:00", "Formatted time at noon should be 12:00.");
    Check(cycle.Phase == DayNightPhase.Day && cycle.IsDay && !cycle.IsNight, "12:00 should be Day phase.");
    Check(cycle.AmbientColor == new Color(255, 255, 255), "Noon ambient color should be pure white.");

    // Advance 6 hours in 30 seconds of simulation
    cycle.Update(30f);
    Near(cycle.TimeOfDay, 18f, "Advancing a quarter of day duration should advance 6 hours.");
    Check(cycle.Phase == DayNightPhase.Dusk, "18:00 should be Dusk phase.");

    cycle.SetHour(23.5f);
    Check(cycle.Phase == DayNightPhase.Night && cycle.IsNight, "23:30 should be Night phase.");
    Check(cycle.AmbientColor.R < 50 && cycle.AmbientColor.G < 50 && cycle.AmbientColor.B < 100,
        "Night ambient color should be dark navy.");

    cycle.SetHour(6.0f);
    Check(cycle.Phase == DayNightPhase.Dawn, "06:00 should be Dawn phase.");
    Check(cycle.AmbientColor.R > 200 && cycle.AmbientColor.G > 100, "Dawn ambient color should be warm golden-rose.");

    // Hour wrapping tests
    cycle.SetHour(25.5f);
    Near(cycle.TimeOfDay, 1.5f, "Hours > 24 should wrap around modulo 24.");
    cycle.SetHour(-3.0f);
    Near(cycle.TimeOfDay, 21.0f, "Negative hours should wrap back from 24.");

    // Paused cycle
    cycle.IsPaused = true;
    cycle.Update(100f);
    Near(cycle.TimeOfDay, 21.0f, "Paused cycle should not advance time.");

    // Scene and Light2D integration
    var scene = new Scene();
    Check(!scene.Lighting.Enabled, "Lighting should be disabled by default.");
    scene.Lighting.Enabled = true;

    var entity = scene.Spawn("lamp", new Vector2(100, 200));
    var light = entity.AddComponent<Light2D>();
    light.Radius = 150f;
    light.Color = new Color(255, 220, 150);
    light.Offset = new Vector2(5, -10);

    scene.Start();
    Check(scene.Lighting.Lights.Count == 1 && scene.Lighting.Lights[0] == light,
        "Light2D should automatically register with scene.Lighting.");
    Check(light.WorldPosition == new Vector2(105, 190), "Light world position should include entity position and offset.");

    // Flicker test
    Near(light.EffectiveRadius, 150f, "Non-flickering light effective radius should equal base radius.");
    light.Flicker = true;
    light.FlickerAmount = 0.2f;
    light.OnUpdate(0.1f);
    Check(MathF.Abs(light.EffectiveRadius - 150f) > 0.001f, "Flickering light should vary effective radius over time.");

    // Destroy entity cleanup
    entity.Destroy();
    scene.FixedUpdate(0.016f); // triggers ApplyDestroyQueue
    Check(scene.Lighting.Lights.Count == 0, "Destroyed entity light should unregister from scene.Lighting.");

    // Standalone light test
    scene.Lighting.AddStandaloneLight(new Vector2(50, 50), 80f, Color.Yellow);
    Check(scene.Lighting.Enabled, "Lighting system should remain enabled.");
}

sealed class SceneLoopProbe : Component
{
    public int FixedUpdates { get; private set; }
    public int VariableUpdates { get; private set; }
    public float LastDeltaTime { get; private set; }

    public override void OnFixedUpdate(float deltaTime) => FixedUpdates++;

    public override void OnUpdate(float deltaTime)
    {
        VariableUpdates++;
        LastDeltaTime = deltaTime;
    }
}

sealed class LifecycleProbe : Component
{
    public int Starts { get; private set; }
    public int Updates { get; private set; }
    public override void OnStart() => Starts++;
    public override void OnUpdate(float deltaTime) => Updates++;
}

sealed class LifecycleCollider : ColliderComponent
{
}
