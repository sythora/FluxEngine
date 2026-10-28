using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Input;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

// Player movement, jumping, animation state for the infinite platformer.
// Handles input, physics interaction, and sprite animation. Exposes Jumped
// and Landed events so the demo can play sounds through GameManager.
public sealed class PlayerController : Component
{
    private readonly InputActions _actions;
    private readonly ParticleEmitter _particles;

    private AnimatedSpriteRenderer _anim = null!;
    private Rigidbody2D _body = null!;

    private float _moveSpeed = 260f;
    private float _jumpForce = -700f;
    private float _maxFallSpeed = 1800f;

    private bool _facingRight = true;
    private bool _wasGrounded;
    private bool _justJumped;
    private float _runAnimTimer;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private int _jumpsRemaining = 2;
    private bool _jumpHeld;

    public event System.Action? Jumped;
    public event System.Action? Landed;

    public float MoveSpeed => _moveSpeed;
    public float JumpForce => MathF.Abs(_jumpForce);
    public float Gravity => _body.Gravity.Y;

    public PlayerController(InputActions actions, ParticleEmitter particles)
    {
        _actions = actions;
        _particles = particles;
    }

    public override void OnStart()
    {
        _anim = Entity.AddComponent<AnimatedSpriteRenderer>();
        _anim.Color = Color.White;
        _anim.Layer = 10;
        _anim.PlayOnStart = false;
        _anim.AddClip(new AnimationClip("Idle", [LoadSprite("player_idle")], 0.1f));
        _anim.AddClip(new AnimationClip("Run", [LoadSprite("player_run_0"), LoadSprite("player_run_1")], 0.12f));
        _anim.AddClip(new AnimationClip("Jump", [LoadSprite("player_jump")], 0.1f));
        _anim.AddClip(new AnimationClip("Fall", [LoadSprite("player_jump")], 0.1f));
        _anim.Play("Idle");

        _body = Entity.AddComponent<Rigidbody2D>();
        _body.Size = new Vector2(16, 28);
        _body.Gravity = new Vector2(0, 1600);
        _body.Drag = 0f;
        _body.Bounciness = 0.0f;
        _body.MaxFallSpeed = _maxFallSpeed;
    }

    public override void OnUpdate(float deltaTime)
    {
        var inputX = _actions.Axis("move.left", "move.right");
        var jumpPressed = _actions.WasPressed("jump");
        if (jumpPressed)
            _jumpBufferTimer = 0.15f;
        else
            _jumpBufferTimer = MathF.Max(0, _jumpBufferTimer - deltaTime);

        // Horizontal movement with responsive acceleration and crisp turning
        var targetVelX = inputX * _moveSpeed;
        var velX = _body.Velocity.X;
        float accel;
        if (inputX != 0)
        {
            accel = (MathF.Sign(velX) != 0 && MathF.Sign(velX) != MathF.Sign(targetVelX)) ? 4500f : 2400f;
            velX = MathUtil.MoveTowards(velX, targetVelX, accel * deltaTime);
        }
        else
        {
            accel = 2400f;
            velX = MathUtil.MoveTowards(velX, 0, accel * deltaTime);
        }

        _body.Velocity = new Vector2(velX, _body.Velocity.Y);

        if (inputX > 0) _facingRight = true;
        if (inputX < 0) _facingRight = false;

        // Ground grace, buffered presses, and one extra mid-air jump make
        // platform edges forgiving without hiding the basic two-jump limit.
        var grounded = _body.OnGround;
        if (grounded && !_wasGrounded)
            _jumpsRemaining = 2;

        if (grounded)
        {
            _coyoteTimer = 0.1f;
            _jumpsRemaining = 2;
        }
        else
        {
            _coyoteTimer = MathF.Max(0, _coyoteTimer - deltaTime);
        }

        if (_jumpBufferTimer > 0 && (grounded || _coyoteTimer > 0 || _jumpsRemaining > 0))
        {
            _body.Velocity = new Vector2(_body.Velocity.X, _jumpForce);
            _jumpBufferTimer = 0;
            _coyoteTimer = 0;
            _jumpsRemaining--;
            _justJumped = true;
            _jumpHeld = true;
            Jumped?.Invoke();
            _particles.Emit(Entity.Position + new Vector2(0, 16), 8);
        }

        if (_jumpHeld && _actions.WasReleased("jump"))
        {
            if (_body.Velocity.Y < 0)
                _body.Velocity = new Vector2(_body.Velocity.X, _body.Velocity.Y * 0.5f);
            _jumpHeld = false;
        }

        if (grounded && _justJumped && _body.Velocity.Y >= 0)
            _justJumped = false;

        // Landing detection
        if (_wasGrounded == false && grounded && _body.Velocity.Y > -50f)
        {
            Landed?.Invoke();
            _particles.Emit(Entity.Position + new Vector2(0, 16), 6);
        }

        _wasGrounded = grounded;

        // Animation state
        var absVelX = MathF.Abs(_body.Velocity.X);

        if (!grounded)
        {
            var clip = _body.Velocity.Y < 0 ? "Jump" : "Fall";
            _anim.Play(clip, restart: false);
        }
        else if (absVelX > 20f)
        {
            _anim.Play("Run", restart: false);
        }
        else
        {
            _anim.Play("Idle", restart: false);
        }

        // Flip the active clip consistently when changing animation states.
        foreach (var frame in _anim.Frames)
            frame.FlipX = !_facingRight;
        _anim.Entity.Scale = Vector2.One;

        // Dust puffs when running on ground
        if (grounded && absVelX > 60f)
        {
            _runAnimTimer += deltaTime;
            if (_runAnimTimer >= 0.25f)
            {
                _runAnimTimer = 0;
                _particles.Emit(Entity.Position + new Vector2(0, 16), 2);
            }
        }
    }

    public void ResetJumpState()
    {
        _coyoteTimer = 0;
        _jumpBufferTimer = 0;
        _jumpsRemaining = 2;
        _jumpHeld = false;
        _justJumped = false;
        _wasGrounded = false;
    }

    private Sprite LoadSprite(string name)
    {
        var engine = Engine.Current!;
        var texture = engine.Assets.LoadTexture(engine.Renderer, $"assets/{name}.png");
        return new Sprite(texture);
    }
}
