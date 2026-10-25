using FluxEngine.Core;
using FluxEngine.Graphics;
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

    private float _minGap = 110f;
    private float _maxGap = 190f;
    private float _minPlatformWidth = 70f;
    private float _maxPlatformWidth = 140f;
    private float _maxHeightDiff = 100f;
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
        _nextSpawnX = -_screenWidth * 0.5f;
    }

    public void Reset()
    {
        _platforms.Clear();
        _hazards.Clear();
        _coins.Clear();
        _spawnedPlatforms = 0;
        _nextSpawnX = -_screenWidth * 0.5f;
        _difficultyTimer = 0;
        _minGap = 110f;
        _maxGap = 190f;
        _minPlatformWidth = 70f;
        _maxPlatformWidth = 140f;
        _maxHeightDiff = 100f;
    }

    public override void OnUpdate(float deltaTime)
    {
        _difficultyTimer += deltaTime;

        if (_difficultyTimer > 10f)
        {
            _difficultyTimer = 0;
            _minGap = MathF.Min(150f, _minGap + 5f);
            _maxGap = MathF.Min(220f, _maxGap + 5f);
            _maxHeightDiff = MathF.Min(130f, _maxHeightDiff + 5f);
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

        var minGap = _minGap;
        var maxGap = MathF.Max(minGap + 1f, MathF.Min(_maxGap, CalculateMaxJumpDistance()));

        var gap = _rng.NextFloat(minGap, maxGap);
        var x = _nextSpawnX + gap;

        var maxYDiff = MathF.Min(_maxHeightDiff, CalculateMaxJumpHeight());
        var lastY = _platforms.Count > 0
            ? _platforms[^1].Position.Y
            : 0;

        var y = lastY + _rng.NextFloat(-maxYDiff, maxYDiff);

        var cameraY = scene.Camera?.Position.Y ?? 0;
        var screenBottom = cameraY + 300f;
        var screenTop = cameraY - 200f;
        y = MathUtil.Clamp(y, screenTop, screenBottom);

        var platform = scene.Spawn(new Vector2(x, y), "platform");
        platform.Tag = "platform";
        var collider = platform.AddComponent<OneWayPlatform>();
        collider.Size = new Vector2(width, 20);

        var sprite = platform.AddComponent<SpriteRenderer>();
        sprite.Size = new Vector2(width, 20);
        sprite.Color = new Color(100, 160, 80);
        sprite.Layer = 5;

        _platforms.Add(platform);
        _spawnedPlatforms++;

        if (_spawnedPlatforms % 2 == 0)
        {
            var coin = scene.Spawn(new Vector2(x + width * 0.5f, y - 55f), "coin");
            coin.Tag = "coin";
            coin.AddComponent<Coin>();
            _coins.Add(coin);
        }

        if (_spawnedPlatforms % 4 == 0)
        {
            var hazardX = x + width * 0.5f;
            var hazard = scene.Spawn(new Vector2(hazardX, y - 22f), "hazard");
            hazard.Tag = "hazard";
            hazard.AddComponent<Hazard>();
            _hazards.Add(hazard);
        }

        _nextSpawnX = x + width;
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
