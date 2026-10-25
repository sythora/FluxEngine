using FluxEngine.Core;
using FluxEngine.Graphics;
using FluxEngine.Mathematics;
using FluxEngine.Physics;

namespace FluxEngine.Graphics;

public sealed class AnimationClip
{
    public AnimationClip(string name, IEnumerable<Sprite> frames, float frameTime = 0.1f, bool loop = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(frames);
        if (!float.IsFinite(frameTime) || frameTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameTime), "Frame time must be finite and greater than zero.");

        Name = name;
        Frames = frames.ToArray();
        if (Frames.Count == 0)
            throw new ArgumentException("An animation clip must contain at least one frame.", nameof(frames));
        FrameTime = frameTime;
        Loop = loop;
    }

    public string Name { get; }
    public IReadOnlyList<Sprite> Frames { get; }
    public float FrameTime { get; }
    public bool Loop { get; }
}

// plays a list of sprites one after another. set Loop to false and it stops
// on the last frame
public sealed class AnimatedSpriteRenderer : Component, ISpriteDrawer
{
    public List<Sprite> Frames { get; } = [];
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);
    private float _frameTime = 0.1f;
    private bool _loop = true;

    public float FrameTime
    {
        get => _frameTime;
        set
        {
            if (!float.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Frame time must be finite and greater than zero.");
            _frameTime = value;
        }
    }

    public bool Loop { get => _loop; set => _loop = value; }
    public float PlaybackSpeed { get; set; } = 1f;
    public string? CurrentClipName { get; private set; }
    public IReadOnlyDictionary<string, AnimationClip> Clips => _clips;
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

    public void AddClip(AnimationClip clip, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!replace && _clips.ContainsKey(clip.Name))
            throw new InvalidOperationException($"An animation clip named '{clip.Name}' already exists.");
        _clips[clip.Name] = clip;
    }

    public bool RemoveClip(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var removed = _clips.Remove(name);
        if (removed && string.Equals(CurrentClipName, name, StringComparison.OrdinalIgnoreCase))
            CurrentClipName = null;
        return removed;
    }

    public bool Play(string name, bool restart = true)
    {
        if (!_clips.TryGetValue(name, out var clip))
            return false;
        if (!restart && string.Equals(CurrentClipName, clip.Name, StringComparison.OrdinalIgnoreCase) && _playing)
            return true;

        Frames.Clear();
        Frames.AddRange(clip.Frames);
        FrameTime = clip.FrameTime;
        Loop = clip.Loop;
        Play();
        CurrentClipName = clip.Name;
        return true;
    }

    public void Play()
    {
        CurrentClipName = null;
        _playing = true;
        _finished = false;
        _frame = 0;
        _timer = 0;
    }

    public void Stop()
    {
        _playing = false;
        _finished = true;
        CurrentClipName = null;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!_playing || Frames.Count == 0 || !float.IsFinite(PlaybackSpeed) || PlaybackSpeed <= 0)
            return;

        _timer += deltaTime * PlaybackSpeed;

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
