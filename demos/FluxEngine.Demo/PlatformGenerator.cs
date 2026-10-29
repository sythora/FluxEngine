using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Graphics.Lighting;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// Procedurally generates platforms ahead of the camera and cleans up old ones.
// Ensures every new platform is reachable based on the player's movement and
// jump parameters. Difficulty increases over time by tightening gaps and
// adding elevation changes.
public sealed class PlatformGenerator : Component
{
    private readonly PlayerController _player;
    private readonly Rng _rng;
    private readonly float _screenWidth;
    private readonly List<Entity> _hazards = [];
    private readonly List<Entity> _coins = [];
    private int _spawnedPlatforms;

    private float _minGap = 55f;
    private float _maxGap = 90f;
    private float _minPlatformWidth = 160f;
    private float _maxPlatformWidth = 220f;
    private float _maxHeightDiff = 55f;
    private float _nextSpawnX;
    private float _difficultyTimer;

    private readonly List<Entity> _platforms = [];

    public PlatformGenerator(PlayerController player, Rng rng, float screenWidth)
    {
        _player = player;
        _rng = rng;
        _screenWidth = screenWidth;
    }

    public override void OnStart()
    {
        // A wide landing sits just below the player spawn, so a new run starts
        // on safe ground instead of falling while the first random platforms load.
        SpawnPlatform(new Vector2(250f, 360f), 520f);
        _nextSpawnX = 510f; // right edge of the starter platform
    }

    public void Reset()
    {
        _platforms.Clear();
        _hazards.Clear();
        _coins.Clear();
        _spawnedPlatforms = 0;
        _nextSpawnX = 510f;
        _difficultyTimer = 0;
        _minGap = 55f;
        _maxGap = 90f;
        _minPlatformWidth = 160f;
        _maxPlatformWidth = 220f;
        _maxHeightDiff = 55f;

        SpawnPlatform(new Vector2(250f, 360f), 520f);
    }

    public override void OnUpdate(float deltaTime)
    {
        _difficultyTimer += deltaTime;

        if (_difficultyTimer > 10f)
        {
            _difficultyTimer = 0;
            _minGap = MathF.Min(95f, _minGap + 4f);
            _maxGap = MathF.Min(135f, _maxGap + 4f);
            _maxHeightDiff = MathF.Min(85f, _maxHeightDiff + 3f);
        }

        var cameraX = Entity.Scene?.Camera?.Position.X ?? 0;
        var spawnThreshold = cameraX + _screenWidth * 1.5f;

        while (_nextSpawnX < spawnThreshold)
        {
            SpawnPlatform();
        }

        var cleanupX = cameraX - _screenWidth * 2f;
        Cleanup(_platforms, cleanupX);
        Cleanup(_hazards, cleanupX);
        Cleanup(_coins, cleanupX);
    }

    private static void Cleanup(List<Entity> entities, float cleanupX)
    {
        for (var i = entities.Count - 1; i >= 0; i--)
        {
            if (entities[i].Position.X < cleanupX)
            {
                entities[i].Destroy();
                entities.RemoveAt(i);
            }
        }
    }

    private void SpawnPlatform()
    {
        var scene = Entity.Scene;
        if (scene is null)
            return;

        var width = _rng.NextFloat(_minPlatformWidth, _maxPlatformWidth);
        var maxGap = MathF.Min(_maxGap, CalculateMaxJumpDistance() * 0.55f);
        var gap = _rng.NextFloat(_minGap, MathF.Max(_minGap + 1f, maxGap));
        var x = _nextSpawnX + gap + width * 0.5f;

        var maxYDiff = MathF.Min(_maxHeightDiff, CalculateMaxJumpHeight() * 0.35f);
        var lastY = _platforms.Count > 0 ? _platforms[^1].Position.Y : 360f;
        var y = lastY + _rng.NextFloat(-maxYDiff, maxYDiff);
        var cameraY = scene.Camera?.Position.Y ?? 360f;
        y = MathUtil.Clamp(y, cameraY - 130f, cameraY + 220f);

        SpawnPlatform(new Vector2(x, y), width);
        _nextSpawnX = x + width * 0.5f;
    }


    private void SpawnPlatform(Vector2 position, float width)
    {
        var scene = Entity.Scene;
        if (scene is null)
            return;

        var platform = scene.Spawn(position, "platform");
        platform.Tag = "platform";
        var collider = platform.AddComponent<OneWayPlatform>();
        collider.Size = new Vector2(width, 24);

        var art = platform.AddComponent<PlatformArt>();
        art.Size = collider.Size;
        _platforms.Add(platform);
        _spawnedPlatforms++;

        if (_spawnedPlatforms > 1 && _spawnedPlatforms % 2 == 0)
        {
            var coin = scene.Spawn(new Vector2(position.X, position.Y - 58f), "coin");
            coin.Tag = "coin";
            coin.AddComponent<Coin>();
            coin.AddComponent(new Light2D { Radius = 70f, Color = new Color(255, 220, 80), Intensity = 0.85f });
            _coins.Add(coin);
        }

        if (_spawnedPlatforms > 1 && _spawnedPlatforms % 4 == 0)
        {
            var hazard = scene.Spawn(new Vector2(position.X, position.Y - 22f), "hazard");
            hazard.Tag = "hazard";
            hazard.AddComponent<Hazard>();
            hazard.AddComponent(new Light2D { Radius = 50f, Color = new Color(255, 60, 40), Intensity = 0.7f });
            _hazards.Add(hazard);
        }
    }

    private float CalculateMaxJumpDistance()
    {
        var jumpVel = _player.JumpForce;
        var gravity = _player.Gravity;
        var moveSpeed = _player.MoveSpeed;

        var timeToApex = MathF.Abs(jumpVel) / gravity;
        var totalTime = timeToApex * 2f;
        return moveSpeed * totalTime * 0.8f;
    }

    private float CalculateMaxJumpHeight()
    {
        var jumpVel = _player.JumpForce;
        var gravity = _player.Gravity;
        return (jumpVel * jumpVel) / (2f * gravity) * 0.8f;
    }
}
