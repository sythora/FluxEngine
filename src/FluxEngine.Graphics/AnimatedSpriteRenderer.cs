using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Graphics;

// plays a list of sprites one after another. set Loop to false and it stops
// on the last frame
public sealed class AnimatedSpriteRenderer : Component, ISpriteDrawer
{
    public List<Sprite> Frames { get; } = [];
    public float FrameTime = 0.1f;
    public bool Loop = true;
    public bool PlayOnStart = true;

    public Color Color { get; set; } = Color.White;
    public int Layer { get; set; } = 0;

    private float _timer;
    private int _frame;
    private bool _playing;
    private bool _finished;

    public int CurrentFrame => _frame;
    public bool IsFinished => _finished;

    public override void OnStart()
    {
        if (PlayOnStart)
            Play();
    }

    public void Play()
    {
        _playing = true;
        _finished = false;
        _frame = 0;
        _timer = 0;
    }

    public void Stop()
    {
        _playing = false;
        _finished = true;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!_playing || Frames.Count == 0)
            return;

        _timer += deltaTime;

        while (_timer >= FrameTime)
        {
            _timer -= FrameTime;
            _frame++;

            if (_frame >= Frames.Count)
            {
                if (Loop)
                    _frame = 0;
                else
                {
                    _frame = Frames.Count - 1;
                    _playing = false;
                    _finished = true;
                    break;
                }
            }
        }
    }

    public override void OnDraw(Renderer renderer)
    {
        if (Frames.Count == 0 || _frame >= Frames.Count)
            return;

        var sprite = Frames[_frame];
        var scale = new Vector2(MathF.Abs(Entity.Scale.X), MathF.Abs(Entity.Scale.Y));
        renderer.DrawSprite(sprite, Entity.Position, Entity.Rotation, scale, Color);
    }
}
