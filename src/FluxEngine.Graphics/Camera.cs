using FluxEngine.Mathematics;

namespace FluxEngine.Graphics;

// view into the world. zoom 1 means one world unit is one pixel. put it on
// Scene.Camera, otherwise everything draws straight in screen coordinates
public sealed class Camera
{
    public Vector2 Position;
    public float Zoom = 1f;

    // shake builds up as "trauma" and the actual offset is trauma squared,
    // so small hits are subtle and big ones are obvious
    public float ShakeDecay = 2.5f;
    public float MaxShakeOffset = 10f;

    private float _trauma;
    private Vector2 _shakeOffset;

    public Vector2 ShakeOffset => _shakeOffset;

    public void AddShake(float amount)
        => _trauma = MathF.Min(1f, _trauma + amount);

    public void Update(float deltaTime)
    {
        if (_trauma <= 0)
        {
            _shakeOffset = Vector2.Zero;
            return;
        }

        _trauma = MathF.Max(0, _trauma - ShakeDecay * deltaTime);

        var strength = _trauma * _trauma * MaxShakeOffset;
        _shakeOffset = new Vector2(
            Rng.Shared.NextFloat(-strength, strength),
            Rng.Shared.NextFloat(-strength, strength));
    }

    // call every frame with the thing you want to look at, smoothing is 0..1
    public void Follow(Vector2 target, float smoothing = 0.1f)
        => Position = Vector2.Lerp(Position, target, smoothing);

    public Vector2 WorldToScreen(Vector2 world, int viewWidth, int viewHeight)
    {
        var center = new Vector2(viewWidth * 0.5f, viewHeight * 0.5f);
        return (world - Position - _shakeOffset) * Zoom + center;
    }

    public Vector2 ScreenToWorld(Vector2 screen, int viewWidth, int viewHeight)
    {
        var center = new Vector2(viewWidth * 0.5f, viewHeight * 0.5f);
        return (screen - center) / Zoom + Position + _shakeOffset;
    }
}
