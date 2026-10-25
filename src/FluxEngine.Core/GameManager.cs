using FluxEngine.Audio;
using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// tracks score, lives, and game state. put this on an entity in your scene
public sealed class GameManager : Component
{
    public int Score { get; private set; }
    public int Lives { get; set; } = 3;
    public bool GameOver { get; private set; }
    public bool Won { get; private set; }

    public Vector2 RespawnPoint { get; set; }

    // sounds - assign them in your game code
    public Sound? JumpSound { get; set; }
    public Sound? CoinSound { get; set; }
    public Sound? DeathSound { get; set; }
    public Sound? GoalSound { get; set; }

    private readonly Entity _player;
    private float _respawnTimer;
    private bool _respawning;

    public GameManager(Entity player)
    {
        _player = player;
        RespawnPoint = player.Position;
    }

    public void AddScore(int amount)
    {
        Score += amount;
    }

    // call this from your audio code instead of directly on Sound
    public void PlaySound(Sound? sound, float volume = 1f)
    {
        if (sound is null)
            return;

        Engine.Current?.Audio.Play(sound, volume);
    }

    public void Die()
    {
        if (_respawning || GameOver || Won)
            return;

        Lives--;
        PlaySound(DeathSound);

        if (Lives <= 0)
        {
            GameOver = true;
            return;
        }

        _respawning = true;
        _respawnTimer = 1f;
        _player.Enabled = false;
    }

    public void Respawn()
    {
        _respawning = false;
        _player.Enabled = true;
        _player.Position = RespawnPoint;

        var body = _player.GetComponent<Rigidbody2D>();
        if (body is not null)
            body.Velocity = Vector2.Zero;
    }

    public void ReachGoal()
    {
        Won = true;
        PlaySound(GoalSound);
    }

    public override void OnUpdate(float deltaTime)
    {
        if (_respawning)
        {
            _respawnTimer -= deltaTime;
            if (_respawnTimer <= 0)
                Respawn();
        }
    }
}
