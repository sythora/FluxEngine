using System.Text.Json;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

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
TestGameTimer();
TestLevelLoader();
TestGameManager();
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
